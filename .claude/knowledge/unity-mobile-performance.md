# Unity Mobile Performance — Engine-layer Knowledge

범용 Unity 모바일 성능 규칙. 특정 프로젝트보다 상위 계층 — 어느 Unity 모바일 프로젝트에나 적용된다. 출처: Unity Technologies, *Optimize Your Mobile Game Performance* (2021 LTS edition).

원칙은 단순하다: **측정하지 않고 최적화하지 않는다.** 아래 규칙은 프로파일러로 문제를 확인한 뒤 적용한다.

---

## 1. Profiling

- **Editor 프로파일링을 신뢰하지 않는다.** Editor 수치는 상대 비교용. 최종 판단은 **실기기 Development Build**에서 내린다. 가장 낮은 지원 스펙 기기로 측정한다.
- **프레임 예산은 타깃의 65%.** 30 fps = 33.33ms 이지만 모바일은 thermal throttle 때문에 실사용 목표는 **~22ms**. 60 fps면 **~11ms**. 나머지는 쿨다운.
- **GPU/CPU bound 구분**은 Profiler Timeline에서 `Gfx.WaitForCommands`(메인 스레드 대기 → CPU bound 가능성) vs `Gfx.WaitForPresent`(GPU 대기 → GPU bound)로 식별.
- **Profile Analyzer 패키지**로 수정 전후 데이터를 diff 한다. 추측 말고 숫자로 증명.
- 플랫폼 전용 도구도 함께 사용: iOS는 Xcode Instruments, Android는 Android Studio Profiler, Arm Mobile Studio, Snapdragon Profiler.
- **프로파일링 중에는 기기를 쿨한 상태로 유지.** 과열된 기기의 수치는 거짓말. 짧게 여러 번 측정.

---

## 2. Memory / GC

Unity는 Boehm-Demers-Weiser GC. GC가 돌면 프로그램 실행이 잠시 멈춘다 → 프레임 스파이크의 주요 원인.

### Heap allocation을 만드는 것들 — 피해야 할 패턴

- **String concatenation / parsing** — `string`은 reference type. `+`, `Substring` 등은 새 객체 할당. 런타임에 조립이 필요하면 `StringBuilder`.
- **JSON/XML 파싱** — 피하고, `ScriptableObject` 또는 바이너리 포맷(MessagePack, Protobuf) 사용.
- **`string` 비교** — `gameObject.tag == "Enemy"`는 `tag` 프로퍼티가 새 문자열을 반환 → allocation. `gameObject.CompareTag("Enemy")`를 쓴다.
- **Boxing** — `int i = 123; object o = i;`는 힙에 박스 생성. `object`, `System.Collections`(non-generic), `LINQ`, `Regex`는 내부적으로 박싱을 유발 → 일반적으로 피한다. `List<T>` 같은 제네릭 컬렉션이 올바른 선택.
- **`new WaitForSeconds(…)` in coroutines** — `yield`는 allocation이 아니지만 `new WaitForSeconds`는 매번 할당. 필드에 캐시하고 재사용.
- **Update 루프 안의 배열/List 생성** — 미리 만들고 재사용.

### GC 제어

- **Incremental GC**를 켠다(Project Settings → Player). 한 번에 멈추는 대신 여러 프레임에 분산. Profile Analyzer로 효과 검증.
- **안전한 시점에 `System.GC.Collect()`**: 컷신, 씬 전환, 로딩 스크린 등 유저가 멈춤을 못 느끼는 지점에 수동 트리거 가능.
- **Memory Profiler** 패키지로 스냅샷을 비교. 누수, 중복 텍스처, 파편화 찾기.

---

## 3. Scripting / PlayerLoop

### Update를 줄여라

- `Update`, `LateUpdate`, `FixedUpdate`에 들어가는 로직은 **매 프레임 필요한지 검증**. 이벤트로 대체 가능하면 이벤트로.
- n 프레임마다 실행: `if (Time.frameCount % interval == 0) ExpensiveCall();` — time slicing 패턴.
- **빈 `Update`/`LateUpdate`도 비용** — 제거한다. 테스트용이면 `#if UNITY_EDITOR`로 감싼다.
- **`Debug.Log`를 매 프레임 쓰지 않는다.** 빌드용 커스텀 Logger를 `[System.Diagnostics.Conditional("ENABLE_LOG")]`로 래핑하고 빌드에서 `ENABLE_LOG` 제거.

