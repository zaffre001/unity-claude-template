# Claude Code Index — 지식·지침·스킬 인덱스

에이전트는 `/task-start`에서 이 파일을 **먼저** 읽고, 작업 주제와 매칭되는 파일만 선별 로드한다.  
모든 지침을 매 세션 통째로 로드하면 토큰 낭비다.

## 읽기 전략

1. **이 파일(`INDEX.md`)만 먼저 읽는다** (수백 토큰).
2. 작업 프롬프트의 키워드와 각 항목의 `keywords` 필드를 매칭한다.
3. 매칭된 파일만 `Read`로 펼친다. 매칭 안 되면 읽지 않는다.
4. `RULES.md`는 **항상 스캔** (불변 제약).
5. `CLAUDE.md`는 **항상 이미 로드되어 있다** (세션 시작 시 주입).

---

## Level 1 — Language & Engine (범용, 최상단)

다른 Unity/C# 프로젝트에도 그대로 적용되는 지식. 프로젝트 도메인보다 상위.

### [knowledge/RULES.md](knowledge/RULES.md) — 항상 스캔 (범용 코딩 강제 규약)
Chris Zimmerman의 21개 프로그래밍 규약 (R1~R21).
- **keywords:** simplicity, abstraction, generalization, optimization, premature optimization, code review, dead code, collapsible code, localize complexity, big-O, convention, naming, root cause, refactor, weed, parallel rework, comment
- **when to read:** **항상** 스캔 (짧고 모든 작업에 적용). 이 파일은 1계층이며, 루트 [`../../RULES.md`](../../RULES.md)(프로젝트 불변 제약, 3계층)와 **다르다**.

### [knowledge/unity-mobile-performance.md](knowledge/unity-mobile-performance.md)
Unity 모바일 성능 최적화 (2021 LTS 기준).
- **keywords:** profiling, GC, GC.Collect, draw call, batching, dynamic batching, SRP batcher, texture, compression, ASTC, PVRTC, ETC, mesh, UGUI, canvas, GraphicRaycaster, raycast target, layout group, physics, FixedUpdate, collider, rigidbody, shader, lighting, lightmap, light probe, LOD, occlusion culling, audio, animation, animator, humanoid rig, coroutine, WaitForSeconds, frame budget, fps, targetFrameRate, vsync, object pool, ScriptableObject, Addressables, overdraw, post-processing
- **when to read:** 성능·메모리·프레임 이슈, 렌더·물리·UI·오디오·애니메이션 튜닝, 빌드/임포트 설정

### [knowledge/csharp-dotnet.md](knowledge/csharp-dotnet.md)
C#/.NET 언어 핵심.
- **keywords:** value type, reference type, struct, class, stack, heap, boxing, unboxing, string, StringBuilder, event, delegate, subscribe, unsubscribe, generic, constraint, Nullable, nullable, null coalescing, equality, Equals, GetHashCode, virtual, override, sealed, async, await, CancellationToken, UniTask, property, field, exception, LINQ, IEnumerable, foreach, interface, List<T>, Dictionary, HashSet, Queue, Stack, ArrayList, Hashtable
- **when to read:** 새 타입 설계, async·이벤트 구현, 자료구조 선택, 박싱/할당 판단, C# 스펙 관련 결정

### [knowledge/qa/](knowledge/qa/) — Global App Testing QA 10원칙 (per-file, 게임 적용 필터 적용)
*The Ultimate QA Testing Handbook* 9장 중 게임 테스트에 유효한 10 규칙만 Do/Don't 로. Crowdtesting·Usability 챕터는 에이전트 QA 와 부적합으로 폐기.
- **keywords:** QA, test case, narrow focus, measurable, test environment, test early, test often, traceability, RTM, requirement, regression, regression script, exploratory, learn design execute, session-based, unit test, integration test, system test, E2E, functional, non-functional, performance, load, reliability, bug report, severity, repro, reproduction, localization, i18n, RTL, TextMeshPro, CultureInfo
- **when to read:** QA 전략 수립·케이스 설계·리포트 작성 시. `/qa` 스킬 §7 인덱스에서 해당 규칙만 펼쳐 본다.

### [knowledge/debugging/](knowledge/debugging/) — Adragna 디버깅 10원칙 (per-file)
P. Adragna, *Software debugging techniques*, CERN School of Computing 2007. 방법론 원칙 10개를 Do/Don't 형식으로.
- **keywords:** debugging, assumption, classify, Bohrbug, Heisenbug, symptom, cause, root cause, fix, bug journal, static analysis, compiler warning, Debug.Log, print debugging, assertion, Assert, rubber duck, ACI, binary split, bisect, stuck, reframe
- **when to read:** 버그·예외·오작동 조우 시. `/debug` 스킬 §6 인덱스에서 해당 규칙만 펼쳐 본다.

