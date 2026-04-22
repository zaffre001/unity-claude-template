# Unity Editor 자동화 — ClaudeBridge 스택

이 프로젝트는 **Unity Editor 작업을 Claude가 직접 수행**할 수 있도록 자체 도구를 내장한다. 에이전트는 Editor 조작을 "사용자에게 시키는" 대신 이 스택을 써서 끝낸다.

## 계층

```
Claude Desktop 채팅
        │
        ▼
  claude-bridge MCP (Python)             ← scripts/claude-bridge-mcp/server.py
        │  unity_call / unity_batch_flush / unity_bridge_status
        ▼
  .claude-bridge/inbox/*.json            ← 커맨드 드롭
        │
        ▼
  ClaudeBridge (Unity C# Editor)         ← Assets/Editor/ClaudeBridge/
        │  Dispatcher → Ops.* (리플렉션)
        ▼
  .claude-bridge/outbox/*.json           ← 결과 드롭
```

## 두 가지 실행 모드

| 모드 | 트리거 | 사용 시점 |
|---|---|---|
| **GUI 상주** (실시간) | `/run editor` 후 Window > Claude Bridge > Start | Editor 화면 보면서 단계별 확인이 필요할 때, 사용자가 Play Mode 누를 예정 |
| **Headless 배치** | `/run bridge` 또는 MCP `unity_batch_flush()` | 사용자가 Editor 안 켜놓음. 여러 커맨드를 한 번에 몰아 실행 |

## 에이전트 호출 패턴

에이전트가 Unity 작업을 해야 할 때 순서:

1. **상태 확인** — `unity_bridge_status()` 로 Editor 가동·큐 점검. `editor_running: null` 이면 macOS/Linux 아닌 환경이거나 정보 부족.
2. **모드 결정**
   - 사용자가 Editor를 보면서 진행 중이면 → 그대로 `unity_call` 반복
   - 사용자가 자리 비운 상태 또는 Editor 안 떠 있음 → `unity_call` 로 쌓고 마지막에 `unity_batch_flush()`
   - MCP 래퍼가 없고 Filesystem MCP만 있는 환경 → `.claude-bridge/inbox/<id>.json` 직접 쓰기 + `/run bridge` 호출
3. **실행** — [`../Assets/Editor/ClaudeBridge/README.md`](../../Assets/Editor/ClaudeBridge/README.md) 의 op 레퍼런스 사용
4. **검증** — outbox의 `ok: true` 확인. 실패면 `error` 필드 사용자 보고 후 다음 단계 중단

## SVG 아이콘 파이프라인 (Claude가 직접 그리는 경로)

Claude는 **단순 도형 SVG를 직접 작성**해서 Unity Sprite로 임포트할 수 있다. 이미지 생성 모델·외부 래스터라이저 모두 필요 없다.

```
1) SVG 작성 (인라인 문자열 또는 Assets/Art/Icons/<name>.svg)
2) unity_call("Sprite.ImportFromSvg", { svgText|svgPath, pngPath, width, height, ppu, ... })
      └─ Unity Vector Graphics (com.unity.vectorgraphics) 가 파싱·테셀레이트·렌더
      └─ Texture2D.EncodeToPNG → 디스크 저장
      └─ AssetDatabase.ImportAsset + TextureImporter(textureType=Sprite) 까지 한 op
```

자주 쓰는 viewBox="0 0 100 100" SVG 패턴:
- 원형 배지 / 둥근 사각형 / 별 / 체크 / X / 화살표 / 다이아·하트·스페이드·클로버 / 기어 / 방패 / 말풍선

상세 예시와 op 호출 인자는 [`../skills/make-asset.md`](../skills/make-asset.md) §4-4-A.

**외부 바이너리 제거됨**: 과거에는 `rsvg-convert` / `magick` / `qlmanage` 로 PNG 래스터화했지만 이제 Unity 내장 렌더링(`VectorUtils.RenderSpriteToTexture2D` + MSAA 4x)으로 대체. 개발자 머신에 추가 설치 불필요, `com.unity.vectorgraphics` 패키지만 있으면 OK (manifest.json 기본 포함).

**한계**: Unity Vector Graphics 는 `filter`, `mask`, 외부 이미지 참조(`<image href>`) 등 일부 스펙 제한적. 결과가 어긋나면 SVG 를 path/polygon/rect/circle + 단색 fill/stroke 로 단순화한다.