### Awake / Start에 무거운 로직 금지

- 첫 씬 로드 시 모든 객체의 `Awake → OnEnable → Start`가 순차 호출 → 첫 프레임까지 시간 지연. 로딩 화면 이후로 미룬다.

### Cache, Cache, Cache

- `GameObject.Find`, `GetComponent<T>`, `Camera.main`(Unity 2020.2 이전 버전)은 Update에서 금지. `Start`에서 캐시.
- Animator/Shader/Material 프로퍼티는 **hash로 접근**: `Animator.StringToHash("Speed")`, `Shader.PropertyToID("_Color")`. 문자열 오버로드는 내부적으로 hash 변환을 반복함.

### 자주 쓰는 최적화 패턴

- **Object Pool** — `Instantiate`/`Destroy`는 allocation + GC 후보 생성. 총알·이펙트 등 자주 만들고 부수는 대상은 미리 N개 만들고 활성/비활성 토글.
- **ScriptableObject** — 변하지 않는 설정·데이터는 `MonoBehaviour`가 아니라 `ScriptableObject`로. 인스턴스화할 때마다 값이 복제되지 않는다.
- **`AddComponent` at runtime은 비싸다** — Prefab에 미리 붙여두고 Instantiate.
- **Transform 변경은 한 번에**: `Transform.SetPositionAndRotation(pos, rot)`으로 position/rotation을 동시에. Instantiate도 `Instantiate(prefab, parent, pos, rot)` 한 방에.
- **Transform 계층을 얕게** 유지. 깊은 계층은 멀티스레드 Transform 업데이트 이득을 잃고, GC 비용도 커진다.

---

## 4. Project Configuration

- **Accelerometer Frequency**: 안 쓰면 0으로. 기본은 초 단위 폴링.
- **Auto Graphics API**: 사용 안 하는 플랫폼은 끈다 — 셰이더 variant 폭증 방지.
- **Target Architectures**: 지원 안 하는 CPU는 뺀다.
- **Physics 비활성화**: 물리 게임이 아니면 `Auto Simulation`, `Auto Sync Transforms` 끈다.
- **Vsync는 모바일에서 항상 하드웨어 수준에서 켜져 있다** — Editor의 Vsync 설정과 무관. 반프레임은 없다.
- **기본 fps는 30** — 60이 꼭 필요하지 않으면 유지. 정적 화면(메뉴 등)에서는 `Application.targetFrameRate`를 더 낮출 수 있다.

---

## 5. Assets (Import Settings)

기본값을 신뢰하지 말 것 — 플랫폼 오버라이드 탭을 쓴다.

### Textures

- **Max Size를 시각적으로 허용 가능한 최소값까지 낮춘다** — 비파괴적, 즉효.
- **POT(Power of Two)** 크기 유지 — PVRTC/ETC 모바일 압축은 POT 필수.
- **Atlas** — Sprite Atlas 또는 TexturePacker로 묶어 draw call 감소.
- **Read/Write Enabled 끈다** — 켜면 CPU+GPU 양쪽에 복사본 → 메모리 2배. 런타임 생성 텍스처는 `Texture2D.Apply(..., makeNoLongerReadable: true)`.
- **Mip Maps** — UI/2D sprite처럼 화면상 크기가 고정인 텍스처는 끈다. 3D 모델은 켠다.
- **압축 포맷**:
  - 기본: **ASTC** (iOS/Android 대다수 지원)
  - iOS A7 이하(iPhone 5/5S): PVRTC
  - 2016년 이전 Android: ETC2
  - 압축이 눈에 거슬리면 **16-bit 비압축**이 32-bit보다 낫다.

### Meshes

- **Compression**: 런타임 메모리는 불변이지만 디스크/다운로드 크기 감소. 양자화로 모델이 찌그러질 수 있어 레벨별로 테스트.
- **Read/Write**: 끈다 (CPU 복사본 불필요). 2019.2 이전은 기본 on.
- **Rigs / BlendShapes**: 스켈레탈/블렌드셰이프 애니메이션 안 쓰면 끈다.
- **Normals / Tangents**: 재질이 필요로 하지 않으면 끈다.
- **폴리곤 수 절제** — 카메라에 안 보이는 면은 DCC에서 삭제. 고밀도 메시 대신 normal map.

