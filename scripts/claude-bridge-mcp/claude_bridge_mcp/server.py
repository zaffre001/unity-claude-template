#!/usr/bin/env python3
"""
ClaudeBridge MCP Server
=======================

ClaudeBridge의 파일 기반 IPC를 MCP 툴 몇 개로 감싸는 얇은 서버.

Claude Code / Claude Desktop에 등록하면 파일 read/write 루프 없이
`unity_call(op, args)` 한 번으로 Unity Editor 조작을 끝낸다.

툴:
    unity_call(op, args)       — ClaudeBridge op 하나 실행 후 결과 반환
    unity_batch_flush()        — inbox에 쌓인 커맨드를 headless Unity로 일괄 실행
    unity_bridge_status()      — 현재 상태 스냅샷

실행 (권장 — 프로젝트 로컬 `uv run`, 맥/윈도우/리눅스 동일):
    # 프로젝트 루트의 `.mcp.json` 이 아래를 자동으로 부른다:
    uv run --directory scripts/claude-bridge-mcp claude-bridge-mcp

    `uv` 가 없다면:
        macOS/Linux:  curl -LsSf https://astral.sh/uv/install.sh | sh
        Windows:      powershell -c "irm https://astral.sh/uv/install.ps1 | iex"

레거시 실행 (pipx, Claude Desktop 전용):
    pipx install /absolute/path/to/unity-claude-template/scripts/claude-bridge-mcp

프로젝트 루트 해석 순서:
    1) env var CLAUDE_BRIDGE_PROJECT      — 명시적 우선
    2) __file__ 위치 추론                  — `uv run`·`python -m` 으로 실행 시
    3) os.getcwd() 추론                    — Claude Code 는 `.mcp.json` 디렉터리를
                                             서버의 CWD 로 launch 하므로 맞을 때가 많다
"""
from __future__ import annotations

import json
import os
import subprocess
import sys
import time
import uuid
from pathlib import Path
from typing import Any

try:
    from mcp.server.fastmcp import FastMCP
except ImportError:
    sys.stderr.write(
        "ERROR: `mcp` 패키지를 찾을 수 없습니다.\n"
        "권장 실행 (프로젝트 로컬 uv):\n"
        "  uv run --directory scripts/claude-bridge-mcp claude-bridge-mcp\n"
        "또는 pipx:\n"
        "  pipx install /path/to/unity-claude-template/scripts/claude-bridge-mcp\n"
    )
    sys.exit(1)


IS_WINDOWS = sys.platform == "win32"


# ── 프로젝트 루트 결정 ──────────────────────────────────────────────────
# 우선순위:
#   1) env var CLAUDE_BRIDGE_PROJECT — pipx·명시적 지정 시
#   2) __file__ 위치에서 자동 추론 — `uv run`·`python -m` 실행 시
#   3) os.getcwd() — Claude Code `.mcp.json` 은 CWD 를 프로젝트 루트로 launch
def _resolve_project_root() -> Path:
    env = os.environ.get("CLAUDE_BRIDGE_PROJECT")
    if env:
        p = Path(env).expanduser().resolve()
        if not (p / "ProjectSettings" / "ProjectVersion.txt").exists():
            sys.stderr.write(
                f"WARNING: CLAUDE_BRIDGE_PROJECT={p} but no ProjectSettings/ProjectVersion.txt found.\n"
                "Check the path in your MCP config.\n"
            )
        return p

    # scripts/claude-bridge-mcp/claude_bridge_mcp/server.py → parents[3]이 프로젝트 루트
    try:
        candidate = Path(__file__).resolve().parents[3]
        if (candidate / "ProjectSettings" / "ProjectVersion.txt").exists():
            return candidate
    except (IndexError, OSError):
        pass

    # CWD 폴백 — Claude Code 가 `.mcp.json` 디렉터리를 CWD 로 서버를 실행할 때 유효
    cwd = Path.cwd().resolve()
    if (cwd / "ProjectSettings" / "ProjectVersion.txt").exists():
        return cwd

    sys.stderr.write(
        "ERROR: Could not locate Unity project root.\n"
        "Tried: CLAUDE_BRIDGE_PROJECT env var, __file__ traversal, CWD.\n"
        "Set CLAUDE_BRIDGE_PROJECT in your MCP config, or run from the project root.\n"
    )
    sys.exit(1)


