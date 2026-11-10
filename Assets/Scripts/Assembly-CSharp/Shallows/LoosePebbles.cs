using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(-5)]
	public class LoosePebbles : MonoBehaviour
	{
		private enum State
		{
			Lying = 0,
			Held = 1,
			Flying = 2,
			Away = 3,
			Gone = 4
		}

		private class Pebble
		{
			public Throwable item;

			public int level;

			public State state;

			public Vector3 lies;

			public bool moved;

			public float stillSince;

			public float releasedAt;
		}

		public const int Taken = 0;

		public const int Rest = 1;

		public const int Sank = 2;

		private const float Inset = 0.3f;

		private const float ShareEvery = 3f;

		private const float RestTime = 0.4f;

		private readonly List<Pebble> pebbles = new List<Pebble>();

		private SortedDictionary<int, List<BoxCollider>> platforms;

		private int laidOutWith;

		private float nextShare;

		public static LoosePebbles I { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x06000453, RVA 0x27AEC1C)
			if (I)
			{
				return;
			}
			foreach (Throwable item in FindObjectsByType<Throwable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (IsLoosePebble(item))
				{
					new GameObject("Loose Pebbles").AddComponent<LoosePebbles>();
					return;
				}
			}
		}

		private static bool IsLoosePebble(Throwable item)
		{
			// Rebuilt from native code (token 0x06000454, RVA 0x27AED98)
			return item.name == "Pebble" && !item.GetComponent<ShopItem>() && !item.GetComponent<Fuse>();
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000455, RVA 0x27AEEAC)
			I = this;
			List<Throwable> found = new List<Throwable>();
			foreach (Throwable item in FindObjectsByType<Throwable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (IsLoosePebble(item))
				{
					found.Add(item);
				}
			}
			found.Sort((a, b) =>
			{
				Vector3 pa = a.transform.position;
				Vector3 pb = b.transform.position;
				int byX = Mathf.RoundToInt(pa.x * 100f).CompareTo(Mathf.RoundToInt(pb.x * 100f));
				if (byX != 0)
				{
					return byX;
				}
				return Mathf.RoundToInt(pa.z * 100f).CompareTo(Mathf.RoundToInt(pb.z * 100f));
			});
			if (found.Count > 64)
			{
				Debug.LogWarning($"Shallows: {found.Count} loose pebbles, only the first 64 are shared online.");
				for (int i = 64; i < found.Count; i++)
				{
					Destroy(found[i].gameObject);
				}
				found.RemoveRange(64, found.Count - 64);
			}
			platforms = PoolPlatforms.ByLevel();
			for (int i = 0; i < found.Count; i++)
			{
				int id = i;
				Throwable item = found[i];
				item.returnsHome = false;
				item.sinksAway = true;
				pebbles.Add(new Pebble
				{
					item = item,
					level = PoolPlatforms.NearestLevel(platforms, item.transform.position),
					lies = item.transform.position
				});
				item.Grabbed += () => OnGrabbed(id);
				item.Released += _ => OnReleased(id);
				item.Sank += () => OnSank(id);
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000456, RVA 0x27AFE40)
			NetBridge.RoomJoined += OnRoomJoined;
			NetBridge.RoomGrew += CatchUp;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000457, RVA 0x27AFF3C)
			NetBridge.RoomJoined -= OnRoomJoined;
			NetBridge.RoomGrew -= CatchUp;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000458, RVA 0x27B0038)
			if (I == this)
			{
				I = null;
			}
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000459, RVA 0x27B0114)
			LayOut(NetBridge.RoomSeed);
		}

		private void LayOut(int seed)
		{
			// Rebuilt from native code (token 0x0600045A, RVA 0x27B01A4)
			laidOutWith = seed;
			System.Random random = new System.Random(seed);
			Dictionary<int, List<Pebble>> byLevel = new Dictionary<int, List<Pebble>>();
			foreach (Pebble p in pebbles)
			{
				if (!byLevel.TryGetValue(p.level, out List<Pebble> onLevel))
				{
					onLevel = new List<Pebble>();
					byLevel[p.level] = onLevel;
				}
				onLevel.Add(p);
			}
			foreach (KeyValuePair<int, List<BoxCollider>> level in platforms)
			{
				if (!byLevel.TryGetValue(level.Key, out List<Pebble> onLevel))
				{
					continue;
				}
				List<BoxCollider> boxes = PoolPlatforms.Shuffled(level.Value, random);
				for (int i = 0; i < onLevel.Count; i++)
				{
					onLevel[i].lies = PoolPlatforms.RandomTop(boxes[i % boxes.Count], random, Inset) + Vector3.up * 0.044f;
				}
			}
			foreach (Pebble p in pebbles)
			{
				if (p.state == State.Held)
				{
					p.item.Holder?.Drop();
				}
				p.state = State.Lying;
				p.moved = false;
				Show(p);
			}
		}

		private void Show(Pebble p)
		{
			// Rebuilt from native code (token 0x0600045B, RVA 0x27B0BD8)
			if (!p.item.gameObject.activeSelf)
			{
				p.item.gameObject.SetActive(true);
			}
			Throwable item = p.item;
			if ((item.transform.position - p.lies).sqrMagnitude > 0.0004f || item.IsHeld || item.Stowed)
			{
				item.Place(p.lies, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), false);
			}
		}

		private static void Hide(Pebble p)
		{
			// Rebuilt from native code (token 0x0600045C, RVA 0x27B0D4C)
			if (p.item.IsHeld || p.item.Stowed)
			{
				p.item.ForceDrop();
			}
			if (p.item.gameObject.activeSelf)
			{
				p.item.gameObject.SetActive(false);
			}
		}

		private void OnGrabbed(int id)
		{
			// Rebuilt from native code (token 0x0600045D, RVA 0x27B0DE0)
			pebbles[id].state = State.Held;
			NetBridge.ReportPebble(Taken, id, pebbles[id].item.transform.position);
		}

		private void OnReleased(int id)
		{
			// Rebuilt from native code (token 0x0600045E, RVA 0x27B0ED8)
			Pebble p = pebbles[id];
			if (p.state != State.Held)
			{
				return;
			}
			p.state = State.Flying;
			p.stillSince = p.releasedAt = Time.time;
		}

		private void OnSank(int id)
		{
			// Rebuilt from native code (token 0x0600045F, RVA 0x27B0F5C)
			Pebble p = pebbles[id];
			if (p.state == State.Gone)
			{
				return;
			}
			p.state = State.Gone;
			NetBridge.ReportPebble(Sank, id, p.item.transform.position);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000460, RVA 0x27B105C)
			foreach (Pebble p in pebbles)
			{
				if (p.state != State.Flying || !p.item.gameObject.activeSelf)
				{
					continue;
				}
				bool underwater = WaterVolume.IsUnderwater(p.item.transform.position, out _);
				if (underwater || p.item.Moving)
				{
					p.stillSince = Time.time;
					if (underwater)
					{
						continue;
					}
				}
				if (Time.time - p.stillSince > RestTime || Time.time - p.releasedAt > 8f)
				{
					p.state = State.Lying;
					p.lies = p.item.transform.position;
					p.moved = true;
					NetBridge.ReportPebble(Rest, pebbles.IndexOf(p), p.lies);
				}
			}
			if (!NetBridge.MonsterAuthority || Time.time < nextShare)
			{
				return;
			}
			nextShare = Time.time + ShareEvery;
			NetBridge.SharePebbles(Snapshot());
		}

		private PebbleSnapshot Snapshot()
		{
			// Rebuilt from native code (token 0x06000461, RVA 0x27B1450)
			long taken = 0L;
			for (int i = 0; i < pebbles.Count; i++)
			{
				if (pebbles[i].state != State.Lying)
				{
					taken |= 1L << i;
				}
			}
			return new PebbleSnapshot
			{
				seed = laidOutWith,
				taken = taken
			};
		}

		private void OnRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x06000462, RVA 0x27B1500)
			if (!fresh)
			{
				return;
			}
			int seed = NetBridge.NewSeed();
			NetBridge.SetRoomSeed(seed);
			LayOut(seed);
			nextShare = 0f;
		}

		private void CatchUp()
		{
			// Rebuilt from native code (token 0x06000463, RVA 0x27B157C)
			if (!NetBridge.MonsterAuthority)
			{
				return;
			}
			NetBridge.SharePebbles(Snapshot());
			nextShare = Time.time + ShareEvery;
			for (int i = 0; i < pebbles.Count; i++)
			{
				if (pebbles[i].state == State.Lying && pebbles[i].moved)
				{
					NetBridge.ReportPebble(Rest, i, pebbles[i].lies);
				}
			}
		}

		public void Receive(int action, int id, Vector3 position)
		{
			// Rebuilt from native code (token 0x06000464, RVA 0x27B1708)
			if (id < 0 || id >= pebbles.Count)
			{
				return;
			}
			Pebble p = pebbles[id];
			if (action == Sank)
			{
				if (p.state != State.Held)
				{
					p.state = State.Gone;
					Hide(p);
				}
			}
			else if (action == Rest)
			{
				if (p.state != State.Held && p.state != State.Flying)
				{
					p.lies = position;
					p.state = State.Lying;
					p.moved = true;
					Show(p);
				}
			}
			else if (action == Taken)
			{
				if (p.state != State.Held && p.state != State.Flying)
				{
					p.state = State.Away;
					Hide(p);
				}
			}
		}

		public void ReceiveState(PebbleSnapshot s)
		{
			// Rebuilt from native code (token 0x06000465, RVA 0x27B184C)
			if (NetBridge.MonsterAuthority)
			{
				return;
			}
			if (laidOutWith != s.seed)
			{
				NetBridge.SetRoomSeed(s.seed);
				LayOut(s.seed);
			}
			for (int i = 0; i < pebbles.Count; i++)
			{
				Pebble p = pebbles[i];
				if (p.state == State.Held || p.state == State.Flying)
				{
					continue;
				}
				if (((s.taken >> i) & 1) != 0)
				{
					if (p.state == State.Lying)
					{
						p.state = State.Away;
						Hide(p);
					}
				}
				else if (p.state != State.Lying)
				{
					p.state = State.Lying;
					Show(p);
				}
			}
		}
	}
}
