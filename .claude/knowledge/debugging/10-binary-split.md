# 10. 이진 탐색으로 범위를 쪼개라

> A binary split can limit the search area of a 1000 line program to just 10 steps!
> — P. Adragna, §2.8

## Do
- 의심 범위의 **중간 지점** 에 체크포인트를 놓는다 — DAP 브레이크포인트, `UnityEngine.Assertions.Assert.*`, 또는 Bridge `Component.GetField` 스냅샷.
- 중간에서 **기대값이 맞으면** 후반부로 좁히고, **틀리면** 전반부로. log₂(N) 회 반복.
- 프레임·이벤트 시퀀스에도 적용: 문제 프레임 범위 [0, 1000] → [500, 1000] → [750, 1000] → ...
- 커밋 범위에도 적용: `git bisect` — 마지막 정상 커밋과 문제 커밋 사이 이진 탐색.
- 씬·프리팹 범위에도 적용: 의심 GameObject 절반 비활성화 → 문제 사라지면 나머지 절반에 원인.

## Don't
- 첫 줄부터 끝까지 순차 스텝 금지. 긴 루프·대규모 업데이트 경로에서 시간만 잃는다.
- BP 를 여러 군데 동시에 박지 않는다 — 중간 1개가 2개보다 정보 밀도가 높다. 정보를 얻은 뒤 다음 1개로 옮긴다.
- 기대값이 뭔지 정하지 않고 BP 멈춤 금지 — 이진 탐색은 "맞다/틀리다" 를 판정할 수 있을 때만 작동.

## Unity 맥락
- Domain Reload 비활성 환경: Play 재진입이 빠르므로 이진 탐색당 반복 비용이 거의 0 — 적극적으로 활용.
- 긴 초기화 체인 (`Awake → Start → 첫 Update`): 중간 메서드 1곳에 어서션 → 위·아래로 좁힌다.
- 어셈블리 의존 문제: `_Core` / `_UI` / `_Combat` 중 어디서 시작됐는지부터 반토막 테스트.
- Bridge 로 자동화 가능: `Component.SetField` 로 절반 비활성 → `EnterPlaymode` → `Component.GetField` 로 판정 → 반복.