### Automate

- `AssetPostprocessor`로 import 시점에 규칙을 코드로 강제. 기본값 의존 금지.
- **Addressables** — 비코드 에셋(Model, Texture, Prefab, Audio, Scene)을 address로 비동기 로드. 초기 빌드 축소 + CDN 전송.

### Audio

- **원본은 WAV** — MP3/Vorbis로 시작하면 Unity가 재압축해서 double-lossy.
- **3D 공간음은 mono** — 또는 Force To Mono. 스테레오를 3D로 쓰면 CPU/메모리 낭비.
- **압축**:
  - 일반 효과음: **Vorbis**
  - 짧고 자주 나는 효과음(발소리, 총소리): **ADPCM** — 압축되지만 디코딩이 싸다.
  - 루프 안 도는 효과음: MP3도 가능.
- **샘플레이트** 22,050 Hz 이하로.
- **Load Type**:
  - `< 200 KB`: Decompress on Load
  - `>= 200 KB`: Compressed in Memory
  - 배경음악: **Streaming** (전체를 메모리에 올리지 않음)
- **Mute는 볼륨 0이 아니라 AudioSource `Destroy`** — 메모리에서 내린다. 토글 빈번하지 않을 때.

---

## 6. Graphics / GPU

### Batching

- **Dynamic batching**: 소형 메시(vertex attribute 900 이하, vertex 300 이하) 전용. 큰 메시에 대해서는 오히려 CPU 낭비 → 꺼야 할 수도.
- **Static batching**: 움직이지 않는 같은 머터리얼 공유 메시. 메모리 추가 대신 draw call 대폭 감소.
- **GPU instancing**: 동일 메시 다수 인스턴스 (풀, 파티클 등).
- **SRP Batcher**: URP Asset → Advanced에서 켠다. 씬에 따라 CPU 렌더 시간 큰 폭 감소.
- **Frame Debugger**로 draw call 단계를 눈으로 확인.

### Lighting

- **동적 라이트 최소화.** URP 기본은 limit가 있다.
- **Shadow casting 끄기** — per-MeshRenderer, per-Light 모두. 가짜 그림자는 블러 텍스처 + quad 또는 blob shader.
- **Lightmap 굽기** — 정적 지오메트리는 Contribute GI로 마킹. Progressive GPU Lightmapper로 가속.
- **Light Probes** — 움직이는 객체에는 Spherical Harmonics 기반 Light Probe (동적 라이트보다 훨씬 싸다).
- **Light Layers / culling mask** — 라이트 영향 범위 제한.

### Draw call / Rendering

- **LOD Groups** — 원거리 메시는 단순 메시 + 단순 셰이더로 스왑.
- **Occlusion Culling** — 큰 가림이 많은 실내/도시 씬에 효과적. Static Occluder/Occludee로 베이크.
- **해상도 하향**: 고해상도 기기에서 `Screen.SetResolution(w, h, false)`로 렌더 해상도 낮추기.
- **카메라 하나 더 = CPU +최대 1ms.** 필요한 카메라만.
- **셰이더 variant 최소화** — URP 기본 Lit/Unlit은 모바일 최적화됨. variant가 늘수록 런타임 메모리 증가.
- **Overdraw / alpha blending 제한** — 반투명 레이어 중첩 줄이기. RenderDoc으로 overdraw 시각화.
- **Post-processing 제한** — fullscreen glow/bloom은 모바일에서 큰 비용.

### 자주 하는 실수

- **`Renderer.material` 접근 = 머터리얼 복제** → 기존 batch 깨짐. 읽기만 할 때는 `Renderer.sharedMaterial`.
- **SkinnedMeshRenderer는 비싸다** — 애니메이션이 항상 필요하지 않으면 `BakeMesh`로 정적 MeshRenderer로 전환.
- **Reflection Probe** — 저해상도 cubemap + culling mask + 텍스처 압축.

---

## 7. UI (UGUI)

Canvas는 draw call 소스가 자주 된다.

