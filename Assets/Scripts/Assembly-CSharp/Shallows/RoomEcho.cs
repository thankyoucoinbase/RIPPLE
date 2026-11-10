using System;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(AudioReverbFilter))]
	public class RoomEcho : MonoBehaviour
	{
		[Tooltip("Used when there's no tuning object in the scene.")]
		public float amount = 1f;

		[Tooltip("How quickly it changes when you walk into a different room.")]
		public float blendSpeed = 1.5f;

		private AudioReverbFilter reverb;

		private float current = -1f;

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600051F, RVA 0x27CD064)
			reverb = GetComponent<AudioReverbFilter>();
			reverb.reverbPreset = AudioReverbPreset.User;
			reverb.dryLevel = 0f;
			reverb.roomHF = -700f;
			reverb.roomLF = 0f;
			reverb.decayTime = 2.3f;
			reverb.decayHFRatio = 0.6f;
			reverb.reflectionsDelay = 0.025f;
			reverb.reverbDelay = 0.045f;
			reverb.diffusion = 90f;
			reverb.density = 85f;
			reverb.hfReference = 5000f;
			reverb.lfReference = 250f;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000520, RVA 0x27CD1D4)
			float echo = Atmosphere.At(transform.position)?.echo ?? 1f;
			float target = echo * (ShallowsTuning.I ? ShallowsTuning.I.echo : amount);
			current = current < 0f ? target : Mathf.Lerp(current, target, 1f - MathF.Exp(-blendSpeed * Time.deltaTime));
			Apply(current);
		}

		private void Apply(float echo)
		{
			// Rebuilt from native code (token 0x06000521, RVA 0x27CD384)
			if (echo < 0.02f)
			{
				reverb.room = -10000f;
				return;
			}
			float level = MathF.Log10(echo) * 2000f;
			reverb.room = Mathf.Clamp(level - 1050f, -10000f, 0f);
			reverb.reflectionsLevel = Mathf.Clamp(level - 1500f, -10000f, 1000f);
			reverb.reverbLevel = Mathf.Clamp(level - 250f, -10000f, 2000f);
		}
	}
}
