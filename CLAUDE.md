# Unity Claude Template — CLAUDE.md

> Claude Code 에이전트 기반 유니티 프로젝트 템플릿. 포크해서 본인 게임 이름으로 바꿔 쓰세요.

## 1. 프로젝트 개요
- **엔진**: Unity 2022.3 LTS
- **언어**: C# 9
- **네임스페이스 루트**: `Project.*`
- **패키지 매니저**: Unity Package Manager (UPM)

## 2. 어셈블리 구조

| 어셈블리 | 경로 | 역할 |
|---|---|---|
| `Project.Core` | `Assets/Scripts/_Core/` | 공통 유틸, 인터페이스, 데이터 구조 |
| `Project.UI` | `Assets/Scripts/_UI/` | UI 시스템 전담 |
| `Project.Combat` | `Assets/Scripts/_Combat/` | 전투 시스템 전담 |
| `Project.Rendering` | `Assets/Scripts/_Rendering/` | 렌더링, 셰이더 관련 |
| `Project.Tests.EditMode` | `Assets/_Tests/EditMode/` | 에디터 모드 테스트 |
| `Project.Tests.PlayMode` | `Assets/_Tests/PlayMode/` | 플레이 모드 테스트 |

### 어셈블리 규칙
- `autoReferenced: false`는 항상 유지한다.
  true로 바꾸면 Unity가 어셈블리 전체를 재컴파일하고 Domain Reload가 유발된다.
  이유는 RULES.md RULE-01 참고.
- 새 어셈블리 추가 시 반드시 `.asmdef` 파일을 생성하고, 의존성을 명시적으로 선언한다.

## 3. 네임스페이스 컨벤션
- 모든 스크립트는 `Project.{어셈블리명}` 네임스페이스 하위에 위치한다.
- 예: `Project.Core.Utils`, `Project.UI.Inventory`, `Project.Combat.Damage`

## 4. Unity 핵심 개념 — 자주 하는 실수

### Domain Reload 비활성화 환경
이 프로젝트는 Enter Play Mode Settings에서 Domain Reload를 비활성화했다.
- `static` 변수는 반드시 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`으로 초기화한다.
- 이벤트 구독(`event`)도 동일하게 초기화 필수.
- 초기화하지 않으면 Play Mode 재진입 시 이전 상태가 유지되어 버그가 발생한다.

```csharp
// Domain Reload 비활성화 시 static 초기화 패턴
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}
```

### 물리 연산에서의 상태 체크
- `Rigidbody.AddForce`, `MovePosition` 등 물리 API 호출 전에 반드시 `_isGrounded` 등 관련 상태를 먼저 확인한다.
- 상태 체크 없이 물리 API를 직접 호출하면 의도하지 않은 중복 입력이 가능해진다.

### MonoBehaviour 라이프사이클
- `Awake()`에서 자기 초기화, `Start()`에서 외부 참조 바인딩.
- `OnDestroy()`에서 이벤트 구독 해제를 반드시 수행한다.
- `GetComponent<T>()` 결과는 캐싱한다. 매 프레임 호출 금지.

## 5. 코딩 컨벤션
- 필드: `_camelCase` (private), `PascalCase` (public/serialized)
- 메서드: `PascalCase`
- 상수: `UPPER_SNAKE_CASE`
- 인터페이스: `I` 접두사 (`IInteractable`)
- `async` 메서드 사용 시 반드시 `CancellationToken`을 인자로 받는다.
- UniTask 사용 시 `CancellationToken` 전달 필수.

## 6. 워크트리 환경
- 이 프로젝트는 Git Worktree + 심링크 기반 병렬 에이전트 환경을 지원한다.
- 심링크 폴더(Art, Audio, Plugins, Prefabs, Materials, Textures, Models, Resources)는 **읽기 전용**이다.
- 심링크 폴더에 새 파일을 생성하지 않는다.
- `.meta` 파일을 직접 편집하지 않는다.
- 워크트리 스크립트: `scripts/` 디렉터리 참고.

## 7. 디버깅
- DAP 기반 디버깅 환경이 구성되어 있다.
- Unity Editor는 Debug 모드로 실행해야 디버거 연결이 가능하다.
- 브레이크포인트에서 멈춘 후 반드시 `continue`를 호출해야 에디터가 재개된다.
- 디버깅 완료 후 Code Optimization을 Release로 되돌린다.

## 8. 참고 파일
- 불변 제약: `RULES.md`
- 스킬(공정 정의): `.claude/skills/`
- 경로별 규칙: `.claude/rules/`
