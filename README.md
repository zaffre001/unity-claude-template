# Unity Claude Template — 클로드 코드와 유니티를 이용해서 나만의 게임을 만드세요!

**한국어** · [English](README.en.md)

> **이 리포지터리는 "내 유니티 게임을 Claude 와 함께 만들기 시작하는 바탕 파일"입니다.**
> AI 에이전트가 씬도 꾸미고, 스크립트도 쓰고, 프리팹도 만들 수 있도록 필요한 장치가 미리 엮여 있어요.
>
> 그냥 빈 프로젝트만 보면 감이 잘 안 오니까, **솔리테어 한 판을 먼저 같이 만들어 봅니다** — 도구들이 어떻게 맞물려 돌아가는지 감을 잡는 연습이에요. 그 뒤에 솔리테어를 말끔히 걷어 내고, 여러분이 정말 하고 싶은 장르(퍼즐·플랫포머·RPG·리듬 게임 — 무엇이든) 로 갈아타시면 됩니다.
>
> 프로그래밍을 해본 적이 없어도 괜찮습니다. 대화만 주고받으시면 Claude 가 알아서 움직여요.

---

## 이 문서가 여러분을 데려가는 길

1. **준비물** (20분쯤) — 유니티·Claude Desktop·이 템플릿 내려받기
2. **브릿지 설치** (5분쯤) — Claude 가 내 파일과 유니티에 손을 뻗을 수 있도록
3. **연습판: 솔리테어 만들어 보기** — 도구들 감 잡기
4. **정리하기** — 솔리테어 걷어 내고 템플릿을 다시 깨끗하게
5. **여러분 게임으로 갈아타기** — 장르별 가이드 + 바로 쓸 수 있는 말투
6. **스킬 상세** — 도구 하나하나 자세히

---

## 1. 준비물

| 갖춰야 할 것 | 어디서 받나요 |
|---|---|
| Unity Hub + Unity 2022.3 LTS | [unity.com/download](https://unity.com/download) 에서 받아 설치 마법사대로 진행 |
| Claude Desktop 앱 | [claude.ai/download](https://claude.ai/download) — 맥·윈도 모두 지원 |
| 파이썬 3.10 이상 | 맥이라면 보통 이미 깔려 있어요. 터미널에 `python3 --version` 쳐서 확인해 주세요. 윈도는 [python.org](https://python.org) |
| 이 템플릿 | 오른쪽 위 **Use this template** 로 내 계정에 복제하시거나, 초록색 **Code** 단추 → **Download ZIP** 으로 받아 압축 풀기 |

압축은 `~/Documents/my-game` 처럼 알아보기 쉬운 자리에 풀어 주세요. **폴더 이름에 한글·띄어쓰기가 없는 편이** 유니티가 읽다가 삐끗하는 일을 줄여 줍니다.

---

## 2. 브릿지 설치

Claude 가 여러분 컴퓨터에서 실제로 일을 하려면 작은 다리 두 개가 필요합니다.

- **Filesystem MCP** — 파일을 열고 새로 쓰는 다리
- **Claude Bridge MCP** — 유니티 에디터를 움직이는 다리 (이 템플릿에 담겨 있어요)

### 2-1. Filesystem 다리 놓기 (30초쯤)

**1)** Claude Desktop 을 켜고 **설정(Settings)** → 왼쪽 메뉴 **커넥터(Connectors)** → 오른쪽 위 **커넥터 둘러보기**

![설정에서 커넥터 찾기](docs/images/01-connector-filesystem.png)

**2)** 목록에서 **Filesystem** 을 고르고 **설치** 단추를 눌러 주세요.

![Filesystem 설치](docs/images/02-filesystem-install.png)

**3)** 토글이 **사용 중** 으로 켜지면 오른쪽 **구성** 단추 → 방금 압축 푼 템플릿 폴더 고르기.

![Filesystem 켜진 모습](docs/images/03-filesystem-active.png)

### 2-2. Claude Bridge 다리 놓기 (2분쯤)

**1)** 터미널을 열고 `pipx` 를 준비합니다 (이미 깔려 있으면 건너뛰세요):

```bash
brew install pipx
pipx ensurepath
```