PROJECT_ROOT = _resolve_project_root()

INBOX = PROJECT_ROOT / ".claude-bridge" / "inbox"
OUTBOX = PROJECT_ROOT / ".claude-bridge" / "outbox"
LOGS = PROJECT_ROOT / ".claude-bridge" / "logs"
PROJECT_VERSION_FILE = PROJECT_ROOT / "ProjectSettings" / "ProjectVersion.txt"


def _read_unity_version() -> str:
    for line in PROJECT_VERSION_FILE.read_text(encoding="utf-8").splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"m_EditorVersion not found in {PROJECT_VERSION_FILE}")


def _unity_editor_paths(version: str) -> list[Path]:
    """OS 별 Unity Editor 바이너리 후보 경로. 첫 번째 존재하는 것 사용."""
    home = Path.home()
    if sys.platform == "darwin":
        return [Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")]
    if sys.platform == "win32":
        program_files = Path(os.environ.get("ProgramFiles", r"C:\Program Files"))
        return [
            program_files / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity.exe",
            home / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity.exe",
        ]
    # Linux
    return [
        home / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity",
        Path(f"/opt/Unity/Hub/Editor/{version}/Editor/Unity"),
    ]


def _find_unity_editor() -> Path:
    version = _read_unity_version()
    for candidate in _unity_editor_paths(version):
        if candidate.exists():
            return candidate
    tried = "\n  ".join(str(p) for p in _unity_editor_paths(version))
    raise FileNotFoundError(
        f"Unity Editor {version} not found. Tried:\n  {tried}\n"
        f"Install Unity {version} via Unity Hub and retry."
    )


def _detect_unity_process() -> bool | None:
    """Editor 프로세스가 떠 있는지 간단 체크. 실패 시 None."""
    try:
        if sys.platform == "win32":
            proc = subprocess.run(
                ["tasklist", "/FI", "IMAGENAME eq Unity.exe", "/NH"],
                capture_output=True,
                text=True,
                timeout=3,
            )
            return "Unity.exe" in proc.stdout
        pattern = "Unity.app/Contents/MacOS/Unity" if sys.platform == "darwin" else "Unity/Hub/Editor"
        rc = subprocess.run(
            ["pgrep", "-f", pattern],
            capture_output=True,
            timeout=3,
        ).returncode
        return rc == 0
    except (FileNotFoundError, subprocess.TimeoutExpired, OSError):
        return None

# Unity Editor가 커맨드 하나를 집어 실행하기까지 기다릴 최대 시간.
# GUI 모드 폴링이 200ms이므로 10초면 네트워크·컴파일 기다림까지 충분.
DEFAULT_TIMEOUT_SEC = 10.0
POLL_INTERVAL_SEC = 0.1


def _ensure_folders() -> None:
    INBOX.mkdir(parents=True, exist_ok=True)
    OUTBOX.mkdir(parents=True, exist_ok=True)


def _submit_and_wait(op: str, args: dict[str, Any], timeout_sec: float) -> dict[str, Any]:
    """
    ClaudeBridge 커맨드 하나를 제출하고 결과를 polling으로 받아온다.

    Returns
    -------
    dict
        Unity C# 측 op의 result 구조체 (dataJson을 이미 파싱한 형태).

    Raises
    ------
    TimeoutError
        timeout_sec 안에 outbox에 결과가 나오지 않을 때.
    RuntimeError
        Unity 측에서 op 실행이 실패했을 때 (result.ok == false).
    """
    _ensure_folders()

    # 파일명은 타임스탬프 정렬 가능한 형태로. 배치 모드 순서 제어에도 이득.
    request_id = f"{int(time.time() * 1000)}-{uuid.uuid4().hex[:8]}"

    # ClaudeBridge는 JsonUtility 기반이라 argsJson이 문자열이어야 함 (이중 인코딩).
    envelope = {
        "id": request_id,
        "op": op,
        "argsJson": json.dumps(args or {}),
    }

    inbox_file = INBOX / f"{request_id}.json"
    outbox_file = OUTBOX / f"{request_id}.json"

    # 원자적 교체로 부분 작성 파일 노출 방지.
    tmp = inbox_file.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(envelope), encoding="utf-8")
    tmp.replace(inbox_file)

    deadline = time.monotonic() + timeout_sec
    while time.monotonic() < deadline:
        if outbox_file.exists():
            raw = outbox_file.read_text(encoding="utf-8")
            try:
                outbox_file.unlink()
            except FileNotFoundError:
                pass

            result = json.loads(raw)
            if not result.get("ok"):
                err = result.get("error") or "unknown error"
                stack = result.get("stack") or ""
                raise RuntimeError(f"{op} failed: {err}\n{stack}")

            # dataJson은 문자열 안의 JSON. 한 번 더 풀어서 돌려준다.
            data_str = result.get("dataJson") or "{}"
            try:
                return json.loads(data_str) if data_str else {}
            except json.JSONDecodeError:
                return {"_raw": data_str}

        time.sleep(POLL_INTERVAL_SEC)

    # 타임아웃: inbox에 남아 있으면 제거 시도 (처리 안됐다는 힌트).
    try:
        if inbox_file.exists():
            inbox_file.unlink()
    except OSError:
        pass

    raise TimeoutError(
        f"{op} did not complete within {timeout_sec}s. "
        "Unity Editor가 안 떠 있거나 Claude Bridge가 Start 상태가 아닐 수 있습니다. "
        "Editor를 열고 Window → Claude Bridge → Start 하거나, "
        "unity_batch_flush()를 호출해 headless로 처리하세요."
    )


