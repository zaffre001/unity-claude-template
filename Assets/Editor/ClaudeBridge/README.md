# ClaudeBridge

Unity Editor 작업을 Claude가 **파일 한 개 드롭으로** 수행하게 해주는 경량 브릿지.
외부 MCP 설치, Node/Python 런타임 불필요 — 순수 Unity C# + 리플렉션.

## 왜 파일 기반인가

- Claude Desktop이 **Filesystem MCP만으로** 바로 구동된다. 추가 MCP 설치 없음.
- FileSystemWatcher는 백그라운드 스레드에서 콜백이 오는데 Unity API는 메인 스레드 전용 → 대신 `EditorApplication.update` 폴링(200ms)으로 처리.
- Domain Reload 비활성화 환경과도 양립: 폴링은 생성자에서 `[InitializeOnLoad]`로 재등록.

## 구조

```
Assets/Editor/ClaudeBridge/
├── Protocol.cs                 # Command / Result + op별 args/result POCO
├── CommandIO.cs                # inbox→dispatcher→outbox 공통 처리 (Server/Batch 공유)
├── ClaudeBridgeServer.cs       # GUI 모드: EditorApplication.update 폴링 서버
├── ClaudeBridgeBatch.cs        # 헤드리스 모드: -executeMethod 진입점
├── Dispatcher.cs               # op 이름 → 핸들러 라우팅
├── ClaudeBridgeMenu.cs         # Window > Claude Bridge > Start/Stop/Status
└── Ops/
    ├── SceneOps.cs             # Scene.New / Open / Save
    ├── GameObjectOps.cs        # GameObject.Create / Find / Delete / SetTransform
    ├── ComponentOps.cs         # Component.Add / SetField / GetField / SetRectTransform
    ├── PrefabOps.cs            # Prefab.Open / Save / Close / Instantiate / Apply / Unpack / GetCurrent
    ├── ReflectionOps.cs        # Reflection.Invoke (탈출구)
    └── AssetOps.cs             # Asset.Refresh / CreatePrefab
```

## 두 가지 실행 모드

### 1) GUI 모드 (Editor 상주)

1. Unity Editor 상단 **Window → Claude Bridge → Start**
   - 콘솔에 `[ClaudeBridge] Started.` 출력 확인
   - 프로젝트 루트에 `.claude-bridge/inbox/`, `.claude-bridge/outbox/` 디렉터리 생성됨
2. 이후 Editor 재시작 시 자동 시작됨 (EditorPrefs 플래그)
3. Claude는 파일을 드롭한 직후 200~400ms 내로 결과를 받을 수 있다 — Editor 상주 폴링(200ms 간격).

### 2) 헤드리스 모드 (Editor 없이 일괄 실행)

사용자가 Unity를 열어두지 않은 상태에서 Claude가 여러 커맨드를 모아 실행할 때.

```bash
./scripts/bridge-run.sh
```
또는 `/run bridge` 스킬 호출.

내부 동작:
- `.claude-bridge/inbox/*.json`에 이미 쌓인 커맨드를 알파벳 순(타임스탬프 파일명 전제)으로 읽음
- `Unity -batchmode -nographics -quit -executeMethod Project.Editor.ClaudeBridge.ClaudeBridgeBatch.Run` 호출
- 하나의 Unity 세션에서 모두 처리 후 종료
- 로그: `.claude-bridge/logs/bridge-<timestamp>.log`
- 종료 코드: `0`=전부 성공 / `1`=일부 실패 (outbox의 `ok` 필드로 개별 확인)

Editor가 같은 프로젝트로 이미 떠 있으면 락 충돌로 실패하니 먼저 닫는다.

## 프로토콜

### 요청 (Claude → Unity)

`.claude-bridge/inbox/<id>.json` 에 드롭:

```json
{
  "id": "2b3f1a",
  "op": "GameObject.Create",
  "argsJson": "{\"name\":\"GameRoot\",\"parentPath\":null}"
}
```

- `id`: UUID 짧은 버전 등. 응답 파일명에 그대로 쓰인다.
- `op`: 아래 op 레퍼런스 표의 키.
- `argsJson`: op별 args 구조체의 `JsonUtility.ToJson()` 결과를 **문자열로** 넣는다 (이중 직렬화).

Unity는 파일을 픽업 후 inbox에서 삭제한다.

### 응답 (Unity → Claude)

`.claude-bridge/outbox/<id>.json`:

