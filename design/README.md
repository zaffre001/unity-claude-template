# design/

`/design` 스킬이 생성한 작업 계획 아티팩트가 쌓이는 폴더입니다.

## 구조

```
design/
└── {slug}/                         ← 기획 한 건당 폴더 하나
    ├── README.md                   ← 전체 설계 요약, 단계 목록, 의존성 그래프
    └── step-NN-{area}-{topic}.md   ← 각 단계 에이전트용 독립 실행 프롬프트
```

## 사용 흐름

1. **계획 수립**
   ```
   /design {기획 내용 또는 Notion 링크}
   ```
   → `design/{slug}/` 폴더에 README와 단계별 프롬프트가 생성됩니다.

2. **검토**  
   아키텍트가 `design/{slug}/README.md`를 읽고 계획을 승인/수정합니다.

3. **실행**  
   각 단계 프롬프트를 에이전트에게 전달합니다. 병렬 실행 가능한 단계는 워크트리를 나눠 동시에 진행할 수 있습니다.
   ```bash
   cat design/{slug}/step-01-*.md | claude
   ```

## 규칙

- 각 계획은 **한 번 실행되면 변경하지 않는 불변 아티팩트**로 취급합니다. 변경이 필요하면 `{slug}-v2` 등으로 새 폴더를 만드세요.
- 단계 파일은 **다음 에이전트가 문맥 없이 실행할 수 있도록** 자기 완결적으로 작성됩니다.
- 스킬 상세 동작은 [`.claude/skills/design/SKILL.md`](../.claude/skills/design/SKILL.md) 참고.