## 자주 쓰는 op 조합

### 씬 초기 조립 (솔리테어 예)

```
Scene.New              → Assets/Scenes/Solitaire.unity
GameObject.Create      → Canvas
Component.Add          → /Canvas, UnityEngine.Canvas
Component.Add          → /Canvas, UnityEngine.UI.CanvasScaler
Component.Add          → /Canvas, UnityEngine.UI.GraphicRaycaster
Component.SetField     → /Canvas, Canvas.renderMode = ScreenSpaceOverlay
GameObject.Create      → GameRoot, parent=/Canvas
Component.Add          → /Canvas/GameRoot, Project.Core.SolitaireGame
Component.SetField     → cardPrefab ← Assets/Prefabs/Card.prefab
Scene.Save
```

### UGUI 프리팹 중첩 (Card 안에 Suit 아이콘)

```
Prefab.Open              → Card.prefab
Prefab.InstantiateAsChild → Suit.prefab as /Card/Suit  (원본 링크 유지)
Component.SetRectTransform → /Card/Suit, anchor/pivot/position
Prefab.Save
Prefab.Close
```

### Variant 만들기

```
Prefab.CreateVariant → sourcePath=Card.prefab, variantPath=Card_Back.prefab
Prefab.Open          → Card_Back.prefab
Component.SetField   → Image.sprite ← card_back.png  (오버라이드만 변이)
Prefab.Save
Prefab.Close
```

## 에이전트가 선제 호출해도 되는 순간

사용자 명령 없어도 판단해서 호출:

- **씬에서 참조되는 프리팹이 `Assets/Prefabs/` 에 없을 때** → `/make-asset ui` 로 임시 블록 프리팹 생성 → 계속 진행. 한 줄 고지: "Card 프리팹 없어서 색 블록으로 임시 생성합니다. 나중에 이미지 있으면 말씀하세요."
- **심볼/아이콘이 필요한데 이미지 없을 때** (하트·별·체크·X·화살표·기어·카드 슈트 등) → 사용자 요청 기다리지 말고 `/make-asset sprite` 로 **SVG 직접 작성 → PNG 래스터화 → 임포트**. Claude는 단순 도형 SVG를 스스로 그릴 수 있으므로 이미지 요청은 사진·복잡한 일러스트일 때만.
- **스크립트 수정·신규 후 동작 확인이 필요한데 Editor가 닫혀 있을 때** → `/run editor` 로 GUI 띄우거나 `/run bridge` 로 헤드리스 검증
- **씬 조립 커맨드 10개+를 쏟아낸 직후** → `/run bridge` 로 일괄 실행 (매 op마다 Editor 왕복하면 느림)

## 한계 / 함정

- **Editor 락**: 같은 프로젝트로 Editor가 열려 있는 상태에서 `/run bridge` 호출 시 Unity가 "project already open"으로 실패. `unity_bridge_status()` 의 `editor_running` 을 반드시 먼저 확인.
- **Domain Reload**: 이 프로젝트는 Domain Reload 비활성화. ClaudeBridge도 `[InitializeOnLoad]` 생성자에서 static 상태 초기화를 수동으로 하므로 안전하지만, 새 op 추가 시 static 필드가 들어가면 RULES.md RULE-01 따라 `[RuntimeInitializeOnLoadMethod]` 초기화 붙이기.
- **파일 락 경합**: GUI 서버가 200ms 폴링하는 동안 배치 모드 진입은 불가. 모드 전환은 명시적으로(`Stop` → 배치 → 끝나면 `Start`).
- **JsonUtility 한계**: Dictionary / polymorphic 타입 직렬화 약함. 새 op은 구조체(POCO)로 args/result 정의 필수.

## 확장 가이드

새 op 추가 순서:
1. `Assets/Editor/ClaudeBridge/Protocol.cs` — args/result 구조체 추가
2. `Ops/XxxOps.cs` — 핸들러 함수 작성 (`string argsJson → string resultJson`)
3. `Dispatcher.cs` — `["Op.Name"] = Ops.XxxOps.Handler` 한 줄 등록
4. `Assets/Editor/ClaudeBridge/README.md` op 레퍼런스 표 업데이트
5. Python MCP 서버는 재시작 불필요 — `unity_call(op, args)` 의 op 문자열을 그대로 통과시키므로 자동 지원
