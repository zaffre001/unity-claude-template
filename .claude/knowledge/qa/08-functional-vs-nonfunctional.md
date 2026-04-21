# 8. Functional vs Non-Functional — 분리해서 평가

> Non-Functional Testing is concerned with the how, whereas Functional Testing is concerned with the what. (...) Functional Testing verifies what the system should do, and Non-Functional Testing tests how well the system works.
> — Global App Testing, Ch 4

## Do — 두 카테고리 리포트 섹션 분리
- **Functional (무엇을)**: 기획 조항 정합성 — "전투 종료 시 보상이 지급되는가", "저장 후 로드 시 인벤토리가 동일한가". §4 RTM 에 연동.
- **Non-Functional (얼마나 잘)**:
  - Performance — 프레임 시간, GC 알로케이션, 드로우 콜
  - Load — 동시 AI 100기 · 동시 파티클 1000개
  - Reliability — 1시간 연속 플레이 시 누수 / 크래시 0건
  - Readiness — 저사양 기기 부팅·로딩 시간
  - Usability — 의도한 입력이 의도한 반응으로 연결되는가 (Ch 9 에서 인간 판정, 에이전트는 부분 대체)
- Functional 통과라도 Non-Functional 미통과면 **릴리즈 보류**.

## Don't
- "기능은 되니까 성능도 괜찮겠지" 금지 — 100명 동시 배틀 60fps 유지를 1인 테스트로 증명할 수 없다.
- Functional 과 Non-Functional 을 같은 스위트에 섞지 않는다. 각 실패의 원인 해석이 다르다 (기획 버그 vs 아키텍처 버그).
- Non-Functional 을 출시 직전에 처음 돌리지 않는다 — 수정이 아키텍처 레벨일 수 있고 그때는 늦다.

## Unity / Agent — 게임 맥락
- **Functional**: Bridge `Component.GetField` 로 게임 상태 스냅샷 → 기대값 비교. `/qa` 스킬 §4 의 주요 경로.
- **Non-Functional**:
  - Profiler API 를 `Reflection.Invoke` 로 샘플링 → 프레임 시간·GC 알로케이션·드로우 콜 자동 수집
  - 판정 지표는 [.claude/knowledge/unity-mobile-performance.md](../unity-mobile-performance.md) 와 연동
  - 모바일 게임 출시 블로커는 대부분 Non-Functional: 60fps 유지·메모리 피크·배터리 발열·로딩 타임
- Load 시뮬레이션: Bridge `Reflection.Invoke` 로 `Instantiate` 대량 호출 → 프레임 시간 측정.