```json
{
  "id": "2b3f1a",
  "ok": true,
  "dataJson": "{\"path\":\"/GameRoot\",\"instanceId\":12345}",
  "error": null,
  "stack": null
}
```

실패 시 `ok: false`, `error`에 `"ArgumentException: ..."` 같은 메시지.

Claude는 `.claude-bridge/outbox/<id>.json`을 폴링 읽기. 보통 200~400ms 내에 나온다.

## Op 레퍼런스

| op | args | result | 비고 |
|---|---|---|---|
| `Scene.New` | `{path}` | `{scenePath}` | |
| `Scene.Open` | `{path}` | `{scenePath}` | |
| `Scene.Save` | `{path?}` | `{scenePath}` | |
| `GameObject.Create` | `{name, parentPath?}` | `{path, instanceId}` | 스테이지가 열려 있으면 parentPath 생략 시 prefab 루트 아래에 생성 |
| `GameObject.Find` | `{path}` | `{found, instanceId}` | 스테이지 인식 |
| `GameObject.Delete` | `{path}` | `{deleted}` | |
| `GameObject.SetTransform` | `{path, position?, rotation?, scale?}` (각 float[3]) | `{path}` | Transform 전용 (3D) |
| `Component.Add` | `{path, type}` (type: 풀네임) | `{type}` | |
| `Component.SetField` | `{path, type, field, valueJson, valueType}` | `{field}` | 범용 |
| `Component.GetField` | `{path, type, field}` | `{valueJson, valueType}` | |
| `Component.SetRectTransform` | `{path, anchorMin?, anchorMax?, pivot?, anchoredPosition?, sizeDelta?, offsetMin?, offsetMax?}` (각 float[2]) | `{path}` | UGUI 편의 — 한 호출로 anchor/pivot/size 전부 |
| `Asset.Refresh` | `{}` | `{refreshed}` | |
| `Asset.CreatePrefab` | `{goPath, prefabPath}` | `{prefabPath}` | 씬 GO → 프리팹 |
| `Prefab.Open` | `{path}` | `{rootPath, rootInstanceId, prefabPath}` | 격리 편집 모드 진입 |
| `Prefab.Save` | `{}` | `{prefabPath}` | 현재 스테이지 저장 |
| `Prefab.Close` | `{save?: bool}` | `{closed, saved}` | 메인 스테이지로 복귀 |
| `Prefab.GetCurrent` | `{}` | `{isOpen, prefabPath?, rootPath?, rootInstanceId?}` | 상태 조회 |
| `Prefab.InstantiateAsChild` | `{prefabPath, parentPath, name?}` | `{path, instanceId}` | **중첩 프리팹** — 원본 링크 유지. RectTransform이면 anchor 기반 리셋 |
| `Prefab.Apply` | `{path}` | `{applied}` | 인스턴스 오버라이드를 원본에 적용 |
| `Prefab.Unpack` | `{path, completely?: bool}` | `{unpacked}` | 프리팹 연결 해제 |
| `Prefab.CreateVariant` | `{sourcePath, variantPath}` | `{variantPath}` | 소스 프리팹을 상속하는 Variant 생성 — 오버라이드만 변이 |
| `Reflection.Invoke` | `{typeName, methodName, targetInstanceId?, argTypes[], argsJson[]}` | `{returnJson, returnType}` | 전용 op 없는 API 탈출구 |

### 값 인코딩 규약 (`Component.SetField`)

| 타입 | `valueType` | `valueJson` 예시 |
|---|---|---|
| string | `System.String` | `"\"Hello\""` |
| int | `System.Int32` | `"42"` |
| bool | `System.Boolean` | `"true"` |
| enum | `UnityEngine.UI.CanvasScaler+ScaleMode` | `"\"ScaleWithScreenSize\""` |
| Vector3 | `UnityEngine.Vector3` | `"{\"x\":1,\"y\":0,\"z\":0}"` |
| Color | `UnityEngine.Color` | `"{\"r\":1,\"g\":0.5,\"b\":0,\"a\":1}"` |
| Object 참조 (에셋) | `UnityEngine.Sprite` 등 | `"{\"assetPath\":\"Assets/Art/Cards/...png\"}"` |
| Object 참조 (씬 인스턴스) | `UnityEngine.GameObject` 등 | `"{\"instanceId\":12345}"` |

## 사용 예시: 솔리테어 씬 한 방에 조립

