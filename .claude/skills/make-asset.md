---
name: make-asset
description: Unity 어셋(UGUI 프리팹 / 파티클 / 프리미티브 placeholder 모델 / SVG 생성·래스터화한 아이콘 스프라이트)을 Claude Bridge 기반으로 제작. 사용자 요구가 모호하면 샘플 이미지·링크를 요구하고, 어떤 종류(UI/파티클/아이콘/모델)를 만들지 선택지로 제시. 에이전트가 게임 프로토타이핑 중 자산이 부족할 때 자동으로 호출해도 되는 스킬.
---

# Skill: /make-asset

유니티 게임에 필요한 어셋을 즉석에서 제작한다. 인간이 그림·3D 모델 없어도 Claude가 **UGUI 프리팹 / 파티클 / 프리미티브 모델 / 스프라이트 임포트** 네 가지를 ClaudeBridge로 조립할 수 있다.

이 스킬은 **에이전트도 사용자 명령 없이 호출** 가능. 예를 들어 솔리테어 게임 로직을 짜다 "카드 프리팹이 필요" 라는 판단이 서면 `/make-asset` 를 불러 즉시 만든다.

---

## 1. 트리거와 인자

```
/make-asset {kind?} {spec?}
```

`kind`는 선택. 없으면 사용자 대화로 결정.

| kind | 용도 | 대표 출력 |
|---|---|---|
| `ui` / `prefab` / `button` | UGUI 프리팹 | `Assets/Prefabs/UI/*.prefab` (RectTransform + Image/Text/Button 조합) |
| `particle` / `vfx` | 파티클 이펙트 | `Assets/Prefabs/VFX/*.prefab` (ParticleSystem + Renderer) |
| `model` / `mesh` / `placeholder` | 프리미티브 조합 임시 모델 | `Assets/Prefabs/Models/*.prefab` (Cube/Sphere/Cylinder 스케일·부모-자식) |
| `sprite` / `icon` | **SVG 직접 작성 → PNG 래스터화 → Sprite 임포트**, 또는 사용자 제공 이미지 임포트 | `Assets/Art/Icons/*.png` + `.svg` 원본 보존 + TextureImporter 옵션 |

`spec`은 자유 서술 (예: "빨간 체크 표시 아이콘 64x64", "카드 한 장. 앞면은 heart A, 뒷면은 파란 패턴", "흰 연기 파티클, 2초 지속").

---

## 2. 명세 수집 (모호하면 질문)

요구사항이 분명하지 않으면 **먼저 물어본다**. 추측으로 진행하지 않는다.

물어볼 체크리스트 (kind별):

### UI 프리팹
1. 화면 어디에 놓일 것? (전체 화면 덮는 패널 / 버튼 / 작은 위젯)
2. 크기 기준 — 디자인 해상도는 얼마? (기본 1920×1080 가정)
3. 상태별 비주얼 — Normal / Hover / Pressed / Disabled 모두 필요?
4. 텍스트 들어가는가? 폰트는?
5. **참조 이미지·목업이 있으면 첨부 부탁** (파일 또는 URL)

### 파티클
1. 효과 성격 (타격, 폭발, 연기, 반짝임, 치유 오라 등)
2. 지속 시간, 루프 여부
3. 지배색
4. 입자 수 규모 (가벼운 반짝임 ~20 / 폭발 ~500)
5. 참조 영상·GIF 있으면 요청

### 프리미티브 모델
1. 뭘 나타내려는지 (캐릭터 / 무기 / 건물 / 장애물)
2. 대략적 비율·크기 (예: 사람 키 정도, 작은 상자)
3. 색 구분이 필요한 부분
4. 나중에 실제 모델로 교체할지 여부 (교체 예정이면 프리팹 이름/경로를 그에 맞게)

### 스프라이트/아이콘
1. **경로 두 갈래 — 먼저 어느 쪽인지 판단**:
   - **(A) 심볼·아이콘·도형류** (하트, 별, 체크, X, 화살표, 기어, 다이아몬드, 방패, 카드 슈트 등): Claude가 SVG를 **직접 작성**하고 PNG로 래스터화해서 임포트. 이미지 요청 불필요.
   - **(B) 사진·복잡한 일러스트**: Claude는 이미지 생성 불가. 사용자에게 파일 또는 URL 요청.
