---
name: qa
description: 기획·스펙·규칙에 비추어 구현을 평가하는 툴 사용 지침. 사용자에게 "플레이해 보고 문제 있나 말해 달라"를 시키지 않고, 에이전트가 LSP 로 소스를 읽고 Bridge 로 실행해 스스로 합격·불합격을 판정한다.
---

# Skill: /qa (Agent-driven QA)

**전제:** QA 의 책임은 에이전트에게 있다. 사용자에게 Play 후 감각으로 "이상하다" 보고 받지 않는다. 에이전트가 **합격 기준을 문서로 명시**하고 → LSP·Grep 으로 정적 평가 → Bridge 로 런타임 확인 → 기준별 Pass/Fail 보고한다.

이 문서는 **어떤 툴로 어떤 기준을 검증할지**만 다룬다. 작업 착수·종료 절차는 `/task-start`, `/task-done` 참고.

### 스케일 전제 — 인디 개발자

이 템플릿은 인디·소규모 팀용이다. QA 엔진이 하는 일은 **사용자가 명시적으로 요청했을 때** 돌아가야 한다. 다음은 **기본 동작 아님**:

- ❌ 매 `/task-done` 뒤 자동 QA 1패스 — 느리고 과하다. 해당 변경이 위험할 때만 `/qa` 호출.
- ❌ 데일리 자동 회귀 `/run bridge` — CI 인프라 가진 팀에나 맞다.
- ❌ 기획 조항 N 개면 N 개 전수 평가 — 중요 조항 위주, 덜 중요한 건 스킵 OK.
- ❌ 자동 Unit 테스트 스위트 유지 강제 — Unity Test Framework 셋업 비용 고려.

**대신 기본 동작:** 사용자가 "QA 해 줘" 또는 "릴리즈 전 검증" 을 요청하면 그때 실행. 이하 10 원칙(§7) 은 **도구 사용법 가이드**이지 **체크리스트 강제** 가 아니다.

---

## 1. 합격 기준의 출처

검증하기 전에 기준을 **문서 링크**로 박는다. 기준 없는 QA 는 감상문이다.

| 출처 | 성격 | 언제 쓰나 |
|---|---|---|
| [`../../RULES.md`](../../RULES.md) | 불변 제약 (6개) | 릴리즈 전 / 의심될 때 스캔 (매번 강제 아님) |
| [`../knowledge/RULES.md`](../knowledge/RULES.md) | Zimmerman 21 범용 코딩 규약 | 코드 품질 QA |
| [`../rules/scripts.md`](../rules/scripts.md) | `.cs` 경로 규칙 | `.cs` 신규·수정 QA |
| [`../rules/asmdef.md`](../rules/asmdef.md) | `.asmdef` 규칙 | 어셈블리 변경 QA |
| [`../knowledge/unity-mobile-performance.md`](../knowledge/unity-mobile-performance.md) | 성능 규약 | 프레임·메모리 QA |
| [`../knowledge/csharp-dotnet.md`](../knowledge/csharp-dotnet.md) | C# 스펙 규약 | 타입·async·이벤트 QA |
| `.claude/domain/*.md` / 기획서 / PR 설명 | 프로젝트 고유 행동 규약 | 기능 QA |

기준이 명시 안 되어 있으면 QA 하지 않는다. 먼저 사용자에게 기준을 묻거나 `/design` 으로 스펙 추출.

---

## 2. 툴 매트릭스 — 기준 유형별 검증 툴

