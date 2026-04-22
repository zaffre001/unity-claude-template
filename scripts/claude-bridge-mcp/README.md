# ClaudeBridge MCP Server

Claude Code (데스크탑 앱·CLI) 가 Unity Editor 를 **한 번의 툴 호출**로 조작할 수 있게 해주는 얇은 Python MCP 래퍼입니다.

[`../../Assets/Editor/ClaudeBridge/`](../../Assets/Editor/ClaudeBridge/) 의 파일 기반 IPC 를 MCP 툴로 감싸기만 합니다. op 추가 같은 무거운 변경은 전부 C# 쪽에 하세요.

## 왜 필요한가

ClaudeBridge 본체만 있으면 일반 파일 읽기/쓰기 툴로도 동작합니다 — 단, Claude 가 매번 "파일 쓰기 → 파일 읽기 루프"를 명시적으로 돌려야 합니다. 이 래퍼를 쓰면:

- `unity_call("Scene.New", {"path": "Assets/Scenes/Solitaire.unity"})` 한 줄로 완료
- 결과(JSON)가 그대로 반환값으로 돌아옴
- 타임아웃·폴링·ID 생성 전부 내부 처리
- 헤드리스 유니티 배치 실행까지 Python 에서 직접 처리 (bash 의존성 없음)

## 설치 — Claude Code (데스크탑 앱 / CLI)

프로젝트 루트의 [`.mcp.json`](../../.mcp.json) 이 이미 이 래퍼를 `uv run` 으로 올리도록 설정되어 있습니다. 템플릿을 Claude Code 로 열기만 하면 됩니다:

1. **`uv` 설치** — 맥·윈도우·리눅스 모두 1줄
   - macOS / Linux: `curl -LsSf https://astral.sh/uv/install.sh | sh`
   - Windows (PowerShell): `powershell -c "irm https://astral.sh/uv/install.ps1 | iex"`
2. **Claude Code 데스크탑 앱** 실행 → **Code 탭** → **Project folder** 로 이 템플릿 루트 선택 (CLI 사용자는 `cd <템플릿 루트> && claude`)
3. MCP 서버 승인 다이얼로그에서 **Approve**
4. 템플릿에 담긴 `/setup` 스킬로 최종 확인 (`/setup 해 줘`)

수동 등록이 필요한 상황은 거의 없습니다. `.mcp.json` 이 사라졌거나 커스터마이징한 경우 다음 내용을 프로젝트 루트에 다시 두세요:

```json
{
  "mcpServers": {
    "claude-bridge": {
      "command": "uv",
      "args": ["run", "--directory", "scripts/claude-bridge-mcp", "claude-bridge-mcp"]
    }
  }
}
```

### 왜 uv 인가, pipx 아니고

- **크로스 플랫폼**: 동일한 1줄 설치자. pipx 는 맥·리눅스 친화적이고 윈도우에선 `python -m pipx` 우회가 필요.
- **venv 자동 관리**: `uv run` 이 실행 시 의존성을 해석해 캐시된 venv 에서 서버를 구동. 사용자가 가상환경을 의식할 필요 없음.
- **업데이트 0단계**: 템플릿을 `git pull` 하면 끝. pipx 는 매번 `pipx install --force` 필요.
- **pip/Homebrew Python 충돌 회피**: 맥 홈브루 파이썬의 `externally-managed-environment` 오류가 안 발생.

### 레거시 경로 — Claude Desktop (chat 앱)

**이 템플릿의 타깃은 Claude Code** 입니다. Claude Desktop chat 앱(claude.ai 클라이언트) 은 별개 제품이며 `.mcp.json` 을 자동 로드하지 않습니다. 그래도 쓰시려면:

1. `pipx install ./` (이 디렉터리 기준)
2. `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) 또는 `%APPDATA%\Claude\claude_desktop_config.json` (Windows) 에 다음 블록 추가:
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
3. Claude Desktop 재시작 → Developer 탭에서 `claude-bridge` MCP 확인
4. 업데이트 시 `pipx install --force ./`

권장하지 않습니다. Claude Code (데스크탑 앱·CLI) 는 slash command, skill, CLAUDE.md 등 이 템플릿의 전체 스택을 지원하지만 Claude Desktop chat 앱은 그렇지 않습니다.

## 노출되는 툴

| 툴 | 역할 |
|---|---|
| `unity_call(op, args, timeout_sec?)` | ClaudeBridge op 하나 실행. Editor 가 Start 상태여야 함. 반환은 op별 result dict |
| `unity_batch_flush(timeout_sec?)` | `.claude-bridge/inbox/` 쌓여 있는 커맨드를 headless Unity 로 일괄 실행. Editor 안 열려 있어도 OK. Python 에서 직접 OS 별 Unity 바이너리를 호출 (bash 의존성 없음) |
| `unity_bridge_status()` | 프로젝트 루트·플랫폼·Unity 버전·Editor 경로·큐 길이 스냅샷 (디버깅용) |

op 이름과 args 구조는 [`Assets/Editor/ClaudeBridge/README.md`](../../Assets/Editor/ClaudeBridge/README.md) 의 Op 레퍼런스 표를 그대로 사용.

## 동작 방식

```
Claude Code (데스크탑 앱 / CLI)
      │
      │  unity_call("Component.Add", {"path": "/Canvas", "type": "UnityEngine.UI.Canvas"})
      ▼
claude-bridge-mcp (uv 가 관리하는 venv)
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
Claude Code (ToolResult)
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
uv run claude-bridge-mcp        # 권장 — venv 자동 관리
# 또는
pip install mcp && python -m claude_bridge_mcp.server
```

`__file__` 위치로부터 프로젝트 루트를 자동 추론하므로 `CLAUDE_BRIDGE_PROJECT` env var 는 필요 없습니다. CWD 가 프로젝트 루트인 경우도 자동으로 맞춰집니다.

## 프로젝트 루트 해석 우선순위

1. env var `CLAUDE_BRIDGE_PROJECT` (레거시 pipx 경로에서 사용)
2. `__file__` 위치 (parents[3], `uv run`·`python -m` 경로에서 유효)
3. `os.getcwd()` — Claude Code 가 `.mcp.json` 디렉터리를 CWD 로 launch

## 한계

- 현재 op 목록은 ClaudeBridge C# 본체에 하드코딩. 새 op 추가 시 이 서버는 재시작 불필요 — `unity_call` op 파라미터가 문자열이라 그대로 통과됩니다. C# 쪽만 갱신하면 끝.
- 파일 락: Editor 가 같은 프로젝트로 떠 있는데 `unity_batch_flush` 를 호출하면 Unity 가 "project already open" 에러로 실패. Editor 를 닫고 호출하세요.
- Windows 네이티브 지원: `unity_batch_flush` 는 Python 에서 Unity 바이너리를 직접 호출하므로 Git Bash/WSL 없이 동작. `tasklist` 기반으로 Editor 프로세스도 감지.
