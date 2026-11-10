using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class BananaBurst : MonoBehaviour
	{
		private const float Separate = 0.45f;

		private const float Hover = 0.35f;

		private const float Stagger = 0.12f;

		private const float Fly = 0.55f;

		private const float Spread = 0.32f;

		private Transform into;

		private Vector3 centre;

		private Transform[] pieces;

		private Vector3[] start;

		private Vector3[] outward;

		private Vector3[] scale;

		private Quaternion[] startRotation;

		private Vector3[] flyFrom;

		private bool[] flying;

		private float t;

		private int arrived;

		public static float Duration(int count)
		{
			// Rebuilt from native code (token 0x06000151, RVA 0x2721B68)
			return Separate + Hover + Mathf.Max(0, count - 1) * Stagger + Fly;
		}

		public static float Play(Transform bunch, Transform into)
		{
			// Rebuilt from native code (token 0x06000152, RVA 0x2721B9C)
			GlowingBananas glowing = bunch.GetComponent<GlowingBananas>();
			Transform[] source;
			Material material;
			if (glowing && glowing.bananas != null && glowing.bananas.Length > 0)
			{
				source = glowing.bananas;
				material = glowing.material;
			}
			else
			{
				var found = new List<Transform>();
				foreach (Renderer renderer in bunch.GetComponentsInChildren<Renderer>())
				{
					if (renderer.name.StartsWith("Banana"))
					{
						found.Add(renderer.transform);
					}
				}
				source = found.ToArray();
				if (source.Length == 0)
				{
					source = new Transform[] { bunch };
				}
				material = null;
			}
			GameObject prefab = Resources.Load<GameObject>("BananaHill/Banana");
			var holder = new GameObject("Banana Burst");
			BananaBurst burst = holder.AddComponent<BananaBurst>();
			burst.into = into;
			int count = source.Length;
			burst.centre = bunch.position;
			burst.pieces = new Transform[count];
			burst.start = new Vector3[count];
			burst.outward = new Vector3[count];
			burst.scale = new Vector3[count];
			burst.startRotation = new Quaternion[count];
			burst.flyFrom = new Vector3[count];
			burst.flying = new bool[count];
			for (int i = 0; i < count; i++)
			{
				Transform piece = source[i];
				GameObject spawned = Instantiate(prefab ? prefab : piece.gameObject, holder.transform);
				spawned.name = "Banana";
				spawned.transform.SetPositionAndRotation(piece.position, piece.rotation);
				if (!prefab)
				{
					spawned.transform.localScale = piece.lossyScale;
				}
				foreach (Collider collider in spawned.GetComponentsInChildren<Collider>())
				{
					Destroy(collider);
				}
				foreach (Renderer renderer in spawned.GetComponentsInChildren<Renderer>())
				{
					renderer.gameObject.layer = 2;
					renderer.shadowCastingMode = ShadowCastingMode.Off;
					Material shared = material ? material : (piece.GetComponent<Renderer>() ? piece.GetComponent<Renderer>().sharedMaterial : null);
					if (shared)
					{
						renderer.sharedMaterial = shared;
					}
				}
				Vector3 from = piece.position;
				Vector3 middle = bunch.position;
				float dx = from.x - middle.x;
				float dz = from.z - middle.z;
				Vector3 away = dx * dx + dz * dz < 1e-06f ? Quaternion.Euler(0f, i * 360f / count, 0f) * Vector3.forward : new Vector3(dx, 0f, dz);
				burst.pieces[i] = spawned.transform;
				burst.start[i] = piece.position;
				burst.outward[i] = away.normalized;
				burst.startRotation[i] = piece.rotation;
				burst.scale[i] = spawned.transform.localScale;
			}
			return Duration(count);
		}

		private Vector3 Floating(int i, float time)
		{
			// Rebuilt from native code (token 0x06000153, RVA 0x27226F4)
			float rest = 1f - Mathf.Clamp01(time / Separate);
			float ease = 1f - rest * rest;
			Vector3 offset = start[i] - centre + outward[i] * (ease * Spread) + Vector3.up * (ease * 0.14f);
			return centre + Quaternion.Euler(0f, time * 50f, 0f) * offset + Vector3.up * (MathF.Sin(time * 5f + i * 1.3f) * 0.025f * ease);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000154, RVA 0x2722918)
			t += Time.deltaTime;
			Vector3 target = into ? into.position + into.up * -0.22f + into.forward * 0.05f : centre;
			int count = pieces.Length;
			for (int i = 0; i < count; i++)
			{
				Transform piece = pieces[i];
				if (!piece)
				{
					continue;
				}
				Quaternion spin = Quaternion.Euler(0f, t * (flying[i] ? 540f : 90f), 0f);
				float leave = Separate + Hover + i * Stagger;
				Quaternion wobble = Quaternion.Euler(MathF.Sin(t * 3f + i) * 12f, 0f, 0f);
				Quaternion turn = spin * wobble;
				if (t < leave)
				{
					piece.position = Floating(i, t);
					piece.rotation = turn * startRotation[i];
					continue;
				}
				if (!flying[i])
				{
					flying[i] = true;
					flyFrom[i] = piece.position;
				}
				float p = Mathf.Clamp01((t - leave) / Fly);
				if (p >= 1f)
				{
					Destroy(piece.gameObject);
					arrived++;
					if (SplashSystem.I)
					{
						SplashSystem.I.PlaySound(Sfx.BananaCollect(), target, 0.4f, arrived * 0.07f + 1.3f);
					}
					XRHands.HapticBoth(arrived * 0.08f + 0.35f, 0.06f, true);
					continue;
				}
				float ease = p * p;
				piece.position = Vector3.up * (MathF.Sin(p * MathF.PI) * 0.15f) + Vector3.Lerp(flyFrom[i], target, ease);
				piece.rotation = turn * startRotation[i];
				piece.localScale = scale[i] * Mathf.Lerp(1f, 0.35f, ease);
			}
			if (arrived >= count || t > Duration(count) + 0.5f)
			{
				Destroy(gameObject);
			}
		}
	}
}
