# 1. 가정을 먼저 의심하라

> "All bugs stem from one basic premise: something thought to be right, was in fact wrong."
> — P. Adragna, §1.1

## Do
- 코드를 열기 전에 "내가 옳다고 믿고 있는 것"의 목록을 명시화한다.
- 각 가정을 `LSP.hover` / `findReferences` / Bridge `Component.GetField` 로 반증 가능한 형태로 검증한다.
- 범위에서 빼기 전에 **그 가정을 부정했을 때 어떤 증거가 나와야 하는지**를 먼저 적는다 (가설-반증 기록).

## Don't
- "여긴 문제없어 보인다"를 근거로 범위에서 제외하지 않는다.
- 직관으로 원인을 좁히지 않는다 — 추측은 가정을 하나 더 쌓는다.
- 스택트레이스가 가리키는 파일만 의심하지 않는다 — 가정 위반은 호출 사이에서 발생한다.

## Unity 맥락
- 특히 `static` 필드 값·이벤트 구독 상태·Play Mode 재진입 직후 상태는 "당연히 그럴 것"이 깨지기 쉬운 1순위 지점 (RULE-01).
- `async` 가 완료됐다는 가정·`CancellationToken` 이 전파됐다는 가정은 RULE-05 기준 항상 검증.
