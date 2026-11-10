using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(120)]
	public class Belt : MonoBehaviour
	{
		private class Slot
		{
			public Throwable item;

			public ShopItem shop;

			public ItemKind last;

			public bool hasLast;

			public float refillAt;
		}

		public PlayerRig rig;

		[Tooltip("Only without the gorilla model (it has a belt round its waist): where the slots are, from your eyes: x to your right, y up, z forward (m).")]
		public Vector3[] slots = new Vector3[3]
		{
			new Vector3(-0.2f, -0.42f, 0.08f),
			new Vector3(0f, -0.44f, 0.16f),
			new Vector3(0.2f, -0.42f, 0.08f)
		};

		[Tooltip("How close to an empty slot the item in your hand has to be when you let go to put it in (m). A hand on the floatie (its outline lit up) always does.")]
		public float stashReach = 0.16f;

		[Tooltip("Your hand moving faster than this (m/s, against your body, so walking or riding a platform doesn't count) when you let go throws the item instead of putting it in.")]
		public float stashMaxSpeed = 2.5f;

		public const int FuseCode = 15;

		private Slot[] state;

		private float yaw;

		private bool yawSet;

		public static Belt I { get; private set; }

		private static BeltLook Look
		{
			get
			{
				// Rebuilt from native code (token 0x0600088A, RVA 0x282CD10)
				return ApeBody.Local ? ApeBody.Local.belt : null;
			}
		}

		public int Found
		{
			get
			{
				// Rebuilt from native code (token 0x0600088E, RVA 0x282D5D8)
				if (state == null)
				{
					return 0;
				}
				int found = 0;
				foreach (Slot slot in state)
				{
					if (slot.item && !slot.shop)
					{
						found++;
					}
				}
				return found;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000885, RVA 0x282C76C)
			I = this;
			state = new Slot[slots.Length];
			for (int i = 0; i < state.Length; i++)
			{
				state[i] = new Slot();
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000886, RVA 0x282C8CC)
			if (I == this)
			{
				I = null;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000887, RVA 0x282C9A8)
			Wallet.Changed += OnWalletChanged;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000888, RVA 0x282CA4C)
			Wallet.Changed -= OnWalletChanged;
		}

		private void OnWalletChanged()
		{
			// Rebuilt from native code (token 0x06000889, RVA 0x282CAF0)
			Trim();
		}

		public Vector3 SlotPosition(int i)
		{
			// Rebuilt from native code (token 0x0600088B, RVA 0x282CE2C)
			SlotPose(i, out Vector3 position, out _);
			return position;
		}

		private void SlotPose(int i, out Vector3 position, out Quaternion rotation)
		{
			// Rebuilt from native code (token 0x0600088C, RVA 0x282CE60)
			BeltLook look = Look;
			if (look && i < BeltLook.Slots)
			{
				look.ItemPose(i, out position, out rotation);
				return;
			}
			rotation = Quaternion.Euler(0f, yaw, 0f);
			position = rig.head.position + rotation * slots[i];
		}

		public int SlotCode(int i)
		{
			// Rebuilt from native code (token 0x0600088D, RVA 0x282D358)
			if (state == null || i >= state.Length || !state[i].item)
			{
				return 0;
			}
			if (state[i].shop)
			{
				return (int)state[i].shop.kind + 1;
			}
			ShopItem shop = state[i].item.GetComponent<ShopItem>();
			if (shop)
			{
				return (int)shop.kind + 1;
			}
			if (state[i].item.GetComponent<LoungeCan>())
			{
				return LoungeCan.Kind + 1;
			}
			return state[i].item.GetComponent<Fuse>() ? FuseCode : 1;
		}

		public static void Size(Throwable item, out float radius, out float length)
		{
			// Rebuilt from native code (token 0x0600088F, RVA 0x282D6D4)
			length = 0f;
			ShopItem shop = item.GetComponent<ShopItem>();
			if (shop)
			{
				radius = Items.Radius(shop.kind);
				length = Items.Length(shop.kind);
				return;
			}
			if (item.GetComponent<Fuse>())
			{
				radius = 0.025f;
				length = 0.21f;
				return;
			}
			radius = Items.Radius(item.GetComponent<LoungeCan>() ? (ItemKind)LoungeCan.Kind : ItemKind.Pebble);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000890, RVA 0x282D968)
			if (!rig || !rig.head)
			{
				return;
			}
			Forget();
			Fill();
			BeltLook look = Look;
			if (look)
			{
				ShowSlots(look);
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000891, RVA 0x282E0D0)
			if (!rig || !rig.head)
			{
				return;
			}
			float heading = rig.head.eulerAngles.y;
			if (!yawSet)
			{
				yaw = heading;
				yawSet = true;
			}
			float delta = Mathf.DeltaAngle(yaw, heading);
			if (Mathf.Abs(delta) > 35f)
			{
				yaw += delta - (delta < 0f ? -35f : 35f);
			}
			yaw = Mathf.MoveTowardsAngle(yaw, heading, Mathf.Abs(delta) * 2f * Time.deltaTime);
			if (Look)
			{
				return;
			}
			for (int i = 0; i < state.Length; i++)
			{
				Throwable item = state[i].item;
				if (item)
				{
					SlotPose(i, out Vector3 position, out Quaternion rotation);
					item.transform.SetPositionAndRotation(position, rotation);
				}
			}
		}

		private void ShowSlots(BeltLook look)
		{
			// Rebuilt from native code (token 0x06000892, RVA 0x282DCBC)
			for (int i = 0; i < BeltLook.Slots && i < state.Length; i++)
			{
				Throwable item = state[i].item;
				float radius = 0f;
				float length = 0f;
				if (item)
				{
					Size(item, out radius, out length);
				}
				look.Show(i, item ? item.transform : null, radius, length);
			}
			Vector3 left = Vector3.zero;
			Vector3 right = Vector3.zero;
			BeltLook.Hand leftHolds = BeltLook.Hand.Other;
			BeltLook.Hand rightHolds = BeltLook.Hand.Other;
			float leftFit = 0f;
			float rightFit = 0f;
			GameManager game = GameManager.I;
			if (game && game.grabbers != null)
			{
				foreach (HandGrabber grabber in game.grabbers)
				{
					if (!grabber)
					{
						continue;
					}
					BeltLook.Hand holds = grabber.Held ? BeltLook.Hand.BeltItem : BeltLook.Hand.Empty;
					float fit = 0f;
					if (grabber.Held)
					{
						Size(grabber.Held, out float heldRadius, out float heldLength);
						fit = BeltLook.Fit(heldRadius, heldLength);
					}
					if (grabber.left)
					{
						left = grabber.transform.position;
						leftHolds = holds;
						leftFit = fit;
					}
					else
					{
						right = grabber.transform.position;
						rightHolds = holds;
						rightFit = fit;
					}
				}
			}
			look.Reach(left, leftHolds, leftFit, right, rightHolds, rightFit);
		}

		internal void Took(Throwable item)
		{
			// Rebuilt from native code (token 0x06000893, RVA 0x282E754)
			for (int i = 0; i < state.Length; i++)
			{
				if (state[i].item != item)
				{
					continue;
				}
				state[i].item = null;
				ShopItem shop = state[i].shop;
				state[i].shop = null;
				state[i].refillAt = Time.time + 0.6f;
				if (shop)
				{
					shop.used = true;
					Wallet.Use(shop.kind);
				}
				Pop(item.transform.position);
				return;
			}
		}

		internal bool TryStash(Throwable item, Vector3 hand, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000894, RVA 0x282EAD0)
			if (!rig || !item || velocity.magnitude > stashMaxSpeed || rig.Frozen)
			{
				return false;
			}
			int best = -1;
			float bestDistance = float.MaxValue;
			for (int i = 0; i < state.Length; i++)
			{
				if (state[i].item)
				{
					continue;
				}
				Vector3 held = item.transform.position;
				SlotPose(i, out Vector3 slot, out _);
				float toFloatie = Vector3.Distance(hand, FloatiePosition(i));
				float toSlot = Vector3.Distance(held, slot);
				if (toFloatie > 0.17f && toSlot > stashReach)
				{
					continue;
				}
				float distance = Mathf.Min(toSlot, toFloatie);
				if (distance < bestDistance)
				{
					best = i;
					bestDistance = distance;
				}
			}
			if (best < 0)
			{
				return false;
			}
			Put(best, item);
			ShopItem shop = state[best].shop;
			if (shop && shop.used)
			{
				shop.used = false;
				Wallet.Unuse(shop.kind);
			}
			Pop(item.transform.position);
			return true;
		}

		internal bool PutBack(Throwable item)
		{
			// Rebuilt from native code (token 0x06000895, RVA 0x282F19C)
			if (!rig || !item)
			{
				return false;
			}
			ShopItem shop = item.GetComponent<ShopItem>();
			int slot = -1;
			for (int i = 0; i < state.Length; i++)
			{
				if (state[i].item)
				{
					continue;
				}
				if (shop && state[i].hasLast && state[i].last == shop.kind)
				{
					slot = i;
					break;
				}
				if (slot < 0)
				{
					slot = i;
				}
			}
			if (slot < 0)
			{
				return false;
			}
			Put(slot, item);
			ShopItem put = state[slot].shop;
			if (put && put.used)
			{
				put.used = false;
				Wallet.Unuse(put.kind);
			}
			Pop(item.transform.position);
			return true;
		}

		internal float Reach(Throwable item, Vector3 hand)
		{
			// Rebuilt from native code (token 0x06000896, RVA 0x282F400)
			for (int i = 0; i < state.Length; i++)
			{
				if (state[i].item == item)
				{
					return Vector3.Distance(hand, FloatiePosition(i)) - 0.05f;
				}
			}
			return float.MaxValue;
		}

		internal bool NearBelt(Vector3 hand, float within)
		{
			// Rebuilt from native code (token 0x06000897, RVA 0x282F560)
			for (int i = 0; i < state.Length; i++)
			{
				if (Vector3.Distance(hand, FloatiePosition(i)) <= within)
				{
					return true;
				}
			}
			return false;
		}

		private Vector3 FloatiePosition(int i)
		{
			// Rebuilt from native code (token 0x06000898, RVA 0x282EEAC)
			BeltLook look = Look;
			if (look && i < BeltLook.Slots)
			{
				return look.Slot(i).position;
			}
			SlotPose(i, out Vector3 position, out _);
			return position;
		}

		private static void Pop(Vector3 at)
		{
			// Rebuilt from native code (token 0x06000899, RVA 0x282E938)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.BeltStash(), at, 0.4f, 1.3f);
			}
		}

		private void Put(int i, Throwable item)
		{
			// Rebuilt from native code (token 0x0600089A, RVA 0x282EF80)
			item.Stow();
			Fuse fuse = item.GetComponent<Fuse>();
			if (fuse)
			{
				fuse.MarkActed();
			}
			SlotPose(i, out Vector3 position, out Quaternion rotation);
			item.transform.SetPositionAndRotation(position, rotation);
			ShopItem shop = item.GetComponent<ShopItem>();
			if (shop && shop.found)
			{
				shop = null;
			}
			state[i].item = item;
			state[i].shop = shop;
			if (shop)
			{
				state[i].hasLast = true;
				state[i].last = shop.kind;
			}
		}

		private void Forget()
		{
			// Rebuilt from native code (token 0x0600089B, RVA 0x282DA4C)
			foreach (Slot slot in state)
			{
				if (slot.item && (!slot.item.Stowed || !slot.item.gameObject.activeInHierarchy))
				{
					slot.item = null;
					slot.shop = null;
				}
			}
		}

		private void Fill()
		{
			// Rebuilt from native code (token 0x0600089C, RVA 0x282DB50)
			for (int i = 0; i < state.Length; i++)
			{
				Slot slot = state[i];
				if (slot.item || Time.time < slot.refillAt)
				{
					continue;
				}
				if (Pick(slot, out ItemKind kind))
				{
					SlotPose(i, out Vector3 position, out _);
					Put(i, Items.Spawn(kind, position).Item);
				}
			}
		}

		private bool Pick(Slot slot, out ItemKind kind)
		{
			// Rebuilt from native code (token 0x0600089D, RVA 0x282F690)
			if (slot.hasLast && Spare(slot.last) > 0)
			{
				kind = slot.last;
				return true;
			}
			for (int pass = 0; pass < 2; pass++)
			{
				foreach (ShopCatalogue.Entry entry in ShopCatalogue.Items)
				{
					if (Spare(entry.kind) > 0 && (pass == 1 || Showing(entry.kind) == 0))
					{
						kind = entry.kind;
						return true;
					}
				}
			}
			kind = ItemKind.Pebble;
			return false;
		}

		private int Showing(ItemKind kind)
		{
			// Rebuilt from native code (token 0x0600089E, RVA 0x282FADC)
			int showing = 0;
			foreach (Slot slot in state)
			{
				if (slot.shop && slot.shop.kind == kind)
				{
					showing++;
				}
			}
			return showing;
		}

		private int Spare(ItemKind kind)
		{
			// Rebuilt from native code (token 0x0600089F, RVA 0x282FA60)
			return Wallet.Count(kind) - Showing(kind);
		}

		private void Trim()
		{
			// Rebuilt from native code (token 0x060008A0, RVA 0x282CAF4)
			if (state == null)
			{
				return;
			}
			foreach (ShopCatalogue.Entry entry in ShopCatalogue.Items)
			{
				int extra = -Spare(entry.kind);
				for (int i = state.Length - 1; i >= 0 && extra > 0; i--)
				{
					if (state[i].shop && state[i].shop.kind == entry.kind)
					{
						Destroy(state[i].shop.gameObject);
						state[i].item = null;
						state[i].shop = null;
						extra--;
					}
				}
			}
		}
	}
}
