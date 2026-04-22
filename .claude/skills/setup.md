---
name: setup
description: 이 템플릿을 처음 클론한 사용자를 위한 자동 부트스트랩. 맥/윈도우 어느 쪽이든 `uv` 설치 여부·`.mcp.json` 존재·Unity Editor 존재·Bridge MCP 연결을 순서대로 점검하고, 빠진 것만 한 번에 고쳐 끝까지 데려간다. 프로그래밍 몰라도 되게 설계됨.
---

# Skill: /setup (프로젝트 자동 세팅)

**언제 돌리나**: 이 템플릿을 처음 클론했을 때 한 번. 또는 다른 컴퓨터에서 열었을 때 한 번. 세팅이 이미 돼 있으면 빠르게 검사만 하고 끝난다.

**사용 환경**: **Claude Code 데스크탑 앱** (Mac/Windows) 이 1차 타깃. Claude Code CLI (`claude`) 도 동일하게 작동 — `.mcp.json`/`settings.json`/CLAUDE.md 모두 공유. **Claude Desktop (chat 앱)** 은 별개 제품이고 `claude_desktop_config.json` 이 자동 연동되지 않는다 — §5 레거시 경로 참고.

**목표**: 사용자는 "`/setup` 해 줘" 한 줄만. 에이전트가 OS 감지 → `uv` 확인 → 필요시 설치 안내 → `.mcp.json` 확인 → Bridge MCP 응답 확인까지 끝낸다.

---

## 0. 실행 원칙

- **사용자 타이핑 0 이 목표**. 사용자가 직접 쳐야 하는 건 단 둘: (a) `/setup` 한 번, (b) 권한 다이얼로그 "Approve" 버튼. 그 외 모든 명령은 **Claude 가 Bash 툴로 직접 실행**한다. "이 명령을 복사해서 터미널에 붙여넣으세요" 는 금지.
- **파괴적 행위 금지**. 기존 설정 파일 덮어쓰기 전에는 반드시 사용자 확인.
- **OS 감지** 후 맥/윈도우/리눅스 브랜치 선택. Bash 툴에서 `uname -s` (맥/리눅스), `$OS` 환경변수 (`Windows_NT`), 또는 `python -c "import sys; print(sys.platform)"` 로 구분.
- **권한 프롬프트 최소화**. `.claude/settings.json` 에 무해한 명령들을 미리 allow 해 둔 덕분에 대부분은 프롬프트 0 회. 네트워크 다운로드 (`curl | sh`) 같은 위험한 명령만 1 회 승인 받는다.
- 한 스텝 끝날 때마다 **한 줄 요약**: "✓ uv 확인 완료" / "✗ Unity Editor 미설치 — 사용자에게 설치 링크 안내".

---

## 1. 프리체크 — 이미 다 돼 있나?

이 세 가지가 모두 참이면 **아무 것도 할 것 없음**. 상태 한 줄 보고하고 끝.

```
[A] .mcp.json 파일이 프로젝트 루트에 존재
[B] `uv --version` 성공
[C] `mcp__claude-bridge__unity_bridge_status` 호출 성공 + project_root 가 이 레포로 잡혀 있음
```

에이전트 실행:
- Read: `.mcp.json`
- Bash: `uv --version` (실패해도 계속)
- MCP 툴: `mcp__claude-bridge__unity_bridge_status` (실패해도 계속)

**모두 OK**: "이미 세팅 완료된 상태입니다. `unity_bridge_status` 응답: {요약}" 한 줄 후 종료.

---

## 2. 스텝별 세팅 (빠진 것만)

### Step 1 — `uv` 설치 확인 (필요하면 Claude 가 직접 설치)

`uv` 는 Python 가상환경·의존성 관리자. 맥/윈도우/리눅스 1개 바이너리. 이 템플릿은 Bridge MCP 서버를 `uv run` 으로 돌려서 **pipx·pip 전역 설치 없이** 작동한다.

**1-a. 이미 있는지 확인 (Bash 툴 직접 실행, 프리어프루브 돼 있어 프롬프트 0회)**:

```bash
uv --version || ~/.local/bin/uv --version || echo "MISSING"
```

**1-b. 없으면 Claude 가 설치 명령을 Bash 툴로 직접 실행**. 사용자에게 "이 명령 치세요" 하지 말고 **실행 승인만** 받는다.

- **macOS / Linux**: `curl -LsSf https://astral.sh/uv/install.sh | sh`
- **Windows (PowerShell)**: `powershell -c "irm https://astral.sh/uv/install.ps1 | iex"` — Windows 에서 Claude Code 가 bash 가 아닌 PowerShell 을 쓴다면 Bash 툴로 그대로 호출. Git Bash 환경이면 이 명령을 `pwsh.exe -c ...` 로 감쌀 것.