윈도라면 `python -m pip install --user pipx` → `python -m pipx ensurepath` 순서로.

**2)** 이 템플릿 안에 담긴 브릿지 서버를 `pipx` 로 깝니다:

```bash
pipx install ~/Documents/my-game/scripts/claude-bridge-mcp
```

경로는 **다운로드 받은 이 깃 폴더를 풀어 놓은 자리**로 바꿔 주세요. 설치가 끝나면 `claude-bridge-mcp` 라는 커맨드가 새로 생깁니다. `which claude-bridge-mcp` 쳐서 경로가 나오면 성공이에요.

> **왜 pipx 인가요?** 맥 홈브루 파이썬에선 그냥 `pip install mcp` 가 `externally-managed-environment` 오류로 막힙니다. pipx 는 서버를 자기만의 가상환경에 깔고 커맨드 이름만 밖으로 꺼내 줘서 이런 충돌이 없어요.

**3)** Claude Desktop 설정 파일을 열어서 다리 정보를 적어 둡니다.

- 맥: Finder 에서 `Cmd + Shift + G` 누르고 `~/Library/Application Support/Claude/` 붙여 넣기 → `claude_desktop_config.json` 을 텍스트 편집기로 열기
- 윈도: `%APPDATA%\Claude\claude_desktop_config.json`

**4)** `mcpServers` 안에 `claude-bridge` 항목을 덧붙여 주세요:

```json
{
  "mcpServers": {
    "claude-bridge": {
      "command": "claude-bridge-mcp",
      "env": {
        "CLAUDE_BRIDGE_PROJECT": "/Users/여러분이름/Documents/my-game"
      }
    }
  }
}
```

`CLAUDE_BRIDGE_PROJECT` 에는 **다운로드 받은 이 깃 폴더를 풀어 놓은 자리** (유니티 프로젝트 루트, 즉 `ProjectSettings` 폴더가 있는 자리) 를 그대로 적어 주세요.

**5)** Claude Desktop 을 완전히 끄고(맥이라면 `Cmd + Q`) 다시 열어 주세요.

**6)** 새 대화창에서 살짝 점검해 봅니다:

> `unity_bridge_status` 도구 한 번 불러서 지금 상태 좀 보여줘.

답변에 `project_root` 가 여러분 템플릿 폴더로 잡혀 있으면 다리가 잘 놓인 거예요. `editor_running: false` 는 아직 유니티를 안 켰다는 뜻이니 신경 쓰지 않으셔도 됩니다.

**업데이트할 때:** 템플릿을 `git pull` 로 당긴 뒤 `pipx install --force ~/Documents/my-game/scripts/claude-bridge-mcp` 한 번 더 돌려 주시면 끝입니다.

---

## 3. 연습판: 솔리테어를 같이 만들어 보기

> 여기는 **도구들이 어떻게 엮여 돌아가는지 손에 익히는 연습 단계**입니다. 나중에 이 결과물을 정리하고 여러분 게임으로 넘어갈 거예요. 그러니 "솔리테어 만드는 법" 보다는 "Claude 한테 뭘 어떻게 시킬 수 있는지" 를 눈에 익혀 두신다는 느낌으로 봐 주세요.

아래 글을 Claude 대화창에 그대로 붙여 넣어 주세요:

> 나 유니티 처음 써봐. 이 폴더가 unity-claude-template 이야. 연습 삼아 클론다이크 솔리테어를 만들어 보자.
>
> 단계를 잘게 쪼개서 하나씩 진행해 줘. 프로그래밍은 몰라서 코드는 네가 알아서 써. 나는 유니티 창에서 결과만 눈으로 확인할게.

Claude 가 대략 이런 식으로 움직입니다.

1. `CLAUDE.md`, `RULES.md`, `.claude/INDEX.md` 를 훑어서 약속 사항 챙기기
2. 만들 순서를 서너 개쯤 제안하기
3. "첫 단계부터 시작할까요?" 하고 여쭤보기

**"응"** 만 답해 주시면 Claude 가 알아서 진행해요. 도중에 유니티 창을 띄워야 하면 Claude 가 `/run editor` 로 스스로 띄우고, 카드 프리팹이 필요하면 `/make-asset` 으로 즉석에서 만듭니다. 하트 같은 작은 기호는 **SVG 도형을 직접 그려서** PNG 로 뽑아 유니티에 넣어 두기까지 해요.