| 기준 유형 | 1순위 (정적) | 2순위 (런타임) | 증거 형태 |
|---|---|---|---|
| **RULE-01** Domain Reload 미트리거 | `Grep '\[InitializeOnLoad\]'` + `Grep '"autoReferenced": true'` (대괄호로 `RuntimeInitializeOnLoadMethod` 오탐 차단) | Bridge `EnterPlaymode` 2회 재진입 + `Component.GetField` 로 static 값 비교 | grep 매치 0 건 + 재진입 후 초기값 일치 |
| **RULE-02** 심링크 폴더 파일 신규 금지 | `Bash git status` 로 `Assets/Art/` 등 변경 여부 | — | `git status` 출력 |
| **RULE-03** `.meta` 직접 편집 금지 | `Bash git diff --stat "*.meta"` | — | diff 라인 0 |
| **RULE-04** 물리 = FixedUpdate | `Grep` 으로 `Update\|LateUpdate` 내 `AddForce/MovePosition` 스캔 + `LSP.incomingCalls` 로 호출자 맥락 | Bridge `EnterPlaymode` + 프레임레이트 흔들어 거동 확인 | 호출자 모두 `FixedUpdate` 계열임을 LSP 로 증명 |
| **RULE-05** async 에 `CancellationToken` 전달 | `Grep "async\s+\w+\s+\w+\("` 후보 → `LSP.hover` 로 시그니처 확인 | — | 모든 async 메서드에 `CancellationToken` 인자 존재 |
| **RULE-06** ProjectSettings 미수정 | `Bash git diff --stat ProjectSettings/` | — | diff 라인 0 |
| 이벤트 구독·해제 쌍 (`rules/scripts.md`) | `Grep "\+=\s*(?:this\.)?\w+"` + 같은 파일에서 `-=` 매칭 | — | `+=` 라인 수 ≤ `-=` 라인 수, OnDestroy/OnDisable 에 해제 존재 |
| `GetComponent` 캐싱 (`rules/scripts.md`) | `Grep "Update\|FixedUpdate" -A 20` 에서 `GetComponent` 동거 여부 | Bridge Profiler hook (선택) | `Update()` 내 호출 0 건 |
| 네임스페이스 `Project.{어셈블리}` | `Grep "^namespace "` + 파일 경로 매칭 | — | 규약 불일치 파일 0 |
| Zimmerman R1-R21 (범용 품질) | `LSP.documentSymbol` 로 복잡도·중복·사용되지 않는 심볼 식별 | — | 위반 심볼 목록 + 해당 규칙 번호 |
| 기능 행동 (기획서 조항) | `LSP.findReferences` / `outgoingCalls` 로 조항 구현 지점 확인 | Bridge 로 해당 상태 세팅 → 값 비교 | 조항별 Pass/Fail + 측정값 |
| 성능 예산 (프레임·할당) | `Grep` 으로 hot path 의 `new`/LINQ/`GetComponent` | Bridge `EnterPlaymode` + Reflection 으로 Profiler API 샘플링 (선택) | 할당 바이트·프레임 시간 측정값 |

---

## 3. 정적 평가 — LSP / Grep / Read

QA 의 대부분은 정적 평가로 해결된다. 런타임 필요 최소화.

### 커버리지 전수 확인

기획서 조항 하나하나가 **실제로 구현됐는지**를 LSP 로 증명:

```
기획서 "콤보 3 이상이면 보너스 5초"
  → LSP.workspaceSymbol "combo"         (심볼 발견)
  → LSP.findReferences                   (모든 사용처)
  → LSP.outgoingCalls 각 지점             (어떤 경로로 보너스 적용)
  → Read 해당 라인                        (>=3 조건 + 5초 증분 확인)
```

조항마다 증거 라인 (파일:라인) 을 보고에 남긴다.

### 규칙 전수 스캔 스니펫

```
# 이벤트 누수
Grep "\+=\s*[A-Za-z_]\w*"  --type cs   # 구독 지점
Grep "-=\s*[A-Za-z_]\w*"   --type cs   # 해제 지점
# → 같은 파일에서 쌍 확인

# Update 에서 물리 호출 (RULE-04 위반)
Grep "void Update"       --type cs -A 30 | Grep "AddForce\|MovePosition"

# async w/o CancellationToken (RULE-05 위반)
Grep "async\s+(?:UniTask|Task)\w*\s+\w+\s*\([^)]*\)" --type cs
  # → 매치마다 LSP.hover 로 CancellationToken 파라미터 유무 확인

# static 초기화 누락 (RULE-01 리스크)
Grep "private\s+static\s+\w+\s+\w+" --type cs   # 필드 목록
Grep "RuntimeInitializeOnLoadMethod"  --type cs  # 초기화 지점
# → 필드 대비 초기화 커버리지
```

### 중복·데드코드 (Zimmerman 위반)

```
LSP.documentSymbol            # 파일 내 심볼 전수
LSP.findReferences 각 심볼    # 참조 0 이면 죽은 코드 후보
```

---

## 4. 런타임 QA — Bridge 중심

정적으로 못 보는 것 (실제 값 전이·Play 모드 스테이트 머신·도메인 리로드 거동) 만 Bridge 로 돌린다.

