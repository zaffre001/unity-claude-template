# RULES.md — 불변 제약

> 이 파일에는 위반 시 시스템이 실제로 망가지는 규칙만 담는다.
> "코드가 지저분해진다"는 망가진 것이 아니다.
> "에디터가 3분 멈추거나, 어셈블리 주입이 실패하거나, DB가 오염되는 것"이 망가진 것이다.

---

### RULE-01 Domain Reload를 트리거하지 않는다

**위반 시:** 에디터가 3분 이상 멈추고 모든 스태틱 상태가 초기화된다.
         하네스 세션이 강제 종료된다.

금지 행위:
- `[InitializeOnLoad]` 어트리뷰트 신규 추가
- `.asmdef` 파일의 `autoReferenced` 값을 `true`로 변경
- `.asmdef` 파일의 구조를 임의로 변경 (의존성 순환 유발)

위반 여부 확인:
```
# 대괄호 포함 매치. "RuntimeInitializeOnLoadMethod" false positive 차단
grep -rE '\[InitializeOnLoad\]' Assets/ --include="*.cs"
grep -r  '"autoReferenced": true' Assets/
```

---

### RULE-02 심링크 폴더에 파일을 생성하지 않는다

**위반 시:** 원본 프로젝트에 의도하지 않은 파일이 반영되어 다른 워크트리에 영향을 준다.
         머지 시 추적 불가능한 충돌이 발생한다.

심링크 폴더 목록:
- `Assets/Art/`, `Assets/Audio/`, `Assets/Prefabs/`, `Assets/Materials/`
- `Assets/Textures/`, `Assets/Models/`, `Assets/Plugins/`, `Assets/Resources/`
- `ProjectSettings/`, `Packages/`

위반 여부 확인:
```
# 워크트리에서 심링크 폴더 내 새 파일 확인
git status --short Assets/Art/ Assets/Audio/ Assets/Plugins/ Assets/Prefabs/
```

---

### RULE-03 .meta 파일을 직접 편집하지 않는다

**위반 시:** GUID 충돌로 에셋 참조가 깨진다. 씬과 프리팹의 직렬화된 참조가 모두 끊어질 수 있다.

- `.meta` 파일은 Unity Editor가 자동 생성·관리한다.
- 스크립트에서 GUID를 직접 참조하지 않는다.

---

### RULE-04 물리 연산은 FixedUpdate에서만 수행한다

**위반 시:** 프레임 레이트에 따라 물리 시뮬레이션이 불안정해진다.
         점프 높이, 이동 속도가 기기마다 달라진다.

- `Rigidbody.AddForce`, `MovePosition`, `velocity` 직접 조작은 `FixedUpdate()`에서만 호출한다.
- `Update()`에서 물리 API를 호출하지 않는다.

---

### RULE-05 async 메서드에는 반드시 CancellationToken을 전달한다

**위반 시:** Play Mode 종료 후에도 비동기 작업이 계속 실행되어
         NullReferenceException 폭주 또는 에디터 크래시가 발생한다.

```csharp
// 올바른 패턴
async UniTask LoadAsync(CancellationToken ct)
{
    await SomeOperation(ct);
}

// 금지 패턴
async UniTask LoadAsync() // CancellationToken 없음
{
    await SomeOperation();
}
```

---

### RULE-06 ProjectSettings를 에이전트가 수정하지 않는다

**위반 시:** 바이너리 파일(GlobalGameManagers) 충돌이 발생하여
         수동 머지가 불가능하다. 프로젝트 전체 설정이 꼬일 수 있다.

- ProjectSettings 변경은 인간 아키텍트만 수행한다.
- 에이전트가 ProjectSettings 수정이 필요한 경우, 변경 사항을 보고하고 승인을 기다린다.
