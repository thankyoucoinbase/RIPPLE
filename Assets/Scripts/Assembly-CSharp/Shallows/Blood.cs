using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class Blood : MonoBehaviour
	{
		private class Splat
		{
			public Transform t;

			public Mesh mesh;

			public Color32[] colors;

			public float born;

			public float life;

			public float slide;
		}

		private static Blood instance;

		public static Transform LensAnchor;

		public static bool LensOff;

		private static readonly Color[] Reds = new Color[]
		{
			new Color(1f, 0.1f, 0.14f, 1f),
			new Color(0.88f, 0.04f, 0.09f, 1f),
			new Color(1f, 0.24f, 0.26f, 1f),
			new Color(0.74f, 0.02f, 0.07f, 1f)
		};

		private const int DropFrames = 4;

		private const int StainFrames = 4;

		private const int SplatFrames = 4;

		private ParticleSystem drops;

		private ParticleSystem stains;

		private Material lensMaterial;

		private Texture2D splatTexture;

		private readonly List<Splat> splats = new List<Splat>();

		private static Blood I
		{
			get
			{
				// Rebuilt from native code (token 0x0600016A, RVA 0x27251FC)
				if (!instance)
				{
					instance = new GameObject("Blood").AddComponent<Blood>();
				}
				return instance;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600016B, RVA 0x2725344)
			instance = null;
			LensAnchor = null;
			LensOff = false;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600016C, RVA 0x27253CC)
			Shader shader = Shader.Find("Shallows/Blood");
			if (!shader)
			{
				shader = Shader.Find("Shallows/Particle");
			}
			drops = MakeParticles("Blood Drops", NewMaterial(shader, MakeDrops(), 3001), ParticleSystemRenderMode.Billboard, 800, 1f, DropFrames);
			ParticleSystem.ColorOverLifetimeModule dropColor = drops.colorOverLifetime;
			dropColor.enabled = true;
			dropColor.color = new ParticleSystem.MinMaxGradient(Fade(0.55f));
			stains = MakeParticles("Blood Stains", NewMaterial(shader, MakeStains(), 3002), ParticleSystemRenderMode.HorizontalBillboard, 120, 0f, StainFrames);
			ParticleSystem.ColorOverLifetimeModule stainColor = stains.colorOverLifetime;
			stainColor.enabled = true;
			stainColor.color = new ParticleSystem.MinMaxGradient(Fade(0.35f));
			ParticleSystem.SizeOverLifetimeModule stainSize = stains.sizeOverLifetime;
			stainSize.enabled = true;
			stainSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f, 0f, 6f), new Keyframe(0.18f, 0.9f), new Keyframe(1f, 1.15f)));
			splatTexture = MakeSplats();
			lensMaterial = NewMaterial(shader, splatTexture, 4000);
			lensMaterial.SetFloat("_ZTest", 8f);
		}

		public static void Burst(Vector3 at, Vector3 direction, float amount)
		{
			// Rebuilt from native code (token 0x0600016D, RVA 0x2726BA0)
			Blood blood = I;
			float g = Mathf.Abs(Physics.gravity.y);
			float t = Mathf.Clamp01(amount / 1.5f);
			int count = Mathf.RoundToInt(Mathf.Lerp(10f, 60f, t));
			Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
			WaterVolume water = WaterVolume.At(at);
			float floor = water ? water.SurfaceY : at.y - 3f;
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
			for (int i = 0; i < count; i++)
			{
				Vector3 velocity = Random.insideUnitSphere * (0.8f + amount * 1.1f) + dir * Random.Range(1.2f, 2.8f) * (0.6f + amount * 0.4f) + Vector3.up * Random.Range(0.4f, 1.6f);
				Vector3 position = at + Random.insideUnitSphere * 0.06f;
				emit.position = position;
				emit.velocity = velocity;
				emit.startSize = (0.8f + amount * 0.3f) * Random.Range(0.045f, 0.09f);
				emit.startColor = Reds[Random.Range(0, Reds.Length)];
				emit.rotation = 0f;
				emit.startLifetime = Mathf.Clamp((velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2f * g * Mathf.Max(0f, position.y - floor))) / g, 0.15f, 2.2f);
				blood.drops.Emit(emit, 1);
			}
			if (water)
			{
				Stain(new Vector3(at.x, water.SurfaceY, at.z), Mathf.Lerp(0.5f, 1.3f, t));
			}
		}

		public static void Stain(Vector3 surfacePoint, float size)
		{
			// Rebuilt from native code (token 0x0600016E, RVA 0x27272C8)
			Blood blood = I;
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
			int count = size > 0.9f ? 3 : 2;
			for (int i = 0; i < count; i++)
			{
				Vector2 offset = Random.insideUnitCircle * size * 0.3f;
				float lift = 0.012f + i * 0.002f;
				emit.position = new Vector3(surfacePoint.x + offset.x, surfacePoint.y + lift, surfacePoint.z + offset.y);
				emit.velocity = Vector3.zero;
				emit.startSize = (i == 0 ? 1f : 0.7f) * (Random.Range(0.7f, 1.1f) * size);
				emit.startColor = Reds[i == 0 ? 0 : Random.Range(1, Reds.Length)];
				emit.rotation = Random.Range(0, 4) * 90f;
				emit.startLifetime = Random.Range(5f, 7f);
				blood.stains.Emit(emit, 1);
			}
		}

		public static void Lens(float amount)
		{
			// Rebuilt from native code (token 0x0600016F, RVA 0x27275D4)
			Transform cam = LensAnchor ? LensAnchor : (Camera.main ? Camera.main.transform : null);
			if (!cam || LensOff)
			{
				return;
			}
			Blood blood = I;
			float scale = 0.75f + amount * 0.3f;
			int count = amount > 1.25f ? 2 : 1;
			for (int i = 0; i < count; i++)
			{
				float angle = Random.Range(0f, MathF.PI * 2f);
				float radius = Random.Range(0.09f, 0.16f);
				float size = Random.Range(0.08f, 0.14f);
				Vector3 local = new Vector3(MathF.Cos(angle) * radius, MathF.Sin(angle) * 0.7f * radius, 0.3f);
				blood.splats.Add(blood.MakeSplat(cam, local, scale * size, Random.Range(0, SplatFrames)));
			}
		}

		public static void ClearLens()
		{
			// Rebuilt from native code (token 0x06000170, RVA 0x2727E80)
			if (!instance)
			{
				return;
			}
			foreach (Splat splat in instance.splats)
			{
				if (splat.t)
				{
					Destroy(splat.t.gameObject);
				}
			}
			instance.splats.Clear();
		}

		private Splat MakeSplat(Transform cam, Vector3 local, float size, int frame)
		{
			// Rebuilt from native code (token 0x06000171, RVA 0x27278D4)
			GameObject go = new GameObject("Blood Splat");
			go.layer = cam.gameObject.layer;
			go.transform.SetParent(cam, false);
			go.transform.localPosition = local;
			go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));
			Mesh mesh = new Mesh();
			mesh.name = "Blood Splat";
			float h = size * 0.5f;
			mesh.vertices = new Vector3[]
			{
				new Vector3(-h, -h, 0f),
				new Vector3(h, -h, 0f),
				new Vector3(-h, h, 0f),
				new Vector3(h, h, 0f)
			};
			float u0 = frame / (float)SplatFrames;
			float u1 = (frame + 1) / (float)SplatFrames;
			mesh.uv = new Vector2[]
			{
				new Vector2(u0, 0f),
				new Vector2(u1, 0f),
				new Vector2(u0, 1f),
				new Vector2(u1, 1f)
			};
			mesh.triangles = new int[] { 0, 2, 1, 1, 2, 3 };
			Color32 red = Reds[Random.Range(0, 2)];
			Color32[] colors = new Color32[] { red, red, red, red };
			mesh.colors32 = colors;
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer renderer = go.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = lensMaterial;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			return new Splat
			{
				t = go.transform,
				mesh = mesh,
				colors = colors,
				born = Time.time,
				life = Random.Range(1.6f, 2.3f),
				slide = Random.Range(0.01f, 0.025f)
			};
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000172, RVA 0x2728114)
			for (int i = splats.Count - 1; i >= 0; i--)
			{
				Splat splat = splats[i];
				float age = Time.time - splat.born;
				if (!splat.t || age > splat.life)
				{
					if (splat.t)
					{
						Destroy(splat.t.gameObject);
					}
					splats.RemoveAt(i);
					continue;
				}
				if (age > 0.35f)
				{
					splat.t.localPosition += Vector3.down * splat.slide * Time.deltaTime;
				}
				byte alpha = (byte)Mathf.RoundToInt((1f - Mathf.Clamp01((age - splat.life * 0.45f) / (splat.life * 0.55f))) * 255f);
				for (int c = 0; c < splat.colors.Length; c++)
				{
					splat.colors[c].a = alpha;
				}
				splat.mesh.colors32 = splat.colors;
			}
		}

		private ParticleSystem MakeParticles(string name, Material material, ParticleSystemRenderMode mode, int max, float gravity, int frames)
		{
			// Rebuilt from native code (token 0x06000173, RVA 0x2725B5C)
			GameObject go = new GameObject(name);
			go.transform.SetParent(transform, false);
			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = system.main;
			main.loop = true;
			main.playOnAwake = false;
			main.duration = 5f;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = max;
			main.startSpeed = 0f;
			main.startLifetime = 1f;
			main.gravityModifier = gravity;
			main.scalingMode = ParticleSystemScalingMode.Hierarchy;
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = false;
			ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
			sheet.enabled = true;
			sheet.mode = ParticleSystemAnimationMode.Grid;
			sheet.numTilesX = frames;
			sheet.numTilesY = 1;
			sheet.animation = ParticleSystemAnimationType.WholeSheet;
			sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
			ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
			renderer.sharedMaterial = material;
			renderer.renderMode = mode;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			renderer.maxParticleSize = 4f;
			system.Play();
			return system;
		}

		private static Material NewMaterial(Shader shader, Texture2D texture, int queue)
		{
			// Rebuilt from native code (token 0x06000174, RVA 0x2725A68)
			Material material = new Material(shader);
			material.name = "Blood";
			material.mainTexture = texture;
			material.renderQueue = queue;
			if (material.HasProperty("_Cutoff"))
			{
				material.SetFloat("_Cutoff", 0.5f);
			}
			return material;
		}

		private static Gradient Fade(float hold)
		{
			// Rebuilt from native code (token 0x06000175, RVA 0x2725EBC)
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, hold),
				new GradientAlphaKey(0f, 1f)
			});
			return gradient;
		}

		private static Texture2D MakeDrops()
		{
			// Rebuilt from native code (token 0x06000176, RVA 0x272585C)
			Texture2D texture = NewTexture(DropFrames * 8, 8);
			System.Random rng = new System.Random(11);
			float[] radius = new float[] { 2.2f, 2.8f, 3.3f, 2.5f };
			for (int f = 0; f < DropFrames; f++)
			{
				float squash = f == 3 ? 0.7f : 1f;
				for (int y = 0; y < 8; y++)
				{
					float dy = squash * (y - 3.5f);
					for (int x = 0; x < 8; x++)
					{
						float dx = x - 3.5f;
						texture.SetPixel(f * 8 + x, y, Texel(Mathf.Sqrt(dx * dx + dy * dy) / radius[f], y == 5 && x == 3, rng));
					}
				}
			}
			texture.Apply(false, false);
			return texture;
		}

		private static Texture2D MakeStains()
		{
			// Rebuilt from native code (token 0x06000177, RVA 0x2726098)
			Texture2D texture = NewTexture(StainFrames * 16, 16);
			System.Random rng = new System.Random(23);
			for (int f = 0; f < StainFrames; f++)
			{
				List<Vector3> blobs = new List<Vector3> { new Vector3(7.5f, 7.5f, 5.2f) };
				for (int b = 0; b < 4; b++)
				{
					float angle = (float)rng.NextDouble() * MathF.PI * 2f;
					float distance = 3.5f + (float)rng.NextDouble() * 2.5f;
					blobs.Add(new Vector3(7.5f + MathF.Cos(angle) * distance, 7.5f + MathF.Sin(angle) * distance, 1.6f + (float)rng.NextDouble() * 1.6f));
				}
				for (int y = 0; y < 16; y++)
				{
					for (int x = 0; x < 16; x++)
					{
						float d = 9f;
						foreach (Vector3 blob in blobs)
						{
							d = Mathf.Min(d, Mathf.Sqrt((x - blob.x) * (x - blob.x) + (y - blob.y) * (y - blob.y)) / blob.z);
						}
						texture.SetPixel(f * 16 + x, y, Texel(d, false, rng));
					}
				}
			}
			texture.Apply(false, false);
			return texture;
		}

		private static Texture2D MakeSplats()
		{
			// Rebuilt from native code (token 0x06000178, RVA 0x2726548)
			Texture2D texture = NewTexture(SplatFrames * 32, 32);
			System.Random rng = new System.Random(37);
			for (int f = 0; f < SplatFrames; f++)
			{
				List<Vector3> blobs = new List<Vector3> { new Vector3(15.5f, 18f, 7f) };
				for (int b = 0; b < 9; b++)
				{
					float angle = (float)rng.NextDouble() * MathF.PI * 2f;
					float distance = 8f + (float)rng.NextDouble() * 6f;
					blobs.Add(new Vector3(15.5f + MathF.Cos(angle) * distance, 18f + MathF.Sin(angle) * distance * 0.8f, 0.9f + (float)rng.NextDouble() * 1.8f));
				}
				int dripCount = rng.Next(2) + 2;
				float[] dripX = new float[dripCount];
				float[] dripEnd = new float[dripCount];
				for (int i = 0; i < dripCount; i++)
				{
					dripX[i] = 10f + (float)rng.NextDouble() * 12f;
					dripEnd[i] = 1f + (float)rng.NextDouble() * 7f;
				}
				for (int y = 0; y < 32; y++)
				{
					for (int x = 0; x < 32; x++)
					{
						float d = 9f;
						foreach (Vector3 blob in blobs)
						{
							d = Mathf.Min(d, Mathf.Sqrt((x - blob.x) * (x - blob.x) + (y - blob.y) * (y - blob.y)) / blob.z);
						}
						for (int i = 0; i < dripCount; i++)
						{
							if (dripEnd[i] <= y && y <= 13)
							{
								d = Mathf.Min(d, Mathf.Abs(x - dripX[i]) / (dripEnd[i] + 1.5f > y ? 1.6f : 1.1f));
							}
						}
						texture.SetPixel(f * 32 + x, y, Texel(d, false, rng));
					}
				}
			}
			texture.Apply(false, false);
			return texture;
		}

		private static Color Texel(float d, bool shine, System.Random rng)
		{
			// Rebuilt from native code (token 0x06000179, RVA 0x27285D0)
			if (d >= 1f)
			{
				return new Color(0f, 0f, 0f, 0f);
			}
			float alpha = Mathf.Clamp(1f - d * 0.85f - (float)rng.NextDouble() * 0.18f + 0.09f, 0.02f, 1f);
			float shade = shine ? 1f : (d > 0.78f ? 0.5f : 0.82f);
			return new Color(shade, shade, shade, alpha);
		}

		private static Texture2D NewTexture(int w, int h)
		{
			// Rebuilt from native code (token 0x0600017A, RVA 0x27284DC)
			Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
			texture.name = "Blood (generated)";
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.SetPixels32(new Color32[w * h]);
			return texture;
		}
	}
}