2. 목표 픽셀 크기 (기본: 단일 아이콘 256×256, 세트는 128×128)
3. 색 팔레트 (지정 없으면 흰색 + 테두리로 placeholder)
4. PPU (기본 100), 필터 모드 (기본 Bilinear, 도트풍이면 Point), 압축 (기본 None for tiny icons)
5. 해상도 스케일이 필요하면 `@2x`, `@4x` 변형도 같이 뽑을 것인가

---

## 3. 선행 조건 확인

`/make-asset`은 ClaudeBridge가 돌고 있어야 한다. 다음 순서로 확인:

1. `unity_bridge_status()` — `editor_running: true` 면 OK, `false`면 사용자에게 `/run editor` 먼저 돌리라고 안내 또는 `unity_batch_flush()` 경로로 전환.
2. 사용자 쪽에 `claude-bridge` MCP가 등록 안 되어 있으면 [`scripts/claude-bridge-mcp/README.md`](../../scripts/claude-bridge-mcp/README.md) 링크 안내.
3. MCP가 없고 Filesystem MCP만 있는 환경이면 `.claude-bridge/inbox/` 직접 쓰기 + `/run bridge` 호출로 대체.

---

## 4. 제작 패턴

### 4-1. UGUI 프리팹 — Button 예시

```
unity_call("Prefab.Open", {path: "Assets/Prefabs/UI/PrimaryButton.prefab"})
    # 파일이 없으면 먼저 Asset.CreatePrefab으로 빈 틀 생성 후 Open

unity_call("Component.Add",           {path: "/PrimaryButton", type: "UnityEngine.UI.Image"})
unity_call("Component.SetRectTransform", {path: "/PrimaryButton", sizeDelta: [320, 80], pivot: [0.5, 0.5]})
unity_call("Component.SetField",      {path: "/PrimaryButton", type: "UnityEngine.UI.Image", field: "color", valueType: "UnityEngine.Color", valueJson: "{\"r\":0.2,\"g\":0.6,\"b\":1,\"a\":1}"})
unity_call("Component.Add",           {path: "/PrimaryButton", type: "UnityEngine.UI.Button"})

# 라벨 자식
unity_call("GameObject.Create",       {name: "Label", parentPath: null})   # 스테이지 모드라 /PrimaryButton 자동 부모
unity_call("Component.Add",           {path: "/PrimaryButton/Label", type: "UnityEngine.UI.Text"})
unity_call("Component.SetRectTransform", {path: "/PrimaryButton/Label", anchorMin: [0,0], anchorMax: [1,1], offsetMin: [0,0], offsetMax: [0,0]})
unity_call("Component.SetField",      {path: "/PrimaryButton/Label", type: "UnityEngine.UI.Text", field: "text", valueType: "System.String", valueJson: "\"Click Me\""})
# alignment, fontSize 등 이어서 세팅

unity_call("Prefab.Save", {})
unity_call("Prefab.Close", {})
```

생성 규칙:
- 파일 경로는 `Assets/Prefabs/UI/<PascalCase>.prefab`
- 상태 변형은 Prefab Variant로 (예: `PrimaryButton_Disabled.prefab`) — `Prefab.CreateVariant` 사용
- 텍스처/스프라이트가 필요하면 사용자에게 이미지 먼저 요청 (3-4 flow)

### 4-2. 파티클

```
unity_call("GameObject.Create",   {name: "HitSpark", parentPath: null})
unity_call("Component.Add",       {path: "/HitSpark", type: "UnityEngine.ParticleSystem"})
# ParticleSystem의 main/emission 모듈 설정은 SerializedObject 경로이므로 Reflection.Invoke로 호출:
unity_call("Reflection.Invoke", {
  typeName: "UnityEngine.ParticleSystem",
  methodName: "set_startLifetime",  # property setter
  targetInstanceId: "<id>",
  argTypes: ["UnityEngine.ParticleSystem+MinMaxCurve"],
  argsJson: ["{\"mode\":0,\"constant\":1.5}"]
})
# 혹은 Component.SetField로 main/emission 구조체 통째 세팅

# Asset.CreatePrefab으로 저장
unity_call("Asset.CreatePrefab", {goPath: "/HitSpark", prefabPath: "Assets/Prefabs/VFX/HitSpark.prefab"})
unity_call("GameObject.Delete", {path: "/HitSpark"})
```

