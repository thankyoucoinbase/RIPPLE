using System;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(AudioSource))]
	public class VoiceLevel : MonoBehaviour
	{
		private AudioSource source;

		private volatile float loudness;

		public float Level { get; private set; }

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000871, RVA 0x282B020)
			source = GetComponent<AudioSource>();
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			// Rebuilt from native code (token 0x06000872, RVA 0x282B078)
			double sum = 0.0;
			for (int i = 0; i < data.Length; i++)
			{
				sum += data[i] * data[i];
			}
			loudness = data.Length == 0 ? 0f : (float)Math.Sqrt(sum / data.Length);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000873, RVA 0x282B148)
			float target = source.isPlaying ? loudness : (loudness = 0f);
			Level = Follow(Level, target);
		}

		public static float Opening(float loudness)
		{
			// Rebuilt from native code (token 0x06000874, RVA 0x282B220)
			ShallowsTuning tuning = ShallowsTuning.I;
			float quiet = tuning ? tuning.mouthQuietDb : -45f;
			float loud = tuning ? tuning.mouthLoudDb : -12f;
			if (loudness <= 1e-06f)
			{
				return 0f;
			}
			float db = 20f * MathF.Log10(loudness);
			return Mathf.Clamp01((db - quiet) / Mathf.Max(loud - quiet, 1f));
		}

		public static float Follow(float level, float loudness)
		{
			// Rebuilt from native code (token 0x06000875, RVA 0x282B1A4)
			float target = Opening(loudness);
			float k = 1f - MathF.Exp(-(target > level ? 70f : 45f) * Time.deltaTime);
			return Mathf.Lerp(level, target, k);
		}
	}
}
