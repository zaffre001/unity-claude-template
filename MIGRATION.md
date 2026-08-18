# Unity Claude Template legacy and migration guide

> - **Repository status:** archived on 2026-08-19
> - **Final legacy release:** `v1.0.0-legacy`
> - **Successor:** [Unity Agent Kit](https://github.com/zaffre001/unity-agent-kit)

## 한국어

### 지금 당장 마이그레이션할 필요는 없습니다

이 저장소를 복제해서 만든 게임은 그대로 동작합니다. 저장소를 보관한다고 해서 기존 클론이나
포크의 파일이 바뀌거나 삭제되지는 않습니다. 현재 프로젝트를 계속 개발해야 한다면 이 최종
상태를 유지해도 됩니다.

Unity Agent Kit은 같은 저장소의 다음 버전이 아닙니다. 완성된 Unity 프로젝트를 복제하는 템플릿에서,
기존 Unity 프로젝트에 추가하는 설치형 패키지로 제품 형태가 바뀝니다. 따라서 이 저장소의 파일을
새 저장소 내용으로 덮어쓰거나 두 저장소 사이에서 `git pull`하지 마세요.

### 후속 패키지가 준비될 때까지

- 이 저장소의 `main`과 `v1.0.0-legacy`는 동일한 최종 legacy 상태로 보존됩니다.
- 기존 `.mcp.json`, `.claude/`, `CLAUDE.md`, `Assets/Editor/ClaudeBridge/`, `scripts/`를
  수동으로 삭제하지 마세요. 현재 템플릿에서는 서로 연결된 구성요소입니다.
- 새 기능, 버그 제보, 설치형 패키지 진행 상황은
  [Unity Agent Kit](https://github.com/zaffre001/unity-agent-kit)에서 확인해 주세요.

### 향후 마이그레이션 원칙

Unity Agent Kit의 첫 안정 릴리스에는 다음 순서의 opt-in 마이그레이션을 제공합니다.

1. 현재 프로젝트를 커밋하고 별도 백업 브랜치를 만듭니다.
2. Unity Agent Kit 패키지를 설치합니다.
3. legacy 감지 도구로 ClaudeBridge, Python MCP, Claude 전용 설정을 확인합니다.
4. 공유 지침과 스킬을 Claude Code·Codex 호환 구조로 변환합니다.
5. 공식 Unity CLI와 `com.unity.pipeline` 연결을 검증합니다.
6. 검증에 성공한 항목만 legacy 구성에서 제거합니다.

이 과정은 기존 게임 코드, 에셋, 씬, `ProjectSettings`를 자동 삭제하거나 덮어쓰지 않는 방향으로
제공할 예정입니다. 마이그레이션 도구가 공개되기 전에는 수동 변환보다 현 상태 유지가 안전합니다.

### legacy 구성 목록

아래 항목은 이 템플릿에서는 정상이며, 향후 설치형 패키지에서 대체될 대상입니다.

| Legacy 구성 | 역할 | 후속 방향 |
|---|---|---|
| `.mcp.json` | Python MCP 서버 등록 | 공식 Unity CLI 또는 내장 `unity mcp` |
| `scripts/claude-bridge-mcp/` | 파일 IPC를 MCP로 노출 | Unity CLI + Unity Pipeline |
| `Assets/Editor/ClaudeBridge/` | Editor 조작용 커스텀 브리지 | `com.unity.pipeline` 명령과 `eval` |
| `.claude/`, `CLAUDE.md` | Claude 전용 지침·스킬 | Claude Code·Codex 공용 설치 자산 |
| `scripts/bridge-run.sh` | headless 큐 실행 | `unity run`, `unity command`, `unity test` |

## English

### You do not need to migrate immediately

Games created from this repository remain usable. Archiving the upstream repository does not alter or
delete files in existing clones or forks. You may keep developing against this final legacy state.

Unity Agent Kit is not an in-place major version of this repository. The product changes from a complete
Unity project template into a package installed into an existing Unity project. Do not overwrite an
existing project with the new repository or attempt to `git pull` across the two unrelated repositories.

Until the successor package reaches its first stable release, keep the connected legacy components in
place. The future migration will be opt-in, start from a committed backup, detect legacy files, install
the official Unity CLI/Pipeline integration, and remove only components that were successfully replaced.
It will not intentionally delete game code, assets, scenes, or `ProjectSettings`.

Track package development and migration releases in
[Unity Agent Kit](https://github.com/zaffre001/unity-agent-kit).