### 스모크 시나리오 템플릿

```
1) unity_bridge_status              # 상태 확인, editor_running 체크
2) Scene.Open   {경로}              # 검증 씬 로드
3) Component.SetField ...           # 초기 조건 세팅 (격리)
4) EnterPlaymode                    # 진입
5) get_isPlaying 1~2s 폴링 (최대 20s) # 실제 진입 확인
6) Component.GetField ... (n 회)    # 기대값 비교
7) ExitPlaymode
8) (RULE-01 검증이면) EnterPlaymode 재진입 후 6) 반복 — 값이 초기값과 일치해야 통과
```

- `unity_call` 단건이면 GUI 상주 모드. 10+ op 이면 inbox 에 쌓고 `/run bridge` 헤드리스.
- Editor 가 같은 프로젝트로 이미 떠 있으면 헤드리스 배치는 락 충돌 — 반드시 `unity_bridge_status` 로 선확인.
- 실패 케이스 재현은 최소 씬(`Scene.New` 또는 전용 QA 씬)에서. 복잡 씬에서 섞지 않는다.

### 필드 기반 어서션 패턴

런타임 unit test 대신 `Component.GetField` 스냅샷 비교:

```
Component.GetField path="/GameRoot" type="Project.Core.ScoreSystem" field="_combo"     → 기대 3
Component.GetField path="/GameRoot" type="Project.Core.ScoreSystem" field="_bonusSec"  → 기대 5
```

스냅샷이 기대와 다르면 Fail. 값 자체를 리포트에 포함.

### Reflection.Invoke 로 상태 덤프

전용 op 없는 상태 확인은 `Reflection.Invoke` 로 임의 메서드 호출. 예: 정적 `Dump()` 를 만들어 놨다면 그것 호출해 JSON 받기.

---

## 5. 리포트 형식

QA 결과는 **기준별 Pass/Fail + 증거 경로** 로 보고한다. 줄글 감상 금지.

```
기준 1: RULE-01 Domain Reload 미트리거
  - [Pass] grep '\[InitializeOnLoad\]' 매치 0 건
  - [Pass] grep '"autoReferenced": true' 매치 0 건
  - [Pass] EnterPlaymode 2회 재진입: _coinCount 초기값 0 유지

기준 2: 기획 §2.1 "콤보 3 이상이면 +5초"
  - [Pass] 구현: Assets/Scripts/_Core/ScoreSystem.cs:47
          (combo >= 3 → _timeRemaining += 5f)
  - [Pass] 런타임: SetField combo=3 후 GetField _bonusSec == 5

기준 3: RULE-05 async + CancellationToken
  - [Fail] Assets/Scripts/_UI/MenuFader.cs:33  FadeOutAsync() — 인자 없음
  - 제안 수정: FadeOutAsync(CancellationToken ct) 로 시그니처 변경

요약: 8 기준 중 7 통과, 1 실패 (RULE-05 위반 1건). 실패는 수정 후 재검증 필요.
```

Fail 은 항상 **파일:라인 + 제안 수정**까지. 단순 "문제 있음" 금지.

---

## 6. 금지 사항 / 가이드라인

### 강한 금지 (반드시 지킨다)
- **사용자에게 "플레이해 보고 이상하면 말씀해 주세요" 금지.** QA 는 에이전트가 판정한다. 사용자의 눈은 최종 샘플링이지 검증 1차 라인이 아니다.
- **컴퓨터 유즈(`mcp__computer-use__*`) 금지.** CLAUDE.md §8. Bridge 로 못 보는 거면 op 추가·사용자에게 한 장면 캡처 요청으로 — 좌표 자동화로 우회 금지.
- **기준 없이 QA 시작 금지.** 스펙 없으면 먼저 사용자에게 묻거나 `/design` 으로 추출.
- **Pass 에 증거 없음 금지.** 각 기준마다 파일:라인 또는 런타임 측정값을 붙인다.

### 약한 가이드 (인디 상황에 따라 조정)
- **Bridge 왕복을 줄인다.** 정적(LSP·Grep) 으로 끝낼 수 있으면 런타임까지 가지 않는다. 이건 효율 가이드지 금지 아님.
- **핵심 조항 위주로.** 기획 조항 100개 전수 평가 강요 안 함. 핵심 루프·결제·저장처럼 **실패 시 아픈 것** 을 우선. 나머지는 시간 생기면.
- **실패 1건 나왔을 때의 행동은 상황 따라.** 블로커성 실패면 즉시 보고하고 나머지는 그 수정 뒤로. 독립 실패면 이어서 전체 훑기. 규칙으로 못 박지 말 것.
- **재검증 주기는 변경 위험도 기준.** 큰 리팩터 뒤엔 재돌. 문구 수정 뒤엔 스킵 OK.

