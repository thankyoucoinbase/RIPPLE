using System;

namespace Shallows
{
	public struct PeakLimiter
	{
		public const float Ceiling = 0.95f;

		private float reduction;

		private float release;

		private int rate;

		public float Gain(float peak, int sampleRate)
		{
			// Rebuilt from native code (token 0x06000267, RVA 0x2784FF0)
			if (rate != sampleRate)
			{
				rate = sampleRate;
				release = (float)Math.Exp(-1.0 / (Math.Max(1, sampleRate) * 0.15));
			}
			if (!(reduction > 0f))
			{
				reduction = 1f;
			}
			float gain = 1f - (1f - reduction) * release;
			if (gain * peak > Ceiling)
			{
				gain = Ceiling / peak;
			}
			reduction = gain;
			return gain;
		}
	}
}
