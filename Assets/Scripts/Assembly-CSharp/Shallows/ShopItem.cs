using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(Throwable))]
	public class ShopItem : MonoBehaviour
	{
		public ItemKind kind;

		internal bool used;

		internal bool found;

		public const float DryLife = 180f;

		private const int MaxCopies = 24;

		private int owner;

		private bool loose;

		private bool restShared;

		private bool quiet;

		private bool copy;

		private float looseSince;

		private float stillSince;

		// Rebuilt from native code (token 0x0600099D, RVA 0x2740064)
		private float wetSince = -1f;

		private float arrivedAt;

		private Vector3 launchedFrom;

		private float launchedAt = -100f;

		// Rebuilt from native code (token 0x0600099E, RVA 0x274007C)
		private static readonly Dictionary<int, ShopItem> shared = new Dictionary<int, ShopItem>();

		private static int nextId;

		public Throwable Item { get; private set; }

		public int NetId { get; private set; }

		private bool Mine
		{
			get
			{
				// Rebuilt from native code (token 0x0600098A, RVA 0x273E7E0)
				return owner == NetBridge.LocalPlayerId;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000989, RVA 0x273E758)
			shared.Clear();
			nextId = 0;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600098B, RVA 0x273E848)
			Item = GetComponent<Throwable>();
			Throwable item = Item;
			item.returnsHome = false;
			item.shareThrow = false;
			item.Grabbed += OnGrabbed;
			Item.Released += OnReleased;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600098C, RVA 0x273E970)
			NetBridge.RoomJoined += OnRoomJoined;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600098D, RVA 0x273EA14)
			NetBridge.RoomJoined -= OnRoomJoined;
		}

		private void OnRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x0600098E, RVA 0x273EAB8)
			if (loose && !Item.IsHeld && !Item.Stowed)
			{
				quiet = true;
				Destroy(gameObject);
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x0600098F, RVA 0x273EB68)
			if (loose && !quiet && NetId != 0 && Mine)
			{
				NetBridge.ReportItemGone(NetId);
			}
			if (NetId != 0 && shared.TryGetValue(NetId, out ShopItem item) && item == this)
			{
				shared.Remove(NetId);
			}
		}

		private void OnGrabbed()
		{
			// Rebuilt from native code (token 0x06000990, RVA 0x273ECE0)
			loose = false;
			copy = false;
			Ghost(false);
			if (NetId != 0)
			{
				NetBridge.ReportItemTaken(NetId, transform.position);
			}
		}

		private void OnReleased(Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000991, RVA 0x273EEF8)
			if (NetId == 0)
			{
				SetId(NewId());
			}
			owner = NetBridge.LocalPlayerId;
			Launched(transform.position);
			Loose();
			NetBridge.ReportItem((int)kind, NetId, transform.position, velocity);
		}

		private void Launched(Vector3 from)
		{
			// Rebuilt from native code (token 0x06000992, RVA 0x273F190)
			launchedFrom = from;
			launchedAt = Time.time;
		}

		private bool NearFor(Func<Vector3, bool> near)
		{
			// Rebuilt from native code (token 0x06000993, RVA 0x273F1E0)
			if (near(transform.position))
			{
				return true;
			}
			return Time.time - launchedAt < 3f && near(launchedFrom);
		}

		private static bool Keeps(int me, int them)
		{
			// Rebuilt from native code (token 0x06000994, RVA 0x273F274)
			return me < them;
		}

		private void Loose()
		{
			// Rebuilt from native code (token 0x06000995, RVA 0x273F1B4)
			loose = true;
			restShared = false;
			looseSince = stillSince = Time.time;
			wetSince = -1f;
		}

		private void SetId(int id)
		{
			// Rebuilt from native code (token 0x06000996, RVA 0x273F0FC)
			NetId = id;
			shared[id] = this;
		}

		private static int NewId()
		{
			// Rebuilt from native code (token 0x06000997, RVA 0x273F034)
			nextId = nextId % 65535 + 1;
			return nextId | ((NetBridge.LocalPlayerId & 0x7FFF) << 16);
		}

		private void Ghost(bool on)
		{
			// Rebuilt from native code (token 0x06000998, RVA 0x273EDAC)
			RubberDuck duck = GetComponent<RubberDuck>();
			if (duck)
			{
				duck.ghost = on;
			}
			Sponge sponge = GetComponent<Sponge>();
			if (sponge)
			{
				sponge.ghost = on;
			}
			if (!on)
			{
				return;
			}
			GlowStick glowStick = GetComponent<GlowStick>();
			if (glowStick)
			{
				glowStick.ghost = true;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000999, RVA 0x273F280)
			if (!loose)
			{
				return;
			}
			if (Item.Moving)
			{
				stillSince = Time.time;
			}
			if (transform.position.y < -3f)
			{
				Destroy(gameObject);
				return;
			}
			bool wet = WaterVolume.IsUnderwater(transform.position, out _);
			if (!wet)
			{
				wetSince = -1f;
			}
			else if (wetSince < 0f)
			{
				wetSince = Time.time;
			}
			if (Mine && !(wet || restShared) && (Time.time - stillSince > 0.5f || Time.time - looseSince > 3f))
			{
				restShared = true;
				NetBridge.ReportItem((int)kind, NetId, transform.position, Vector3.zero);
			}
			if (kind == ItemKind.GlowStick)
			{
				return;
			}
			float grace = Mine ? 0f : 5f;
			if (wet && kind != ItemKind.Sponge)
			{
				if (Time.time - stillSince > grace + 5f)
				{
					Destroy(gameObject);
					return;
				}
				if (Time.time - wetSince > grace + (kind == ItemKind.Duck ? 14f : 10f))
				{
					Destroy(gameObject);
				}
			}
			else if (Time.time - looseSince > grace + DryLife)
			{
				Destroy(gameObject);
			}
		}

		public static ShopItem Arrive(int kind, int id, int from, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x0600099A, RVA 0x273F5F8)
			if (kind < 0 || kind > 3 || id == 0)
			{
				return null;
			}
			if (shared.TryGetValue(id, out ShopItem item) && item)
			{
				if (item.Item.IsHeld || item.Item.Stowed)
				{
					if (NetBridge.LocalPlayerId != from)
					{
						NetBridge.ReportItemTaken(id, item.transform.position);
					}
					return null;
				}
				bool resting = velocity.sqrMagnitude < 0.0001f;
				if (resting && item.owner == from && (item.transform.position - position).sqrMagnitude < 0.0025f)
				{
					return item;
				}
				item.owner = from;
				item.Ghost(true);
				item.Item.Launch(position, velocity);
				item.Launched(position);
				if (!resting || !item.loose)
				{
					item.Loose();
				}
				return item;
			}
			Trim();
			item = Items.Spawn((ItemKind)kind, position);
			item.name = ShopCatalogue.Get((ItemKind)kind).name.ToLowerInvariant() + " (someone's)";
			item.found = true;
			item.copy = true;
			item.arrivedAt = Time.time;
			item.owner = from;
			item.SetId(id);
			item.Ghost(true);
			item.Item.Launch(position, velocity);
			item.Launched(position);
			item.Loose();
			return item;
		}

		public static void Taken(int id, bool gone, int from, Func<Vector3, bool> near)
		{
			// Rebuilt from native code (token 0x0600099B, RVA 0x273FD68)
			if (!shared.TryGetValue(id, out ShopItem item) || !item || NetBridge.LocalPlayerId == from)
			{
				return;
			}
			if (gone ? item.owner != from : !item.NearFor(near))
			{
				return;
			}
			if (item.Item.IsHeld || item.Item.Stowed)
			{
				if (gone || Keeps(NetBridge.LocalPlayerId, from))
				{
					return;
				}
				Debug.Log($"Shallows: {item.name} went to player {from} (both grabbed it at once)");
				if (item.Item.Stowed && !item.found)
				{
					Wallet.Use(item.kind);
				}
				item.Item.ForceDrop();
			}
			item.quiet = true;
			Destroy(item.gameObject);
		}

		private static void Trim()
		{
			// Rebuilt from native code (token 0x0600099C, RVA 0x273FA84)
			ShopItem oldest = null;
			int copies = 0;
			foreach (ShopItem item in shared.Values)
			{
				if (!item || !item.copy || !item.loose)
				{
					continue;
				}
				copies++;
				if (!oldest || item.arrivedAt < oldest.arrivedAt)
				{
					oldest = item;
				}
			}
			if (copies < MaxCopies || !oldest)
			{
				return;
			}
			oldest.quiet = true;
			shared.Remove(oldest.NetId);
			Destroy(oldest.gameObject);
		}
	}
}