끝나면 유니티의 **▶ 재생** 단추를 누르세요. 카드가 분배되는 첫 화면이 뜨면 성공입니다.

> **막히면**: 오류 메시지를 그대로 붙여 넣으시고 "고쳐줘" 라고 하시면 대체로 해결됩니다. 아예 말씀을 안 하셔도 Claude 가 콘솔을 읽고 스스로 고치기도 해요.

---

## 4. 솔리테어 걷어 내고 템플릿을 깨끗하게

연습이 끝났으면 솔리테어를 말끔히 정리하고 빈 바탕으로 돌아갑니다. 이것도 Claude 한테 시키시면 됩니다:

> 이제 솔리테어 연습 끝났어. 다음 네 가지를 해줘:
>
> 1. 솔리테어 관련 파일 전부 지워 줘 — `Assets/Scripts/_Core/Card*.cs, Deck*.cs, Solitaire*.cs` / `Assets/Prefabs/Card.prefab` / `Assets/Scenes/Solitaire.unity` / 관련 Variant 프리팹들.
> 2. `Assets/Art/Cards/` 는 남길지 지울지 한 번 물어봐. 다른 게임에서도 쓸 수 있을 것 같으면 남기고, 아니면 지워 줘.
> 3. 네가 지우는 각 파일을 한 줄씩 내게 먼저 보여 주고 "지워도 돼?" 확인받아.
> 4. 다 지운 뒤에 컴파일 오류나 끊긴 참조가 있는지 살펴보고, 있으면 고쳐 줘.

Claude 가 지워도 되는지 묻는 파일을 하나하나 확인받고 정리해 줄 거예요. 솔리테어에서 배운 패턴은 `.claude/domain/` 이나 매뉴얼에 남길 만한 게 있는지도 함께 살펴봐 줍니다.

**빈 바탕으로 완전히 돌아가고 싶으시면** 이 한 줄이면 됩니다:

> /task-start 와 /task-done 까지 다 쓰면서, 솔리테어 흔적을 남김없이 지워 원래 템플릿 상태로 돌려 줘. 내 허락 없이 `Assets/Editor/`, `scripts/`, `.claude/` 아래는 건드리지 마 — 템플릿의 뼈대니까.

---

## 5. 여러분 게임으로 갈아타기

이제 진짜 여러분 게임을 시작할 시간이에요. 세 단계면 충분합니다.

### 5-1. 게임 이름·네임스페이스 바꾸기

템플릿 기본값은 `Project.Core`, `Project.UI` 같은 네임스페이스를 씁니다. 여러분 게임 이름으로 바꾸셔야 깔끔해요.

> 내 게임 이름은 "무지개 낚시" 야. 이 템플릿의 `Project.*` 네임스페이스를 `RainbowFishing.*` 으로 바꿔 줘. `.asmdef` 의 name 필드, 모든 `.cs` 의 `namespace`, `CLAUDE.md` 안의 설명 전부 포함해서. 그리고 `ProjectSettings/ProjectSettings.asset` 의 productName 도 "Rainbow Fishing" 으로 고쳐 줘.

### 5-2. 어셈블리 골조 장르에 맞게 고치기

템플릿 기본 뼈대는 `_Core`, `_UI`, `_Combat`, `_Rendering` 네 개예요. 여러분 장르에 쓸모없는 건 지우고, 더 필요한 영역이 있으면 더하시면 됩니다.

| 장르 | 추천 어셈블리 골조 |
|---|---|
| 퍼즐 / 카드 / 보드 | `_Core`, `_UI`, `_Gameplay` (← `_Combat` 리네임), `_Rendering` 지움 |
| 플랫포머 / 액션 | `_Core`, `_UI`, `_Player`, `_Level`, `_Combat`, `_Rendering` |
| RPG / 어드벤처 | `_Core`, `_UI`, `_Battle`, `_Inventory`, `_Dialog`, `_Quest`, `_Rendering` |
| 리듬 / 음악 | `_Core`, `_UI`, `_Audio`, `_Chart`, `_Input`, `_Rendering` 지움 |
| 시뮬레이션 / 경영 | `_Core`, `_UI`, `_Economy`, `_AI`, `_Time` |
| 레이싱 / 스포츠 | `_Core`, `_UI`, `_Vehicle`, `_Track`, `_Physics`, `_Rendering` |

