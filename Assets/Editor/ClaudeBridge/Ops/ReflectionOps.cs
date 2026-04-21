using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// 전용 op으로 표현 안 되는 호출을 위한 탈출구.
    /// AssetDatabase.Refresh(), Selection.activeObject 등 에디터 API 전반을 이걸로 커버.
    /// 파괴적 API라 위험 — 튜토리얼 독자는 전용 op을 먼저 쓰도록 README에 명시.
    /// </summary>
    public static class ReflectionOps
    {
        public static string Invoke(string argsJson)
        {
            var a = JsonUtility.FromJson<ReflectionInvokeArgs>(argsJson);

            var type = TypeResolver.Resolve(a.typeName) ?? throw new ArgumentException($"Type not found: {a.typeName}");
            var argTypes = (a.argTypes ?? Array.Empty<string>())
                .Select(n => TypeResolver.Resolve(n) ?? throw new ArgumentException($"Arg type not found: {n}"))
                .ToArray();

            var method = type.GetMethod(a.methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                binder: null, types: argTypes, modifiers: null);
            if (method == null)
                throw new ArgumentException($"Method not found: {a.typeName}.{a.methodName}({string.Join(", ", argTypes.Select(t => t.Name))})");

            object target = null;
            if (!method.IsStatic)
            {
                if (!int.TryParse(a.targetInstanceId, out var iid) || iid == 0)
                    throw new ArgumentException("Non-static method requires targetInstanceId");
                target = EditorUtility.InstanceIDToObject(iid)
                         ?? throw new ArgumentException($"Instance not found: {iid}");
            }

            var argValues = new object[argTypes.Length];
            for (int i = 0; i < argTypes.Length; i++)
                argValues[i] = ValueCodec.Decode(a.argsJson[i], argTypes[i].FullName);

            var ret = method.Invoke(target, argValues);
            var retType = method.ReturnType;
            return JsonUtility.ToJson(new ReflectionInvokeResult
            {
                returnJson = (retType == typeof(void)) ? "" : ValueCodec.Encode(ret, retType),
                returnType = retType.FullName,
            });
        }
    }
}
