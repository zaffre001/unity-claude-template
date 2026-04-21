# C# / .NET — Language-layer Knowledge

범용 C# 언어와 .NET 런타임 개념. 어느 .NET 프로젝트에나 적용된다. 출처: Charles Petzold, *.NET Book Zero*, 및 Unity 환경에서의 실무 해석.

Unity는 Mono(또는 IL2CPP) 런타임 위에서 C#을 실행한다. 이 문서는 "왜 이 규칙이 성립하는가"의 근거를 제공한다. 구체적 Unity 성능 규칙은 [unity-mobile-performance.md](unity-mobile-performance.md)에 있다.

---

## 1. Value Type vs Reference Type (핵심 멘탈 모델)

C#의 가장 중요한 구분:

> **Classes are reference types; structures are value types.**

| | Value type (`struct`, primitives) | Reference type (`class`, `string`, arrays) |
|---|---|---|
| 저장 위치 | 스택 또는 컨테이닝 객체 인라인 | 힙. 스택에는 참조만. |
| 대입 시 | **복사** (전체 값) | **참조 복사** (같은 객체 가리킴) |
| `null` 가능 | X (`Nullable<T>` 예외) | O |
| GC 대상 | X (수명이 스코프·컨테이너에 귀속) | O |
| 크기 | 고정 (struct의 필드 총합) | 참조는 4/8바이트, 실제 크기는 힙에서 |

기본 value types: `sbyte, byte, short, ushort, int, uint, long, ulong, float, double, decimal, char, bool` (전부 `System` 네임스페이스의 `struct`의 alias).

실무 결론:
- **8바이트 이하 데이터**: `struct`로. 할당 없음, 캐시 효율 좋음.
- **큰 데이터 또는 상속 필요**: `class`.
- **배열로 다룰 때**: `struct` 배열은 메모리 인라인 한 번, `class` 배열은 N개 참조 + 각각의 힙 객체 = N+1번 할당.

---

## 2. Stack vs Heap / Garbage Collection

### 스택

- 스레드마다 하나씩. 함수 호출 시 메모리 예약, 리턴 시 자동 해제.
- 지역 value-type 변수, 참조 변수 자체, 리턴 주소, 매개변수가 여기 저장.
- 크기 유동적인 데이터(`string`, 배열)는 스택에 못 담는다 → 참조만 담고 실체는 힙.

### 힙 (managed heap)

- 모든 reference type 인스턴스가 여기 산다.
- **GC가 관리**한다: 어떤 참조도 안 가리키는 객체는 회수 대상.
- Unity의 GC는 Boehm-Demers-Weiser (stop-the-world). **실행 중 멈춤 → 프레임 스파이크**.
- 회수 빈도를 줄이려면 **할당 자체를 줄여야 한다**.

### Null 의미론

- `string D;` — 스택에 자리는 있지만 **uninitialized**. 읽기 불가 (컴파일러가 막음).
- `D = null;` — 스택의 참조를 0으로. 힙 메모리 없음. `D.Length` → `NullReferenceException`.
- `D = "";` — empty string. 힙에 "길이 0" 객체 존재. `D.Length` = 0.

**null과 empty는 다르다.** API가 empty를 리턴하도록 통일하면 null-check 부담이 줄어든다.

---

## 3. Boxing / Unboxing

Value type을 `object` 또는 인터페이스에 대입하면 **힙에 박스를 만든다**.

```csharp
int i = 123;
object o = i;        // BOXING — 힙 할당 발생
int j = (int)o;      // UNBOXING — 복사
```

박싱은 **할당 + 복사 + 추후 GC**의 3중 비용.