파티클은 Unity 직접 API가 복잡하므로 대부분 `Reflection.Invoke` 또는 구조체 통째 `Component.SetField` 패턴. 단순한 경우(색, 수명, 방출량)만 이 스킬이 커버하고, 복잡한 커브는 "사용자가 원하는 느낌 설명 → Claude가 근사치 프리팹 생성 → 사용자가 Editor에서 미세 조정"으로 넘긴다.

### 4-3. 프리미티브 placeholder 모델

```
unity_call("GameObject.Create", {name: "Enemy", parentPath: null})

# 몸통 — Cube
unity_call("GameObject.Create", {name: "Body", parentPath: "/Enemy"})
unity_call("Reflection.Invoke", {
  typeName: "UnityEngine.GameObject",
  methodName: "CreatePrimitive",
  argTypes: ["UnityEngine.PrimitiveType"],
  argsJson: ["\"Cube\""]
})  # (이 방식은 Enemy 자식으로 안 감. 실제로는 primitive 생성 후 SetParent가 더 정석)

# 간편화: Component.Add + MeshFilter/MeshRenderer로 조립하거나,
# 새로운 전용 op "Primitive.Create(parent, kind)"를 C# 쪽에 추가하는 게 깔끔
```

> **TODO**: 프리미티브 조립이 잦으면 `Primitive.Create { parentPath, kind: Cube|Sphere|Cylinder|Capsule|Plane|Quad, localScale, color }` op을 ClaudeBridge에 추가할 것.
> 현재는 Reflection.Invoke로 `GameObject.CreatePrimitive` 호출 후 수동 부모화가 최선.

### 4-4. 스프라이트 / 아이콘

#### 4-4-A. Claude가 SVG로 직접 그리는 경로 (심볼·도형류)

**전체 흐름**: SVG 작성 → PNG 래스터화 → `Assets/Art/Icons/` 로 이동 → `Asset.Refresh` → TextureImporter를 Sprite로 구성.

**Step 1. SVG 작성.** 파일은 `Assets/Art/Icons/<name>.svg` 에 원본 보존 (재래스터화·편집용).

예시 — 하트 아이콘 (카드 슈트):

```xml
<!-- Assets/Art/Icons/heart.svg -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <path d="M50 85 C 20 65, 10 35, 30 20 C 40 12, 50 20, 50 30 C 50 20, 60 12, 70 20 C 90 35, 80 65, 50 85 Z"
        fill="#e74c3c" stroke="#c0392b" stroke-width="2"/>
</svg>
```

자주 쓰는 도형 패턴:
- **원형 배지**: `<circle cx="50" cy="50" r="45" fill="..." />`
- **둥근 사각형**: `<rect x="10" y="10" width="80" height="80" rx="15" fill="..." />`
- **별**: `<polygon points="50,5 61,38 95,38 67,58 78,92 50,72 22,92 33,58 5,38 39,38"/>`
- **체크**: `<path d="M20 50 L45 75 L85 25" stroke="#2ecc71" stroke-width="10" fill="none" stroke-linecap="round" />`
- **X**: `<path d="M20 20 L80 80 M80 20 L20 80" stroke="#e74c3c" stroke-width="10" stroke-linecap="round" />`
- **화살표**: `<path d="M20 50 L70 50 M55 35 L70 50 L55 65" stroke="#333" stroke-width="8" fill="none" stroke-linecap="round" stroke-linejoin="round" />`

`viewBox`는 항상 `0 0 100 100` 정방형으로 통일. 그 안에서 좌표 사용하면 PNG 크기 바꿔도 동일 형태.

**Step 2. PNG 래스터화.** 도구 우선순위 (Bash로 확인 후 사용):

```bash
# 1) rsvg-convert (권장, brew install librsvg 필요) — SVG 스펙 정확
rsvg-convert -w 256 -h 256 heart.svg -o heart.png

# 2) ImageMagick (기본 설치 가능성 높음) — 내부 SVG 렌더러, 간단한 도형엔 충분
magick -background none -density 400 heart.svg -resize 256x256 heart.png

# 3) macOS QuickLook (폴백) — rsvg/magick 둘 다 없을 때만
qlmanage -t -s 256 -o . heart.svg && mv heart.svg.png heart.png
```

ImageMagick의 SVG 내부 렌더러는 `stroke-linecap`, gradient 일부를 제대로 안 그릴 수 있음. 결과 확인 후 틀어지면 `brew install librsvg`로 rsvg-convert 설치 권유.

