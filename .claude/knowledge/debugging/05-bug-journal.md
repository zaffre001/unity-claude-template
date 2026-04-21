# 5. 버그 저널을 남겨라

> it is good practice filling a small file with detailed explanations about the way the bug was discovered and corrected.
> — P. Adragna, §1.4

## 기록 항목 (Adragna §1.4)
- **발견 경로** — 어떤 증상·테스트로 드러났는가. 재현 테스트 작성 근거가 됨.
- **추적 방법** — 어떤 LSP / Grep / Bridge 조합이 통했는가. 다음 유사 버그에 재사용 가능한 레시피.
- **버그 유형** — §1.2 분류 (Syntactical / Build / Basic Semantic / Semantic, Bohrbug / Heisenbug).
- **반복 빈도** — 잦으면 `RULES.md` 혹은 `.claude/rules/` 로 승격 후보.
- **잘못됐던 초기 가정** — 이것이 진짜 가치. 가정 목록이 쌓이면 도메인 지식이 된다.

## Unity / Claude 맥락 — 공식 승격 경로
- `/task-done` STEP 5 (도메인 지식 승격) = 저널의 공식 창구. 세션에서 드러난 이 프로젝트 고유 지식은 `.claude/domain/{system}.md` 로.
- 범용 패턴 (어느 Unity 프로젝트에도 적용) → `/self-update` 로 `.claude/knowledge/` 혹은 `RULES.md` 승격 제안.
- 승격 시 반드시 `.claude/INDEX.md` keywords 함께 업데이트 — 안 그러면 다음 세션에서 발견 안 됨.

## Don't
- 일회성 에피소드를 무리해서 규칙으로 승격하지 않는다. 인덱스 비대는 다음 세션의 선별 로드를 망친다.
- 저널 대신 주석으로 남기지 않는다 — 주석은 rot 하고 이동 불가. 저널은 추상화 경계 밖 문서.
- "다음에 또 나오면 쓰자" 로 미루지 않는다. 증거와 맥락은 지금이 아니면 휘발된다.
