using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class GlowStick : MonoBehaviour
	{
		public bool ghost;

		[Tooltip("How long it glows once it's left your hand (s).")]
		public float glowFor = 60f;

		[Tooltip("The last seconds of that, fading out.")]
		public float fadeFor = 6f;

		public Color color = new Color(0.45f, 1f, 0.25f, 1f);

		[Tooltip("How far its light reaches (m).")]
		public float range = 4f;

		[Tooltip("The real light's intensity (URP/Lit things).")]
		public float intensity = 1.6f;

		[Tooltip("How bright it makes the tiles and water (the Shallows shaders).")]
		public float shaderBrightness = 1.1f;

		public const int MaxShaderGlows = 4;

		private static readonly List<GlowStick> lit = new List<GlowStick>();

		private static readonly int PosId = Shader.PropertyToID("_ShallowsGlowPos");

		private static readonly int ColorId = Shader.PropertyToID("_ShallowsGlowColor");

		private static readonly int InfoId = Shader.PropertyToID("_ShallowsGlowInfo");

		private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

		private static readonly Vector4[] positions = new Vector4[MaxShaderGlows];

		private static readonly Vector4[] colors = new Vector4[MaxShaderGlows];

		private static int pushedFrame = -1;

		private Throwable item;

		private Rigidbody rb;

		private Renderer core;

		private MaterialPropertyBlock block;

		private Light glow;

		private bool cracked;

		private bool loose;

		private float looseSince;

		private float level;

		private float shown = -1f;

		public float Level
		{
			get
			{
				// Rebuilt from native code (token 0x060008C7, RVA 0x28361E4)
				return level;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060008C6, RVA 0x2836144)
			lit.Clear();
			pushedFrame = -1;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x060008C8, RVA 0x28361EC)
			item = GetComponent<Throwable>();
			if (!item)
			{
				return;
			}
			item.Grabbed += Crack;
			item.Released += _ =>
			{
				loose = true;
				looseSince = Time.time;
			};
			item.Grabbed += () => loose = false;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x060008C9, RVA 0x2836390)
			Transform coreTransform = LifeguardProps.Find(transform, "Core");
			core = coreTransform ? coreTransform.GetComponent<Renderer>() : null;
			block = new MaterialPropertyBlock();
			if (ghost)
			{
				cracked = true;
				loose = true;
				looseSince = Time.time;
			}
			Show(cracked ? 1f : 0.12f);
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060008CA, RVA 0x2836640)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060008CB, RVA 0x28366E4)
			PhysicsClock.Step -= PhysicsStep;
			lit.Remove(this);
			if (lit.Count == 0)
			{
				Shader.SetGlobalVector(InfoId, Vector4.zero);
			}
		}

		private void Crack()
		{
			// Rebuilt from native code (token 0x060008CC, RVA 0x2836870)
			if (cracked)
			{
				return;
			}
			cracked = true;
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.GlowStickCrack(), transform.position, 0.7f, 1f);
			}
			XRHands.HapticBoth(0.25f, 0.06f);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060008CD, RVA 0x2836A48)
			if (!cracked)
			{
				return;
			}
			float age = loose ? Time.time - looseSince : 0f;
			level = Mathf.Clamp01((glowFor - age) / Mathf.Max(fadeFor, 0.01f));
			float flicker = 0.93f + Mathf.PerlinNoise(Time.time * 3f, GetInstanceID() * 0.01f) * 0.07f;
			Show(Mathf.Max(level * flicker, 0.12f));
			if (level <= 0.01f)
			{
				lit.Remove(this);
				if (glow)
				{
					glow.enabled = false;
				}
				if (loose)
				{
					Destroy(gameObject);
				}
				return;
			}
			if (!lit.Contains(this))
			{
				lit.Add(this);
			}
			if (!glow)
			{
				glow = MakeLight();
			}
			glow.intensity = flicker * (level * intensity);
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060008CE, RVA 0x2836EA0)
			if (pushedFrame != Time.frameCount)
			{
				pushedFrame = Time.frameCount;
				Push();
			}
		}

		private static void Push()
		{
			// Rebuilt from native code (token 0x060008CF, RVA 0x2836F40)
			if (lit.Count == 0)
			{
				Shader.SetGlobalVector(InfoId, Vector4.zero);
				return;
			}
			Camera main = Camera.main;
			Vector3 eye = main ? main.transform.position : Vector3.zero;
			lit.RemoveAll(g => !g);
			lit.Sort((a, b) => (a.transform.position - eye).sqrMagnitude.CompareTo((b.transform.position - eye).sqrMagnitude));
			int count = lit.Count;
			int active = Mathf.Min(count, MaxShaderGlows);
			for (int i = 0; i < MaxShaderGlows; i++)
			{
				if (i < count)
				{
					GlowStick stick = lit[i];
					Vector3 p = stick.transform.position;
					positions[i] = new Vector4(p.x, p.y, p.z, stick.range);
					colors[i] = new Vector4(stick.color.r * stick.shaderBrightness * stick.level, stick.color.g * stick.shaderBrightness * stick.level, stick.color.b * stick.shaderBrightness * stick.level, 0f);
				}
				else
				{
					positions[i] = Vector4.zero;
					colors[i] = Vector4.zero;
				}
			}
			Shader.SetGlobalVectorArray(PosId, positions);
			Shader.SetGlobalVectorArray(ColorId, colors);
			Shader.SetGlobalVector(InfoId, new Vector4(active, 0f, 0f, 0f));
		}

		private Light MakeLight()
		{
			// Rebuilt from native code (token 0x060008D0, RVA 0x2836D74)
			GameObject go = new GameObject("Glow Light");
			go.transform.SetParent(transform, false);
			Light light = go.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = color;
			light.range = range;
			light.shadows = LightShadows.None;
			light.renderMode = LightRenderMode.ForcePixel;
			return light;
		}

		private void Show(float amount)
		{
			// Rebuilt from native code (token 0x060008D1, RVA 0x2836510)
			if (!core || Mathf.Abs(amount - shown) < 0.01f)
			{
				return;
			}
			shown = amount;
			core.GetPropertyBlock(block);
			block.SetColor(EmissionId, new Color(amount * 1.21f, amount * 2.2f, amount * 0.77f, amount * 2.2f));
			core.SetPropertyBlock(block);
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x060008D2, RVA 0x2837494)
			if (!rb)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (!rb || rb.isKinematic)
			{
				return;
			}
			Vector3 position = rb.position;
			if (!WaterVolume.IsUnderwater(position + Vector3.down * 0.01f, out WaterVolume water))
			{
				return;
			}
			float depth = (water.SurfaceY - 0.005f - position.y) / 0.02f;
			rb.AddForce(Vector3.up * -(Physics.gravity.y * (Mathf.Clamp(depth, -1f, 2f) + 1f)), ForceMode.Acceleration);
			if (!ghost)
			{
				return;
			}
			rb.linearDamping = 4f;
			rb.angularDamping = 3f;
		}
	}
}
