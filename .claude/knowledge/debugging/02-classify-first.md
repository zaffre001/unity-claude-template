# 2. 버그를 먼저 분류하라

> Syntactical < Build < Basic Semantic < Semantic. Bohrbug(결정적) vs Heisenbug(비결정).
> — P. Adragna, §1.2

## Do
- 난이도 사다리 (Syntactical → Build → Basic Semantic → Semantic) 에서 어디에 속하는지 먼저 판정. 쉬운 쪽부터 배제한다.
- **Bohrbug**: 재현 입력 고정 → 추적 용이. LSP 중심 정적 평가로 충분한 경우 많음.
- **Heisenbug**: 스레드·타이밍·초기화 순서·메모리 레이아웃 같은 환경 요인부터 의심. 재현 조건 자체를 다진 뒤 수정.
- 컴파일러 / IDE analyzer / LSP `documentSymbol` 이 잡을 수 있는 것은 런타임 올리기 전에 끝낸다.

## Don't
- 한 번 재현됐다고 Heisenbug 를 Bohrbug 로 단정하지 않는다. 최소 2회 같은 조건에서 일치해야 Bohrbug.
- 컴파일러가 잡을 수 있는 것(Syntactical·Build) 을 디버거로 추격하지 않는다 — 시간 낭비.
- "드물게 터지는데 괜찮겠지" 금지 — Heisenbug 는 릴리즈 이후 배로 아프다.

## Unity 맥락 — Heisenbug 단골 원인
- Domain Reload 비활성 환경의 **static 잔재**: Play 재진입 시 전 세션 값 남음 (RULE-01, `[RuntimeInitializeOnLoadMethod]` 누락).
- **FixedUpdate 외 물리 호출**: 프레임레이트 의존성 → 기기마다·실행마다 다른 결과 (RULE-04).
- **async 취소 누수**: Play 종료 후 살아남은 Task → 다음 Play 의 NRE 폭주 (RULE-05).
- **스크립트 실행 순서**: `Script Execution Order` 미지정 시 프레임마다 다른 순서.
