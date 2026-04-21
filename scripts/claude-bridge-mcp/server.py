#!/usr/bin/env python3
"""
ClaudeBridge MCP Server
=======================

ClaudeBridge의 파일 기반 IPC를 MCP 툴 두 개로 감싸는 얇은 서버.

Claude Desktop에 등록하면 Claude가 파일 read/write 루프 없이
`unity_call(op, args)` 한 번으로 Unity Editor 조작을 끝낸다.

툴:
    unity_call(op, args)       — ClaudeBridge op 하나 실행 후 결과 반환
    unity_batch_flush()        — inbox에 쌓인 커맨드를 headless Unity로 일괄 실행

왜 얇게 만드나:
    ClaudeBridge 본체(Unity C#)가 유일한 실행 주체여야 한다.
    Python은 파일 쓰기/읽기·타임아웃·배치 트리거만 담당.
    op 추가는 C# 쪽에만 하면 되고 이 서버는 그대로 통과시킨다.

설치:
    pip install mcp

Claude Desktop 등록 (claude_desktop_config.json):
    {
      "mcpServers": {
        "claude-bridge": {
          "command": "python3",
          "args": ["/absolute/path/to/unity-claude-template/scripts/claude-bridge-mcp/server.py"]
        }
      }
    }

    project root는 server.py 위치에서 자동 추론되므로 env var 불필요.
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
        "ERROR: `mcp` 패키지가 필요합니다. 설치: pip install mcp\n"
    )
    sys.exit(1)


# ── 프로젝트 루트 자동 추론 ─────────────────────────────────────────────
# scripts/claude-bridge-mcp/server.py → ../.. 가 프로젝트 루트.
# env var CLAUDE_BRIDGE_PROJECT 로 덮어쓸 수 있음.
SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = Path(
    os.environ.get("CLAUDE_BRIDGE_PROJECT", SCRIPT_PATH.parent.parent.parent)
).resolve()

INBOX = PROJECT_ROOT / ".claude-bridge" / "inbox"
OUTBOX = PROJECT_ROOT / ".claude-bridge" / "outbox"
BRIDGE_RUN = PROJECT_ROOT / "scripts" / "bridge-run.sh"

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
    내부적으로 scripts/bridge-run.sh를 호출하여 Unity를 -batchmode로 띄우고,
    inbox 전체를 처리한 뒤 종료합니다.

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
    if not BRIDGE_RUN.exists():
        raise FileNotFoundError(f"bridge-run.sh not found: {BRIDGE_RUN}")

    _ensure_folders()

    proc = subprocess.run(
        ["/bin/bash", str(BRIDGE_RUN)],
        cwd=str(PROJECT_ROOT),
        capture_output=True,
        text=True,
        timeout=timeout_sec,
    )

    # 로그 최근 파일 tail
    log_dir = PROJECT_ROOT / ".claude-bridge" / "logs"
    log_tail = ""
    if log_dir.exists():
        logs = sorted(log_dir.glob("bridge-*.log"))
        if logs:
            try:
                log_tail = "\n".join(logs[-1].read_text(encoding="utf-8").splitlines()[-20:])
            except OSError:
                pass

    return {
        "exit_code": proc.returncode,
        "processed": len(list(OUTBOX.glob("*.json"))),
        "remaining_inbox": len(list(INBOX.glob("*.json"))),
        "stdout_tail": "\n".join(proc.stdout.splitlines()[-10:]),
        "stderr_tail": "\n".join(proc.stderr.splitlines()[-10:]),
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
          "inbox_pending": int,
          "outbox_unread": int,
          "editor_running": bool | None,   # pgrep 결과. macOS/Linux만 정확
        }
    """
    _ensure_folders()
    editor_running: bool | None = None
    try:
        rc = subprocess.run(
            ["pgrep", "-f", "Unity.app/Contents/MacOS/Unity"],
            capture_output=True,
            timeout=2,
        ).returncode
        editor_running = (rc == 0)
    except (FileNotFoundError, subprocess.TimeoutExpired):
        pass

    return {
        "project_root": str(PROJECT_ROOT),
        "inbox_pending": len(list(INBOX.glob("*.json"))),
        "outbox_unread": len(list(OUTBOX.glob("*.json"))),
        "editor_running": editor_running,
    }


if __name__ == "__main__":
    mcp.run()
