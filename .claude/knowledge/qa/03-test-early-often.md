# 3. Test Early, Test Often — 후기 QA 는 재앙

> too many companies treat localization as an afterthought, tacking it on to the end of the process when the product is almost ready. This is a mistake. (...) Suddenly, the text doesn't fit in buttons or boxes, and everything looks out of whack.
> — Global App Testing, Ch 4 & 5

## 인디 톤
이 원칙은 **"릴리즈 직전에 몰아서 하면 망한다"** 가 핵심. 매 커밋마다 자동 QA 돌리라는 뜻은 아님. 인디는 시간·자동화 인프라가 제한적이니 **"릴리즈 전/핵심 기능 변경 뒤/의심될 때"** 에 요청 시 실행하는 선에서 충분하다.

## Do
- **위험한 변경** 직후에는 바로 확인: 결제·저장·핵심 루프 같은 "깨지면 아픈" 것을 건드렸으면 같은 세션에서 `/qa` 로 스모크.
- **릴리즈 전** 에 최소 1회 전수 스캔 — 이때 처음 QA 하면 수정 비용이 N배.
- 기능 추가할 때 해당 조항의 테스트도 가능한 한 같은 커밋에 — 나중에 추가하려면 맥락 잊는다.

## Don't
- **UAT · 릴리즈 직전 단계에 처음 QA 를 시작하지 않는다.** 이 시점 버그는 기획·디자인까지 되돌려야 할 수도.
- "완성되면 테스트" 식으로 미루지 않는다. 불완전 기능도 부분 스모크는 가능.
- 릴리즈 막바지에 localization · performance 같은 Non-Functional 을 처음 돌리지 않는다 (§10 · §8).

## 강제하지 않는 것 (기본 동작 ❌)
- 매 `/task-done` 뒤 자동 QA 1패스 — **기본값 아님**. 사용자가 호출해야 돌아간다.
- 데일리 `/run bridge` 헤드리스 회귀 — CI 인프라 있는 팀 얘기. 인디는 필요할 때.
- 모든 PR 에 테스트 추가 — 여력 있을 때.

## Unity / Agent
- 스모크 1회 = Bridge `EnterPlaymode` → 핵심 필드 `Component.GetField` 1~2 건 확인. 보통 30초.
- Agile Ch 8 "Continuous feedback" 의 실천 형태 — 같은 세션 안에서 사용자에게 확인 내용 즉시 공유.
