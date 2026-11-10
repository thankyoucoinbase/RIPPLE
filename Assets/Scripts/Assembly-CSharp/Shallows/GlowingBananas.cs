using System;
using UnityEngine;

namespace Shallows
{
	public class GlowingBananas : MonoBehaviour
	{
		[Tooltip("Each banana in the bunch. When you grab it, a single banana flies off from each one.")]
		public Transform[] bananas;

		[Tooltip("What the bananas are drawn with; the single bananas use it too.")]
		public Material material;

		public Light glow;

		public float glowIntensity = 1.6f;

		private void Update()
		{
			// Rebuilt from native code (token 0x060001DF, RVA 0x277B144)
			if (!glow) return;
			glow.intensity = glowIntensity * (MathF.Sin(Time.time * 3f) * 0.15f + 1f);
		}
	}
}
