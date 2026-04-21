---
name: run
description: Unity 플레이어를 빌드하고 바이너리를 실행합니다. 산출물은 프로젝트 상위 `builds/` 폴더에 `{label}-{branch}-{shortsha}[-dirty]-{target}-{timestamp}/` 이름으로 저장되어 메인·워크트리·커밋을 식별할 수 있습니다. 인자로 플랫폼을 받습니다 — mac | win | linux | webgl | android | ios (기본: 현재 OS).
---

# Skill: /run

유니티 플레이어를 빌드하고, 가능한 경우 빌드된 바이너리를 실행합니다.

실제 작업은 두 파일이 수행합니다:
- [`scripts/run.sh`](../../scripts/run.sh) — 플랫폼 감지, 모듈 확인, 핑거프린트, Unity 호출, 런치
- [`Assets/Editor/RunBuildCommand.cs`](../../Assets/Editor/RunBuildCommand.cs) — Unity batchmode에서 호출되는 빌드 메서드 (`Project.Editor.RunBuildCommand.Build`)

이 스킬 파일은 인자 파싱과 출력 해석만 담당합니다.

---

## 1. 인자

- **없음** → 현재 OS로 기본 (`mac` / `win` / `linux` 자동 감지)
- **`mac`** / `macos` / `osx` → `StandaloneOSX`
- **`win`** / `windows` → `StandaloneWindows64`
- **`linux`** → `StandaloneLinux64`
- **`web`** / `webgl` → `WebGL`
- **`android`** → `Android` (APK)
- **`ios`** → `iOS` (Xcode 프로젝트, macOS 호스트 필요)

---

## 2. 실행

프로젝트 루트(메인 또는 워크트리) 에서:

```
./scripts/run.sh {platform}
```

## 3. 출력 구조

```
<project-parent>/builds/
└── {label}-{branch}-{shortsha}[-dirty]-{target}-{timestamp}/
    ├── BUILD_INFO.txt       ← 레이블/브랜치/커밋/Unity 버전/호스트 등 핑거프린트
    ├── unity-build.log      ← 배치모드 Unity 로그 전문
    └── <player artifact>    ← .app / .exe / WebGL 디렉토리 / .apk / .xcodeproj 등
```

`label` 의미:
- `main` — 현재 디렉토리가 primary git worktree일 때
- `agent-0` / `agent-1` / ... — 워크트리일 때 해당 디렉토리 이름

---

## 4. 오류 해석

스크립트는 종료 코드로 실패 원인을 구분합니다. 그대로 사용자에게 전달하세요.

| 종료 코드 | 의미 | 대응 |
|---|---|---|
| `2` | 프로젝트 루트가 아님 / 플랫폼 인자 오타 | 경로 확인 후 재시도 |
| `3` | Unity Editor 미설치 | 스크립트가 출력한 Unity Hub 설치 안내를 그대로 전달. 버전은 `ProjectSettings/ProjectVersion.txt` 기준 |
| `4` | **플랫폼 모듈 미설치** | 스크립트가 출력한 Unity Hub GUI 절차 + CLI 명령을 그대로 전달. 임의로 모듈을 자동 설치하지 말 것 (시간 오래 걸림, 대화형 프롬프트 가능성) |
| `5` | 빌드 실패 | `unity-build.log` 끝 40줄을 스크립트가 출력함. 그 중 컴파일 에러·예외 스택트레이스를 요약해 보고 |

### 자주 나오는 빌드 실패

- **`No enabled scenes`** — 스켈레톤 상태의 템플릿에서는 정상. 사용자에게 `File → Build Settings`에서 씬을 추가하거나 `Assets/Scenes/`에 씬을 만들라고 안내한다.
- **컴파일 에러** — 로그 말미의 `error CS` 라인을 찾아 원인을 짚는다. 현재 세션에서 수정한 파일과 연관 있는지 먼저 확인.

---

## 5. 금지 사항

- **빌드 스크립트·`RunBuildCommand.cs`를 임의로 수정하지 않는다.** 수정이 필요해 보이면 아키텍트에게 보고 후 승인받는다.
- **플랫폼 모듈을 자동 설치하지 않는다.** 가이드만 제시.
- **다른 워크트리의 빌드 결과를 삭제하지 않는다.** `builds/` 정리는 별도 지시가 있을 때만.
- **`unity-build.log` 전문을 대화창에 붙여넣지 않는다.** 요약 + 원인 라인 위주로 보고.
