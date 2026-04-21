using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// GameObject 생성/검색/삭제/Transform 변경.
    /// 경로 규약은 Hierarchy full path (예: "/Canvas/Panel/Button"). 루트는 슬래시로 시작.
    ///
    /// 프리팹 스테이지가 열려 있으면 경로는 스테이지의 prefabContentsRoot 아래에서 해석된다.
    /// 이 덕에 Claude는 씬 편집과 프리팹 편집을 같은 op 집합으로 처리할 수 있다.
    /// </summary>
    public static class GameObjectOps
    {
        public static string Create(string argsJson)
        {
            var a = JsonUtility.FromJson<GoCreateArgs>(argsJson);
            var go = new GameObject(string.IsNullOrEmpty(a.name) ? "GameObject" : a.name);

            GameObject parent = null;
            if (!string.IsNullOrEmpty(a.parentPath))
            {
                parent = GameObjectPath.Find(a.parentPath)
                    ?? throw new ArgumentException($"Parent not found: {a.parentPath}");
            }
            else
            {
                // 프리팹 스테이지가 열려 있으면 루트 생성은 불가(스테이지는 단일 루트).
                // parentPath 미지정은 스테이지 루트 아래에 붙이는 것으로 해석.
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null) parent = stage.prefabContentsRoot;
            }

            if (parent != null)
            {
                Undo.SetTransformParent(go.transform, parent.transform, "ClaudeBridge.Create");
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                // 씬 루트 신규 오브젝트. 현재 활성 씬에 그대로 남음.
            }

            Undo.RegisterCreatedObjectUndo(go, "ClaudeBridge.Create");
            return JsonUtility.ToJson(new GoCreateResult
            {
                path = GameObjectPath.FullPath(go),
                instanceId = go.GetInstanceID(),
            });
        }

        public static string Find(string argsJson)
        {
            var a = JsonUtility.FromJson<GoFindArgs>(argsJson);
            var go = GameObjectPath.Find(a.path);
            return JsonUtility.ToJson(new GoFindResult
            {
                found = go != null,
                instanceId = go != null ? go.GetInstanceID() : 0,
            });
        }

        public static string Delete(string argsJson)
        {
            var a = JsonUtility.FromJson<GoDeleteArgs>(argsJson);
            var go = GameObjectPath.Find(a.path);
            if (go == null) return JsonUtility.ToJson(new GoDeleteResult { deleted = false });
            Undo.DestroyObjectImmediate(go);
            return JsonUtility.ToJson(new GoDeleteResult { deleted = true });
        }

        public static string SetTransform(string argsJson)
        {
            var a = JsonUtility.FromJson<GoSetTransformArgs>(argsJson);
            var go = GameObjectPath.Find(a.path);
            if (go == null) throw new ArgumentException($"GameObject not found: {a.path}");

            Undo.RecordObject(go.transform, "ClaudeBridge.SetTransform");
            if (a.position != null && a.position.Length == 3)
                go.transform.localPosition = new Vector3(a.position[0], a.position[1], a.position[2]);
            if (a.rotation != null && a.rotation.Length == 3)
                go.transform.localEulerAngles = new Vector3(a.rotation[0], a.rotation[1], a.rotation[2]);
            if (a.scale != null && a.scale.Length == 3)
                go.transform.localScale = new Vector3(a.scale[0], a.scale[1], a.scale[2]);

            return JsonUtility.ToJson(new GoSetTransformResult { path = GameObjectPath.FullPath(go) });
        }
    }

    /// <summary>
    /// 경로 유틸. "/A/B/C" 형식과 GameObject 간 변환.
    /// 프리팹 스테이지가 열려 있으면 검색 범위가 자동으로 스테이지 루트로 스위치됨.
    /// </summary>
    internal static class GameObjectPath
    {
        public static GameObject Find(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            var path = fullPath.StartsWith("/") ? fullPath.Substring(1) : fullPath;
            var parts = path.Split('/');

            // 검색 루트 결정: 프리팹 스테이지가 열렸으면 그 루트 하나, 아니면 활성 씬의 루트들.
            GameObject[] roots;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null) roots = new[] { stage.prefabContentsRoot };
            else               roots = SceneManager.GetActiveScene().GetRootGameObjects();

            GameObject current = null;
            foreach (var r in roots)
                if (r != null && r.name == parts[0]) { current = r; break; }

            if (current == null) return null;
            for (int i = 1; i < parts.Length; i++)
            {
                var child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }
            return current;
        }

        public static string FullPath(GameObject go)
        {
            if (go == null) return null;
            var t = go.transform;
            var path = "/" + go.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = "/" + t.name + path;
            }
            return path;
        }
    }
}