실행 승인 다이얼로그 1회만 뜬다 (네트워크 다운로드 + 파일 시스템 변경이라 이건 **프리어프루브 안함** — 정당한 안전 장치).

**1-c. 설치 직후 PATH 반영**. 같은 쉘 세션에서는 `uv` 가 PATH 에 없을 수 있으므로:

```bash
~/.local/bin/uv --version       # macOS / Linux
"$USERPROFILE/.local/bin/uv.exe" --version    # Windows (Git Bash)
```

후속 명령도 풀 경로로 호출하거나, Claude Code 세션을 껐다 다시 시작하면 PATH 가 반영된다.

> 공식 문서: <https://docs.astral.sh/uv/getting-started/installation/>

### Step 2 — `.mcp.json` 확인

프로젝트 루트에 `.mcp.json` 이 있는지 확인. **이 템플릿은 기본으로 커밋되어 있다**. 없으면 (사용자가 실수로 지웠거나 커스터마이징한 경우) 아래 내용으로 생성:

```json
{
  "mcpServers": {
    "claude-bridge": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "scripts/claude-bridge-mcp",
        "claude-bridge-mcp"
      ]
    }
  }
}
```

생성·수정한 경우 데스크탑 앱이면 "세션을 새로 시작(또는 같은 프로젝트 폴더를 다시 선택)해 주세요", CLI 면 "Claude Code 를 종료했다 다시 열어 주세요" 고지. Claude Code 데스크탑 앱은 프로젝트 폴더를 선택해 세션이 시작된 순간 `.mcp.json` 을 읽는다. 권한 승인 다이얼로그가 뜨면 사용자에게 **Approve** 하도록 안내.

### Step 3 — Unity Editor 설치 확인

`mcp__claude-bridge__unity_bridge_status` 가 성공하면 응답에 `unity_version` / `unity_editor_path` 가 있다.

- `unity_editor_path` 가 **null**: Unity Hub 에서 해당 버전 설치 필요. 버전은 `ProjectSettings/ProjectVersion.txt` 의 `m_EditorVersion` 값. Unity Hub 링크 안내: <https://unity.com/download>
- **있음**: 다음 스텝.

### Step 4 — Bridge MCP 연결 테스트

```
mcp__claude-bridge__unity_bridge_status()
```

기대 응답:
```json
{
  "project_root": "/절대/경로/이/레포",
  "platform": "darwin" | "win32" | "linux",
  "unity_version": "2022.3.x",
  "unity_editor_path": "/Applications/Unity/Hub/Editor/.../Unity" | "C:\\...\\Unity.exe",
  "editor_running": false,
  ...
}
```

- `project_root` 가 **이 레포 경로와 다르면**: `.mcp.json` 을 프로젝트 루트에서 부르고 있는지 확인. 다른 디렉터리에서 Claude Code 를 켰을 수도 있다.
- `editor_running: false` 는 정상 (아직 Unity 안 띄웠으니). `/run editor` 로 띄워 본다.

### Step 5 — 최종 스모크 테스트 (선택)

사용자가 바로 써보고 싶다면:
- 에이전트가 `/run bridge` 로 헤드리스 테스트 op (예: `unity_bridge_status` 만) 돌려 연결 확인.
- 또는 `/run editor` 로 GUI 띄우고 `Window → Claude Bridge → Start` 눌렀는지만 확인하도록 안내.

---

## 3. OS별 주의사항

### macOS
- `uv` 설치 후 새 터미널 창 열거나 `source ~/.zshrc` / `~/.zprofile` 로 PATH 리로드 필요할 수 있음.
- Unity Hub / Unity Editor 는 macOS Gatekeeper 첫 실행 시 "열기" 허용 다이얼로그 뜨는 게 정상.
- zsh 의 `$status` 등 예약어 주의 (CLAUDE.md §8 참고).

### Windows
- **PowerShell 권장**. Git Bash 에서 `uv` 도 동작하지만 PATH 세팅은 PowerShell 기준.
- 만약 사용자가 **WSL** 안에서 Claude Code 를 띄우고 있다면 Unity 는 Windows 쪽에서 도는 중이라 경로가 어긋날 수 있음. 이 경우 WSL 이 아니라 네이티브 Windows 터미널에서 Claude Code 를 쓰도록 안내.
- `mcp__claude-bridge__unity_bridge_status` 결과의 `platform` 이 `"win32"` 인지 확인.

### Linux
- `uv` 설치 후 `~/.local/bin` 이 PATH 에 있는지 (`echo $PATH`) 확인.
- Unity Hub 는 AppImage 배포. `$HOME/Unity/Hub/Editor/<version>/Editor/Unity` 에 있어야 자동 감지 됨.

