using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Fusion;
using UnityEngine;

namespace Shallows
{
	public class CheatWatch : MonoBehaviour
	{
		public enum Check
		{
			LongArms = 0,
			Speed = 1,
			Teleport = 2,
			Flying = 3,
			Noclip = 4,
			GodMode = 5
		}

		private class Watched
		{
			public PlayerRef player;

			public Vector3 head;

			public Vector4 shape;

			public float changedAt;

			public float checkedAt;

			public float graceUntil;

			public float airSince = -1f;

			public float contactY;

			public float armsSince = -1f;

			public float inGripSince = -1f;

			public Vector3 windowStart;

			public float windowAt;

			public int fastWindows;

			public bool hoverCounted;

			public bool riseCounted;

			public bool caught;

			public readonly List<float>[] seen = new List<float>[Names.Length];

			public Watched()
			{
				// Rebuilt from native code (token 0x060005B5, RVA 0x27E11CC)
				for (int i = 0; i < seen.Length; i++)
				{
					seen[i] = new List<float>();
				}
			}
		}

		// Rebuilt from native code (token 0x060005B4, RVA 0x27E370C)
		private static readonly string[] Names = new string[6] { "long arms", "speed", "teleport", "flying", "noclip", "god mode" };

		private static readonly int[] Needed = new int[6] { 3, 3, 2, 3, 3, 2 };

		private static readonly float[] Window = new float[6] { 60f, 60f, 60f, 90f, 60f, 120f };

		private const float MaxReach = 2.6f;

		private const float MaxSpeed = 16f;

		private const float TeleportDistance = 7f;

		private const float TeleportSpeed = 30f;

		private const float HoverTime = 2.5f;

		private const float NearWater = 1.2f;

		private const float RiseLimit = 3.5f;

		private const float GodModeTime = 1.2f;

		private const float Grace = 3f;

		private const float Interval = 0.05f;

		private readonly Dictionary<NetAvatar, Watched> watched = new Dictionary<NetAvatar, Watched>();

		private readonly List<NetAvatar> gone = new List<NetAvatar>();

		private static readonly Collider[] hits = new Collider[16];

		private static readonly RaycastHit[] rays = new RaycastHit[8];

		private static readonly HashSet<string> reported = new HashSet<string>();

		private static readonly Regex PlayFabIdPattern = new Regex("^[0-9A-Fa-f]{6,24}$");

		private NetworkRunner Runner
		{
			get
			{
				// Rebuilt from native code (token 0x060005A6, RVA 0x27E07FC)
				if (!NetworkManager.I)
				{
					return null;
				}
				return NetworkManager.I.Runner;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060005A5, RVA 0x27E0784)
			reported.Clear();
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060005A7, RVA 0x27E0918)
			NetGuard.Track();
			NetworkRunner runner = Runner;
			if (!runner || !runner.IsRunning)
			{
				watched.Clear();
				return;
			}
			float now = Time.time;
			foreach (NetAvatar a in NetAvatar.Remotes)
			{
				if (!a || !a.Object)
				{
					continue;
				}
				if (!watched.TryGetValue(a, out Watched w))
				{
					w = new Watched
					{
						player = a.Object.InputAuthority,
						head = a.HeadPosition,
						changedAt = now
					};
					watched[a] = w;
					Reset(w, a, now);
				}
				if (w.caught || now - w.checkedAt < Interval)
				{
					continue;
				}
				Vector4 shape = new Vector4(a.HeadRotation.x + a.LeftPosition.x, a.HeadRotation.y + a.RightPosition.y, a.HeadRotation.z + a.LeftPosition.z, a.HeadRotation.w + a.RightPosition.x);
				if ((a.HeadPosition - w.head).sqrMagnitude < 1E-10f && (shape - w.shape).sqrMagnitude < 1E-10f)
				{
					continue;
				}
				w.shape = shape;
				w.checkedAt = now;
				Look(runner, a, w, now);
			}
			gone.Clear();
			foreach (KeyValuePair<NetAvatar, Watched> pair in watched)
			{
				if (!pair.Key)
				{
					gone.Add(pair.Key);
				}
			}
			foreach (NetAvatar a in gone)
			{
				watched.Remove(a);
			}
		}

		private static void Reset(Watched w, NetAvatar a, float now)
		{
			// Rebuilt from native code (token 0x060005A8, RVA 0x27E13A0)
			w.airSince = -1f;
			w.armsSince = -1f;
			w.inGripSince = -1f;
			w.graceUntil = now + Grace;
			w.contactY = a.NetFeet.y;
			w.windowStart = a.HeadPosition;
			w.windowAt = now;
			w.fastWindows = 0;
		}

