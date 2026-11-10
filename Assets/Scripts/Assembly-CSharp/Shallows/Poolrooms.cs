using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Shallows
{
	[ExecuteAlways]
	[DefaultExecutionOrder(-30)]
	public class Poolrooms : MonoBehaviour
	{
		internal class Room
		{
			public int id;

			public string name;

			public Transform hall;

			public WaterVolume water;

			public float x0;

			public float x1;

			public float left;

			public float right;

			// Rebuilt from native code (token 0x060004E5, RVA 0x27C54DC)
			public float halfWidth = 32f;

			public float ceiling = 16f;

			public float deckTop = 0.38f;

			public float poolStart;

			public float poolEnd;

			public bool dark;

			public Transform ceilingObject;

			public float ceilingTop = 16.5f;

			public Rect loftHole;

			public float loftSide;

			public float loftU0;

			public float loftFloor;

			public float loftLadderX;

			public readonly List<PoolroomsProps.ShellBox> loftShell = new List<PoolroomsProps.ShellBox>();

			public readonly List<Vector3> loftLamps = new List<Vector3>();

			public readonly List<Panel> panels = new List<Panel>();

			public Look look;
		}

		internal class Panel
		{
			public Vector3 centre;

			public Vector2 half;

			public int state;
		}

		internal struct Look
		{
			public Color tile;

			public Color fog;

			public Color lamp;

			public Color water;

			public float wear;

			public int flicker;

			public int dead;

			public Look(Color tile, float wear, Color fog, Color lamp, Color water, int flicker, int dead)
			{
				// Rebuilt from native code (token 0x060004E7, RVA 0x27C5634)
				this.tile = tile;
				this.wear = wear;
				this.fog = fog;
				this.lamp = lamp;
				this.water = water;
				this.flicker = flicker;
				this.dead = dead;
			}
		}

		private class Flicker
		{
			public System.Random rng;

			// Rebuilt from native code (token 0x060004E8, RVA 0x27C565C)
			public float value = 1f;

			public float next;

			public float burstEnd;

			public float toggle;

			public bool on = true;

			public readonly List<AudioSource> hums = new List<AudioSource>();
		}

		// Rebuilt from native code (token 0x060004E1, RVA 0x27C4A10)
		public static bool Enabled = true;

		// Rebuilt from native code (token 0x060004E0, RVA 0x27C4770)
		[Header("Light")]
		[Tooltip("How much of the old flat light stays, now that the ceiling lamps light the rooms.")]
		[Range(0f, 1.5f)]
		public float flatLight = 0.8f;

		[Tooltip("How bright the ceiling lamps' light is on the tiles.")]
		[Range(0f, 2f)]
		public float lampStrength = 0.6f;

		[Tooltip("How many steps the lamp light is drawn in (fewer = chunkier pixel bands).")]
		[Range(1f, 16f)]
		public int lightBands = 7;

		[Tooltip("Wear on top of each level's own amount.")]
		[Range(0f, 1.5f)]
		public float wear = 1f;

		[Header("Flicker")]
		[Tooltip("How loud a flickering lamp buzzes, right under it.")]
		[Range(0f, 1f)]
		public float humVolume = 0.14f;

		[Header("Detail")]
		public bool props = true;

		private static readonly Color KeepWater = C(0f, 0f, 0f, 0f);

		private static readonly Look[] Looks = new Look[]
		{
			new Look(C(1f, 0.97f, 0.9f), 0.3f, C(0.83f, 0.8f, 0.69f), C(1f, 0.93f, 0.78f), KeepWater, 0, 0),
			new Look(C(0.8f, 0.98f, 0.95f), 0.45f, C(0.71f, 0.82f, 0.76f), C(1f, 0.95f, 0.84f), C(0.16f, 0.68f, 0.74f), 1, 1),
			new Look(C(1f, 0.92f, 0.68f), 0.6f, C(0.83f, 0.79f, 0.58f), C(1f, 0.9f, 0.66f), C(0.3f, 0.62f, 0.56f), 2, 2),
			new Look(C(0.8f, 0.94f, 0.64f), 0.7f, C(0.7f, 0.77f, 0.55f), C(0.92f, 1f, 0.72f), C(0.3f, 0.58f, 0.34f), 2, 2),
			new Look(C(1f, 0.8f, 0.86f), 0.6f, C(0.85f, 0.73f, 0.71f), C(1f, 0.86f, 0.86f), C(0.5f, 0.5f, 0.78f), 1, 2),
			new Look(C(0.66f, 0.78f, 1f), 0.7f, C(0.58f, 0.65f, 0.73f), C(0.84f, 0.9f, 1f), C(0.1f, 0.32f, 0.76f), 2, 2),
			new Look(C(0.84f, 0.76f, 1f), 0.75f, C(0.68f, 0.64f, 0.73f), C(0.92f, 0.86f, 1f), C(0.32f, 0.36f, 0.78f), 2, 3),
			new Look(C(0.86f, 1f, 0.9f), 0.6f, C(0.75f, 0.84f, 0.74f), C(0.94f, 1f, 0.94f), C(0.22f, 0.74f, 0.64f), 1, 2),
			new Look(C(0.82f, 0.85f, 0.88f), 1f, C(0.02f, 0.02f, 0.03f), C(0f, 0f, 0f), KeepWater, 0, 0),
			new Look(C(1f, 0.8f, 0.68f), 0.9f, C(0.75f, 0.6f, 0.48f), C(1f, 0.76f, 0.56f), C(0.34f, 0.46f, 0.46f), 4, 3)
		};

		private static readonly Look DarkLook = Looks[8];

		private const int Outside = 15;

		private const int MaxRooms = 16;

		private const float LightTexel = 0.5f;

		private const float LevelTexel = 0.2f;

		private static readonly int DressedId = Shader.PropertyToID("_ShallowsDressed");

		private static readonly int MapRectId = Shader.PropertyToID("_ShallowsMapRect");

		private static readonly int LevelMapId = Shader.PropertyToID("_ShallowsLevelMap");

		private static readonly int LightMapId = Shader.PropertyToID("_ShallowsLightMap");

		private static readonly int TintId = Shader.PropertyToID("_ShallowsLevelTint");

		private static readonly int LampId = Shader.PropertyToID("_ShallowsLevelLamp");

		private static readonly int InfoId = Shader.PropertyToID("_ShallowsLevelInfo");

		private static readonly int FogRangeId = Shader.PropertyToID("_ShallowsLevelFogRange");

		private static readonly int SpanId = Shader.PropertyToID("_ShallowsLevelSpan");

		private static readonly int WaterColorId = Shader.PropertyToID("_WaterColor");

		private readonly List<Room> rooms = new List<Room>();

		private readonly Vector4[] tints = new Vector4[MaxRooms];

		private readonly Vector4[] spans = new Vector4[MaxRooms];

		private readonly Vector4[] lamps = new Vector4[MaxRooms];

		private readonly Vector4[] infos = new Vector4[MaxRooms];

		private readonly Vector4[] fogRanges = new Vector4[MaxRooms];

		private readonly Flicker[] flickers = new Flicker[MaxRooms];

		private readonly List<UnityEngine.Object> made = new List<UnityEngine.Object>();

		private readonly List<Renderer> buried = new List<Renderer>();

		private readonly List<Renderer> cutCeilings = new List<Renderer>();

		private readonly List<Collider> cutColliders = new List<Collider>();

		private Texture2D levelMap;

		private Texture2D lightMap;

		private Transform propsRoot;

		private Transform backdrop;

		private AtmosphereZone outsideZone;

		private Camera view;

		private Color fogNow;

		private bool fogSet;

		private const float LadderStep = 0.3f;

		private const float LadderSection = 0.9f;

		private const float LadderWall = 0.14f;

		private const float LadderHalf = 0.22f;

		public static Poolrooms I { get; private set; }

		private static Color C(float r, float g, float b, float a = 1f)
		{
			// Rebuilt from native code (token 0x060004BE, RVA 0x27BAAB4)
			return new Color(r, g, b, a);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060004BF, RVA 0x27BAAB8)
			I = null;
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x060004C0, RVA 0x27BABC8)
			SceneManager.sceneLoaded += OnSceneLoaded;
			Ensure(false);
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// Rebuilt from native code (token 0x060004C1, RVA 0x27BAEDC)
			Ensure(false);
		}

		public static Poolrooms Ensure(bool editorOnly)
		{
			// Rebuilt from native code (token 0x060004C2, RVA 0x27BAC94)
			if (I || !Enabled)
			{
				return I;
			}
			if (FindRooms(new List<Room>()) == 0)
			{
				return null;
			}
			GameObject go = new GameObject(editorOnly ? "Poolrooms (look only, not saved)" : "Poolrooms");
			if (editorOnly)
			{
				go.hideFlags = HideFlags.DontSave;
			}
			return go.AddComponent<Poolrooms>();
		}

		public static void Remove()
		{
			// Rebuilt from native code (token 0x060004C3, RVA 0x27BC010)
			if (!I)
			{
				return;
			}
			GameObject go = I.gameObject;
			if (Application.isPlaying)
			{
				Destroy(go);
			}
			else
			{
				DestroyImmediate(go);
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060004C4, RVA 0x27BC1B4)
			if (I && I != this)
			{
				enabled = false;
				return;
			}
			I = this;
			Dress();
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060004C5, RVA 0x27BCBAC)
			if (I == this)
			{
				Undress();
				I = null;
			}
		}

		private void OnValidate()
		{
			// Rebuilt from native code (token 0x060004C6, RVA 0x27BD3B4)
			if (I == this && isActiveAndEnabled)
			{
				PushGlobals();
			}
		}

		private void Dress()
		{
			// Rebuilt from native code (token 0x060004C7, RVA 0x27BC36C)
			rooms.Clear();
			if (FindRooms(rooms, buried) == 0)
			{
				return;
			}
			foreach (Renderer r in buried)
			{
				r.forceRenderingOff = true;
			}
			foreach (Room room in rooms)
			{
				room.look = room.dark ? DarkLook : Looks[room.id == 0 ? 0 : (room.id - 1) % (Looks.Length - 1) + 1];
				PickBrokenLamps(room);
				if (props)
				{
					PoolroomsProps.PlanLoft(room);
				}
			}
			BuildMaps();
			TintWater(true);
			GameObject root = new GameObject("Poolrooms Props");
			root.hideFlags = HideFlags.DontSave;
			propsRoot = root.transform;
			for (int i = 0; i < MaxRooms; i++)
			{
				flickers[i] = null;
			}
			foreach (Room room in rooms)
			{
				if (!room.panels.Exists(p => p.state == 1))
				{
					continue;
				}
				Flicker f = flickers[room.id] = new Flicker { rng = new System.Random(77 + room.id * 13) };
				f.next = (float)f.rng.NextDouble() * 4f + 1f;
				if (Application.isPlaying)
				{
					AddHums(room, f);
				}
			}
			if (props)
			{
				BuildProps();
			}
			if (Application.isPlaying)
			{
				BuildBackdrop();
			}
			PushGlobals();
		}

		private void Undress()
		{
			// Rebuilt from native code (token 0x060004C8, RVA 0x27BCCE0)
			Shader.SetGlobalVector(DressedId, Vector4.zero);
			TintWater(false);
			if (propsRoot)
			{
				Kill(propsRoot.gameObject);
			}
			propsRoot = null;
			foreach (UnityEngine.Object o in made)
			{
				Kill(o);
			}
			made.Clear();
			lightMap = null;
			levelMap = null;
			foreach (Renderer r in buried)
			{
				if (r)
				{
					r.forceRenderingOff = false;
				}
			}
			buried.Clear();
			foreach (Renderer r in cutCeilings)
			{
				if (r)
				{
					r.forceRenderingOff = false;
				}
			}
			cutCeilings.Clear();
			foreach (Collider c in cutColliders)
			{
				if (c)
				{
					c.enabled = true;
				}
			}
			cutColliders.Clear();
			for (int i = 0; i < MaxRooms; i++)
			{
				flickers[i] = null;
			}
		}

		private static void Kill(UnityEngine.Object o)
		{
			// Rebuilt from native code (token 0x060004C9, RVA 0x27C017C)
			if (!o)
			{
				return;
			}
			if (Application.isPlaying)
			{
				Destroy(o);
			}
			else
			{
				DestroyImmediate(o);
			}
		}

		private T Made<T>(T o) where T : UnityEngine.Object
		{
			// Rebuilt from native code (token 0x060004CA, shared instantiation RVA 0x2E1FA74)
			o.hideFlags = HideFlags.DontSave;
			made.Add(o);
			return o;
		}

		internal static int FindRooms(List<Room> found, List<Renderer> buried = null)
		{
			// Rebuilt from native code (token 0x060004CB, RVA 0x27BAF2C)
			Atmosphere atmosphere = FindAnyObjectByType<Atmosphere>();
			foreach (WaterVolume water in FindObjectsByType<WaterVolume>(FindObjectsSortMode.None))
			{
				Transform hall = water.transform.parent;
				if (!hall || !hall.name.StartsWith("Level "))
				{
					continue;
				}
				int dash = hall.name.IndexOf(" - ", StringComparison.Ordinal);
				if (dash < 0 || !int.TryParse(hall.name.Substring(6, dash - 6), out int level) || level < 1)
				{
					continue;
				}
				if (found.Exists(r => r.hall == hall))
				{
					continue;
				}
				Vector3 position = water.transform.position;
				Room room = new Room
				{
					id = Mathf.Min(level, 14),
					name = hall.name.Substring(dash + 3),
					hall = hall,
					water = water,
					poolStart = position.x - water.size.x * 0.5f,
					poolEnd = position.x + water.size.x * 0.5f,
					halfWidth = water.size.y * 0.5f
				};
				room.x0 = FindBounds(hall, "Start Deck (checkpoint)", out Bounds start) ? start.min.x : room.poolStart - 5f;
				if (start.size.y > 0f)
				{
					room.deckTop = start.max.y;
				}
				room.x1 = FindBounds(hall, "Exit Deck", out Bounds exit) ? exit.max.x : room.poolEnd + 3f;
				if (FindBounds(hall, "Ceiling", out Bounds ceiling))
				{
					room.ceiling = ceiling.min.y;
					room.ceilingTop = ceiling.max.y;
					room.ceilingObject = hall.Find("Ceiling");
				}
				room.dark = room.name.Contains("Lights Out");
				if (atmosphere && atmosphere.zones != null)
				{
					foreach (AtmosphereZone zone in atmosphere.zones)
					{
						if (zone.darkness > 0.9f && zone.bounds.Contains(new Vector3(position.x, zone.bounds.center.y, 0f)))
						{
							room.dark = true;
						}
					}
				}
				found.Add(room);
			}
			if (found.Count == 0)
			{
				return 0;
			}
			GameObject spawn = GameObject.Find("Spawn Room");
			if (spawn && FindBounds(spawn.transform, "Floor", out Bounds floor))
			{
				Room room = new Room
				{
					id = 0,
					name = "Spawn Room",
					hall = spawn.transform,
					halfWidth = floor.extents.z,
					x0 = floor.min.x,
					x1 = floor.max.x,
					poolStart = float.NaN,
					poolEnd = float.NaN
				};
				if (FindBounds(spawn.transform, "Ceiling", out Bounds ceiling))
				{
					room.ceiling = ceiling.min.y;
				}
				found.Add(room);
			}
			found.Sort((a, b) => a.x0.CompareTo(b.x0));
			for (int i = 0; i < found.Count; i++)
			{
				found[i].left = i == 0 ? found[0].x0 - 1f : (found[i - 1].x1 + found[i].x0) * 0.5f;
				found[i].right = i < found.Count - 1 ? (found[i].x1 + found[i + 1].x0) * 0.5f : found[i].x1 + 0.3f;
			}
			MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
			List<MeshRenderer> pillars = new List<MeshRenderer>();
			foreach (MeshRenderer r in renderers)
			{
				if (r.name.IndexOf("Pillar", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					pillars.Add(r);
				}
			}
			foreach (MeshRenderer r in renderers)
			{
				if (r.name != "Light Panel" || !r.sharedMaterial || r.sharedMaterial.shader.name != "Shallows/Glow")
				{
					continue;
				}
				Bounds b = r.bounds;
				Room room = found.Find(x => b.center.x >= x.left && b.center.x < x.right);
				if (room == null || room.dark)
				{
					continue;
				}
				if (Buried(b, pillars))
				{
					buried?.Add(r);
					continue;
				}
				room.panels.Add(new Panel
				{
					centre = b.center,
					half = new Vector2(b.extents.x, b.extents.z)
				});
			}
			foreach (Room room in found)
			{
				room.panels.Sort((a, b) => a.centre.x != b.centre.x ? a.centre.x.CompareTo(b.centre.x) : a.centre.z.CompareTo(b.centre.z));
			}
			return found.Count;
		}

		private static bool Buried(Bounds lamp, List<MeshRenderer> pillars)
		{
			// Rebuilt from native code (token 0x060004CC, RVA 0x27C038C)
			foreach (MeshRenderer pillar in pillars)
			{
				Bounds b = pillar.bounds;
				if (b.min.y > lamp.center.y || b.max.y < lamp.center.y - 0.5f)
				{
					continue;
				}
				if (b.extents.x > 5f || b.extents.z > 5f)
				{
					continue;
				}
				float dx = Mathf.Max(0f, Mathf.Abs(b.center.x - lamp.center.x) - lamp.extents.x);
				float dz = Mathf.Max(0f, Mathf.Abs(b.center.z - lamp.center.z) - lamp.extents.z);
				Mesh mesh = pillar.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
				if (mesh && mesh.name.StartsWith("Cylinder"))
				{
					if (dx * dx + dz * dz < b.extents.x * b.extents.z)
					{
						return true;
					}
				}
				else if (dx < b.extents.x && dz < b.extents.z)
				{
					return true;
				}
			}
			Collider[] hits = Physics.OverlapBox(new Vector3(lamp.center.x, lamp.center.y - 0.4f, lamp.center.z), new Vector3(Mathf.Max(lamp.extents.x - 0.05f, 0.05f), 0.25f, Mathf.Max(lamp.extents.z - 0.05f, 0.05f)), Quaternion.identity, -1, QueryTriggerInteraction.Ignore);
			foreach (Collider hit in hits)
			{
				if (hit.transform.root.name != "Poolrooms Props")
				{
					return true;
				}
			}
			return false;
		}

		private static bool FindBounds(Transform parent, string child, out Bounds bounds)
		{
			// Rebuilt from native code (token 0x060004CD, RVA 0x27C0250)
			Transform t = parent.Find(child);
			Renderer r = t ? t.GetComponent<Renderer>() : null;
			bounds = r ? r.bounds : default(Bounds);
			return r;
		}

		private static void PickBrokenLamps(Room room)
		{
			// Rebuilt from native code (token 0x060004CE, RVA 0x27BDA30)
			System.Random rng = new System.Random(room.id * 31 + 4242);
			List<Panel> pool = new List<Panel>(room.panels);
			for (int i = 0; i < room.look.dead + room.look.flicker; i++)
			{
				if (pool.Count <= 3)
				{
					break;
				}
				int pick = rng.Next(pool.Count);
				pool[pick].state = i < room.look.flicker ? 1 : 2;
				pool.RemoveAt(pick);
			}
		}

		private void BuildMaps()
		{
			// Rebuilt from native code (token 0x060004CF, RVA 0x27BDBB8)
			float minLeft = float.MaxValue;
			float maxRight = float.MinValue;
			float maxHalfWidth = 1f;
			foreach (Room room in rooms)
			{
				minLeft = Mathf.Min(minLeft, room.left);
				maxRight = Mathf.Max(maxRight, room.right);
				maxHalfWidth = Mathf.Max(maxHalfWidth, room.halfWidth);
			}
			float minX = minLeft - 2f;
			float span = maxRight + 2f - minX;
			float texel = Mathf.Max(LightTexel, span / 4096f);
			int width = Mathf.CeilToInt(span / texel);
			float halfZ = maxHalfWidth + 1f;
			int height = Mathf.CeilToInt(halfZ * 2f / texel);
			float mapWidth = texel * width;

			int levelWidth = Mathf.Clamp(Mathf.CeilToInt(mapWidth / LevelTexel), 16, 8192);
			levelMap = Made(new Texture2D(levelWidth, 1, TextureFormat.R8, false, true)
			{
				name = "Poolrooms Level Map",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp
			});
			byte[] levels = new byte[levelWidth];
			for (int i = 0; i < levelWidth; i++)
			{
				float x = minX + mapWidth * (i + 0.5f) / levelWidth;
				Room room = rooms.Find(r => x >= r.left && x < r.right);
				levels[i] = Encode(room != null ? room.id : Outside);
			}
			levelMap.SetPixelData(levels, 0);
			levelMap.Apply(false, !Application.isEditor);

			float[] glow = new float[width * height];
			float[] light = new float[width * height];
			float[] flickerGlow = new float[width * height];
			float[] flickerLight = new float[width * height];
			foreach (Room room in rooms)
			{
				float reach = Mathf.Clamp(room.ceiling * 0.22f, 1.2f, 3.5f);
				int col0 = Mathf.Max(0, Mathf.FloorToInt((room.left - minX) / texel));
				int col1 = Mathf.Min(width - 1, Mathf.CeilToInt((room.right - minX) / texel));
				foreach (Panel p in room.panels)
				{
					if (p.state == 2)
					{
						continue;
					}
					float glowRange = Mathf.Max(p.half.x, p.half.y) * 2.2f;
					float[] glowTarget = p.state == 1 ? flickerGlow : glow;
					float[] lightTarget = p.state == 1 ? flickerLight : light;
					float radius = reach * 5f;
					int c0 = Mathf.Max(col0, Mathf.FloorToInt((p.centre.x - radius - minX) / texel));
					int c1 = Mathf.Min(col1, Mathf.CeilToInt((p.centre.x + radius - minX) / texel));
					int r0 = Mathf.Max(0, Mathf.FloorToInt((p.centre.z - radius + halfZ) / texel));
					int r1 = Mathf.Min(height - 1, Mathf.CeilToInt((p.centre.z + radius + halfZ) / texel));
					for (int row = r0; row <= r1; row++)
					{
						float z = texel * (row + 0.5f) - halfZ;
						float dz = z - p.centre.z;
						for (int col = c0; col <= c1; col++)
						{
							float x = minX + texel * (col + 0.5f);
							if (x < room.left || x >= room.right)
							{
								continue;
							}
							int index = row * width + col;
							float ex = Mathf.Max(Mathf.Abs(x - p.centre.x) - p.half.x, 0f);
							float ez = Mathf.Max(Mathf.Abs(dz) - p.half.y, 0f);
							float t = Mathf.Clamp01(1f - Mathf.Sqrt(ex * ex + ez * ez) / glowRange);
							glowTarget[index] += t * t;
							float dx = x - p.centre.x;
							float d = Mathf.Sqrt(dz * dz + dx * dx) / reach;
							if (d < 5f)
							{
								lightTarget[index] += (1f - d / 5f) * (1f / (d * d + 1f));
							}
						}
					}
				}
				int z0 = Mathf.Max(0, Mathf.FloorToInt((halfZ - room.halfWidth) / texel));
				int z1 = Mathf.Min(height - 1, Mathf.FloorToInt((halfZ + room.halfWidth) / texel));
				if (z0 > z1)
				{
					continue;
				}
				double sum = 0.0;
				int cells = 0;
				for (int row = z0; row <= z1; row++)
				{
					for (int col = col0; col <= col1; col++)
					{
						int index = row * width + col;
						sum += light[index] + flickerLight[index];
						cells++;
					}
				}
				if (sum <= 0.0001)
				{
					continue;
				}
				float scale = (float)(cells * 0.75 / sum);
				for (int row = 0; row < height; row++)
				{
					for (int col = col0; col <= col1; col++)
					{
						int index = row * width + col;
						light[index] *= scale;
						flickerLight[index] *= scale;
					}
				}
			}

			lightMap = Made(new Texture2D(width, height, TextureFormat.RGBA32, false, true)
			{
				name = "Poolrooms Light Map",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			});
			Color32[] pixels = new Color32[width * height];
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = new Color32(Half(glow[i]), Half(light[i]), Half(flickerGlow[i]), Half(flickerLight[i]));
			}
			lightMap.SetPixels32(pixels);
			lightMap.Apply(false, !Application.isEditor);
			Shader.SetGlobalTexture(LevelMapId, levelMap);
			Shader.SetGlobalTexture(LightMapId, lightMap);
			Shader.SetGlobalVector(MapRectId, new Vector4(minX, -halfZ, 1f / mapWidth, 1f / (texel * height)));
		}

		private static byte Encode(int id)
		{
			// Rebuilt from native code (token 0x060004D0, RVA 0x27C0820)
			return (byte)Mathf.RoundToInt((id + 0.5f) / MaxRooms * 255f);
		}

		private static byte Half(float v)
		{
			// Rebuilt from native code (token 0x060004D1, RVA 0x27C0918)
			return (byte)Mathf.RoundToInt(Mathf.Clamp01(v * 0.5f) * 255f);
		}

		private void PushGlobals()
		{
			// Rebuilt from native code (token 0x060004D2, RVA 0x27BD49C)
			if (!levelMap)
			{
				return;
			}
			for (int i = 0; i < MaxRooms; i++)
			{
				tints[i] = new Vector4(1f, 1f, 1f, 0f);
				spans[i] = Vector4.zero;
				lamps[i] = new Vector4(0f, 0f, 0f, 1f);
				infos[i] = new Vector4(16f, 1f, 0f, 0f);
				fogRanges[i] = Vector4.zero;
			}
			foreach (Room room in rooms)
			{
				tints[room.id] = new Vector4(room.look.tile.r, room.look.tile.g, room.look.tile.b, room.look.wear * wear);
				spans[room.id] = new Vector4(room.left, room.right, 0f, 0f);
				lamps[room.id] = new Vector4(room.look.lamp.r, room.look.lamp.g, room.look.lamp.b, Flick(room.id));
				infos[room.id] = new Vector4(room.ceiling, flatLight, room.dark ? 1f : 0f, 0f);
			}
			float right = float.MinValue;
			foreach (Room room in rooms)
			{
				right = Mathf.Max(right, room.right);
			}
			spans[Outside] = new Vector4(right, 10000000f, 0f, 0f);
			outsideZone = OutsideZone();
			if (outsideZone != null)
			{
				fogRanges[Outside] = new Vector4(outsideZone.fogStart, outsideZone.fogEnd, 1f, 0f);
			}
			Shader.SetGlobalVectorArray(FogRangeId, fogRanges);
			Shader.SetGlobalVectorArray(SpanId, spans);
			Shader.SetGlobalVectorArray(TintId, tints);
			Shader.SetGlobalVectorArray(LampId, lamps);
			Shader.SetGlobalVectorArray(InfoId, infos);
			Shader.SetGlobalVector(DressedId, new Vector4(1f, flatLight, lampStrength, lightBands));
		}

		private float Flick(int id)
		{
			// Rebuilt from native code (token 0x060004D3, RVA 0x27C0A18)
			return flickers[id] == null ? 1f : flickers[id].value;
		}

		private AtmosphereZone OutsideZone()
		{
			// Rebuilt from native code (token 0x060004D4, RVA 0x27C0A58)
			Atmosphere atmosphere = Atmosphere.I ? Atmosphere.I : FindAnyObjectByType<Atmosphere>();
			if (!atmosphere)
			{
				return null;
			}
			if (atmosphere.zones != null)
			{
				foreach (AtmosphereZone zone in atmosphere.zones)
				{
					float x = zone.bounds.center.x;
					if (zone.darkness < 0.5f && !rooms.Exists(r => x >= r.left && x < r.right))
					{
						return zone;
					}
				}
			}
			return null;
		}

		private void TintWater(bool on)
		{
			// Rebuilt from native code (token 0x060004D5, RVA 0x27BEDFC)
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			foreach (Room room in rooms)
			{
				if (!room.water)
				{
					continue;
				}
				foreach (MeshRenderer r in room.water.GetComponentsInChildren<MeshRenderer>())
				{
					if (!r.sharedMaterial || !r.sharedMaterial.HasProperty(WaterColorId))
					{
						continue;
					}
					block.Clear();
					if (on && room.look.water.a > 0f)
					{
						Color water = room.look.water;
						block.SetColor(WaterColorId, new Color(water.r, water.g, water.b, r.sharedMaterial.GetColor(WaterColorId).a));
					}
					r.SetPropertyBlock(block);
				}
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060004D6, RVA 0x27C0CF0)
			if (!levelMap)
			{
				return;
			}
			float now = Application.isPlaying ? Time.time : 0f;
			Transform head = PlayerRig.I ? PlayerRig.I.head : null;
			for (int i = 0; i < MaxRooms; i++)
			{
				Flicker f = flickers[i];
				if (f == null || !Application.isPlaying)
				{
					continue;
				}
				if (now >= f.next)
				{
					f.burstEnd = now + 0.15f + (float)f.rng.NextDouble() * 1.2f;
					f.next = f.burstEnd + 2.5f + (float)f.rng.NextDouble() * 7f;
					f.toggle = now;
				}
				bool wasOn = f.on;
				if (now >= f.burstEnd)
				{
					f.on = true;
					f.value = 1f;
				}
				else if (now >= f.toggle)
				{
					f.on = !wasOn || f.rng.NextDouble() < 0.25;
					f.value = f.on ? 0.65f + (float)f.rng.NextDouble() * 0.35f : (float)f.rng.NextDouble() * 0.2f;
					f.toggle = now + 0.03f + (float)f.rng.NextDouble() * 0.1f;
				}
				foreach (AudioSource hum in f.hums)
				{
					if (!hum)
					{
						continue;
					}
					bool near = head && (hum.transform.position - head.position).sqrMagnitude < hum.maxDistance * hum.maxDistance;
					if (near)
					{
						if (!hum.isPlaying)
						{
							hum.Play();
						}
					}
					else if (hum.isPlaying)
					{
						hum.Stop();
					}
					hum.volume = humVolume * (f.on ? 1f : 0.15f);
					if (near && f.on && !wasOn)
					{
						hum.PlayOneShot(PoolroomsSounds.Strike(), 0.6f);
					}
				}
			}
			PushGlobals();
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060004D7, RVA 0x27C1344)
			if (!Application.isPlaying || !levelMap || !PlayerRig.I)
			{
				return;
			}
			Vector3 head = PlayerRig.I.head.position;
			if (!view)
			{
				view = PlayerRig.I.head.GetComponent<Camera>();
			}
			Color fog = RenderSettings.fogColor;
			if (Atmosphere.At(head) == null)
			{
				Room room = rooms.Find(r => head.x >= r.left && head.x < r.right);
				if (room != null)
				{
					fog = room.look.fog.gamma;
				}
			}
			if (fogSet)
			{
				fog = Color.Lerp(fogNow, fog, 1f - MathF.Exp(Time.deltaTime * -3f));
			}
			fogNow = fog;
			fogSet = true;
			Color shown = Color.Lerp(fog, RenderSettings.fogColor, Mathf.Clamp01(Shader.GetGlobalVector("_ShallowsDark").x));
			RenderSettings.fogColor = shown;
			if (!view)
			{
				return;
			}
			view.backgroundColor = shown;
			AtmosphereZone outside = outsideZone;
			if (outside != null && outside.farClip > view.farClipPlane)
			{
				float distance = Mathf.Sqrt(outside.bounds.SqrDistance(head));
				if (distance < 25f)
				{
					view.farClipPlane = Mathf.Lerp(outside.farClip, view.farClipPlane, distance / 25f);
				}
			}
			if (!backdrop)
			{
				return;
			}
			backdrop.position = head;
			backdrop.localScale = Vector3.one * (view.farClipPlane * 0.97f * 2f);
		}

		private void BuildBackdrop()
		{
			// Rebuilt from native code (token 0x060004D8, RVA 0x27BFE90)
			Shader shader = Resources.Load<Shader>("Poolrooms/ShallowsBackdrop");
			if (!shader || !shader.isSupported)
			{
				return;
			}
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Mesh mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
			DestroyImmediate(sphere);
			GameObject go = new GameObject("Fog Backdrop");
			go.hideFlags = HideFlags.DontSave;
			go.transform.SetParent(propsRoot, false);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer r = go.AddComponent<MeshRenderer>();
			r.sharedMaterial = Made(new Material(shader) { name = "Poolrooms Fog Backdrop" });
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.receiveShadows = false;
			r.lightProbeUsage = LightProbeUsage.Off;
			r.reflectionProbeUsage = ReflectionProbeUsage.Off;
			r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
			backdrop = go.transform;
		}

		private void AddHums(Room room, Flicker f)
		{
			// Rebuilt from native code (token 0x060004D9, RVA 0x27BF1B0)
			foreach (Panel p in room.panels)
			{
				if (p.state != 1)
				{
					continue;
				}
				GameObject go = new GameObject("Lamp Buzz");
				go.hideFlags = HideFlags.DontSave;
				go.transform.SetParent(propsRoot, false);
				go.transform.position = p.centre + Vector3.down * 0.3f;
				AudioSource hum = go.AddComponent<AudioSource>();
				hum.clip = PoolroomsSounds.Hum();
				hum.loop = true;
				hum.spatialBlend = 1f;
				hum.rolloffMode = AudioRolloffMode.Linear;
				hum.minDistance = 1.5f;
				hum.maxDistance = Mathf.Max(14f, room.ceiling + 6f);
				hum.dopplerLevel = 0f;
				hum.volume = humVolume;
				hum.priority = 256;
				hum.time = (float)f.rng.NextDouble() * 0.9f;
				f.hums.Add(hum);
			}
		}

		private void BuildProps()
		{
			// Rebuilt from native code (token 0x060004DA, RVA 0x27BF640)
			Shader shader = Resources.Load<Shader>("Poolrooms/ShallowsProp");
			if (!shader || !shader.isSupported)
			{
				return;
			}
			Texture2D atlas = Made(PoolroomsProps.MakeAtlas());
			Material material = Made(new Material(shader)
			{
				name = "Poolrooms Props",
				mainTexture = atlas
			});
			Physics.SyncTransforms();
			foreach (Room room in rooms)
			{
				if (room.id == 0 || float.IsNaN(room.poolStart))
				{
					continue;
				}
				List<PoolroomsProps.Solid> solids = new List<PoolroomsProps.Solid>();
				Mesh mesh = PoolroomsProps.Build(room, solids);
				if (!mesh)
				{
					continue;
				}
				Made(mesh);
				GameObject go = new GameObject("Props - " + room.name);
				go.hideFlags = HideFlags.DontSave;
				go.transform.SetParent(propsRoot, false);
				go.AddComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer r = go.AddComponent<MeshRenderer>();
				r.sharedMaterial = material;
				r.shadowCastingMode = ShadowCastingMode.Off;
				r.receiveShadows = false;
				r.lightProbeUsage = LightProbeUsage.Off;
				r.reflectionProbeUsage = ReflectionProbeUsage.Off;
				foreach (PoolroomsProps.Solid solid in solids)
				{
					GameObject box = new GameObject("Solid");
					box.hideFlags = HideFlags.DontSave;
					box.transform.SetParent(go.transform, false);
					box.transform.SetPositionAndRotation(solid.centre, solid.rotation);
					box.AddComponent<BoxCollider>().size = Vector3.Max(solid.size, new Vector3(0.02f, 0.02f, 0.02f));
				}
				if (room.loftHole.width > 0f)
				{
					BuildLoft(room, go.transform);
				}
			}
		}

		private void BuildLoft(Room room, Transform parent)
		{
			// Rebuilt from native code (token 0x060004DB, RVA 0x27C18C0)
			MeshRenderer ceiling = room.ceilingObject ? room.ceilingObject.GetComponent<MeshRenderer>() : null;
			if (!ceiling || !ceiling.sharedMaterial)
			{
				return;
			}
			Material tiles = ceiling.sharedMaterial;
			Transform deck = room.hall.Find("Start Deck (checkpoint)");
			MeshRenderer deckRenderer = deck ? deck.GetComponent<MeshRenderer>() : null;
			Material floor = deckRenderer && deckRenderer.sharedMaterial ? deckRenderer.sharedMaterial : tiles;
			int layer = room.ceilingObject.gameObject.layer;
			Bounds cb = ceiling.bounds;
			Rect hole = room.loftHole;
			List<PoolroomsProps.ShellBox> cut = new List<PoolroomsProps.ShellBox>();
			Piece(cb.min.x, hole.xMin, cb.min.z, cb.max.z);
			Piece(hole.xMax, cb.max.x, cb.min.z, cb.max.z);
			Piece(hole.xMin, hole.xMax, cb.min.z, hole.yMin);
			Piece(hole.xMin, hole.xMax, hole.yMax, cb.max.z);
			LoftPart("Hall Ceiling (with the lounge hatch)", cut, tiles, layer, parent);
			ceiling.forceRenderingOff = true;
			cutCeilings.Add(ceiling);
			if (Application.isPlaying)
			{
				foreach (Collider c in room.ceilingObject.GetComponents<Collider>())
				{
					if (c.enabled)
					{
						c.enabled = false;
						cutColliders.Add(c);
					}
				}
			}
			LoftPart("Lounge Floor", room.loftShell.FindAll(x => x.floor), floor, layer, parent);
			LoftPart("Lounge Walls", room.loftShell.FindAll(x => !x.floor), tiles, layer, parent);
			LoftModel(room, parent, layer);
			Material lampMaterial = null;
			foreach (MeshRenderer r in room.hall.GetComponentsInChildren<MeshRenderer>(true))
			{
				if (r.name == "Light Panel" && r.sharedMaterial)
				{
					lampMaterial = r.sharedMaterial;
					break;
				}
			}
			if (!lampMaterial)
			{
				return;
			}
			Mesh quad = Made(new Mesh { name = "Lounge Lamp" });
			quad.SetVertices(new List<Vector3>
			{
				new Vector3(-0.5f, 0f, -0.5f),
				new Vector3(0.5f, 0f, -0.5f),
				new Vector3(0.5f, 0f, 0.5f),
				new Vector3(-0.5f, 0f, 0.5f)
			});
			quad.SetNormals(new List<Vector3> { Vector3.down, Vector3.down, Vector3.down, Vector3.down });
			quad.SetUVs(0, new List<Vector2>
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			});
			quad.SetTriangles(new int[] { 0, 1, 2, 0, 2, 3 }, 0);
			quad.RecalculateBounds();
			foreach (Vector3 at in room.loftLamps)
			{
				GameObject lamp = new GameObject("Lounge Lamp");
				lamp.hideFlags = HideFlags.DontSave;
				lamp.layer = layer;
				lamp.transform.SetParent(parent, false);
				lamp.transform.position = at;
				lamp.transform.localScale = new Vector3(1.2f, 1f, 0.6f);
				lamp.AddComponent<MeshFilter>().sharedMesh = quad;
				MeshRenderer r = lamp.AddComponent<MeshRenderer>();
				r.sharedMaterial = lampMaterial;
				r.shadowCastingMode = ShadowCastingMode.Off;
				r.receiveShadows = false;
			}

			// Rebuilt from native code (token 0x060004E2, RVA 0x27C2B1C)
			void Piece(float xa, float xb, float za, float zb)
			{
				if (xb - xa > 0.001f && zb - za > 0.001f)
				{
					cut.Add(new PoolroomsProps.ShellBox
					{
						centre = new Vector3((xa + xb) * 0.5f, cb.center.y, (za + zb) * 0.5f),
						size = new Vector3(xb - xa, cb.size.y, zb - za),
						floor = false
					});
				}
			}
		}

		public static bool LoungeFrame(out Vector3 corner, out float side, out float deckTop, out float ceiling)
		{
			// Rebuilt from native code (token 0x060004DC, RVA 0x27C41B8)
			List<Room> found = new List<Room>();
			FindRooms(found);
			Room room = found.Find(r => r.id == 2 && r.ceilingObject);
			corner = default(Vector3);
			side = 1f;
			ceiling = 0f;
			deckTop = 0f;
			if (room != null)
			{
				side = room.id % 2 == 0 ? 1f : -1f;
				corner = new Vector3(room.x0 + 0.35f, room.ceilingTop + 0.05f, side * room.halfWidth);
				deckTop = room.deckTop;
				ceiling = room.ceiling;
			}
			return room != null;
		}

		private void LoftModel(Room room, Transform parent, int layer)
		{
			// Rebuilt from native code (token 0x060004DD, RVA 0x27C307C)
			GameObject prefab = Resources.Load<GameObject>("Lounge/StaffLounge");
			Texture2D albedo = Resources.Load<Texture2D>("Lounge/StaffLounge_Albedo");
			Shader shader = Resources.Load<Shader>("Poolrooms/ShallowsProp");
			if (!prefab || !albedo || !shader)
			{
				return;
			}
			Material material = Made(new Material(shader)
			{
				name = "Staff Lounge",
				mainTexture = albedo
			});
			Material bulbs = material;
			Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
			if (unlit)
			{
				bulbs = Made(new Material(unlit) { name = "Staff Lounge Bulbs" });
				bulbs.SetTexture("_BaseMap", albedo);
				bulbs.mainTexture = albedo;
			}
			if (room.loftSide < 0f)
			{
				return;
			}
			GameObject lounge = Instantiate(prefab, new Vector3(room.loftU0, room.loftFloor, room.halfWidth), Quaternion.identity, parent);
			lounge.name = "Staff Lounge";
			List<Renderer> renderers = new List<Renderer>();
			Transform section = null;
			Transform top = null;
			Transform boombox = null;
			foreach (Transform t in lounge.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.hideFlags = HideFlags.DontSave;
				t.gameObject.layer = layer;
				if (t.name == "Ladder Section")
				{
					section = t;
				}
				if (t.name == "Ladder Top")
				{
					top = t;
				}
				MeshRenderer mr = t.GetComponent<MeshRenderer>();
				MeshFilter mf = t.GetComponent<MeshFilter>();
				if (!mr || !mf || !mf.sharedMesh)
				{
					continue;
				}
				mr.sharedMaterial = t.name.StartsWith("Glow") ? bulbs : material;
				mr.shadowCastingMode = ShadowCastingMode.Off;
				mr.receiveShadows = false;
				mr.lightProbeUsage = LightProbeUsage.Off;
				mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
				if (t.name.StartsWith("Item"))
				{
					continue;
				}
				if (!t.name.StartsWith("Ladder"))
				{
					renderers.Add(mr);
				}
				if (t.name == "Solid Boombox")
				{
					boombox = t;
				}
				if (!t.name.StartsWith("Solid"))
				{
					continue;
				}
				if (t.name.StartsWith("Solid Mesh"))
				{
					t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
					continue;
				}
				Bounds b = mf.sharedMesh.bounds;
				if (t.name.StartsWith("Solid Low"))
				{
					float h = Mathf.Min(b.size.y, 0.45f);
					b = new Bounds(new Vector3(b.center.x, b.min.y + h * 0.5f, b.center.z), new Vector3(b.size.x * 0.45f, h, b.size.z * 0.45f));
				}
				BoxCollider box = t.gameObject.AddComponent<BoxCollider>();
				box.center = b.center;
				box.size = b.size;
			}
			if (top)
			{
				LadderBoxes(top.gameObject, Local, room.loftLadderX - room.loftU0, -LadderSection, 0.95f, true);
			}
			if (section)
			{
				section.gameObject.SetActive(false);
				float y = -LadderSection;
				for (int i = 0; room.loftFloor + y > room.deckTop + 0.05f; i++, y -= LadderSection)
				{
					GameObject copy = Instantiate(section.gameObject, lounge.transform);
					copy.name = "Ladder Section " + i;
					copy.hideFlags = HideFlags.DontSave;
					copy.transform.localPosition = new Vector3(room.loftLadderX - room.loftU0, y, 0f);
					copy.transform.localRotation = Quaternion.identity;
					copy.SetActive(true);
					LadderBoxes(copy, Local, 0f, -LadderSection, 0f, false);
					if (i > 78)
					{
						break;
					}
				}
			}
			LoungeVisibility visibility = lounge.AddComponent<LoungeVisibility>();
			visibility.renderers = renderers.ToArray();
			visibility.fromY = room.ceiling - 2.5f;
			if (!Application.isPlaying)
			{
				return;
			}
			List<Transform> cans = new List<Transform>();
			foreach (Transform t in lounge.GetComponentsInChildren<Transform>(true))
			{
				if (t.name.StartsWith("Item Can"))
				{
					cans.Add(t);
				}
			}
			foreach (Transform can in cans)
			{
				LoungeCan.Make(can, parent);
			}
			if (!boombox)
			{
				return;
			}
			MeshFilter speaker = boombox.GetComponent<MeshFilter>();
			GameObject music = new GameObject("Lounge Music");
			music.hideFlags = HideFlags.DontSave;
			music.transform.SetParent(parent, false);
			music.transform.position = speaker && speaker.sharedMesh ? boombox.TransformPoint(speaker.sharedMesh.bounds.center) : boombox.position;
			LoungeMusic player = music.AddComponent<LoungeMusic>();
			player.fromY = (room.loftFloor + room.deckTop) * 0.5f;
			player.floorY = room.loftFloor;
			player.ladder = new Vector2(room.loftLadderX, room.loftSide * (room.halfWidth - LadderWall));

			// Rebuilt from native code (token 0x060004E3, RVA 0x27C53BC)
			static Vector3 Local(float u, float y, float v)
			{
				return new Vector3(u, y, -v);
			}
		}

		private static void LadderBoxes(GameObject piece, Func<float, float, float, Vector3> local, float x, float bottom, float railTop, bool hooks)
		{
			// Rebuilt from native code (token 0x060004DE, RVA 0x27C43C0)
			for (int side = -1; side <= 1; side += 2)
			{
				float u = side * LadderHalf + x;
				Box(local(u, (bottom + railTop) * 0.5f, LadderWall), new Vector3(0.045f, railTop - bottom, 0.045f));
				if (hooks)
				{
					Box(local(u, 1.05f, 0.2f), new Vector3(0.045f, 0.2f, 0.16f));
					Box(local(u, 1.12f, 0.38f), new Vector3(0.045f, 0.045f, 0.24f));
					Box(local(u, 0.98f, 0.54f), new Vector3(0.045f, 0.26f, 0.06f));
				}
			}
			for (int i = 0; i < 3; i++)
			{
				Box(local(x, (i + 0.5f) * LadderStep + bottom, LadderWall), new Vector3(0.44f, 0.032f, 0.032f));
			}

			// Rebuilt from native code (token 0x060004E4, RVA 0x27C46BC)
			void Box(Vector3 centre, Vector3 size)
			{
				BoxCollider box = piece.AddComponent<BoxCollider>();
				box.center = centre;
				box.size = size;
			}
		}

		private void LoftPart(string name, List<PoolroomsProps.ShellBox> boxes, Material material, int layer, Transform parent)
		{
			// Rebuilt from native code (token 0x060004DF, RVA 0x27C2C40)
			Mesh mesh = PoolroomsProps.BoxMesh(boxes, name);
			if (!mesh)
			{
				return;
			}
			Made(mesh);
			GameObject part = new GameObject(name);
			part.hideFlags = HideFlags.DontSave;
			part.layer = layer;
			part.transform.SetParent(parent, false);
			part.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer r = part.AddComponent<MeshRenderer>();
			r.sharedMaterial = material;
			r.shadowCastingMode = ShadowCastingMode.Off;
			r.lightProbeUsage = LightProbeUsage.Off;
			r.reflectionProbeUsage = ReflectionProbeUsage.Off;
			foreach (PoolroomsProps.ShellBox box in boxes)
			{
				GameObject solid = new GameObject("Solid");
				solid.hideFlags = HideFlags.DontSave;
				solid.layer = layer;
				solid.transform.SetParent(part.transform, false);
				solid.transform.position = box.centre;
				solid.AddComponent<BoxCollider>().size = box.size;
			}
		}
	}
}
