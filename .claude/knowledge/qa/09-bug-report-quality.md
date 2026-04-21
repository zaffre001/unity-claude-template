# 9. 버그 리포트 — 재현 조건 + 증거 + Severity

> When bugs are identified, the testing team will pass the metrics to the development team in the form of a test report. This contains details on how many defects or bugs were found, how many test cases failed and which need to be re-run.
> — Global App Testing, Ch 7 (stage 5 "Report bugs")

## Do — 리포트 필수 4요소

### 1) 재현 최소 단계
몇 단계로 같은 버그가 또 나는가. Bridge 커맨드 시퀀스 또는 Unity Test `[UnityTest]` 로 표현. **재현 없는 리포트 = 보고 실패**.

### 2) 증거
- 기대값 vs 실제값 (숫자·enum·boolean)
- 파일:라인 (수정 대상 지목)
- 스택트레이스 요약 (전문 금지 — `error CS` / exception type / 마지막 user code 프레임)
- 필요 시 스크린샷 (`Component.GetField` 로 Transform 값 포함, 좌표 자동화 우회 금지)

### 3) Severity — 영향 기준으로 한 레벨만
- **Critical** — 게임 진행 불가 / 데이터 파괴 / 크래시
- **High** — 기획 조항 불일치 / 핵심 루프 기능 손상
- **Medium** — UX 훼손 / UI 깨짐 / 비핵심 플로우 버그
- **Low** — cosmetic / 콘솔 경고 / 낮은 빈도 시각 글리치

### 4) 영향 범위
어떤 디바이스·OS·해상도·설정 조합에서 재현되는가. 한 조합만 봤으면 "저사양 / 고사양 미검증" 을 명시.

## Don't
- "가끔 이상해요" / "작동이 안 돼요" / "뭔가 느린 것 같아요" 금지 — 재현 없으면 개발자가 할 수 있는 일 없음.
- 증거 없이 severity 주장 금지 — Critical 이라 쓰면 증거로 증명해야.
- 대화창 줄글로만 남기고 잊지 않는다. **최소한** `.claude/domain/qa-journal.md` 같은 버전관리 파일에 추가 — 다음 세션에서 검색 가능해야 한다.
- Severity 인플레이션 금지 — 전부 Critical 이면 아무것도 Critical 이 아니다.

## 강제하지 않는 것 (기본 동작 ❌)
- Jira / Linear / GitHub Issues 트래커 의무화 — 인디면 위 `qa-journal.md` 한 파일로도 충분.
- 모든 Low 버그까지 리포트 작성 — 핵심 루프·결제·저장 영향 건만 문서화, 로그 경고는 묶어서 1건으로.

## Unity / Agent
- Bridge 커맨드 시퀀스 자체가 재현 스크립트 — 리포트에 해당 `.claude-bridge/inbox/*.json` 예시 첨부 가능.
- CLAUDE.md §8 와 동일 원칙: Unity 로그 전문 붙이지 않기. 요약 + 원인 라인.
- 재현 단계가 Bridge 로 표현 불가하면 op 추가부터 제안 — 컴퓨터 유즈 우회 금지.
