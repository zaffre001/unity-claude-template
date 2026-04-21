# ClaudeBridge MCP Server

Claude Desktop이 Unity Editor를 **한 번의 툴 호출**로 조작할 수 있게 해주는 얇은 Python MCP 래퍼입니다.

[`../../Assets/Editor/ClaudeBridge/`](../../Assets/Editor/ClaudeBridge/) 의 파일 기반 IPC를 MCP 툴로 감싸기만 합니다. op 추가 같은 무거운 변경은 전부 C# 쪽에 하세요.

## 왜 필요한가

ClaudeBridge 본체만 있으면 Claude Desktop의 Filesystem MCP로도 동작합니다 — 단, Claude가 매번 "파일 쓰기 → 파일 읽기 루프"를 명시적으로 돌려야 합니다. 이 래퍼를 쓰면:

- `unity_call("Scene.New", {"path": "Assets/Scenes/Solitaire.unity"})` 한 줄로 완료
- 결과(JSON)가 그대로 반환값으로 돌아옴
- 타임아웃·폴링·ID 생성 전부 내부 처리

## 설치

### 1. Python 패키지

```bash
pip install mcp
```

Python 3.10 이상. macOS의 시스템 파이썬은 `python3` 명령으로 접근.

### 2. Claude Desktop에 등록

`~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) 열고 `mcpServers`에 추가:

```json
{
  "mcpServers": {
    "claude-bridge": {
      "command": "python3",
      "args": [
        "/Users/YOU/path/to/unity-claude-template/scripts/claude-bridge-mcp/server.py"
      ]
    }
  }
}
```

경로는 본인 위치로. 환경 변수는 기본적으로 필요 없음 — `server.py`가 자기 위치에서 프로젝트 루트를 추론합니다. 여러 프로젝트를 다룰 경우만 `env.CLAUDE_BRIDGE_PROJECT`로 덮어쓰세요.

Claude Desktop 재시작 후 Developer 탭에서 `claude-bridge` MCP가 떴는지 확인.

## 노출되는 툴

| 툴 | 역할 |
|---|---|
| `unity_call(op, args, timeout_sec?)` | ClaudeBridge op 하나 실행. Editor가 Start 상태여야 함. 반환은 op별 result dict |
| `unity_batch_flush(timeout_sec?)` | `.claude-bridge/inbox/` 쌓여있는 커맨드를 headless Unity로 일괄 실행. Editor 안 열려있어도 OK |
| `unity_bridge_status()` | Editor 가동 여부 + 큐 길이 스냅샷 (디버깅용) |

op 이름과 args 구조는 [`Assets/Editor/ClaudeBridge/README.md`](../../Assets/Editor/ClaudeBridge/README.md) 의 Op 레퍼런스 표를 그대로 사용.

## 동작 방식

```
Claude Desktop
      │
      │  unity_call("Component.Add", {"path": "/Canvas", "type": "UnityEngine.UI.Canvas"})
      ▼
server.py
      │  ID 생성 → .claude-bridge/inbox/<id>.json 드롭 → outbox/<id>.json 폴링
      ▼
Unity Editor (ClaudeBridgeServer polling)
      │  리플렉션 기반 핸들러 실행
      ▼
.claude-bridge/outbox/<id>.json  ← 결과 드롭
      │
      ▼
server.py (폴링 적중) → dataJson 파싱 → dict 반환
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

`unity_call`은 Editor가 GUI로 떠 있을 때 ~300ms 내 반환합니다. 헤드리스 처리를 염두에 두면 여러 호출을 타임아웃 없이 쌓아두고 마지막에 flush하는 패턴도 가능하지만, 현재 구현은 호출마다 결과를 동기 대기합니다. headless 전용으로 쓰려면 Editor를 먼저 닫고 `unity_batch_flush`를 쓰세요.

## 한계

- 현재 op 목록은 ClaudeBridge C# 본체에 하드코딩. 새 op 추가 시 이 서버는 재시작 불필요 — `unity_call` op 파라미터가 문자열이라 그대로 통과됩니다.
- 파일 락: Editor가 같은 프로젝트로 떠 있는데 `unity_batch_flush`를 호출하면 Unity가 "project already open" 에러로 실패. Editor를 닫고 호출하세요.
- Windows에서는 `subprocess`에서 `/bin/bash`를 직접 부르므로 WSL 또는 Git Bash가 필요. macOS/Linux는 자연스럽게 동작.