**Step 3. Unity 임포트 + Sprite 설정.** PNG를 `Assets/Art/Icons/`로 이동한 뒤 ClaudeBridge로:

```python
unity_call("Asset.Refresh", {})

# TextureImporter 얻기 (리플렉션 — AssetImporter.GetAtPath는 static)
r = unity_call("Reflection.Invoke", {
    "typeName": "UnityEditor.AssetImporter",
    "methodName": "GetAtPath",
    "argTypes": ["System.String"],
    "argsJson": ["\"Assets/Art/Icons/heart.png\""]
})
# r.returnJson 의 instanceId 를 뽑아서 target으로 재사용

importer_id = json.loads(r["returnJson"])["instanceId"]

# textureType = Sprite (TextureImporterType.Sprite = 8)
unity_call("Reflection.Invoke", {
    "typeName": "UnityEditor.TextureImporter",
    "methodName": "set_textureType",
    "targetInstanceId": str(importer_id),
    "argTypes": ["UnityEditor.TextureImporterType"],
    "argsJson": ["\"Sprite\""]
})

# spritePixelsPerUnit, filterMode, textureCompression 등 추가 세팅
# ... (Component.SetField 대신 Reflection.Invoke로 property setter 호출)

# 변경 반영
unity_call("Reflection.Invoke", {
    "typeName": "UnityEditor.AssetImporter",
    "methodName": "SaveAndReimport",
    "targetInstanceId": str(importer_id),
    "argTypes": [],
    "argsJson": []
})
```

> **향후 개선**: 임포터 설정이 3~5번 반복 호출되므로 `Asset.ImportAsSprite(path, ppu, filterMode, compression)` 같은 전용 op을 ClaudeBridge에 추가하면 한 줄로 끝남.

#### 4-4-B. 사용자 제공 이미지 임포트 (사진·일러스트)

```
# 사용자가 이미지 파일을 Assets/Art/ 아래 드롭하거나 URL 제공.
# URL이면 curl로 받아서 해당 경로로 이동.
curl -L -o Assets/Art/Icons/player-portrait.png "https://..."

# 이후는 4-4-A의 Step 3과 동일 (Asset.Refresh + TextureImporter 구성)
```

---

## 5. 완료 후

1. 생성된 프리팹 경로를 사용자에게 알린다.
2. 변경된 어셋에 대해 `Asset.Refresh` 한 번 호출.
3. 에이전트 내부에서 호출한 경우: 부모 태스크(`/task-start`로 선언한 범위)로 돌아가 이 어셋을 이용한 다음 단계 진행.

---

## 6. 에이전트 자율 호출 지침

사용자가 직접 `/make-asset`을 말하지 않아도, 아래 상황에서는 에이전트가 선제적으로 호출한다:

- 스크립트를 작성 중인데 Inspector에 붙일 프리팹이 없을 때 (예: `[SerializeField] GameObject cardPrefab;`)
- 씬을 조립 중인데 "카드", "버튼", "이펙트" 같은 게임 오브젝트가 참조로 지목됐지만 해당 에셋이 `Assets/` 어디에도 없을 때
- 솔리테어/퍼즐/플랫포머 같은 기본 장르 프로토타입을 만들 때 첫 단계

호출 전 사용자에게:
- "Card 프리팹이 필요한데 없습니다. `/make-asset ui` 로 간단한 카드 프리팹 만들까요? 이미지 있으면 첨부해 주세요 — 없으면 색 블록으로 임시 생성합니다."
식으로 한 줄만 알리고 진행한다. 무응답 시 기본값(임시 색 블록)으로 제작하되 나중에 교체 가능하도록 파일명·경로를 명시.

---

## 7. 금지 사항

- **외부 에셋 스토어에서 자동 다운로드 금지**. 라이선스 불명. 사용자가 명시적으로 URL을 주거나 파일을 첨부할 때만 임포트.
- **`.meta` 파일 직접 편집 금지** (RULES.md RULE-04). 모든 에셋 설정은 `AssetImporter` API를 통해.
- **심링크 폴더(`Art`, `Prefabs`, `Materials` 등)는 워크트리에선 읽기 전용**. 메인 워크트리에서만 생성. 워크트리에서 호출되면 사용자에게 "메인 워크트리로 이동 후 다시 요청" 안내.
- **대용량 바이너리(>10MB) 리포지토리 커밋 금지**. Git LFS 또는 외부 스토리지 권장.
