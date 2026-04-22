# `/debug` 스킬 테스트 시나리오

`.claude/skills/debug/SKILL.md` + `.claude/knowledge/debugging/*` 의 툴 매트릭스·Adragna 10원칙이 실제 버그 앞에서 작동하는지 검증.

> **검증 기준 — 시나리오 Pass 조건**
> 1. 스킬이 지시한 1순위 툴(Grep / LSP / Bridge) 이 실행 가능해야 한다.
> 2. 툴 결과가 버그의 원인 후보로 해석 가능해야 한다 (원인 확정까지는 후속 단계).
> 3. 심어 둔 의도적 버그를 **전부** 검출해야 한다 (false negative 0).

---

## S1 — RULES.md 전수 준수 스캔 (실 템플릿 대상)

**목적:** debug.md §6 "Adragna §6 정적 분석" + 툴 매트릭스 "RULES.md 전수 스캔" 이 현재 프로젝트에서 실제로 의도한 결과를 내는가.

**대상:** 이 레포지토리 전체 (스켈레톤 상태 → 위반 0 예상).

**툴·쿼리:**
| 규칙 | 툴 | 쿼리 |
|---|---|---|
| RULE-01 | Grep | `InitializeOnLoad` in Assets/ |
| RULE-01 | Grep | `"autoReferenced": true` in Assets/ |
| RULE-02 | Bash | `git status --short` on 심링크 폴더 |
| RULE-03 | Bash | `git diff --stat "*.meta"` |
| RULE-04 | Grep | `AddForce\|MovePosition\|\.velocity\s*=` in Assets/ |
| RULE-05 | Grep | `async\s+(UniTask\|Task)\w*` in Assets/ |
| RULE-06 | Bash | `git diff --stat ProjectSettings/` |

**Pass:** 모든 쿼리 실행 성공. 결과가 각 RULE 위반 여부 판정 가능 형태.

---

## S2 — 샌드박스 버그 파일 정적 추적

**목적:** 여러 유형 버그가 혼재한 파일에 Grep + LSP 를 돌려 debug.md §1 툴 매트릭스가 후보를 짚어내는가.

**대상:** `/tmp/debug-sandbox/BuggyComponent.cs` (시나리오 실행 직전 생성, 종료 후 삭제).

**심은 버그 목록:**
| ID | 유형 | 스킬 매트릭스 줄 | 기대 검출 툴 |
|---|---|---|---|
| B1 | `[InitializeOnLoad]` 추가 (RULE-01) | §6 정적 분석 | Grep |
| B2 | `static` 필드 + `[RuntimeInitializeOnLoadMethod]` 누락 | 매트릭스 "도메인 리로드" | Grep |
| B3 | `Update()` 내 `AddForce` (RULE-04) | 매트릭스 "물리 버그" | Grep |
| B4 | `async UniTask DoAsync()` — CancellationToken 없음 (RULE-05) | §6 Adragna 8 | Grep |
| B5 | 이벤트 `+=` 만 있고 `-=` 없음 | 매트릭스 "이벤트 누수" | Grep |
| B6 | 초기화 누락 필드로 NRE 유발 | 매트릭스 "NRE" | LSP.documentSymbol |

**Pass:** 6개 버그 모두 Grep·LSP 중 최소 1개 툴이 정확히 라인을 짚는다 (false negative 0). 오탐(false positive) 은 허용 — debug 단계에서 후보 좁히기는 다음 스텝.

---

## S3 — 이진 탐색 개념 적용 (Adragna §10)

**목적:** binary split 10원칙이 긴 가설 목록에서 log₂ 안에 좁혀지는가 개념 증명.

**대상:** 10개 함수 가설 체인 `f01 → f02 → ... → f10`. 실제 버그는 `f07` 에 있다고 가정.

**절차:**
1. 중간(f05) 체크 → "f01-f05 정상 확인" 기대
2. 후반부(f06-f10) 중간(f08) 체크 → "여전히 정상 아님 = f06-f07 이 범인"
3. f07 체크 → 버그 확인
4. 반복 횟수 N = 3 (log₂(10) ≈ 3.32)

**Pass:** 3회 이하 분할 만에 1건으로 좁혀짐. 시뮬레이션 로그를 리포트에 포함.

---

## S4 — Bridge 상태 조회 (툴 가용성)

**목적:** `unity_bridge_status` 가 호출되는지, Editor 실행 상태·inbox/outbox 큐 현황이 반환되는지.

**대상:** 현재 세션의 `mcp__claude-bridge__unity_bridge_status` MCP 툴.

**Pass:** 호출 성공 + 응답 필드 (`project_root`, `inbox_pending`, `outbox_unread`, `editor_running`) 파싱 가능.

---

## S5 — Adragna §1 "가정 의심" 적용

**목적:** 스킬의 1원칙 "모든 가정을 코드로 검증" 을 실제 의사결정에 적용.

**대상:** "이 템플릿에는 `Assets/Scripts/` 하위에 .cs 파일이 없다" 라는 CLAUDE.md 의 서술.

**절차:** 믿지 않고 `Glob` 로 실제 확인. 결과와 가정이 일치하는지 보고.

**Pass:** 실제 파일 수를 수치로 보고. 가정과 다르면 즉시 고지 (isolation).

---

## 실행 결과 기록

각 시나리오 결과는 [`debug-skill-results.md`](debug-skill-results.md) 에 기록.