Claude가 이 순서로 inbox에 드롭하면 됨:

```
1) Scene.New          → Assets/Scenes/Solitaire.unity
2) GameObject.Create  → name="Canvas", parentPath=null
3) Component.Add      → path="/Canvas", type="UnityEngine.Canvas"
4) Component.Add      → path="/Canvas", type="UnityEngine.UI.CanvasScaler"
5) Component.SetField → path="/Canvas", type="UnityEngine.UI.CanvasScaler",
                        field="uiScaleMode", valueType=".../CanvasScaler+ScaleMode",
                        valueJson="\"ScaleWithScreenSize\""
6) GameObject.Create  → name="GameRoot", parentPath="/Canvas"
7) Component.Add      → path="/Canvas/GameRoot", type="Project.Core.SolitaireGame"
8) Component.SetField → Card Prefab 필드에 Assets/Prefabs/Card.prefab 연결
9) Scene.Save         → (인자 없음)
```

## UGUI 중첩 프리팹 예시: Card 만들고 Button 중첩

시나리오: `Assets/Prefabs/Button.prefab` 이 미리 있다. 이걸 안에 넣는 `Card.prefab` 을 만든다.

```
1) Asset.CreatePrefab   → 임시 빈 GO로 Card.prefab 틀 생성 (혹은 기존 파일 전제)
2) Prefab.Open          → path="Assets/Prefabs/Card.prefab" (스테이지 진입)
3) Component.Add        → path="/Card", type="UnityEngine.RectTransform"  (프리팹 루트에)
4) Component.Add        → path="/Card", type="UnityEngine.UI.Image"
5) Component.SetRectTransform → path="/Card", sizeDelta=[140, 190]  (카드 크기)
6) GameObject.Create    → name="Front", parentPath=null  (스테이지 모드이므로 자동으로 /Card 아래)
7) Component.Add        → path="/Card/Front", type="UnityEngine.UI.Image"
8) Component.SetRectTransform → path="/Card/Front", anchorMin=[0,0], anchorMax=[1,1], offsetMin=[0,0], offsetMax=[0,0]
9) Component.SetField   → /Card/Front Image.sprite ← Assets/Art/Cards/.../card_hearts_A.png
10) Prefab.InstantiateAsChild → prefabPath="Assets/Prefabs/Button.prefab", parentPath="/Card", name="TapArea"
                                (중첩 프리팹: Button 원본 수정 시 Card 안에도 반영됨)
11) Prefab.Save
12) Prefab.Close
```

스테이지가 열려 있을 때 `parentPath=null`은 자동으로 prefab 루트 아래로 해석되고, 경로(`/Card/...`)는 스테이지 범위에서 검색된다.

## 안전장치

- 모든 쓰기는 `Undo.RecordObject`/`Undo.AddComponent` 로 감싸서 에디터에서 Ctrl+Z 복구 가능.
- `Component.Add` 는 `Component` 서브클래스만 허용 (다른 타입 방지).
- 타임아웃·재시도는 클라이언트(Claude) 책임. Unity는 요청당 1회 실행.
- 파괴적 API(`Reflection.Invoke`)는 별도 op. 튜토리얼은 전용 op 우선 사용을 권장.
- `outbox/` 는 `tmp → rename` 원자적 교체. Claude가 부분 작성된 파일을 읽을 가능성 없음.
- `Scene.Save`·`Prefab.Save` 는 명시적 호출이 있어야 디스크에 반영. 배치 모드가 크래시해도 AssetDatabase는 일관 상태 유지.

## 한계 / 향후 개선

- `JsonUtility`가 Dictionary/polymorphic 타입을 다루지 못해 op마다 args 구조체가 필요. Newtonsoft.Json으로 이관하면 범용 `Dictionary<string, object>` 가능.
- 멀티 씬·서브 애셋은 미지원 (필요 시 op 추가).
- 프리미티브 GameObject 생성(`GameObject.CreatePrimitive`)은 아직 전용 op 없음 — 현재 `Reflection.Invoke` 로 호출 후 수동 부모화. 자주 쓴다면 `Primitive.Create` op 추가 권장.
- Python MCP 래퍼([`scripts/claude-bridge-mcp/`](../../scripts/claude-bridge-mcp/))는 GUI 모드 전제로 `unity_call` 호출마다 동기 대기. 순수 헤드리스 배치용 워크플로는 `unity_batch_flush()` 로 분리.
