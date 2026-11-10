using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Shallows
{
	public static class NetGuard
	{
		public enum Kind
		{
			Splash = 0,
			Tap = 1,
			Throw = 2,
			Caught = 3,
			Escape = 4,
			FuseRequest = 5,
			FuseThrow = 6,
			Pebble = 7,
			Item = 8,
			Tree = 9
		}

		private class Sender
		{
			// Rebuilt from native code (token 0x0600063A, RVA 0x27F354C)
			public readonly float[] tokens = new float[Rate.Length];

			public readonly float[] filled = new float[Rate.Length];

			public int drops;

			public float dropsSince;

			public bool ignored;

			public string why;

			public float lastCaught = -100f;

			public readonly List<Thrown> thrown = new List<Thrown>();
		}

		private struct Thrown
		{
			public Transform transform;

			public Vector3 last;

			public float until;
		}

		// Rebuilt from native code (token 0x06000639, RVA 0x27F4C60)
		private static readonly float[] Rate = new float[] { 20f, 30f, 3f, 0.5f, 0.05f, 8f, 3f, 6f, 8f, 0.5f };

		private static readonly float[] Burst = new float[] { 40f, 50f, 6f, 2f, 2f, 16f, 6f, 12f, 16f, 3f };

		private const int FloodDrops = 150;

		private const float FloodWindow = 5f;

		private const float WorldLimit = 5000f;

		private static readonly Dictionary<int, Sender> senders = new Dictionary<int, Sender>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000627, RVA 0x27F32F0)
			senders.Clear();
		}

		public static void Clear()
		{
			// Rebuilt from native code (token 0x06000628, RVA 0x27F3368)
			senders.Clear();
		}

		private static Sender Of(PlayerRef player)
		{
			// Rebuilt from native code (token 0x06000629, RVA 0x27F33E0)
			if (!senders.TryGetValue(player.PlayerId, out Sender sender))
			{
				sender = senders[player.PlayerId] = new Sender();
			}
			return sender;
		}

		public static bool Allow(NetworkRunner runner, RpcInfo info, Kind kind)
		{
			// Rebuilt from native code (token 0x0600062A, RVA 0x27F3670)
			if (!runner || info.IsInvokeLocal || !info.Source.IsRealPlayer || info.Source == runner.LocalPlayer)
			{
				return false;
			}
			Sender sender = Of(info.Source);
			if (sender.ignored)
			{
				return false;
			}
			float now = Time.unscaledTime;
			int k = (int)kind;
			if (sender.filled[k] <= 0f)
			{
				sender.tokens[k] = Burst[k];
			}
			else
			{
				sender.tokens[k] = Mathf.Min(Burst[k], sender.tokens[k] + (now - sender.filled[k]) * Rate[k]);
			}
			sender.filled[k] = now;
			if (sender.tokens[k] >= 1f)
			{
				sender.tokens[k] -= 1f;
				return true;
			}
			if (now - sender.dropsSince > FloodWindow)
			{
				sender.dropsSince = now;
				sender.drops = 1;
				return false;
			}
			if (++sender.drops >= FloodDrops)
			{
				Ignore(runner, info.Source, "flooding");
			}
			return false;
		}

		public static bool FromMaster(NetworkRunner runner, RpcInfo info)
		{
			// Rebuilt from native code (token 0x0600062B, RVA 0x27F3B4C)
			if (!runner || info.IsInvokeLocal || !info.Source.IsRealPlayer || info.Source == runner.LocalPlayer)
			{
				return false;
			}
			try
			{
				return info.Source == runner.GetMasterClient();
			}
			catch
			{
				return false;
			}
		}

		public static void Ignore(NetworkRunner runner, PlayerRef player, string why)
		{
			// Rebuilt from native code (token 0x0600062C, RVA 0x27F39A4)
			Sender sender = Of(player);
			if (sender.ignored)
			{
				return;
			}
			sender.why = why;
			sender.ignored = true;
			Debug.LogWarning(string.Format("Shallows: ignoring player {0} from now on ({1}).", player.PlayerId, why));
			if (why == "flooding")
			{
				CheatWatch.ReportFlood(runner, player);
			}
		}

		public static bool Ignored(PlayerRef player)
		{
			// Rebuilt from native code (token 0x0600062D, RVA 0x27F3CC8)
			return senders.TryGetValue(player.PlayerId, out Sender sender) && sender.ignored;
		}

		public static NetAvatar AvatarOf(PlayerRef player)
		{
			// Rebuilt from native code (token 0x0600062E, RVA 0x27F3DB4)
			foreach (NetAvatar avatar in NetAvatar.Remotes)
			{
				if (avatar && avatar.Object && avatar.Object.StateAuthority == player)
				{
					return avatar;
				}
			}
			return null;
		}

		public static bool Near(PlayerRef player, Vector3 point, float reach)
		{
			// Rebuilt from native code (token 0x0600062F, RVA 0x27F30C4)
			NetAvatar avatar = AvatarOf(player);
			if (!avatar)
			{
				return false;
			}
			float reach2 = reach * reach;
			return (avatar.HeadPosition - point).sqrMagnitude < reach2
				|| (avatar.LeftPosition - point).sqrMagnitude < reach2
				|| (avatar.RightPosition - point).sqrMagnitude < reach2
				|| (avatar.NetFeet - point).sqrMagnitude < reach2;
		}

		public static bool NearAnythingOf(PlayerRef player, Vector3 point, float reach)
		{
			// Rebuilt from native code (token 0x06000630, RVA 0x27F4050)
			if (Near(player, point, reach))
			{
				return true;
			}
			Sender sender = Of(player);
			float now = Time.time;
			float reach2 = reach * reach;
			for (int i = sender.thrown.Count - 1; i >= 0; i--)
			{
				Thrown thrown = sender.thrown[i];
				if (now > thrown.until)
				{
					sender.thrown.RemoveAt(i);
					continue;
				}
				if (thrown.transform)
				{
					thrown.last = thrown.transform.position;
					sender.thrown[i] = thrown;
				}
				if ((thrown.last - point).sqrMagnitude < reach2)
				{
					return true;
				}
			}
			GorillaRagdoll body = DeathSequence.I ? DeathSequence.I.BodyOf(player.PlayerId) : null;
			return body && (body.Position - point).sqrMagnitude < reach2 * 4f;
		}

		public static void Track()
		{
			// Rebuilt from native code (token 0x06000631, RVA 0x27F43D4)
			foreach (Sender sender in senders.Values)
			{
				for (int i = 0; i < sender.thrown.Count; i++)
				{
					Thrown thrown = sender.thrown[i];
					if (thrown.transform)
					{
						thrown.last = thrown.transform.position;
						sender.thrown[i] = thrown;
					}
				}
			}
		}

		public static void Threw(PlayerRef player, Transform thing, float seconds = 15f)
		{
			// Rebuilt from native code (token 0x06000632, RVA 0x27F4670)
			if (!thing)
			{
				return;
			}
			Sender sender = Of(player);
			if (sender.thrown.Count > 16)
			{
				sender.thrown.RemoveAt(0);
			}
			sender.thrown.Add(new Thrown
			{
				transform = thing,
				last = thing.position,
				until = Time.time + seconds
			});
		}

		public static void Caught(PlayerRef player)
		{
			// Rebuilt from native code (token 0x06000633, RVA 0x27F484C)
			Of(player).lastCaught = Time.time;
		}

		public static float LastCaught(PlayerRef player)
		{
			// Rebuilt from native code (token 0x06000634, RVA 0x27F48BC)
			return Of(player).lastCaught;
		}

		public static bool Ok(float f)
		{
			// Rebuilt from native code (token 0x06000635, RVA 0x27F4920)
			return float.IsFinite(f) && Mathf.Abs(f) < 1e6f;
		}

		public static bool Ok(Vector3 v)
		{
			// Rebuilt from native code (token 0x06000636, RVA 0x27F4944)
			return Ok(v.x) && Ok(v.y) && Ok(v.z) && v.sqrMagnitude < WorldLimit * WorldLimit;
		}

		public static bool Ok(Quaternion q)
		{
			// Rebuilt from native code (token 0x06000637, RVA 0x27F4A58)
			return Ok(q.x) && Ok(q.y) && Ok(q.z) && Ok(q.w) && Quaternion.Dot(q, q) > 0.5f;
		}

		public static Vector3 Speed(Vector3 v, float max)
		{
			// Rebuilt from native code (token 0x06000638, RVA 0x27F4BAC)
			if (v.sqrMagnitude > max * max)
			{
				return v / v.magnitude * max;
			}
			return v;
		}
	}
}
