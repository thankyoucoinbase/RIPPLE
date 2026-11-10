using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public static class PoolroomsProps
	{
		internal struct ShellBox
		{
			public Vector3 centre;

			public Vector3 size;

			public bool floor;
		}

		internal struct Solid
		{
			public Vector3 centre;

			public Vector3 size;

			public Quaternion rotation;
		}

		private class Builder
		{
			private readonly List<Vector3> v = new List<Vector3>();

			private readonly List<Vector3> n = new List<Vector3>();

			private readonly List<Vector2> uv = new List<Vector2>();

			private readonly List<Color> col = new List<Color>();

			private readonly List<int> tri = new List<int>();

			public List<Solid> solids;

			public bool solid;

			private void AddSolid(Vector3 centre, Vector3 size, Quaternion rotation)
			{
				// Rebuilt from native code (token 0x0600050C, RVA 0x27CBCCC)
				if (!solid || solids == null)
				{
					return;
				}
				solids.Add(new Solid
				{
					centre = centre,
					size = size,
					rotation = rotation
				});
			}

			private static Vector2 UV(RectInt r, float u, float t)
			{
				// Rebuilt from native code (token 0x0600050D, RVA 0x27CBDD8)
				return new Vector2(Mathf.Lerp(r.xMin + 0.02f, r.xMax - 0.02f, u) / Size, 1f - Mathf.Lerp(r.yMax - 0.02f, r.yMin + 0.02f, t) / Size);
			}

			private int Vert(Vector3 p, Vector3 normal, Vector2 uv0, Color c)
			{
				// Rebuilt from native code (token 0x0600050E, RVA 0x27CBEF0)
				v.Add(p);
				n.Add(normal);
				uv.Add(uv0);
				col.Add(c);
				return v.Count - 1;
			}

			private void Tri(int a, int b, int c)
			{
				// Rebuilt from native code (token 0x0600050F, RVA 0x27CC144)
				Vector3 face = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
				if (Vector3.Dot(face, n[a]) >= 0f)
				{
					tri.Add(a);
					tri.Add(b);
					tri.Add(c);
				}
				else
				{
					tri.Add(a);
					tri.Add(c);
					tri.Add(b);
				}
			}

			public void Panel(Vector3 centre, Vector3 normal, Vector3 up, float width, float height, RectInt r, Color c, bool solidToo = true)
			{
				// Rebuilt from native code (token 0x06000510, RVA 0x27CAC30)
				if (solidToo)
				{
					AddSolid(centre - normal * 0.01f, new Vector3(width, height, 0.02f), Quaternion.LookRotation(normal, up));
				}
				Vector3 right = Vector3.Cross(normal, up).normalized * (width * 0.5f);
				Vector3 top = up.normalized * (height * 0.5f);
				int a = Vert(centre - right - top, normal, UV(r, 0f, 0f), c);
				int b = Vert(centre + right - top, normal, UV(r, 1f, 0f), c);
				int d = Vert(centre + right + top, normal, UV(r, 1f, 1f), c);
				int e = Vert(centre - right + top, normal, UV(r, 0f, 1f), c);
				Tri(a, e, d);
				Tri(a, d, b);
			}

			public void Flat(Vector3 centre, Vector3 right, Vector3 top, float width, float depth, RectInt r, Color c)
			{
				// Rebuilt from native code (token 0x06000511, RVA 0x27C903C)
				Vector3 across = right.normalized * (width * 0.5f);
				Vector3 along = top.normalized * (depth * 0.5f);
				int a = Vert(centre - across - along, Vector3.up, UV(r, 0f, 0f), c);
				int b = Vert(centre + across - along, Vector3.up, UV(r, 1f, 0f), c);
				int d = Vert(centre + across + along, Vector3.up, UV(r, 1f, 1f), c);
				int e = Vert(centre - across + along, Vector3.up, UV(r, 0f, 1f), c);
				Tri(a, e, d);
				Tri(a, d, b);
			}

			public void Box(Vector3 centre, Vector3 size, Quaternion rotation, RectInt r, Color c, bool worldSize = false)
			{
				// Rebuilt from native code (token 0x06000512, RVA 0x27C65BC)
				if (worldSize)
				{
					size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
				}
				AddSolid(centre, size, rotation);
				Vector3[] axes = new Vector3[3] { Vector3.right, Vector3.up, Vector3.forward };
				float[] half = new float[3] { size.x * 0.5f, size.y * 0.5f, size.z * 0.5f };
				for (int i = 0; i < 3; i++)
				{
					int j = (i + 1) % 3;
					int k = (i + 2) % 3;
					for (int s = -1; s <= 1; s += 2)
					{
						Vector3 normal = rotation * axes[i] * s;
						Vector3 du = rotation * axes[j] * half[j];
						Vector3 dv = rotation * axes[k] * half[k];
						Vector3 mid = centre + normal * half[i];
						int a = Vert(mid - du - dv, normal, UV(r, 0f, 0f), c);
						int b = Vert(mid + du - dv, normal, UV(r, 1f, 0f), c);
						int d = Vert(mid + du + dv, normal, UV(r, 1f, 1f), c);
						int e = Vert(mid - du + dv, normal, UV(r, 0f, 1f), c);
						Tri(a, b, d);
						Tri(a, d, e);
					}
				}
			}

			public void Tube(List<Vector3> path, Vector3 side, float radius, int sides, Color c)
			{
				// Rebuilt from native code (token 0x06000513, RVA 0x27CA26C)
				for (int i = 0; i < path.Count - 1; i++)
				{
					Vector3 step = path[i + 1] - path[i];
					if (step.sqrMagnitude > 0.0001f)
					{
						AddSolid((path[i] + path[i + 1]) * 0.5f, new Vector3(radius * 2f, radius * 2f, step.magnitude), Quaternion.LookRotation(step, side));
					}
				}
				int start = v.Count;
				for (int i = 0; i < path.Count; i++)
				{
					Vector3 along = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
					Vector3 right = Vector3.Cross(along, side).normalized;
					for (int k = 0; k <= sides; k++)
					{
						float angle = k * Mathf.PI * 2f / sides;
						Vector3 normal = right * MathF.Cos(angle) + side * MathF.Sin(angle);
						Vert(path[i] + normal * radius, normal, UV(Chrome, (float)k / sides, 0.5f), c);
					}
				}
				for (int i = 0; i < path.Count - 1; i++)
				{
					for (int k = 0; k < sides; k++)
					{
						int a = start + i * (sides + 1) + k;
						int b = a + sides + 1;
						Tri(a, b, b + 1);
						Tri(a, b + 1, a + 1);
					}
				}
			}

			public void Torus(Vector3 centre, Vector3 axis, float R, float r, int segments, int sides, Func<int, Color> colour)
			{
				// Rebuilt from native code (token 0x06000514, RVA 0x27CC4E4)
				Vector3 a = axis.normalized;
				Vector3 u = Vector3.Cross(a, Mathf.Abs(a.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
				Vector3 w = Vector3.Cross(a, u);
				AddSolid(centre, new Vector3((R + r) * 2f, (R + r) * 2f, r * 2f), Quaternion.LookRotation(a, u));
				for (int i = 0; i < segments; i++)
				{
					Color c = colour(i);
					int start = v.Count;
					for (int k = 0; k < 2; k++)
					{
						float around = (i + k) * Mathf.PI * 2f / segments;
						Vector3 dir = u * MathF.Cos(around) + w * MathF.Sin(around);
						Vector3 ring = centre + dir * R;
						for (int j = 0; j <= sides; j++)
						{
							float turn = j * Mathf.PI * 2f / sides;
							Vector3 normal = dir * MathF.Cos(turn) + a * MathF.Sin(turn);
							Vert(ring + normal * r, normal, UV(White, 0.5f, 0.5f), c);
						}
					}
					for (int j = 0; j < sides; j++)
					{
						int p = start + j;
						int q = p + sides + 1;
						Tri(p, q, q + 1);
						Tri(p, q + 1, p + 1);
					}
				}
			}

			public Mesh ToMesh(string name)
			{
				// Rebuilt from native code (token 0x06000515, RVA 0x27C6C10)
				if (v.Count == 0)
				{
					return null;
				}
				var mesh = new Mesh();
				mesh.name = name;
				if (v.Count > 65000)
				{
					mesh.indexFormat = IndexFormat.UInt32;
				}
				mesh.SetVertices(v);
				mesh.SetNormals(n);
				mesh.SetUVs(0, uv);
				mesh.SetColors(col);
				mesh.SetTriangles(tri, 0);
				mesh.RecalculateBounds();
				mesh.UploadMeshData(!Application.isEditor);
				return mesh;
			}
		}

		internal const int LoftLevel = 2;

		private const float LoftWide = 8f;

		private const float LoftDeep = 5.5f;

		private const float LoftHigh = 2.9f;

		private const float LoftWall = 0.25f;

		private const float LoftSlab = 0.05f;

		private const float HatchHalf = 0.6f;

		private const float HatchDeep = 1.15f;

		private const int Size = 256;

		private const float Px = 0.015f;

		private static readonly RectInt White = new RectInt(0, 0, 8, 8);

		private static readonly RectInt Chrome = new RectInt(8, 0, 8, 8);

		private static readonly RectInt Grate = new RectInt(16, 0, 16, 16);

		private static readonly RectInt Gutter = new RectInt(32, 0, 16, 8);

		private static readonly RectInt Wet = new RectInt(48, 0, 24, 32);

		private static readonly RectInt Depth = new RectInt(80, 0, 32, 12);

		private const int PlaqueTop = 32;

		private const int PlaqueW = 84;

		private const int PlaqueH = 26;

		private static readonly Color Ink = new Color(0.08f, 0.08f, 0.09f, 1f);

		private static readonly Color SignRed = new Color(0.78f, 0.12f, 0.1f, 1f);

		private static readonly Color SignBlue = new Color(0.12f, 0.3f, 0.62f, 1f);

		private static readonly Color SignYellow = new Color(1f, 0.8f, 0.12f, 1f);

		private static readonly Color Paper = new Color(0.94f, 0.93f, 0.88f, 1f);

		private static readonly (string text, int style)[] Plaques = new(string, int)[12]
		{
			("NO RUNNING", 0),
			("NO DIVING", 0),
			("SHALLOW|END", 1),
			("DEEP END", 1),
			("NO LIFEGUARD|ON DUTY", 2),
			("SWIM AT YOUR|OWN RISK", 2),
			("POOL CLOSED", 0),
			("NO|HORSEPLAY", 0),
			("STAFF ONLY", 3),
			("KEEP OUT", 3),
			("SHOWER|BEFORE SWIM", 1),
			("DO NOT|ENTER", 3)
		};

		private static readonly Dictionary<char, string> Font = new Dictionary<char, string>
		{
			['A'] = ".###.#...##...#######...##...##...#",
			['B'] = "####.#...##...#####.#...##...#####.",
			['C'] = ".###.#...##....#....#....#...#.###.",
			['D'] = "####.#...##...##...##...##...#####.",
			['E'] = "######....#....####.#....#....#####",
			['F'] = "######....#....####.#....#....#....",
			['G'] = ".###.#...##....#.####...##...#.####",
			['H'] = "#...##...##...#######...##...##...#",
			['I'] = ".###...#....#....#....#....#...###.",
			['J'] = "..###...#....#....#....#.#..#..##..",
			['K'] = "#...##..#.#.#..##...#.#..#..#.#...#",
			['L'] = "#....#....#....#....#....#....#####",
			['M'] = "#...###.###.#.##.#.##...##...##...#",
			['N'] = "#...###..##.#.##..###...##...##...#",
			['O'] = ".###.#...##...##...##...##...#.###.",
			['P'] = "####.#...##...#####.#....#....#....",
			['Q'] = ".###.#...##...##...##.#.##..#..##.#",
			['R'] = "####.#...##...#####.#.#..#..#.#...#",
			['S'] = ".#####....#.....###.....#....#####.",
			['T'] = "#####..#....#....#....#....#....#..",
			['U'] = "#...##...##...##...##...##...#.###.",
			['V'] = "#...##...##...##...##...#.#.#...#..",
			['W'] = "#...##...##...##.#.##.#.##.#.#.#.#.",
			['X'] = "#...##...#.#.#...#...#.#.#...##...#",
			['Y'] = "#...##...#.#.#...#....#....#....#..",
			['Z'] = "#####....#...#...#...#...#....#####",
			['0'] = ".###.#...##..###.#.###..##...#.###.",
			['1'] = "..#...##....#....#....#....#...###.",
			['2'] = ".###.#...#....#...#...#...#...#####",
			['3'] = "####.....#....#.###.....#....#####.",
			['4'] = "...#...##..#.#.#..#.#####...#....#.",
			['5'] = "######....####.....#....##...#.###.",
			['6'] = "..##..#...#....####.#...##...#.###.",
			['7'] = "#####....#...#...#...#....#....#...",
			['8'] = ".###.#...##...#.###.#...##...#.###.",
			['9'] = ".###.#...##...#.####....#...#..##..",
			['.'] = "..........................##...##..",
			['!'] = "..#....#....#....#....#.........#..",
			['-'] = "................###................"
		};

		internal static void PlanLoft(Poolrooms.Room room)
		{
			// Rebuilt from native code (token 0x060004FB, RVA 0x27C59E0)
			if (room.id != LoftLevel || !room.ceilingObject || float.IsNaN(room.poolStart))
			{
				return;
			}
			if (Quaternion.Angle(room.ceilingObject.rotation, Quaternion.identity) > 0.5f)
			{
				return;
			}
			if (room.poolStart - room.x0 < 3.5f)
			{
				return;
			}
			float side = room.id % 2 == 0 ? 1f : -1f;
			float hw = room.halfWidth;
			float u0 = room.x0 + 0.35f;
			float floor = room.ceilingTop + LoftSlab;
			float ladderX = u0 + 2.05f;
			room.loftSide = side;
			room.loftU0 = u0;
			room.loftFloor = floor;
			room.loftLadderX = ladderX;
			float wall = hw * side;
			float edge = (hw - HatchDeep) * side;
			room.loftHole = Rect.MinMaxRect(ladderX - HatchHalf, Mathf.Min(wall, edge), ladderX + HatchHalf, Mathf.Max(wall, edge));
			float h0 = room.loftHole.x - u0;
			float h1 = room.loftHole.xMax - u0;
			Shell(0f, h0, -LoftSlab, 0f, 0f, LoftDeep, true);
			Shell(h1, LoftWide, -LoftSlab, 0f, 0f, LoftDeep, true);
			Shell(h0, h1, -LoftSlab, 0f, HatchDeep, LoftDeep, true);
			Shell(-LoftWall, LoftWide + LoftWall, -LoftSlab, LoftHigh, -LoftWall, 0f, false);
			Shell(-LoftWall, LoftWide + LoftWall, -LoftSlab, LoftHigh, LoftDeep, LoftDeep + LoftWall, false);
			Shell(-LoftWall, 0f, -LoftSlab, LoftHigh, 0f, LoftDeep, false);
			Shell(LoftWide, LoftWide + LoftWall, -LoftSlab, LoftHigh, 0f, LoftDeep, false);
			Shell(-LoftWall, LoftWide + LoftWall, LoftHigh, LoftHigh + LoftWall, -LoftWall, LoftDeep + LoftWall, false);
			foreach (float x in new float[2] { 2.3f, 5.7f })
			{
				Vector3 lamp = Corner(x, 2.88f, 2.9f);
				room.panels.Add(new Poolrooms.Panel
				{
					centre = lamp,
					half = new Vector2(0.6f, 0.3f)
				});
				room.loftLamps.Add(lamp);
			}

			Vector3 Corner(float u, float y, float v)
			{
				// Rebuilt from native code (token 0x06000508, RVA 0x27C61A8)
				return new Vector3(u0 + u, floor + y, side * (hw - v));
			}

			void Shell(float ua, float ub, float ya, float yb, float va, float vb, bool isFloor)
			{
				// Rebuilt from native code (token 0x06000509, RVA 0x27C5FF4)
				Vector3 a = Corner(ua, ya, va);
				Vector3 b = Corner(ub, yb, vb);
				Vector3 size = new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
				if (size.x > 0.001f && size.y > 0.001f && size.z > 0.001f)
				{
					room.loftShell.Add(new ShellBox
					{
						centre = (a + b) * 0.5f,
						size = size,
						floor = isFloor
					});
				}
			}
		}

		private static void Loft(Builder b, Poolrooms.Room room)
		{
			// Rebuilt from native code (token 0x060004FC, RVA 0x27C61C4)
		}

		internal static Mesh BoxMesh(List<ShellBox> boxes, string name)
		{
			// Rebuilt from native code (token 0x060004FD, RVA 0x27C61C8)
			var b = new Builder();
			foreach (ShellBox box in boxes)
			{
				b.Box(box.centre, box.size, Quaternion.identity, White, Color.white);
			}
			return b.ToMesh(name);
		}

		public static Texture2D MakeAtlas()
		{
			// Rebuilt from native code (token 0x060004FE, RVA 0x27C6D74)
			var px = new Color[Size * Size];
			var rng = new System.Random(5);
			Fill(White, Color.white);
			float[] shades = new float[8] { 0.5f, 0.7f, 0.92f, 1f, 0.78f, 0.55f, 0.62f, 0.85f };
			for (int x = 0; x < Chrome.width; x++)
			{
				Fill(new RectInt(Chrome.x + x, Chrome.y, 1, Chrome.height), Color.white * shades[x]);
			}
			for (int y = 0; y < Grate.height; y++)
			{
				for (int x = 0; x < Grate.width; x++)
				{
					bool edge = y == 0 || x == 0 || x == Grate.width - 1 || y == Grate.height - 1;
					bool bar = x == 1 || x == Grate.width - 2 || y % 3 == 1 || y == Grate.height - 2;
					Color c;
					if (edge)
					{
						c = new Color(0.36f, 0.36f, 0.34f);
					}
					else
					{
						c = bar ? new Color(0.27f, 0.27f, 0.26f) : new Color(0.04f, 0.05f, 0.05f);
						if (rng.NextDouble() < 0.12)
						{
							c = Color.Lerp(c, new Color(0.45f, 0.28f, 0.12f), 0.6f);
						}
					}
					Set(x + Grate.x, Grate.y + y, c * Jit());
				}
			}
			for (int y = 0; y < Gutter.height; y++)
			{
				for (int x = 0; x < Gutter.width; x++)
				{
					Color c = y == 0 || y == Gutter.height - 1 ? new Color(0.62f, 0.62f, 0.6f) : (x % 2 == 1 ? new Color(0.05f, 0.06f, 0.06f) : new Color(0.5f, 0.5f, 0.49f));
					Set(x + Gutter.x, Gutter.y + y, c * Jit());
				}
			}
			Fill(Wet, SignYellow);
			for (int y = 0; y < Wet.height; y++)
			{
				for (int x = 0; x < Wet.width; x++)
				{
					if (y == 0 || x == 0 || x == Wet.width - 1 || y == Wet.height - 1)
					{
						Set(x + Wet.x, Wet.y + y, Ink);
					}
				}
			}
			for (int row = 0; row < 9; row++)
			{
				for (int dx = -row; dx <= row; dx++)
				{
					if ((row >= 2 && row <= 4 && dx == 0) || row > 6 || Mathf.Abs(dx) >= row - 1 || (row == 6 && dx == 0))
					{
						Set(dx + Wet.x + 12, row + 3 + Wet.y, Ink);
					}
				}
			}
			Text("WET", Wet.x + 3, Wet.y + 19, Ink, Set);
			Fill(Depth, SignBlue);
			Text("0.3M", Depth.x + 4, Depth.y + 3, Paper, Set);
			for (int i = 0; i < Plaques.Length; i++)
			{
				PaintPlaque(Plaques[i].text, Plaques[i].style, PlaqueRect(i), i + 31, Set);
			}
			var atlas = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
			atlas.name = "Poolrooms Props Atlas";
			atlas.filterMode = FilterMode.Point;
			atlas.wrapMode = TextureWrapMode.Clamp;
			atlas.SetPixels(px);
			atlas.Apply(true, !Application.isEditor);
			return atlas;

			void Set(int x, int y, Color c)
			{
				// Rebuilt from native code (token 0x06000518, RVA 0x27C7AA0)
				if (x >= 0 && y >= 0 && x < Size && y < Size)
				{
					px[(Size - 1 - y) * Size + x] = c;
				}
			}

			void Fill(RectInt r, Color c)
			{
				// Rebuilt from native code (token 0x06000519, RVA 0x27C7938)
				for (int y = r.yMin; y < r.yMax; y++)
				{
					for (int x = r.xMin; x < r.xMax; x++)
					{
						Set(x, y, c);
					}
				}
			}

			float Jit()
			{
				// Rebuilt from native code (token 0x0600051A, RVA 0x27C7A60)
				return (float)rng.NextDouble() * 0.08f + 0.94f;
			}
		}

		private static RectInt PlaqueRect(int i)
		{
			// Rebuilt from native code (token 0x060004FF, RVA 0x27C7CB8)
			return new RectInt(i % 3 * PlaqueW, PlaqueTop + i / 3 * PlaqueH, PlaqueW, PlaqueH);
		}

		private static void PaintPlaque(string text, int style, RectInt r, int seed, Action<int, int, Color> set)
		{
			// Rebuilt from native code (token 0x06000500, RVA 0x27C7D00)
			Color back;
			Color ink;
			switch (style)
			{
			case 1:
				back = SignBlue;
				ink = Paper;
				break;
			case 2:
				back = SignYellow;
				ink = Ink;
				break;
			default:
				back = Paper;
				ink = style == 0 ? SignRed : Ink;
				break;
			}
			var rng = new System.Random(seed);
			for (int y = 0; y < r.height; y++)
			{
				bool stripe = style == 3 && y >= 3 && y <= 6;
				for (int x = 0; x < r.width; x++)
				{
					bool border = y == 0 || x == 0 || x == r.width - 1 || y == r.height - 1;
					bool trim = y == 2 || x == 2 || x == r.width - 3 || y == r.height - 3;
					Color c = border ? new Color(0.3f, 0.3f, 0.3f) : (stripe ? SignRed : (trim ? ink : back));
					set(r.x + x, y + r.y, c * ((float)rng.NextDouble() * 0.08f + 0.92f));
				}
			}
			set(r.x + 4, r.y + 4, Ink);
			set(r.x + r.width - 5, r.y + 4, Ink);
			int streak = rng.Next(3, 9);
			for (int i = 0; i < streak; i++)
			{
				set(r.x + r.width - 5, r.y + 5 + i, new Color(0.55f, 0.35f, 0.18f, 1f));
			}
			string[] lines = text.Split('|');
			int top = r.y + (style == 3 ? 2 : 0) + (r.height - lines.Length * 9 + 2) / 2;
			for (int i = 0; i < lines.Length; i++)
			{
				Text(lines[i], r.x + (r.width + 1 - lines[i].Length * 6) / 2, top + i * 9, ink, set);
			}
		}

		private static void Text(string text, int x, int y, Color c, Action<int, int, Color> set)
		{
			// Rebuilt from native code (token 0x06000501, RVA 0x27C7AF8)
			for (int i = 0; i < text.Length; i++)
			{
				if (Font.TryGetValue(char.ToUpperInvariant(text[i]), out string glyph))
				{
					for (int row = 0; row < 7; row++)
					{
						for (int column = 0; column < 5; column++)
						{
							if (glyph[row * 5 + column] == '#')
							{
								set(x + column, row + y, c);
							}
						}
					}
				}
				x += 6;
			}
		}

		internal static Mesh Build(Poolrooms.Room room, List<Solid> solids)
		{
			// Rebuilt from native code (token 0x06000502, RVA 0x27C83F8)
			if (!room.water)
			{
				return null;
			}
			var b = new Builder();
			b.solids = solids;
			var rng = new System.Random(900 + room.id * 53);
			float hw = room.halfWidth;
			float deck = room.deckTop;
			float floorY = room.water.FloorY;
			foreach (float end in new float[2] { room.poolStart, room.poolEnd })
			{
				float dir = end == room.poolStart ? 1f : -1f;
				float tile = Gutter.width * Px;
				for (float z = 0.4f - hw; z + tile <= hw - 0.4f; z += tile)
				{
					b.Flat(new Vector3(end - dir * 0.27f, deck + 0.008f, z + tile * 0.5f), Vector3.forward, new Vector3(dir, 0f, 0f), tile, 0.3f, Gutter, Color.white);
				}
				for (float z = 6f - hw; z < hw - 4f; z += 12f)
				{
					b.Flat(new Vector3(end - dir * 0.75f, deck + 0.008f, z), -Vector3.forward * dir, new Vector3(dir, 0f, 0f), Depth.width * Px, Depth.height * Px, Depth, Color.white);
				}
			}
			for (int i = 0; i < 2; i++)
			{
				float t = i == 0 ? 0.33f : 0.67f;
				b.Flat(new Vector3(Mathf.Lerp(room.poolStart, room.poolEnd, t), floorY + 0.006f, R(-1f, 1f)), Vector3.right, Vector3.forward, 0.7f, 0.7f, Grate, Color.white);
			}
			for (int i = 0; i < 6; i++)
			{
				b.Flat(new Vector3(R(room.poolStart + 3f, room.poolEnd - 3f), floorY + 0.006f, R(3f - hw, hw - 3f)), Vector3.right, Vector3.forward, 0.4f, 0.4f, Grate, Color.white);
			}
			foreach (float end in new float[2] { room.poolStart, room.poolEnd })
			{
				float dir = end == room.poolStart ? 1f : -1f;
				for (int s = -1; s <= 1; s += 2)
				{
					float z = R(hw * 0.4f, hw * 0.75f) * s;
					b.solid = true;
					PoolLadder(b, end, dir, z, deck, floorY);
					b.solid = false;
				}
			}
			rng.NextDouble();
			if (room.loftHole.width > 0f)
			{
				b.solid = true;
				Loft(b, room);
				b.solid = false;
			}
			bool flip = rng.NextDouble() < 0.5;
			b.solid = true;
			WetFloorSign(b, new Vector3(R(room.x0 + 1f, room.poolStart - 1.2f), deck, flip ? -R(5f, hw - 5f) : R(5f, hw - 5f)), R(0f, 360f));
			var order = new List<int>();
			for (int i = 0; i < Plaques.Length; i++)
			{
				order.Insert(rng.Next(order.Count + 1), i);
			}
			Plaque(b, order[0], new Vector3(room.x0 + 0.012f, deck + R(1.3f, 1.7f), rng.NextDouble() < 0.5 ? -R(4f, 7f) : R(4f, 7f)), Vector3.right);
			for (int i = 1; i < 3; i++)
			{
				float side = i == 1 ? 1f : -1f;
				float along = R(room.poolStart + 4f, room.poolEnd - 4f);
				Plaque(b, order[i], new Vector3(along, deck + R(1.3f, 2.1f), (hw - 0.012f) * side), new Vector3(0f, 0f, -side));
			}
			b.solid = false;
			return b.ToMesh("Poolrooms Props - " + room.name);

			float R(float a, float c)
			{
				// Rebuilt from native code (token 0x0600050A, RVA 0x27C9470)
				return a + (c - a) * (float)rng.NextDouble();
			}
		}

		private static void PoolLadder(Builder b, float edge, float into, float z, float deck, float floorY)
		{
			// Rebuilt from native code (token 0x06000503, RVA 0x27C94B4)
			var steel = new Color(0.85f, 0.88f, 0.9f, 1f);
			for (int s = -1; s <= 1; s += 2)
			{
				float zz = z + s * 0.2f;
				var rail = new List<Vector3>();
				rail.Add(P(-0.32f, deck - 0.01f, zz));
				rail.Add(P(-0.32f, deck + 0.28f, zz));
				for (int k = 1; k < 6; k++)
				{
					float angle = (1f - k / 6f) * Mathf.PI;
					rail.Add(P(MathF.Cos(angle) * 0.18f - 0.14f, deck + 0.28f + MathF.Sin(angle) * 0.18f, zz));
				}
				rail.Add(P(0.04f, deck + 0.28f, zz));
				rail.Add(P(0.04f, floorY + 0.02f, zz));
				b.Tube(rail, Vector3.forward, 0.022f, 6, steel);
				b.Box(P(-0.32f, deck + 0.004f, zz), new Vector3(0.09f, 0.008f, 0.09f), Quaternion.identity, Chrome, steel);
			}
			for (int i = 0; i < 2; i++)
			{
				b.Box(P(0.06f, floorY + 0.1f + i * 0.12f, z), new Vector3(0.06f, 0.025f, 0.42f), Quaternion.identity, Chrome, steel);
			}

			Vector3 P(float along, float y, float zz)
			{
				// Rebuilt from native code (token 0x0600050B, RVA 0x27CA25C)
				return new Vector3(edge + into * along, y, zz);
			}
		}

		private static void WallLadder(Builder b, float x, float side, float hw, float deck, float top, float rungTop, bool hatch)
		{
			// Rebuilt from native code (token 0x06000504, RVA 0x27CA824)
			var steel = new Color(0.5f, 0.52f, 0.55f, 0.6f);
			for (int s = -1; s <= 1; s += 2)
			{
				b.Box(new Vector3(x + s * 0.22f, (deck + top) * 0.5f, side * hw - side * 0.14f), new Vector3(0.045f, top - deck, 0.045f), Quaternion.identity, Chrome, steel);
			}
			for (float y = deck + 0.3f; y < rungTop; y += 0.3f)
			{
				b.Box(new Vector3(x, y, side * hw - side * 0.14f), new Vector3(0.44f, 0.03f, 0.03f), Quaternion.identity, Chrome, steel);
			}
			for (float y = deck + 1f; y < top; y += 2.5f)
			{
				for (int s = -1; s <= 1; s += 2)
				{
					b.Box(new Vector3(x + s * 0.22f, y, side * hw - side * 0.14f * 0.5f), new Vector3(0.03f, 0.04f, 0.14f), Quaternion.identity, Chrome, steel);
				}
			}
			if (hatch)
			{
				b.Box(new Vector3(x, top + 0.05f - 0.02f, side * hw - side * 0.55f), new Vector3(0.8f, 0.04f, 0.8f), Quaternion.identity, White, new Color(0.2f, 0.21f, 0.22f, 0.3f));
			}
		}

		private static void WetFloorSign(Builder b, Vector3 foot, float yaw)
		{
			// Rebuilt from native code (token 0x06000505, RVA 0x27C9ACC)
			Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
			for (int s = -1; s <= 1; s += 2)
			{
				Vector3 bottom = foot + turn * new Vector3(0f, 0f, s * 0.09f);
				Vector3 apex = foot + turn * new Vector3(0f, 0.42f, 0f);
				Vector3 up = (apex - bottom).normalized;
				Vector3 normal = Vector3.Cross(up, turn * Vector3.right);
				normal *= Vector3.Dot(normal, turn * Vector3.forward * s) > 0f ? 1f : -1f;
				Vector3 mid = (bottom + apex) * 0.5f;
				float length = Vector3.Distance(bottom, apex);
				b.Panel(mid + normal * 0.004f, normal, up, 0.27f, length, Wet, Color.white);
				b.Panel(mid - normal * 0.004f, -normal, up, 0.27f, length, Wet, Color.white);
			}
		}

		private static void Plaque(Builder b, int index, Vector3 at, Vector3 facing)
		{
			// Rebuilt from native code (token 0x06000506, RVA 0x27C9F68)
			Vector3 size = Quaternion.LookRotation(-facing, Vector3.up) * new Vector3(1.29f, 0.42f, 0.02f);
			b.Box(at - facing * 0.01f, size, Quaternion.identity, White, new Color(0.25f, 0.25f, 0.26f, 0f), true);
			b.Panel(at + facing * 0.002f, facing, Vector3.up, 1.26f, 0.39f, PlaqueRect(index), Color.white, false);
		}
	}
}
