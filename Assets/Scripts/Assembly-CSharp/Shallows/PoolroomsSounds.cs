using System;
using UnityEngine;

namespace Shallows
{
	public static class PoolroomsSounds
	{
		private const int Rate = 22050;

		private static AudioClip hum;

		private static AudioClip strike;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600051B, RVA 0x27CCA84)
			strike = null;
			hum = null;
		}

		public static AudioClip Hum()
		{
			// Rebuilt from native code (token 0x0600051C, RVA 0x27CCAEC)
			if (hum)
			{
				return hum;
			}
			System.Random random = new System.Random(120);
			float[] d = new float[Rate];
			float noise = 0f;
			for (int i = 0; i < d.Length; i++)
			{
				double r = random.NextDouble();
				float t = i / (float)Rate;
				float mains = MathF.Sin(t * 753.98224f);
				float tone = mains * 0.5f + MathF.Sin(t * 1507.9645f) * 0.25f + MathF.Sin(t * 2261.9468f) * 0.12f + Mathf.Sign(mains) * 0.08f;
				noise += ((float)(r * 2.0 - 1.0) - noise) * 0.3f;
				d[i] = tone + Mathf.Abs(MathF.Sin(t * 376.99112f)) * (noise * 0.25f);
			}
			hum = Make("Shallows Lamp Hum", d);
			return hum;
		}

		public static AudioClip Strike()
		{
			// Rebuilt from native code (token 0x0600051D, RVA 0x27CCE58)
			if (strike)
			{
				return strike;
			}
			System.Random random = new System.Random(7);
			float[] d = new float[2646];
			for (int i = 0; i < d.Length; i++)
			{
				float t = i / (float)Rate;
				d[i] = (float)(random.NextDouble() * 2.0 - 1.0) * MathF.Exp(-t / 0.018f) + MathF.Sin(t * 15079.6455f) * 0.6f * MathF.Exp(-t / 0.004f);
			}
			strike = Make("Shallows Lamp Strike", d);
			return strike;
		}

		private static AudioClip Make(string name, float[] d)
		{
			// Rebuilt from native code (token 0x0600051E, RVA 0x27CCD8C)
			float peak = 0.0001f;
			foreach (float v in d)
			{
				peak = Mathf.Max(peak, Mathf.Abs(v));
			}
			for (int i = 0; i < d.Length; i++)
			{
				d[i] *= 0.9f / peak;
			}
			AudioClip clip = AudioClip.Create(name, d.Length, 1, Rate, false);
			clip.SetData(d, 0);
			return clip;
		}
	}
}
