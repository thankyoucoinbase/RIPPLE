using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	[DisallowMultipleComponent]
	public class FuseBeacon : MonoBehaviour
	{
		// Rebuilt from native code (token 0x0600011E, RVA 0x271C0D4)
		public Color glowColor = new Color(1f, 0.55f, 0.12f, 0.9f);

		[Tooltip("Across the glow on the pool floor (m).")]
		public float glowSize = 2.2f;

		public float bubblesPerSecond = 10f;

		[Tooltip("How fast the bubbles rise (m/s).")]
		public float bubbleSpeed = 0.32f;

		[Tooltip("Seconds between spark pops on the surface, picked at random each time.")]
		public Vector2 sparkEvery = new Vector2(0.3f, 0.75f);

		[Tooltip("Seconds between rings spreading on the surface.")]
		public float ringEvery = 1.2f;

		private Fuse fuse;

		private GameObject root;

		private Transform glow;

		private Transform halo;

		private ParticleSystem bubbles;

		private ParticleSystem sparks;

		private WaterVolume water;

		private bool shown;

		private bool preview;

		private float clock;

		private float nextSpark;

		private float nextRing;

		private float nextFloorCheck;

		private Vector3 floorPoint;

		private static Material discShared;

		private static Material bubbleShared;

		private static Material sparkShared;

		private static Material haloShared;

		private static Mesh quad;

		private static Mesh standing;

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000110, RVA 0x2719A34)
			fuse = GetComponent<Fuse>();
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000111, RVA 0x2719A8C)
			Show(false);
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000112, RVA 0x2719B84)
			if (!root)
			{
				return;
			}
			if (Application.isPlaying)
			{
				Destroy(root);
			}
			else
			{
				DestroyImmediate(root);
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000113, RVA 0x2719C60)
			if (preview)
			{
				return;
			}
			bool on = fuse && fuse.Lying && WaterVolume.IsUnderwater(transform.position, out water);
			Tick(Time.deltaTime, on);
		}

		private void Tick(float dt, bool on)
		{
			// Rebuilt from native code (token 0x06000114, RVA 0x2719D84)
			if (shown != on)
			{
				Show(on);
				if (!on)
				{
					return;
				}
				nextFloorCheck = 0f;
			}
			else if (!on)
			{
				return;
			}
			if (!water || !Build())
			{
				return;
			}
			clock += dt;
			Vector3 p = transform.position;
			float dx = p.x - floorPoint.x;
			float dz = p.z - floorPoint.z;
			if (clock >= nextFloorCheck || dx * dx + dz * dz > 0.01f)
			{
				nextFloorCheck = clock + 0.5f;
				floorPoint = FindFloor(p);
			}
			glow.SetPositionAndRotation(floorPoint + Vector3.up * 0.006f, Quaternion.identity);
			glow.localScale = Vector3.one * glowSize;
			float haloY = water.SurfaceY + 0.03f;
			if (halo)
			{
				halo.position = new Vector3(p.x, haloY, p.z);
			}
			float surface = water.SurfaceY;
			bubbles.transform.position = p;
			ParticleSystem.MainModule main = bubbles.main;
			main.startLifetime = Mathf.Max(0.1f, Mathf.Max(0.03f, surface - p.y - 0.005f) / Mathf.Max(0.05f, bubbleSpeed));
			main.startSpeed = bubbleSpeed;
			ParticleSystem.EmissionModule emission = bubbles.emission;
			emission.rateOverTime = bubblesPerSecond;
			float sparkY = surface + 0.005f;
			if (clock >= nextSpark)
			{
				nextSpark = clock + Random.Range(Mathf.Min(sparkEvery.x, sparkEvery.y), Mathf.Max(sparkEvery.x, sparkEvery.y));
				int count = Random.Range(4, 9);
				ParticleSystem.EmitParams spark = new ParticleSystem.EmitParams();
				for (int i = 0; i < count; i++)
				{
					Vector2 jitter = Random.insideUnitCircle * 0.06f;
					spark.position = new Vector3(p.x + jitter.x, sparkY, p.z + jitter.y);
					Vector2 push = Random.insideUnitCircle * 0.35f;
					spark.velocity = new Vector3(push.x, Random.Range(0.55f, 1.05f), push.y);
					spark.startLifetime = Random.Range(0.3f, 0.55f);
					spark.startSize = Random.Range(0.025f, 0.045f);
					spark.startColor = Random.value < 0.5f ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.5f, 0.1f);
					sparks.Emit(spark, 1);
				}
			}
			if (clock >= nextRing)
			{
				nextRing = clock + ringEvery;
				WaterVolume.AddRipple(new Vector3(p.x, sparkY, p.z), 0.2f);
			}
		}

		private void Show(bool on)
		{
			// Rebuilt from native code (token 0x06000115, RVA 0x2719A94)
			shown = on;
			if (!root)
			{
				return;
			}
			root.SetActive(on);
			if (on && !preview && bubbles && !bubbles.isPlaying)
			{
				bubbles.Play();
			}
		}

		private Vector3 FindFloor(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000116, RVA 0x271AD14)
			Vector3 floor = new Vector3(p.x, water ? water.FloorY : p.y - 0.05f, p.z);
			float nearest = float.MaxValue;
			foreach (RaycastHit hit in Physics.RaycastAll(p + Vector3.up * 0.1f, Vector3.down, 1.2f, -1, QueryTriggerInteraction.Ignore))
			{
				if (hit.collider.transform.IsChildOf(transform))
				{
					continue;
				}
				if (hit.collider.attachedRigidbody && hit.collider.attachedRigidbody.transform == transform)
				{
					continue;
				}
				if (hit.distance < nearest)
				{
					nearest = hit.distance;
					floor = hit.point;
				}
			}
			return floor;
		}

		private bool Build()
		{
			// Rebuilt from native code (token 0x06000117, RVA 0x271A3C8)
			if (root)
			{
				return true;
			}
			if (!Materials())
			{
				return false;
			}
			root = new GameObject("Fuse Beacon " + name);
			if (preview)
			{
				root.hideFlags = HideFlags.DontSave;
			}
			GameObject floor = new GameObject("Floor Glow");
			floor.transform.SetParent(root.transform, false);
			floor.AddComponent<MeshFilter>().sharedMesh = Quad();
			MeshRenderer floorRenderer = floor.AddComponent<MeshRenderer>();
			floorRenderer.sharedMaterial = discShared;
			floorRenderer.shadowCastingMode = ShadowCastingMode.Off;
			floorRenderer.receiveShadows = false;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			block.SetFloat("_Phase", fuse ? fuse.Id : 0f);
			floorRenderer.SetPropertyBlock(block);
			glow = floor.transform;
			if (haloShared)
			{
				GameObject surface = new GameObject("Surface Glow");
				surface.transform.SetParent(root.transform, false);
				surface.AddComponent<MeshFilter>().sharedMesh = Standing();
				MeshRenderer surfaceRenderer = surface.AddComponent<MeshRenderer>();
				surfaceRenderer.sharedMaterial = haloShared;
				surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
				surfaceRenderer.receiveShadows = false;
				halo = surface.transform;
			}
			bubbles = NewSystem("Bubbles", bubbleShared, 40);
			ParticleSystem.ShapeModule shape = bubbles.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Cone;
			shape.angle = 9f;
			shape.radius = 0.03f;
			shape.rotation = new Vector3(-90f, 0f, 0f);
			ParticleSystem.MainModule bubbleMain = bubbles.main;
			bubbleMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
			bubbleMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.65f, 0.85f), new Color(1f, 0.7f, 0.35f, 0.7f));
			ParticleSystem.NoiseModule noise = bubbles.noise;
			noise.enabled = true;
			noise.strength = 0.08f;
			noise.frequency = 2.5f;
			ParticleSystem.EmissionModule bubbleEmission = bubbles.emission;
			bubbleEmission.enabled = true;
			bubbleEmission.rateOverTime = bubblesPerSecond;
			sparks = NewSystem("Sparks", sparkShared, 60);
			ParticleSystem.MainModule sparkMain = sparks.main;
			sparkMain.gravityModifier = 0.55f;
			ParticleSystem.EmissionModule sparkEmission = sparks.emission;
			sparkEmission.enabled = false;
			ParticleSystem.ColorOverLifetimeModule fadeOut = sparks.colorOverLifetime;
			fadeOut.enabled = true;
			Gradient gradient = new Gradient();
			gradient.SetKeys(new[]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(Color.white, 1f)
			}, new[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 0.6f),
				new GradientAlphaKey(0f, 1f)
			});
			fadeOut.color = gradient;
			root.SetActive(shown);
			if (shown && !preview)
			{
				bubbles.Play();
			}
			return true;
		}

		private ParticleSystem NewSystem(string label, Material material, int max)
		{
			// Rebuilt from native code (token 0x06000118, RVA 0x271BAF8)
			GameObject go = new GameObject(label);
			go.transform.SetParent(root.transform, false);
			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = system.main;
			main.playOnAwake = false;
			main.loop = true;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = max;
			main.startSpeed = 0f;
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = false;
			ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
			renderer.sharedMaterial = material;
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			return system;
		}

		private static bool Materials()
		{
			// Rebuilt from native code (token 0x06000119, RVA 0x271B068)
			if (discShared)
			{
				return true;
			}
			Shader shader = Shader.Find("Shallows/Fuse Glow");
			if (!shader)
			{
				return false;
			}
			discShared = new Material(shader) { name = "Fuse Floor Glow" };
			discShared.SetFloat("_Disc", 1f);
			discShared.SetColor("_BaseColor", new Color(1f, 0.55f, 0.12f, 1.15f));
			bubbleShared = new Material(shader) { name = "Fuse Bubbles", mainTexture = Bubble() };
			bubbleShared.SetFloat("_Disc", 0f);
			bubbleShared.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
			sparkShared = new Material(shader) { name = "Fuse Sparks", mainTexture = Spark() };
			sparkShared.SetFloat("_Disc", 0f);
			sparkShared.SetColor("_BaseColor", new Color(1.4f, 1.2f, 1f, 1f));
			Shader haloShader = Shader.Find("Shallows/Halo");
			if (haloShader)
			{
				haloShared = new Material(haloShader) { name = "Fuse Surface Glow" };
				haloShared.SetColor("_BaseColor", new Color(1f, 0.5f, 0.1f, 0.75f));
				haloShared.SetFloat("_Size", 0.9f);
				haloShared.SetFloat("_Steps", 4f);
				haloShared.SetFloat("_Pulse", 0.18f);
			}
			return true;
		}

		private static Texture2D Bubble()
		{
			// Rebuilt from native code (token 0x0600011A, RVA 0x271BCFC)
			Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				name = "Fuse Bubble"
			};
			Color32[] pixels = new Color32[8 * 8];
			for (int y = 0; y < 8; y++)
			{
				float dy = y + 0.5f - 4f;
				for (int x = 0; x < 8; x++)
				{
					float dx = x + 0.5f - 4f;
					float d = Mathf.Sqrt(dy * dy + dx * dx);
					byte alpha = d < 2.6f ? (byte)40 : d < 3.9f ? (byte)255 : (byte)0;
					if ((y == 5 && x == 2) || (y == 4 && x == 2) || (y == 5 && x == 3))
					{
						alpha = 255;
					}
					pixels[y * 8 + x] = new Color32(255, 255, 255, alpha);
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply(false, true);
			return texture;
		}

		private static Texture2D Spark()
		{
			// Rebuilt from native code (token 0x0600011B, RVA 0x271BED8)
			Texture2D texture = new Texture2D(5, 5, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				name = "Fuse Spark"
			};
			Color32[] pixels = new Color32[5 * 5];
			for (int y = 0; y < 5; y++)
			{
				for (int x = 0; x < 5; x++)
				{
					bool cross = x == 2 || y == 2;
					bool tip = x == 0 || x == 4 || y == 0 || y == 4;
					pixels[y * 5 + x] = !cross ? new Color32(0, 0, 0, 0) : tip ? new Color32(255, 255, 255, 120) : new Color32(255, 255, 255, 255);
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply(false, true);
			return texture;
		}

		private static Mesh Standing()
		{
			// Rebuilt from native code (token 0x0600011C, RVA 0x271B7B4)
			if (!standing)
			{
				standing = new Mesh { name = "Fuse Halo Quad" };
				standing.vertices = new[]
				{
					new Vector3(-0.5f, -0.5f, 0f),
					new Vector3(0.5f, -0.5f, 0f),
					new Vector3(0.5f, 0.5f, 0f),
					new Vector3(-0.5f, 0.5f, 0f)
				};
				standing.uv = new[]
				{
					new Vector2(0f, 0f),
					new Vector2(1f, 0f),
					new Vector2(1f, 1f),
					new Vector2(0f, 1f)
				};
				standing.triangles = new[] { 0, 2, 1, 0, 3, 2 };
				standing.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
			}
			return standing;
		}

		private static Mesh Quad()
		{
			// Rebuilt from native code (token 0x0600011D, RVA 0x271B520)
			if (!quad)
			{
				quad = new Mesh { name = "Fuse Glow Quad" };
				quad.vertices = new[]
				{
					new Vector3(-0.5f, 0f, -0.5f),
					new Vector3(0.5f, 0f, -0.5f),
					new Vector3(0.5f, 0f, 0.5f),
					new Vector3(-0.5f, 0f, 0.5f)
				};
				quad.uv = new[]
				{
					new Vector2(0f, 0f),
					new Vector2(1f, 0f),
					new Vector2(1f, 1f),
					new Vector2(0f, 1f)
				};
				quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
				quad.RecalculateBounds();
			}
			return quad;
		}
	}
}
