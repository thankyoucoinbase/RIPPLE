using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class FoamCaster : MonoBehaviour
	{
		// Rebuilt from native code (token 0x06000B15, RVA 0x276B844)
		internal static readonly List<FoamCaster> All = new List<FoamCaster>();

		private BoxCollider box;

		private Collider col;

		private Vector3 lastCentre;

		private float lastYaw;

		private bool wasAbove;

		private bool started;

		private float churn;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000B0E, RVA 0x276AB3C)
			All.Clear();
		}

		public static void Ensure(GameObject go)
		{
			// Rebuilt from native code (token 0x06000B0F, RVA 0x276ABD4)
			if (!go.GetComponent<FoamCaster>())
			{
				go.AddComponent<FoamCaster>();
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000B10, RVA 0x276AC8C)
			box = GetComponent<BoxCollider>();
			col = box ? box : GetComponentInChildren<Collider>();
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000B11, RVA 0x276AD60)
			All.Add(this);
			started = false;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000B12, RVA 0x276AE38)
			All.Remove(this);
		}

		internal bool Foam(float dt, out Vector4 shape, out Vector4 look, out Vector4 amount)
		{
			// Rebuilt from native code (token 0x06000B13, RVA 0x276AEB8)
			shape = look = amount = Vector4.zero;
			if (!col)
			{
				return false;
			}
			Vector3 centre;
			Vector3 half;
			float yaw;
			if (box)
			{
				centre = transform.TransformPoint(box.center);
				Vector3 scale = transform.lossyScale;
				Vector3 size = box.size;
				half = new Vector3(Mathf.Abs(scale.x) * size.x, Mathf.Abs(scale.y) * size.y, Mathf.Abs(scale.z) * size.z) * 0.5f;
				yaw = transform.eulerAngles.y;
			}
			else
			{
				Bounds bounds = col.bounds;
				centre = bounds.center;
				half = bounds.extents;
				yaw = 0f;
			}
			if (!started)
			{
				lastCentre = centre;
				lastYaw = yaw;
			}
			Vector3 prev = lastCentre;
			float prevYaw = lastYaw;
			lastCentre = centre;
			lastYaw = yaw;
			float spin = Mathf.DeltaAngle(prevYaw, yaw);
			WaterVolume water = WaterVolume.At(centre);
			if (!water)
			{
				started = false;
				return false;
			}
			float vy = (centre.y - prev.y) / dt;
			float surface = water.SurfaceY;
			float top = centre.y + half.y;
			bool above = top > surface;
			if (started && wasAbove != above)
			{
				churn = 1f;
				float splash = Mathf.Clamp01(Mathf.Abs(vy) * 0.4f + 0.45f);
				Vector3 hit = new Vector3(centre.x, surface, centre.z);
				WaterVolume.AddRipple(hit, splash);
				Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
				WaterVolume.AddRipple(hit + rot * new Vector3(half.x * 0.6f, 0f, half.z * 0.6f), splash * 0.7f);
				WaterVolume.AddRipple(hit + rot * new Vector3(half.x * -0.6f, 0f, half.z * -0.6f), splash * 0.7f);
			}
			wasAbove = above;
			started = true;
			churn = Mathf.MoveTowards(churn, 0f, dt / 0.9f);
			if (centre.y - half.y > surface + 0.01f)
			{
				return false;
			}
			float depth = surface - top;
			if (!above && depth > 0.25f && churn <= 0f)
			{
				return false;
			}
			float turn = Mathf.Abs(spin) * Mathf.Deg2Rad / dt;
			float speed = new Vector2((centre.x - prev.x) / dt, (centre.z - prev.z) / dt).magnitude + Mathf.Max(half.x, half.z) * turn;
			Vector2 foam;
			if (above)
			{
				foam = new Vector2(Mathf.Min(speed * 0.3f, 0.2f) + 0.8f + churn * 0.3f + Mathf.Abs(vy) * 0.5f, 0f);
			}
			else
			{
				foam = new Vector2(churn * 0.9f, Mathf.Clamp01(1f - depth / 0.3f) * Mathf.Max(churn, Mathf.Min(Mathf.Abs(vy) * 0.8f, 1f)));
			}
			float rim = Mathf.Min(speed * 0.18f, 0.35f) + 0.28f + churn * 0.3f + Mathf.Min(Mathf.Abs(vy) * 0.3f, 0.3f);
			shape = new Vector4(centre.x, centre.z, half.x, half.z);
			float angle = yaw * Mathf.Deg2Rad;
			look = new Vector4(MathF.Cos(angle), MathF.Sin(angle), 0.03f, rim);
			amount = new Vector4(Mathf.Clamp01(foam.x), Mathf.Clamp01(foam.y), 0f, 0f);
			return true;
		}
	}
}
