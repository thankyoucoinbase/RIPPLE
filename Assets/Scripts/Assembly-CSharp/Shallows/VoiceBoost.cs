using System;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(AudioSource))]
	public class VoiceBoost : MonoBehaviour
	{
		private PeakLimiter limiter;

		private volatile float gain = 1f;

		private volatile bool muted;

		private int sampleRate;

		public bool Muted
		{
			get
			{
				// Rebuilt from native code (token 0x06000868, RVA 0x282AC0C)
				return muted;
			}
			set
			{
				// Rebuilt from native code (token 0x06000869, RVA 0x282AC24)
				muted = value;
			}
		}

		public static VoiceBoost Fit(Transform voice)
		{
			// Rebuilt from native code (token 0x0600086A, RVA 0x282AC48)
			if (!voice)
			{
				return null;
			}
			if (!voice.GetComponent<AudioSource>())
			{
				return null;
			}
			VoiceBoost boost = voice.GetComponent<VoiceBoost>();
			if (!boost)
			{
				return voice.gameObject.AddComponent<VoiceBoost>();
			}
			return boost;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600086B, RVA 0x282AD80)
			sampleRate = AudioSettings.outputSampleRate;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600086C, RVA 0x282AD9C)
			ShallowsTuning tuning = ShallowsTuning.I;
			float db = tuning ? tuning.voiceBoostDb : 10f;
			gain = MathF.Pow(10f, db / 20f);
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			// Rebuilt from native code (token 0x0600086D, RVA 0x282AE68)
			if (channels < 1)
			{
				return;
			}
			if (muted)
			{
				Array.Clear(data, 0, data.Length);
				return;
			}
			float boost = gain;
			for (int i = 0; i + channels <= data.Length; i += channels)
			{
				float peak = 0f;
				for (int c = 0; c < channels; c++)
				{
					peak = Mathf.Max(peak, Mathf.Abs(data[i + c]));
				}
				float scale = boost * limiter.Gain(boost * peak, sampleRate);
				for (int c = 0; c < channels; c++)
				{
					data[i + c] *= scale;
				}
			}
		}
	}
}
