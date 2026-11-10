using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public static class NetBridge
	{
		public static bool MonsterAuthority = true;

		public static bool MasterSilent;

		public static float LastMonsterUpdate;

		public static int LocalPlayerId;

		public static bool RunsMonsters
		{
			get
			{
				// Rebuilt from native code (token 0x0600021D, RVA 0x2781440)
				return MonsterAuthority || MasterSilent;
			}
		}

		// Rebuilt from native code (token 0x0600021E, RVA 0x27814C4; token 0x0600021F, RVA 0x27815B8)
		public static event Action<SplashEvent> LocalSplash;

		// Rebuilt from native code (token 0x06000220, RVA 0x27816AC; token 0x06000221, RVA 0x27817A0)
		public static event Action<int, int> LocalCaught;

		// Rebuilt from native code (token 0x06000222, RVA 0x2781894; token 0x06000223, RVA 0x2781988)
		public static event Action<int, Vector3, Vector3> LocalThrow;

		// Rebuilt from native code (token 0x06000224, RVA 0x2781A7C; token 0x06000225, RVA 0x2781B70)
		public static event Action<int, int, Vector3, Vector3> LocalItem;

		// Rebuilt from native code (token 0x06000226, RVA 0x2781C64; token 0x06000227, RVA 0x2781D58)
		public static event Action<int, bool, Vector3> LocalItemTaken;

		// Rebuilt from native code (token 0x06000228, RVA 0x2781E4C; token 0x06000229, RVA 0x2781F40)
		public static event Action<Vector3, float> LocalSponge;

		// Rebuilt from native code (token 0x0600022A, RVA 0x2782034; token 0x0600022B, RVA 0x2782128)
		public static event Action<string, float> LocalEscape;

		// Rebuilt from native code (token 0x0600022C, RVA 0x278221C; token 0x0600022D, RVA 0x2782310)
		public static event Action<int, int, Vector3> LocalFuseRequest;

		// Rebuilt from native code (token 0x0600022E, RVA 0x2782404; token 0x0600022F, RVA 0x27824F8)
		public static event Action<FuseSnapshot> FuseStateChanged;

		// Rebuilt from native code (token 0x06000230, RVA 0x27825EC; token 0x06000231, RVA 0x27826E0)
		public static event Action<int, Vector3, Vector3> LocalFuseThrow;

		// Rebuilt from native code (token 0x06000232, RVA 0x27827D4; token 0x06000233, RVA 0x27828C8)
		public static event Action<Vector3, float> LocalTap;

		// Rebuilt from native code (token 0x06000234, RVA 0x27829BC; token 0x06000235, RVA 0x2782AB0)
		public static event Action<int, int, Vector3> LocalPebble;

		// Rebuilt from native code (token 0x06000236, RVA 0x2782BA4; token 0x06000237, RVA 0x2782C98)
		public static event Action<PebbleSnapshot> PebblesShared;

		// Rebuilt from native code (token 0x06000238, RVA 0x2782D8C; token 0x06000239, RVA 0x2782E80)
		public static event Action<TreeSnapshot> TreeShared;

		// Rebuilt from native code (token 0x0600023A, RVA 0x2782F74; token 0x0600023B, RVA 0x2783068)
		public static event Action<int> LocalTreeRip;

		public static int RoomSeed { get; private set; } = NewSeed();

		// Rebuilt from native code (token 0x0600023E, RVA 0x2783210; token 0x0600023F, RVA 0x27832EC)
		public static event Action RoomSeedChanged;

		// Rebuilt from native code (token 0x06000240, RVA 0x27833C8; token 0x06000241, RVA 0x27834BC)
		public static event Action<bool> RoomJoined;

		// Rebuilt from native code (token 0x06000242, RVA 0x27835B0; token 0x06000243, RVA 0x278368C)
		public static event Action RoomGrew;

		public static Func<int, bool> PlayerInRoom = id => LocalPlayerId == id;

		public static readonly List<Vector3> RemoteFeet = new List<Vector3>();

		public static readonly List<Vector3> RemoteHands = new List<Vector3>();

		public static readonly List<float> RemoteVoices = new List<float>();

		private static float clockOffset;

		private static float clockTarget;

		private static bool clockSet;

		public static float SharedTime
		{
			get
			{
				// Rebuilt from native code (token 0x0600024C, RVA 0x2783BE4)
				return Time.time + clockOffset;
			}
		}

		public static bool Silenced { get; private set; }

		// Rebuilt from native code (token 0x06000253, RVA 0x2784250; token 0x06000254, RVA 0x278432C)
		public static event Action SilencedChanged;

		public static int NewSeed()
		{
			// Rebuilt from native code (token 0x06000244, RVA 0x2783768)
			return (Guid.NewGuid().GetHashCode() & int.MaxValue) | 1;
		}

		public static void SetRoomSeed(int seed)
		{
			// Rebuilt from native code (token 0x06000245, RVA 0x27837A0)
			if (RoomSeed == seed) return;
			RoomSeed = seed;
			RoomSeedChanged?.Invoke();
		}

		public static void ReportRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x06000246, RVA 0x278389C)
			RoomJoined?.Invoke(fresh);
		}

		public static void ReportRoomGrew()
		{
			// Rebuilt from native code (token 0x06000247, RVA 0x2783918)
			RoomGrew?.Invoke();
		}

		public static void ReportPebble(int action, int pebble, Vector3 position)
		{
			// Rebuilt from native code (token 0x06000248, RVA 0x278398C)
			LocalPebble?.Invoke(action, pebble, position);
		}

		public static void SharePebbles(PebbleSnapshot state)
		{
			// Rebuilt from native code (token 0x06000249, RVA 0x2783A4C)
			PebblesShared?.Invoke(state);
		}

		public static void ShareTree(TreeSnapshot state)
		{
			// Rebuilt from native code (token 0x0600024A, RVA 0x2783ADC)
			TreeShared?.Invoke(state);
		}

		public static void ReportTreeRip(int id)
		{
			// Rebuilt from native code (token 0x0600024B, RVA 0x2783B68)
			LocalTreeRip?.Invoke(id);
		}

		public static void SyncClock(float authorityTime)
		{
			// Rebuilt from native code (token 0x0600024D, RVA 0x2783C54)
			clockTarget = authorityTime - Time.time;
			if (!clockSet || Mathf.Abs(clockTarget - clockOffset) > 1f)
			{
				clockOffset = clockTarget;
			}
			clockSet = true;
		}

		public static void TickClock(float dt)
		{
			// Rebuilt from native code (token 0x0600024E, RVA 0x2783D34)
			clockOffset = Mathf.MoveTowards(clockOffset, clockTarget, dt * 0.05f);
		}

		public static void ResetClock()
		{
			// Rebuilt from native code (token 0x0600024F, RVA 0x2783DC8)
			if (!clockSet) return;
			clockSet = false;
			clockTarget = clockOffset;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000250, RVA 0x2783E44)
			MonsterAuthority = true;
			MasterSilent = false;
			LastMonsterUpdate = 0f;
			LocalPlayerId = 0;
			LocalSplash = null;
			LocalCaught = null;
			LocalThrow = null;
			LocalItem = null;
			LocalItemTaken = null;
			LocalSponge = null;
			LocalEscape = null;
			LocalFuseRequest = null;
			FuseStateChanged = null;
			LocalFuseThrow = null;
			LocalTap = null;
			LocalPebble = null;
			PebblesShared = null;
			TreeShared = null;
			LocalTreeRip = null;
			RoomSeed = NewSeed();
			RoomSeedChanged = null;
			RoomJoined = null;
			RoomGrew = null;
			PlayerInRoom = id => LocalPlayerId == id;
			RemoteFeet.Clear();
			RemoteHands.Clear();
			RemoteVoices.Clear();
			clockSet = false;
			clockOffset = 0f;
			clockTarget = 0f;
		}

		public static void Silence(bool silenced)
		{
			// Rebuilt from native code (token 0x06000255, RVA 0x2784408)
			if (Silenced == silenced) return;
			Silenced = silenced;
			SilencedChanged?.Invoke();
		}

		public static void ReportSplash(SplashEvent e)
		{
			// Rebuilt from native code (token 0x06000256, RVA 0x2784514)
			LocalSplash?.Invoke(e);
		}

		public static void ReportTap(Vector3 position, float hit)
		{
			// Rebuilt from native code (token 0x06000257, RVA 0x27845A0)
			LocalTap?.Invoke(position, hit);
		}

		public static void ReportCaught(int monster, int kill)
		{
			// Rebuilt from native code (token 0x06000258, RVA 0x278464C)
			LocalCaught?.Invoke(monster, kill);
		}

		public static void ReportThrow(int kind, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000259, RVA 0x27846DC)
			LocalThrow?.Invoke(kind, position, velocity);
		}

		public static void ReportItem(int kind, int id, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x0600025A, RVA 0x27847AC)
			LocalItem?.Invoke(kind, id, position, velocity);
		}

		public static void ReportItemTaken(int id, Vector3 position)
		{
			// Rebuilt from native code (token 0x0600025B, RVA 0x2784890)
			LocalItemTaken?.Invoke(id, false, position);
		}

		public static void ReportItemGone(int id)
		{
			// Rebuilt from native code (token 0x0600025C, RVA 0x2784940)
			LocalItemTaken?.Invoke(id, true, Vector3.zero);
		}

		public static void ReportSponge(Vector3 position, float yaw)
		{
			// Rebuilt from native code (token 0x0600025D, RVA 0x27849F8)
			LocalSponge?.Invoke(position, yaw);
		}

		public static void ReportEscape(string name, float time)
		{
			// Rebuilt from native code (token 0x0600025E, RVA 0x277A5D0)
			LocalEscape?.Invoke(name, time);
		}

		public static void RequestFuse(int action, int fuse, Vector3 position)
		{
			// Rebuilt from native code (token 0x0600025F, RVA 0x2784AA4)
			LocalFuseRequest?.Invoke(action, fuse, position);
		}

		public static void ShareFuses(FuseSnapshot state)
		{
			// Rebuilt from native code (token 0x06000260, RVA 0x2784B64)
			FuseStateChanged?.Invoke(state);
		}

		public static void ReportFuseThrow(int fuse, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000261, RVA 0x2784BF8)
			LocalFuseThrow?.Invoke(fuse, position, velocity);
		}
	}
}