- **Canvas 분할** — 큰 단일 Canvas는 한 요소만 바뀌어도 전체 재빌드 → CPU 스파이크. 정적 UI와 동적 UI를 다른 Canvas로.
- **같은 Canvas 안 요소는 동일 Z, 동일 material, 동일 texture** — 아니면 batching이 끊긴다.
- **숨겨진 UI는 `Canvas` 컴포넌트 `enabled = false`** — GameObject 비활성화 대신. mesh/vertex 재빌드 회피.
- **`GraphicRaycaster`는 루트에서 제거**하고 필요한 개별 요소(버튼, Scroll Rect)에만 붙인다.
- **`Raycast Target` 끄기** — 텍스트/이미지 중 interact 필요 없는 것 전부.
- **Layout Group은 비싸다** — 정적 레이아웃이면 anchor로 대체. 꼭 써야 하면 중첩 피하기.
- **대형 List/Grid**는 풀링해서 화면에 보이는 만큼만 활성화.
- **오버레이 많으면 런타임에 병합** — 카드가 겹친 전투 UI 등.
- **Fullscreen UI (일시정지/메뉴)** — 3D 카메라 render 끄기, `targetFrameRate` 낮추기.
- **World/Camera Space Canvas**는 Event Camera 명시 — 빈칸이면 `Camera.main`을 매번 호출해서 비싸다. 가능하면 `Screen Space – Overlay`.

---

## 8. Animation

- **Humanoid Rig는 Generic Rig 대비 CPU 30–50% 추가 사용** — IK, retargeting 계산이 매 프레임 돈다. 필요 없으면 Generic.
- **Animator를 단순 값 애니메이션에 쓰지 말 것** — UI alpha 트윈 같은 건 DOTween 같은 트윈 라이브러리가 훨씬 싸다.
- 모바일 UI는 legacy Animation 컴포넌트도 고려.

---

## 9. Physics

- **`Prebake Collision Meshes`** 켠다 (Player Settings).
- **Layer Collision Matrix 단순화** — 의미 없는 레이어 쌍 체크 끄기.
- **`Auto Sync Transforms` 끄기, `Reuse Collision Callbacks` 켜기** (Project Settings → Physics).
- **Mesh Collider 최소화** — 단순 primitive(Box, Sphere, Capsule) 조합으로 근사.
- **Rigidbody는 `Transform`이 아닌 `MovePosition` / `AddForce`로 이동** — Transform 직접 조작은 물리 월드 재계산을 유발. `FixedUpdate`에서만.
- **Fixed Timestep을 타깃 프레임레이트에 맞춘다** — 기본 0.02(50 Hz). 30 fps 타깃이면 ~0.03. 타임스텝이 촘촘하면 프레임 드롭 시 FixedUpdate가 여러 번 호출되며 스파이크.
- **Maximum Allowed Timestep 낮추기** — 히치가 길어질 때 물리/애니메이션 영향 제한.

---

## 10. Adaptive Performance (Samsung 전용)

- 온도·파워 상태를 API로 읽어 LOD bias 등을 동적으로 조정.
- Automatic mode는 frame rate, 온도, thermal proximity, CPU/GPU bound 4개 지표로 자동 튜닝.
- 이 기능은 **Samsung 기기에서만 동작** — 크로스 플랫폼 대응책이 아님.

---

## 11. Workflow

- **Asset Serialization Mode는 항상 Force Text** (Editor Settings) — diff/merge 가능.
- **외부 VCS면 Version Control Mode = Visible Meta Files**.
- **씬을 작게 나누기** — 하나의 대형 씬은 협업 충돌. `SceneManager.LoadSceneAsync(..., LoadSceneMode.Additive)`.
- **서드파티 플러그인의 샘플/테스트 에셋 제거** — 빌드에 딸려 들어간다.

---

## 마스터 체크리스트

문제가 있을 때 이 순서로 의심:

1. 프로파일을 실기기에서 잡았는가? (Editor 숫자는 참고만)
2. CPU-bound vs GPU-bound 식별했는가?
3. GC 스파이크가 원인인가? (GC Alloc 열 확인)
4. 프레임 budget 안에 있는가? (30fps = ~22ms / 60fps = ~11ms)
5. 텍스처 import 설정이 플랫폼별로 최적화되어 있는가?
6. Batching 상태는? (Frame Debugger)
7. Dynamic light 수, Shadow caster 수는?
8. Canvas 분할됐는가, Raycast Target 정리됐는가?
9. 물리가 FixedUpdate에 한정되어 있는가?
10. 수정 전후 Profile Analyzer로 diff를 뜨고 있는가?