Claude 에게는 이렇게 부탁해 보세요:

> 나 퍼즐 게임 만들 거야. 현재 `_Combat`, `_Rendering` 어셈블리는 지우고, `_Puzzle` 어셈블리 하나 새로 만들어 줘. 네임스페이스는 `RainbowFishing.Puzzle`. `.asmdef` 에서 의존 관계도 맞게 설정하고 `autoReferenced: false` 유지해 줘.

### 5-3. 기획 들려 주고 /design 으로 쪼개기

이제 여러분 게임 기획을 자연어로 들려 주세요. Claude 가 분야별로 쪼개고 각 단계마다 "이 프롬프트 그대로 다음 작업에 붙여넣으면 돼요" 수준으로 작업서를 만들어 줍니다.

> /design 으로 다음 기획을 쪼개 줘:
>
> 무지개 낚시 — 2D 탑다운 낚시 퍼즐. 플레이어는 물고기를 색깔별로 낚아서 일곱 빛깔 무지개를 완성한다. 시간 제한은 90초. 같은 색깔 세 마리 잡으면 보너스 시간 5초.
>
> 분야는 `_Core` (데이터·룰), `_UI` (타이머·무지개 게이지·점수), `_Puzzle` (낚시 판정·스폰). 각 분야의 1단계 작업은 다음 에이전트가 바로 실행할 수 있게 구체적으로 써 줘.

그러면 Claude 가 `design/rainbow-fishing/` 폴더 안에 영역별 작업서 파일을 떨어뜨려 놓습니다. 이후 하나씩 "이 작업서대로 해 줘" 라고 맡기시면 돼요.

### 5-4. 장르별 첫 어셋 예시 프롬프트

**퍼즐**
> /make-asset — 3×3 격자 타일 프리팹. 클릭하면 색이 바뀌는 간단한 동작. 색은 빨·파·노 세 가지, Inspector 에서 고를 수 있게.

**플랫포머**
> /make-asset — 플레이어 임시 모델. 프리미티브 Capsule 로 몸통, Cube 로 모자. `_Player` 네임스페이스 아래 `PlayerController.cs` 붙을 수 있게 `Assets/Prefabs/Player/Player.prefab`.

**RPG**
> /make-asset particle — 회복 마법 파티클. 연초록색 빛이 머리 위로 부드럽게 올라가는 느낌. 2초쯤 지속.

**리듬 게임**
> /make-asset ui — 박자 판정 링. 바깥 테두리는 흰색, 안으로 줄어드는 링은 노란색. 256×256 프리팹으로.

**낚시·시뮬레이션**
> /make-asset icon — 낚시찌 아이콘 128×128. 빨간 공에 아래쪽 하얀 막대. SVG 로 그려서 넣어 줘.

---

## 6. 뭔가 어긋났을 때

**Claude 가 오류를 만나면** 대체로 스스로 해결해요. "그 오류 고쳐줘" 한마디로 충분할 때가 많습니다.

**유니티가 안 열리면**: 터미널에서 `./scripts/run-editor.sh` 를 직접 돌리시거나, Unity Hub 에서 이 프로젝트 폴더를 `Add` 하고 두 번 눌러 여세요.

**브릿지가 대답 안 한다고 하면**: 에디터에서 **Window → Claude Bridge → Start** 눌러 주셨는지 살펴봐 주세요. 그래도 안 되면 에디터를 닫고 Claude 에게 "`/run bridge` 로 돌려줘" 라고 하시면 유니티 창 없이도 처리가 됩니다.

**정리하다가 중요한 걸 지운 것 같으면**: 패닉하지 마세요. 템플릿은 깃(Git) 리포라 언제든 되돌릴 수 있어요. Claude 에게 "되돌려 줘, 방금 지운 것 중 뭐가 중요한지 짚어 줘" 라고 하시면 됩니다.

---

## 7. 쓸 수 있는 스킬들

