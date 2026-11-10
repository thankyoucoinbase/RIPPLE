using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class RippleShop : MonoBehaviour
	{
		public const string Model = "RippleShop";

		private const string Folder = "Lifeguard/";

		private const float DoorHalfWidth = 0.95f;

		private const float DoorHeight = 2.55f;

		private const float DoorFromSpeaker = 2.35f;

		private const float WestOfDoor = 7.4f;

		private const float EastOfDoor = 8.6f;

		private const string Colliders = "Ripple Shop Colliders";

		private const string Lights = "Ripple Shop Lights";

		private const string OldTimer = "Weekly Timer";

		private static readonly Color LampColor = new Color(1f, 0.86f, 0.64f, 1f);

		[Tooltip("How far the floating figures bob up and down (metres either way).")]
		public float bobHeight = 0.035f;

		[Tooltip("Seconds for one bob up and down.")]
		public float bobPeriod = 3.2f;

		[Tooltip("How far the floating figures sway round (degrees either way).")]
		public float swayAngle = 2.5f;

		private static readonly List<RippleShop> shops = new List<RippleShop>();

		private static Material lamps;

		private const string OutfitPrefix = "Outfit ";

		private static readonly Dictionary<string, Material> outfits = new Dictionary<string, Material>();

		private Bounds inside;

		private bool hasInside;

		private readonly List<Transform> floats = new List<Transform>();

		private readonly List<int> floatIndex = new List<int>();

		private readonly List<Vector3> floatHome = new List<Vector3>();

		private readonly List<Quaternion> floatTurn = new List<Quaternion>();

		private readonly List<Transform> pearls = new List<Transform>();

		private readonly List<Vector3> pearlHome = new List<Vector3>();

		private readonly List<Quaternion> pearlTurn = new List<Quaternion>();

		private const string FloatingPearl = "Floating Pearl ";

		public const bool ClosedInBuilds = false;

		private static readonly Dictionary<string, (float alpha, float smoothness, float glow)> Tinted = new Dictionary<string, (float alpha, float smoothness, float glow)>
		{
			["JellyfishHat"] = (0.76f, 0.25f, 0.1f),
			["StarSunglasses Lens"] = (0.92f, 0.85f, 0f)
		};

		private const string CollisionFolder = "Lifeguard/RippleShopCollision/";

		public static bool ClosedForBeta
		{
			get
			{
				// Rebuilt from native code (token 0x0600094A, RVA 0x27369E8)
				return false;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000949, RVA 0x273691C)
			shops.Clear();
			lamps = null;
			outfits.Clear();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void EnsureOne()
		{
			// Rebuilt from native code (token 0x0600094B, RVA 0x27369F0)
			RippleShop shop = FindAnyObjectByType<RippleShop>(FindObjectsInactive.Include);
			if (!shop)
			{
				GameManager game = FindAnyObjectByType<GameManager>();
				if (game)
				{
					shop = Place(game);
				}
			}
			PearlVendingMachine.EnsureIn(shop);
		}

		public static bool Contains(Vector3 p)
		{
			// Rebuilt from native code (token 0x0600094C, RVA 0x27373E0)
			foreach (RippleShop shop in shops)
			{
				if (shop && shop.hasInside && shop.inside.Contains(shop.transform.InverseTransformPoint(p)))
				{
					return true;
				}
			}
			return false;
		}

		public static RippleShop Place(GameManager game)
		{
			// Rebuilt from native code (token 0x0600094D, RVA 0x2736E7C)
			GameObject room = GameObject.Find("Spawn Room");
			Transform wall = room ? room.transform.Find("Wall North") : null;
			Renderer wallRenderer = wall ? wall.GetComponent<Renderer>() : null;
			if (!wallRenderer)
			{
				return null;
			}
			Bounds bounds = wallRenderer.bounds;
			float face = bounds.min.z;
			Transform over = wall.parent.Find(wall.name + " (Over Door)");
			float x;
			if (over)
			{
				x = over.GetComponent<Renderer>().bounds.center.x;
			}
			else
			{
				LobbySpeaker speaker = FindAnyObjectByType<LobbySpeaker>(FindObjectsInactive.Include);
				float speakerX = speaker && Mathf.Abs(speaker.transform.position.z - face) < 1.5f ? speaker.transform.position.x : game.safeZone.center.x;
				float min = bounds.min.x + 0.5f + DoorHalfWidth;
				float max = bounds.max.x - 0.5f - EastOfDoor;
				if (min > max)
				{
					return null;
				}
				x = Mathf.Clamp(speakerX - DoorFromSpeaker, min, max);
			}
			float floor = game.spawnPoint ? game.spawnPoint.position.y : 0.38f;
			if (Physics.Raycast(new Vector3(x, game.safeZone.center.y + 1f, face - 0.3f), Vector3.down, out RaycastHit hit, 6f, -1, QueryTriggerInteraction.Ignore))
			{
				floor = hit.point.y;
			}
			if (!over)
			{
				CutDoor(wall, x, floor);
			}
			GameObject shopObject = new GameObject("Ripple Shop");
			shopObject.transform.SetPositionAndRotation(new Vector3(x, floor, face), Quaternion.identity);
			RippleShop shop = shopObject.AddComponent<RippleShop>();
			shop.Dress();
			return shop;
		}

		private static void CloseAll()
		{
			// Rebuilt from native code (token 0x0600094E, RVA 0x2736B18)
			foreach (RippleShop shop in FindObjectsByType<RippleShop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				shop.gameObject.SetActive(false);
			}
			GameObject room = GameObject.Find("Spawn Room");
			Transform wall = room ? room.transform.Find("Wall North") : null;
			if (!wall)
			{
				return;
			}
			foreach (string suffix in new string[4] { " (West of Door)", " (East of Door)", " (Over Door)", " (Under Door)" })
			{
				Transform piece = wall.parent.Find(wall.name + suffix);
				if (piece)
				{
					piece.gameObject.SetActive(false);
				}
			}
			wall.gameObject.SetActive(true);
		}

		private static void CutDoor(Transform wall, float x, float floor)
		{
			// Rebuilt from native code (token 0x0600094F, RVA 0x27375FC)
			Bounds b = wall.GetComponent<Renderer>().bounds;
			float left = x - DoorHalfWidth;
			float right = x + DoorHalfWidth;
			Piece(wall, " (West of Door)", b, b.min.x, left, b.min.y, b.max.y);
			Piece(wall, " (East of Door)", b, right, b.max.x, b.min.y, b.max.y);
			Piece(wall, " (Over Door)", b, left, right, floor + DoorHeight, b.max.y);
			Piece(wall, " (Under Door)", b, left, right, b.min.y, floor - 0.02f);
			wall.gameObject.SetActive(false);
		}

		private static void Piece(Transform wall, string suffix, Bounds b, float x0, float x1, float y0, float y1)
		{
			// Rebuilt from native code (token 0x06000950, RVA 0x2737C10)
			float width = x1 - x0;
			if (width < 0.01f)
			{
				return;
			}
			float height = y1 - y0;
			if (height < 0.01f)
			{
				return;
			}
			GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
			piece.name = wall.name + suffix;
			piece.layer = wall.gameObject.layer;
			piece.tag = wall.tag;
			piece.transform.SetParent(wall.parent, false);
			piece.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, b.center.z);
			Vector3 scale = wall.parent ? wall.parent.lossyScale : Vector3.one;
			piece.transform.localScale = new Vector3(width / scale.x, height / scale.y, b.size.z / scale.z);
			MeshRenderer source = wall.GetComponent<MeshRenderer>();
			MeshRenderer target = piece.GetComponent<MeshRenderer>();
			target.sharedMaterials = source.sharedMaterials;
			target.shadowCastingMode = source.shadowCastingMode;
			target.receiveShadows = source.receiveShadows;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000951, RVA 0x2737F14)
			shops.Add(this);
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000952, RVA 0x2737FE8)
			shops.Remove(this);
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000953, RVA 0x2738068)
			Dress();
			Transform model = transform.Find("RippleShop/RippleShop Model");
			if (!model)
			{
				return;
			}
			Transform safeMin = LifeguardProps.Find(model, "Safe Min");
			Transform safeMax = LifeguardProps.Find(model, "Safe Max");
			if (safeMin && safeMax)
			{
				inside = new Bounds(transform.InverseTransformPoint(safeMin.position), Vector3.zero);
				inside.Encapsulate(transform.InverseTransformPoint(safeMax.position));
				hasInside = true;
			}
			foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
			{
				if (t.name.StartsWith(FloatingPearl))
				{
					pearls.Add(t);
					pearlHome.Add(t.localPosition);
					pearlTurn.Add(t.localRotation);
					continue;
				}
				int index = FloatIndex(t.name);
				if (index >= 0)
				{
					floats.Add(t);
					floatIndex.Add(index);
					floatHome.Add(transform.InverseTransformPoint(t.position));
					floatTurn.Add(Quaternion.Inverse(transform.rotation) * t.rotation);
				}
			}
			ShopStands.Build(this, model);
			TryOnCabana.Build(this, model);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000954, RVA 0x2738BE8)
			BobPearls();
			if (floats.Count == 0)
			{
				return;
			}
			float t = Time.time * (Mathf.PI * 2f / Mathf.Max(0.1f, bobPeriod));
			for (int i = 0; i < floats.Count; i++)
			{
				Transform figure = floats[i];
				if (figure)
				{
					float phase = floatIndex[i] * 1.9f;
					float bob = MathF.Sin(t + phase);
					float sway = MathF.Sin(t * 0.5f + phase);
					figure.SetPositionAndRotation(transform.TransformPoint(floatHome[i]) + transform.up * (bobHeight * bob), transform.rotation * Quaternion.AngleAxis(swayAngle * sway, Vector3.up) * floatTurn[i]);
				}
			}
		}

		private void BobPearls()
		{
			// Rebuilt from native code (token 0x06000955, RVA 0x2738FF8)
			float time = Time.time;
			for (int i = 0; i < pearls.Count; i++)
			{
				Transform pearl = pearls[i];
				if (!pearl)
				{
					continue;
				}
				float phase = i * 2.17f;
				float t = time * (Mathf.PI * 2f / ((i & 3) * 0.37f + 2.6f));
				float bob = MathF.Sin(phase + t);
				float drift = phase + t * 0.5f;
				float driftX = MathF.Cos(drift);
				float driftZ = MathF.Sin(drift);
				float spin = (i & 1) == 0 ? 24f : -18f;
				Transform parent = pearl.parent;
				Vector3 home = parent ? parent.TransformPoint(pearlHome[i]) : pearlHome[i];
				Quaternion turn = parent ? parent.rotation * pearlTurn[i] : pearlTurn[i];
				Vector3 position = home + Vector3.up * (bob * 0.045f) + new Vector3(driftX, 0f, driftZ) * 0.012f;
				pearl.SetPositionAndRotation(position, turn * Quaternion.AngleAxis(i * 47f + time * spin, Vector3.up));
			}
		}

		private static int FloatIndex(string name)
		{
			// Rebuilt from native code (token 0x06000956, RVA 0x2738830)
			string digits = null;
			if (name.StartsWith("Float "))
			{
				digits = name.Substring(6);
			}
			else if (name.StartsWith(OutfitPrefix))
			{
				int at = name.IndexOf(".F");
				if (at >= 0)
				{
					int end = name.IndexOf(' ', at);
					digits = end < 0 ? name.Substring(at + 2) : name.Substring(at + 2, end - at - 2);
				}
			}
			if (digits == null)
			{
				return -1;
			}
			return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ? index : -1;
		}

		public bool Dress()
		{
			// Rebuilt from native code (token 0x06000957, RVA 0x273786C)
			Transform holder = transform.Find(Model);
			Transform model = holder ? holder.Find(Model + " Model") : null;
			bool had = model;
			if (!had)
			{
				GameObject prefab = Resources.Load<GameObject>(Folder + Model);
				if (!prefab)
				{
					return false;
				}
				if (holder)
				{
					DestroyImmediate(holder.gameObject);
				}
				Transform root = new GameObject(Model).transform;
				root.SetParent(transform, false);
				model = Instantiate(prefab, root, false).transform;
				model.name = Model + " Model";
				LifeguardProps.Align(root, model);
			}
			bool painted = Paint(model);
			bool solid = Solids(model);
			bool lit = Lamps(model);
			Transform timer = transform.Find(OldTimer);
			if (!timer)
			{
				return painted | !had | solid | lit;
			}
			DestroyImmediate(timer.gameObject);
			return true;
		}

		private static bool Paint(Transform model)
		{
			// Rebuilt from native code (token 0x06000958, RVA 0x2739494)
			bool changed = false;
			foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>(true))
			{
				bool lamp = r.name.StartsWith("Lamps");
				bool clear = false;
				Material material;
				if (r.name.StartsWith(OutfitPrefix))
				{
					string part = OutfitPart(r.name);
					clear = part.IndexOf(' ') >= 0;
					material = lamp ? LampMaterial() : OutfitMaterial(part);
				}
				else
				{
					material = lamp ? LampMaterial() : LifeguardProps.MaterialFor(Model, null);
				}
				if (r.sharedMaterial != material)
				{
					r.sharedMaterial = material;
					changed = true;
				}
				ShadowCastingMode shadows = clear || lamp ? ShadowCastingMode.Off : ShadowCastingMode.On;
				if (r.shadowCastingMode != shadows)
				{
					r.shadowCastingMode = shadows;
					changed = true;
				}
			}
			return changed;
		}

		private static string OutfitPart(string name)
		{
			// Rebuilt from native code (token 0x06000959, RVA 0x273A12C)
			string part = name.Substring(OutfitPrefix.Length);
			int dot = part.IndexOf('.');
			return dot < 0 ? part : part.Substring(0, dot);
		}

		private static Material LampMaterial()
		{
			// Rebuilt from native code (token 0x0600095A, RVA 0x273A46C)
			if (!lamps)
			{
				lamps = SavedMaterial("RippleShop Lamps", "Universal Render Pipeline/Unlit", m =>
				{
					Texture2D albedo = Resources.Load<Texture2D>(Folder + Model + "_Albedo");
					if (m.HasProperty("_BaseMap"))
					{
						m.SetTexture("_BaseMap", albedo);
					}
					if (m.HasProperty("_BaseColor"))
					{
						m.SetColor("_BaseColor", Color.white);
					}
					m.mainTexture = albedo;
				});
			}
			return lamps;
		}

		internal static Material OutfitMaterial(string part)
		{
			// Rebuilt from native code (token 0x0600095B, RVA 0x273A1C4)
			if (outfits.TryGetValue(part, out Material material) && material)
			{
				return material;
			}
			string[] words = part.Split(' ', StringSplitOptions.None);
			bool tinted = Tinted.TryGetValue(part, out (float alpha, float smoothness, float glow) tint);
			bool glass = words.Length > 1 && !tinted;
			material = SavedMaterial("RippleShop " + string.Join(" ", words), "Universal Render Pipeline/Lit", m =>
			{
				if (m.HasProperty("_Smoothness"))
				{
					m.SetFloat("_Smoothness", tinted ? tint.smoothness : (glass ? 0.85f : 0.1f));
				}
				if (m.HasProperty("_Metallic"))
				{
					m.SetFloat("_Metallic", 0f);
				}
				if (glass)
				{
					if (m.HasProperty("_BaseColor"))
					{
						m.SetColor("_BaseColor", new Color(0.88f, 0.95f, 1f, 0.32f));
					}
					SeeThrough(m);
					return;
				}
				Texture2D albedo = Resources.Load<Texture2D>(Folder + words[0] + "_Albedo");
				if (m.HasProperty("_BaseMap"))
				{
					m.SetTexture("_BaseMap", albedo);
				}
				if (m.HasProperty("_BaseColor"))
				{
					m.SetColor("_BaseColor", new Color(1f, 1f, 1f, tinted ? tint.alpha : 1f));
				}
				m.mainTexture = albedo;
				if (!tinted)
				{
					return;
				}
				if (tint.glow > 0f && m.HasProperty("_EmissionColor"))
				{
					m.SetTexture("_EmissionMap", albedo);
					m.SetColor("_EmissionColor", new Color(tint.glow, tint.glow, tint.glow, 1f));
					m.EnableKeyword("_EMISSION");
					m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
				}
				SeeThrough(m);
				m.SetShaderPassEnabled("DepthOnly", false);
			});
			outfits[part] = material;
			return material;
		}

		private static void SeeThrough(Material m)
		{
			// Rebuilt from native code (token 0x0600095C, RVA 0x273A77C)
			m.SetFloat("_Surface", 1f);
			m.SetFloat("_Blend", 0f);
			m.SetFloat("_SrcBlend", 5f);
			m.SetFloat("_DstBlend", 10f);
			m.SetFloat("_SrcBlendAlpha", 1f);
			m.SetFloat("_DstBlendAlpha", 10f);
			m.SetFloat("_ZWrite", 0f);
			m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			m.SetOverrideTag("RenderType", "Transparent");
			m.SetShaderPassEnabled("ShadowCaster", false);
			m.renderQueue = 3000;
		}

		private static Material SavedMaterial(string name, string shader, Action<Material> setup)
		{
			// Rebuilt from native code (token 0x0600095D, RVA 0x273A640)
			Material saved = Resources.Load<Material>(Folder + name);
			if (saved)
			{
				return saved;
			}
			Material material = new Material(Shader.Find(shader));
			material.name = name;
			setup(material);
			return material;
		}

		private bool Solids(Transform model)
		{
			// Rebuilt from native code (token 0x0600095E, RVA 0x2739750)
			bool changed = false;
			bool any = false;
			foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
			{
				GameObject part = filter.gameObject;
				Mesh mesh = Skipped(part.name) ? null : Resources.Load<Mesh>(CollisionFolder + part.name);
				MeshCollider meshCollider = part.GetComponent<MeshCollider>();
				if (mesh)
				{
					any = true;
					if (!meshCollider)
					{
						meshCollider = part.AddComponent<MeshCollider>();
						changed = true;
					}
					if (meshCollider.sharedMesh != mesh || meshCollider.convex)
					{
						meshCollider.convex = false;
						meshCollider.sharedMesh = mesh;
						changed = true;
					}
				}
				else if (meshCollider)
				{
					DestroyImmediate(meshCollider);
					changed = true;
				}
			}
			if (!any)
			{
				return changed | Boxes(model);
			}
			Transform boxes = transform.Find(Colliders);
			if (boxes)
			{
				DestroyImmediate(boxes.gameObject);
				changed = true;
			}
			return changed;
		}

		public static bool Skipped(string name)
		{
			// Rebuilt from native code (token 0x0600095F, RVA 0x273A97C)
			return name.StartsWith(OutfitPrefix) || name.StartsWith("Float ") || name.StartsWith(FloatingPearl) || name.StartsWith("Lamps");
		}

		private bool Boxes(Transform model)
		{
			// Rebuilt from native code (token 0x06000960, RVA 0x273AA58)
			List<Transform> marks = Marks(model, "Collider ");
			Transform old = transform.Find(Colliders);
			if (old && old.childCount == marks.Count)
			{
				bool same = true;
				for (int i = 0; i < marks.Count; i++)
				{
					Transform child = old.GetChild(i);
					if ((child.position - marks[i].position).sqrMagnitude >= 1E-06f || Quaternion.Angle(child.rotation, transform.rotation * Quaternion.Euler(0f, MarkTurn(marks[i]) + MovedTurn(transform, marks[i]), 0f)) >= 0.1f)
					{
						same = false;
						break;
					}
				}
				if (same)
				{
					return false;
				}
			}
			if (old)
			{
				DestroyImmediate(old.gameObject);
			}
			Transform root = new GameObject(Colliders).transform;
			root.SetParent(transform, false);
			foreach (Transform mark in marks)
			{
				string[] words = mark.name.Split(' ', StringSplitOptions.None);
				if (words.Length < 4)
				{
					continue;
				}
				string[] size = words[words.Length - 2].Split('x', StringSplitOptions.None);
				if (size.Length != 3 || !TryFloat(words[words.Length - 1].TrimStart('r'), out float turn) || !TryFloat(size[0], out float sizeX) || !TryFloat(size[1], out float sizeY) || !TryFloat(size[2], out float sizeZ))
				{
					continue;
				}
				GameObject box = new GameObject(string.Join(" ", words, 2, words.Length - 4));
				box.layer = gameObject.layer;
				box.transform.SetParent(root, false);
				box.transform.SetPositionAndRotation(mark.position, transform.rotation * Quaternion.Euler(0f, turn + MovedTurn(transform, mark), 0f));
				box.AddComponent<BoxCollider>().size = new Vector3(sizeX, sizeY, sizeZ);
			}
			return true;
		}

		private bool Lamps(Transform model)
		{
			// Rebuilt from native code (token 0x06000961, RVA 0x2739ADC)
			List<Transform> marks = Marks(model, "Light ");
			Transform old = transform.Find(Lights);
			if (old && old.childCount == marks.Count)
			{
				bool same = true;
				for (int i = 0; i < marks.Count; i++)
				{
					if (old.GetChild(i).name != "Shop " + marks[i].name)
					{
						same = false;
						break;
					}
				}
				if (same)
				{
					return false;
				}
			}
			if (old)
			{
				DestroyImmediate(old.gameObject);
			}
			Transform root = new GameObject(Lights).transform;
			root.SetParent(transform, false);
			foreach (Transform mark in marks)
			{
				string[] words = mark.name.Split(' ', StringSplitOptions.None);
				if ((words.Length != 4 && words.Length != 5) || !TryFloat(words[2], out float range) || !TryFloat(words[3], out float intensity))
				{
					continue;
				}
				Color color = LampColor;
				if (words.Length == 5 && !ColorUtility.TryParseHtmlString("#" + words[4], out color))
				{
					color = LampColor;
				}
				GameObject lamp = new GameObject("Shop " + mark.name);
				lamp.transform.SetParent(root, false);
				lamp.transform.position = mark.position;
				Light light = lamp.AddComponent<Light>();
				light.type = LightType.Point;
				light.range = range;
				light.intensity = intensity;
				light.color = color;
				light.shadows = LightShadows.None;
			}
			return true;
		}

		private static float MarkTurn(Transform mark)
		{
			// Rebuilt from native code (token 0x06000962, RVA 0x273B608)
			int space = mark.name.LastIndexOf(' ');
			if (space < 1)
			{
				return 0f;
			}
			return TryFloat(mark.name.Substring(space + 1).TrimStart('r'), out float turn) ? turn : 0f;
		}

		internal static float MovedTurn(Transform shop, Transform mark)
		{
			// Rebuilt from native code (token 0x06000963, RVA 0x273B6E4)
			for (Transform t = mark.parent; t && t != shop; t = t.parent)
			{
				if (!t.name.StartsWith("Move "))
				{
					continue;
				}
				Transform facing = t.Find(t.name + " Facing");
				if (!facing)
				{
					return 0f;
				}
				Vector3 flat = Vector3.ProjectOnPlane(facing.position - t.position, shop.up);
				if (flat.sqrMagnitude < 1E-08f)
				{
					return 0f;
				}
				return Vector3.SignedAngle(shop.forward, flat, shop.up);
			}
			return 0f;
		}

		private static List<Transform> Marks(Transform model, string prefix)
		{
			// Rebuilt from native code (token 0x06000964, RVA 0x273B424)
			List<Transform> marks = new List<Transform>();
			foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
			{
				if (t.name.StartsWith(prefix) && !t.GetComponent<Renderer>())
				{
					marks.Add(t);
				}
			}
			return marks;
		}

		private static bool TryFloat(string s, out float value)
		{
			// Rebuilt from native code (token 0x06000965, RVA 0x273B9B0)
			return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
	}
}
