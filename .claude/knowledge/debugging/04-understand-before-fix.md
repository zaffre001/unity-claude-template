# 4. 완전히 이해한 뒤 고쳐라

> A bug should be fully understood before attempting to fix it. Trying to fix a bug before understanding it completely could end in provoking even more damage to the code, since the problem could change form and manifest itself somewhere else, maybe randomly.
> — P. Adragna, §1.3

## Do
- 수정 전에 **4종 세트**를 문서화: 원인 / 재현 조건 / 영향 범위 / 왜 지금까지 드러나지 않았는가.
- 메모리·상태 오염류는 오염된 데이터가 흐른 **모든 경로**를 확인한 뒤 수정. `LSP.findReferences` 로 전수.
- "단순 프로그래밍 오류" 인지 "알고리즘·설계 자체가 틀렸는지" 구분 — 후자면 수정이 아니라 재설계 보고.
- 수정 후 원래 재현 조건을 Bridge 로 재현 → 증상 사라짐을 증거로 남긴다.

## Don't
- 증상만 가리는 국소 패치 금지. 버그가 모습을 바꿔 다른 곳·다른 타이밍에 다시 나타난다.
- "일단 고치고 문제 생기면 롤백" 금지 — 반쯤 고친 상태가 원인을 은폐해 다음 디버깅을 2배 어렵게 만든다.
- NRE 를 `?.` 로 덮지 않는다. null 이 왜 흘러왔는지 모르면 null-safe 가 다음 증상을 만든다.
- `try-catch` 로 예외를 삼키지 않는다 — 원인이 사라지면 추적 경로도 사라진다.

## 재설계 판정 기준
- 같은 클래스에서 유사 버그가 2회 이상 반복 → 설계 결함 가능성.
- 수정안이 `if` 분기를 더하는 형태면 경계조건 폭발 신호.
- 버그 저널 (§5) 에 같은 가정이 반복 등장 → 추상화 경계가 잘못 그어졌다는 뜻.