Claude 에게 `/스킬명`형식으로 시킬 수 있는 작업 묶음이에요. 여러분이 굳이 부르지 않아도 클로드가 알아서 쓸 수 있는 스킬은 **자동** 표시를 붙였습니다. 각 스킬이 해주는 일과 예시 프롬프트를 함께 적어 뒀어요.

### `/task-start` — 작업 시작 채비 (자동)

새 작업을 맡기기 전에 **범위를 짜고** 꼭 필요한 매뉴얼만 골라 읽는 루틴입니다. `.claude/INDEX.md` 로 키워드를 훑어 관련된 지식만 펼쳐 봐요. 덕분에 긴 매뉴얼을 매번 통째로 읽지 않아도 됩니다.

**할 수 있는 일**
- 작업 주제 키워드로 `.claude/knowledge/*`, `.claude/rules/*`, `RULES.md` 중 관련 파일만 추려 읽기
- 이번 작업에서 건드릴 파일을 미리 그레프로 찾아 보고 범위 선언
- `autoReferenced: false` 같은 프로젝트의 절대 규칙 확인
- 어디까지가 이번 작업인지 한 줄로 요약해 보여 주기

**예시 프롬프트**
> /task-start 점수판 HUD 만들어 줘. `_UI` 어셈블리만 건드리고 다른 어셈블리는 손대지 마.
>
> /task-start 플레이어 점프 로직 고치는데, 이중 점프가 두 번 넘게 안 되게 할 거야. 관련 파일 찾아 주고 시작하자.

### `/task-done` — 작업 마무리 (자동)

작업을 끝낼 때 **결과를 정리하고 이 프로젝트만의 새 지식을 매뉴얼로 올리는** 루틴이에요. 다음 에이전트가 같은 실수를 반복하지 않도록 돕습니다.

**할 수 있는 일**
- 이번에 건드린 파일·컴포넌트를 묶어서 바뀐 점 요약
- 임시로 끈 코드·주석·디버그 로그 같은 찌꺼기 청소
- 도메인 지식(기획·시스템·관계)을 `.claude/domain/*.md` 새 파일로 승격 제안
- 필요하면 바로 `/self-update` 로 이어 가기
- 빌드나 테스트가 잘 돌아가는지 짧게 검증

**예시 프롬프트**
> 여기까지 잘 됐어. /task-done 해줘.
>
> /task-done 하면서 혹시 매뉴얼에 넣어 두면 좋을 배움이 있으면 같이 정리해줘.

### `/self-update` — 배운 걸 매뉴얼로 올리기

이번 세션에서 알게 된 실수·함정·패턴을 **어느 매뉴얼 계층에 올리는 게 맞을지** Claude 가 제안해 주는 루틴입니다. 올리기 전에 늘 여러분 승인을 받아요.

**할 수 있는 일**
- 다섯 단계 지식 계층(RULES / knowledge / domain / CLAUDE / local) 중 알맞은 자리 찾기
- "어디에, 어떤 문장으로 넣을지" 구체 수정안 보여 주기
- 이미 있는 지식과 겹치지 않는지 대조
- 한 줄짜리 팁처럼 자잘한 건 덜어 내기 (매뉴얼이 두꺼워지지 않도록)

**예시 프롬프트**
> 오늘 RectTransform 의 anchor 가지고 계속 헤맸어. /self-update 해서 매뉴얼에 팁 넣자.
>
> /self-update — 이번에 낚싯대 콜라이더에서 배운 거 어디 넣으면 좋을지 봐 줘.

### `/design` — 기획을 작업 프롬프트로 쪼개기

자연어로 쓴 기획이나 노션 링크를 받아 **분야별로 나누고, 각 분야마다 다음 에이전트가 그대로 쓸 수 있는 프롬프트**로 만들어 주는 스킬입니다. 혼자가 아니라 여러 에이전트가 나눠 일할 때 특히 좋아요.

**할 수 있는 일**
- 기획을 `_Core` / `_UI` / `_Puzzle` / `_Player` 같은 영역별로 나누기
- 각 단계의 산출물·검증 방법·제약을 함께 적어 주기
- `design/{slug}/` 폴더에 단계별 마크다운 파일로 떨어뜨리기
- 단계 사이 의존 관계(먼저 해야 할 일, 나중에 해도 되는 일) 표시

