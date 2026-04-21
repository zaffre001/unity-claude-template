# Unity Claude Template

Claude Code 에이전트와 함께 유니티 게임을 만들 때 바로 시작할 수 있는 스켈레톤 프로젝트입니다.
블로그 3부작 "유니티 × Claude Code"에서 설계한 에이전트 하네스, 병렬 워크트리, DAP 디버깅 환경을 하나의 템플릿으로 묶었습니다.

## 이 템플릿이 제공하는 것

- **어셈블리 분리된 스켈레톤** — `_Core`, `_UI`, `_Combat`, `_Rendering` 네 개의 `.asmdef`와 테스트 어셈블리 두 개
- **에이전트 지침 파일 생태계**
  - [`CLAUDE.md`](CLAUDE.md) — 팀 공유 도메인 지식 (에이전트가 세션 시작 시 읽음)
  - [`RULES.md`](RULES.md) — 불변 제약 6개 (Domain Reload, 심링크, `.meta`, 물리, async, ProjectSettings)
  - [`.claude/skills/`](.claude/skills/) — `/self-update`, `/task-start`, `/task-done` 슬래시 커맨드
  - [`.claude/rules/`](.claude/rules/) — 경로·파일 타입별 스코프 규칙
- **병렬 에이전트 워크트리 도구** — [`scripts/`](scripts/)
  - `create-worktrees.sh`, `setup-symlinked-worktree.sh`, `create-symlinked-worktrees.sh`, `cleanup-worktrees.sh`
  - Git Worktree + 심링크로 소스코드만 격리, Library/Assets 바이너리는 읽기 전용 공유
- **Domain Reload 비활성화 설정** — [`Assets/Editor/ParallelAgentSetup.cs`](Assets/Editor/ParallelAgentSetup.cs)
  - Play Mode 진입 대기 시간을 사실상 0으로
  - `static` 필드는 `[RuntimeInitializeOnLoadMethod]`로 수동 초기화 필요 (자세한 내용은 CLAUDE.md §4)

## 요구 사항

- Unity 2022.3 LTS 이상
- Git 2.20+
- [Claude Code CLI](https://claude.com/claude-code)

## 시작하기

```bash
# 1. 이 템플릿을 포크하거나 클론합니다
git clone <your-fork-url> my-game
cd my-game

# 2. Unity Hub에서 프로젝트를 열어 에디터가 임포트를 마칠 때까지 대기
#    (Library는 로컬에서 최초 1회 생성됨)

# 3. 단일 세션으로 시작하려면
claude
```

### 병렬 에이전트로 시작하려면

```bash
# 메인 프로젝트 루트에서
./scripts/create-symlinked-worktrees.sh 3

# 터미널 3개에서 각각
cd ../worktrees/agent-0 && claude
cd ../worktrees/agent-1 && claude
cd ../worktrees/agent-2 && claude
```

각 세션에 서로 다른 어셈블리(`_UI`, `_Combat`, `_Rendering`) 작업을 지시하면 Domain Reload 경합 없이 병렬로 진행됩니다. 머지 충돌도 최소화됩니다.

## 내 프로젝트로 변환하기

1. `ProjectSettings/ProjectSettings.asset`의 `productName`을 본인 게임 이름으로 변경
2. `CLAUDE.md`의 네임스페이스 루트 `Project.*`를 본인 이름(예: `MyGame.*`)으로 일괄 치환
3. 각 `.asmdef` 파일의 `"name"` 필드도 동일하게 변경
4. `CLAUDE.md`의 "1. 프로젝트 개요" 섹션을 본인 프로젝트에 맞게 갱신

## 에이전트 워크플로우 요약

```
/task-start   ← 범위를 먼저 확정한다
     ↓
 작업 실행     ← 범위 안에서만 움직인다
     ↓
/task-done    ← 마무리를 빠뜨리지 않는다
     ↓
/self-update  ← 실수가 지침으로 흡수된다
     ↓
 다음 태스크   ← 같은 실수를 하지 않는다
```

## 기반 블로그 시리즈

1. [유니티 × Claude Code: 에이전트의 뇌를 설계하는 법](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-AI-에이전트를-실무에-쓴다)
2. [유니티 × Claude Code: Domain Reload 없는 병렬 에이전트 워크트리 설계](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-병렬-에이전트-설계)
3. [유니티 × Claude Code: DAP 기반으로 에이전트가 직접 디버깅하는 환경 만들기](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-DAP-기반-에이전트-환경-만들기)

## 라이선스

[MIT](LICENSE)
