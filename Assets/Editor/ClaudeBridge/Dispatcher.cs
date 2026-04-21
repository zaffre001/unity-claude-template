using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Editor.ClaudeBridge
{
    /// <summary>
    /// op 문자열 → 핸들러 함수 매핑. 새 op 추가는 여기에 한 줄.
    /// 핸들러 시그니처: argsJson(string) → resultJson(string).
    /// </summary>
    public static class Dispatcher
    {
        static readonly Dictionary<string, Func<string, string>> _handlers = new()
        {
            // Scene
            ["Scene.New"]   = Ops.SceneOps.New,
            ["Scene.Open"]  = Ops.SceneOps.Open,
            ["Scene.Save"]  = Ops.SceneOps.Save,

            // GameObject
            ["GameObject.Create"]       = Ops.GameObjectOps.Create,
            ["GameObject.Find"]         = Ops.GameObjectOps.Find,
            ["GameObject.Delete"]       = Ops.GameObjectOps.Delete,
            ["GameObject.SetTransform"] = Ops.GameObjectOps.SetTransform,

            // Component
            ["Component.Add"]              = Ops.ComponentOps.Add,
            ["Component.SetField"]         = Ops.ComponentOps.SetField,
            ["Component.GetField"]         = Ops.ComponentOps.GetField,
            ["Component.SetRectTransform"] = Ops.ComponentOps.SetRectTransform,

            // Reflection (탈출구)
            ["Reflection.Invoke"] = Ops.ReflectionOps.Invoke,

            // Asset
            ["Asset.Refresh"]      = Ops.AssetOps.Refresh,
            ["Asset.CreatePrefab"] = Ops.AssetOps.CreatePrefab,

            // Prefab (stage-mode + 중첩 인스턴스)
            ["Prefab.Open"]               = Ops.PrefabOps.Open,
            ["Prefab.Save"]               = Ops.PrefabOps.Save,
            ["Prefab.Close"]              = Ops.PrefabOps.Close,
            ["Prefab.GetCurrent"]         = Ops.PrefabOps.GetCurrent,
            ["Prefab.InstantiateAsChild"] = Ops.PrefabOps.InstantiateAsChild,
            ["Prefab.Apply"]              = Ops.PrefabOps.Apply,
            ["Prefab.Unpack"]             = Ops.PrefabOps.Unpack,
            ["Prefab.CreateVariant"]      = Ops.PrefabOps.CreateVariant,
        };

        public static string Dispatch(string op, string argsJson)
        {
            if (!_handlers.TryGetValue(op, out var handler))
                throw new ArgumentException($"Unknown op: {op}. Known: {string.Join(", ", _handlers.Keys)}");
            return handler(argsJson ?? "{}");
        }
    }
}
