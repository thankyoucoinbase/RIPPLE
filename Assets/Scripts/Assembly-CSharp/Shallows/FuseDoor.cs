using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(-10)]
	public class FuseDoor : MonoBehaviour
	{
		public const int Grab = 0;

		public const int Land = 1;

		public const int Slot = 2;

		public Fuse[] fuses;

		[Tooltip("Where fuses can turn up. Three are picked at random each round.")]
		public Transform[] spots;

		[Tooltip("The three slots by the door.")]
		public Transform[] slots;

		public Renderer[] slotLamps;

		public Transform door;

		[Tooltip("How far the door slides when it opens.")]
		public Vector3 openOffset = new Vector3(0f, 2.75f, 0f);

		public Light alarm;

		[Tooltip("The alarm beacon's lens over the door: dim red, flashing with the alarm light.")]
		public Renderer alarmLamp;

		public TextMesh countdown;

		public Material huskMaterial;

		[Tooltip("Seconds the door stays open.")]
		public float openSeconds = 30f;

		[Tooltip("A fuse someone has held this long without doing anything goes back out (they probably left).")]
		public float heldTimeout = 120f;

		[Tooltip("How close a held fuse (any part of it, or the hand holding it) has to come to a free slot to click in (m).")]
		public float slotReach = 0.25f;

		[Tooltip("Letting go of a fuse this close to a free slot puts it in, too (m).")]
		public float releaseReach = 0.35f;

		public int seed = 9;

		private readonly Fuse.Where[] where = new Fuse.Where[3];

		private readonly int[] index = new int[3];

		private readonly int[] holder = new int[3] { -1, -1, -1 };

		private readonly Vector3[] loose = new Vector3[3];

		private readonly float[] heldSince = new float[3];

		private readonly float[] remoteFlight = new float[3];

		private readonly float[] absentSince = new float[3] { -1f, -1f, -1f };

		private float openLeft;

		private int round;

		private bool heard;

		private const float GoneFor = 3f;

		private bool dirty;

		private float nextShare;

		private float nextApply;

		private float doorOpen;

		private float nextBeep;

		private float flash;

		private Vector3 doorClosed;

		private MaterialPropertyBlock block;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

		public static FuseDoor I { get; private set; }

		public bool Open
		{
			get
			{
				// Rebuilt from native code (token 0x06000121, RVA 0x271C1D8)
				return openLeft > 0f;
			}
		}

		public int Slotted
		{
			get
			{
				// Rebuilt from native code (token 0x06000122, RVA 0x271C1E8)
				int count = 0;
				for (int i = 0; i < 3; i++)
				{
					if (where[i] == Fuse.Where.Slotted)
					{
						count++;
					}
				}
				return count;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000123, RVA 0x271C234)
			I = this;
			block = new MaterialPropertyBlock();
			ExitDoorProps.Dress(this);
			if (door)
			{
				doorClosed = door.position;
			}
			for (int i = 0; i < fuses.Length; i++)
			{
				ItemModels.DressFuse(fuses[i]);
				fuses[i].door = this;
				fuses[i].Id = i;
			}
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000124, RVA 0x271C42C)
			SpotsOnPlatforms();
			Scatter(round);
			ApplyAll();
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000125, RVA 0x271CEF4)
			NetBridge.RoomSeedChanged += OnRoomSeedChanged;
			NetBridge.RoomJoined += OnRoomJoined;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000126, RVA 0x271CFF0)
			NetBridge.RoomSeedChanged -= OnRoomSeedChanged;
			NetBridge.RoomJoined -= OnRoomJoined;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000127, RVA 0x271D0EC)
			if (I == this)
			{
				I = null;
			}
		}

		private void SpotsOnPlatforms()
		{
			// Rebuilt from native code (token 0x06000128, RVA 0x271C450)
			SortedDictionary<int, List<BoxCollider>> levels = PoolPlatforms.ByLevel();
			Transform near = door ? door : transform;
			if (!levels.TryGetValue(PoolPlatforms.NearestLevel(levels, near.position), out List<BoxCollider> boxes) || boxes.Count < 3)
			{
				return;
			}
			System.Random random = new System.Random(seed * 7 + 3);
			List<BoxCollider> shuffled = PoolPlatforms.Shuffled(boxes, random);
			if (shuffled.Count > 31)
			{
				shuffled.RemoveRange(31, shuffled.Count - 31);
			}
			Transform[] made = new Transform[shuffled.Count];
			for (int i = 0; i < shuffled.Count; i++)
			{
				Transform spot = new GameObject("Fuse Spot (platform)").transform;
				spot.SetParent(transform, true);
				Vector3 top = PoolPlatforms.RandomTop(shuffled[i], random, 0.35f) + Vector3.up * 0.025f;
				spot.SetPositionAndRotation(top, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 90f));
				made[i] = spot;
			}
			spots = made;
		}

		private void OnRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x06000129, RVA 0x271D218)
			openLeft = 0f;
			round = 0;
			heard = false;
			flash = 0f;
			doorOpen = 0f;
			if (door)
			{
				door.position = doorClosed;
			}
			for (int i = 0; i < 3; i++)
			{
				holder[i] = -1;
				remoteFlight[i] = 0f;
				absentSince[i] = -1f;
			}
			foreach (Fuse fuse in fuses)
			{
				if (fuse)
				{
					fuse.Abandon();
					if (!fuse.gameObject.activeSelf)
					{
						fuse.gameObject.SetActive(true);
					}
				}
			}
			if (spots != null && spots.Length >= 3)
			{
				Scatter(round);
			}
			dirty = true;
			ApplyAll();
		}

		private void OnRoomSeedChanged()
		{
			// Rebuilt from native code (token 0x0600012A, RVA 0x271D3F8)
			if (spots == null || spots.Length < 3 || (heard && !NetBridge.MonsterAuthority))
			{
				return;
			}
			for (int i = 0; i < 3; i++)
			{
				if (where[i] == Fuse.Where.Held || where[i] == Fuse.Where.Slotted)
				{
					return;
				}
			}
			if (openLeft > 0f)
			{
				return;
			}
			Scatter(round);
			dirty = true;
			ApplyAll();
		}

		public void Send(int action, int fuse, Vector3 position)
		{
			// Rebuilt from native code (token 0x0600012B, RVA 0x271D4E8)
			if (!NetBridge.MonsterAuthority)
			{
				NetBridge.RequestFuse(action, fuse, position);
				return;
			}
			Handle(action, fuse, position, NetBridge.LocalPlayerId);
		}

		public void Grabbed(int fuse, Vector3 position)
		{
			// Rebuilt from native code (token 0x0600012C, RVA 0x271D970)
			if (fuse >= 0 && fuse <= 2 && !NetBridge.MonsterAuthority && (where[fuse] == Fuse.Where.Spot || where[fuse] == Fuse.Where.Loose))
			{
				where[fuse] = Fuse.Where.Held;
				holder[fuse] = NetBridge.LocalPlayerId;
				heldSince[fuse] = Time.time;
			}
			Send(Grab, fuse, position);
		}

		public Vector3 FusePosition(int fuse)
		{
			// Rebuilt from native code (token 0x0600012D, RVA 0x271DAAC)
			if (fuse >= 0 && fuse < fuses.Length && fuses[fuse])
			{
				return fuses[fuse].transform.position;
			}
			return transform.position;
		}

		public Fuse.Where State(int fuse)
		{
			// Rebuilt from native code (token 0x0600012E, RVA 0x271DB74)
			return fuse >= 0 && fuse <= 2 ? where[fuse] : Fuse.Where.Spot;
		}

		public bool HeldBy(int fuse, int player)
		{
			// Rebuilt from native code (token 0x0600012F, RVA 0x271DBB4)
			return fuse >= 0 && fuse <= 2 && where[fuse] == Fuse.Where.Held && holder[fuse] == player;
		}

		public void Handle(int action, int fuse, Vector3 position, int player)
		{
			// Rebuilt from native code (token 0x06000130, RVA 0x271D5DC)
			if (fuse < 0 || fuse > 2)
			{
				return;
			}
			dirty = true;
			switch (action)
			{
			case Grab:
				switch (where[fuse])
				{
				case Fuse.Where.Spot:
				case Fuse.Where.Loose:
					where[fuse] = Fuse.Where.Held;
					holder[fuse] = player;
					heldSince[fuse] = Time.time;
					break;
				case Fuse.Where.Held:
					if (holder[fuse] == player)
					{
						heldSince[fuse] = Time.time;
					}
					break;
				}
				break;
			case Land:
				if (where[fuse] == Fuse.Where.Held && holder[fuse] == player)
				{
					holder[fuse] = -1;
					if (Lost(position))
					{
						PutOnSpot(fuse, position);
					}
					else
					{
						where[fuse] = Fuse.Where.Loose;
						loose[fuse] = position;
					}
				}
				break;
			case Slot:
				if (where[fuse] == Fuse.Where.Held && holder[fuse] == player && !(openLeft > 0f))
				{
					holder[fuse] = -1;
					int slot = NearestFreeSlot(position, fuse);
					if (slot >= 0)
					{
						where[fuse] = Fuse.Where.Slotted;
						index[fuse] = slot;
					}
					else
					{
						where[fuse] = Fuse.Where.Loose;
						loose[fuse] = position + new Vector3(-0.4f, -0.55f, 0f);
					}
				}
				break;
			}
			if (NetBridge.LocalPlayerId == player)
			{
				ApplyAll();
			}
		}

		public void RemoteThrow(int fuse, Vector3 position, Vector3 velocity, int player)
		{
			// Rebuilt from native code (token 0x06000131, RVA 0x271DFA0)
			if (fuse < 0 || fuse > 2 || fuse >= fuses.Length || where[fuse] != Fuse.Where.Held || holder[fuse] != player || NetBridge.LocalPlayerId == player)
			{
				return;
			}
			remoteFlight[fuse] = Time.time + 6f;
			Fuse thrown = fuses[fuse];
			if (!thrown.gameObject.activeSelf)
			{
				thrown.gameObject.SetActive(true);
			}
			thrown.Item.Launch(position, velocity);
		}

		public void Receive(FuseSnapshot s)
		{
			// Rebuilt from native code (token 0x06000132, RVA 0x271E178)
			if (NetBridge.MonsterAuthority)
			{
				return;
			}
			heard = true;
			bool blew = s.round == round + 1 && (openLeft > 0f || Slotted > 0 || doorOpen > 0.01f);
			for (int i = 0; i < 3; i++)
			{
				int bits = s.packed >> (i * 8);
				Fuse.Where state = (Fuse.Where)(bits & 7);
				if (state == Fuse.Where.Held && where[i] != Fuse.Where.Held)
				{
					heldSince[i] = Time.time;
				}
				where[i] = state;
				index[i] = (bits >> 3) & 0x1F;
			}
			holder[0] = s.holder0;
			holder[1] = s.holder1;
			holder[2] = s.holder2;
			loose[0] = s.loose0;
			loose[1] = s.loose1;
			loose[2] = s.loose2;
			openLeft = s.openLeft;
			if (blew)
			{
				BlowEffect(round + 1);
			}
			round = s.round;
			ApplyAll();
		}

		private FuseSnapshot Snapshot()
		{
			// Rebuilt from native code (token 0x06000133, RVA 0x271EE4C)
			int packed = 0;
			for (int i = 0; i < 3; i++)
			{
				packed |= ((int)where[i] | (index[i] << 3)) << (i * 8);
			}
			return new FuseSnapshot
			{
				packed = packed,
				holder0 = holder[0],
				holder1 = holder[1],
				holder2 = holder[2],
				loose0 = loose[0],
				loose1 = loose[1],
				loose2 = loose[2],
				openLeft = openLeft,
				round = round
			};
		}

		public bool CanGrab(int fuse)
		{
			// Rebuilt from native code (token 0x06000134, RVA 0x271EF40)
			if (where[fuse] == Fuse.Where.Spot || where[fuse] == Fuse.Where.Loose)
			{
				return true;
			}
			return HeldByMe(fuse);
		}

		public bool HeldByMe(int fuse)
		{
			// Rebuilt from native code (token 0x06000135, RVA 0x271EF88)
			return where[fuse] == Fuse.Where.Held && holder[fuse] == NetBridge.LocalPlayerId;
		}

		public bool TrySlot(Fuse fuse, float reach = -1f)
		{
			// Rebuilt from native code (token 0x06000136, RVA 0x271F044)
			if (openLeft > 0f || slots == null || !fuse)
			{
				return false;
			}
			float best = reach < 0f ? slotReach : reach;
			HandGrabber hand = fuse.Item.Holder;
			Vector3 centre = fuse.transform.position;
			Vector3 half = fuse.transform.up * 0.105f;
			Vector3 a = centre - half;
			Vector3 b = centre + half;
			int slot = -1;
			for (int i = 0; i < slots.Length; i++)
			{
				if (!slots[i] || SlotTaken(i, fuse.Id))
				{
					continue;
				}
				Vector3 p = slots[i].position;
				float distance = DistanceToSegment(p, a, b);
				if (hand)
				{
					distance = Mathf.Min(distance, Vector3.Distance(p, hand.transform.position));
				}
				if (distance < best)
				{
					best = distance;
					slot = i;
				}
			}
			if (slot < 0)
			{
				return false;
			}
			fuse.Item.Place(slots[slot].position, slots[slot].rotation, true);
			fuse.MarkActed();
			if (hand)
			{
				XRHands.Haptic(hand.left, 0.7f, 0.08f);
			}
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.FuseSlot(), slots[slot].position, 0.8f, 1f);
			}
			if (!NetBridge.MonsterAuthority)
			{
				where[fuse.Id] = Fuse.Where.Slotted;
				index[fuse.Id] = slot;
				holder[fuse.Id] = -1;
			}
			Send(Slot, fuse.Id, slots[slot].position);
			return true;
		}

		private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
		{
			// Rebuilt from native code (token 0x06000137, RVA 0x271F734)
			Vector3 ab = b - a;
			float t = Vector3.Dot(p - a, ab) / Mathf.Max(1E-06f, ab.sqrMagnitude);
			return Vector3.Distance(p, a + ab * Mathf.Clamp01(t));
		}

		private bool SlotTaken(int slot, int except)
		{
			// Rebuilt from native code (token 0x06000138, RVA 0x271F6AC)
			for (int i = 0; i < 3; i++)
			{
				if (i != except && where[i] == Fuse.Where.Slotted && index[i] == slot)
				{
					return true;
				}
			}
			return false;
		}

		private int NearestFreeSlot(Vector3 p, int fuse)
		{
			// Rebuilt from native code (token 0x06000139, RVA 0x271DEB8)
			float best = float.MaxValue;
			int found = -1;
			for (int i = 0; i < slots.Length; i++)
			{
				if (SlotTaken(i, fuse))
				{
					continue;
				}
				float distance = (slots[i].position - p).sqrMagnitude;
				if (distance < best)
				{
					best = distance;
					found = i;
				}
			}
			return found;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600013A, RVA 0x271F87C)
			float dt = Time.deltaTime;
			if (!NetBridge.MonsterAuthority)
			{
				if (openLeft > 0f)
				{
					openLeft = Mathf.Max(0f, openLeft - dt);
				}
			}
			else
			{
				for (int i = 0; i < 3; i++)
				{
					bool present = true;
					if (where[i] == Fuse.Where.Held && !NetBridge.PlayerInRoom(holder[i]))
					{
						if (absentSince[i] < 0f)
						{
							absentSince[i] = Time.time;
						}
						present = false;
					}
					else
					{
						absentSince[i] = -1f;
					}
					if (where[i] == Fuse.Where.Held && (Time.time - heldSince[i] > heldTimeout || (!present && Time.time - absentSince[i] > GoneFor)))
					{
						holder[i] = -1;
						absentSince[i] = -1f;
						PutOnSpot(i);
						dirty = true;
					}
					else if (where[i] == Fuse.Where.Loose && Lost(loose[i]))
					{
						PutOnSpot(i, loose[i]);
						dirty = true;
					}
				}
				if (openLeft > 0f)
				{
					openLeft -= dt;
					if (openLeft <= 0f)
					{
						openLeft = 0f;
						round++;
						BlowEffect(round);
						Scatter(round);
						dirty = true;
					}
				}
				else if (Slotted == 3)
				{
					openLeft = openSeconds;
					dirty = true;
				}
				if (dirty || Time.time >= nextShare)
				{
					nextShare = Time.time + 3f;
					NetBridge.ShareFuses(Snapshot());
					if (dirty)
					{
						ApplyAll();
					}
					dirty = false;
				}
			}
			if (Time.time >= nextApply)
			{
				ApplyAll();
			}
			UpdateDoor(dt);
		}

		private void Scatter(int r)
		{
			// Rebuilt from native code (token 0x0600013B, RVA 0x271C8E4)
			System.Random random = new System.Random(seed * 31 + r * 7919 + NetBridge.RoomSeed);
			int count = spots.Length;
			int[] order = new int[count];
			for (int i = 0; i < count; i++)
			{
				order[i] = i;
			}
			for (int i = count - 1; i > 0; i--)
			{
				int j = random.Next(i + 1);
				(order[i], order[j]) = (order[j], order[i]);
			}
			for (int i = 0; i < 3; i++)
			{
				where[i] = Fuse.Where.Spot;
				index[i] = order[i % count];
				holder[i] = -1;
			}
		}

		internal static bool Lost(Vector3 position)
		{
			// Rebuilt from native code (token 0x0600013C, RVA 0x271DC24)
			WaterVolume water = WaterVolume.At(position);
			float floor = water ? Mathf.Min(-2f, water.FloorY - 1f) : -2f;
			return position.y < floor;
		}

		private void PutOnSpot(int fuse, Vector3 near)
		{
			// Rebuilt from native code (token 0x0600013D, RVA 0x271DD10)
			float best = float.MaxValue;
			int found = -1;
			for (int i = 0; i < spots.Length; i++)
			{
				if (!spots[i] || SpotUsed(i, fuse))
				{
					continue;
				}
				float distance = (spots[i].position - near).sqrMagnitude;
				if (distance < best)
				{
					best = distance;
					found = i;
				}
			}
			if (found < 0)
			{
				PutOnSpot(fuse);
				return;
			}
			where[fuse] = Fuse.Where.Spot;
			index[fuse] = found;
		}

		private bool SpotUsed(int spot, int except)
		{
			// Rebuilt from native code (token 0x0600013E, RVA 0x2720940)
			for (int i = 0; i < 3; i++)
			{
				if (i != except && where[i] == Fuse.Where.Spot && index[i] == spot)
				{
					return true;
				}
			}
			return false;
		}

		private void PutOnSpot(int fuse)
		{
			// Rebuilt from native code (token 0x0600013F, RVA 0x271FD00)
			for (int attempt = 0; attempt < 20; attempt++)
			{
				int spot = Random.Range(0, spots.Length);
				if (!SpotUsed(spot, fuse))
				{
					where[fuse] = Fuse.Where.Spot;
					index[fuse] = spot;
					return;
				}
			}
			where[fuse] = Fuse.Where.Spot;
			index[fuse] = fuse;
		}

		private void ApplyAll()
		{
			// Rebuilt from native code (token 0x06000140, RVA 0x271CB18)
			nextApply = Time.time + 1f;
			for (int i = 0; i < 3 && i < fuses.Length; i++)
			{
				Fuse fuse = fuses[i];
				int held = holder[i];
				int me = NetBridge.LocalPlayerId;
				if (where[i] != Fuse.Where.Held)
				{
					remoteFlight[i] = 0f;
				}
				else if (held != me && Time.time < remoteFlight[i])
				{
					continue;
				}
				if (where[i] == Fuse.Where.Held && held != me)
				{
					fuse.Abandon();
					if (fuse.gameObject.activeSelf)
					{
						fuse.gameObject.SetActive(false);
					}
					continue;
				}
				if (!fuse.gameObject.activeSelf)
				{
					fuse.gameObject.SetActive(true);
				}
				if (fuse.Busy)
				{
					continue;
				}
				switch (where[i])
				{
				case Fuse.Where.Spot:
				{
					Transform spot = spots[Mathf.Clamp(index[i], 0, spots.Length - 1)];
					MoveTo(fuse, spot.position, spot.rotation);
					break;
				}
				case Fuse.Where.Held:
					Send(Land, i, fuse.transform.position);
					break;
				case Fuse.Where.Loose:
					MoveTo(fuse, loose[i], fuse.transform.rotation);
					break;
				case Fuse.Where.Slotted:
				{
					Transform slot = slots[Mathf.Clamp(index[i], 0, slots.Length - 1)];
					MoveTo(fuse, slot.position, slot.rotation);
					break;
				}
				}
			}
		}

		private static void MoveTo(Fuse f, Vector3 position, Quaternion rotation)
		{
			// Rebuilt from native code (token 0x06000141, RVA 0x27209C4)
			float moved = (f.transform.position - position).sqrMagnitude;
			Rigidbody body = f.Item.GetComponent<Rigidbody>();
			if (moved < 0.0004f && body.isKinematic)
			{
				return;
			}
			f.Item.Place(position, rotation, true);
			f.Item.SetHome(position, rotation);
		}

		private void UpdateDoor(float dt)
		{
			// Rebuilt from native code (token 0x06000142, RVA 0x271FE40)
			bool open = openLeft > 0f;
			float was = doorOpen;
			doorOpen = Mathf.MoveTowards(doorOpen, open ? 1f : 0f, (open ? 0.6f : 5f) * dt);
			if (door)
			{
				door.position = doorClosed + openOffset * (doorOpen * doorOpen * (3f - 2f * doorOpen));
				if (was > 0f && doorOpen <= 0f && SplashSystem.I)
				{
					SplashSystem.I.PlaySound(Sfx.DoorSlam(), doorClosed, 1f, 0.55f);
					if (PlayerRig.I && (PlayerRig.I.head.position - doorClosed).sqrMagnitude < 100f)
					{
						XRHands.HapticBoth(0.5f, 0.15f);
					}
				}
			}
			if (open && Time.time >= nextBeep && SplashSystem.I)
			{
				nextBeep = Time.time + (openLeft < 5f ? 0.5f : (openLeft < 10f ? 1f : 2f));
				SplashSystem.I.PlaySound(Sfx.DoorAlarm(), doorClosed + Vector3.up * 3f, openLeft < 10f ? 0.9f : 0.5f, 1f);
			}
			flash = Mathf.MoveTowards(flash, 0f, dt * 3f);
			float pulse = open ? MathF.Sin(Time.time * (openLeft < 10f ? 14f : 6f)) * 0.5f + 0.5f : 0f;
			if (alarm)
			{
				alarm.enabled = open || flash > 0f;
				alarm.color = flash > 0f ? Color.Lerp(new Color(1f, 0.2f, 0.1f), new Color(1f, 0.95f, 0.8f), flash) : new Color(1f, 0.2f, 0.1f);
				alarm.intensity = pulse * 2.5f + flash * 6f;
			}
			if (alarmLamp)
			{
				Color lamp = flash > 0f ? Color.Lerp(new Color(1f, 0.2f, 0.1f), new Color(1f, 0.95f, 0.8f), flash) : new Color(1f, 0.2f, 0.1f);
				float glow = Mathf.Max(pulse, flash);
				block.SetColor(BaseColorId, Color.Lerp(new Color(0.45f, 0.07f, 0.05f), lamp, glow));
				block.SetColor(EmissionId, lamp * (glow * 3f + 0.15f));
				alarmLamp.SetPropertyBlock(block);
			}
			if (countdown)
			{
				string text = open ? string.Format("DOOR OPEN\n{0}", Mathf.CeilToInt(openLeft)) : string.Format("FUSES {0}/3", Slotted);
				if (countdown.text != text)
				{
					countdown.text = text;
				}
				bool hurry = open && openLeft < 10f;
				countdown.color = new Color(1f, hurry ? 0.3f : 0.85f, hurry ? 0.2f : 0.4f, 1f);
			}
			if (slotLamps == null)
			{
				return;
			}
			for (int i = 0; i < slotLamps.Length; i++)
			{
				if (slotLamps[i])
				{
					Color c = SlotTaken(i, -1) ? new Color(0.3f, 1f, 0.4f, 1f) : new Color(0.25f, 0.05f, 0.03f, 1f);
					block.SetColor(BaseColorId, c);
					block.SetColor(EmissionId, c * 1.5f);
					slotLamps[i].SetPropertyBlock(block);
				}
			}
		}

		private void BlowEffect(int r)
		{
			// Rebuilt from native code (token 0x06000143, RVA 0x271E3D4)
			flash = 1f;
			System.Random random = new System.Random(r + seed * 131);
			for (int i = 0; i < slots.Length; i++)
			{
				Vector3 position = slots[i].position;
				if (SplashSystem.I)
				{
					SplashSystem.I.PlaySound(Sfx.FusesBlow(), position, 1f, 0.9f + i * 0.1f);
				}
				GameObject husk;
				CapsuleCollider capsule;
				if (ItemModels.Has("BlownFuse"))
				{
					husk = new GameObject("Blown Fuse");
					husk.transform.SetPositionAndRotation(position, slots[i].rotation);
					ItemModels.Wear(husk.transform, "BlownFuse");
					capsule = husk.AddComponent<CapsuleCollider>();
					capsule.radius = 0.025f;
					capsule.height = 0.21f;
				}
				else
				{
					husk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
					husk.name = "Blown Fuse";
					husk.transform.SetPositionAndRotation(position, slots[i].rotation);
					husk.transform.localScale = new Vector3(0.05f, 0.07f, 0.05f);
					Destroy(husk.GetComponent<Collider>());
					capsule = husk.AddComponent<CapsuleCollider>();
					capsule.radius = 0.5f;
					capsule.height = 2f;
					if (huskMaterial)
					{
						husk.GetComponent<Renderer>().sharedMaterial = huskMaterial;
					}
				}
				husk.layer = 2;
				if (PlayerRig.I)
				{
					foreach (Collider part in PlayerRig.I.Colliders)
					{
						if (part)
						{
							Physics.IgnoreCollision(capsule, part, true);
						}
					}
				}
				Rigidbody body = husk.AddComponent<Rigidbody>();
				body.mass = 0.2f;
				body.interpolation = RigidbodyInterpolation.Interpolate;
				body.linearVelocity = -transform.right * ((float)random.NextDouble() * 3f + 3.5f) + Vector3.up * ((float)random.NextDouble() * 2f + 2.5f) + transform.forward * ((float)random.NextDouble() * 4f - 2f);
				body.angularVelocity = new Vector3((float)random.NextDouble() * 12f, (float)random.NextDouble() * 12f, (float)random.NextDouble() * 12f);
				husk.AddComponent<FuseHusk>().loud = random.NextDouble() < 0.67;
			}
			if (PlayerRig.I && (PlayerRig.I.head.position - doorClosed).sqrMagnitude < 400f)
			{
				XRHands.HapticBoth(0.4f, 0.2f);
			}
		}
	}
}