### [knowledge/unity-editor-automation.md](knowledge/unity-editor-automation.md)
ClaudeBridge 스택 (C# op + Python MCP + /run + /make-asset 연동) 운용 지침.
- **keywords:** ClaudeBridge, unity_call, unity_batch_flush, bridge-run, editor automation, headless, batchmode, prefab stage, prefab variant, nested prefab, inbox, outbox, Component.SetRectTransform, Prefab.Open, Prefab.CreateVariant, InstantiatePrefab, make-asset, run editor, run bridge
- **when to read:** Unity 씬/프리팹/컴포넌트 조작이 필요할 때, 에이전트가 Editor 작업을 자동 실행하려 할 때, ClaudeBridge op 추가·확장 작업

---

## Level 2 — Project Domain (이 프로젝트 전용)

프로젝트만의 기획·시스템·유기적 관계. `/task-done`이 새 도메인 지식을 여기에 쌓는다.

### [../CLAUDE.md](../CLAUDE.md) — 세션 시작 시 자동 로드됨
프로젝트 개요, 어셈블리 구조, 네임스페이스, 워크트리·디버깅 환경, 에디터 실행 검증 규칙(컴퓨터 유즈 금지·Bridge 우선), 코딩 컨벤션.
- **keywords:** Project.Core, Project.UI, Project.Combat, Project.Rendering, assembly, asmdef, autoReferenced, namespace, `_Core`, `_UI`, `_Combat`, `_Rendering`, worktree, Domain Reload disabled, RuntimeInitializeOnLoadMethod, ClaudeBridge, bridge-run, computer-use forbidden, editor verification, run editor
- **when to read:** **항상** (자동 주입)

### [domain/](domain/) — 시스템·기획 단위 파일 누적
빈 스켈레톤 상태에선 파일이 없다. `/task-done`이 새 지식을 파일로 추가한다.
- **keywords (동적):** 파일이 생길 때마다 이 인덱스에 `keywords` 추가.
- **when to read:** 해당 시스템/기획의 내부 동작·연관 관계 파악이 필요할 때

---

## Level 3 — Immutable Constraints

### [../RULES.md](../RULES.md) — 항상 스캔
위반 시 시스템이 실제로 망가지는 규칙만. 현재 6개.
- **keywords:** RULE-01, RULE-02, RULE-03, RULE-04, RULE-05, RULE-06, Domain Reload, InitializeOnLoad, symlink, .meta, GUID, FixedUpdate, physics, CancellationToken, ProjectSettings
- **when to read:** 모든 작업 (짧으니 매번 재확인)

---

## Level 4 — Path-scoped Rules

특정 경로/파일 타입에 한정된 규칙. 해당 경로 작업 시에만 로드.

### [rules/scripts.md](rules/scripts.md)
`Assets/Scripts/**/*.cs` 작성 규칙.
- **keywords:** namespace, static, event, OnDestroy, GetComponent, async, cache
- **when to read:** .cs 파일 생성/수정

### [rules/asmdef.md](rules/asmdef.md)
`.asmdef` 파일 규칙.
- **keywords:** asmdef, assembly definition, autoReferenced, defineConstraints, dependency
- **when to read:** .asmdef 생성/수정

### [rules/parallel-work.md](rules/parallel-work.md)
워크트리 병렬 작업 중 에셋·프리팹·파일 배치 규칙.
- **keywords:** worktree, parallel, prefab placement, feature folder, symlink, add vs modify, multi-editor
- **when to read:** 워크트리에서 작업 중, 새 프리팹·머티리얼·텍스처 생성 직전

---

## Level 5 — Skills (on-demand)

에이전트가 `/name`으로 호출하는 공정. **사용자 명령을 기다리지 말고 적절한 타이밍에 에이전트가 선제적으로 호출해도 되는 스킬은 "자동 호출 가능" 표시.**

| Skill | 역할 | 호출 타이밍 | 자동 호출 |
|---|---|---|---|
| [task-start](skills/task-start.md) | 작업 착수 브리핑 (이 인덱스 활용) | 모든 작업 시작 | ✓ |
| [task-done](skills/task-done.md) | 작업 마무리 + 도메인 지식 승격 | 모든 작업 완료 | ✓ |
| [self-update](skills/self-update.md) | 세션 지식을 5개 계층에 승격 제안 | 새 패턴 발견 시 | — |
| [design](skills/design.md) | 기획 → 단계별 에이전트 프롬프트 | 새 기획 받았을 때 | — |
| [run](skills/run.md) | Unity 빌드 / Editor 실행(`editor`) / ClaudeBridge 헤드리스(`bridge`) | 검증·실행·Editor 구동 필요 시 | **✓** — 빌드 확인, Editor 띄워야 할 때, inbox 커맨드 flush가 필요할 때 에이전트 판단으로 호출 |
| [make-asset](skills/make-asset.md) | Unity 어셋 제작: UGUI 프리팹 / 파티클 / 프리미티브 모델 / **SVG 직접 그려 PNG 래스터화한 아이콘 스프라이트** / 사용자 제공 이미지 임포트 | 참조할 어셋이 Assets/ 아래 없는데 필요할 때 | **✓** — 씬 조립 중 missing prefab 발견, 프로토타이핑 첫 어셋 필요 시, 심볼/아이콘(하트·별·체크 등)이 요구될 때 SVG로 즉석 생성 |
| [debug](skills/debug.md) | 에이전트 주도 디버깅 툴 사용 지침: LSP 로 소스 평가 → Bridge 로 런타임 재현·검증 (DAP 대체 포함) | 버그·예외·오작동 증상을 받은 직후 | — 사용자가 `/debug` 로 호출 (자동 호출 아님; 워크플로우는 별도 규약) |
| [qa](skills/qa.md) | 에이전트 주도 QA 툴 사용 지침: 기준 문서화 → LSP/Grep 정적 평가 → Bridge 런타임 확인 → 기준별 Pass/Fail 보고 | 기능·규칙 준수 여부 평가가 필요할 때 | — 사용자가 `/qa` 로 호출 (자동 호출 아님; 워크플로우는 별도 규약) |
| [lsp-setup](skills/lsp-setup.md) | Claude Code 의 LSP 툴이 `.cs` 를 인식하도록 OmniSharp 자동 설치·설정. 프로그래밍 모르는 사용자 대상 에이전트 주도 세팅. | `No LSP server available for file type: .cs` 응답 받은 직후 | **✓** — `/debug`·`/qa` 가 LSP 미연결 탐지하면 자동 호출 |
| [synth](skills/synth.md) | sin/cos 가산합성 신디사이저. 피아노·패드·리드·베이스·앰비언트 프리셋 + ADSR·비브라토·디튠·bitcrush 로 NES 풍 WAV 생성. Python stdlib 만. | 효과음·BGM·placeholder 오디오가 필요한데 `AudioClip` 이 비어 있을 때 | **✓** — UI SE 공란, 점프/피격/메뉴 피드백 부재, 타이틀/앰비언트 BGM 필요 시 에이전트 판단으로 호출 |

**자동 호출의 의미**: `/run editor`·`/run bridge`·`/make-asset`은 사용자가 입력한 적 없어도 에이전트가 판단해서 호출 가능. 호출 전에 한 줄로 사용자에게 알리되, 무응답 시 합리적 기본값으로 진행하고 나중에 되돌릴 수 있게 기록을 남긴다.

---

## Level 6 — Sub-agents (작업 분업)

`/task-start` ~ `/task-done` 파이프라인을 **리서치 ↔ 구현**으로 분리한 서브 에이전트. Agent 툴의 `subagent_type` 으로 호출한다.

| Sub-agent | 역할 | 호출 타이밍 |
|---|---|---|
| [agents/task-researcher.md](agents/task-researcher.md) | `/task-start` 착수 직후 CLAUDE.md·INDEX.md·RULES.md 스캔 + grep/LSP 로 증거 수집 → **구현 설계서** 산출. 코드 수정 금지. | 모든 작업 착수 시 선행 |
| [agents/task-engineer.md](agents/task-engineer.md) | 리서처의 설계서를 **임의 판단 없이** 그대로 실행. 범위 밖 변경 금지. 위반 신호 시 즉시 멈춤. | 설계서 완성 후 구현·검증 |

설계서가 불완전하면 엔지니어를 부르지 말고 리서처로 되돌린다. 엔지니어는 스스로 설계서를 재해석하지 않는다.

---

## 인덱스 갱신 규칙

- 새 지식/규칙/스킬/도메인 파일을 추가하면 **이 인덱스도 함께 갱신**한다.
- `keywords`는 작업 프롬프트에 실제로 나올 단어를 고른다. 너무 일반적(`game`, `code`)이면 매칭 품질이 떨어져 결국 전부 로드하게 된다.
- 각 항목 설명은 한 줄. 상세는 링크로.
- 이 파일은 짧게 유지한다. 200줄 넘어가면 섹션 분리를 고려한다.
