using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Shallows
{
	public static class LookCheck
	{
		private static readonly Regex PlayFabIdPattern = new Regex("^[0-9A-Fa-f]{6,24}$");

		private const float BatchEvery = 1f;

		private const float RecheckAfter = 15f;

		private const float RetryAfter = 10f;

		private const int MaxPerCall = 10;

		private static readonly Dictionary<string, HashSet<string>> owned = new Dictionary<string, HashSet<string>>();

		private static readonly Dictionary<string, float> askedAt = new Dictionary<string, float>();

		private static readonly HashSet<string> wanted = new HashSet<string>();

		private static bool asking;

		private static float lastCall = -999f;

		public static int Version { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000904, RVA 0x272D8BC)
			owned.Clear();
			askedAt.Clear();
			wanted.Clear();
			asking = false;
			lastCall = -999f;
			Version = 0;
		}

		public static bool Allowed(string userId, string cosmetic)
		{
			// Rebuilt from native code (token 0x06000905, RVA 0x272D9E0)
			if (string.IsNullOrEmpty(cosmetic) || !PlayFabClient.LoggedIn)
			{
				return true;
			}
			if (string.IsNullOrEmpty(userId) || !PlayFabIdPattern.IsMatch(userId))
			{
				return false;
			}
			if (userId == PlayFabClient.PlayFabId)
			{
				return true;
			}
			bool known = owned.TryGetValue(userId, out HashSet<string> cosmetics);
			if (known && cosmetics.Contains(cosmetic))
			{
				return true;
			}
			float now = Time.unscaledTime;
			if (!askedAt.TryGetValue(userId, out float at) || now - at >= (known ? RecheckAfter : RetryAfter))
			{
				wanted.Add(userId);
			}
			return false;
		}

		public static void Tick()
		{
			// Rebuilt from native code (token 0x06000906, RVA 0x272DC5C)
			if (asking || wanted.Count == 0 || !PlayFabClient.LoggedIn)
			{
				return;
			}
			float now = Time.unscaledTime;
			if (now - lastCall < BatchEvery)
			{
				return;
			}
			var ids = new List<object>();
			foreach (string id in wanted)
			{
				ids.Add(id);
				if (ids.Count >= MaxPerCall)
				{
					break;
				}
			}
			foreach (string id in ids)
			{
				wanted.Remove(id);
				askedAt[id] = now;
			}
			asking = true;
			lastCall = now;
			var args = new Dictionary<string, object>();
			args["ids"] = ids;
			PlayFabClient.RunScript("looks", args, (result, error) =>
			{
				// Rebuilt from native code (token 0x0600090A, RVA 0x272E4A8)
				asking = false;
				Dictionary<string, object> looks = MiniJson.Obj(result, "looks");
				if (looks == null)
				{
					return;
				}
				foreach (KeyValuePair<string, object> look in looks)
				{
					if (look.Value is List<object> list)
					{
						var cosmetics = new HashSet<string>();
						foreach (object item in list)
						{
							if (item is string cosmetic)
							{
								cosmetics.Add(cosmetic);
							}
						}
						owned[look.Key] = cosmetics;
					}
				}
				Version++;
			});
		}
	}
}
