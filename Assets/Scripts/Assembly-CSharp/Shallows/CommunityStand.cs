using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class CommunityStand : MonoBehaviour
	{
		public enum Kind
		{
			Review = 0,
			Discord = 1
		}

		private class ScreenStars
		{
			private static readonly Vector2 Fill = Swatch(8f, 8f);

			private static readonly Vector2 Rim = Swatch(24f, 8f);

			private static readonly Vector2 Spark = Swatch(40f, 8f);

			private static readonly string[] Shape = new string[]
			{
				"......#......",
				".....###.....",
				".....###.....",
				"....#####....",
				"#############",
				".###########.",
				"..#########..",
				"...#######...",
				"...#######...",
				"..####.####..",
				"..###...###..",
				".###.....###.",
				".##.......##."
			};

			private const int Count = 5;

			private const float Radius = 0.036f;

			private const float Pixel = 0.0058f;

			private const float Edge = 0.042f;

			private const float BaseSpeed = 0.16f;

			private const int SparkPool = 30;

			private readonly Transform screen;

			private readonly float halfW;

			private readonly float halfH;

			private readonly Transform[] star = new Transform[Count];

			private readonly Vector2[] pos = new Vector2[Count];

			private readonly Vector2[] vel = new Vector2[Count];

			private readonly float[] spin = new float[Count];

			private readonly float[] angle = new float[Count];

			private readonly Transform[] spark = new Transform[SparkPool];

			private readonly Vector2[] sparkPos = new Vector2[SparkPool];

			private readonly Vector2[] sparkVel = new Vector2[SparkPool];

			private readonly float[] sparkLife = new float[SparkPool];

			private int nextSpark;

			private static Vector2 Swatch(float x, float y)
			{
				// Rebuilt from native code (token 0x0600040B, RVA 0x27A67E0)
				return new Vector2(x / 512f, 1f - y / 512f);
			}

			public ScreenStars(Transform owner, Vector3 centre, Vector3 right, Vector3 top, Material material)
			{
				// Rebuilt from native code (token 0x0600040C, RVA 0x27A4DEC)
				Vector3 across = right - centre;
				Vector3 up = top - centre;
				halfW = across.magnitude;
				halfH = up.magnitude;
				Vector3 normal = Vector3.Cross(up, across).normalized;
				screen = new GameObject("Screen Stars").transform;
				screen.SetParent(owner, true);
				screen.SetPositionAndRotation(centre + normal * 0.002f, Quaternion.LookRotation(-normal, up.normalized));
				Mesh starMesh = StarMesh();
				Mesh sparkMesh = Square(0.00464f, Spark);
				System.Random rng = new System.Random(owner.GetInstanceID());
				for (int i = 0; i < Count; i++)
				{
					star[i] = Make("Star " + (i + 1), starMesh, material, i * -0.0005f);
					pos[i] = new Vector2(Mathf.Lerp(Edge - halfW, halfW - Edge, (i + 0.5f) / Count), (halfH - Edge) * (float)(rng.NextDouble() * 2.0 - 1.0) * 0.7f);
					float heading = (float)(rng.NextDouble() * Mathf.PI * 2.0);
					vel[i] = new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * BaseSpeed * ((float)rng.NextDouble() * 0.4f + 0.8f);
					spin[i] = ((float)rng.NextDouble() * 2f - 1f) * 70f;
				}
				for (int i = 0; i < SparkPool; i++)
				{
					spark[i] = Make("Spark", sparkMesh, material, -0.003f);
					spark[i].gameObject.SetActive(false);
				}
				Place();
			}

			private Transform Make(string name, Mesh mesh, Material material, float z)
			{
				// Rebuilt from native code (token 0x0600040D, RVA 0x27A7358)
				GameObject go = new GameObject(name);
				go.transform.SetParent(screen, false);
				go.transform.localPosition = new Vector3(0f, 0f, z);
				go.AddComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = material;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
				meshRenderer.receiveShadows = false;
				return go.transform;
			}

			public void Kick()
			{
				// Rebuilt from native code (token 0x0600040E, RVA 0x27A59A8)
				for (int i = 0; i < Count; i++)
				{
					vel[i] *= 2.4f;
				}
			}

			public void Tick(float dt)
			{
				// Rebuilt from native code (token 0x0600040F, RVA 0x27A5EC4)
				float settle = 1f - MathF.Exp(dt * -0.9f);
				for (int i = 0; i < Count; i++)
				{
					float speed = vel[i].magnitude;
					if (speed > 0.0001f)
					{
						vel[i] *= Mathf.Lerp(speed, BaseSpeed, settle) / speed;
					}
					pos[i] += vel[i] * dt;
					spin[i] += (angle[i] * -30f + spin[i] * -2.5f) * dt;
					angle[i] += spin[i] * dt;
					float maxX = halfW - Edge;
					float maxY = halfH - Edge;
					if (pos[i].x > maxX)
					{
						pos[i].x = maxX;
						vel[i].x = -Mathf.Abs(vel[i].x);
						spin[i] += Wobble(vel[i]);
					}
					if (pos[i].x < -maxX)
					{
						pos[i].x = -maxX;
						vel[i].x = Mathf.Abs(vel[i].x);
						spin[i] += Wobble(vel[i]);
					}
					if (pos[i].y > maxY)
					{
						pos[i].y = maxY;
						vel[i].y = -Mathf.Abs(vel[i].y);
						spin[i] += Wobble(vel[i]);
					}
					if (pos[i].y < -maxY)
					{
						pos[i].y = -maxY;
						vel[i].y = Mathf.Abs(vel[i].y);
						spin[i] += Wobble(vel[i]);
					}
				}
				for (int a = 0; a < Count; a++)
				{
					for (int b = a + 1; b < Count; b++)
					{
						Vector2 gap = pos[b] - pos[a];
						float dist = gap.magnitude;
						if (dist >= Radius * 2f || dist < 1E-05f)
						{
							continue;
						}
						Vector2 n = gap / dist;
						Vector2 push = n * ((Radius * 2f - dist) * 0.5f);
						pos[a] -= push;
						pos[b] += push;
						float closing = Vector2.Dot(n, vel[a] - vel[b]);
						if (closing <= 0f)
						{
							continue;
						}
						vel[a] -= n * closing;
						vel[b] += n * closing;
						float kick = closing * 900f;
						spin[a] += kick;
						spin[b] -= kick;
						Burst(pos[a] + n * Radius);
					}
				}
				for (int i = 0; i < SparkPool; i++)
				{
					if (sparkLife[i] <= 0f)
					{
						continue;
					}
					sparkLife[i] -= dt;
					if (sparkLife[i] <= 0f)
					{
						spark[i].gameObject.SetActive(false);
						continue;
					}
					sparkPos[i] += sparkVel[i] * dt;
					sparkVel[i] *= 1f - dt * 3f;
				}
				Place();
			}

			private static float Wobble(Vector2 v)
			{
				// Rebuilt from native code (token 0x06000410, RVA 0x27A76E4)
				return v.x * v.y >= 0f ? 45f : -45f;
			}

			private void Burst(Vector2 at)
			{
				// Rebuilt from native code (token 0x06000411, RVA 0x27A7704)
				for (int k = 0; k < 6; k++)
				{
					int s = nextSpark;
					nextSpark = (nextSpark + 1) % SparkPool;
					float heading = k * Mathf.PI / 3f + Random.Range(-0.3f, 0.3f);
					sparkPos[s] = at;
					sparkVel[s] = new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * Random.Range(0.12f, 0.22f);
					sparkLife[s] = Random.Range(0.25f, 0.4f);
					spark[s].gameObject.SetActive(true);
				}
			}

			private void Place()
			{
				// Rebuilt from native code (token 0x06000412, RVA 0x27A74B0)
				for (int i = 0; i < Count; i++)
				{
					Transform t = star[i];
					t.localPosition = new Vector3(pos[i].x, pos[i].y, t.localPosition.z);
					t.localRotation = Quaternion.Euler(0f, 0f, angle[i]);
				}
				for (int i = 0; i < SparkPool; i++)
				{
					if (sparkLife[i] <= 0f)
					{
						continue;
					}
					Transform t = spark[i];
					Vector2 p = new Vector2(Mathf.Clamp(sparkPos[i].x, -halfW, halfW), Mathf.Clamp(sparkPos[i].y, -halfH, halfH));
					t.localPosition = new Vector3(p.x, p.y, t.localPosition.z);
					t.localScale = Vector3.one * Mathf.Clamp01(sparkLife[i] * 4f);
				}
			}

			private static Mesh StarMesh()
			{
				// Rebuilt from native code (token 0x06000413, RVA 0x27A67F8)
				int n = Shape.Length;
				List<Vector3> verts = new List<Vector3>();
				List<Vector2> uvs = new List<Vector2>();
				List<int> tris = new List<int>();
				for (int y = 0; y < n; y++)
				{
					float y0 = (n / 2f - y - 1f) * Pixel;
					float y1 = y0 + Pixel;
					for (int x = 0; x < n; x++)
					{
						if (!On(x, y))
						{
							continue;
						}
						Vector2 uv = !On(x - 1, y) || !On(x + 1, y) || !On(x, y - 1) || !On(x, y + 1) ? Rim : (y >= 5 && y <= 6 && x >= 5 && x <= 6 ? Spark : Fill);
						int first = verts.Count;
						float x0 = (x - n / 2f) * Pixel;
						float x1 = x0 + Pixel;
						verts.Add(new Vector3(x0, y0, 0f));
						verts.Add(new Vector3(x0, y1, 0f));
						verts.Add(new Vector3(x1, y1, 0f));
						verts.Add(new Vector3(x1, y0, 0f));
						for (int k = 0; k < 4; k++)
						{
							uvs.Add(uv);
						}
						tris.AddRange(new int[] { first, first + 1, first + 2, first, first + 2, first + 3 });
					}
				}
				Mesh mesh = new Mesh();
				mesh.name = "Screen Star";
				mesh.SetVertices(verts);
				mesh.SetUVs(0, uvs);
				mesh.SetTriangles(tris, 0);
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				return mesh;

				bool On(int x, int y)
				{
					// Rebuilt from native code (token 0x06000416, RVA 0x27A78BC)
					return x >= 0 && y >= 0 && x < n && y < n && Shape[y][x] == '#';
				}
			}

			private static Mesh Square(float size, Vector2 uv)
			{
				// Rebuilt from native code (token 0x06000414, RVA 0x27A6E7C)
				Mesh mesh = new Mesh();
				mesh.name = "Screen Spark";
				float h = size * 0.5f;
				mesh.SetVertices(new List<Vector3>
				{
					new Vector3(-h, -h, 0f),
					new Vector3(-h, h, 0f),
					new Vector3(h, h, 0f),
					new Vector3(h, -h, 0f)
				});
				mesh.SetUVs(0, new List<Vector2> { uv, uv, uv, uv });
				mesh.SetTriangles(new int[] { 0, 1, 2, 0, 2, 3 }, 0);
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				return mesh;
			}
		}

		public const string ReviewModel = "ReviewArcade";

		public const string DiscordModel = "DiscordBooth";

		public const string DiscordInvite = "https://discord.com/invite/ripple";

		private const string Folder = "Lifeguard/";

		public Kind kind;

		[Tooltip("The link the button opens. Empty = the store page (Review) or the Discord invite (Discord).")]
		public string link = "";

		private static readonly Dictionary<string, Material> glows = new Dictionary<string, Material>();

		private Renderer bulbsA;

		private Renderer bulbsB;

		private Material lit;

		private Material glow;

		private PokeButton button;

		private float nextOpen;

		private float nextChase;

		private float openAt = -1f;

		private Transform shaker;

		private float shakeStart = -10f;

		private const float ShakeTime = 0.42f;

		private bool chaseFlip;

		private ScreenStars stars;

		public static string StorePage
		{
			get
			{
				// Rebuilt from native code (token 0x060003FC, RVA 0x27A28C8)
				return "https://www.meta.com/experiences/1343442268863054/";
			}
		}

		public string Model
		{
			get
			{
				// Rebuilt from native code (token 0x060003FD, RVA 0x27A2908)
				return kind == Kind.Review ? ReviewModel : DiscordModel;
			}
		}

		public string Link
		{
			get
			{
				// Rebuilt from native code (token 0x060003FE, RVA 0x27A2974)
				if (!string.IsNullOrEmpty(link))
				{
					return link;
				}
				return kind == Kind.Review ? StorePage : DiscordInvite;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060003FF, RVA 0x27A2A2C)
			glows.Clear();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void EnsureBoth()
		{
			// Rebuilt from native code (token 0x06000400, RVA 0x27A2AA4)
			GameManager game = FindAnyObjectByType<GameManager>();
			if (!game)
			{
				return;
			}
			bool review = false;
			bool discord = false;
			foreach (CommunityStand stand in FindObjectsByType<CommunityStand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				review |= stand.kind == Kind.Review;
				discord |= stand.kind != Kind.Review;
			}
			if (!review)
			{
				Place(game, Kind.Review);
			}
			if (!discord)
			{
				Place(game, Kind.Discord);
			}
		}

		public static CommunityStand Place(GameManager game, Kind kind)
		{
			// Rebuilt from native code (token 0x06000401, RVA 0x27A2C24)
			Bounds zone = game.safeZone;
			Vector3 from = new Vector3(zone.min.x + (kind == Kind.Review ? 1.7f : 3.1f), zone.center.y + 1f, zone.min.z + 0.34f);
			float floor = Physics.Raycast(from, Vector3.down, out RaycastHit hit, 6f, -1, QueryTriggerInteraction.Ignore) ? hit.point.y : 0.38f;
			GameObject go = new GameObject(kind == Kind.Review ? "Review Stand" : "Discord Stand");
			go.transform.SetPositionAndRotation(new Vector3(from.x, floor, from.z), Quaternion.Euler(0f, 180f, 0f));
			CommunityStand stand = go.AddComponent<CommunityStand>();
			stand.kind = kind;
			stand.WearModel();
			return stand;
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x06000402, RVA 0x27A2E80)
			Transform model = LifeguardProps.Dress(transform, Model, Vector3.zero, Quaternion.identity);
			if (!model)
			{
				return null;
			}
			Material glowMaterial = GlowMaterial(Model);
			foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>(true))
			{
				if ((r.name.StartsWith("Glow") || r.name == "Discord Button") && r.sharedMaterial != glowMaterial)
				{
					r.sharedMaterial = glowMaterial;
				}
			}
			foreach (MeshFilter f in model.GetComponentsInChildren<MeshFilter>(true))
			{
				if (!Collides(f.name) || !f.sharedMesh)
				{
					continue;
				}
				foreach (BoxCollider box in f.GetComponents<BoxCollider>())
				{
					if (!box.isTrigger)
					{
						if (Application.isPlaying)
						{
							Destroy(box);
						}
						else
						{
							DestroyImmediate(box);
						}
					}
				}
				MeshCollider meshCollider = f.GetComponent<MeshCollider>();
				if (!meshCollider)
				{
					meshCollider = f.gameObject.AddComponent<MeshCollider>();
				}
				if (meshCollider.sharedMesh != f.sharedMesh)
				{
					meshCollider.sharedMesh = f.sharedMesh;
				}
			}
			return model;
		}

		private static bool Collides(string part)
		{
			// Rebuilt from native code (token 0x06000403, RVA 0x27A4074)
			return part == "Body" || part == "Top Cap" || part.StartsWith("Side Panel");
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000404, RVA 0x27A4128)
			Transform model = WearModel();
			if (!model)
			{
				return;
			}
			shaker = model.parent;
			glow = GlowMaterial(Model);
			lit = LifeguardProps.MaterialFor(Model, null);
			Transform press = LifeguardProps.Find(model, kind == Kind.Review ? "Star Button" : "Discord Button");
			if (press)
			{
				Transform pressed = LifeguardProps.Find(model, "Button Press");
				Vector3 push = pressed ? (pressed.position - press.position).normalized * 0.014f : transform.up * -0.014f;
				button = LifeguardProps.Button(press, push);
				button.repeat = false;
				button.quiet = true;
				if (kind == Kind.Discord)
				{
					button.pressedColor = new Color(0.72f, 0.76f, 1f, 1f);
				}
				button.onPress += Open;
			}
			if (kind != Kind.Review)
			{
				return;
			}
			Transform bulbs = LifeguardProps.Find(model, "Glow Bulbs A");
			Transform bulbs2 = LifeguardProps.Find(model, "Glow Bulbs B");
			bulbsA = bulbs ? bulbs.GetComponent<Renderer>() : null;
			bulbsB = bulbs2 ? bulbs2.GetComponent<Renderer>() : null;
			Transform centre = LifeguardProps.Find(model, "Screen Center");
			Transform right = LifeguardProps.Find(model, "Screen Right");
			Transform top = LifeguardProps.Find(model, "Screen Top");
			if (!centre || !right || !top)
			{
				return;
			}
			stars = new ScreenStars(model, centre.position, right.position, top.position, glow);
		}

		private void Open()
		{
			// Rebuilt from native code (token 0x06000405, RVA 0x27A56E8)
			AudioClip clip = kind == Kind.Review ? Sfx.ReviewPress() : Sfx.DiscordPress();
			Vector3 at = button ? button.transform.position : transform.position + Vector3.up * 0.45f;
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(clip, at, 0.9f, 1f);
			}
			shakeStart = Time.unscaledTime;
			stars?.Kick();
			if (Time.unscaledTime < nextOpen)
			{
				return;
			}
			nextOpen = Time.unscaledTime + 2.5f;
			openAt = Time.unscaledTime + 0.45f;
		}

		private void Shake()
		{
			// Rebuilt from native code (token 0x06000406, RVA 0x27A59FC)
			if (!shaker)
			{
				return;
			}
			float t = Time.unscaledTime - shakeStart;
			if (t > 0.52f)
			{
				return;
			}
			float fade = t >= ShakeTime ? 0f : 1f - t / ShakeTime;
			float k = fade * fade;
			shaker.localRotation = Quaternion.Euler(0f, 0f, MathF.Sin(t * 52f) * 1.6f * k);
			float bob = Mathf.Abs(MathF.Sin(t * 26f)) * 0.008f;
			shaker.localPosition = new Vector3(MathF.Sin(t * 37f) * 0.004f, bob, 0f) * k;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000407, RVA 0x27A5B80)
			Shake();
			if (openAt >= 0f && Time.unscaledTime >= openAt)
			{
				openAt = -1f;
				Debug.Log("Shallows: " + name + " opens " + Link);
				Application.OpenURL(Link);
			}
			PlayerRig rig = PlayerRig.I;
			if (rig && rig.head && (rig.head.position - transform.position).sqrMagnitude >= 324f)
			{
				return;
			}
			if (bulbsA && bulbsB && Time.unscaledTime >= nextChase)
			{
				chaseFlip = !chaseFlip;
				nextChase = Time.unscaledTime + 0.32f;
				bulbsA.sharedMaterial = chaseFlip ? glow : lit;
				bulbsB.sharedMaterial = chaseFlip ? lit : glow;
			}
			stars?.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
		}

		private static Material GlowMaterial(string model)
		{
			// Rebuilt from native code (token 0x06000408, RVA 0x27A3D8C)
			if (glows.TryGetValue(model, out Material material) && material)
			{
				return material;
			}
			string glowName = model + " Glow";
			material = Resources.Load<Material>(Folder + glowName);
			if (!material)
			{
				material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
				material.name = glowName;
				Texture2D albedo = Resources.Load<Texture2D>(Folder + model + "_Albedo");
				material.SetTexture("_BaseMap", albedo);
				material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
				material.mainTexture = albedo;
			}
			glows[model] = material;
			return material;
		}
	}
}
