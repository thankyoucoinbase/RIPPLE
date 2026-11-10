using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(-60)]
	[RequireComponent(typeof(Rigidbody))]
	public class MovingPlatform : MonoBehaviour
	{
		public enum Mode
		{
			Spin = 0,
			Shuttle = 1,
			Drift = 2
		}

		public Mode mode = Mode.Shuttle;

		[Header("Spin")]
		[Tooltip("Degrees per second around its center. Negative turns the other way.")]
		public float degreesPerSecond = 20f;

		public float startAngle;

		[Header("Shuttle")]
		[Tooltip("From where it starts to the far end of its run (m).")]
		public Vector3 travel = new Vector3(6f, 0f, 0f);

		[Tooltip("Seconds for a full trip there and back.")]
		public float period = 12f;

		[Tooltip("Seconds it waits at each end.")]
		public float pause = 1f;

		[Header("Drift")]
		[Tooltip("How far it wanders from home in x and z (m). It wanders at least Wander, room allowing.")]
		public Vector2 driftRadius = new Vector2(1.2f, 2f);

		[Tooltip("Longer is slower (s).")]
		public float driftPeriod = 20f;

		[Tooltip("How far it wanders from home at least (m), as long as it can't reach anything else: it stops short of pillars, decks and the other rafts.")]
		public float wander = 1.6f;

		[Tooltip("How fast it drifts about (1 = about 0.6 m/s on average, with sudden spurts).")]
		public float driftSpeed = 1f;

		[Tooltip("0 to 1: how jerky it drifts. Above 0 it goes in surges of a few seconds, often stopping dead for a moment and then lunging off to catch up. 0 glides smoothly.")]
		[Range(0f, 1f)]
		public float surge;

		[Tooltip("Gentle bobbing on the water (m).")]
		public float bob = 0.015f;

		[Tooltip("How far it dips while someone stands on it (m).")]
		public float dip = 0.05f;

		[Tooltip("0 to 1: where in its cycle it starts, so neighbours don't move in step.")]
		public float phase;

		private static readonly List<MovingPlatform> all = new List<MovingPlatform>();

		private static int movedFrame = -1;

		private Vector3 home;

		private Quaternion homeRotation;

		private Collider col;

		private Vector3 half;

		private Vector3 boxCentre;

		private Quaternion boxRotation;

		private float dipped;

		private float reach = -1f;

		private Vector3 driftFrequency;

		private float seed;

		private float surgeCycle;

		private uint surgeSeed;

		public float DriftReach
		{
			get
			{
				// Rebuilt from native code (token 0x06000498, RVA 0x27B7B00)
				if (reach < 0f)
				{
					WorkOutDrift();
				}
				return reach;
			}
		}

		private float MovingReach
		{
			get
			{
				// Rebuilt from native code (token 0x0600049C, RVA 0x27B840C)
				if (mode == Mode.Shuttle)
				{
					return travel.magnitude;
				}
				if (mode != Mode.Spin)
				{
					return 0f;
				}
				float footprint = new Vector2(half.x, half.z).magnitude + Vector2.Distance(new Vector2(boxCentre.x, boxCentre.z), new Vector2(home.x, home.z));
				return footprint - Mathf.Min(half.x, half.z);
			}
		}

		public Vector3 Home
		{
			get
			{
				// Rebuilt from native code (token 0x0600049D, RVA 0x27B8578)
				return home;
			}
		}

		public float ReachBound
		{
			get
			{
				// Rebuilt from native code (token 0x0600049E, RVA 0x27B8584)
				float travelled;
				if (mode == Mode.Shuttle)
				{
					travelled = travel.magnitude;
				}
				else if (mode != Mode.Drift)
				{
					travelled = 0f;
				}
				else
				{
					travelled = DriftReach;
				}
				float footprint = new Vector2(half.x, half.z).magnitude + Vector2.Distance(new Vector2(boxCentre.x, boxCentre.z), new Vector2(home.x, home.z));
				return footprint + travelled;
			}
		}

		public static IReadOnlyList<MovingPlatform> All
		{
			get
			{
				// Rebuilt from native code (token 0x060004A0, RVA 0x27B8700)
				return all;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000495, RVA 0x27B72EC)
			all.Clear();
			movedFrame = -1;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000496, RVA 0x27B738C)
			home = transform.position;
			homeRotation = transform.rotation;
			col = GetComponent<Collider>();
			if (!col)
			{
				col = GetComponentInChildren<Collider>();
			}
			Bounds bounds = col ? col.bounds : new Bounds(home, Vector3.one);
			if (col is BoxCollider box && box.transform == transform)
			{
				half = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
				boxCentre = transform.TransformPoint(box.center);
				boxRotation = homeRotation;
			}
			else
			{
				half = bounds.extents;
				boxCentre = bounds.center;
				boxRotation = Quaternion.identity;
			}
			Rigidbody body = GetComponent<Rigidbody>();
			body.isKinematic = true;
			body.interpolation = RigidbodyInterpolation.None;
			FoamCaster.Ensure(gameObject);
		}

		public float ReachDistance(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000497, RVA 0x27B7758)
			if (mode == Mode.Shuttle)
			{
				int steps = Mathf.Max(Mathf.CeilToInt(travel.magnitude * 4f), 1);
				float best = float.MaxValue;
				for (int i = 0; i <= steps; i++)
				{
					float k = (float)i / steps;
					best = Mathf.Min(best, BoxDistance(p - travel * k));
				}
				return best;
			}
			if (mode != Mode.Spin)
			{
				return BoxDistance(p) - DriftReach;
			}
			float footprint = new Vector2(half.x, half.z).magnitude + Vector2.Distance(new Vector2(boxCentre.x, boxCentre.z), new Vector2(home.x, home.z));
			return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(home.x, home.z)) - footprint;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000499, RVA 0x27B821C)
			if (mode == Mode.Drift)
			{
				WorkOutDrift();
			}
		}

		private void WorkOutDrift()
		{
			// Rebuilt from native code (token 0x0600049A, RVA 0x27B7B24)
			seed = phase + Mathf.Repeat(home.x * 0.173f + home.z * 0.311f, 1f);
			driftFrequency = new Vector3(1f / Mathf.Lerp(8f, 10.5f, Mathf.Repeat(seed * 7.31f, 1f)), 1f / Mathf.Lerp(8.5f, 11f, Mathf.Repeat(seed * 13.7f, 1f)), 1f / Mathf.Lerp(8f, 11.5f, Mathf.Repeat(seed * 29.3f, 1f)));
			surgeCycle = Mathf.Lerp(2.3f, 3.6f, Mathf.Repeat(seed * 5.17f, 1f));
			surgeSeed = (uint)Mathf.RoundToInt(Mathf.Repeat(seed, 1f) * 65521f);
			float limit = Mathf.Max(wander, Mathf.Max(driftRadius.x, driftRadius.y));
			foreach (MovingPlatform other in all)
			{
				if (!other || other == this)
				{
					continue;
				}
				float gap = other.HomeGap(this);
				if (other.mode == Mode.Drift)
				{
					limit = Mathf.Min(limit, (gap - 0.25f) * 0.5f);
				}
				else
				{
					limit = Mathf.Min(limit, gap - 0.25f - other.MovingReach);
				}
			}
			Physics.SyncTransforms();
			float height = Mathf.Max(half.y - 0.03f, 0.01f);
			while (limit > 0.05f)
			{
				Collider[] hits = Physics.OverlapBox(boxCentre, new Vector3(half.x + (limit + 0.25f), height, half.z + (limit + 0.25f)), boxRotation, -1, QueryTriggerInteraction.Ignore);
				bool blocked = false;
				foreach (Collider hit in hits)
				{
					if (hit.GetComponentInParent<MovingPlatform>() || hit.GetComponentInParent<MonsterAI>() || hit.GetComponentInParent<PlayerRig>() || hit.gameObject.layer == 2 || (hit.attachedRigidbody && !hit.attachedRigidbody.isKinematic))
					{
						continue;
					}
					blocked = true;
					break;
				}
				if (!blocked)
				{
					break;
				}
				limit -= 0.1f;
			}
			reach = Mathf.Max(limit, 0.05f);
		}

		private float HomeGap(MovingPlatform other)
		{
			// Rebuilt from native code (token 0x0600049B, RVA 0x27B8230)
			Vector3 offset = Quaternion.Inverse(boxRotation) * (other.boxCentre - boxCentre);
			Vector3 axis = Quaternion.Inverse(boxRotation) * (other.boxRotation * Vector3.right);
			float gapX = Mathf.Abs(offset.x) - half.x - (other.half.x * Mathf.Abs(axis.x) + Mathf.Abs(axis.z) * other.half.z);
			float gapZ = Mathf.Abs(offset.z) - half.z - (other.half.x * Mathf.Abs(axis.z) + Mathf.Abs(axis.x) * other.half.z);
			if (gapX > 0f && gapZ > 0f)
			{
				return Mathf.Sqrt(gapX * gapX + gapZ * gapZ);
			}
			return Mathf.Max(gapX, gapZ);
		}

		private float BoxDistance(Vector3 p)
		{
			// Rebuilt from native code (token 0x0600049F, RVA 0x27B7A10)
			Vector3 local = Quaternion.Inverse(boxRotation) * (p - boxCentre);
			float dx = Mathf.Abs(local.x) - half.x;
			float dz = Mathf.Abs(local.z) - half.z;
			float outsideX = Mathf.Max(dx, 0f);
			float outsideZ = Mathf.Max(dz, 0f);
			return Mathf.Min(Mathf.Max(dx, dz), 0f) + Mathf.Sqrt(outsideX * outsideX + outsideZ * outsideZ);
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060004A1, RVA 0x27B8758)
			all.Add(this);
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060004A2, RVA 0x27B882C)
			all.Remove(this);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060004A3, RVA 0x27B88AC)
			if (movedFrame == Time.frameCount)
			{
				return;
			}
			movedFrame = Time.frameCount;
			float t = NetBridge.SharedTime;
			PlayerRig rig = PlayerRig.I;
			for (int i = 0; i < all.Count; i++)
			{
				all[i].Move(t, rig);
			}
			Physics.SyncTransforms();
		}

		private void Move(float t, PlayerRig rig)
		{
			// Rebuilt from native code (token 0x060004A4, RVA 0x27B8A88)
			bool riding = rig && !rig.Frozen && Carries(rig);
			Vector3 local = riding ? transform.InverseTransformPoint(rig.BodyBottom) : Vector3.zero;
			if (mode == Mode.Drift)
			{
				float target = riding || RemoteOnTop() ? dip : 0f;
				dipped = Mathf.MoveTowards(dipped, target, Time.deltaTime * 0.25f);
			}
			Pose(t, out Vector3 position, out Quaternion rotation);
			transform.SetPositionAndRotation(position, rotation);
			if (riding)
			{
				rig.Carry(transform.TransformPoint(local) - rig.BodyBottom, VelocityAt(t, local));
			}
		}

		private Vector3 VelocityAt(float t, Vector3 local)
		{
			// Rebuilt from native code (token 0x060004A5, RVA 0x27B92AC)
			Vector3 scale = transform.lossyScale;
			Pose(t - 0.04f, out Vector3 before, out Quaternion beforeRotation);
			Pose(t + 0.04f, out Vector3 after, out Quaternion afterRotation);
			Vector3 from = Matrix4x4.TRS(before, beforeRotation, scale).MultiplyPoint3x4(local);
			Vector3 to = Matrix4x4.TRS(after, afterRotation, scale).MultiplyPoint3x4(local);
			return (to - from) / 0.08f;
		}

		private void Pose(float t, out Vector3 position, out Quaternion rotation)
		{
			// Rebuilt from native code (token 0x060004A6, RVA 0x27B8F44)
			position = home;
			rotation = homeRotation;
			switch (mode)
			{
				case Mode.Spin:
					rotation = Quaternion.Euler(0f, startAngle + degreesPerSecond * t, 0f) * homeRotation;
					break;
				case Mode.Shuttle:
					position = home + travel * ShuttleAmount(t);
					break;
				case Mode.Drift:
				{
					float range = DriftReach;
					float time = Surged(t) * Mathf.Clamp(20f / Mathf.Max(driftPeriod, 1f), 0.8f, 1.25f);
					float angle = Mathf.Max(driftSpeed, 0.1f) * time * Mathf.PI * 2f;
					float x = MathF.Sin(seed * 11f + driftFrequency.x * angle) * 0.6f + MathF.Sin(seed * 23f + driftFrequency.y * angle * 1.9f) * 0.28f + MathF.Sin(seed * 37f + driftFrequency.z * angle * 3.4f) * 0.12f;
					float z = MathF.Sin(seed * 17f + driftFrequency.y * angle * 1.13f) * 0.6f + MathF.Sin(seed * 29f + driftFrequency.z * angle * 2.3f) * 0.28f + MathF.Sin(seed * 41f + driftFrequency.x * angle * 3.1f) * 0.12f;
					position = new Vector3(home.x + range * x, home.y + (bob * MathF.Sin((t / 3.1f + phase) * Mathf.PI * 2f) - dipped), home.z + range * z);
					break;
				}
			}
		}

		private float Surged(float t)
		{
			// Rebuilt from native code (token 0x060004A7, RVA 0x27B9620)
			if (surge <= 0f || surgeCycle <= 0f)
			{
				return t;
			}
			float cycles = t / surgeCycle;
			float cycle = Mathf.Floor(cycles);
			float within = cycles - cycle;
			uint hash = surgeSeed * 0x9E37u ^ (uint)Mathf.FloorToInt(cycles) * 0x9E3779B1u;
			hash ^= hash >> 15;
			hash *= 0x85EBCA77u;
			hash ^= hash >> 13;
			hash *= 0xC2B2AE3Du;
			hash ^= hash >> 16;
			float roll = (hash & 0xFFFF) / 65535f;
			float rest = roll < 0.3f ? 0f : Mathf.Lerp(0.12f, 0.3f, (roll - 0.3f) / 0.7f);
			float surged = within < rest ? 0f : Smooth((within - rest) / (1f - rest));
			return surgeCycle * (cycle + Mathf.Lerp(within, surged, Mathf.Clamp01(surge)));
		}

		private float ShuttleAmount(float t)
		{
			// Rebuilt from native code (token 0x060004A8, RVA 0x27B94AC)
			float length = Mathf.Max(period, 0.5f);
			float halfway = length * 0.5f;
			float wait = Mathf.Clamp(pause, 0f, halfway * 0.9f);
			float at = length * Mathf.Repeat(phase + t / length, 1f);
			float run = halfway - wait;
			if (at < wait)
			{
				return 0f;
			}
			if (at < halfway)
			{
				return Smooth((at - wait) / run);
			}
			if (at < halfway + wait)
			{
				return 1f;
			}
			return 1f - Smooth((at - halfway - wait) / run);
		}

		private static float Smooth(float x)
		{
			// Rebuilt from native code (token 0x060004A9, RVA 0x27B97F0)
			x = Mathf.Clamp01(x);
			return x * x * (3f - 2f * x);
		}

		private bool Carries(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x060004AA, RVA 0x27B8D28)
			if (!col)
			{
				return false;
			}
			if (Near(rig.BodyBottom, 0.08f))
			{
				return true;
			}
			GorillaLocomotion locomotion = rig.locomotion;
			if (!locomotion || !locomotion.enabled)
			{
				return false;
			}
			return (locomotion.LeftTouching && Near(rig.leftHand.position, 0.1f)) || (locomotion.RightTouching && Near(rig.rightHand.position, 0.1f));
		}

		private bool Near(Vector3 p, float distance)
		{
			// Rebuilt from native code (token 0x060004AB, RVA 0x27B9820)
			return (col.ClosestPoint(p) - p).sqrMagnitude < distance * distance;
		}

		private bool RemoteOnTop()
		{
			// Rebuilt from native code (token 0x060004AC, RVA 0x27B8E58)
			List<Vector3> feet = NetBridge.RemoteFeet;
			for (int i = 0; i < feet.Count; i++)
			{
				if (Near(feet[i], 0.15f))
				{
					return true;
				}
			}
			return false;
		}
	}
}
