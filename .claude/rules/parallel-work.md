# 병렬 워크트리 작업 규칙

여러 워크트리에서 동시에 작업할 때 지켜야 할 배치·편집 규칙.

## 1. 심링크 폴더 자체는 계속 읽기 전용 (RULES.md RULE-02 유지)

다음 폴더는 워크트리에서 **수정 금지**:

- `Assets/Art/`, `Assets/Audio/`, `Assets/Prefabs/`, `Assets/Materials/`
- `Assets/Textures/`, `Assets/Models/`, `Assets/Plugins/`, `Assets/Resources/`
- `ProjectSettings/`, `Packages/`

## 2. 새 에셋·프리팹은 "기능 폴더" 안에 둔다

기능 전용으로 **새로 만드는** 프리팹·머티리얼·텍스처는 `Assets/Prefabs/` 같은 공유 심링크 폴더가 아니라 **그 기능을 쓰는 스크립트 바로 옆**에 둔다.

- ✅ `Assets/Scripts/_UI/Game2048/Celebration/CelebrationVfx.prefab`
- ✅ `Assets/Scripts/_UI/Game2048/PauseMenu/PauseMenu.prefab`
- ❌ `Assets/Prefabs/PauseMenu.prefab` — 심링크 폴더라 워크트리 git 이 새 파일을 못 본다

### 왜 이 규칙이 중요한가

워크트리의 `Assets/Prefabs` 는 메인 프로젝트를 가리키는 **심링크**다. 워크트리 에디터에서 여기에 파일을 새로 만들면:

1. 파일은 실제로 **메인 프로젝트의 `Assets/Prefabs/`** 에 생긴다 (심링크 관통)
2. 워크트리의 `git status` 는 이 파일을 **못 본다** (git 은 심링크를 따라가지 않음)
3. 메인의 `git status` 에는 untracked 로 뜬다 — 엉뚱한 브랜치로 새어나간다

반면 `Assets/Scripts/_UI/Game2048/` 같은 기능 폴더는 심링크가 아닌 **실제 폴더**라, 새 파일이 그 워크트리의 브랜치에 정상적으로 올라간다.

## 3. "추가" 는 병렬 안전, "수정" 은 직렬화

서로 다른 워크트리에서 **같은 어셈블리 안의 새 파일** 을 만드는 것은 안전하다 (파일 이름만 겹치지 않으면 merge 가 자동). 반면 **이미 있는 파일을 두 워크트리가 동시에 수정**하면 merge conflict 가 거의 확실하다.

| 시나리오 | 안전성 | 해결법 |
|---|---|---|
| agent-0: `_UI/Game2048/Celebration/*.cs` 신규 / agent-1: `_UI/Game2048/PauseMenu/*.cs` 신규 | 안전 | 그대로 진행 |
| agent-0·agent-1 둘 다 `Board.cs` 수정 | 위험 | 한 쪽만 수정하고 먼저 머지, 다른 쪽은 rebase 후 수정 |
| agent-0 이 공유 `Assets/Prefabs/` 에 새 프리팹 추가 | **금지** (§2) | 기능 폴더로 옮긴다 |

## 4. 한 파일의 같은 함수를 두 워크트리가 건드려야 할 때

사전 `/design` 으로 편집 구간을 쪼개 둔다 (함수 A 는 agent-0, 함수 B 는 agent-1). 그래도 겹칠 것 같으면 병렬로 가지 말고 **직렬로** 처리 — 작업서의 1번이 먼저 merge 된 뒤 2번 시작.

## 5. 멀티 에디터 기동

각 워크트리 루트에서 `./scripts/run-editor.sh` 를 돌리면 이미 유니티가 떠 있어도 **새 인스턴스로** 뜬다 (macOS `open -n` 로직 자동 적용). 같은 프로젝트를 두 번 열려고 하면 기존 인스턴스가 포그라운드로만 올라온다.