**예시 프롬프트**
> /design 무지개 낚시 — 90초 동안 물고기 낚아서 일곱 빛깔 모으기. `_Core`, `_UI`, `_Puzzle` 세 영역으로 쪼개 줘.
>
> 이 노션 링크 읽고 /design 으로 작업 계획 쪼개 줘: https://notion...

### `/run` — 유니티 돌리기 (자동)

상황에 따라 세 갈래로 갈라집니다. 에이전트가 스스로 골라 쓸 수 있어요.

**인자 1. (없음 또는 플랫폼 이름) — 빌드해서 실행**
- `mac` / `win` / `linux` / `webgl` / `android` / `ios`
- 결과는 프로젝트 옆 `builds/{이름표}-{브랜치}-{짧은커밋}-{대상}-{시간}/` 에 차곡차곡 쌓여요
- 빌드가 끝나면 해당 바이너리를 바로 띄워 줍니다 (웹GL 은 서빙 명령을 안내)

**인자 2. `editor` — 유니티 에디터 창 열기**
- 빌드 없이 에디터만 켜 주는 모드
- 같은 프로젝트로 이미 유니티가 떠 있으면 그 창이 앞으로 올라옵니다

**인자 3. `bridge` — 쌓여 있는 명령 한꺼번에 돌리기**
- 유니티 창을 안 켜 둬도, `.claude-bridge/inbox/` 에 쌓인 JSON 명령들을 헤드리스로 전부 처리해요
- 결과 요약 + 실패한 명령 목록을 돌려줍니다

**예시 프롬프트**
> /run — 지금 내 맥으로 빌드해서 바로 띄워 줘.
>
> /run editor — 유니티 좀 켜 줘, Hierarchy 직접 보고 싶어.
>
> /run bridge — 아까 쌓아 둔 명령들 한 번에 처리해 줘.
>
> /run webgl — 웹에서도 해볼 수 있게 빌드.

### `/make-asset` — 어셋 만들기 (자동)

유니티 어셋을 즉석에서 만들어 주는 스킬이에요. 씬을 짜는데 프리팹이 없거나, 스크립트가 어떤 참조를 기대하는데 그 자리가 비어 있으면 Claude 가 먼저 이걸 꺼내 듭니다.

**할 수 있는 일**
- **UGUI 프리팹** — 단추·패널·글자판 같은 것을 RectTransform · Image · Text · Button 조합으로. 상태별 Variant 까지.
- **파티클** — 타격·폭발·반짝임·치유 오라 같은 느낌으로 ParticleSystem 프리팹
- **프리미티브 placeholder 모델** — 실제 3D 모델 오기 전에 Cube · Sphere · Cylinder 로 조합한 임시 모델
- **SVG 로 직접 그린 아이콘** — 하트·별·체크·X·화살표·카드 슈트처럼 **간단한 도형은 Claude 가 SVG 를 써서 바로 그리고**, PNG 로 뽑아 유니티에 Sprite 로 불러옵니다. 이미지 파일을 따로 주시지 않아도 돼요.
- **사용자 이미지 임포트** — 사진·복잡한 일러스트는 파일이나 링크를 주시면 Sprite 로 설정까지 마무리

요구 사항이 뚜렷하지 않으면 크기·색·쓰이는 자리 같은 걸 먼저 여쭤봅니다. 답이 없으면 "임시로 색 블록 만들어 두고 나중에 바꿀 수 있게 해 둘게요" 식으로 안전하게 진행해요.

**예시 프롬프트**
> /make-asset — 퍼즐 타일 프리팹. 3×3 격자에 놓을 거니까 Inspector 에서 색 바꿀 수 있게.
>
> /make-asset particle — 플레이어 점프할 때 밑에서 먼지 툭 튀는 느낌. 짧고 가볍게.
>
> /make-asset icon — 체력 하트 아이콘 128×128. 빨간색, 테두리 살짝 어둡게. SVG 로 그려서 넣어 줘.
>
> /make-asset prefab — 대화창 팝업. 가운데 글자판, 아래에 "다음" 단추. 스킨은 나중에 바꿀 거니까 기본 스타일로.
>
> /make-asset model — 플레이어 임시 모델. 프리미티브 Capsule 몸통에 Sphere 머리, 파란색.

