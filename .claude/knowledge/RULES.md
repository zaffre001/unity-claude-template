# Programming RULES — Language & Engine (범용 강제 규약)

Chris Zimmerman, *The Rules of Programming* (Sucker Punch Productions)의 **21개 규칙 개념만 참고**하여 Unity/C# 맥락으로 **전면 재작성**한 규약 모음이다. 책 본문은 인용하지 않았으며, 모든 예시와 문구는 이 프로젝트용으로 새로 작성했다. 저작권은 원저자에게 있으며, 규칙의 깊은 맥락을 얻으려면 [원서](https://www.oreilly.com/library/view/the-rules-of/9781098133108/) 구매를 권장한다.

에이전트는 이 규약들을 **강제 준수**한다. 위반해야 할 불가피한 이유가 있으면 사유와 함께 아키텍트에게 보고한다.

> 루트 [`RULES.md`](../../RULES.md)는 이 프로젝트 고유의 **불변 제약**이다. 이 파일(`knowledge/RULES.md`)은 어느 프로젝트에나 적용되는 **범용 코딩 규약**이다. 혼동하지 않는다.

---

## R1 — 가능한 한 단순하게, 그러나 그 이상은 아니게

DON'T
```csharp
// 현재 사용처 1곳, 미래 수요 없음
public abstract class EventHandlerBase<TEvent, TContext> where TEvent : IEvent
{
    protected abstract void Handle(TEvent evt, TContext ctx);
}
public class DeathHandler : EventHandlerBase<DeathEvent, GameContext> { ... }
```

DO
```csharp
public class DeathHandler
{
    public void Handle(DeathEvent evt) { ... }
}
```

왜: 사용처 1곳에서 제네릭·추상·래퍼를 만들면 **문제보다 코드가 더 복잡**해진다. 수요가 생기면 그때 올린다(→ R4).

---

## R2 — 버그는 전염된다

DON'T
```csharp
// DamageCalc가 방어력을 두 번 곱하는 버그 발견 → 급한 대로 호출부에서 sqrt로 보정
float displayed = Mathf.Sqrt(calc.Compute(atk, def));
```

DO
```csharp
// 원인을 그 자리에서 수정 + 회귀 테스트 추가
// DamageCalc 내부의 중복 곱셈 제거
float Compute(int atk, int def) => Mathf.Max(0, atk - def);
// Tests/EditMode/DamageCalcTests.cs 에 케이스 추가
```

왜: 호출부가 버그에 적응한 코드가 쌓이면 **원 버그를 고칠 수 없게 된다**. 발견 즉시 뿌리부터 고친다.

---

## R3 — 좋은 이름이 최고의 문서

DON'T
```csharp
public class Manager
{
    public void Process(object data) { ... }
    public bool Check() => _flag;
}
```

DO
```csharp
public class InventoryRepository
{
    public void SaveItem(InventoryItem item) { ... }
    public bool IsFull => _slots.Count >= Capacity;
}
```

왜: `Manager`, `Helper`, `Util`, `Data`, `Info`는 아무 말도 하지 않는다. 주석으로 이름을 설명하고 싶어지면 **이름을 고친다**.

---

## R4 — 일반화는 세 번째 사례부터

DON'T
```csharp
// 사례 1: 플레이어 공격 — 구현
// 사례 2: 적 공격 등장 → 즉시 Generic<T>로 추출
public class AttackSystem<T> where T : ICombatant { ... }
```

DO
```csharp
// 사례 1, 2 는 별도 클래스로 나란히 둔다
public class PlayerAttack { ... }
public class EnemyAttack  { ... }
// 사례 3이 등장하는 순간, 공통 부분을 추출한다
```

왜: 두 사례로 일반화하면 **잘못된 모양**이 굳어져 세 번째 케이스가 끼지 않는다. 중복 2회는 허용 비용이다.

---

## R5 — 최적화의 첫 수업은 "최적화하지 말 것"

DON'T
```csharp
// "string 연결은 느릴 거야" — 프로파일 없이 StringBuilder 남발
var sb = new StringBuilder();
sb.Append(user.Name);
sb.Append(": ");
sb.Append(msg);
Log(sb.ToString());      // 메시지 한 줄에 불과
```