박싱을 유발하는 흔한 상황:
- `ArrayList`, `Hashtable` 같은 **non-generic 컬렉션** — `List<T>`, `Dictionary<TK, TV>`를 써라.
- **LINQ**의 `IEnumerable<T>` 변환 체인 — 지연 실행 내부에서 박스.
- **Regex** 내부 처리.
- **`string.Format` / `$"{x}"` (보간)** — value 타입 인자가 `object[]`로 박싱. 핫 패스에선 `StringBuilder` 또는 직접 조립.
- `==` 오버로드가 없는 `object` 비교.
- `foreach`가 `IEnumerator` interface를 value-type enumerator에 대해 호출할 때 (C# 컴파일러가 대개 패턴 매칭으로 피해주지만 주의).

**제네릭은 박싱을 피하는 주된 도구.** `List<int>`는 내부에 `int[]`를 그대로 둠. `ArrayList`는 `object[]`에 박스.

---

## 4. Strings

- `string`은 **reference type**이지만 **불변(immutable)**.
- 연결·치환·Substring은 **전부 새 객체 할당**.
- 루프·핫 패스에서 누적은 **`StringBuilder`** 사용:

```csharp
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++) sb.Append(i);
string s = sb.ToString();
```

- String interning: 리터럴은 CLR이 하나만 유지 → `"foo" == "foo"` 는 저렴.
- `string.Equals(a, b)` 와 `a == b` 는 `string`에 대해 value equality 동일.
- `ReadOnlySpan<char>`/`Span<char>`는 할당 없이 부분 문자열을 다룰 수 있다 (modern .NET).

---

## 5. Events and Delegates

### Delegate

- **타입 안전한 함수 포인터**.
- 시그니처 선언: `public delegate void EventHandler(object sender, EventArgs e);`
- 변수에 메서드 할당 가능, 조합 가능(`+`), 호출 가능.

### Event

- 클래스가 외부에 노출하는 **등록 가능한 알림**.
- 내부 구현은 delegate 다중 구독 리스트.

```csharp
public event EventHandler OnDeath;

void Die()
{
    OnDeath?.Invoke(this, EventArgs.Empty);  // null-safe invoke
}
```

### 구독 / 해제

```csharp
enemy.OnDeath += HandleEnemyDeath;    // 구독
enemy.OnDeath -= HandleEnemyDeath;    // 해제
```

**핵심 규칙: 구독한 쪽이 해제해야 한다.** 해제하지 않으면:
- GC가 구독자를 못 거둬간다 (이벤트 발행자가 구독자를 참조하므로).
- Unity에선 씬 전환·객체 파괴 후에도 callback이 dead object에 도달 → `NullReferenceException` 또는 의도치 않은 side effect.
- Domain Reload 비활성화 환경에선 static 이벤트가 Play Mode 재진입 후에도 살아있다.

Unity 관례: `OnEnable`에서 `+=`, `OnDisable`/`OnDestroy`에서 `-=`. static 이벤트는 `[RuntimeInitializeOnLoadMethod]`로도 리셋.

### Anonymous / Lambda handlers

```csharp
enemy.OnDeath += (sender, args) => Debug.Log("died");
```

편하지만 **해제할 수 없다** (레퍼런스가 없어서). 해제가 필요한 핸들러는 이름이 있는 메서드로 만들거나 델리게이트 변수를 필드에 보관.

---

## 6. Generics

C# 2.0 도입. 타입 안전성 + 박싱 회피.

```csharp
class Point<T> where T : struct, IConvertible
{
    public T X, Y;
}

var p = new Point<int>();   // int 필드 인라인
var q = new Point<double>();// double 필드 인라인
```

### Constraints

- `where T : class` — reference type만
- `where T : struct` — value type만
- `where T : new()` — 매개변수 없는 생성자 필요
- `where T : SomeBase` — 특정 기본 클래스 파생
- `where T : IInterface` — 특정 인터페이스 구현
- 복수 조합 가능: `where T : class, IDisposable, new()`

### 제네릭 컬렉션 (`System.Collections.Generic`)

- `List<T>` (not `ArrayList`)
- `Dictionary<TKey, TValue>` (not `Hashtable`)
- `Queue<T>`, `Stack<T>`, `HashSet<T>`, `SortedList<TKey, TValue>`

non-generic 컬렉션은 **object 기반 → 박싱 폭탄**. 신규 코드에 쓰지 않는다.

### 데이터 구조 선택 가이드

| 필요 | 추천 |
|---|---|
| 순차 접근, append 빈번 | `List<T>` |
| 키로 조회 | `Dictionary<TKey, TValue>` |
| 중복 없는 집합 | `HashSet<T>` |
| FIFO | `Queue<T>` |
| LIFO | `Stack<T>` |
| 고정 크기·인덱스 접근 | `T[]` (배열) |
| 스레드 안전 필요 | `ConcurrentDictionary<>` 등 `System.Collections.Concurrent` |

---

## 7. Nullable Value Types

`int?` = `Nullable<int>` = value type + `HasValue` 플래그.

```csharp
int? count = null;
count = 5;
if (count.HasValue) Console.WriteLine(count.Value);
```

### Null-coalescing / conditional operators

- `a ?? b` — a가 null이면 b
- `a?.Foo()` — a가 null이면 호출 안 하고 null 반환
- `a ??= b` — a가 null이면 b 대입

### Reference type nullability (C# 8+)

```csharp
string notNull = "x";
string? maybeNull = null;
```

컴파일러가 흐름 분석으로 null 접근을 경고. `#nullable enable`로 파일/프로젝트 단위 활성화.

---

## 8. Equality Semantics

- **`==` 연산자**:
  - value type: 비트/필드 비교 (struct는 개발자가 오버로드 필요).
  - reference type: **참조 동일성** (같은 힙 객체냐).
  - `string`: 예외적으로 value equality (컴파일러 특별 처리).
- **`Equals(object)` 메서드**: 기본은 참조 비교. `override`해서 value equality 구현 가능.
- **`GetHashCode()`를 `Equals`와 쌍으로 오버라이드**. 같은 객체면 같은 hash — 안 그러면 `Dictionary`/`HashSet`가 깨진다.
- **`IEquatable<T>`** 구현으로 박싱 없는 비교 지원.

struct는 기본 `Equals`가 reflection 기반이라 느리다 — 커스텀 타입이면 `IEquatable<T>` 구현을 기본으로 생각한다.

---

## 9. Virtual / Override / Sealed / Abstract

- `virtual` — 파생 클래스가 재정의 가능 (vtable 경유, 호출 비용 아주 약간).
- `override` — 기반 가상 메서드 재정의.
- `new` (method hiding) — 상속 트리를 숨김. 런타임 다형성 안 됨. 대개 **잘못 쓴 신호**.
- `sealed` — 파생/재정의 금지. JIT가 직접 호출로 인라인 가능.
- `abstract` — 본문 없음, 파생 강제.

성능 관점: hot path의 가상 호출은 직접 호출보다 조금 느리다 (인라인 방해, 가상 디스패치). 단 일상적 코드에선 차이 무시 가능. 정말 뜨거운 경우에만 `sealed` 또는 static 분기 고려.

---

## 10. Properties vs Fields

```csharp
public int Hp { get; private set; }     // auto-property
public int Mp { get { return _mp; } set { _mp = Math.Max(0, value); } }
```

- Property는 **메서드 호출**로 컴파일됨 (JIT가 단순 auto-property는 인라인).
- Public 직접 필드 대신 property를 쓰는 이유: 미래 변경 시 호출부 깨뜨리지 않음, setter 검증 가능, 직렬화 제어.
- **Unity 직렬화**는 `[SerializeField] private` 필드를 선호. Inspector에 노출하는 값은 private 필드 + 필요시 public property getter.

---

## 11. Exception Handling

- Exception은 **비쌈** — throw 시 스택 walk, 추후 catch까지 비용 큼.
- **예상 가능한 실패 흐름에는 exception을 쓰지 않는다.** `TryParse`, `TryGetValue` 같은 패턴으로 대체.
- `try/catch` 블록 자체의 오버헤드는 작다 (catch 안 되면). catch가 실제로 발동할 때 비싸다.
- Unity에선 Play Mode 중 catch 안 된 예외는 GameObject를 비활성화시키지 않지만, 한 프레임의 `Update`는 거기서 중단.
- `async` 메서드에서의 예외는 `Task`에 저장됐다가 `await` 시 재throw — `await` 없는 fire-and-forget은 예외가 삼켜질 수 있다.

---

## 12. Async / Await + CancellationToken

- `async` 메서드는 **상태 머신으로 컴파일** — 힙 할당 발생 가능 (struct `ValueTask`로 감소 가능).
- **모든 async 메서드는 `CancellationToken`을 받아야 한다.** 호출자가 중단 가능하도록.

```csharp
async Task LoadAsync(CancellationToken ct)
{
    await SomeIo(ct);
    ct.ThrowIfCancellationRequested();
}
```

- Unity에서는 `async Task` 보다 **UniTask** (`Cysharp.Threading.Tasks`)가 권장 — zero allocation, PlayerLoop 통합.
- Play Mode 종료 후에도 돌아가는 async는 `NullReferenceException` 폭주의 주범 → cancellation 필수.

---

## 13. Interfaces

- C# 8+: 기본 구현(default interface method) 가능. 사용은 절제.
- Interface를 통한 호출은 가상 호출 (vtable + 인터페이스 맵).
- **작은 인터페이스 여러 개** > 거대한 single interface (ISP).
- 박싱 조심: value type을 interface 변수에 넣으면 박스. 제네릭 제약 (`where T : IFoo`)은 박싱 없음.

---

## 14. Unity에서 특히 중요한 포인트 요약

이 문서의 규칙 중 Unity 모바일에 직결되는 것들 (자세한 건 [unity-mobile-performance.md](unity-mobile-performance.md)):

1. `string` 연결·`$""` 보간 → StringBuilder (§4)
2. `ArrayList`/`Hashtable` 금지, `List<T>`/`Dictionary<K,V>` (§6)
3. `foreach`가 박싱을 유발하면 `for` (§3)
4. 이벤트는 `OnDisable`에서 해제 (§5)
5. `async` 메서드는 `CancellationToken` 필수 (§12)
6. 작은 데이터는 `struct` 배열이 `class` 배열보다 싸다 (§1)
7. `Equals`/`GetHashCode` 쌍 오버라이드 (§8)
8. Nullable은 value type 전용; C# 8+의 reference nullable은 경고만 (§7)

---

## 참고 자료

- Charles Petzold, *.NET Book Zero* — C/C++ 프로그래머용 C# 입문.
- Microsoft Learn: *C# programming guide*.
- Microsoft Learn: *Performance tips in .NET*.
- Unity Manual: *Understanding the managed heap*.
