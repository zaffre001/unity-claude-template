---
name: self-update
description: 현재 세션에서 습득한 지식을 5개 계층(RULES / knowledge / domain / CLAUDE / skills / local) 중 가장 적합한 곳으로 승격 제안합니다. 아키텍트 승인 후 반영.
---

# Skill: /self-update (Knowledge Synthesis)

당신은 프로젝트의 **지식 관리자**이다. 지침이 비대해지지 않도록 각 파일의 성격을 지키며, 습득한 정보를 적절한 계층으로 분류한다.

---

## 1. 지식의 5계층

| # | 계층 | 위치 | 갱신 주체 |
|---|---|---|---|
| 1 | **언어·엔진 (범용)** | `.claude/knowledge/*.md` | /self-update |
| 2 | **프로젝트 도메인** | `.claude/domain/*.md`, `CLAUDE.md` | /task-done, /self-update |
| 3 | **불변 제약** | `RULES.md` | /self-update (엄격 관리) |
| 4 | **경로·파일 타입 스코프** | `.claude/rules/*.md` | /self-update |
| 5 | **스킬 (공정)** | `.claude/skills/*.md` | 아키텍트 직접 |

실시간·개인 메모는 **`CLAUDE.local.md`** (gitignore).

---

## 2. 계층 결정 트리

```
이 내용이 위반될 경우 시스템이 실제로 망가지는가?
 ├─ YES → RULES.md
 └─ NO →
     어느 Unity/C# 프로젝트에도 그대로 적용되는 범용 지식인가?
      ├─ YES → .claude/knowledge/{topic}.md
      └─ NO →
          이 프로젝트만의 기획·시스템·유기적 관계인가?
           ├─ YES → .claude/domain/{system}.md
           └─ NO →
               특정 경로·파일 타입에서만 필요한 규칙인가?
                ├─ YES → .claude/rules/{scope}.md
                └─ NO →
                    매 세션 바뀌는 휘발성 정보인가?
                     ├─ YES → CLAUDE.local.md
                     └─ NO → CLAUDE.md (프로젝트 개요)
```

"시스템이 망가진다"의 기준은 **엄격하게** 잡는다: 에디터가 수 분 멈춤, 어셈블리 로드 실패, DB 오염 등. 코드가 지저분해지는 건 망가진 게 아니다.

---

## 3. 작성 원칙

- **원리 중심:** 단순 코드 조각이 아닌 *왜(Why)*에 집중.
- **측정 가능:** grep·코드 리뷰로 위반 여부를 확인할 수 있어야 지침이다.
- **간결:** 한 규칙은 한 단락을 넘지 않는다.
- **직접 수정 금지:** diff 형식으로 제안하고 **아키텍트 승인**을 기다린다.
- **인덱스 동기화:** `.claude/knowledge/`, `.claude/domain/`, `.claude/rules/`에 파일을 신설했으면 [`.claude/INDEX.md`](../INDEX.md)의 해당 섹션과 `keywords`도 함께 업데이트한다. 누락하면 다음 세션의 `/task-start`가 선별 로드를 못 한다.

---

## 4. 출력 형식

```markdown
## /self-update 결과

### 이번 세션에서 새로 습득한 지식
{요약}

### 계층 판단
{트리 분기 결과와 선택 사유}

### 제안하는 변경
**대상 파일:** {경로}
**섹션:** {섹션명 또는 신규}
**변경 유형:** {추가 | 수정 | 삭제}

\```diff
+ 추가할 내용
- 삭제할 내용
\```

**이유:** {왜 이 지침이 필요한지}

**INDEX.md 갱신 (해당 시):**
\```diff
+ keywords: {추가할 키워드}
\```

---
승인하시면 파일을 직접 수정하겠습니다.
```
