# `/debug` 스킬 테스트 결과 리포트

실행일: 2026-04-21
시나리오 원본: [debug-skill-scenarios.md](debug-skill-scenarios.md)

---

## 요약

| # | 시나리오 | 결과 | 비고 |
|---|---|---|---|
| S1 | RULES.md 전수 준수 스캔 | **Pass (부분 플래그)** | RULE-06 잠재 위반 1건 노출 — 사용자 확인 필요 |
| S2 | 샌드박스 버그 정적 추적 | **Pass** | 6/6 검출 (Grep 으로 전부 커버) |
| S3 | 이진 탐색 개념 적용 | **Pass** | 3회 분할로 1건으로 수렴 (log₂(10) 기대치 내) |
| S4 | Bridge 상태 조회 | **Pass** | MCP 정상, Editor 미실행 상태 확인 |
| S5 | Adragna §1 가정 의심 | **Pass — 가정이 실제로 틀림** | CLAUDE.md 기술과 리얼리티 불일치 검출 |

**전체 Pass.** 검출 커버리지 100%. 단 스킬 문서 보정 포인트 2건 + 프로젝트 상태 보정 포인트 2건 발견 (후술).

---

## S1 — RULES.md 전수 준수 스캔

| RULE | 쿼리 | 결과 | 해석 |
|---|---|---|---|
| RULE-01 `InitializeOnLoad` | Grep Assets/ | 2건 hit | **False positive** — `Assets/Editor/ParallelAgentSetup.cs:4`, `Assets/Editor/ClaudeBridge/ClaudeBridgeServer.cs:16`. RULES.md 는 "**신규** 추가" 금지 — 둘 다 템플릿 의도. 한 건(`Game2048Bootstrap.cs:13`) 은 `RuntimeInitializeOnLoadMethod` 로 `InitializeOnLoad` 와 다른 어트리뷰트 (정규식 부분일치 탓). **스킬이 후보를 짚고 맥락으로 판정하는 흐름이 정상 작동**. |
| RULE-01 `autoReferenced: true` | Grep Assets/ | 0건 | 준수 |
| RULE-02 심링크 폴더 변경 | `git status --short` | 심링크 폴더 전부 clean | 준수 |
| RULE-03 `.meta` 직접 편집 | `git diff --stat '*.meta'` | 0 라인 | 준수 |
| RULE-04 Update 물리 호출 | Grep `AddForce\|MovePosition\|velocity=` | 0건 | 준수 |
| RULE-05 async + CT | Grep `async (UniTask\|Task)` | 0건 (현재 Assets/ 내 async 메서드 자체 0건) | 준수 |
| RULE-06 ProjectSettings 수정 | `git status` | **`ProjectSettings/PackageManagerSettings.asset` untracked** | ⚠ 사용자 확인 필요 — RULE-06 "ProjectSettings 변경은 인간 아키텍트만" 에 해당 가능 |

**툴 작동 판정:** ✅ 전부 실행 성공 + 결과 해석 가능.
**추가 발견(프로젝트 상태):** `PackageManagerSettings.asset` 이 git 바깥에 있음. 템플릿 의도인지 사용자 확인 요망.

---

## S2 — 샌드박스 버그 정적 추적

**대상:** `/tmp/debug-sandbox/BuggyComponent.cs`

| ID | 버그 | 검출 툴·쿼리 | 라인 | 검출 여부 |
|---|---|---|---|---|
| B1 | `[InitializeOnLoad]` 추가 | Grep `InitializeOnLoad` | 11 | ✅ |
| B2 | `static` 필드 + 초기화 누락 | Grep `static int\|static BuggyComponent` + Grep `RuntimeInitializeOnLoadMethod` (**부재 확인**) | 20, 21 | ✅ |
| B3 | `Update()` 내 `AddForce` | Grep `AddForce`, 파일 읽기로 Update 인지 확인 | 41 | ✅ |
| B4 | `async` w/o `CancellationToken` | Grep `async (UniTask\|Task)` + 시그니처 인자 리스트 읽기 | 46, 52 | ✅ |
| B5 | `+=` 구독 있고 `-=` 해제 없음 | Grep `\+=\s*\w+` vs `-=\s*\w+` 비교 (-= 는 주석 1건뿐) | 35 (vs 34 주석) | ✅ |
| B6 | NRE 유발 (필드 초기화 누락) | Grep `_rb\s*=` (실제 대입 0건, 주석 1건만) | 32(주석) | ✅ |

**검출 커버리지:** 6/6 (100%).
**False positive:** 0건.
**LSP 가용성:** ❌ `.cs` 에 LSP 서버 연결 안 됨 (`No LSP server available for file type: .cs`). 환경 한계 — Grep 이 전체 대체 커버.