DO
```csharp
Log($"{user.Name}: {msg}");
// 프로파일러가 여기를 핫스팟으로 지목하면 그때 StringBuilder 로 교체
```

왜: **측정 없이 최적화**하면 가독성만 잃는다. Unity Profiler·Profile Analyzer로 병목을 특정한 뒤에만 손댄다.

---

## R6 — 코드 리뷰는 세 가지 이유로 좋다

DON'T
```
# 한 PR 에 UI 리팩터 + 전투 버그 + 렌더 최적화 + 오타 수정
feat: a bunch of stuff
 43 files changed, +1843 −921
```

DO
```
PR #1  fix(combat): damage calc 음수 클램프
PR #2  refactor(ui): inventory panel 분리
PR #3  perf(rendering): shadow caster 줄이기
```

왜: 큰 PR은 **리뷰가 불가능**하다. 한 PR = 한 주제. 리뷰 부담이 커지면 쪼갠다.

---

## R7 — 실패 케이스를 제거한다

DON'T
```csharp
public void TakeDamage(int amount)
{
    if (amount < 0) return;          // 호출자가 실수할 수 있음
    if (this == null) return;        // 방어 코드 남발
    currentHp -= amount;
}
```

DO
```csharp
public readonly struct DamageAmount
{
    public readonly int Value;
    public DamageAmount(int v) => Value = Math.Max(0, v); // 경계에서 1회 검증
}

public void TakeDamage(DamageAmount dmg)
{
    currentHp -= dmg.Value;           // 내부에선 믿고 쓴다
}
```

왜: 타입으로 **잘못된 상태를 표현 불가능**하게 만들면 런타임 검증이 줄어든다. 검증은 시스템 경계에서 한 번만.

---

## R8 — 실행되지 않는 코드는 작동하지 않는다

DON'T
```csharp
// if (false)  // old behavior — keep just in case
// {
//     DoLegacyThing();
// }

public void UnusedHelper() { ... }   // 호출처 없음
```

DO
```csharp
// 그냥 삭제한다. 과거 이력은 git log 에 있다.
```

왜: 죽은 코드는 **검증되지 않고 썩는다**. 남겨두면 언젠가 되살아나서 깨진다.

---

## R9 — 접혀서 읽히는 코드를 쓴다

DON'T
```csharp
public void ExecuteTurn()
{
    // 120줄의 인라인 로직 — 입력 파싱, 유효성 검사, 데미지 계산,
    // 상태 이상 적용, 애니메이션 재생, UI 업데이트, 네트워크 싱크...
}
```

DO
```csharp
public void ExecuteTurn()
{
    var action = ParseInput();
    if (!action.IsValid) return;

    ApplyDamage(action);
    TriggerAnimations(action);
    BroadcastToPeers(action);
}
```

왜: 함수를 **접었을 때 이름만 보고도** 흐름이 이해돼야 한다. 20~30줄 넘는 함수, 3단 이상 중첩은 추출 후보.

---

## R10 — 복잡성은 한 곳에 가둔다

DON'T
```csharp
// 할인 로직이 여러 곳에 흩뿌려짐
class CartUI   { void Refresh()  { if (isMember) price *= 0.9f; ... } }
class Checkout { void Finalize() { if (isMember) price *= 0.9f; ... } }
class Receipt  { void Build()    { if (isMember) price *= 0.9f; ... } }
```

DO
```csharp
class PricingService
{
    public Price Apply(Cart c, User u) { /* 모든 할인 로직 여기 */ }
}
// 다른 곳에선 PricingService.Apply 만 호출
```

왜: 복잡성이 **여러 모듈에 퍼지면** 변경 비용이 기하급수적으로 커진다. 어셈블리(`_Core`/`_UI`/`_Combat`/`_Rendering`)가 격리의 자연 단위.

---

## R11 — 2배 이상 좋은가?

DON'T
```
제안: ECS 로 모든 시스템 재작성
예상 이득: 평균 프레임 10% 개선, 특정 씬 5% 개선
예상 비용: 3개월, 기존 코드 전면 교체
→ GO
```

