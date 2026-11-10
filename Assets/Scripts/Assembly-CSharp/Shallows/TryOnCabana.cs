using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(120)]
	public class TryOnCabana : MonoBehaviour
	{
		private class Panel
		{
			public Transform root;

			public float width;

			public float height;
		}

		private class Tile
		{
			public string id;

			public PokeButton button;

			public Transform icon;

			public Renderer strip;

			public TextMesh tag;

			public Transform pearl;

			public float width;
		}

		private class CartLine
		{
			public Transform root;

			public TextMesh name;

			public TextMesh price;

			public Transform pearl;

			public PokeButton remove;

			public PokeButton buy;

			public TextMesh buyLabel;

			public string id;
		}

		private const string Prefix = "Anchor TryOn ";

		[Tooltip("Eye height of the big monke of you (metres; a player's is about 0.55).")]
		public float monkeEye = 0.8f;

		[Tooltip("How high the monke of you floats over its turntable (metres), so its arms don't go through the floor when yours hang down.")]
		public float hover = 0.4f;

		[Tooltip("How far it bobs up and down while it floats (metres).")]
		public float bob = 0.025f;

		[Tooltip("How far the monke turns per degree you turn the wheel.")]
		public float wheelGear = 1.5f;

		[Tooltip("How quickly a spun monke slows down after you let go of the wheel (per second).")]
		public float spinDrag = 2.2f;

		[Tooltip("How much of your own head turning the monke copies (0 = it just looks out at you).")]
		[Range(0f, 1f)]
		public float headFollow = 0.35f;

		[Tooltip("Beyond this distance (metres) from the monke, the menu and the monke switch off.")]
		public float awakeRange = 9f;

		private static readonly Color TileIdle = new Color(0.07f, 0.19f, 0.13f, 1f);

		private static readonly Color TileOwned = new Color(0.38f, 0.3f, 0.07f, 1f);

		private static readonly Color TileTrying = new Color(0.12f, 0.52f, 0.58f, 1f);

		private static readonly Color TileWorn = new Color(0.2f, 0.62f, 0.32f, 1f);

		private static readonly Color TabIdle = new Color(0.1f, 0.26f, 0.18f, 1f);

		private static readonly Color TabOn = new Color(0.45f, 0.95f, 0.55f, 1f);

		private static readonly Color Pressed = new Color(1f, 0.85f, 0.45f, 1f);

		private static readonly Color Green = new Color(0.62f, 1f, 0.66f, 1f);

		private static readonly Color Dim = new Color(0.38f, 0.66f, 0.44f, 1f);

		private static readonly Color Ink = new Color(0.03f, 0.1f, 0.05f, 1f);

		private static readonly Color PearlCyan = new Color(0.47f, 0.91f, 1f, 1f);

		private static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);

		private static readonly Color White = new Color(0.95f, 0.98f, 0.95f, 1f);

		private static readonly Color BuyRed = new Color(0.86f, 0.2f, 0.16f, 1f);

		private static readonly Color Grey = new Color(0.22f, 0.26f, 0.24f, 1f);

		private static readonly Color BuyGreen = new Color(0.22f, 0.8f, 0.4f, 1f);

		private static readonly Color TagDark = new Color(0.02f, 0.05f, 0.035f, 1f);

		private static readonly Color TagWearing = new Color(0.45f, 0.95f, 0.55f, 1f);

		private const float TagH = 0.026f;

		private const float TagText = 0.012f;

		private const float ScreenH = 0.415f;

		private const float MenuW = 1.1f;

		private const float CartW = 0.54f;

		private RippleShop shop;

		private Bounds zone;

		private Transform turntable;

		private Transform wheel;

		private Quaternion turntableRest;

		private Quaternion wheelRest;

		private Vector3 stageTop;

		private Vector3 wheelCentre;

		private Vector3 wheelAxis;

		private Vector3 wheelRef;

		private Quaternion stageFacing;

		private Panel menu;

		private Panel cart;

		private Transform uiRoot;

		private readonly List<(string slot, string label)> tabs = new List<(string slot, string label)>();

		private readonly List<PokeButton> tabButtons = new List<PokeButton>();

		private readonly List<TextMesh> tabLabels = new List<TextMesh>();

		private readonly Dictionary<string, List<Tile>> tilesBySlot = new Dictionary<string, List<Tile>>();

		private readonly Dictionary<string, List<Transform>> tabPages = new Dictionary<string, List<Transform>>();

		private readonly Dictionary<string, int> pageOf = new Dictionary<string, int>();

		private string tab;

		private Transform tilesRoot;

		private string ownedPieces;

		private readonly List<CartLine> lines = new List<CartLine>();

		private TextMesh balance;

		private TextMesh total;

		private TextMesh totalAmount;

		private TextMesh cartNote;

		private TextMesh cartHint;

		private TextMesh buyAllLabel;

		private TextMesh clearLabel;

		private PokeButton buyAll;

		private PokeButton clearAll;

		private Transform totalPearl;

		private string note;

		private float noteUntil;

		private bool buying;

		private const float ConfirmTime = 3f;

		private float confirmUntil;

		private int confirmSum;

		private Transform buyAllPearl;

		private float spin;

		private float spinVelocity;

		private float wheelAngle;

		private float lastClick;

		private bool facingHome;

		private readonly bool[] grabbing = new bool[2];

		private readonly float[] handAngle = new float[2];

		private ApeBody monke;

		private CosmeticLook monkeLook;

		private Transform proxyHead;

		private Transform proxyLeft;

		private Transform proxyRight;

		private float eyeLift;

		private bool awake = true;

		private bool dirty = true;

		private static Material tileMaterial;

		private static Material pearlMaterial;

		private static readonly Dictionary<string, List<(Mesh mesh, Material[] materials, Matrix4x4 pose)>> bakedIcons = new Dictionary<string, List<(Mesh mesh, Material[] materials, Matrix4x4 pose)>>();

		private const string SetsTab = "sets";

		private static Quaternion? front;

		private static readonly List<TryOnCabana> stations = new List<TryOnCabana>();

		private readonly bool[] wasOn = new bool[2];

		private readonly bool[] wasGrab = new bool[2];

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060009EB, RVA 0x2749114)
			pearlMaterial = null;
			tileMaterial = null;
			bakedIcons.Clear();
			stations.Clear();
			front = null;
		}

		public static void Build(RippleShop shop, Transform model)
		{
			// Rebuilt from native code (token 0x060009EC, RVA 0x2749210)
			if (!Application.isPlaying || !model)
			{
				return;
			}
			if (shop.GetComponentInChildren<TryOnCabana>())
			{
				return;
			}
			int n = 1;
			while (BuildStation(shop, model, n == 1 ? "" : n + " "))
			{
				n++;
			}
		}

		private static bool BuildStation(RippleShop shop, Transform model, string tag)
		{
			// Rebuilt from native code (token 0x060009ED, RVA 0x27493A8)
			Transform Mark(string name) => LifeguardProps.Find(model, Prefix + tag + name);

			Transform menuCentre = Mark("Menu");
			Transform menuRight = Mark("Menu X");
			Transform menuUp = Mark("Menu Y");
			Transform cartCentre = Mark("Cart");
			Transform cartRight = Mark("Cart X");
			Transform cartUp = Mark("Cart Y");
			Transform wheelMark = Mark("Wheel");
			Transform wheelUp = Mark("Wheel Up");
			Transform stage = Mark("Stage");
			Transform zoneMin = LifeguardProps.Find(model, "Anchor TryOn Zone Min");
			Transform zoneMax = LifeguardProps.Find(model, "Anchor TryOn Zone Max");
			if (!menuCentre || !menuRight || !menuUp || !cartCentre || !cartRight || !cartUp || !wheelMark || !wheelUp || !stage || !zoneMin || !zoneMax)
			{
				return false;
			}
			GameObject go = new GameObject(tag == "" ? "Try-On Cabana" : "Try-On Cabana " + tag.Trim());
			go.transform.SetParent(shop.transform, false);
			TryOnCabana station = go.AddComponent<TryOnCabana>();
			station.shop = shop;
			station.zone = new Bounds(shop.transform.InverseTransformPoint(zoneMin.position), Vector3.zero);
			station.zone.Encapsulate(shop.transform.InverseTransformPoint(zoneMax.position));
			station.turntable = LifeguardProps.Find(model, "TryOn " + tag + "Turntable");
			station.wheel = LifeguardProps.Find(model, "TryOn " + tag + "Wheel");
			if (station.turntable)
			{
				station.turntableRest = station.turntable.rotation;
			}
			if (station.wheel)
			{
				station.wheelRest = station.wheel.rotation;
			}
			station.stageTop = stage.position;
			station.wheelCentre = wheelMark.position;
			station.wheelAxis = (wheelUp.position - wheelMark.position).normalized;
			station.wheelRef = Vector3.ProjectOnPlane(shop.transform.right, station.wheelAxis).normalized;
			Vector3 toMenu = Vector3.ProjectOnPlane(menuCentre.position - stage.position, shop.transform.up);
			Vector3 facing = toMenu.sqrMagnitude > 0.0001f ? toMenu.normalized : -shop.transform.forward;
			station.stageFacing = Quaternion.LookRotation(facing, shop.transform.up);
			station.uiRoot = new GameObject("Screens").transform;
			station.uiRoot.SetParent(go.transform, false);
			station.menu = station.MakePanel("Menu", menuCentre, menuRight, menuUp, MenuW, ScreenH);
			station.cart = station.MakePanel("Cart", cartCentre, cartRight, cartUp, CartW, ScreenH);
			station.BuildMenu();
			station.BuildCart();
			station.BuildFaceMe();
			return true;
		}

		private Panel MakePanel(string name, Transform centre, Transform right, Transform up, float width, float height)
		{
			// Rebuilt from native code (token 0x060009EE, RVA 0x2749E8C)
			Vector3 x = (right.position - centre.position).normalized;
			Vector3 y = (up.position - centre.position).normalized;
			Transform root = new GameObject(name).transform;
			root.SetParent(uiRoot, false);
			root.SetPositionAndRotation(centre.position, Quaternion.LookRotation(Vector3.Cross(x, y), y));
			return new Panel
			{
				root = root,
				width = width,
				height = height
			};
		}

		private static string TabLabel(string slot)
		{
			// Rebuilt from native code (token 0x060009EF, RVA 0x274BD50)
			return slot switch
			{
				"head" => "HEAD",
				"face" => "FACE",
				"neck" => "NECK",
				"outfit" => "BODY",
				_ => slot.ToUpperInvariant()
			};
		}

		private static int TabOrder(string slot)
		{
			// Rebuilt from native code (token 0x060009F0, RVA 0x274BE98)
			return slot switch
			{
				"head" => 0,
				"face" => 1,
				"neck" => 2,
				"outfit" => 9,
				_ => 5
			};
		}

		private void BuildMenu()
		{
			// Rebuilt from native code (token 0x060009F1, RVA 0x274A20C)
			List<string> slots = new List<string>();
			bool hasSets = false;
			foreach (Cosmetics.Entry e in Cosmetics.All)
			{
				if (IsSet(e))
				{
					hasSets = true;
				}
				else if (InSlotTab(e, false) && !slots.Contains(e.slot))
				{
					slots.Add(e.slot);
				}
			}
			slots.Sort((a, b) => TabOrder(a).CompareTo(TabOrder(b)));
			if (hasSets)
			{
				tabs.Add((SetsTab, "SETS"));
			}
			foreach (string slot in slots)
			{
				tabs.Add((slot, TabLabel(slot)));
			}
			if (tabs.Count == 0)
			{
				return;
			}
			float height = (menu.height - 0.012f * (tabs.Count + 2)) / (tabs.Count + 1);
			float x = 0.012f - menu.width * 0.5f + 0.08f;
			float top = menu.height * 0.5f - 0.012f - height * 0.5f;
			for (int i = 0; i < tabs.Count; i++)
			{
				int index = i;
				PokeButton button = Button(menu.root, "Tab " + tabs[i].label, new Vector2(x, top - (height + 0.012f) * i), new Vector2(0.16f, height), TabIdle);
				button.onPress = () => ShowTab(tabs[index].slot);
				tabButtons.Add(button);
				tabLabels.Add(Label(button.transform.parent, "Label", Vector2.zero, tabs[i].label, 0.02f, Green, TextAnchor.MiddleCenter));
				pageOf[tabs[i].slot] = 0;
			}
			BuildTiles();
			ShowTab(tabs[0].slot);
		}

		private void BuildTiles()
		{
			// Rebuilt from native code (token 0x060009F2, RVA 0x274C590)
			if (tilesRoot)
			{
				Destroy(tilesRoot.gameObject);
			}
			tilesRoot = new GameObject("Tiles").transform;
			tilesRoot.SetParent(menu.root, false);
			tabPages.Clear();
			tilesBySlot.Clear();
			ownedPieces = OwnedPieces();
			float height = menu.height;
			float left = 0.012f - menu.width * 0.5f + 0.16f + 0.02f;
			float tileWidth = (menu.width * 0.5f - 0.012f - left - 0.048f) / 5f;
			float tileHeight = (height - 0.048f) / 3f;
			foreach ((string slot, string label) in tabs)
			{
				List<Transform> pages = new List<Transform>();
				tabPages[slot] = pages;
				List<Tile> tiles = new List<Tile>();
				tilesBySlot[slot] = tiles;
				List<Cosmetics.Entry> owned = new List<Cosmetics.Entry>();
				List<Cosmetics.Entry> rest = new List<Cosmetics.Entry>();
				foreach (Cosmetics.Entry e in Cosmetics.All)
				{
					bool shown = slot == SetsTab ? IsSet(e) : e.slot == slot && InSlotTab(e, true);
					if (shown)
					{
						(Wallet.HasCosmetic(e.id) ? owned : rest).Add(e);
					}
				}
				for (int i = 1; i < rest.Count; i++)
				{
					for (int j = i; j > 0 && rest[j - 1].pearls > rest[j].pearls; j--)
					{
						(rest[j - 1], rest[j]) = (rest[j], rest[j - 1]);
					}
				}
				owned.AddRange(rest);
				int n = 0;
				foreach (Cosmetics.Entry e in owned)
				{
					if (n % 15 == 0)
					{
						Transform page = new GameObject(string.Format("Page {0} {1}", label, pages.Count + 1)).transform;
						page.SetParent(tilesRoot, false);
						pages.Add(page);
					}
					int cell = n % 15;
					int column = cell % 5;
					int row = cell / 5;
					Vector2 at = new Vector2(left + tileWidth * 0.5f + (tileWidth + 0.012f) * column, height * 0.5f - 0.012f - tileHeight * 0.5f - (tileHeight + 0.012f) * row);
					tiles.Add(MakeTile(pages[pages.Count - 1], e, at, new Vector2(tileWidth, tileHeight)));
					n++;
				}
				foreach (Transform page in pages)
				{
					page.gameObject.SetActive(false);
				}
				if (pageOf.TryGetValue(slot, out int shownPage) && shownPage >= pages.Count)
				{
					pageOf[slot] = 0;
				}
			}
		}

		private static bool IsSet(Cosmetics.Entry e)
		{
			// Rebuilt from native code (token 0x060009F3, RVA 0x274BF88)
			return e.model != null && !e.PaidOnly && Cosmetics.PiecesOf(e.id).Length != 0;
		}

		private static bool InSlotTab(Cosmetics.Entry e, bool ownedPiecesOnly)
		{
			// Rebuilt from native code (token 0x060009F4, RVA 0x274C01C)
			if (e.model == null || Cosmetics.PiecesOf(e.id).Length != 0)
			{
				return false;
			}
			if (e.PaidOnly)
			{
				return ownedPiecesOnly && Wallet.HasCosmetic(e.id);
			}
			return string.IsNullOrEmpty(e.set) || !ownedPiecesOnly || Wallet.HasCosmetic(e.id);
		}

		private static string OwnedPieces()
		{
			// Rebuilt from native code (token 0x060009F5, RVA 0x274D430)
			StringBuilder owned = new StringBuilder();
			foreach (Cosmetics.Entry e in Cosmetics.All)
			{
				if (Wallet.HasCosmetic(e.id))
				{
					owned.Append(e.id).Append(',');
				}
			}
			return owned.ToString();
		}

		private Tile MakeTile(Transform page, Cosmetics.Entry e, Vector2 at, Vector2 size)
		{
			// Rebuilt from native code (token 0x060009F6, RVA 0x274D568)
			Tile tile = new Tile
			{
				id = e.id
			};
			tile.button = Button(page, "Tile " + e.id, at, size, TileIdle);
			tile.button.onPress = () => PressTile(tile);
			Transform parent = tile.button.transform.parent;
			tile.icon = Icon(parent, e, new Vector2(0f, 0.013f), new Vector2(size.x - 0.02f, size.y - 0.016f - TagH));
			tile.width = size.x;
			GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(strip.GetComponent<Collider>());
			strip.name = "Tag";
			strip.transform.SetParent(parent, false);
			float tagY = -size.y * 0.5f + TagH * 0.5f + 0.004f;
			strip.transform.localPosition = new Vector3(0f, tagY, -0.0001f);
			strip.transform.localScale = new Vector3(size.x - 0.008f, TagH, 0.0002f);
			tile.strip = strip.GetComponent<Renderer>();
			tile.strip.sharedMaterial = TileMaterial();
			tile.strip.shadowCastingMode = ShadowCastingMode.Off;
			tile.tag = Label(parent, "Tag Text", new Vector2(0f, tagY), "", TagText, White, TextAnchor.MiddleCenter);
			tile.pearl = Pearl(parent, new Vector2(0f, tagY), TagText);
			return tile;
		}

		private void ShowTab(string slot)
		{
			// Rebuilt from native code (token 0x060009F7, RVA 0x274D428)
			ShowTab(slot, true);
		}

		private void ShowTab(string slot, bool flip)
		{
			// Rebuilt from native code (token 0x060009F8, RVA 0x274E8F4)
			List<Transform> pages = tabPages[slot];
			if (flip && slot == tab && pages.Count > 1)
			{
				pageOf[slot] = (pageOf[slot] + 1) % pages.Count;
			}
			tab = slot;
			for (int i = 0; i < tabs.Count; i++)
			{
				string tabSlot = tabs[i].slot;
				bool on = tabSlot == slot;
				tabButtons[i].SetIdleColor(on ? TabOn : TabIdle);
				tabLabels[i].color = on ? Ink : Green;
				List<Transform> tabPagesOfSlot = tabPages[tabSlot];
				for (int p = 0; p < tabPagesOfSlot.Count; p++)
				{
					tabPagesOfSlot[p].gameObject.SetActive(on && p == pageOf[tabSlot]);
				}
				tabLabels[i].text = tabPagesOfSlot.Count > 1 ? string.Format("{0}\n{1}/{2}", tabs[i].label, pageOf[tabSlot] + 1, tabPagesOfSlot.Count) : tabs[i].label;
				PixelFonts.Use(tabLabels[i], PixelFonts.Sign, tabPagesOfSlot.Count > 1 ? 0.015f : 0.02f, 1.3f);
			}
			dirty = true;
		}

		private void PressTile(Tile tile)
		{
			// Rebuilt from native code (token 0x060009F9, RVA 0x274EDFC)
			bool wasWearing = TryOn.Wearing(tile.id);
			TryOn.Toggle(tile.id);
			if (!wasWearing)
			{
				Splash();
			}
			dirty = true;
		}

		private void Splash()
		{
			// Rebuilt from native code (token 0x060009FA, RVA 0x274EE88)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.SplashSmall(Random.Range(0, 4)), stageTop + Vector3.up * 0.3f, 0.35f, Random.Range(1.15f, 1.35f));
			}
		}

		private void BuildCart()
		{
			// Rebuilt from native code (token 0x060009FB, RVA 0x274ABD0)
			float width = cart.width;
			float height = cart.height;
			GameObject divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(divider.GetComponent<Collider>());
			divider.name = "Divider";
			divider.transform.SetParent(cart.root, false);
			divider.transform.localPosition = new Vector3(-width * 0.5f - 0.022f, 0f, -0.0008f);
			divider.transform.localScale = new Vector3(0.004f, height - 0.03f, 0.0015f);
			Renderer dividerRenderer = divider.GetComponent<Renderer>();
			dividerRenderer.sharedMaterial = TileMaterial();
			dividerRenderer.shadowCastingMode = ShadowCastingMode.Off;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			block.SetColor("_BaseColor", Dim);
			dividerRenderer.SetPropertyBlock(block);
			float left = -width * 0.5f;
			float right = width * 0.5f;
			float top = height * 0.5f;
			float nameX = left + 0.018f;
			Label(cart.root, "Title", new Vector2(nameX, top - 0.026f), "CART", 0.026f, Green, TextAnchor.MiddleLeft);
			balance = Label(cart.root, "Balance", new Vector2(right - 0.034f, top - 0.026f), "", 0.0125f, PearlCyan, TextAnchor.MiddleRight);
			Pearl(cart.root, new Vector2(right - 0.022f, top - 0.026f), 0.0115f);
			float pearlX = right - 0.19f;
			float priceX = right - 0.18f;
			for (int i = 0; i < Cosmetics.Slots.Length; i++)
			{
				CartLine line = new CartLine();
				line.root = new GameObject("Line " + i).transform;
				line.root.SetParent(cart.root, false);
				float y = top - 0.068f - i * 0.042f;
				line.name = Label(line.root, "Name", new Vector2(nameX, y), "", 0.0125f, Green, TextAnchor.MiddleLeft);
				line.pearl = Pearl(line.root, new Vector2(pearlX, y), 0.0115f);
				line.price = Label(line.root, "Price", new Vector2(priceX, y), "", 0.0125f, PearlCyan, TextAnchor.MiddleLeft);
				CartLine line_ = line;
				line.buy = Button(line.root, "Buy", new Vector2(right - 0.082f, y), new Vector2(0.052f, 0.032f), Gold);
				line.buy.onPress = () => BuyPaid(line_);
				line.buyLabel = Label(line.buy.transform.parent, "Label", Vector2.zero, "", 0.0085f, Ink, TextAnchor.MiddleCenter);
				line.remove = Button(line.root, "Take Off", new Vector2(right - 0.03f, y), new Vector2(0.034f, 0.032f), Grey);
				line.remove.onPress = () =>
				{
					if (line_.id != null)
					{
						TryOn.TakeOffItem(line_.id);
					}
					dirty = true;
				};
				Label(line.remove.transform.parent, "Label", Vector2.zero, "X", 0.016f, White, TextAnchor.MiddleCenter);
				lines.Add(line);
			}
			float bottom = -height * 0.5f;
			cartHint = Label(cart.root, "Hint", new Vector2(0f, 0.01f), "PUT THINGS ON\nTO SEE THEM HERE", 0.0125f, Dim, TextAnchor.MiddleCenter);
			cartHint.lineSpacing = 1.5f;
			float totalY = bottom + 0.085f;
			total = Label(cart.root, "Total", new Vector2(nameX, totalY), "TOTAL", 0.014f, Green, TextAnchor.MiddleLeft);
			totalPearl = Pearl(cart.root, new Vector2(pearlX, totalY), 0.0125f);
			totalAmount = Label(cart.root, "Amount", new Vector2(priceX, totalY), "", 0.014f, PearlCyan, TextAnchor.MiddleLeft);
			cartNote = Label(cart.root, "Note", new Vector2(0f, totalY), "", 0.0115f, White, TextAnchor.MiddleCenter);
			cartNote.lineSpacing = 1.4f;
			float buttonY = bottom + 0.035f;
			buyAll = Button(cart.root, "Buy All", new Vector2(nameX + 0.15f, buttonY), new Vector2(0.3f, 0.05f), BuyGreen);
			buyAll.onPress = BuyAll;
			buyAllLabel = Label(buyAll.transform.parent, "Label", Vector2.zero, "BUY", 0.016f, White, TextAnchor.MiddleCenter);
			buyAllPearl = Pearl(buyAll.transform.parent, Vector2.zero, 0.015f);
			clearAll = Button(cart.root, "Take All Off", new Vector2(right - 0.018f - 0.095f, buttonY), new Vector2(0.19f, 0.05f), Grey);
			clearAll.onPress = () =>
			{
				TryOn.ClearCart();
				dirty = true;
			};
			clearLabel = Label(clearAll.transform.parent, "Label", Vector2.zero, "TAKE ALL\nOFF", 0.011f, White, TextAnchor.MiddleCenter);
			clearLabel.lineSpacing = 1.3f;
		}

		private void BuyAll()
		{
			// Rebuilt from native code (token 0x060009FC, RVA 0x274F0C0)
			if (buying)
			{
				return;
			}
			List<string> pearlItems = new List<string>();
			int sum = 0;
			foreach (string id in TryOn.Cart())
			{
				if (Cosmetics.TryGet(id, out Cosmetics.Entry e) && !e.PaidOnly)
				{
					pearlItems.Add(id);
					sum += e.pearls;
				}
			}
			if (pearlItems.Count == 0)
			{
				Denied();
				return;
			}
			if (sum > Wallet.Pearls)
			{
				confirmUntil = 0f;
				buying = true;
				dirty = true;
				string pack = MetaIAP.PearlPackFor(sum - Wallet.Pearls);
				MetaIAP.Buy(pack, (ok, message) =>
				{
					if (!this)
					{
						return;
					}
					if (!ok)
					{
						buying = false;
						if (!string.IsNullOrEmpty(message))
						{
							Denied();
						}
						dirty = true;
						return;
					}
					StartCoroutine(BuyAfter(PearlVendingMachine.Shower(buyAll.transform.position, MetaIAP.PearlsIn(pack)), pearlItems));
				});
				return;
			}
			if (Time.time >= confirmUntil || sum != confirmSum)
			{
				confirmSum = sum;
				confirmUntil = Time.time + ConfirmTime;
				Say("TAP AGAIN TO BUY", ConfirmTime);
				return;
			}
			confirmUntil = 0f;
			buying = true;
			Say("BUYING...");
			BuyNext(pearlItems, 0);
		}

		private IEnumerator BuyAfter(float seconds, List<string> ids)
		{
			// Rebuilt from native code (token 0x060009FD, RVA 0x274FA74; MoveNext RVA 0x27561F0)
			if (seconds > 0f)
			{
				yield return new WaitForSeconds(seconds);
			}
			BuyNext(ids, 0);
		}

		private void BuyNext(List<string> ids, int index)
		{
			// Rebuilt from native code (token 0x060009FE, RVA 0x274F7B0)
			if (index >= ids.Count)
			{
				buying = false;
				if (SplashSystem.I)
				{
					SplashSystem.I.PlaySound(Sfx.ShopBuy(), buyAll.transform.position, 0.8f, 1f);
				}
				Splash();
				dirty = true;
				return;
			}
			Wallet.BuyCosmetic(ids[index], (ok, message) =>
			{
				if (!this)
				{
					return;
				}
				if (!ok)
				{
					buying = false;
					Denied();
					dirty = true;
					return;
				}
				BuyNext(ids, index + 1);
			});
		}

		private void BuyPaid(CartLine line)
		{
			// Rebuilt from native code (token 0x060009FF, RVA 0x27501F4)
			if (buying || line.id == null)
			{
				return;
			}
			string sku = TryOn.SkuFor(line.id);
			if (sku == null)
			{
				Denied();
				return;
			}
			buying = true;
			Say("OPENING STORE...");
			MetaIAP.Buy(sku, (ok, message) =>
			{
				if (!this)
				{
					return;
				}
				buying = false;
				if (!ok)
				{
					if (!string.IsNullOrEmpty(message))
					{
						Denied();
					}
				}
				else
				{
					ShopStands.Bought((line.buy ? line.buy : buyAll).transform.position, sku);
					Splash();
				}
				dirty = true;
			});
		}

		private void Say(string message, float seconds = 4f)
		{
			// Rebuilt from native code (token 0x06000A00, RVA 0x274F784)
			note = null;
			noteUntil = 0f;
			dirty = true;
		}

		private void Denied()
		{
			// Rebuilt from native code (token 0x06000A01, RVA 0x274F5E8)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ShopDenied(), buyAll.transform.position, 0.6f, 0.8f);
			}
		}

		private void BuildFaceMe()
		{
			// Rebuilt from native code (token 0x06000A02, RVA 0x274BAD0)
			if (tabs.Count == 0)
			{
				return;
			}
			float height = (menu.height - 0.012f * (tabs.Count + 2)) / (tabs.Count + 1);
			PokeButton faceMe = Button(menu.root, "Face Me", new Vector2(0.012f - menu.width * 0.5f + 0.08f, 0.012f - menu.height * 0.5f + height * 0.5f), new Vector2(0.16f, height), BuyRed);
			faceMe.onPress = () => facingHome = true;
			TextMesh label = Label(faceMe.transform.parent, "Label", Vector2.zero, "FACE\nME", 0.014f, White, TextAnchor.MiddleCenter);
			label.alignment = TextAlignment.Center;
			label.lineSpacing = 1.3f;
		}

		private PokeButton Button(Transform parent, string name, Vector2 at, Vector2 size, Color color)
		{
			// Rebuilt from native code (token 0x06000A03, RVA 0x274C110)
			Transform holder = new GameObject(name).transform;
			holder.SetParent(parent, false);
			holder.localPosition = new Vector3(at.x, at.y, -0.009500001f);
			GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(key.GetComponent<Collider>());
			key.name = "Key";
			key.transform.SetParent(holder, false);
			key.transform.localPosition = new Vector3(0f, 0f, 0.0050000004f);
			key.transform.localScale = new Vector3(size.x, size.y, 0.008f);
			Renderer keyRenderer = key.GetComponent<Renderer>();
			keyRenderer.sharedMaterial = TileMaterial();
			keyRenderer.shadowCastingMode = ShadowCastingMode.Off;
			PokeButton button = LifeguardProps.Button(key.transform, holder.forward * 0.005f);
			button.color = color;
			button.repeat = false;
			button.pressedColor = Pressed;
			return button;
		}

		private static TextMesh Label(Transform parent, string name, Vector2 at, string text, float height, Color color, TextAnchor anchor)
		{
			// Rebuilt from native code (token 0x06000A04, RVA 0x274C3F8)
			TextAlignment alignment = anchor switch
			{
				TextAnchor.UpperCenter or TextAnchor.MiddleCenter => TextAlignment.Center,
				TextAnchor.MiddleRight or TextAnchor.UpperRight => TextAlignment.Right,
				_ => TextAlignment.Left
			};
			TextMesh label = WorldText.Create(name, parent, new Vector3(at.x, at.y, -0.0006f), text, height, color, anchor, alignment);
			PixelFonts.Use(label, PixelFonts.Sign, height);
			label.transform.localRotation = Quaternion.identity;
			return label;
		}

		private static Transform Pearl(Transform parent, Vector2 at, float size)
		{
			// Rebuilt from native code (token 0x06000A05, RVA 0x274E864)
			return ShopStands.PixelPearl(parent, new Vector3(at.x, at.y, -0.0009f), size * 1.15f);
		}

		private static Material TileMaterial()
		{
			// Rebuilt from native code (token 0x06000A06, RVA 0x274E6C8)
			if (!tileMaterial)
			{
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
				tileMaterial = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = "TryOn Key"
				};
			}
			return tileMaterial;
		}

		private static Material PearlMaterial()
		{
			// Rebuilt from native code (token 0x06000A07, RVA 0x27503B0)
			if (!pearlMaterial)
			{
				pearlMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = "TryOn Pearl"
				};
				pearlMaterial.color = new Color(0.94f, 0.93f, 0.97f, 1f);
				if (pearlMaterial.HasProperty("_BaseColor"))
				{
					pearlMaterial.SetColor("_BaseColor", new Color(0.94f, 0.93f, 0.97f, 1f));
				}
				if (pearlMaterial.HasProperty("_Smoothness"))
				{
					pearlMaterial.SetFloat("_Smoothness", 0.85f);
				}
			}
			return pearlMaterial;
		}

		private Transform Icon(Transform parent, Cosmetics.Entry e, Vector2 at, Vector2 box)
		{
			// Rebuilt from native code (token 0x06000A08, RVA 0x274DA54)
			List<(Mesh mesh, Material[] materials, Matrix4x4 pose)> parts = Baked(e.model);
			if (parts == null || parts.Count == 0)
			{
				return null;
			}
			Transform icon = new GameObject("Icon").transform;
			icon.SetParent(parent, false);
			icon.localPosition = new Vector3(at.x, at.y, -0.004f);
			Transform model = new GameObject("Model").transform;
			model.SetParent(icon, false);
			Bounds bounds = default;
			bool any = false;
			foreach ((Mesh mesh, Material[] materials, Matrix4x4 pose) in parts)
			{
				GameObject part = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
				part.layer = parent.gameObject.layer;
				part.transform.SetParent(model, false);
				part.transform.localPosition = pose.GetPosition();
				part.transform.localRotation = pose.rotation;
				part.transform.localScale = pose.lossyScale;
				part.GetComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
				partRenderer.sharedMaterials = materials;
				partRenderer.shadowCastingMode = ShadowCastingMode.Off;
				foreach (Vector3 corner in Corners(mesh.bounds))
				{
					Vector3 point = pose.MultiplyPoint3x4(corner);
					if (any)
					{
						bounds.Encapsulate(point);
					}
					else
					{
						bounds = new Bounds(point, Vector3.zero);
						any = true;
					}
				}
			}
			if (!any)
			{
				return icon;
			}
			Vector3 size = bounds.size;
			if (size.y < 0.0001f)
			{
				return icon;
			}
			Vector3 forward = menu.root.forward;
			model.rotation = Quaternion.LookRotation(-forward, menu.root.up) * CosmeticFront();
			Vector3 turned = CosmeticFront() * size;
			float scale = Mathf.Min(box.y / Mathf.Abs(turned.y), box.x / Mathf.Max(Mathf.Abs(turned.x), 0.0001f));
			model.localScale = Vector3.one * scale;
			model.position = icon.position - model.rotation * (bounds.center * scale) - forward * (Mathf.Abs(turned.z) * scale * 0.5f);
			return icon;
		}

		private static Quaternion CosmeticFront()
		{
			// Rebuilt from native code (token 0x06000A09, RVA 0x2750D48)
			if (front.HasValue)
			{
				return front.Value;
			}
			front = Quaternion.identity;
			GameObject prefab = Resources.Load<GameObject>("Ape/ShallowsPlayer");
			if (prefab)
			{
				GameObject ape = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
				ape.SetActive(false);
				Transform head = null;
				Transform eyeL = null;
				Transform eyeR = null;
				foreach (Transform t in ape.GetComponentsInChildren<Transform>(true))
				{
					if (t.name == "Head")
					{
						head = t;
					}
					else if (t.name == "Eye.L")
					{
						eyeL = t;
					}
					else if (t.name == "Eye.R")
					{
						eyeR = t;
					}
				}
				if (head && eyeL && eyeR)
				{
					Vector3 look = Vector3.ProjectOnPlane((eyeL.position + eyeR.position) * 0.5f - head.position, Vector3.up);
					if (look.sqrMagnitude > 1E-08f)
					{
						front = Quaternion.FromToRotation(look.normalized, Vector3.forward);
					}
				}
				Destroy(ape);
			}
			return front.Value;
		}

		private static IEnumerable<Vector3> Corners(Bounds b)
		{
			// Rebuilt from native code (token 0x06000A0A, RVA 0x2750CC8; MoveNext RVA 0x2756300)
			for (int i = 0; i < 8; i++)
			{
				yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
			}
		}

		private static List<(Mesh, Material[], Matrix4x4)> Baked(string model)
		{
			// Rebuilt from native code (token 0x06000A0B, RVA 0x275063C)
			if (bakedIcons.TryGetValue(model, out List<(Mesh mesh, Material[] materials, Matrix4x4 pose)> parts))
			{
				return parts;
			}
			parts = new List<(Mesh mesh, Material[] materials, Matrix4x4 pose)>();
			bakedIcons[model] = parts;
			GameObject prefab = Resources.Load<GameObject>("Cosmetics/" + model);
			if (!prefab)
			{
				return parts;
			}
			GameObject spawned = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
			foreach (SkinnedMeshRenderer skin in spawned.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (!skin.sharedMesh)
				{
					continue;
				}
				Mesh mesh = new Mesh();
				mesh.name = model + " Icon";
				skin.BakeMesh(mesh, true);
				Material[] materials = new Material[Mathf.Max(skin.sharedMaterials.Length, 1)];
				for (int i = 0; i < materials.Length; i++)
				{
					materials[i] = RippleShop.OutfitMaterial(skin.name);
				}
				parts.Add((mesh, materials, Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one)));
			}
			spawned.SetActive(false);
			Destroy(spawned);
			return parts;
		}

		public static bool InZone(Vector3 world)
		{
			// Rebuilt from native code (token 0x06000A0C, RVA 0x2751444)
			foreach (TryOnCabana station in stations)
			{
				if (station && station.shop && station.zone.Contains(station.shop.transform.InverseTransformPoint(world)))
				{
					return true;
				}
			}
			return false;
		}

		private static TryOnCabana Nearest(Vector3 head)
		{
			// Rebuilt from native code (token 0x06000A0D, RVA 0x2751698)
			TryOnCabana nearest = null;
			float best = float.MaxValue;
			foreach (TryOnCabana station in stations)
			{
				if (!station || station.menu == null)
				{
					continue;
				}
				float distance = (station.menu.root.position - head).sqrMagnitude;
				if (distance < best)
				{
					best = distance;
					nearest = station;
				}
			}
			return nearest;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000A0E, RVA 0x27518B8)
			stations.Add(this);
			Wallet.Changed += MarkDirty;
			TryOn.Changed += MarkDirty;
			MetaIAP.PricesChanged += MarkDirty;
			PlayerProfile.Changed += FurChanged;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000A0F, RVA 0x2751BD8)
			stations.Remove(this);
			Wallet.Changed -= MarkDirty;
			TryOn.Changed -= MarkDirty;
			MetaIAP.PricesChanged -= MarkDirty;
			PlayerProfile.Changed -= FurChanged;
			TryOn.Active = false;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000A10, RVA 0x2751EB0)
			if (monke)
			{
				Destroy(monke.gameObject);
			}
		}

		private void MarkDirty()
		{
			// Rebuilt from native code (token 0x06000A11, RVA 0x2751F54)
			dirty = true;
		}

		private void FurChanged()
		{
			// Rebuilt from native code (token 0x06000A12, RVA 0x2751F60)
			if (monke)
			{
				monke.SetFur(PlayerProfile.Color);
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000A13, RVA 0x2752010)
			PlayerRig rig = PlayerRig.I;
			if (!rig || !rig.head)
			{
				return;
			}
			Vector3 head = rig.head.position;
			TryOn.Active = zone.Contains(shop.transform.InverseTransformPoint(head));
			bool near = (head - stageTop).sqrMagnitude < awakeRange * awakeRange && Nearest(head) == this;
			if (awake != near)
			{
				awake = near;
				uiRoot.gameObject.SetActive(near);
				if (monke)
				{
					monke.gameObject.SetActive(near);
				}
				dirty = true;
			}
			if (!awake)
			{
				return;
			}
			if (!monke)
			{
				MakeMonke(rig);
			}
			if (confirmUntil > 0f && Time.time >= confirmUntil)
			{
				confirmUntil = 0f;
				dirty = true;
			}
			if (noteUntil > 0f && Time.time >= noteUntil)
			{
				noteUntil = 0f;
				note = null;
				dirty = true;
			}
			if (dirty)
			{
				Refresh();
			}
			Wheel(rig);
			if (monke)
			{
				Pose(rig);
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000A14, RVA 0x2754AA8)
			if (!awake || !monke || !monke.gameObject.activeInHierarchy)
			{
				return;
			}
			float floatAt = stageTop.y + hover + bob * MathF.Sin(Time.time * 2.2f);
			eyeLift = Mathf.Clamp(eyeLift + (floatAt - monke.transform.position.y), monkeEye * 0.5f, monkeEye * 2f + hover + bob);
		}

		private void Wheel(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x06000A15, RVA 0x27536D8)
			float dt = Mathf.Max(Time.deltaTime, 0.0001f);
			float turned = 0f;
			int turning = 0;
			for (int i = 0; i < 2; i++)
			{
				bool left = i == 0;
				Transform hand = left ? rig.leftHand : rig.rightHand;
				bool squeezing = hand && XRHands.Grip(left) > 0.55f;
				Vector3 palm = hand ? hand.position : wheelCentre;
				grabbing[i] = grabbing[i] ? squeezing && Near(palm, 0.3f) : squeezing && OnRim(palm, 0.08f);
				Vector3 tip = wheelCentre;
				bool touching = !grabbing[i] && Fingertips.Get(left, out tip) && OnRim(tip, 0.035f);
				bool on = touching || grabbing[i];
				float angle = 0f;
				if (on)
				{
					angle = Angle(grabbing[i] ? palm : tip);
					if (!wasOn[i])
					{
						XRHands.Haptic(left, 0.2f, 0.02f);
					}
					else if (wasGrab[i] == grabbing[i])
					{
						turned += Mathf.DeltaAngle(handAngle[i], angle);
						turning++;
					}
				}
				handAngle[i] = angle;
				wasOn[i] = on;
				wasGrab[i] = grabbing[i];
			}
			if (turning > 0)
			{
				facingHome = false;
				float step = turned / turning;
				wheelAngle += step;
				spin += step * wheelGear;
				spinVelocity = Mathf.Lerp(spinVelocity, step * wheelGear / dt, 1f - MathF.Exp(-12f * dt));
				if (Mathf.Abs(wheelAngle - lastClick) > 18f)
				{
					lastClick = wheelAngle;
					if (SplashSystem.I)
					{
						SplashSystem.I.PlaySound(Sfx.ButtonPress(), wheelCentre, 0.25f, 1.7f);
					}
					for (int i = 0; i < 2; i++)
					{
						if (wasOn[i])
						{
							XRHands.Haptic(i == 0, 0.12f, 0.015f);
						}
					}
				}
			}
			else if (facingHome)
			{
				float home = Mathf.Round(spin / 360f) * 360f;
				spinVelocity = 0f;
				float before = spin;
				spin = Mathf.MoveTowards(spin, home, dt * Mathf.Max(90f, Mathf.Abs(home - spin) * 4f));
				wheelAngle += (spin - before) / wheelGear;
				if (Mathf.Approximately(spin, home))
				{
					facingHome = false;
				}
			}
			else if (Mathf.Abs(spinVelocity) > 0.5f)
			{
				spinVelocity *= MathF.Exp(-spinDrag * dt);
				float step = dt * spinVelocity;
				spin += step;
				wheelAngle += step / wheelGear;
			}
			else
			{
				spinVelocity = 0f;
			}
			if (turntable)
			{
				turntable.rotation = Quaternion.AngleAxis(spin, shop.transform.up) * turntableRest;
			}
			if (wheel)
			{
				wheel.rotation = Quaternion.AngleAxis(wheelAngle, wheelAxis) * wheelRest;
			}
		}

		private bool OnRim(Vector3 p, float reach)
		{
			// Rebuilt from native code (token 0x06000A16, RVA 0x2754BD0)
			Vector3 offset = p - wheelCentre;
			float along = Vector3.Dot(offset, wheelAxis);
			if (Mathf.Abs(along) >= reach)
			{
				return false;
			}
			float radius = (offset - wheelAxis * along).magnitude;
			return radius > 0.03f && radius < reach + 0.12f;
		}

		private bool Near(Vector3 p, float distance)
		{
			// Rebuilt from native code (token 0x06000A17, RVA 0x2754CDC)
			return (p - wheelCentre).sqrMagnitude < distance * distance;
		}

		private float Angle(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000A18, RVA 0x2754D14)
			return Vector3.SignedAngle(wheelRef, Vector3.ProjectOnPlane(p - wheelCentre, wheelAxis), wheelAxis);
		}

		private void MakeMonke(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x06000A19, RVA 0x2752388)
			proxyHead = new GameObject("Monke Head Target").transform;
			proxyLeft = new GameObject("Monke Left Hand Target").transform;
			proxyRight = new GameObject("Monke Right Hand Target").transform;
			foreach (Transform proxy in new Transform[3] { proxyHead, proxyLeft, proxyRight })
			{
				proxy.SetParent(transform, false);
			}
			proxyHead.SetPositionAndRotation(stageTop + Vector3.up * monkeEye, stageFacing);
			proxyLeft.position = proxyRight.position = stageTop + Vector3.up * monkeEye * 0.6f;
			Renderer hand = rig.leftHand ? rig.leftHand.GetComponent<Renderer>() : null;
			monke = ApeBody.Create(transform, proxyHead, proxyLeft, proxyRight, () => monkeEye, hand ? hand.sharedMaterial : null);
			if (!monke)
			{
				enabled = false;
				return;
			}
			monke.name = "Try-On Monke";
			eyeLift = monkeEye * (ShallowsTuning.I ? ShallowsTuning.I.bodySize : 1.15f) * 0.94f;
			monke.SetFur(PlayerProfile.Color);
			monke.SetVisible(true);
			monkeLook = CosmeticLook.On(monke);
			dirty = true;
		}

		private void Pose(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x06000A1A, RVA 0x2754128)
			Transform head = rig.head;
			Vector3 up = Vector3.up;
			Transform body = ApeBody.Local ? ApeBody.Local.transform : head;
			Vector3 flat = Vector3.ProjectOnPlane(body.forward, up);
			if (flat.sqrMagnitude < 0.0001f)
			{
				flat = Vector3.ProjectOnPlane(head.up, up);
			}
			Quaternion you = Quaternion.LookRotation(flat.normalized, up);
			Quaternion it = Quaternion.AngleAxis(spin, up) * stageFacing;
			Vector3 eyes = stageTop + up * eyeLift;
			float k = monkeEye / Mathf.Max(0.2f, rig.eyeHeight);
			Quaternion headTurn = Mirror(Quaternion.Inverse(you) * head.rotation);
			float rise = eyes.y - head.position.y;
			float across = Mathf.Max(0.5f, Vector3.ProjectOnPlane(eyes - head.position, up).magnitude);
			float pitch = Mathf.Clamp(MathF.Atan2(rise, across) * Mathf.Rad2Deg, -5f, 25f);
			proxyHead.SetPositionAndRotation(eyes, it * Quaternion.Slerp(Quaternion.Euler(pitch, 0f, 0f), headTurn, headFollow));
			Place(proxyLeft, rig.rightHand, head.position, you, it, eyes, k);
			Place(proxyRight, rig.leftHand, head.position, you, it, eyes, k);
			AboveStage(proxyLeft);
			AboveStage(proxyRight);
			monke.leftFingers = ApeBody.ReadFingers(false);
			monke.rightFingers = ApeBody.ReadFingers(true);
		}

		private void AboveStage(Transform t)
		{
			// Rebuilt from native code (token 0x06000A1B, RVA 0x2755110)
			if (t.position.y < stageTop.y + 0.05f)
			{
				t.position = new Vector3(t.position.x, stageTop.y + 0.05f, t.position.z);
			}
		}

		private static void Place(Transform target, Transform hand, Vector3 headPos, Quaternion you, Quaternion it, Vector3 eyes, float k)
		{
			// Rebuilt from native code (token 0x06000A1C, RVA 0x2754E24)
			if (!hand)
			{
				return;
			}
			Vector3 local = Quaternion.Inverse(you) * (hand.position - headPos);
			local.x = -local.x;
			target.SetPositionAndRotation(eyes + it * (local * k), it * Mirror(Quaternion.Inverse(you) * hand.rotation));
		}

		private static Quaternion Mirror(Quaternion q)
		{
			// Rebuilt from native code (token 0x06000A1D, RVA 0x2754E18)
			return new Quaternion(q.x, -q.y, -q.z, q.w);
		}

		private void Refresh()
		{
			// Rebuilt from native code (token 0x06000A1E, RVA 0x2752A14)
			dirty = false;
			if (tabs.Count > 0 && OwnedPieces() != ownedPieces)
			{
				BuildTiles();
				ShowTab(tab ?? tabs[0].slot, false);
			}
			if (monkeLook)
			{
				foreach (string slot in Cosmetics.Slots)
				{
					monkeLook.Wear(slot, TryOn.Look(slot));
				}
			}
			if (tab != null && tilesBySlot.TryGetValue(tab, out List<Tile> tiles))
			{
				foreach (Tile tile in tiles)
				{
					ShowTile(tile);
				}
			}
			List<string> inCart = TryOn.Cart();
			int sum = 0;
			int pearlLines = 0;
			float right = cart.width * 0.5f;
			for (int i = 0; i < lines.Count; i++)
			{
				CartLine line = lines[i];
				line.root.gameObject.SetActive(i < inCart.Count);
				if (i >= inCart.Count)
				{
					line.id = null;
					continue;
				}
				line.id = inCart[i];
				if (!Cosmetics.TryGet(inCart[i], out Cosmetics.Entry e))
				{
					continue;
				}
				line.name.text = e.name;
				bool paid = e.PaidOnly;
				string sku = paid ? TryOn.SkuFor(e.id) : null;
				line.pearl.gameObject.SetActive(!paid);
				line.price.text = paid ? (sku != null ? "" : "SOON") : e.pearls.ToString("N0");
				if (paid && sku != null)
				{
					line.buyLabel.text = MetaIAP.Price(sku);
				}
				line.price.color = paid ? Gold : PearlCyan;
				Vector3 pricePosition = line.price.transform.localPosition;
				pricePosition.x = right - (paid ? 0.2f : 0.18f);
				line.price.transform.localPosition = pricePosition;
				line.buy.transform.parent.gameObject.SetActive(paid && sku != null);
				if (!paid)
				{
					sum += e.pearls;
					pearlLines++;
				}
			}
			balance.text = Wallet.Pearls.ToString("N0");
			int count = inCart.Count;
			cartHint.gameObject.SetActive(count == 0 && note == null);
			cartNote.text = note ?? "";
			cartNote.transform.localPosition = new Vector3(0f, count == 0 && note != null ? 0.01f : -cart.height * 0.5f + 0.085f, -0.0006f);
			total.gameObject.SetActive(false);
			totalPearl.gameObject.SetActive(false);
			totalAmount.gameObject.SetActive(false);
			totalAmount.text = sum.ToString("N0");
			totalAmount.color = sum > Wallet.Pearls ? Gold : PearlCyan;
			bool ready = pearlLines > 0 && !buying;
			bool sure = Time.time < confirmUntil && ready;
			buyAll.SetIdleColor(sure ? Gold : ready ? BuyGreen : Grey);
			bool showPrice = pearlLines > 0 && !sure;
			string label = sure ? "SURE?" : showPrice ? "BUY  " + sum.ToString("N0") : "BUY";
			buyAllLabel.text = label;
			buyAllLabel.color = sure ? Ink : White;
			buyAllPearl.gameObject.SetActive(showPrice);
			if (showPrice)
			{
				Vector3 pearlPosition = buyAllPearl.localPosition;
				pearlPosition.x = -label.Length * 0.016f * 0.5f + 0.072f;
				buyAllPearl.localPosition = pearlPosition;
			}
			clearAll.SetIdleColor(count != 0 ? new Color(0.36f, 0.4f, 0.38f, 1f) : Grey);
		}

		private void ShowTile(Tile tile)
		{
			// Rebuilt from native code (token 0x06000A1F, RVA 0x2755170)
			bool owned = Wallet.HasCosmetic(tile.id);
			bool wearing = TryOn.Wearing(tile.id);
			tile.button.SetIdleColor(wearing ? (owned ? TileWorn : TileTrying) : (owned ? TileOwned : TileIdle));
			string text;
			Color stripColor;
			Color textColor;
			bool pearl;
			if (owned && wearing)
			{
				stripColor = TagWearing;
				text = "WEARING";
				textColor = Ink;
				pearl = false;
			}
			else
			{
				stripColor = TagDark;
				if (owned)
				{
					text = "OWNED";
					textColor = Green;
					pearl = false;
				}
				else
				{
					Cosmetics.TryGet(tile.id, out Cosmetics.Entry e);
					text = e.pearls.ToString("N0");
					textColor = White;
					pearl = true;
				}
			}
			if (tile.tag.text != text)
			{
				tile.tag.text = text;
			}
			tile.tag.color = textColor;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			block.SetColor("_BaseColor", stripColor);
			tile.strip.SetPropertyBlock(block);
			int length = text.Length;
			tile.pearl.gameObject.SetActive(pearl);
			float textWidth = length * TagText;
			float withPearl = textWidth + 0.0186f;
			Vector3 tagPosition = tile.tag.transform.localPosition;
			tagPosition.x = pearl ? 0.0138f - withPearl * 0.5f + 0.0048f + textWidth * 0.5f : 0f;
			tile.tag.transform.localPosition = tagPosition;
			Vector3 pearlPosition = tile.pearl.localPosition;
			pearlPosition.x = (pearl ? withPearl : textWidth) * -0.5f + 0.0069f;
			tile.pearl.localPosition = pearlPosition;
		}
	}
}
