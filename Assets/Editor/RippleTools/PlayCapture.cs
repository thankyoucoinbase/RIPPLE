using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RippleTools
{
    /// Editor-only test harness (not part of the shipped game). Opens a scene, enters Play mode offline, lets the
    /// game's own scripts build the runtime scene for a few seconds, then logs every error/exception and renders the
    /// active cameras to PNG. Writes Recovery/playtest/<scene>/report.txt and *.png, then exits the editor.
    /// Batchmode (no -nographics, so cameras render):
    ///   -executeMethod RippleTools.PlayCapture.Run -captureScene Assets/Shallows/Scenes/Test3_FullRun.unity -captureSeconds 12
    public static class PlayCapture
    {
        const string StateKey = "RippleTools.PlayCapture";
        static readonly List<string> Logs = new List<string>();
        static double startTime;
        static string outDir;
        static float seconds;

        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs();
            string scene = Arg(args, "-captureScene", "Assets/Shallows/Scenes/Test3_FullRun.unity");
            seconds = float.Parse(Arg(args, "-captureSeconds", "12"), System.Globalization.CultureInfo.InvariantCulture);
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Recovery", "playtest",
                Path.GetFileNameWithoutExtension(scene)));
            Directory.CreateDirectory(outDir);
            SessionState.SetString(StateKey, outDir + "|" + seconds);
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            var state = SessionState.GetString(StateKey, "");
            if (string.IsNullOrEmpty(state)) return;
            var parts = state.Split('|');
            outDir = parts[0];
            seconds = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    Logs.Add($"[{type}] {msg}\n{FirstLines(stack, 4)}");
                else if (msg.StartsWith("[OfflineGuard]"))
                    Logs.Add($"[Guard] {msg}");
            };
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode) startTime = EditorApplication.timeSinceStartup;
            };
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || startTime == 0) return;
            if (EditorApplication.timeSinceStartup - startTime < seconds) return;
            EditorApplication.update -= Tick;
            Capture();
            SessionState.EraseString(StateKey);
            EditorApplication.Exit(0);
        }

        static void Capture()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"scene ran {seconds}s in Play mode; frame {Time.frameCount}; time {Time.time:F1}");
            var distinct = Logs.GroupBy(l => l.Split('\n')[0]).Select(g => $"x{g.Count()} {g.First()}").ToList();
            report.AppendLine($"errors/exceptions/guard lines: {Logs.Count} ({distinct.Count} distinct)");
            foreach (var d in distinct) report.AppendLine(d);

            int n = 0;
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled && c.gameObject.activeInHierarchy))
            {
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var old = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = old;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                var file = Path.Combine(outDir, $"cam{n++}_{Safe(cam.name)}.png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                report.AppendLine($"camera {cam.name} at {cam.transform.position} -> {Path.GetFileName(file)}");
                Object.DestroyImmediate(rt);
            }
            // Extra views from the scene's own markers: every child of "Checkpoints" at eye height, and one framing
            // the Banana Shop (for comparing with the store screenshots).
            var main = Camera.main;
            if (main != null)
            {
                var views = new List<(string, Vector3, Quaternion)>();
                var cps = GameObject.Find("Checkpoints");
                if (cps != null)
                    foreach (Transform t in cps.transform)
                        views.Add(("cp_" + Safe(t.name), t.position + Vector3.up * 1.6f, t.rotation));
                var shop = GameObject.Find("Banana Shop");
                if (shop != null)
                {
                    var p = shop.transform.position - shop.transform.forward * 2.6f + Vector3.up * 1.6f;  // the shop faces -forward
                    views.Add(("banana_shop", p, Quaternion.LookRotation(shop.transform.position + Vector3.up * 1.2f - p)));
                }
                var go = new GameObject("PlayCapture View");
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(main);
                cam.rect = new Rect(0, 0, 1, 1);
                foreach (var (name, pos, rot) in views)
                {
                    go.transform.SetPositionAndRotation(pos, rot);
                    var file = Path.Combine(outDir, $"view_{name}.png");
                    Shoot(cam, file);
                    report.AppendLine($"view {name} at {pos} -> {Path.GetFileName(file)}");
                }
                Object.DestroyImmediate(go);
            }
            report.AppendLine("root objects: " + string.Join(", ",
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.name)));
            File.WriteAllText(Path.Combine(outDir, "report.txt"), report.ToString());
        }

        static void Shoot(Camera cam, string file)
        {
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(rt);
        }

        static string Arg(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return fallback;
        }

        static string FirstLines(string s, int n) => string.Join("\n", (s ?? "").Split('\n').Take(n));

        static string Safe(string s) => string.Concat(s.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
    }
}