		private void Look(NetworkRunner runner, NetAvatar a, Watched w, float now)
		{
			// Rebuilt from native code (token 0x060005A9, RVA 0x27E1530)
			Vector3 head = a.HeadPosition;
			Vector3 feet = a.NetFeet;
			Vector3 lastHead = w.head;
			float lastChange = w.changedAt;
			w.head = head;
			w.changedAt = now;
			if (!NetGuard.Ok(head) || !NetGuard.Ok(a.LeftPosition) || !NetGuard.Ok(a.RightPosition))
			{
				return;
			}
			if (a.NetDying)
			{
				Reset(w, a, now);
			}
			else if (AtRespawn(feet) && (head - lastHead).sqrMagnitude > 4f)
			{
				Reset(w, a, now);
			}
			if (w.graceUntil > now)
			{
				w.contactY = feet.y;
				w.windowStart = head;
				w.windowAt = now;
				return;
			}
			int mask = (PlayerRig.I && PlayerRig.I.locomotion) ? PlayerRig.I.locomotion.locomotionEnabledLayers.value : Physics.DefaultRaycastLayers;
			float moved = (head - lastHead).magnitude;
			float dt = now - lastChange;
			if (moved > TeleportDistance && moved / Math.Max(dt, 0.01f) > TeleportSpeed)
			{
				Saw(runner, a, w, Check.Teleport, now);
				Reset(w, a, now);
				return;
			}
			if (dt < 0.25f && moved > 0.05f && Physics.Linecast(lastHead, head, out RaycastHit hit, mask, QueryTriggerInteraction.Ignore) && IsWall(hit.collider) && !Physics.CheckSphere(head, 0.12f, mask, QueryTriggerInteraction.Ignore))
			{
				Saw(runner, a, w, Check.Noclip, now);
			}
			float leftReach = (a.LeftPosition - head).magnitude;
			float rightReach = (a.RightPosition - head).magnitude;
			if (Mathf.Max(leftReach, rightReach) > MaxReach)
			{
				if (w.armsSince < 0f)
				{
					w.armsSince = now;
				}
				else if (now - w.armsSince > 1f)
				{
					Saw(runner, a, w, Check.LongArms, now);
					w.armsSince = -1f;
				}
			}
			else
			{
				w.armsSince = -1f;
			}
			if (now - w.windowAt >= 0.5f)
			{
				Vector3 flat = head - w.windowStart;
				flat.y = 0f;
				if (flat.magnitude / (now - w.windowAt) > MaxSpeed)
				{
					if (++w.fastWindows >= 3)
					{
						Saw(runner, a, w, Check.Speed, now);
						w.fastWindows = 0;
					}
				}
				else
				{
					w.fastWindows = 0;
				}
				w.windowStart = head;
				w.windowAt = now;
			}
			if (Touching(a, feet, mask))
			{
				w.contactY = feet.y;
				w.airSince = -1f;
				w.hoverCounted = false;
				w.riseCounted = false;
			}
			else
			{
				if (w.airSince < 0f)
				{
					w.airSince = now;
				}
				if (!w.riseCounted && feet.y > w.contactY + RiseLimit)
				{
					w.riseCounted = true;
					Saw(runner, a, w, Check.Flying, now);
				}
				if (!w.hoverCounted && now - w.airSince > HoverTime && feet.y > w.contactY - 0.5f)
				{
					w.hoverCounted = true;
					Saw(runner, a, w, Check.Flying, now);
				}
			}
			GameManager gm = GameManager.I;
			if (!NetBridge.MonsterAuthority || !gm || gm.IsSafe(feet + Vector3.up * 0.1f))
			{
				return;
			}
			bool gripped = false;
			foreach (MonsterAI monster in MonsterAI.All)
			{
				if (monster && monster.WouldCatch(feet, 0.6f) && monster.IsExposed(feet))
				{
					gripped = true;
					break;
				}
			}
			if (!gripped)
			{
				w.inGripSince = -1f;
			}
			else if (w.inGripSince < 0f)
			{
				w.inGripSince = now;
			}
			else if (now - w.inGripSince > GodModeTime && NetGuard.LastCaught(w.player) < w.inGripSince)
			{
				Saw(runner, a, w, Check.GodMode, now);
				w.inGripSince = -1f;
			}
		}

