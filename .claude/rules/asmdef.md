---
description: .asmdef 파일 수정 시 적용되는 규칙
globs: ["**/*.asmdef"]
---

# 어셈블리 정의 파일 규칙

- `autoReferenced`는 반드시 `false`로 유지한다. (RULES.md RULE-01)
- 의존성 순환을 만들지 않는다.
- 새 어셈블리 추가 시 아키텍트 승인이 필요하다.
- `defineConstraints`를 통해 에디터 전용 어셈블리를 명확히 분리한다.
