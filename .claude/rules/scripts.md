---
description: Assets/Scripts/ 하위 C# 파일 수정 시 적용되는 규칙
globs: ["Assets/Scripts/**/*.cs"]
---

# 스크립트 작성 규칙

- 네임스페이스는 반드시 `Project.{어셈블리명}` 하위에 위치한다.
- `static` 변수 추가 시 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 초기화 메서드를 함께 작성한다.
- 이벤트 구독(`+=`) 시 반드시 `OnDestroy()`에서 해제(`-=`)한다.
- `GetComponent<T>()` 결과는 필드에 캐싱한다. `Update()`에서 매 프레임 호출 금지.
- `async` 메서드는 `CancellationToken`을 반드시 인자로 받는다.
