using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RippleTools
{
    /// Calls every guarded connect entry point by reflection and records whether the
    /// [OfflineGuard] blocked it. Writes Recovery/offline-guard-check.txt.
    /// Batchmode: -executeMethod RippleTools.OfflineGuardCheck.Run
    public static class OfflineGuardCheck
    {
        public static void Run()
        {
            var log = new List<string>();
            Application.LogCallback hook = (msg, _, type) => { if (msg.StartsWith("[OfflineGuard]")) log.Add(msg); };
            Application.logMessageReceived += hook;
            var report = new StringBuilder();
            report.AppendLine($"RIPPLE_ALLOW_ONLINE={Environment.GetEnvironmentVariable("RIPPLE_ALLOW_ONLINE") ?? "(unset)"}");
            var all = AppDomain.CurrentDomain.GetAssemblies();
            Type T(string name) => all.Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            void Check(string label, Func<object> call, Func<object, bool> blocked)
            {
                int before = log.Count;
                try
                {
                    var r = call();
                    bool warned = log.Skip(before).Any(m => m.Contains(label));
                    report.AppendLine($"{(warned && blocked(r) ? "BLOCKED" : "NOT BLOCKED")}  {label}  (result {r ?? "null"})");
                }
                catch (Exception e)
                {
                    report.AppendLine($"ERROR  {label}: {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message}");
                }
            }

            // The guard is the first statement, so an uninitialised client is enough to reach it.
            var rc = T("Photon.Realtime.RealtimeClient");
            var appSettings = T("Photon.Realtime.AppSettings");
            object Client() => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rc);
            Check("Photon ConnectUsingSettings",
                () => rc.GetMethod("ConnectUsingSettings", any).Invoke(Client(), new[] { Activator.CreateInstance(appSettings) }),
                r => r is bool b && !b);
            Check("Photon ReconnectToMaster",
                () => rc.GetMethod("ReconnectToMaster", any).Invoke(Client(), null),
                r => r is bool b && !b);
            Check("Photon ReconnectAndRejoin",
                () => rc.GetMethod("ReconnectAndRejoin", any).Invoke(Client(), new object[] { null }),
                r => r is bool b && !b);

            string failed = null;
            Check("PlayFab Call",
                () => T("Shallows.PlayFabClient").GetMethod("Call", any).Invoke(null,
                    new object[] { "Client/LoginWithCustomID", new Dictionary<string, object>(), false, null, (Action<string>)(e => failed = e) }),
                r => failed == "offline" && (r == null || !((IEnumerator)r).MoveNext()));

            Check("Meta Platform init",
                () => { T("Shallows.MetaPlatform").GetMethod("Begin", any).Invoke(null, null); return "returned"; },
                r => true);

            var go = new GameObject("OfflineGuardCheck") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var nmType = T("Shallows.NetworkManager");
                var nm = go.AddComponent(nmType);
                Check("Fusion StartRunner",
                    () => nmType.GetMethod("StartRunner", any).Invoke(nm, new object[] { "TEST" }),
                    r => r is Task<bool> t && t.IsCompleted && !t.Result);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }

            Application.logMessageReceived -= hook;
            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Recovery", "offline-guard-check.txt"));
            File.WriteAllText(outPath, report.ToString());
            Debug.Log("[OfflineGuardCheck]\n" + report);
        }
    }
}
