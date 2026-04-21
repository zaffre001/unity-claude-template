# 8. 어서션으로 가정을 코드에 박아라

> Writing assertions in the code makes assumptions explicit. (...) The important point to remember about assertions is that it make no sense to execute a program after an assertion fails.
> — P. Adragna, §2.5

## Do
- 함수 진입 / 상태 전이 / 외부 참조 해제 직전에 `UnityEngine.Assertions.Assert.*` 로 가정을 박는다.
- 어서션은 테스트가 아니라 **"이 라인 이후엔 이 조건이 참이다"** 를 코드로 남기는 문서다.
- 어서션 실패 = 즉시 정지. 예외 무시 / `try-catch` 로 덮기 금지.
- 가정이 바뀌면 어서션도 함께 고친다 — 어서션이 stale 해지면 오히려 방해.

## Don't
- 사용자 입력 검증에 `Assert` 쓰지 않는다 — 검증은 `if` + 오류 처리. `Assert` 는 "절대 있어선 안 될 조건" 전용.
- 릴리즈에서 살아남아야 할 로직을 `Assert` 안에 넣지 않는다. `Assert.IsTrue(OperationWithSideEffect())` 가 릴리즈에서 날아가면 부수효과가 사라짐.
- 어서션으로 "방어 코딩" 흉내내지 않는다 — 정상 제어 흐름은 어서션이 아니라 명시적 분기로.

## Unity 맥락
- `UnityEngine.Assertions.Assert.*` 는 에디터 · `DEVELOPMENT_BUILD` 에서만 활성. 릴리즈 자동 제거.
- 자주 쓰는 패턴:
  - `Assert.IsNotNull(_rigidbody, $"{nameof(_rigidbody)} not wired on {name}")` — Awake 끝에.
  - `Assert.IsTrue(_isGrounded.HasValue, "State must be known before physics call")` — FixedUpdate 진입부 (RULE-04 방어).
  - `Assert.IsNotNull(ct, "CancellationToken must be provided")` — async 진입 (RULE-05 방어).
- 어서션 메시지에는 **컨텍스트 값**을 넣는다 (`{name}`, 상태 enum 등) — 실패 로그만 보고 상황을 재구성할 수 있게.