# ── FastMCP 서버 ────────────────────────────────────────────────────────
mcp = FastMCP("claude-bridge")


@mcp.tool()
def unity_call(op: str, args: dict[str, Any] | None = None, timeout_sec: float = DEFAULT_TIMEOUT_SEC) -> dict[str, Any]:
    """
    ClaudeBridge op 하나를 Unity Editor에서 실행하고 결과를 반환합니다.

    Parameters
    ----------
    op : str
        ClaudeBridge op 이름. 예시:
        - "Scene.New", "Scene.Open", "Scene.Save"
        - "GameObject.Create", "GameObject.Find", "GameObject.Delete", "GameObject.SetTransform"
        - "Component.Add", "Component.SetField", "Component.GetField", "Component.SetRectTransform"
        - "Prefab.Open", "Prefab.Save", "Prefab.Close", "Prefab.GetCurrent",
          "Prefab.InstantiateAsChild", "Prefab.Apply", "Prefab.Unpack", "Prefab.CreateVariant"
        - "Asset.Refresh", "Asset.CreatePrefab"
        - "Reflection.Invoke"

        전체 op 레퍼런스: Assets/Editor/ClaudeBridge/README.md

    args : dict | None
        op별 args 구조. 예:
        - Scene.New          → {"path": "Assets/Scenes/Solitaire.unity"}
        - GameObject.Create  → {"name": "Canvas", "parentPath": null}
        - Component.Add      → {"path": "/Canvas", "type": "UnityEngine.UI.CanvasScaler"}
        - Component.SetRectTransform → {"path": "/Card", "sizeDelta": [140, 190], "pivot": [0.5, 0.5]}

    timeout_sec : float
        Unity가 응답하기까지 기다릴 최대 초. 기본 10.

    Returns
    -------
    dict
        op별 result 구조. 예:
        - Scene.New          → {"scenePath": "Assets/Scenes/Solitaire.unity"}
        - GameObject.Create  → {"path": "/Canvas", "instanceId": 12345}

    Notes
    -----
    Editor가 열려 있고 ClaudeBridge가 Start 상태이면 ~300ms 내 반환.
    Editor가 없으면 TimeoutError. 그럴 땐 inbox에 커맨드를 쌓고
    unity_batch_flush()를 호출하는 식으로 headless 처리 가능.
    """
    return _submit_and_wait(op, args or {}, timeout_sec)


