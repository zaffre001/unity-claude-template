# 6. 탐색적 테스트 사이클 — Learn → Design → Execute

> Exploratory Testing has three main components: Learning, Designing, Executing. (...) tests are designed frequently and freely, making designing a crucial skill for an exploratory tester. (...) These kinds of tests can't be executed via automation. While a script could check that tokens or inventory items are purchasable, it can't emulate a human doing battle!
> — Global App Testing, Ch 6

## Do (3단계 사이클)
1. **Learn** — 시스템 동작·기획 의도·상호작용을 먼저 이해. 테스트 전에 `LSP.documentSymbol` + `Read` 로 코드 전수. `.claude/domain/` 도메인 지식도 훑는다.
2. **Design** — 이해한 상태에서 **스크립트로 못 잡을 emergent state** 에 집중해 케이스를 즉석 설계. 예: 콤보 × 버프 × 장애물 동시 상호작용, 프레임 드랍 중 입력, 인벤토리 오버플로.
3. **Execute** — 설계 즉시 Bridge 로 실행. 결과가 다음 학습 입력으로 피드백되는 순환.

**Session-based 구조화 (Ch 8):** 세션당 **charter** (예: "인벤토리 오버플로 경로를 찾는다") 를 먼저 적어 범위 고정 → 그 범위 안에서 자유롭게. 탐색의 창의성과 커버리지의 균형.

## Don't
- 자동 회귀(§5) 만으로 종결하지 않는다. "script can check tokens are purchasable, but can't emulate a human doing battle" — Ch 6.
- 탐색 없이 스크립트 테스트만 믿지 않는다 — 스크립트 외부 버그는 영원히 못 찾는다.
- 탐색을 무제한으로 펼치지 않는다 — 목적(charter) 없는 탐색은 버그가 아니라 의문만 쌓인다.

## Unity / Agent — 게임 맥락
- 전투·퍼즐·물리 같은 **동적 상호작용 시스템** 은 탐색 1순위. Bridge `EnterPlaymode` + `Component.SetField` 로 비정상 조합 상태 만들어 던진다.
- `Agent` 서브에이전트(`Explore` / `general-purpose`) 에 "이 시스템이 비정상 상태로 갈 수 있는 경로를 찾아줘" 위임 — 에이전트 기반 탐색은 독립 맥락이라 사람 탐색자의 창의성을 일부 대체.
- 탐색 결과 중 재현되는 것은 즉시 회귀(§5) 로 승격 — 같은 버그가 다시 나는 걸 방지.