		private static bool Touching(NetAvatar a, Vector3 feet, int mask)
		{
			// Rebuilt from native code (token 0x060005AA, RVA 0x27E2AE0)
			WaterVolume water = WaterVolume.At(feet);
			if (water && feet.y < water.SurfaceY + NearWater)
			{
				return true;
			}
			if (Solid(feet, 0.5f, mask) || Solid(a.HeadPosition, 0.4f, mask) || Solid(a.LeftPosition, 0.4f, mask) || Solid(a.RightPosition, 0.4f, mask))
			{
				return true;
			}
			int count = Physics.RaycastNonAlloc(feet + Vector3.up * 0.1f, Vector3.down, rays, 1.3f, mask, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < count; i++)
			{
				if (Counts(rays[i].collider))
				{
					return true;
				}
			}
			return false;
		}

		private static bool Solid(Vector3 point, float radius, int mask)
		{
			// Rebuilt from native code (token 0x060005AB, RVA 0x27E2E3C)
			int count = Physics.OverlapSphereNonAlloc(point, radius, hits, mask, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < count; i++)
			{
				if (Counts(hits[i]))
				{
					return true;
				}
			}
			return false;
		}

		private static bool Counts(Collider c)
		{
			// Rebuilt from native code (token 0x060005AC, RVA 0x27E2F84)
			if (!c || c.GetComponentInParent<NetAvatar>())
			{
				return false;
			}
			Rigidbody body = c.attachedRigidbody;
			if (!body)
			{
				return true;
			}
			return body.isKinematic;
		}

		private static bool IsWall(Collider c)
		{
			// Rebuilt from native code (token 0x060005AD, RVA 0x27E285C)
			if (!Counts(c) || c.attachedRigidbody)
			{
				return false;
			}
			if (c.bounds.size.y < 2f)
			{
				return false;
			}
			if (c.GetComponentInParent<PlayerRig>() || c.GetComponentInParent<MovingPlatform>() || c.GetComponentInParent<SinkingPlatform>())
			{
				return false;
			}
			Transform door = FuseDoor.I ? FuseDoor.I.door : null;
			if (!door)
			{
				return true;
			}
			return !c.transform.IsChildOf(door);
		}

		private static bool AtRespawn(Vector3 feet)
		{
			// Rebuilt from native code (token 0x060005AE, RVA 0x27E211C)
			GameManager gm = GameManager.I;
			if (!gm)
			{
				return false;
			}
			if (gm.IsSafe(feet + Vector3.up * 0.1f))
			{
				return true;
			}
			if (gm.spawnPoint && (gm.spawnPoint.position - feet).sqrMagnitude < 9f)
			{
				return true;
			}
			if (gm.levels == null)
			{
				return false;
			}
			foreach (LevelInfo level in gm.levels)
			{
				if (level != null && level.spawn && (level.spawn.position - feet).sqrMagnitude < 9f)
				{
					return true;
				}
			}
			return false;
		}

		private void Saw(NetworkRunner runner, NetAvatar a, Watched w, Check check, float now)
		{
			// Rebuilt from native code (token 0x060005AF, RVA 0x27E2388)
			int c = (int)check;
			List<float> seen = w.seen[c];
			seen.Add(now);
			seen.RemoveAll(t => now - t > Window[c]);
			Debug.Log(string.Format("Shallows: CheatWatch saw player {0} ({1}): {2} ({3}/{4}).", w.player.PlayerId, a.PlayerName, Names[c], seen.Count, Needed[c]));
			if (seen.Count >= Needed[c])
			{
				w.caught = true;
				Caught(runner, w.player, a.PlayerName, Names[c]);
			}
		}

		public static void ReportFlood(NetworkRunner runner, PlayerRef player)
		{
			// Rebuilt from native code (token 0x060005B0, RVA 0x27E31E0)
			NetAvatar avatar = NetGuard.AvatarOf(player);
			Report(runner, player, avatar ? avatar.PlayerName : "?", "flooding");
		}

		private static void Caught(NetworkRunner runner, PlayerRef player, string name, string why)
		{
			// Rebuilt from native code (token 0x060005B1, RVA 0x27E312C)
			NetGuard.Ignore(runner, player, why);
			Report(runner, player, name, why);
		}

		private static void Report(NetworkRunner runner, PlayerRef player, string name, string why)
		{
			// Rebuilt from native code (token 0x060005B2, RVA 0x27E32FC)
			Debug.LogWarning(string.Format("Shallows: player {0} ({1}) caught cheating: {2}.", player.PlayerId, name, why));
			string id = runner ? runner.GetPlayerUserId(player) : null;
			if (string.IsNullOrEmpty(id) || !PlayFabIdPattern.IsMatch(id) || id == PlayFabClient.PlayFabId || !reported.Add(id))
			{
				return;
			}
			Wallet.Report(id, why);
		}
	}
}
