using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class SpongeRaft : MonoBehaviour
	{
		public static readonly Vector3 Size = new Vector3(2.2f, 0.39f, 1.5f);

		public const float TopAbove = 0.12f;

		public const float SwellFor = 0.75f;

		public const float HoldFor = 18f;

		public const float ShrinkFor = 10f;

		public const float SinkFor = 1.2f;

		private const float ShrunkAcross = 0.3f;

		private const float ShrunkHeight = 0.6f;

		private const int MaxRafts = 6;

		private static readonly List<SpongeRaft> all = new List<SpongeRaft>();

		private float surfaceY;

		private float born;

		private Transform look;

		private BoxCollider box;

		private Vector3 small;

		private bool sank;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060009D1, RVA 0x2746750)
			all.Clear();
		}

		public static SpongeRaft Open(Vector3 surfacePoint, float yaw, bool remote)
		{
			// Rebuilt from native code (token 0x060009D2, RVA 0x2745E70)
			while (all.Count >= MaxRafts)
			{
				SpongeRaft oldest = all[0];
				all.RemoveAt(0);
				if (oldest)
				{
					Destroy(oldest.gameObject);
				}
			}
			GameObject go = new GameObject(remote ? "Sponge Raft (someone else's)" : "Sponge Raft");
			go.transform.SetPositionAndRotation(surfacePoint, Quaternion.Euler(0f, yaw, 0f));
			SpongeRaft raft = go.AddComponent<SpongeRaft>();
			raft.surfaceY = surfacePoint.y;
			raft.born = Time.time;
			Rigidbody body = go.AddComponent<Rigidbody>();
			body.isKinematic = true;
			body.interpolation = RigidbodyInterpolation.None;
			raft.box = go.AddComponent<BoxCollider>();
			raft.box.size = Size;
			raft.box.enabled = false;
			Transform look = new GameObject("Look").transform;
			look.SetParent(go.transform, false);
			raft.look = look;
			if (!ItemModels.Wear(look, "SpongeBig"))
			{
				GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
				Destroy(cube.GetComponent<Collider>());
				cube.transform.SetParent(look, false);
				cube.transform.localScale = Size;
				MaterialPropertyBlock block = new MaterialPropertyBlock();
				block.SetColor("_BaseColor", new Color(0.96f, 0.8f, 0.17f, 1f));
				cube.GetComponent<Renderer>().SetPropertyBlock(block);
			}
			raft.small = new Vector3(Items.SpongeSize.x / Size.x, Items.SpongeSize.y / Size.y, Items.SpongeSize.z / Size.z);
			raft.Apply(raft.small, 0f);
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.SpongeSwell(), surfacePoint, 1f, Random.Range(0.92f, 1.05f));
				if (!remote)
				{
					SplashSystem.I.Loud(surfacePoint, 0.85f, SplashKind.Object);
				}
			}
			WaterVolume.AddRipple(surfacePoint, 1f);
			all.Add(raft);
			return raft;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060009D3, RVA 0x274699C)
			all.Remove(this);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060009D4, RVA 0x2746A1C)
			float age = Time.time - born;
			Vector3 scale;
			float bob;
			if (age < SwellFor)
			{
				float t = age / SwellFor;
				float ease = 1f + 2.70158f * MathF.Pow(t - 1f, 3f) + 1.70158f * MathF.Pow(t - 1f, 2f);
				scale = Vector3.LerpUnclamped(small, Vector3.one, ease);
				scale.y *= 1f + (1f - t) * (MathF.Sin(t * MathF.PI * 3f) * 0.12f);
				bob = 0f;
			}
			else if (age < SwellFor + HoldFor)
			{
				scale = Vector3.one;
				bob = MathF.Sin(age * 1.7f) * 0.008f;
			}
			else if (age < SwellFor + HoldFor + ShrinkFor)
			{
				float shrink = Mathf.SmoothStep(0f, 1f, (age - SwellFor - HoldFor) / ShrinkFor);
				scale = Vector3.Lerp(Vector3.one, new Vector3(ShrunkAcross, ShrunkHeight, ShrunkAcross), shrink);
				bob = MathF.Sin(age * 1.7f) * 0.008f;
			}
			else
			{
				float sink = Mathf.Clamp01((age - SwellFor - HoldFor - ShrinkFor) / SinkFor);
				if (!sank)
				{
					sank = true;
					if (SplashSystem.I)
					{
						SplashSystem.I.Quiet(new Vector3(transform.position.x, surfaceY, transform.position.z), 1f);
					}
				}
				Apply(new Vector3((1f - sink) * ShrunkAcross, (1f - sink * 0.5f) * ShrunkHeight, (1f - sink) * ShrunkAcross), sink * -0.3f);
				if (sink >= 1f)
				{
					Destroy(gameObject);
				}
				return;
			}
			Apply(scale, 0f);
			if (look)
			{
				look.localPosition = new Vector3(0f, bob, 0f);
			}
		}

		private void Apply(Vector3 scale, float sink)
		{
			// Rebuilt from native code (token 0x060009D5, RVA 0x27467E8)
			transform.localScale = scale;
			Vector3 position = transform.position;
			transform.position = new Vector3(position.x, Mathf.Clamp01(scale.y) * TopAbove + surfaceY + sink - scale.y * Size.y * 0.5f, position.z);
			if (box)
			{
				box.enabled = scale.x > 0.25f && !sank;
			}
		}
	}
}
