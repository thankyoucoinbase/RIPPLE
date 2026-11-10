using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class DebugRing : MonoBehaviour
	{
		private const int Segments = 72;

		private const float GrowTime = 0.35f;

		private const float FadeTime = 0.9f;

		private LineRenderer line;

		private Vector3 center;

		private float radius;

		private float startTime;

		private Color color;

		private readonly Vector3[] points = new Vector3[Segments];

		public void Init(Material material)
		{
			// Rebuilt from native code (token 0x06000095, RVA 0x2711740)
			line = gameObject.AddComponent<LineRenderer>();
			line.useWorldSpace = true;
			line.loop = true;
			line.positionCount = Segments;
			line.sharedMaterial = material;
			line.shadowCastingMode = ShadowCastingMode.Off;
			line.receiveShadows = false;
			line.numCapVertices = 0;
			line.alignment = LineAlignment.TransformZ;
			transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			gameObject.SetActive(false);
		}

		public void Play(Vector3 at, float hearingRadius, Color ringColor)
		{
			// Rebuilt from native code (token 0x06000096, RVA 0x27118B4)
			center = at;
			radius = hearingRadius;
			color = ringColor;
			startTime = Time.time;
			line.widthMultiplier = Mathf.Lerp(0.05f, 0.16f, Mathf.InverseLerp(5f, 45f, hearingRadius));
			gameObject.SetActive(true);
			Update();
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000097, RVA 0x271196C)
			float t = Time.time - startTime;
			float alpha = 1f - Mathf.Clamp01((t - GrowTime) / FadeTime);
			if (alpha <= 0f)
			{
				gameObject.SetActive(false);
				return;
			}
			float k = 1f - Mathf.Clamp01(t / GrowTime);
			float r = (1f - k * k) * radius;
			for (int i = 0; i < Segments; i++)
			{
				float a = i * Mathf.PI * 2f / Segments;
				points[i] = center + new Vector3(r * MathF.Cos(a), 0f, r * MathF.Sin(a));
			}
			line.SetPositions(points);
			line.startColor = new Color(color.r, color.g, color.b, alpha);
			line.endColor = new Color(color.r, color.g, color.b, alpha);
		}
	}
}