---

## 4. 폴백·트러블슈팅

| 증상 | 진단 | 조치 |
|---|---|---|
| `/mcp` 에 `claude-bridge` 가 안 뜸 | `.mcp.json` 변경이 적용 안 됨 | 데스크탑 앱: 세션 종료 → 같은 프로젝트 폴더로 새 세션. CLI: `claude` 종료 후 재실행 |
| 데스크탑 앱에서 프로젝트 폴더가 잘못 잡힘 | Code 탭에서 다른 폴더로 세션 시작함 | Code 탭 → Project folder 를 이 템플릿 루트로 다시 선택 (`ProjectSettings/` 가 있는 디렉터리) |
| `uv` 는 있는데 첫 `unity_bridge_status` 가 느림 (10~30초) | `uv` 가 처음으로 venv 만드는 중 | 정상. 이후부터는 즉시 응답 |
| `unity_batch_flush` 에서 `Unity Editor ... not found` | Unity 버전 미설치 또는 다른 경로 설치 | Unity Hub 에서 해당 버전 설치. 또는 `CLAUDE_BRIDGE_PROJECT` 가 올바른 프로젝트를 가리키는지 확인 |
| Windows 에서 `UnicodeEncodeError` 로그 | 코드페이지 문제 | PowerShell 에서 `chcp 65001` (UTF-8) |
| 모든 게 정상인데 Editor GUI 에 `Claude Bridge` 메뉴가 없음 | `Assets/Editor/ClaudeBridge/` 가 컴파일 안 된 상태 | Unity Editor 에서 **Assets → Reimport All** |

---

## 5. 대체 경로 — Claude Desktop (chat 앱) 레거시

**Claude Code 데스크탑 앱 / Claude Code CLI** 를 쓰면 이 템플릿의 `.mcp.json` 이 자동으로 잡힌다. 문서를 혼동하지 말 것:

| 제품 | 이 템플릿과의 관계 |
|---|---|
| Claude Code 데스크탑 앱 (Mac/Windows) | ✅ 1차 타깃. `.mcp.json` 자동 로드. Code 탭 → Project folder 로 폴더 선택 |
| Claude Code CLI (`claude` in terminal) | ✅ 동일하게 작동. 설정 파일(`.mcp.json`/`settings.json`/CLAUDE.md) 데스크탑 앱과 공유 |
| **Claude Desktop (chat 앱, claude.ai)** | ❌ 별개 제품. `claude_desktop_config.json` 은 이 템플릿과 무관 |

Claude Desktop chat 앱에서 브릿지를 쓰려는 사용자가 **정말** 있다면 (권장하지 않음):

1. `pipx install ./scripts/claude-bridge-mcp`
2. `~/Library/Application Support/Claude/claude_desktop_config.json` (또는 Windows `%APPDATA%\Claude\claude_desktop_config.json`) 에 수동으로 아래 블록 추가:
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
3. Claude Desktop 재시작.

이 경로는 **권장하지 않는다**. 업데이트 시 매번 `pipx install --force` 필요하고, Claude Code (CLI·데스크탑 앱) 가 지원하는 slash command/skill/CLAUDE.md 등이 모두 안 된다.

---

## 6. 완료 보고 포맷

스텝을 끝낸 뒤 사용자에게 이렇게 한 번에 보고:

```
세팅 점검 결과
  ✓ uv 설치 확인 (0.5.x)
  ✓ .mcp.json 발견
  ✓ Unity Editor 감지: /Applications/Unity/Hub/Editor/2022.3.45f1/Unity.app
  ✓ Bridge MCP 응답: project_root = /Users/.../unity-claude-template

다음 단계
  · /run editor 로 Unity 를 띄워 보기
  · README §3 "솔리테어 연습" 진행
```

빠진 게 있으면 ✗ 로 표시하고 바로 옆에 조치 1줄.

---

## 7. 가드레일 (RULES.md 연동)

- `.mcp.json` 은 프로젝트 공유 설정이라 커밋에 포함된다. 사용자 개인용 MCP 서버가 필요하면 `.mcp.json` 을 수정하지 말고 `.claude/settings.local.json` 이나 사용자 스코프 `~/.claude.json` 을 쓰도록 안내.
- 이 스킬은 `Assets/`·`ProjectSettings/`·`Packages/` 를 건드리지 않는다. 세팅 문제만 다룬다.
- `scripts/claude-bridge-mcp/` 는 이 스킬의 조사 대상. **수정 금지**. 버그면 사용자 승인 후 별도 작업으로 분리.
