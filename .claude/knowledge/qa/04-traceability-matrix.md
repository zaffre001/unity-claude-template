# 4. 스펙 ↔ 구현 ↔ 테스트 매핑 (RTM 경량 버전)

> Creating a Requirement Traceability Matrix (RTM) is one way to ensure all business requirements are tested. (...) you'll ensure that you discover more software bugs and cover more testing bases.
> — Global App Testing, Ch 4

## 인디 톤
RTM 이라는 엔터프라이즈 용어는 인디에 과하다. 여기서 말하는 건 **"기획서 조항에 ID 를 붙이고, 구현·테스트가 그 ID 를 역참조 가능하게 한다"** 정도. 별도 매트릭스 문서 유지 강제 아님.

## Do — 경량 매핑
- 기획서 각 조항에 **ID** 부여 (§1, §2.1 ...). `.claude/domain/{system}.md` 에 작성.
- 구현 코드의 해당 메서드 위에 `// Spec: §2.1` 한 줄 주석. 이게 매핑.
- 테스트가 있다면 테스트 이름에 ID 포함 (예: `Test_Spec_2_1_ComboBonus`).
- QA 요청 받으면 `Grep "Spec:"` 또는 `Grep "§"` 로 매핑 누수를 즉석에서 찾는다 — 별도 매트릭스 문서 유지 불필요.

## Don't
- **대표 케이스만** 으로 릴리즈하지 않는다. 핵심 루프 (결제·저장·진행) 는 조항 단위 매핑이 있어야 한다.
- 기획이 바뀌었는데 코드 주석·테스트 이름의 ID 를 그대로 두지 않는다. ID 는 살아 있어야 의미.

## 강제하지 않는 것 (기본 동작 ❌)
- 별도 RTM.xlsx / Notion 문서 유지 — **과함**. 주석·테스트 이름으로 충분.
- 모든 조항 100% 매핑 — 이상적이나 인디는 핵심 80% 만이라도 실전적.
- 매핑 없는 QA 전면 금지 — 매핑 있으면 훨씬 좋지만, 없어도 `/qa` 돌릴 수 있다. 그 경우 리포트에 "매핑 없음" 명시.

## Unity / Agent
- `/qa` 스킬 §5 "기준별 Pass/Fail 증거 리포트" 가 RTM 의 실행 형태 — 기준 목록을 리포트 시점에 생성한다.
- 매핑 누수 스캔 스니펫 (필요할 때만):
  ```
  Grep "§[0-9]"        .claude/domain/*.md   # 기획 조항 ID 전수
  Grep "Spec: §"       Assets/                # 매핑된 구현
  # diff → 매핑 안 된 조항
  ```