@mcp.tool()
def unity_batch_flush(timeout_sec: float = 300.0) -> dict[str, Any]:
    """
    `.claude-bridge/inbox/*.json` 에 쌓인 커맨드를 headless Unity로 일괄 실행합니다.

    사용자가 Unity Editor를 열어두지 않은 경우에 쓰세요.
    Python 에서 Unity 를 직접 -batchmode 로 띄우고 (맥/윈도우/리눅스 자동 감지),
    inbox 전체를 처리한 뒤 종료합니다. 쉘(bash) 의존성 없음.

    Parameters
    ----------
    timeout_sec : float
        Unity 배치 프로세스 전체 타임아웃. Unity 구동 시간 포함해 기본 5분.

    Returns
    -------
    dict
        {
          "exit_code": int,      # 0=전부 성공, 1=일부 실패, 그 외=프로세스 실패
          "processed": int,      # outbox에 쓰인 result 파일 수 (실행 후 스냅샷)
          "remaining_inbox": int,
          "log_tail": str        # 로그 말미 20줄 (에러 파악용)
        }

    Notes
    -----
    Editor가 같은 프로젝트로 이미 떠 있으면 락 충돌로 실패합니다.
    그 경우 Editor를 닫거나 unity_call을 GUI 모드로 직접 쓰세요.
    """
    _ensure_folders()
    LOGS.mkdir(parents=True, exist_ok=True)

    unity_bin = _find_unity_editor()
    timestamp = time.strftime("%Y%m%d-%H%M%S")
    log_file = LOGS / f"bridge-{timestamp}.log"

    # -nographics 빼는 이유: Sprite.ImportFromSvg 처럼 GL/RenderTexture 가 필요한 op 은
    # -nographics 에서 no-op 으로 무음 실패 (빈 PNG 생성). macOS/Windows 에서는
    # -batchmode 만으로도 숨겨진 Metal/D3D 컨텍스트가 떠서 완전 비대화형 유지.
    cmd = [
        str(unity_bin),
        "-batchmode",
        "-quit",
        "-projectPath", str(PROJECT_ROOT),
        "-executeMethod", "Project.Editor.ClaudeBridge.ClaudeBridgeBatch.Run",
        "-logFile", str(log_file),
    ]

    proc = subprocess.run(
        cmd,
        cwd=str(PROJECT_ROOT),
        capture_output=True,
        text=True,
        timeout=timeout_sec,
    )

    log_tail = ""
    if log_file.exists():
        try:
            log_tail = "\n".join(log_file.read_text(encoding="utf-8").splitlines()[-20:])
        except OSError:
            pass

    return {
        "exit_code": proc.returncode,
        "processed": len(list(OUTBOX.glob("*.json"))),
        "remaining_inbox": len(list(INBOX.glob("*.json"))),
        "stdout_tail": "\n".join(proc.stdout.splitlines()[-10:]),
        "stderr_tail": "\n".join(proc.stderr.splitlines()[-10:]),
        "log_file": str(log_file),
        "log_tail": log_tail,
    }


@mcp.tool()
def unity_bridge_status() -> dict[str, Any]:
    """
    ClaudeBridge 상태 스냅샷. 디버깅용.

    Returns
    -------
    dict
        {
          "project_root": str,
          "platform": str,                 # darwin | win32 | linux
          "unity_version": str | None,     # ProjectVersion.txt 파싱 값
          "unity_editor_path": str | None, # OS 별 Unity Editor 탐지 (없으면 None — 설치 안 된 상태)
          "inbox_pending": int,
          "outbox_unread": int,
          "editor_running": bool | None,   # pgrep/tasklist 결과. 실패 시 None
        }
    """
    _ensure_folders()

    # Unity Editor 설치 경로 확인 (자동 세팅 검증용)
    unity_editor_path: str | None = None
    unity_version: str | None = None
    try:
        unity_version = _read_unity_version()
        unity_editor_path = str(_find_unity_editor())
    except (FileNotFoundError, OSError, RuntimeError):
        pass

    return {
        "project_root": str(PROJECT_ROOT),
        "platform": sys.platform,
        "unity_version": unity_version,
        "unity_editor_path": unity_editor_path,
        "inbox_pending": len(list(INBOX.glob("*.json"))),
        "outbox_unread": len(list(OUTBOX.glob("*.json"))),
        "editor_running": _detect_unity_process(),
    }


def main() -> None:
    """[project.scripts] 진입점. `uv run claude-bridge-mcp` 또는 pipx 설치 후 쉘에서 호출."""
    mcp.run()


if __name__ == "__main__":
    main()
