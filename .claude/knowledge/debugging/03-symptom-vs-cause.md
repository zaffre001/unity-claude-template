# 3. 증상과 원인을 혼동하지 마라

> do not confuse observing symptoms with finding the real source of the problem;
> check if similar mistakes (especially wrong assumptions) were made elsewhere in the code.
> — P. Adragna, §1.3

## Do
- 스택트레이스·에러 메시지는 **증상 위치**로만 다룬다. `LSP.findReferences` + `incomingCalls` 로 원인 후보지점을 역추적.
- 같은 가정이 다른 파일·다른 시스템에도 있는지 `Grep` 으로 전수 확인 — 원인이 1곳이면 증상은 N곳에 흩어진다.
- 수정 범위를 선언할 때 **"같은 실수가 발생 가능한 대상 전부"** 를 명시 (예: `_cachedTransform` 누수 패턴이 파일 3개에 존재).

## Don't
- NRE 가 뜬 라인을 "버그 라인" 으로 확정하지 않는다. NRE 는 대입·초기화 누락의 멀리 떨어진 증상인 경우가 대부분.
- 한 곳만 고치고 종결하지 않는다 — 같은 잘못된 가정은 거의 항상 다른 곳에도 있다.
- "다른 곳도 고치면 스코프 초과" 로 미루지 않는다. 스코프를 넓혀 보고하고 승인받는다.

## 추적 예시
```
증상: OnClick 에서 NRE
  → LSP.hover "_cachedButton" → 필드 타입·선언 확인
  → LSP.findReferences            → 대입 지점 전수 (Awake? Start? 누가 null 로 남김?)
  → Grep "private\s+.*\s+_cached" → 같은 패턴 다른 파일
  → 원인 후보 확정 → 수정 대상 목록 전체로 확장
```
