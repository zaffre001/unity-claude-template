using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// 씬 생성/열기/저장. EditorSceneManager 래핑.
    /// </summary>
    public static class SceneOps
    {
        public static string New(string argsJson)
        {
            var a = JsonUtility.FromJson<SceneNewArgs>(argsJson);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            if (!string.IsNullOrEmpty(a.path))
            {
                EnsureFolder(Path.GetDirectoryName(a.path));
                EditorSceneManager.SaveScene(scene, a.path);
            }
            return JsonUtility.ToJson(new SceneNewResult { scenePath = scene.path });
        }

        public static string Open(string argsJson)
        {
            var a = JsonUtility.FromJson<SceneOpenArgs>(argsJson);
            var scene = EditorSceneManager.OpenScene(a.path, OpenSceneMode.Single);
            return JsonUtility.ToJson(new SceneOpenResult { scenePath = scene.path });
        }

        public static string Save(string argsJson)
        {
            var a = JsonUtility.FromJson<SceneSaveArgs>(argsJson);
            var scene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(a?.path))
                EditorSceneManager.SaveScene(scene, a.path);
            else
                EditorSceneManager.SaveScene(scene);
            return JsonUtility.ToJson(new SceneSaveResult { scenePath = scene.path });
        }

        static void EnsureFolder(string assetRelDir)
        {
            if (string.IsNullOrEmpty(assetRelDir)) return;
            if (AssetDatabase.IsValidFolder(assetRelDir)) return;

            // "Assets/A/B/C" → 재귀적으로 폴더 생성
            var parts = assetRelDir.Replace('\\', '/').Split('/');
            string cur = parts[0]; // "Assets"
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
