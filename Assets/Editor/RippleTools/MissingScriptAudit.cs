using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RippleTools
{
    /// Counts missing MonoBehaviours in every scene, prefab and ScriptableObject asset and
    /// writes the totals plus each broken script GUID to Recovery/missing-scripts.txt.
    /// Batchmode: -executeMethod RippleTools.MissingScriptAudit.Run
    public static class MissingScriptAudit
    {
        [MenuItem("Tools/Ripple/Missing Script Audit")]
        public static void Run()
        {
            var report = new StringBuilder();
            var guids = new Dictionary<string, int>();
            int prefabMissing = 0, sceneMissing = 0, assetMissing = 0;

            foreach (var path in Find("t:Prefab"))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                int n = CountMissing(root);
                if (n > 0) { prefabMissing += n; report.AppendLine($"prefab {path}: {n}"); CollectGuids(path, guids); }
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (var path in Find("t:Scene"))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = 0;
                foreach (var go in scene.GetRootGameObjects()) n += CountMissing(go);
                if (n > 0) { sceneMissing += n; report.AppendLine($"scene {path}: {n}"); CollectGuids(path, guids); }
            }

            foreach (var path in Find("t:ScriptableObject"))
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(MonoBehaviour))
                {
                    assetMissing++;
                    report.AppendLine($"asset {path}");
                    CollectGuids(path, guids);
                }
            }

            var head = new StringBuilder();
            head.AppendLine($"missing in prefabs: {prefabMissing}, scenes: {sceneMissing}, assets: {assetMissing}");
            foreach (var kv in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(kv.Key);
                if (string.IsNullOrEmpty(p) || AssetDatabase.LoadAssetAtPath<MonoScript>(p)?.GetClass() == null)
                    head.AppendLine($"  unresolved script guid {kv.Key} ({(string.IsNullOrEmpty(p) ? "no asset" : p)}) x{kv.Value}");
            }
            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Recovery", "missing-scripts.txt"));
            File.WriteAllText(outPath, head.ToString() + report);
            Debug.Log($"[MissingScriptAudit] {head.ToString().Split('\n')[0]} -> {outPath}");
        }

        static IEnumerable<string> Find(string filter)
        {
            foreach (var g in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
                yield return AssetDatabase.GUIDToAssetPath(g);
        }

        static int CountMissing(GameObject root)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                n += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            return n;
        }

        // Script references in the YAML whose GUID has no compiled class behind it.
        static void CollectGuids(string path, Dictionary<string, int> guids)
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full)) return;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
                         File.ReadAllText(full), @"m_Script: \{fileID: (-?\d+), guid: ([0-9a-f]{32})"))
            {
                var g = m.Groups[2].Value;
                var p = AssetDatabase.GUIDToAssetPath(g);
                bool ok = !string.IsNullOrEmpty(p) && (p.EndsWith(".dll") || AssetDatabase.LoadAssetAtPath<MonoScript>(p)?.GetClass() != null);
                if (!ok) guids[g] = guids.TryGetValue(g, out var c) ? c + 1 : 1;
            }
        }
    }
}