DO
```
제안: ECS 로 전투 시스템만 교체
예상 이득: 전투 씬 프레임 200% 개선 (측정된 병목)
예상 비용: 2주, 인터페이스 유지 채 병행 교체
→ GO
```

왜: 마이너 개선을 위해 **기존 아키텍처를 갈아엎지 않는다**. 개선 요구가 누적되면 임계점에서 한 번에 교체.

---

## R12 — 큰 팀은 강한 컨벤션이 필요하다

DON'T
```csharp
// CLAUDE.md 는 private 필드 _camelCase 인데, 내가 선호하는 m_ 적용
public class Player : MonoBehaviour
{
    private int m_hp;
}
```

DO
```csharp
public class Player : MonoBehaviour
{
    private int _hp;   // 프로젝트 컨벤션 준수
}
// 컨벤션 변경이 필요하면 아키텍트에게 제안 후 일괄 적용
```

왜: **일관성이 집단 생산성**을 만든다. 개인 취향으로 규약을 깨면 전체가 느려진다.

---

## R13 — 눈사태를 시작한 조약돌을 찾아라

DON'T
```csharp
// 가끔 NullReferenceException 발생 → catch 로 덮음
try { target.ApplyEffect(effect); }
catch (NullReferenceException) { /* 그냥 무시 */ }
```

DO
```csharp
// "왜 target 이 null 인가?" 를 추적한다
// → 풀에서 꺼낸 객체가 이미 Destroy 된 것이었음
// → 풀 반환 시점을 바로잡아 근본 해결
var pooled = _pool.Rent();
try { pooled.ApplyEffect(effect); }
finally { _pool.Return(pooled); }
```

왜: **증상을 덮으면 원인이 더 깊이 숨는다**. 예외를 삼키지 않는다. "왜"를 설명할 수 있을 때까지 파고든다.

---

## R14 — 코드에는 4가지 맛이 있다 (쉬운 문제엔 쉬운 코드)

DON'T
```csharp
// "현재 체력 출력" 이라는 쉬운 문제에
public interface IHealthPresenter { void Present(); }
public class HealthPresenterFactory { ... }
public class HealthPresenterRegistry { ... }
```

DO
```csharp
void OnGUI()
{
    GUI.Label(new Rect(10, 10, 100, 20), $"HP {player.Hp}");
}
```

왜: **쉬운 문제에 Hard 기법**(팩토리·레지스트리·이벤트 버스)을 얹으면 전체가 어려워진다. 복잡한 기법은 복잡한 문제용.

---

## R15 — 잡초는 즉시 뽑는다

DON'T
```csharp
// TODO: 나중에 리팩터 (2년째 방치)
public void DoThingOldWay() { ... }
```

DO
```csharp
// 지금 고친다. 못 하면 즉시 티켓으로 기록한다.
// - 5분 내 고칠 수 있는가? → 지금.
// - 그보다 크면 → /task-done 의 도메인 승격 또는 이슈 트래커로.
```

왜: **작은 방치가 쌓여 시스템을 망친다**. "나중에"는 "영원히"다.

---

## R16 — 결과에서 거꾸로 설계한다

DON'T
```csharp
// "이 클래스로 뭘 할 수 있지?" 부터 시작
public class DataManager
{
    public List<Thing> GetAll() { ... }
    public Dictionary<K,V> ConvertTo() { ... }
    public async Task<Stream> Export() { ... }
}
// → 실제로 필요한 건 "지금 점수판 TOP 10 표시" 뿐이었음
```

DO
```csharp
// 필요한 결과: "점수판 TOP 10 표시"
// 역산 → IScoreBoard.Top10() 한 메서드로 충분
public interface IScoreBoard { IReadOnlyList<Score> Top10(); }
```

왜: "이 코드로 뭘 할 수 있나"가 아니라 **"이 결과를 내려면 어떤 코드가 필요한가"**로 시작.

---

## R17 — 때로 더 큰 문제가 더 쉽다

DON'T
```csharp
void OnItemPickup(Item it)
{
    if      (it is HealthPotion h)  ApplyHeal(h);
    else if (it is ManaPotion   m)  ApplyMana(m);
    else if (it is Buff         b)  ApplyBuff(b);
    else if (it is Debuff       d)  ApplyDebuff(d);
    // ... 12개 더
}
```

