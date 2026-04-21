using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.Editor.ClaudeBridge.Ops
{
    /// <summary>
    /// 프리팹 스테이지(격리 편집 모드) + 중첩 프리팹 인스턴스 조작.
    ///
    /// 일반적인 UGUI 중첩 프리팹 작업 흐름:
    ///   1) <see cref="Open"/>        — Card.prefab을 격리 모드로 연다
    ///   2) GameObject.Create / Component.Add / Component.SetRectTransform — 프리팹 내부 편집
    ///   3) Prefab.InstantiateAsChild — 더 작은 프리팹(예: Suit 아이콘)을 Card 안에 중첩
    ///   4) <see cref="Save"/>
    ///   5) <see cref="Close"/>
    ///
    /// 스테이지가 열린 동안 GameObject/Component op들의 경로는 자동으로 프리팹 루트 아래에서 해석된다
    /// (GameObjectPath.Find 가 현재 스테이지를 인식).
    /// </summary>
    public static class PrefabOps
    {
        public static string Open(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabOpenArgs>(argsJson);
            if (string.IsNullOrEmpty(a.path)) throw new ArgumentException("path required");

            var stage = PrefabStageUtility.OpenPrefab(a.path);
            if (stage == null) throw new Exception($"Failed to open prefab stage: {a.path}");

            var root = stage.prefabContentsRoot;
            return JsonUtility.ToJson(new PrefabOpenResult
            {
                rootPath = GameObjectPath.FullPath(root),
                rootInstanceId = root.GetInstanceID(),
                prefabPath = stage.assetPath,
            });
        }

        public static string Save(string argsJson)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage()
                ?? throw new InvalidOperationException("No prefab stage is currently open. Call Prefab.Open first.");

            bool ok = PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            if (!ok) throw new Exception($"SaveAsPrefabAsset failed: {stage.assetPath}");

            return JsonUtility.ToJson(new PrefabSaveResult { prefabPath = stage.assetPath });
        }

        public static string Close(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabCloseArgs>(argsJson);
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return JsonUtility.ToJson(new PrefabCloseResult { closed = false, saved = false });

            bool saved = false;
            if (a.save)
            {
                saved = PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                if (!saved) throw new Exception($"Auto-save before close failed: {stage.assetPath}");
            }

            StageUtility.GoToMainStage();
            return JsonUtility.ToJson(new PrefabCloseResult { closed = true, saved = saved });
        }

        public static string GetCurrent(string argsJson)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                return JsonUtility.ToJson(new PrefabGetCurrentResult { isOpen = false });

            var root = stage.prefabContentsRoot;
            return JsonUtility.ToJson(new PrefabGetCurrentResult
            {
                isOpen = true,
                prefabPath = stage.assetPath,
                rootPath = GameObjectPath.FullPath(root),
                rootInstanceId = root.GetInstanceID(),
            });
        }

        /// <summary>
        /// 중첩 프리팹 인스턴스 생성. <see cref="PrefabUtility.InstantiatePrefab"/>을 쓰므로
        /// 원본 프리팹과의 링크가 유지되고, 원본 편집 시 인스턴스에 자동 반영된다.
        /// UGUI에서 Card/Button/Suit 같은 재사용 단위를 조립할 때 핵심.
        /// </summary>
        public static string InstantiateAsChild(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabInstantiateArgs>(argsJson);
            if (string.IsNullOrEmpty(a.prefabPath)) throw new ArgumentException("prefabPath required");

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(a.prefabPath)
                ?? throw new ArgumentException($"Prefab not found: {a.prefabPath}");
            var parent = GameObjectPath.Find(a.parentPath)
                ?? throw new ArgumentException($"Parent not found: {a.parentPath}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.transform);
            if (instance == null) throw new Exception($"InstantiatePrefab returned null for {a.prefabPath}");

            if (!string.IsNullOrEmpty(a.name)) instance.name = a.name;

            // RectTransform이라면 anchor 기반 초기값으로 리셋 (UGUI 중첩 시 부모 기준 위치가 꼬이는 것 방지).
            if (instance.transform is RectTransform rt)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
            else
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }

            Undo.RegisterCreatedObjectUndo(instance, "ClaudeBridge.InstantiatePrefab");
            return JsonUtility.ToJson(new PrefabInstantiateResult
            {
                path = GameObjectPath.FullPath(instance),
                instanceId = instance.GetInstanceID(),
            });
        }

        public static string Apply(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabApplyArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");

            if (!PrefabUtility.IsPartOfPrefabInstance(go))
                throw new InvalidOperationException($"{a.path} is not a prefab instance");

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
            return JsonUtility.ToJson(new PrefabApplyResult { applied = true });
        }

        public static string Unpack(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabUnpackArgs>(argsJson);
            var go = GameObjectPath.Find(a.path) ?? throw new ArgumentException($"GameObject not found: {a.path}");

            if (!PrefabUtility.IsPartOfPrefabInstance(go))
                return JsonUtility.ToJson(new PrefabUnpackResult { unpacked = false });

            var mode = a.completely ? PrefabUnpackMode.Completely : PrefabUnpackMode.OutermostRoot;
            PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.AutomatedAction);
            return JsonUtility.ToJson(new PrefabUnpackResult { unpacked = true });
        }

        /// <summary>
        /// Variant 생성. 구현은 소스 프리팹을 임시 인스턴스화 → 그 인스턴스를 variantPath로 SaveAsPrefabAsset.
        /// Instance는 원본 프리팹 링크를 유지하고 있으므로 저장 결과가 자동으로 "variant"가 된다.
        /// 임시 인스턴스는 씬에 잠깐 떠 있다가 삭제하지만 Undo에 기록해서 사용자가 눈치 못 채게.
        /// </summary>
        public static string CreateVariant(string argsJson)
        {
            var a = JsonUtility.FromJson<PrefabCreateVariantArgs>(argsJson);
            if (string.IsNullOrEmpty(a.sourcePath))  throw new ArgumentException("sourcePath required");
            if (string.IsNullOrEmpty(a.variantPath)) throw new ArgumentException("variantPath required");

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(a.sourcePath)
                ?? throw new ArgumentException($"Source prefab not found: {a.sourcePath}");

            EnsureFolder(Path.GetDirectoryName(a.variantPath));

            var temp = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var variant = PrefabUtility.SaveAsPrefabAsset(temp, a.variantPath);
                if (variant == null) throw new Exception($"SaveAsPrefabAsset returned null for {a.variantPath}");
                if (!PrefabUtility.IsPartOfVariantPrefab(variant))
                    Debug.LogWarning($"[ClaudeBridge] {a.variantPath} was saved but is not a Variant — source may not be a prefab asset.");
            }
            finally
            {
                // 씬에 남겨두지 않는다. Undo로도 깔끔히 말소.
                UnityEngine.Object.DestroyImmediate(temp);
            }

            return JsonUtility.ToJson(new PrefabCreateVariantResult { variantPath = a.variantPath });
        }

        static void EnsureFolder(string assetRelDir)
        {
            if (string.IsNullOrEmpty(assetRelDir)) return;
            if (AssetDatabase.IsValidFolder(assetRelDir)) return;
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
