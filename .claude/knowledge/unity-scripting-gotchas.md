# Unity Scripting — 고위험 함정 3선

다른 `knowledge/*.md`·`CLAUDE.md`·`RULES.md` 와 겹치지 않고, 모델이 **자주 틀리는** 항목만. Unity 2022.3 LTS 기준.

---

## 1. 직렬화 (Serialization)

### 1-1. 깊이 7 제한

중첩된 `[Serializable]` class/struct/List/배열이 **7 단계를 넘으면 그 아래는 조용히 저장 안 됨**. 런타임 기본값으로 돌아온다.

- 재귀 트리 (노드가 자식 `List<Node>`) 는 이 벽에 거의 항상 부딪힌다.
- 회피: 자식을 `UnityEngine.Object` 파생 (ScriptableObject 등) 으로 **참조**로 끊기 → 직렬화가 참조 지점에서 종료.

### 1-2. null 필드 자동 부활

`[Serializable]` 커스텀 class 타입 필드가 `null` 이면, Unity 는 **그 타입의 기본 생성자로 새 인스턴스를 만들어 채운다.** `if (field == null)` 기반 로직이 망가진다.

- "비어 있음" 을 표현해야 하면 옵션:
  - `bool hasValue` 플래그를 같이 둔다 (가장 단순)
  - 타입을 `UnityEngine.Object` 파생으로 바꾼다 — Object 참조는 null 을 유지함
  - `[SerializeReference]` + 명시적 null — 이 속성은 예외적으로 null 을 허용

### 1-3. 인라인 복제

non-`UnityEngine.Object` `[Serializable]` 클래스는 **값처럼 인라인 직렬화**. 두 필드가 같은 인스턴스를 참조해도 저장→로드 후엔 **별개의 두 객체**가 된다. 공유가 필요하면 ScriptableObject 로 빼서 그걸 참조.

### 1-4. `[SerializeReference]` (2019.3+) — 다형성·null 허용

`Animal[]` 에 `Dog`/`Cat` 을 섞고 싶거나 null 을 유지하고 싶을 때:

```csharp
[SerializeReference] public IAction[] actions; // Dog/Cat/null 모두 OK
```

주의: reference 로 저장되므로 동일 asset 내부에서의 **참조 공유**도 유지된다 (§1-3 회피책으로도 쓸 수 있다). 단, GUID 기반이 아닌 내부 ID 라 에셋 경계 넘어서는 못 쓴다.

### 1-5. `ISerializationCallbackReceiver` 로 Dictionary 저장

Unity 는 `Dictionary<K,V>` 를 직렬화하지 않는다. 정석 패턴:

```csharp
public class Table : MonoBehaviour, ISerializationCallbackReceiver {
    [SerializeField] List<string> _keys = new();
    [SerializeField] List<int>    _vals = new();
    public Dictionary<string,int> Runtime = new();

    public void OnBeforeSerialize() {
        _keys.Clear(); _vals.Clear();
        foreach (var kv in Runtime) { _keys.Add(kv.Key); _vals.Add(kv.Value); }
    }
    public void OnAfterDeserialize() {
        Runtime.Clear();
        for (int i = 0; i < _keys.Count; i++) Runtime[_keys[i]] = _vals[i];
    }
}
```

`OnAfterDeserialize` 는 **메인 스레드가 아닐 수 있음** → Unity API 호출 금지. 순수 데이터 변환만.

---

## 2. 코루틴 중단 조건 — 자주 틀리는 것

| 트리거 | 코루틴 중단? |
|---|---|
| `StopCoroutine(handle)` / `StopAllCoroutines()` | ✅ |
| `gameObject.SetActive(false)` | ✅ 즉시 |
| `Destroy(gameObject)` / `Destroy(component)` | ✅ |
| **`behaviour.enabled = false`** | ❌ **계속 돈다** |
| 씬 언로드 | ✅ |

→ `enabled=false` 로 컴포넌트를 "꺼도" 그 MB가 시작시킨 코루틴은 살아서 상태를 계속 바꾼다. 끄려면 반드시 명시적 `StopCoroutine` 또는 GO 비활성.

### `WaitForSeconds` vs `WaitForSecondsRealtime`

| | `timeScale = 0` 일 때 | 용도 |
|---|---|---|
| `new WaitForSeconds(t)` | **영원히 멈춤** | 게임 내 시간 (일시정지 영향 받음) |
| `new WaitForSecondsRealtime(t)` | 그대로 진행 | 일시정지 UI·메뉴 애니메이션·토스트 |

`Pause()` 만들면서 `WaitForSeconds` 쓴 타이머가 얼어붙는 버그 흔함. 실시간 기준이 맞다면 Realtime 쪽.

---

## 3. IL2CPP Managed Code Stripping (iOS·콘솔·WebGL 빌드)

IL2CPP 백엔드는 스트리핑을 **끌 수 없다.** 런타임에 사용하는 타입·메서드가 정적 참조로 잡히지 않으면 링커가 제거 → `MissingMethodException` / `Type not found`.

### 잘리는 대표 케이스

- `Type.GetType("Foo.Bar")` / `Activator.CreateInstance(type)` — 문자열로만 참조되는 타입
- `JsonUtility.FromJson<T>` 의 `T` 가 어디서도 `new T()` 되지 않을 때 (순수 역직렬화 대상 POCO)
- `MakeGenericMethod` / `MakeGenericType` — AOT 가 조합을 미리 알아야 함
- `[SerializeField] MyPoco x;` 의 `MyPoco` 가 데이터로만 등장

### 보존 방법

#### `[Preserve]` — 소스에 직접

```csharp
using UnityEngine.Scripting;

[Preserve]
public class SaveDataV2 {
    [Preserve] public int level;
}
```

#### `link.xml` — `Assets/` 어디든

```xml
<linker>
  <assembly fullname="Assembly-CSharp">
    <type fullname="Project.Core.SaveData" preserve="all"/>
    <type fullname="Project.Combat.Effects.*"/>
  </assembly>
  <assembly fullname="ThirdPartyPlugin" ignoreIfMissing="1">
    <type fullname="ThirdParty.Foo" preserve="all"/>
  </assembly>
</linker>
```

- `preserve="all"`: 타입·멤버 전부 유지
- `ignoreIfMissing="1"`: 해당 어셈블리가 빌드에 없어도 에러 안 냄 (플러그인 방어)
- 네임스페이스 와일드카드 `Project.X.*` 로 묶기 가능

### IL2CPP 에서 아예 깨지는 것

- `System.Reflection.Emit` (런타임 IL 생성) — AOT 불가
- 동적 어셈블리 로드 (`Assembly.Load(byte[])`) — 일부 구성만 가능, 대부분 실패

### 실전 흐름

1. 개발 빌드: Managed Stripping Level = **Minimal/Low**
2. 릴리스 직전 **High** 전환 → 주요 경로 플레이테스트
3. `MissingMethodException` / `TypeLoadException` 뜨면 해당 타입을 `link.xml` 에 등록
4. 플러그인은 기본적으로 `ignoreIfMissing="1" preserve="all"` 로 선방어
