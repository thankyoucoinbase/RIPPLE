using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class AmbientSounds : MonoBehaviour
	{
		private static AmbientSounds instance;

		private AudioSource room;

		private AudioSource wading;

		private Vector3 lastBody;

		private bool hasBody;

		private float wadeSpeed;

		private float roomLevel;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600014B, RVA 0x27212B4)
			instance = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Begin()
		{
			// Rebuilt from native code (token 0x0600014C, RVA 0x2721308)
			if (!instance)
			{
				GameObject go = new GameObject("Ambient Sounds");
				DontDestroyOnLoad(go);
				instance = go.AddComponent<AmbientSounds>();
			}
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x0600014D, RVA 0x272142C)
			room = Loop(Sfx.Ambience());
			wading = Loop(Sfx.WadingLoop());
			if (room)
			{
				room.time = Random.Range(0f, room.clip.length);
				room.Play();
			}
		}

		private AudioSource Loop(AudioClip clip)
		{
			// Rebuilt from native code (token 0x0600014E, RVA 0x2721558)
			if (!clip)
			{
				return null;
			}
			AudioSource source = gameObject.AddComponent<AudioSource>();
			source.clip = clip;
			source.loop = true;
			source.playOnAwake = false;
			source.spatialBlend = 0f;
			source.volume = 0f;
			source.dopplerLevel = 0f;
			return source;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600014F, RVA 0x2721660)
			ShallowsTuning tuning = ShallowsTuning.I;
			float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
			if (room)
			{
				roomLevel = Mathf.MoveTowards(roomLevel, 1f, dt / 3f);
				room.volume = roomLevel * (tuning ? tuning.ambienceVolume : 0.35f);
			}
			if (!wading)
			{
				return;
			}
			float wade = 0f;
			PlayerRig rig = PlayerRig.I;
			if (rig && rig.head)
			{
				Vector3 body = rig.BodyBottom;
				if (hasBody)
				{
					float dx = body.x - lastBody.x;
					float dz = body.z - lastBody.z;
					float speed = Mathf.Sqrt(dx * dx + dz * dz) / dt;
					if (speed > 20f)
					{
						speed = 0f;
					}
					wadeSpeed = Mathf.Lerp(wadeSpeed, speed, 1f - MathF.Exp(-6f * dt));
				}
				lastBody = body;
				hasBody = true;
				if (rig.InWater && !rig.Frozen)
				{
					wade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 1.4f, wadeSpeed));
				}
			}
			float volume = Mathf.MoveTowards(wading.volume, wade * (tuning ? tuning.wadingVolume : 0.6f), dt * 2.5f);
			wading.volume = volume;
			wading.pitch = Mathf.Lerp(0.88f, 1.08f, wadeSpeed * 0.5f);
			if (volume > 0.001f && !wading.isPlaying)
			{
				wading.time = Random.Range(0f, wading.clip.length);
				wading.Play();
			}
			else if (volume <= 0.001f && wading.isPlaying)
			{
				wading.Pause();
			}
		}
	}
}
