using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class LoungeCan : MonoBehaviour
	{
		public const int Kind = 9;

		public const float Radius = 0.033f;

		public const float HalfHeight = 0.06f;

		private static Mesh mesh;

		private static Material material;

		public static bool Is(int kind)
		{
			// Rebuilt from native code (token 0x0600046F, RVA 0x27B1E74)
			return kind == Kind;
		}

		public static bool Is(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000470, RVA 0x27B1E80)
			return kind == (ItemKind)Kind;
		}

		public static Throwable Make(Transform part, Transform parent)
		{
			// Rebuilt from native code (token 0x06000471, RVA 0x27B1E8C)
			GameObject go = part.gameObject;
			if (!mesh)
			{
				MeshFilter meshFilter = go.GetComponent<MeshFilter>();
				MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
				if (meshFilter)
				{
					mesh = meshFilter.sharedMesh;
				}
				if (meshRenderer)
				{
					material = meshRenderer.sharedMaterial;
				}
			}
			part.SetParent(parent, true);
			go.layer = 2;
			CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
			capsule.direction = 1;
			capsule.radius = Radius;
			capsule.height = 0.12f;
			Rigidbody body = go.AddComponent<Rigidbody>();
			body.mass = 0.15f;
			body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			body.interpolation = RigidbodyInterpolation.Interpolate;
			SplashSource splash = go.AddComponent<SplashSource>();
			splash.kind = SplashKind.Object;
			splash.loudness = 1f;
			go.AddComponent<LoungeCan>();
			Throwable throwable = go.AddComponent<Throwable>();
			throwable.netKind = Kind;
			throwable.returnsHome = true;
			throwable.respawnAfterWater = 6f;
			return throwable;
		}

		public static GameObject Visual()
		{
			// Rebuilt from native code (token 0x06000472, RVA 0x27B21A4)
			GameObject go = new GameObject("can");
			go.layer = 2;
			if (!mesh)
			{
				Find();
			}
			if (mesh)
			{
				GameObject child = new GameObject("Can");
				child.layer = 2;
				child.transform.SetParent(go.transform, false);
				child.AddComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = material;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
			return go;
		}

		public static Collider GhostCollider(GameObject go)
		{
			// Rebuilt from native code (token 0x06000473, RVA 0x27B2728)
			CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
			capsule.direction = 1;
			capsule.radius = Radius;
			capsule.height = 0.12f;
			return capsule;
		}

		private static void Find()
		{
			// Rebuilt from native code (token 0x06000474, RVA 0x27B23AC)
			GameObject lounge = Resources.Load<GameObject>("Lounge/StaffLounge");
			if (!lounge)
			{
				return;
			}
			foreach (Transform t in lounge.GetComponentsInChildren<Transform>(true))
			{
				if (t.name.StartsWith("Item Can"))
				{
					MeshFilter meshFilter = t.GetComponent<MeshFilter>();
					if (meshFilter && meshFilter.sharedMesh)
					{
						mesh = meshFilter.sharedMesh;
						break;
					}
				}
			}
			if (!mesh)
			{
				return;
			}
			Texture2D albedo = Resources.Load<Texture2D>("Lounge/StaffLounge_Albedo");
			Shader shader = Resources.Load<Shader>("Poolrooms/ShallowsProp");
			if (!albedo || !shader)
			{
				return;
			}
			Material canMaterial = new Material(shader);
			canMaterial.name = "Lounge Can";
			canMaterial.mainTexture = albedo;
			material = canMaterial;
		}
	}
}
