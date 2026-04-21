# 7. print/Debug.Log 남용 금지

> printing statements are not reusable, and so are deprecated. (...) printing statements clobber the normal output of the program (...) performance reasons, output is usually buffered and, in case of crash, the buffer is destroyed and the important information is lost, possibly resulting in starting the debugging process in the wrong place.
> — P. Adragna, §2.3

## Do
- 레벨 있는 로거(`DEBUG / INFO / WARN / ERROR`) 또는 `#if` 토글 가능한 매크로성 로그만 사용.
- 런타임 값 확인은 Bridge `Component.GetField` 스냅샷 → 정상 출력 오염 없음, 재사용 가능, 코드에 흔적 안 남음.
- 호출 흐름 추적은 LSP `incomingCalls` / `outgoingCalls` 로 정적 대체. 런타임 로그로 경로를 재현할 필요 없음.
- 프레임마다 터지는 로그가 필요하면 **카운트·샘플링** (매 N 프레임 1회) 로.

## Don't
- 임시 `Debug.Log` 를 박고 나중에 지우는 루프에 의존 금지. Adragna 가 지적: ad-hoc·재사용 불가·성능 저하.
- 크래시 직전 값을 `Debug.Log` 로 잡으려 하지 않는다 — 버퍼가 날아가 잘못된 위치에서 디버깅을 시작하게 만든다. 어서션·DAP BP 로 잡는다.
- `Debug.LogError` 를 "눈에 띄게" 용도로 남발하지 않는다 — 콘솔이 오염되면 진짜 오류를 놓친다.
- `Debug.Log(obj)` 로 복잡 객체를 찍지 않는다 — ToString 이 GC 알로케이션 + Unity Editor 의 직렬화 중지 비용.

## 불가피하게 써야 한다면
- 스테디 상태 로그 아닌 **발생 순간 1회** (edge-triggered).
- `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 가드, 릴리즈에 안 나가게.
- 커밋 직전 `Grep "Debug\.Log"` 로 남은 임시 로그 전수 제거.
- 로거를 정말 넣으려면 `Conditional("UNITY_EDITOR")` 또는 커스텀 `ILogger` 추상화.

## 대체 도구 우선순위 (이 프로젝트)
1. **LSP 정적 추적** — 흐름·참조는 실행 없이 확인.
2. **Bridge `Component.GetField`** — 런타임 값 스냅샷.
3. **DAP 브레이크포인트 + evaluate** (설정됐을 때) — 지역변수·스텝.
4. **어서션 (§8)** — 가정을 코드에 박아 실패 시 정지.
5. **로거 (레벨 + 가드)** — 위 4개로 안 될 때만.
