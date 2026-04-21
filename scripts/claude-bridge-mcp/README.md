# ClaudeBridge MCP Server

Claude Desktop이 Unity Editor를 **한 번의 툴 호출**로 조작할 수 있게 해주는 얇은 Python MCP 래퍼입니다.

[`../../Assets/Editor/ClaudeBridge/`](../../Assets/Editor/ClaudeBridge/) 의 파일 기반 IPC를 MCP 툴로 감싸기만 합니다. op 추가 같은 무거운 변경은 전부 C# 쪽에 하세요.

## 왜 필요한가

ClaudeBridge 본체만 있으면 Claude Desktop의 Filesystem MCP로도 동작합니다 — 단, Claude가 매번 "파일 쓰기 → 파일 읽기 루프"를 명시적으로 돌려야 합니다. 이 래퍼를 쓰면:

- `unity_call("Scene.New", {"path": "Assets/Scenes/Solitaire.unity"})` 한 줄로 완료
- 결과(JSON)가 그대로 반환값으로 돌아옴
- 타임아웃·폴링·ID 생성 전부 내부 처리

## 설치

### 1. pipx 설치 (macOS 기준, 한 번만)

```bash
brew install pipx
pipx ensurepath
```

Windows는 `python -m pip install --user pipx` → `python -m pipx ensurepath`.

### 2. 이 서버를 pipx로 설치

```bash
pipx install /absolute/path/to/unity-claude-template/scripts/claude-bridge-mcp
```

경로는 본인이 템플릿을 풀어 놓은 자리로 바꿔 주세요. 설치가 끝나면 `claude-bridge-mcp` 커맨드가 PATH 에 생성됩니다:

```bash
$ which claude-bridge-mcp
/Users/YOU/.local/bin/claude-bridge-mcp
```

### 3. Claude Desktop 에 등록

`~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) 를 열고 `mcpServers` 에 다음을 추가:

```json
{
  "mcpServers": {
    "claude-bridge": {
      "command": "claude-bridge-mcp",
      "env": {
        "CLAUDE_BRIDGE_PROJECT": "/absolute/path/to/your/unity/project"
      }
    }
  }
}
```

`CLAUDE_BRIDGE_PROJECT` 는 **Unity 프로젝트 루트 경로** (ProjectSettings/ 폴더가 있는 자리) 로 지정.

Claude Desktop 재시작 후 Developer 탭에서 `claude-bridge` MCP 가 떴는지 확인하세요.

### 참고: 왜 pipx 인가, pip 아니고

`pip install mcp` 후 `python3 server.py` 방식은 맥OS Homebrew Python 의 `externally-managed-environment` 오류로 막히거나, 설치된 Python 과 Claude Desktop 이 부르는 `python3` 이 다른 환경일 때 `ImportError: No module named 'mcp'` 가 납니다. pipx 는 격리된 venv 에 깔고 CLI 커맨드만 PATH 에 노출하므로 이런 충돌이 없습니다.

### 업데이트

템플릿을 `git pull` 로 최신화한 뒤:

```bash
pipx install --force /absolute/path/to/unity-claude-template/scripts/claude-bridge-mcp
```

## 노출되는 툴

| 툴 | 역할 |
|---|---|
| `unity_call(op, args, timeout_sec?)` | ClaudeBridge op 하나 실행. Editor 가 Start 상태여야 함. 반환은 op별 result dict |
| `unity_batch_flush(timeout_sec?)` | `.claude-bridge/inbox/` 쌓여 있는 커맨드를 headless Unity 로 일괄 실행. Editor 안 열려 있어도 OK |
| `unity_bridge_status()` | Editor 가동 여부 + 큐 길이 스냅샷 (디버깅용) |

op 이름과 args 구조는 [`Assets/Editor/ClaudeBridge/README.md`](../../Assets/Editor/ClaudeBridge/README.md) 의 Op 레퍼런스 표를 그대로 사용.

## 동작 방식

```
Claude Desktop
      │
      │  unity_call("Component.Add", {"path": "/Canvas", "type": "UnityEngine.UI.Canvas"})
      ▼
claude-bridge-mcp (pipx venv)
      │  ID 생성 → .claude-bridge/inbox/<id>.json 드롭 → outbox/<id>.json 폴링
      ▼
Unity Editor (ClaudeBridgeServer polling)
      │  리플렉션 기반 핸들러 실행
      ▼
.claude-bridge/outbox/<id>.json  ← 결과 드롭
      │
      ▼
claude-bridge-mcp (폴링 적중) → dataJson 파싱 → dict 반환
      │
      ▼
Claude Desktop (ToolResult)
```

## 배치 vs 상호작용

| 상황 | 쓰는 툴 |
|---|---|
| Editor 열려 있고 실시간으로 보면서 작업 | `unity_call(...)` 여러 번 |
| Editor 안 켜놨고 한 번에 몰아 처리 | `unity_call(...)` 로 inbox에 쌓기 (타임아웃 무시) → `unity_batch_flush()` |
| 현재 상태 점검 | `unity_bridge_status()` |

`unity_call` 은 Editor 가 GUI 로 떠 있을 때 ~300ms 내 반환합니다. 헤드리스 처리를 염두에 두면 여러 호출을 타임아웃 없이 쌓아두고 마지막에 flush 하는 패턴도 가능하지만, 현재 구현은 호출마다 결과를 동기 대기합니다. headless 전용으로 쓰려면 Editor 를 먼저 닫고 `unity_batch_flush` 를 쓰세요.

## 개발 모드 (설치 없이 바로 돌리기)

코드 수정·디버깅 중이라면 설치 없이 직접 실행할 수 있습니다:

```bash
cd scripts/claude-bridge-mcp
pip install mcp     # 또는 uv pip install mcp
python -m claude_bridge_mcp.server
```

이 경우엔 `__file__` 위치로부터 프로젝트 루트를 자동 추론하므로 `CLAUDE_BRIDGE_PROJECT` env var 는 필요 없습니다. 단 Claude Desktop 에 등록할 때는 전체 경로를 `command` / `args` 로 꺼내는 형식으로 바꿔야 해요 — 이건 권장하지 않는 개발용 경로입니다.

## 한계

- 현재 op 목록은 ClaudeBridge C# 본체에 하드코딩. 새 op 추가 시 이 서버는 재시작 불필요 — `unity_call` op 파라미터가 문자열이라 그대로 통과됩니다. C# 쪽만 갱신하면 끝.
- 파일 락: Editor 가 같은 프로젝트로 떠 있는데 `unity_batch_flush` 를 호출하면 Unity 가 "project already open" 에러로 실패. Editor 를 닫고 호출하세요.
- Windows 는 `subprocess` 에서 `/bin/bash` 를 직접 부르므로 WSL 또는 Git Bash 가 필요. macOS/Linux 는 자연스럽게 동작.