---

## 7. Global App Testing 10 원칙 (Do/Don't)

소스: *Global App Testing, The Ultimate QA Testing Handbook*. [원문 PDF](https://540930.fs1.hubspotusercontent-na1.net/hubfs/540930/Global%20App%20Testing%20-%20The%20Ultimate%20QA%20Testing%20Handbook.pdf)

핸드북 9장 중 게임 테스트에 유효하지 않은 챕터는 **폐기**. 남은 10개 규칙을 per-file 로. 이 섹션은 인덱스만.

**폐기 근거:**
- Ch 3 **Crowdtesting** — 외부 인간 테스터 고용 모델. 에이전트 주도 QA 와 상반, 출판사 커머셜 피치. 게임 테스트 가치 0.
- Ch 9 **Usability Testing** — 실 유저 주관적 피드백 기반. 에이전트 대체 불가 (§6 탐색적 테스트에서 부분 커버).
- Ch 8 Agile 원칙 중 **face-to-face / focus on people / enjoy** — 인간 팀 사회적 역동, 에이전트 QA 와 무관.
- Ch 1 "**outsource to QA engineers**" 권고 — 에이전트 주도와 상반.

### 채택 10 규칙

| # | 원칙 | 출처 | 언제 펼칠 것인가 | 파일 |
|---|---|---|---|---|
| 1 | 좁은 포커스 + 측정 가능한 기대값 | Ch 1 | 테스트 케이스 초안 작성 직전 | [01-narrow-test-cases.md](../knowledge/qa/01-narrow-test-cases.md) |
| 2 | 테스트 환경은 개발 환경과 분리 | Ch 1 | Play Mode 만 확인하고 완료 보고하려 할 때 | [02-separate-environments.md](../knowledge/qa/02-separate-environments.md) |
| 3 | Test Early, Test Often | Ch 4·5·8 | 기능 완성 직후·매 `/task-done` | [03-test-early-often.md](../knowledge/qa/03-test-early-often.md) |
| 4 | Requirements Traceability Matrix | Ch 4 | 기획 조항 받은 직후 | [04-traceability-matrix.md](../knowledge/qa/04-traceability-matrix.md) |
| 5 | 회귀 스크립트 자체부터 의심 | Ch 2 | 회귀 실패 보고 받은 직후 | [05-regression-script-skepticism.md](../knowledge/qa/05-regression-script-skepticism.md) |
| 6 | 탐색적 테스트 사이클 (Learn→Design→Execute) | Ch 6 | 자동 회귀로 못 잡은 게임 버그 의심 시 | [06-exploratory-cycle.md](../knowledge/qa/06-exploratory-cycle.md) |
| 7 | Unit → Integration → System 순서 엄수 | Ch 1·2·7 | E2E 바로 돌리고 싶어질 때 | [07-testing-pyramid-order.md](../knowledge/qa/07-testing-pyramid-order.md) |
| 8 | Functional vs Non-Functional 분리 | Ch 4 | 성능·로드·신뢰성 평가가 필요할 때 | [08-functional-vs-nonfunctional.md](../knowledge/qa/08-functional-vs-nonfunctional.md) |
| 9 | 버그 리포트: 재현 + 증거 + severity | Ch 7 | QA 결과 보고 작성 시 | [09-bug-report-quality.md](../knowledge/qa/09-bug-report-quality.md) |
| 10 | Localization: 초기부터 — 후기 = 재앙 | Ch 5 | 다국어 출시 계획 있는 순간부터 | [10-localization-early.md](../knowledge/qa/10-localization-early.md) |

> **원저 요지:** 소프트웨어 품질은 테스트 자체가 아니라 **테스트로 얻은 데이터를 어떻게 쓰느냐** 에서 나온다. 모든 QA 전략은 제품과 수명주기에 맞춰 유일해야 한다. 이 10 원칙은 게임이라는 제품 특성에 맞춰 필터된 최소 집합.
