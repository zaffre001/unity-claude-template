# Unity Claude Template — CLAUDE.md

> Claude Code 에이전트 기반 유니티 프로젝트 템플릿. 포크해서 본인 게임 이름으로 바꿔 쓰세요.

## 0. 이 프로젝트에 대하여

이 레포지토리는 **Unity Claude Template**이다. Claude Code 에이전트와 함께 유니티 게임을 만들기 위한 스켈레톤 프로젝트로, 블로그 시리즈 "유니티 × Claude Code"의 설계를 구현한다.

### 에이전트가 먼저 알아야 할 것

- **`Assets/Scripts/` 하위는 비어 있다.** 각 어셈블리 폴더(`_Core`, `_UI`, `_Combat`, `_Rendering`)에는 `.asmdef`만 있고 소스 코드는 없다. 이것이 정상 상태다.
- "스크립트가 하나도 없다"는 것은 버그가 아니라 템플릿 설계다. 사용자의 지시에 따라 코드를 채워 넣는 것이 기본 작업이다.
- 어셈블리 4개는 게임 장르에 맞게 **삭제하거나 리네임 가능**하다. 예: 퍼즐 게임이면 `_Combat`을 지우고 `_Puzzle`을 만든다.
- 네임스페이스 루트 `Project.*`는 템플릿 기본값이다. 포크 사용자가 본인 이름(예: `MyGame.*`)으로 치환하도록 설계되어 있다.

### 에이전트 기본 워크플로우

새 작업을 받으면 다음 순서로 스킬을 호출한다:

```
/task-start   ← 범위 확정 (RULES.md 재확인, grep으로 대상 파일 확인, 작업 범위 선언)
     ↓
 작업 실행     ← 선언한 범위 안에서만 수정
     ↓
/task-done    ← 변경 요약, 자체 검증, 정리, /self-update 판단
     ↓
/self-update  ← 새로 습득한 지식을 지침 파일로 승격 제안 (아키텍트 승인 필수)
```

스킬 상세는 `.claude/skills/` 참고.

### 이 구조의 근거가 된 블로그 시리즈

1. [에이전트의 뇌를 설계하는 법](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-AI-에이전트를-실무에-쓴다) — CLAUDE.md / RULES.md / skills 계층 설계
2. [Domain Reload 없는 병렬 에이전트 워크트리 설계](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-병렬-에이전트-설계) — Git Worktree + 심링크 병렬 환경
3. [DAP 기반 에이전트 디버깅 환경](https://velog.io/@zaffre/게임개발과-AI-유니티xClaude-Code-DAP-기반-에이전트-환경-만들기) — 런타임 변수 검증 루프

---

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

## 8. 지식 계층 & 인덱스

에이전트는 `/task-start`에서 [`.claude/INDEX.md`](.claude/INDEX.md)를 먼저 읽고, 작업 주제에 매칭되는 파일만 선별 로드한다. 이래서 토큰과 시간을 아낀다.

| 계층 | 위치 | 성격 |
|---|---|---|
| 1. 언어·엔진 (범용) | `.claude/knowledge/*.md` | Unity / C#.NET / 코딩 규약 (Zimmerman 21 Rules 포함) |
| 2. 프로젝트 도메인 | 이 파일 + `.claude/domain/*.md` | 이 프로젝트만의 기획·시스템·유기적 관계 |
| 3. 불변 제약 | `RULES.md` | 위반 시 시스템이 망가지는 프로젝트 고유 규칙 (RULE-01 ~) |
| 4. 경로 스코프 | `.claude/rules/*.md` | 경로·파일 타입별 규칙 |
| 5. 스킬 (on-demand) | `.claude/skills/*.md` | `/task-start`, `/task-done`, `/self-update`, `/design`, `/run` |

추가로:
- `CLAUDE.local.md` — 개인·실시간 지침 (`.gitignore` 포함, 버전 컨트롤 제외)
- `.claude/domain/` — `/task-done`이 새 도메인 지식을 쌓는 곳 (빈 스켈레톤에선 비어 있음)

지식 계층의 **1계층 RULES**(`.claude/knowledge/RULES.md`, 범용 코딩 규약)와 **3계층 RULES**(루트 `RULES.md`, 이 프로젝트 불변 제약)를 혼동하지 않는다. 전자는 포크해 쓰는 모든 프로젝트에 그대로, 후자는 이 프로젝트 전용.