---

## S3 — 이진 탐색 개념 적용

**대상:** 10개 함수 체인 `f01 → f02 → ... → f10`, 실 버그는 `f07`.

| 라운드 | 범위 | 중간 | 체크 지점 | 결과 | 다음 범위 |
|---|---|---|---|---|---|
| 1 | [f01–f10] | f05 | f05 출구 기대값 | 정상 | [f06–f10] |
| 2 | [f06–f10] | f08 | f08 진입 기대값 | **비정상** | [f06–f07] |
| 3 | [f06–f07] | f06 | f06 출구 기대값 | 정상 | {f07} |

**반복 횟수:** 3 (log₂(10) ≈ 3.32 이하 → Pass).
**의미:** Adragna §10 "1000줄 → 10 스텝" 법칙이 소규모(10단계)에서도 예상대로 작동.

---

## S4 — Bridge 상태 조회

**툴:** `mcp__claude-bridge__unity_bridge_status`
**응답:**
```json
{
  "project_root": "/Users/zaffre/Documents/unity/unity-claude-template",
  "inbox_pending": 0,
  "outbox_unread": 0,
  "editor_running": false
}
```
**해석:** MCP 가용. Editor 미실행 → GUI 모드 불가, 런타임 검증 필요하면 `/run editor` 또는 `/run bridge` 헤드리스 선행.

---

## S5 — Adragna §1 "가정 의심" 적용 — **가정이 틀림**

**가정:** CLAUDE.md §0 "**`Assets/Scripts/` 하위는 비어 있다**. 각 어셈블리 폴더에는 `.asmdef` 만 있고 소스 코드는 없다."

**툴 검증:** `Glob Assets/Scripts/**/*.cs`

**결과:**
```
Assets/Scripts/_Core/Game2048/Direction.cs
Assets/Scripts/_Core/Game2048/Board.cs
Assets/Scripts/_UI/Game2048/TileColors.cs
Assets/Scripts/_UI/Game2048/TileView.cs
Assets/Scripts/_UI/Game2048/BoardView.cs
Assets/Scripts/_UI/Game2048/GameController.cs
Assets/Scripts/_UI/Game2048/Game2048Bootstrap.cs
```

**판정:** 가정이 **틀렸다**. Game2048 샘플 코드 7개 `.cs` + 2 meta 파일이 untracked 로 존재 (`git status` 에서도 확인됨).

**함의:**
1. CLAUDE.md 의 "빈 스켈레톤" 기술이 **stale** — 현재 이 작업공간에는 Game2048 샘플이 얹혀 있다.
2. Adragna §1 원칙의 실효성 증명 — "CLAUDE.md 에 써 있으니까 빈 폴더겠지" 로 넘어갔다면 이 사실을 못 봤을 것.
3. `/task-start` 착수 시에도 이 갭이 드러났어야 — Grep 이 기본 점검이어야 한다는 근거 강화.

---

## 스킬 문서 보정 포인트

### 1) LSP 가용성 경고 추가
`debug.md` §2 "정적 평가 — LSP 중심" 이 Roslyn LSP 연결을 전제하지만, 기본 Claude Code 환경에 LSP 서버가 항상 있는 건 아님. Grep 폴백 경로를 1순위로 조정하거나 "LSP 없을 때 Grep 전수 대체" 를 명시하는 문장 추가 권장.

### 2) 정규식 오탐 주의
`Grep InitializeOnLoad` 는 `RuntimeInitializeOnLoadMethod` 도 잡는다. debug.md 의 예시 쿼리를 `\[InitializeOnLoad\]` 식으로 어트리뷰트 대괄호까지 포함해 명시하면 false positive 가 줄어듦.

---

## 프로젝트 상태 보정 포인트 (사용자 확인 요망)

### A) CLAUDE.md 기술 갱신
§0 "`Assets/Scripts/` 하위는 비어 있다" 는 현재 사실과 다름. Game2048 샘플이 있음. CLAUDE.md 를 현 상태에 맞춰 갱신하거나, Game2048 를 정리하거나, 둘 중 하나 필요.

### B) `ProjectSettings/PackageManagerSettings.asset` untracked
RULE-06 "ProjectSettings 변경은 인간 아키텍트만" 에 해당할 수 있음. 의도한 것이면 커밋, 아니면 원복.

---

## 샌드박스 정리

`/tmp/debug-sandbox/` 는 OS 재시작 시 자동 소멸. 즉시 제거하려면 `rm -rf /tmp/debug-sandbox`.
