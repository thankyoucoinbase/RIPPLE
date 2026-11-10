using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class WaterVolume : MonoBehaviour
	{
		public const int MaxRipples = 16;

		[Tooltip("Width (x) and length (z) of the water, in meters. Axis-aligned.")]
		public Vector2 size = new Vector2(48f, 28f);

		[Tooltip("Floor to surface, in meters. 0.28 is waist deep on the gorilla.")]
		public float depth = 0.28f;

		private static readonly List<WaterVolume> all = new List<WaterVolume>();

		private static readonly Vector4[] ripples = new Vector4[MaxRipples];

		private static readonly Vector4[] rippleData = new Vector4[MaxRipples];

		private static int nextRipple;

		private static readonly int RipplesId = Shader.PropertyToID("_ShallowsRipples");

		private static readonly int RippleDataId = Shader.PropertyToID("_ShallowsRippleData");

		private static readonly int TimeId = Shader.PropertyToID("_ShallowsTime");

		private static readonly int WaterYId = Shader.PropertyToID("_ShallowsWaterY");

		public float SurfaceY
		{
			get
			{
				// Rebuilt from native code (token 0x06000B3D, RVA 0x275DCEC)
				return transform.position.y + depth;
			}
		}

		public float FloorY
		{
			get
			{
				// Rebuilt from native code (token 0x06000B3E, RVA 0x276FCD8)
				return transform.position.y;
			}
		}

		public static WaterVolume First
		{
			get
			{
				// Rebuilt from native code (token 0x06000B46, RVA 0x276EAB4)
				if (all.Count < 1)
				{
					return null;
				}
				return all[0];
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000B3F, RVA 0x276FD00)
			all.Clear();
			nextRipple = 0;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000B40, RVA 0x276FD9C)
			if (all.Count == 0)
			{
				ClearRipples();
			}
			all.Add(this);
			Shader.SetGlobalFloat(WaterYId, SurfaceY);
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000B41, RVA 0x276FFE0)
			all.Remove(this);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000B42, RVA 0x2770060)
			if (all.Count < 1 || all[0] != this)
			{
				return;
			}
			Shader.SetGlobalFloat(TimeId, Time.time);
		}

		public bool ContainsXZ(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000B43, RVA 0x2770194)
			Vector3 position = transform.position;
			return Mathf.Abs(p.x - position.x) <= size.x * 0.5f && Mathf.Abs(p.z - position.z) <= size.y * 0.5f;
		}

		public Vector3 RandomPoint(float inset)
		{
			// Rebuilt from native code (token 0x06000B44, RVA 0x2770200)
			Vector3 position = transform.position;
			float halfX = Mathf.Max(0f, size.x * 0.5f - inset);
			float halfZ = Mathf.Max(0f, size.y * 0.5f - inset);
			return new Vector3(position.x + Random.Range(-halfX, halfX), FloorY, position.z + Random.Range(-halfZ, halfZ));
		}

		public Vector3 ClampXZ(Vector3 p, float inset)
		{
			// Rebuilt from native code (token 0x06000B45, RVA 0x27702B8)
			Vector3 position = transform.position;
			float halfX = Mathf.Max(0f, size.x * 0.5f - inset);
			float halfZ = Mathf.Max(0f, size.y * 0.5f - inset);
			return new Vector3(Mathf.Clamp(p.x, position.x - halfX, position.x + halfX), p.y, Mathf.Clamp(p.z, position.z - halfZ, position.z + halfZ));
		}

		public static WaterVolume At(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000B47, RVA 0x276B5D4)
			for (int i = 0; i < all.Count; i++)
			{
				if (all[i].ContainsXZ(p))
				{
					return all[i];
				}
			}
			return null;
		}

		public static bool IsUnderwater(Vector3 p, out WaterVolume water)
		{
			// Rebuilt from native code (token 0x06000B48, RVA 0x275DBE8)
			water = At(p);
			return water != null && p.y < water.SurfaceY && p.y > water.FloorY - 1f;
		}

		public static void AddRipple(Vector3 p, float strength)
		{
			// Rebuilt from native code (token 0x06000B49, RVA 0x276B70C)
			if (strength <= 0f)
			{
				return;
			}
			ripples[nextRipple] = new Vector4(p.x, p.y, p.z, Time.time);
			rippleData[nextRipple] = new Vector4(Mathf.Clamp01(strength), 0f, 0f, 0f);
			nextRipple = (nextRipple + 1) % MaxRipples;
			Push();
		}

		private static void ClearRipples()
		{
			// Rebuilt from native code (token 0x06000B4A, RVA 0x276FED0)
			for (int i = 0; i < MaxRipples; i++)
			{
				ripples[i] = new Vector4(0f, 0f, 0f, -1000f);
				rippleData[i] = Vector4.zero;
			}
			Push();
		}

		private static void Push()
		{
			// Rebuilt from native code (token 0x06000B4B, RVA 0x2770360)
			Shader.SetGlobalVectorArray(RipplesId, ripples);
			Shader.SetGlobalVectorArray(RippleDataId, rippleData);
		}

		private void OnDrawGizmos()
		{
			// Rebuilt from native code (token 0x06000B4C, RVA 0x27703D8)
			Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.6f);
			Gizmos.DrawWireCube(transform.position + Vector3.up * depth * 0.5f, new Vector3(size.x, depth, size.y));
		}
	}
}
