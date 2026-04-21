# 9. 막히면 관점을 바꿔라 — ACI / 코드 다시 읽기

> the best way to learn something is to teach it. (...) illustrating the problem to him forces the programmer to rethink his assumptions and explain what it is really happening inside his code.
> — P. Adragna, §2.6 (ACI = *Automobile Club d'Italia* 기법, 일명 러버덕)

> Understanding what a program does without actually running it is a valuable skill that a programmer must develop.
> — P. Adragna, §2.7

## Do
- 30분 이상 진전이 없으면 **설명 모드로 전환**: 사용자에게 `[알고 있는 사실 / 시도한 가설 / 남은 가정]` 을 정리해 보고. 정리 과정에서 스스로 답이 나오는 경우가 많다.
- **정적 통독 모드**: 디버거·Bridge 끄고 관련 파일 전체를 처음부터 끝까지 읽고 어노테이션. 실행 없이 프로그램이 뭘 하는지 이해하는 훈련.
- 독립 시야 필요 시 `Agent` 서브에이전트(`general-purpose` / `Explore`) 에 원인 분석 위임 — 서브에이전트는 현재 대화 맥락을 공유하지 않으므로 독립 견해를 준다. 이것이 "자동화된 peer review" (Adragna §2.6 말미).

## Don't
- 같은 가설로 디버거 왕복 반복 금지. 5회 재현해도 같은 정보면 방법이 틀린 것.
- "조금만 더 해보면 보일 것 같다" 금지 — 가정 누수가 깊을수록 이 함정이 크다. 물러나서 가정을 다시 쓴다.
- 막혔다는 것을 사용자에게 숨기지 않는다. "지금까지 본 것 / 막힌 지점 / 다음 시도" 를 공유하면 사용자가 맥락 일부를 보충해 준다.

## 서브에이전트에게 위임할 때
- 프롬프트를 자립시켜라 — 서브에이전트는 현재 대화를 보지 않는다. 파일 경로·증상·시도한 가설·원하는 답 형태를 다 적는다.
- 독립 견해를 얻고 싶다면 당신의 잠정 결론을 **쓰지 않는다**. "다음 기준으로 원인을 찾아 줘" 가 아니라 "다음 증상의 원인을 조사해 줘".