---

## 8. 템플릿 안쪽 구경하기 (원하실 때만)

연습판만 돌릴 거면 지금 안 읽으셔도 돼요. 본격적으로 만들기 시작하실 때 한 번 훑어보시면 됩니다.

### Claude 가 따르는 약속들
| 파일 | 하는 일 |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | 세션 시작할 때 자동으로 읽히는 "팀 공유 매뉴얼". 여러분 게임 개요도 여기에 적어 두면 좋아요 |
| [`RULES.md`](RULES.md) | 어기면 유니티가 실제로 어긋나는 절대 규칙 여섯 가지 |
| [`.claude/INDEX.md`](.claude/INDEX.md) | 어떤 지식이 어디 있는지 알려 주는 차림표 (에이전트가 제일 먼저 봐요) |
| [`.claude/knowledge/`](.claude/knowledge/) | 유니티 성능, C# 언어, 에디터 자동화 같은 널리 쓰이는 지식 |
| [`.claude/rules/`](.claude/rules/) | 경로·파일 종류별 규칙 (예: `.asmdef` 는 이렇게 다뤄) |
| [`.claude/domain/`](.claude/domain/) | 여러분 게임만의 기획·시스템 지식이 차곡차곡 쌓이는 자리 |

### 스킬들이 담긴 자리
`.claude/skills/` 아래에 §7 에 적어 둔 스킬 파일들이 있어요. 직접 열어 보시면 각 스킬이 더 자세히 기술되어 있습니다.

### 유니티 에디터 자동화 도구
| 경로 | 하는 일 |
|---|---|
| [`Assets/Editor/ClaudeBridge/`](Assets/Editor/ClaudeBridge/README.md) | 파일로 주고받는 다리 + 리플렉션으로 돌아가는 op 스물두 가지 (씬 / 게임오브젝트 / 컴포넌트 / 프리팹 / 어셋 / 리플렉션). 유니티 창이 열려 있을 때도, 닫혀 있을 때도 모두 대응 |
| [`scripts/claude-bridge-mcp/`](scripts/claude-bridge-mcp/README.md) | 파이썬으로 쓴 얇은 MCP 서버. Claude Desktop 이 `unity_call(op, args)` 한 번으로 유니티를 다룰 수 있게 해줘요 |
| [`scripts/run.sh`](scripts/run.sh) / [`run-editor.sh`](scripts/run-editor.sh) / [`bridge-run.sh`](scripts/bridge-run.sh) | `/run` 스킬이 상황 따라 부르는 세 가지 실제 셸 파일 |
| [`Assets/Editor/ParallelAgentSetup.cs`](Assets/Editor/ParallelAgentSetup.cs) | Domain Reload 를 꺼서 Play 모드 진입이 거의 순식간 |
| [`scripts/create-symlinked-worktrees.sh`](scripts/create-symlinked-worktrees.sh) | 깃 워크트리 + 심링크로 여러 에이전트를 동시에 돌릴 수 있게 도와주는 셸 파일 |

### 어셈블리 뼈대
기본은 `_Core`, `_UI`, `_Combat`, `_Rendering` 그리고 테스트 두 개입니다. §5-2 의 장르별 추천표를 참고해서 필요 없는 건 지우고 필요한 건 더하세요.

---

## 9. 이 템플릿의 바탕이 된 글

더 깊은 설계 이야기가 궁금하시면:

1. [에이전트의 뇌를 설계하는 법](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-AI-에이전트를-실무에-쓴다)
2. [Domain Reload 없는 병렬 에이전트 워크트리](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-병렬-에이전트-설계)
3. [DAP 기반 에이전트 디버깅 환경](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-DAP-기반-에이전트-환경-만들기)

## 참조 링크

- 템플릿 코드: [MIT](LICENSE)
- 카드 그림(연습판에만 쓰임): 케니(Kenney) 플레잉 카드 묶음 (CC0) — 자세한 약속은 [`Assets/Art/Cards/License.txt`](Assets/Art/Cards/License.txt). 연습이 끝나면 지우셔도 됩니다.
