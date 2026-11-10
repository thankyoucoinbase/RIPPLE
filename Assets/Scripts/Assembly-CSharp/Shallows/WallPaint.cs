using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class WallPaint : MonoBehaviour
	{
		public const string Name = "Wall Paint";

		private const string Folder = "WallPaint/";

		private const string Texture = "WallPaint_Alpha";

		private const float PixelsPerMetre = 128f;

		private static readonly Color Paint = new Color(0.035f, 0.035f, 0.035f, 1f);

		private static Material material;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000534, RVA 0x27CF8C0)
			material = null;
		}

		public static bool EnsureOn(ShopCounter shop)
		{
			// Rebuilt from native code (token 0x06000535, RVA 0x27CF91C)
			if (!shop || shop.GetComponentInChildren<WallPaint>(true))
			{
				return false;
			}
			Texture2D alpha = Resources.Load<Texture2D>(Folder + Texture);
			if (!alpha)
			{
				return false;
			}
			GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Object.DestroyImmediate(quad.GetComponent<Collider>());
			quad.name = Name;
			quad.transform.SetParent(shop.transform, false);
			Vector3 origin = shop.transform.TransformPoint(new Vector3(0f, 1.85f, 0f));
			float depth = 0.44f;
			if (Physics.Raycast(origin, shop.transform.forward, out RaycastHit hit, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
			{
				depth = shop.transform.InverseTransformPoint(hit.point).z - 0.01f;
			}
			float width = alpha.width / PixelsPerMetre;
			float height = alpha.height / PixelsPerMetre;
			quad.transform.localPosition = new Vector3(0f, height * 0.5f + 1.85f, depth);
			quad.transform.localRotation = Quaternion.identity;
			quad.transform.localScale = new Vector3(width, height, 1f);
			quad.AddComponent<WallPaint>().Dress();
			return true;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000536, RVA 0x27CFE7C)
			Dress();
		}

		private void Dress()
		{
			// Rebuilt from native code (token 0x06000537, RVA 0x27CFD18)
			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
			if (!meshRenderer)
			{
				return;
			}
			Material paint = MaterialFor();
			if (paint && meshRenderer.sharedMaterial != paint)
			{
				meshRenderer.sharedMaterial = paint;
			}
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
		}

		private static Material MaterialFor()
		{
			// Rebuilt from native code (token 0x06000538, RVA 0x27CFE80)
			if (!material)
			{
				material = Resources.Load<Material>(Folder + Name + " Material");
				if (!material)
				{
					Shader shader = Shader.Find("Shallows/Text");
					if (!shader)
					{
						return null;
					}
					material = new Material(shader)
					{
						name = Name + " Material"
					};
					material.SetTexture("_MainTex", Resources.Load<Texture2D>(Folder + Texture));
					material.SetColor("_Color", Paint);
				}
			}
			return material;
		}
	}
}
