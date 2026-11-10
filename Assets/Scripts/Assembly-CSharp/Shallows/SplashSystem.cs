using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(-50)]
	public class SplashSystem : MonoBehaviour
	{
		public const int DropletFrames = 4;

		// Rebuilt from native code (token 0x06000B21, RVA 0x276CF3C; token 0x06000B22, RVA 0x276D030)
		public static event Action<SplashEvent> Heard;

		public Material dropletMaterial;

		public Material ringMaterial;

		// Rebuilt from native code (token 0x06000B2E, RVA 0x276E1D0)
		[Tooltip("How many sounds can play at once.")]
		public int audioVoices = 12;

		private ParticleSystem droplets;

		private AudioSource[] voices;

		private int nextVoice;

		private DebugRing[] rings;

		private int nextRing;

		private int heardBy;

		private int splashVariant;

		// Rebuilt from native code (token 0x06000B2F, RVA 0x276E1E0)
		private static readonly Color[] DropletColors = new Color[]
		{
			new Color(1f, 1f, 1f, 1f),
			new Color(0.92f, 0.97f, 1f, 1f),
			new Color(0.82f, 0.93f, 1f, 1f)
		};

		public static SplashSystem I { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000B23, RVA 0x276D124)
			Heard = null;
			I = null;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000B24, RVA 0x276D1CC)
			I = this;
			droplets = MakeParticles("Droplets", dropletMaterial, ParticleSystemRenderMode.Billboard, 1500, 1f);
			ParticleSystem.TextureSheetAnimationModule sheet = droplets.textureSheetAnimation;
			sheet.enabled = true;
			sheet.mode = ParticleSystemAnimationMode.Grid;
			sheet.numTilesX = DropletFrames;
			sheet.numTilesY = 1;
			sheet.animation = ParticleSystemAnimationType.WholeSheet;
			sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
			voices = new AudioSource[Mathf.Max(audioVoices, 1)];
			for (int i = 0; i < voices.Length; i++)
			{
				GameObject voice = new GameObject("Voice " + i);
				voice.transform.SetParent(transform, false);
				AudioSource source = voice.AddComponent<AudioSource>();
				source.playOnAwake = false;
				source.spatialBlend = 1f;
				source.rolloffMode = AudioRolloffMode.Logarithmic;
				source.minDistance = 1.5f;
				source.maxDistance = 60f;
				source.dopplerLevel = 0f;
				voices[i] = source;
			}
			rings = new DebugRing[8];
			for (int i = 0; i < rings.Length; i++)
			{
				GameObject ring = new GameObject("Hearing Ring " + i);
				ring.transform.SetParent(transform, false);
				rings[i] = ring.AddComponent<DebugRing>();
				rings[i].Init(ringMaterial);
			}
		}

		private ParticleSystem MakeParticles(string name, Material material, ParticleSystemRenderMode mode, int maxParticles, float gravity)
		{
			// Rebuilt from native code (token 0x06000B25, RVA 0x276D6CC)
			GameObject go = new GameObject(name);
			go.transform.SetParent(transform, false);
			ParticleSystem particles = go.AddComponent<ParticleSystem>();
			particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = particles.main;
			main.loop = true;
			main.playOnAwake = false;
			main.duration = 5f;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = maxParticles;
			main.startSpeed = 0f;
			main.startLifetime = 1f;
			main.gravityModifier = gravity;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			ParticleSystem.EmissionModule emission = particles.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = particles.shape;
			shape.enabled = false;
			ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
			renderer.sharedMaterial = material;
			renderer.renderMode = mode;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			renderer.maxParticleSize = 2f;
			particles.Play();
			return particles;
		}

		public void Loud(Vector3 surfacePoint, float intensity, SplashKind kind, bool remote = false, Vector3 heading = default(Vector3))
		{
			// Rebuilt from native code (token 0x06000B26, RVA 0x275DD1C)
			float i = Mathf.Clamp01(intensity);
			SpawnVfx(surfacePoint, Mathf.Lerp(0.3f, 1.6f, i), 1f);
			PlaySound(Sfx.Splash(i, splashVariant++), surfacePoint, Mathf.Lerp(0.45f, 1f, i), Mathf.Lerp(1.35f, 0.8f, i));
			WaterVolume.AddRipple(surfacePoint, Mathf.Lerp(0.45f, 1f, i));
			ShallowsTuning tuning = ShallowsTuning.I;
			float hearingRadius = tuning ? tuning.HearingRadius(i) : (i >= 0.75f ? 1000f : Mathf.Lerp(6f, 45f, i));
			if (kind == SplashKind.Balloon)
			{
				hearingRadius = 1000f;
			}
			heardBy = 0;
			SplashEvent splash = new SplashEvent
			{
				position = surfacePoint,
				intensity = i,
				hearingRadius = hearingRadius,
				kind = kind,
				heading = heading
			};
			Heard?.Invoke(splash);
			if (!remote)
			{
				NetBridge.ReportSplash(splash);
			}
			if (tuning && tuning.showDebug)
			{
				rings[nextRing].Play(surfacePoint + Vector3.up * 0.03f, hearingRadius, new Color(1f, heardBy > 0 ? 0.25f : 0.9f, 0.2f, 1f));
				nextRing = (nextRing + 1) % rings.Length;
			}
		}

		public void Quiet(Vector3 surfacePoint, float amount)
		{
			// Rebuilt from native code (token 0x06000B27, RVA 0x276CCAC)
			WaterVolume.AddRipple(surfacePoint, Mathf.Lerp(0.15f, 0.35f, Mathf.Clamp01(amount)));
		}

		public void Ripple(Vector3 surfacePoint, float strength)
		{
			// Rebuilt from native code (token 0x06000B28, RVA 0x276CD5C)
			WaterVolume.AddRipple(surfacePoint, strength);
		}

		public void Visual(Vector3 surfacePoint, float size)
		{
			// Rebuilt from native code (token 0x06000B29, RVA 0x276DF14)
			SpawnVfx(surfacePoint, size, 1f);
			WaterVolume.AddRipple(surfacePoint, Mathf.Clamp01(size));
		}

		public void ReportHeard()
		{
			// Rebuilt from native code (token 0x06000B2A, RVA 0x276DFCC)
			heardBy++;
		}

		public void PlaySound(AudioClip clip, Vector3 position, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x06000B2B, RVA 0x276A6CC)
			if (clip == null)
			{
				return;
			}
			float clipPitch = Sfx.PitchFor(clip, pitch);
			AudioSource source = voices[nextVoice];
			nextVoice = (nextVoice + 1) % voices.Length;
			source.transform.position = position;
			source.pitch = clipPitch;
			source.volume = Mathf.Clamp01(volume);
			source.clip = clip;
			source.Play();
		}

		private void SpawnVfx(Vector3 p, float size, float density)
		{
			// Rebuilt from native code (token 0x06000B2C, RVA 0x276D98C)
			size = Mathf.Max(0.05f, size);
			float g = Mathf.Abs(Physics.gravity.y);
			ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
			{
				applyShapeToPosition = false
			};
			int crown = Mathf.RoundToInt(Mathf.Lerp(6f, 70f, size / 1.6f) * density);
			for (int i = 0; i < crown; i++)
			{
				float angle = Random.value * Mathf.PI * 2f;
				Vector3 dir = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
				float up = (size * 2.6f + 1.1f) * Random.Range(0.55f, 1f);
				float outward = (size * 1.4f + 0.5f) * Random.Range(0.3f, 1f);
				EmitDrop(ref ep, p + dir * (size * 0.04f) + Vector3.up * 0.01f, dir * outward + Vector3.up * up, size, g);
			}
			if (size <= 0.7f)
			{
				return;
			}
			int column = Mathf.RoundToInt(size * 14f * density);
			for (int i = 0; i < column; i++)
			{
				Vector3 r = Random.insideUnitSphere;
				float up = (size * 3f + 1.6f) * Random.Range(0.8f, 1.15f);
				EmitDrop(ref ep, p + new Vector3(r.x, 0f, r.z) * 0.05f, new Vector3(r.x * 0.35f, 0f, r.z * 0.35f) * size + Vector3.up * up, size * 1.2f, g);
			}
		}

		private void EmitDrop(ref ParticleSystem.EmitParams ep, Vector3 position, Vector3 velocity, float s, float g)
		{
			// Rebuilt from native code (token 0x06000B2D, RVA 0x276DFDC)
			ep.position = position;
			ep.velocity = velocity;
			ep.startSize = (0.7f + s * 0.4f) * Random.Range(0.055f, 0.1f);
			ep.startColor = DropletColors[Random.Range(0, DropletColors.Length)];
			ep.rotation = 0f;
			ep.startLifetime = Mathf.Clamp(2f * Mathf.Max(0f, velocity.y) / g + 0.02f, 0.08f, 2.2f);
			droplets.Emit(ep, 1);
		}
	}
}
