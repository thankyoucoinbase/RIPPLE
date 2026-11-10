using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class Items : MonoBehaviour
	{
		public Material pebble;

		public Material coconut;

		public Material duck;

		public Material beak;

		public Material balloon;

		private const int IgnoreRaycast = 2;

		// Rebuilt from native code (token 0x06000901, RVA 0x283AE24)
		public static readonly Vector3 SpongeSize = new Vector3(0.11f, 0.054f, 0.074f);

		public static Items I { get; private set; }

		private void Awake()
		{
			// Rebuilt from native code (token 0x060008F7, RVA 0x283A1C8)
			I = this;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060008F8, RVA 0x283A260)
			if (I == this)
			{
				I = null;
			}
		}

		public static ShopItem Spawn(ItemKind kind, Vector3 position)
		{
			// Rebuilt from native code (token 0x060008F9, RVA 0x282F7C4)
			GameObject go = Visual(kind);
			go.name = ShopCatalogue.Get(kind).name.ToLowerInvariant() + " (yours)";
			go.transform.position = position;
			AddCollider(go, kind);
			Rigidbody body = go.AddComponent<Rigidbody>();
			body.mass = kind switch
			{
				ItemKind.GlowStick => 0.08f,
				ItemKind.Sponge => 0.06f,
				_ => 0.15f
			};
			body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			if (kind == ItemKind.Pebble || kind == ItemKind.Duck)
			{
				SplashSource splash = go.AddComponent<SplashSource>();
				splash.kind = SplashKind.Object;
				splash.loudness = kind == ItemKind.Duck ? 1.2f : 1f;
			}
			go.AddComponent<Throwable>().netKind = (int)kind;
			switch (kind)
			{
			case ItemKind.GlowStick:
				go.AddComponent<GlowStick>();
				break;
			case ItemKind.Duck:
				go.AddComponent<RubberDuck>();
				break;
			case ItemKind.Sponge:
				go.AddComponent<Sponge>();
				break;
			}
			ShopItem item = go.AddComponent<ShopItem>();
			item.kind = kind;
			return item;
		}

		public static GameObject Visual(ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008FA, RVA 0x283A38C)
			if (LoungeCan.Is(kind))
			{
				return LoungeCan.Visual();
			}
			GameObject go = new GameObject(ShopCatalogue.Get(kind).name.ToLowerInvariant());
			go.layer = IgnoreRaycast;
			if (ItemModels.Wear(go.transform, ItemModels.ModelFor(kind)))
			{
				return go;
			}
			Items items = I;
			switch (kind)
			{
			case ItemKind.Pebble:
				Shape(PrimitiveType.Sphere, go, Vector3.zero, new Vector3(0.075f, 0.06f, 0.07f), items ? items.pebble : null, new Color(0.45f, 0.42f, 0.38f, 1f));
				break;
			case ItemKind.GlowStick:
				Shape(PrimitiveType.Cylinder, go, Vector3.zero, new Vector3(0.032f, 0.09f, 0.032f), null, new Color(0.45f, 1f, 0.2f, 1f));
				break;
			case ItemKind.Duck:
				Shape(PrimitiveType.Sphere, go, new Vector3(0f, -0.005f, -0.01f), new Vector3(0.085f, 0.065f, 0.105f), items ? items.duck : null, new Color(1f, 0.85f, 0.15f, 1f));
				Shape(PrimitiveType.Sphere, go, new Vector3(0f, 0.04f, 0.03f), Vector3.one * 0.055f, items ? items.duck : null, new Color(1f, 0.85f, 0.15f, 1f));
				Shape(PrimitiveType.Cube, go, new Vector3(0f, 0.035f, 0.062f), new Vector3(0.03f, 0.012f, 0.025f), items ? items.beak : null, new Color(1f, 0.45f, 0.1f, 1f));
				break;
			case ItemKind.Sponge:
				Shape(PrimitiveType.Cube, go, new Vector3(0f, -0.004f, 0f), new Vector3(0.11f, 0.044f, 0.074f), null, new Color(0.96f, 0.8f, 0.17f, 1f));
				Shape(PrimitiveType.Cube, go, new Vector3(0f, 0.022f, 0f), new Vector3(0.105f, 0.009f, 0.07f), null, new Color(0.15f, 0.45f, 0.17f, 1f));
				break;
			}
			return go;
		}

		public static float Radius(ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008FB, RVA 0x282D908)
			return kind switch
			{
				ItemKind.GlowStick => 0.02f,
				ItemKind.Duck => 0.05f,
				ItemKind.Sponge => 0.05f,
				_ => LoungeCan.Is(kind) ? 0.05f : 0.036f
			};
		}

		public static float Length(ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008FC, RVA 0x282D950)
			return kind == ItemKind.GlowStick ? 0.2f : 0f;
		}

		private static Collider AddCollider(GameObject go, ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008FD, RVA 0x283A9B4)
			switch (kind)
			{
			case ItemKind.Sponge:
			{
				BoxCollider box = go.AddComponent<BoxCollider>();
				box.size = SpongeSize;
				return box;
			}
			case ItemKind.GlowStick:
			{
				CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
				capsule.direction = 1;
				capsule.radius = 0.019f;
				capsule.height = 0.19f;
				return capsule;
			}
			default:
			{
				SphereCollider sphere = go.AddComponent<SphereCollider>();
				sphere.radius = Radius(kind);
				return sphere;
			}
			}
		}

		public static Collider GhostCollider(GameObject go, ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008FE, RVA 0x283ADB8)
			return AddCollider(go, kind);
		}

		private static void Shape(PrimitiveType type, GameObject root, Vector3 position, Vector3 scale, Material material, Color fallback)
		{
			// Rebuilt from native code (token 0x060008FF, RVA 0x283AB74)
			GameObject primitive = GameObject.CreatePrimitive(type);
			Destroy(primitive.GetComponent<Collider>());
			primitive.layer = IgnoreRaycast;
			primitive.transform.SetParent(root.transform, false);
			primitive.transform.localPosition = position;
			primitive.transform.localScale = scale;
			Renderer renderer = primitive.GetComponent<Renderer>();
			if (material)
			{
				renderer.sharedMaterial = material;
			}
			else
			{
				MaterialPropertyBlock block = new MaterialPropertyBlock();
				block.SetColor("_BaseColor", fallback);
				renderer.SetPropertyBlock(block);
			}
			renderer.shadowCastingMode = ShadowCastingMode.Off;
		}
	}
}
