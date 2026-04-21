using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// 컴포넌트 추가 / 필드·프로퍼티 읽기·쓰기.
    /// 리플렉션 기반이라 Unity 내장, UGUI, TextMeshPro, 사용자 정의 스크립트 전부 대응 가능.
    /// </summary>
    public static class ComponentOps
    {
        public static string Add(string argsJson)
        {
            var a = JsonUtility.FromJson<CompAddArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");
            var type = TypeResolver.Resolve(a.type) ?? throw new ArgumentException($"Type not found: {a.type}");
            if (!typeof(Component).IsAssignableFrom(type))
                throw new ArgumentException($"Not a Component type: {a.type}");

            Undo.AddComponent(go, type);
            return JsonUtility.ToJson(new CompAddResult { type = type.FullName });
        }

        public static string SetField(string argsJson)
        {
            var a = JsonUtility.FromJson<CompSetFieldArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");
            var type = TypeResolver.Resolve(a.type) ?? throw new ArgumentException($"Type not found: {a.type}");
            var comp = go.GetComponent(type) ?? throw new ArgumentException($"Component not found on {a.path}: {a.type}");

            Undo.RecordObject(comp, "ClaudeBridge.SetField");

            var value = ValueCodec.Decode(a.valueJson, a.valueType);

            var fi = type.GetField(a.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null) { fi.SetValue(comp, value); goto done; }

            var pi = type.GetProperty(a.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && pi.CanWrite) { pi.SetValue(comp, value); goto done; }

            throw new ArgumentException($"Field/property not found: {type.FullName}.{a.field}");

        done:
            EditorUtility.SetDirty(comp);
            return JsonUtility.ToJson(new CompSetFieldResult { field = a.field });
        }

        public static string GetField(string argsJson)
        {
            var a = JsonUtility.FromJson<CompGetFieldArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");
            var type = TypeResolver.Resolve(a.type) ?? throw new ArgumentException($"Type not found: {a.type}");
            var comp = go.GetComponent(type) ?? throw new ArgumentException($"Component not found on {a.path}: {a.type}");

            object raw = null; Type raw_t = null;
            var fi = type.GetField(a.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null) { raw = fi.GetValue(comp); raw_t = fi.FieldType; }
            else
            {
                var pi = type.GetProperty(a.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null) throw new ArgumentException($"Field/property not found: {type.FullName}.{a.field}");
                raw = pi.GetValue(comp); raw_t = pi.PropertyType;
            }

            return JsonUtility.ToJson(new CompGetFieldResult
            {
                valueJson = ValueCodec.Encode(raw, raw_t),
                valueType = raw_t.FullName,
            });
        }

        /// <summary>
        /// UGUI RectTransform을 한 호출에 설정. anchor/pivot/size를 각각 SetField 하려면
        /// 7~8번 호출해야 하는데, 이 op 하나로 끝난다. null 또는 길이 2가 아닌 배열은 "변경 안함".
        /// offsetMin/Max는 anchor·sizeDelta 기반이라 다른 값과 함께 지정하면 마지막에 적용됨.
        /// </summary>
        public static string SetRectTransform(string argsJson)
        {
            var a = JsonUtility.FromJson<CompSetRectTransformArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");
            var rt = go.GetComponent<RectTransform>()
                ?? throw new ArgumentException($"No RectTransform on: {a.path}");

            Undo.RecordObject(rt, "ClaudeBridge.SetRectTransform");

            if (Has2(a.anchorMin))        rt.anchorMin        = V2(a.anchorMin);
            if (Has2(a.anchorMax))        rt.anchorMax        = V2(a.anchorMax);
            if (Has2(a.pivot))            rt.pivot            = V2(a.pivot);
            if (Has2(a.anchoredPosition)) rt.anchoredPosition = V2(a.anchoredPosition);
            if (Has2(a.sizeDelta))        rt.sizeDelta        = V2(a.sizeDelta);
            // offsetMin/Max는 anchor+size를 덮어쓰므로 위 필드 뒤에 적용.
            if (Has2(a.offsetMin))        rt.offsetMin        = V2(a.offsetMin);
            if (Has2(a.offsetMax))        rt.offsetMax        = V2(a.offsetMax);

            EditorUtility.SetDirty(rt);
            return JsonUtility.ToJson(new CompSetRectTransformResult { path = GameObjectPath.FullPath(go) });

            static bool Has2(float[] arr) => arr != null && arr.Length == 2;
            static Vector2 V2(float[] arr) => new Vector2(arr[0], arr[1]);
        }
    }

    /// <summary>
    /// 타입 풀네임 → Type. 로드된 모든 어셈블리에서 검색하므로 사용자 스크립트까지 커버한다.
    /// </summary>
    internal static class TypeResolver
    {
        public static Type Resolve(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;

            // 1) 직접 시도 (AssemblyQualifiedName 이거나 운이 좋으면 맞음)
            var t = Type.GetType(fullName);
            if (t != null) return t;

            // 2) 전체 어셈블리 순회
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(fullName, throwOnError: false);
                if (t != null) return t;
            }

            // 3) "GameObject", "Transform" 같은 짧은 이름 편의 지원 (UnityEngine 네임스페이스 우선)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var candidate in asm.GetTypes())
                if (candidate.Name == fullName) return candidate;

            return null;
        }
    }

    /// <summary>
    /// 원시 타입과 UnityEngine의 자주 쓰는 값 타입을 JSON과 주고받는 코덱.
    /// 복잡한 오브젝트(Sprite, Material 등)는 AssetPath 또는 instanceId 표기 지원.
    /// </summary>
    internal static class ValueCodec
    {
        public static object Decode(string valueJson, string valueTypeName)
        {
            var t = TypeResolver.Resolve(valueTypeName);
            if (t == null) throw new ArgumentException($"Type not found: {valueTypeName}");

            // 원시/enum/string
            if (t == typeof(string))  return TrimQuotes(valueJson);
            if (t == typeof(bool))    return bool.Parse(valueJson);
            if (t == typeof(int))     return int.Parse(valueJson);
            if (t == typeof(long))    return long.Parse(valueJson);
            if (t == typeof(float))   return float.Parse(valueJson);
            if (t == typeof(double))  return double.Parse(valueJson);
            if (t.IsEnum)             return Enum.Parse(t, TrimQuotes(valueJson), ignoreCase: true);

            // UnityEngine 값 타입은 JsonUtility가 지원
            if (t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) ||
                t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Rect) || t == typeof(Bounds))
                return JsonUtility.FromJson(valueJson, t);

            // UnityEngine.Object 참조: {"assetPath":"..."} 또는 {"instanceId":123}
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                var refSpec = JsonUtility.FromJson<ObjectRef>(valueJson);
                if (!string.IsNullOrEmpty(refSpec.assetPath))
                {
                    var asset = AssetDatabase.LoadAssetAtPath(refSpec.assetPath, t);
                    if (asset == null) throw new ArgumentException($"Asset not found or wrong type: {refSpec.assetPath}");
                    return asset;
                }
                if (refSpec.instanceId != 0)
                {
                    var obj = EditorUtility.InstanceIDToObject(refSpec.instanceId);
                    if (obj == null) throw new ArgumentException($"Instance not found: {refSpec.instanceId}");
                    return obj;
                }
                return null; // 참조 제거
            }

            // 마지막 폴백: JsonUtility에게 떠넘김 (struct/class with [Serializable])
            return JsonUtility.FromJson(valueJson, t);
        }

        public static string Encode(object value, Type declaredType)
        {
            if (value == null) return "null";
            var t = declaredType ?? value.GetType();

            if (t == typeof(string))  return $"\"{value}\"";
            if (t == typeof(bool) || t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double))
                return value.ToString();
            if (t.IsEnum) return $"\"{value}\"";

            if (t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) ||
                t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Rect) || t == typeof(Bounds))
                return JsonUtility.ToJson(value);

            if (value is UnityEngine.Object uo)
            {
                var assetPath = AssetDatabase.GetAssetPath(uo);
                return JsonUtility.ToJson(new ObjectRef { assetPath = assetPath, instanceId = uo.GetInstanceID() });
            }

            try { return JsonUtility.ToJson(value); }
            catch { return $"\"(unencodable: {t.FullName})\""; }
        }

        static string TrimQuotes(string s)
        {
            if (s == null) return null;
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2);
            return s;
        }

        [Serializable]
        class ObjectRef { public string assetPath; public int instanceId; }
    }
}
