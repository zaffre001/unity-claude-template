# 7. Unit → Integration → System — 순서는 있지만 전부 해야 하는 건 아니다

> Unit tests must always be executed by the developer and should be built into the code itself. (...) run unit tests in parallel to save time, but only move onto integration tests once you've ensured individual components work as they should.
> — Global App Testing, Ch 1 · 2 · 7

## 인디 톤
이 원칙의 핵심은 **"하위 단계 실패 상태에서 상위 단계 돌리지 말라"** 이다. Unity Test Framework 셋업해서 Unit → Integration → System 3계층 전부 유지하라는 뜻 아님. 인디는 대부분 **Bridge 로 돌리는 시스템 스모크** 만으로 시작해도 괜찮다.

## Do (여력 순서)
1. **최소한: 시스템 스모크 (E2E)** — Bridge `EnterPlaymode` + 핵심 필드 스냅샷. 이거 하나만 있어도 "게임이 돌아가는가" 는 증명 가능.
2. **여력 있으면: Integration** — 서브시스템 조합 (Inventory + Economy). 버그 재현 씬으로 격리.
3. **진짜 여력 있으면: Unit** — Unity Test Framework EditMode (순수 C# 로직). 셋업 시간 투자 필요.

**순서 규칙** (이건 지킨다):
- Unit 깨진 상태에서 E2E 돌리면 디버깅 불가 → 하위 실패는 상위 중단 사유.
- Unit / Integration 이 **있다면** 통과가 E2E 선행 조건. **없다면** 이 규칙은 해당 없음.

## Don't
- Unit 실패를 무시하고 "어차피 E2E 에서 다 볼 것" 으로 스킵 금지 — 있다면 지킨다.
- 역피라미드 (Unit 없고 System 수동 검증만) 을 **장기 기본값** 으로 삼지 않는다. 인디 초기엔 OK, 팀·제품 커지면 Unit 부터 쌓는다.

## 강제하지 않는 것 (기본 동작 ❌)
- Unity Test Framework 셋업 강제 — 인디 시간 투자 비용 큼. 게임 장르 (결정적 로직 많은 퍼즐·카드류) 에서만 ROI 좋음.
- Unit 테스트 커버리지 수치 (예: 80%) — 무의미한 목표.

## Unity / Agent
- `Project.Tests.EditMode` / `Project.Tests.PlayMode` 폴더(CLAUDE.md §2) 는 **준비돼 있지만 빈 상태**. 쓸지 말지 사용자 결정에 맡긴다.
- Bridge 스모크 기반 System 테스트가 이 템플릿의 기본 경로. Unit 은 optional.
- 실패 단계 요약 (있을 때만): `U ok · I fail at X · S skipped` 식. Unit 안 쓰면 그냥 `S fail at X`.
