using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public static class ItemModels
	{
		private const string Folder = "Items/";

		private const string Atlas = "Items_Albedo";

		private const int IgnoreRaycast = 2;

		public const string FuseModel = "Fuse";

		public const string BlownFuseModel = "BlownFuse";

		public const string HelmetModel = "HeadlampHelmet";

		public const string GlowStickModel = "GlowStick";

		public const string SpongeModel = "Sponge";

		public const string SpongeBigModel = "SpongeBig";

		private const string Atlas2 = "Items2_Albedo";

		private static readonly string[] Pebbles = new string[3] { "Pebble_A", "Pebble_B", "Pebble_C" };

		public const float PebbleRadius = 0.034f;

		public const float FuseRadius = 0.025f;

		public const float FuseLength = 0.21f;

		private static Material shared;

		private static Material glow;

		private static Material shared2;

		private static Material glowStick;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060008DC, RVA 0x2837B10)
			glowStick = null;
			shared2 = null;
			glow = null;
			shared = null;
		}

		public static string ModelFor(ItemKind kind)
		{
			// Rebuilt from native code (token 0x060008DD, RVA 0x2837BA8)
			switch (kind)
			{
			case ItemKind.GlowStick:
				return GlowStickModel;
			case ItemKind.Duck:
				return "RubberDuck";
			case ItemKind.Sponge:
				return SpongeModel;
			default:
				return Pebbles[Random.Range(0, Pebbles.Length)];
			}
		}

		public static string PebbleFor(Vector3 position)
		{
			// Rebuilt from native code (token 0x060008DE, RVA 0x2837C78)
			int x = Mathf.RoundToInt(position.x * 7.3f);
			int z = Mathf.RoundToInt(position.z * 5.9f);
			return Pebbles[Mathf.Abs(x * 31 + z * 17) % Pebbles.Length];
		}

		public static bool Has(string model)
		{
			// Rebuilt from native code (token 0x060008DF, RVA 0x2837EE0)
			return Resources.Load<GameObject>(Folder + model);
		}

		public static Transform Wear(Transform holder, string model)
		{
			// Rebuilt from native code (token 0x060008E0, RVA 0x2837F88)
			string name = model + " Model";
			Transform instance = holder.Find(name);
			if (!instance)
			{
				GameObject prefab = Resources.Load<GameObject>(Folder + model);
				if (!prefab)
				{
					return null;
				}
				Renderer renderer = holder.GetComponent<Renderer>();
				if (renderer)
				{
					renderer.enabled = false;
				}
				foreach (Transform child in holder)
				{
					if (child.name == "Cap" || child.name == "Shell" || child.name == "Lamp")
					{
						child.gameObject.SetActive(false);
					}
				}
				instance = Object.Instantiate(prefab, holder, false).transform;
				instance.name = name;
				Align(holder, instance);
				Vector3 lossy = holder.lossyScale;
				if (lossy.x > 1E-05f && lossy.y > 1E-05f && lossy.z > 1E-05f)
				{
					instance.localScale = Vector3.Scale(instance.localScale, new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z));
				}
			}
			Finish(instance, model);
			return instance;
		}

		public static bool DressPebble(GameObject pebble)
		{
			// Rebuilt from native code (token 0x060008E1, RVA 0x2838C40)
			if (pebble.transform.Find("Pebble_A Model") || pebble.transform.Find("Pebble_B Model") || pebble.transform.Find("Pebble_C Model"))
			{
				return false;
			}
			string model = PebbleFor(pebble.transform.position);
			if (!Has(model))
			{
				return false;
			}
			pebble.transform.localScale = Vector3.one;
			SphereCollider collider = pebble.GetComponent<SphereCollider>();
			if (collider)
			{
				collider.center = Vector3.zero;
				collider.radius = PebbleRadius;
			}
			Wear(pebble.transform, model);
			return true;
		}

		public static bool DressFuse(Fuse fuse)
		{
			// Rebuilt from native code (token 0x060008E2, RVA 0x2838F1C)
			Transform holder = fuse.transform;
			bool dressed = holder.Find("Fuse Model");
			if (!dressed)
			{
				if (!Has(FuseModel))
				{
					return false;
				}
				holder.localScale = Vector3.one;
			}
			CapsuleCollider collider = fuse.GetComponent<CapsuleCollider>();
			if (collider && !dressed)
			{
				collider.center = Vector3.zero;
				collider.direction = 1;
				collider.radius = FuseRadius;
				collider.height = FuseLength;
			}
			Transform model = Wear(holder, FuseModel);
			Transform core = model ? LifeguardProps.Find(model, "Core") : null;
			Renderer renderer = core ? core.GetComponent<Renderer>() : null;
			bool changed = !dressed;
			if (renderer && fuse.core != renderer)
			{
				fuse.core = renderer;
				changed = true;
			}
			return changed;
		}

		private static void Align(Transform holder, Transform instance)
		{
			// Rebuilt from native code (token 0x060008E3, RVA 0x2838854)
			Transform origin = LifeguardProps.Find(instance, "Origin");
			Transform front = LifeguardProps.Find(instance, "Front");
			Transform up = LifeguardProps.Find(instance, "Up");
			Transform right = LifeguardProps.Find(instance, "Right");
			if (!origin || !front || !up)
			{
				return;
			}
			Vector3 o = holder.InverseTransformPoint(origin.position);
			Vector3 f = holder.InverseTransformPoint(front.position);
			Vector3 u = holder.InverseTransformPoint(up.position);
			Quaternion fix = Quaternion.Inverse(Quaternion.LookRotation(f - o, u - o));
			instance.localRotation = fix * instance.localRotation;
			instance.localPosition = fix * instance.localPosition;
			instance.localPosition -= holder.InverseTransformPoint(origin.position);
			if (right && holder.InverseTransformPoint(right.position).x < 0f)
			{
				Vector3 scale = instance.localScale;
				scale.x = -scale.x;
				instance.localScale = scale;
				Vector3 position = instance.localPosition;
				position.x = -position.x;
				instance.localPosition = position;
			}
		}

		private static void Finish(Transform instance, string model)
		{
			// Rebuilt from native code (token 0x060008E4, RVA 0x2838580)
			foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
			{
				bool second = model == GlowStickModel || model == SpongeModel || model == SpongeBigModel;
				Material material;
				if (model == FuseModel && renderer.name == "Core")
				{
					material = GlowMaterial();
				}
				else if (model == GlowStickModel && renderer.name == "Core")
				{
					material = GlowStickMaterial();
				}
				else
				{
					material = second ? Material2() : Material();
				}
				if (renderer.sharedMaterial != material)
				{
					renderer.sharedMaterial = material;
				}
				renderer.shadowCastingMode = ShadowCastingMode.Off;
				renderer.gameObject.layer = IgnoreRaycast;
			}
		}

		public static Material Material()
		{
			// Rebuilt from native code (token 0x060008E5, RVA 0x283927C)
			if (!shared)
			{
				shared = Make("Items Material", false);
			}
			return shared;
		}

		public static Material GlowMaterial()
		{
			// Rebuilt from native code (token 0x060008E6, RVA 0x2839644)
			if (!glow)
			{
				glow = Make("Fuse Glow Material", true);
			}
			return glow;
		}

		public static Material Material2()
		{
			// Rebuilt from native code (token 0x060008E7, RVA 0x28393A4)
			if (!shared2)
			{
				shared2 = Make("Items 2 Material", false, Atlas2);
			}
			return shared2;
		}

		public static Material GlowStickMaterial()
		{
			// Rebuilt from native code (token 0x060008E8, RVA 0x28394CC)
			if (!glowStick)
			{
				glowStick = Make("Glow Stick Material", true, Atlas2, new Color(1.21f, 2.2f, 0.77f, 2.2f));
			}
			return glowStick;
		}

		private static Material Make(string name, bool emissive, string atlas = "Items_Albedo", Color? glowColor = null)
		{
			// Rebuilt from native code (token 0x060008E9, RVA 0x283976C)
			Material loaded = Resources.Load<Material>(Folder + name);
			if (loaded)
			{
				return loaded;
			}
			Texture2D texture = Resources.Load<Texture2D>(Folder + atlas);
			Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			material.name = name;
			material.SetTexture("_BaseMap", texture);
			material.mainTexture = texture;
			material.SetColor("_BaseColor", Color.white);
			material.SetFloat("_Smoothness", emissive ? 0.6f : 0.2f);
			material.SetFloat("_Metallic", 0f);
			if (emissive)
			{
				material.SetTexture("_EmissionMap", texture);
				material.SetColor("_EmissionColor", glowColor ?? new Color(1.5f, 1.125f, 0.3f, 1.5f));
				material.EnableKeyword("_EMISSION");
				material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
			}
			return material;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void DressLoadedScenes()
		{
			// Rebuilt from native code (token 0x060008EA, RVA 0x2839A70)
			foreach (Throwable item in Object.FindObjectsByType<Throwable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				if (item.GetComponent<ShopItem>())
				{
					continue;
				}
				Fuse fuse = item.GetComponent<Fuse>();
				if (fuse)
				{
					DressFuse(fuse);
				}
				else if (item.name == "Pebble")
				{
					DressPebble(item.gameObject);
				}
			}
		}

		public static IEnumerable<GameObject> LoosePebbles(GameObject root)
		{
			// Rebuilt from native code (token 0x060008EB, RVA 0x2839C58; MoveNext token 0x060008EF, RVA 0x2839E28)
			foreach (Throwable item in root.GetComponentsInChildren<Throwable>(true))
			{
				if (item.name == "Pebble" && !item.GetComponent<ShopItem>() && !item.GetComponent<Fuse>())
				{
					yield return item.gameObject;
				}
			}
		}
	}
}