DO
```csharp
// 특수 케이스를 올려 일반화
public interface IItem { void ApplyTo(Player p); }
void OnItemPickup(IItem it) => it.ApplyTo(_player);
// 단, R4 준수 — 세 번째 케이스가 나왔을 때만 올린다
```

왜: 특수 로직 10개보다 **원칙 1개**가 유지하기 쉽다. 단, 이른 일반화도 독이다(→ R4).

---

## R18 — 코드가 자기 이야기를 하게 한다

DON'T
```csharp
// 플레이어 체력이 0 이하면 죽음 처리
if (h <= 0) { d = true; a.Play("d"); } // h: hp, d: dead, a: animator
```

DO
```csharp
if (hp <= 0)
{
    isDead = true;
    animator.Play(AnimationNames.Death);
}
// 주석 없이도 읽힌다
```

왜: 주석은 **WHY**만 쓴다. WHAT은 이름·구조가 말하게 한다. 주석은 검증되지 않고 썩는다 — 코드와 어긋나면 주석이 거짓말한다.

---

## R19 — 병렬로 리팩터링한다

DON'T
```
git checkout -b refactor-everything
# 3주간 트렁크와 단절된 채 작업
# 머지 시 충돌 1,200건
```

DO
```bash
# 워크트리 병행 (이 프로젝트의 scripts/ 활용)
./scripts/create-symlinked-worktrees.sh 2
# worktrees/agent-0 에서 새 구현, 기존 인터페이스 유지
# 호출처를 한 모듈씩 이전 → 각 이전은 작은 PR
```

왜: **big-bang 리팩터는 실패**한다. 인터페이스를 유지한 채 새 구현을 병행하고, 호출처를 점진적으로 옮긴다.

---

## R20 — 수학을 한다

DON'T
```
"파티클 많이 쓰면 느릴 것 같은데 줄여야 되나?"
(감에 의존)
```

DO
```
# 목표: 모바일 30fps → 프레임 예산 22ms (thermal 65%)
# 현재 GPU 시간: 18ms, Particle.Draw: 6ms
# 파티클을 2/3 줄이면 GPU 14ms, 예산 내 복귀
# → 정량 근거로 결정
```

왜: 성능·메모리 결정은 **수치**로 한다. "느낌"은 R5 위반이다. Big-O, 바이트, 밀리초로 말한다.

---

## R21 — 때로는 그냥 망치로 못을 박는다

DON'T
```csharp
// 50개 Prefab 의 레이어를 일괄 변경
// → 자동화 스크립트 작성, 단위 테스트, 리뷰...
// → 반나절 소모, 실제 작업은 2분
```

DO
```
- Prefab 열기 → Layer 선택 → 저장
- 50번 반복
- 15분 소요
```

왜: **반복적·창의력 불필요한 작업에 영리한 해법을 만들 필요 없다**. 자동화 비용이 작업 비용보다 크면 그냥 한다.

---

## 에이전트가 특히 자주 어기는 방향

이 21개 중 에이전트가 **자동으로 과잉 제공**하는 경향이 있는 것들. 의식적으로 피한다:

- **R1, R4** — 요청하지 않은 제네릭·추상화·인터페이스 추가
- **R5** — "느릴 것 같아서" 하는 예측 최적화
- **R7** 잘못된 해석 — 방어 코드 남발(`if (x == null) return;`)
- **R8** — 주석 처리된 옛 코드 남김
- **R15** — 현재 작업과 무관한 "잡초"까지 건드려 범위 폭증
- **R18** — WHAT을 설명하는 장황한 주석

범위를 벗어나는 개선이 보이면 **수행하지 말고 기록**한다 — `/task-done`의 도메인 승격, 또는 별도 티켓.

---

## 프로젝트 규칙과의 우선순위

이 범용 규약과 프로젝트 [`RULES.md`](../../RULES.md)가 충돌하면 **프로젝트 RULES.md가 우선**한다 (불변 제약이기 때문). 예: R18이 "주석 최소화"를 권하지만 프로젝트 RULE-XX이 "public API에 XML doc comment 필수"라면 프로젝트 규칙을 따른다.
