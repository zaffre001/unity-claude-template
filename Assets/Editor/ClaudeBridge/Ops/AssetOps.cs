using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// AssetDatabase 래퍼. 주로 Refresh와 Prefab 생성.
    /// </summary>
    public static class AssetOps
    {
        public static string Refresh(string argsJson)
        {
            AssetDatabase.Refresh();
            return JsonUtility.ToJson(new AssetRefreshResult { refreshed = true });
        }

        public static string CreatePrefab(string argsJson)
        {
            var a = JsonUtility.FromJson<AssetCreatePrefabArgs>(argsJson);
            var go = GameObjectPath.Find(a.goPath) ?? throw new ArgumentException($"GameObject not found: {a.goPath}");

            var dir = Path.GetDirectoryName(a.prefabPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                CreateFolderRecursive(dir);

            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, a.prefabPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new Exception("Prefab creation failed");
            return JsonUtility.ToJson(new AssetCreatePrefabResult { prefabPath = a.prefabPath });
        }

        static void CreateFolderRecursive(string assetRelDir)
        {
            var parts = assetRelDir.Replace('\\', '/').Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
