using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace RippleTools
{
	/// Reads the weaved Shallows.NetAvatar by reflection and compares it with the shipped weave facts
	/// (Rebuild/net/shipped_weave.json): NetworkBehaviourWeaved word count, each [Networked] property's
	/// NetworkedWeaved offset/size, every RPC's Key, attribute arguments and RpcPayload sizes. Also loads the
	/// global NetworkProjectConfig and lists its prefab table. Writes Rebuild/net/weave_check.txt.
	/// Batchmode: -executeMethod RippleTools.FusionWeaveCheck.Run
	public static class FusionWeaveCheck
	{
		const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		public static void Run()
		{
			string net = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Rebuild/net"));
			string shipped = File.ReadAllText(Path.Combine(net, "shipped_weave.json"));
			var report = new StringBuilder();
			int mismatches = 0;

			void Row(string what, string expected, string actual)
			{
				bool ok = expected == actual;
				if (!ok)
				{
					mismatches++;
				}
				report.AppendLine($"{(ok ? "OK      " : "MISMATCH")}  {what,-34} shipped {expected,-28} ours {actual}");
			}

			var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Shallows.NetAvatar")).FirstOrDefault(t => t != null);
			Row("Assembly-CSharp NetworkAssemblyWeaved", "True", type.Assembly.IsDefined(typeof(NetworkAssemblyWeavedAttribute)).ToString());
			Row("NetworkBehaviourWeaved word count", Regex.Match(shipped, "\"wordCount\": (\\d+)").Groups[1].Value,
				type.GetCustomAttribute<NetworkBehaviourWeavedAttribute>()?.WordCount.ToString() ?? "(none)");

			var shippedProps = Regex.Matches(shipped, "\"name\": \"(\\w+)\",\\s*\"type\": \"([^\"]+)\",\\s*\"access\": \"\\w+\",\\s*\"offset\": (\\d+),\\s*\"size\": (\\d+)");
			var ours = type.GetProperties(Declared).Where(p => p.IsDefined(typeof(NetworkedWeavedAttribute))).ToList();
			Row("networked property count", shippedProps.Count.ToString(), ours.Count.ToString());
			for (int i = 0; i < shippedProps.Count; i++)
			{
				var m = shippedProps[i];
				var p = i < ours.Count ? ours[i] : null;
				var w = p?.GetCustomAttribute<NetworkedWeavedAttribute>();
				Row($"[{i}] property", $"{m.Groups[1].Value} {m.Groups[2].Value}", p == null ? "(none)" : $"{p.Name} {Short(p.PropertyType)}");
				Row($"[{i}] {m.Groups[1].Value} NetworkedWeaved", $"({m.Groups[3].Value}, {m.Groups[4].Value})", w == null ? "(none)" : $"({w.WordOffset}, {w.WordCount})");
			}
			var backing = type.GetFields(Declared).Select(f => f.GetCustomAttribute<DefaultForPropertyAttribute>()).Where(a => a != null).ToList();
			foreach (Match b in Regex.Matches(shipped, "\"property\": \"(\\w+)\",\\s*\"offset\": (\\d+),\\s*\"size\": (\\d+)"))
			{
				var a = backing.FirstOrDefault(x => x.PropertyName == b.Groups[1].Value);
				Row($"DefaultForProperty {b.Groups[1].Value}", $"({b.Groups[2].Value}, {b.Groups[3].Value})", a == null ? "(none)" : $"({a.WordOffset}, {a.WordCount})");
			}

			var invokers = type.GetMethods(Declared).Select(m => m.GetCustomAttribute<NetworkRpcWeavedInvokerAttribute>()).Where(a => a != null).Select(a => a.Key).ToHashSet();
			foreach (Match r in Regex.Matches(shipped, "\"name\": \"(Rpc_\\w+)\",[^{]*?\"key\": (\\d+),\\s*\"args\": \\{([^}]*)\\},\\s*\"parameters\": \\[(.*?)\\]\\s*\\}", RegexOptions.Singleline))
			{
				string name = r.Groups[1].Value;
				var method = type.GetMethod(name, Declared);
				var rpc = method?.GetCustomAttribute<RpcAttribute>();
				Row($"{name} Key", r.Groups[2].Value, rpc == null ? "(none)" : rpc.Key.ToString());
				Row($"{name} invoker", "True", (rpc != null && invokers.Contains(rpc.Key)).ToString());
				string args = string.Join(", ", Regex.Matches(r.Groups[3].Value, "\"(\\w+)\": \"([^\"]+)\"").Cast<Match>()
					.Select(a => $"{a.Groups[1].Value}={a.Groups[2].Value.Replace("RpcChannel.", "")}"));
				string ourArgs = rpc == null ? "(none)" : string.Join(", ", new[]
				{
					rpc.Channel != RpcChannel.Reliable ? $"Channel={rpc.Channel}" : null,
					rpc.InvokeLocal ? null : "InvokeLocal=false",
				}.Where(s => s != null));
				Row($"{name} [Rpc] args", args, ourArgs);
				string payload = string.Join(" ", Regex.Matches(r.Groups[4].Value, "\"attrs\": \"([^\"]*)\"").Cast<Match>().Select(a => a.Groups[1].Value == "" ? "-" : a.Groups[1].Value));
				string ourPayload = method == null ? "(none)" : string.Join(" ", method.GetParameters().Select(p =>
				{
					var a = p.GetCustomAttribute<RpcPayloadAttribute>();
					return a == null ? "-" : a.ByteCount > 0 ? $"[RpcPayload({a.ByteCount})]" : "[RpcPayload]";
				}));
				Row($"{name} RpcPayload", payload, ourPayload);
			}

			report.AppendLine();
			report.AppendLine("Prefab table (NetworkProjectConfig.Global):");
			try
			{
				NetworkProjectConfigAsset.UnloadGlobal();
				var table = NetworkProjectConfig.Global.PrefabTable;
				foreach (var (id, source) in table.GetEntries())
				{
					string guid = source.AssetGuid.ToUnityGuidString();
					string path = AssetDatabase.GUIDToAssetPath(guid);
					bool match = path.EndsWith(".prefab") && shipped.Contains(guid);
					report.AppendLine($"  {id}  {source.GetType().Name}  {guid}  {(match ? "OK" : "MISMATCH")}  {path}");
					if (!match)
					{
						mismatches++;
					}
				}
				report.AppendLine($"  {table.Prefabs.Count} prefabs, loaded without exceptions");
			}
			catch (Exception e)
			{
				mismatches++;
				report.AppendLine($"  ERROR {e.GetType().Name}: {e.Message}");
			}

			report.Insert(0, $"Fusion weave check: {mismatches} mismatches{Environment.NewLine}{Environment.NewLine}");
			File.WriteAllText(Path.Combine(net, "weave_check.txt"), report.ToString());
			Debug.Log($"[FusionWeaveCheck] {mismatches} mismatches");
		}

		static string Short(Type t)
		{
			if (!t.IsGenericType)
			{
				return t.Name switch { "Single" => "float", "Int32" => "int", _ => t.Name };
			}
			return $"{t.Name.Substring(0, t.Name.IndexOf('`'))}<{string.Join(", ", t.GetGenericArguments().Select(Short))}>";
		}
	}
}
