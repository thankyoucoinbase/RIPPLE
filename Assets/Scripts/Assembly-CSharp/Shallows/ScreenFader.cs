using System.Collections;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(MeshRenderer))]
	public class ScreenFader : MonoBehaviour
	{
		public bool startBlack = true;

		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private MeshRenderer quad;

		private Material material;

		private Color color = Color.black;

		private float alpha;

		private Coroutine running;

		public bool IsFading
		{
			get
			{
				// Rebuilt from native code (token 0x06000320, RVA 0x2793968)
				return running != null;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000321, RVA 0x2793978)
			quad = GetComponent<MeshRenderer>();
			material = quad.material;
			material.renderQueue = 5000;
			alpha = startBlack ? 1f : 0f;
			Apply();
		}

		public Coroutine FadeTo(float target, float seconds, Color? to = null)
		{
			// Rebuilt from native code (token 0x06000322, RVA 0x2793AF0)
			if (running != null)
			{
				StopCoroutine(running);
			}
			if (to.HasValue)
			{
				color = to.Value;
			}
			running = StartCoroutine(Fade(Mathf.Clamp01(target), seconds));
			return running;
		}

		public void SetInstant(float target, Color? to = null)
		{
			// Rebuilt from native code (token 0x06000323, RVA 0x2793C4C)
			if (running != null)
			{
				StopCoroutine(running);
			}
			running = null;
			if (to.HasValue)
			{
				color = to.Value;
			}
			alpha = Mathf.Clamp01(target);
			Apply();
		}

		private IEnumerator Fade(float target, float seconds)
		{
			// Rebuilt from native code (token 0x06000324, RVA 0x2793BCC; MoveNext 0x2793DBC)
			float from = alpha;
			for (float t = 0f; t < seconds; t += Time.deltaTime)
			{
				alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / seconds));
				Apply();
				yield return null;
			}
			alpha = target;
			Apply();
			running = null;
		}

		private void Apply()
		{
			// Rebuilt from native code (token 0x06000325, RVA 0x2793A30)
			material.SetColor(ColorId, new Color(color.r, color.g, color.b, alpha));
			quad.enabled = alpha > 0.001f;
		}
	}
}
