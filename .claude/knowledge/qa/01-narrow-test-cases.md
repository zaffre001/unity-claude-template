# 1. 테스트 케이스는 좁은 포커스 + 측정 가능한 기대값

> Write each test case with a narrow focus, but remember to retain cohesion across your entire test case suite. (...) Each test should be based on clear expectations and result in a measurable outcome.
> — Global App Testing, *The Ultimate QA Testing Handbook*, Ch 1

## Do
- 한 테스트는 **한 컴포넌트·한 기획 조항만** 검증. 여러 시스템을 섞지 않는다.
- 기대값은 **숫자·enum·boolean** 수준으로 측정 가능하게. 0.5초 이내 / HP == 0 / IsGrounded == true 식.
- 스위트 전체는 중복 없이 응집되게 구성 — 한 케이스 실패가 어떤 조항을 깬 건지 즉시 추적 가능해야.

## Don't
- "로그인 → 인벤토리 → 상점 → 전투" 같은 멀티 시스템 시나리오를 단일 테스트로 만들지 않는다. 어디서 깨졌는지 추적 불가.
- "잘 작동" / "자연스럽다" / "괜찮다" 식 종료 조건 금지 — 측정 불가는 Pass/Fail 판정이 안 된다.
- 여러 조항을 한 테스트로 번들링하지 않는다. 조항 A 가 실패하면 B·C 는 영원히 "미확인" 상태.

## Unity / Agent — 게임 맥락
- Bridge `Component.GetField` 로 필드값 스냅샷 → 기대값 비교가 자연스러운 측정 단위.
- 기획 조항당 테스트 1:1 매핑 — §4 RTM 연계. 조항 ID 를 테스트 이름에 넣는다 (예: `Test_GameSpec_2_3_ComboBonus`).
- 탐색적(§6) 과 구분: 탐색적은 케이스 자체를 즉석 설계, 여기 §1 은 공식 회귀 케이스.
