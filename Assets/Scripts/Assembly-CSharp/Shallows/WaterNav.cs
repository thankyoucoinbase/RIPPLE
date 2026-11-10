using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class WaterNav
	{
		private const float Cell = 0.5f;

		private const int Obstacles = -5;

		private static readonly Dictionary<WaterVolume, WaterNav> maps = new Dictionary<WaterVolume, WaterNav>();

		private static readonly Collider[] hits = new Collider[32];

		private readonly WaterVolume water;

		private readonly int width;

		private readonly int length;

		private readonly Vector3 origin;

		private readonly bool[] blocked;

		private readonly byte[] near;

		private int[] region;

		private int mainRegion;

		private static float[] cost;

		private static int[] from;

		private static bool[] closed;

		private static readonly List<KeyValuePair<float, int>> heap = new List<KeyValuePair<float, int>>();

		private static readonly int[] dx = new int[8] { 1, -1, 0, 0, 1, 1, -1, -1 };

		private static readonly int[] dz = new int[8] { 0, 0, 1, -1, 1, -1, 1, -1 };

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000591, RVA 0x27DD7FC)
			maps.Clear();
		}

		public static WaterNav For(WaterVolume water, float clearance, float edgeInset)
		{
			// Rebuilt from native code (token 0x06000592, RVA 0x27DD874)
			if (!water)
			{
				return null;
			}
			if (!maps.TryGetValue(water, out WaterNav nav))
			{
				nav = new WaterNav(water, clearance, edgeInset);
				maps[water] = nav;
			}
			return nav;
		}

		private WaterNav(WaterVolume water, float clearance, float edgeInset)
		{
			// Rebuilt from native code (token 0x06000593, RVA 0x27DD9C0)
			this.water = water;
			Vector3 position = water.transform.position;
			width = Mathf.Max(1, Mathf.FloorToInt(water.size.x / Cell));
			length = Mathf.Max(1, Mathf.FloorToInt(water.size.y / Cell));
			origin = new Vector3(position.x - width * Cell * 0.5f + Cell * 0.5f, water.FloorY, position.z - length * Cell * 0.5f + Cell * 0.5f);
			blocked = new bool[width * length];
			near = new byte[length * width];
			Physics.SyncTransforms();
			float floor = water.FloorY;
			for (int x = 0; x < width; x++)
			{
				for (int z = 0; z < length; z++)
				{
					Vector3 p = new Vector3(x * Cell + origin.x, 0f, z * Cell + origin.z);
					int i = x + width * z;
					if (!InsideEdge(p, edgeInset))
					{
						blocked[i] = true;
						continue;
					}
					Vector3 bottom = p + new Vector3(0f, floor + 0.3f, 0f);
					Vector3 top = p + new Vector3(0f, floor + 1.4f, 0f);
					if (Solid(bottom, top, clearance, floor))
					{
						blocked[i] = true;
					}
					else if (Solid(bottom, top, clearance + 0.6f, floor))
					{
						near[i] = 1;
					}
				}
			}
			foreach (MovingPlatform platform in MovingPlatform.All)
			{
				if (!platform || !water.ContainsXZ(platform.Home))
				{
					continue;
				}
				Vector3 home = platform.Home;
				float reach = platform.ReachBound + clearance + 0.6f;
				for (int x = 0; x < width; x++)
				{
					for (int z = 0; z < length; z++)
					{
						int i = x + width * z;
						Vector3 c = CellCentre(x, z);
						if (!blocked[i] && Mathf.Abs(c.x - home.x) <= reach && Mathf.Abs(c.z - home.z) <= reach)
						{
							float distance = platform.ReachDistance(c);
							if (distance < clearance)
							{
								blocked[i] = true;
							}
							else if (distance < clearance + 0.6f)
							{
								near[i] = 1;
							}
						}
					}
				}
			}
			FindRegions();
		}

		internal WaterNav(Vector3 origin, int width, int length, bool[] blocked, byte[] near)
		{
			// Rebuilt from native code (token 0x06000594, RVA 0x27DE828)
			this.origin = origin;
			this.width = width;
			this.length = length;
			this.blocked = blocked;
			this.near = near;
			FindRegions();
		}

		private void FindRegions()
		{
			// Rebuilt from native code (token 0x06000595, RVA 0x27DE540)
			region = new int[length * width];
			for (int i = 0; i < region.Length; i++)
			{
				region[i] = -1;
			}
			Queue<int> queue = new Queue<int>();
			mainRegion = -1;
			int biggest = 0;
			int next = 0;
			for (int i = 0; i < region.Length; i++)
			{
				if (blocked[i] || region[i] >= 0)
				{
					continue;
				}
				region[i] = next;
				queue.Enqueue(i);
				int count = 0;
				while (queue.Count > 0)
				{
					int cell = queue.Dequeue();
					count++;
					int z = cell / width;
					int x = cell % width;
					for (int k = 0; k < 4; k++)
					{
						int nx = x + dx[k];
						int nz = z + dz[k];
						if (Open(nx, nz) && region[nx + width * nz] < 0)
						{
							region[nx + width * nz] = next;
							queue.Enqueue(nx + width * nz);
						}
					}
				}
				if (count > biggest)
				{
					mainRegion = next;
					biggest = count;
				}
				next++;
			}
		}

		private bool InsideEdge(Vector3 p, float inset)
		{
			// Rebuilt from native code (token 0x06000596, RVA 0x27DE26C)
			return Mathf.Abs(p.x - water.transform.position.x) <= water.size.x * 0.5f - inset && Mathf.Abs(p.z - water.transform.position.z) <= water.size.y * 0.5f - inset;
		}

		private static bool Solid(Vector3 bottom, Vector3 top, float radius, float floor)
		{
			// Rebuilt from native code (token 0x06000597, RVA 0x27DE2FC)
			int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, hits, Obstacles, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < count; i++)
			{
				Bounds bounds = hits[i].bounds;
				if (bounds.center.y + bounds.extents.y >= floor + 0.06f && !hits[i].GetComponentInParent<MovingPlatform>())
				{
					return true;
				}
			}
			return false;
		}

		private Vector3 CellCentre(int x, int z)
		{
			// Rebuilt from native code (token 0x06000598, RVA 0x27DE250)
			return new Vector3(x * Cell + origin.x, origin.y, z * Cell + origin.z);
		}

		private bool ToCell(Vector3 p, out int x, out int z)
		{
			// Rebuilt from native code (token 0x06000599, RVA 0x27DE910)
			x = Mathf.FloorToInt((p.x - origin.x) / Cell + 0.5f);
			z = Mathf.FloorToInt((p.z - origin.z) / Cell + 0.5f);
			bool inside = x >= 0 && z >= 0 && x < width && z < length;
			x = Mathf.Clamp(x, 0, width - 1);
			z = Mathf.Clamp(z, 0, length - 1);
			return inside;
		}

		private bool Open(int x, int z)
		{
			// Rebuilt from native code (token 0x0600059A, RVA 0x27DE8AC)
			return x >= 0 && z >= 0 && x < width && z < length && !blocked[x + width * z];
		}

		public bool IsOpen(Vector3 p)
		{
			// Rebuilt from native code (token 0x0600059B, RVA 0x27DEA84)
			return ToCell(p, out int x, out int z) && Open(x, z);
		}

		public bool InMainRegion(Vector3 p)
		{
			// Rebuilt from native code (token 0x0600059C, RVA 0x27DEAC8)
			return ToCell(p, out int x, out int z) && Open(x, z) && region[x + width * z] == mainRegion;
		}

		public Vector3 NearestOpen(Vector3 p, Vector3? from = null)
		{
			// Rebuilt from native code (token 0x0600059D, RVA 0x27DEB5C)
			int want = mainRegion;
			if (from.HasValue && ToCell(from.Value, out int fx, out int fz) && Open(fx, fz))
			{
				want = region[fx + width * fz];
			}
			ToCell(p, out int cx, out int cz);
			if (!Open(cx, cz) || region[cx + width * cz] != want)
			{
				for (int r = 1; r < Mathf.Max(width, length); r++)
				{
					Vector3 best = Vector3.zero;
					float bestDistance = float.MaxValue;
					// Finish the next ring too once one is found: a cell there can still be nearer in a straight line.
					for (int ring = r; ring <= r + 1; ring++)
					{
						for (int x = cx - ring; x <= cx + ring; x++)
						{
							for (int z = cz - ring; z <= cz + ring; z++)
							{
								if (Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(z - cz)) != ring || !Open(x, z) || region[x + width * z] != want)
								{
									continue;
								}
								Vector3 c = CellCentre(x, z);
								float ox = c.x - p.x;
								float oz = c.z - p.z;
								float distance = ox * ox + oz * oz;
								if (distance < bestDistance)
								{
									best = c;
									bestDistance = distance;
								}
							}
						}
						if (bestDistance == float.MaxValue)
						{
							break;
						}
					}
					if (bestDistance < float.MaxValue)
					{
						return best;
					}
				}
			}
			return new Vector3(p.x, origin.y, p.z);
		}

		public Vector3 RandomOpen(float inset)
		{
			// Rebuilt from native code (token 0x0600059E, RVA 0x27DF064)
			for (int i = 0; i < 30; i++)
			{
				Vector3 p = water.RandomPoint(inset);
				if (ToCell(p, out int x, out int z) && Open(x, z) && region[x + width * z] == mainRegion)
				{
					return p;
				}
			}
			return NearestOpen(water.RandomPoint(inset));
		}

		public bool Clear(Vector3 a, Vector3 b)
		{
			// Rebuilt from native code (token 0x0600059F, RVA 0x27DF164)
			float ax = (a.x - origin.x) / Cell + 0.5f;
			float az = (a.z - origin.z) / Cell + 0.5f;
			float bx = (b.x - origin.x) / Cell + 0.5f;
			float bz = (b.z - origin.z) / Cell + 0.5f;
			int x = Mathf.FloorToInt(ax);
			int z = Mathf.FloorToInt(az);
			int endX = Mathf.FloorToInt(bx);
			int endZ = Mathf.FloorToInt(bz);
			int stepX = bx - ax > 0f ? 1 : -1;
			int stepZ = bz - az > 0f ? 1 : -1;
			float deltaX = Mathf.Abs(bx - ax) > 1E-06f ? 1f / Mathf.Abs(bx - ax) : float.MaxValue;
			float deltaZ = Mathf.Abs(bz - az) > 1E-06f ? 1f / Mathf.Abs(bz - az) : float.MaxValue;
			float nextX = deltaX < float.MaxValue ? deltaX * (bx - ax > 0f ? x + 1 - ax : ax - x) : float.MaxValue;
			float nextZ = deltaZ < float.MaxValue ? deltaZ * (bz - az > 0f ? z + 1 - az : az - z) : float.MaxValue;
			for (int guard = 0; (x != endX || z != endZ) && guard < 4096; guard++)
			{
				if (nextX > 1f && nextZ > 1f)
				{
					break;
				}
				if (Mathf.Abs(nextX - nextZ) < 1E-05f)
				{
					// Through a corner: both side cells have to be open.
					if (!Open(x + stepX, z) || !Open(x, z + stepZ))
					{
						return false;
					}
					nextX += deltaX;
					nextZ += deltaZ;
					x += stepX;
					z += stepZ;
				}
				else if (nextX < nextZ)
				{
					nextX += deltaX;
					x += stepX;
				}
				else
				{
					nextZ += deltaZ;
					z += stepZ;
				}
				if (!Open(x, z))
				{
					return false;
				}
			}
			return Open(endX, endZ);
		}

		public void FindPath(Vector3 start, Vector3 goal, List<Vector3> path)
		{
			// Rebuilt from native code (token 0x060005A0, RVA 0x27DF520)
			path.Clear();
			Vector3 begin = IsOpen(start) ? start : NearestOpen(start);
			Vector3 target = NearestOpen(goal, begin);
			if (Clear(begin, target))
			{
				path.Add(target);
				return;
			}
			int cells = length * width;
			if (cost == null || cost.Length < cells)
			{
				cost = new float[cells];
				from = new int[cells];
				closed = new bool[cells];
			}
			for (int i = 0; i < cells; i++)
			{
				cost[i] = float.MaxValue;
				closed[i] = false;
			}
			ToCell(begin, out int sx, out int sz);
			ToCell(target, out int gx, out int gz);
			int startCell = sx + width * sz;
			cost[startCell] = 0f;
			from[startCell] = -1;
			heap.Clear();
			Push(0f, startCell);
			int goalCell = gx + width * gz;
			int end = -1;
			int nearest = startCell;
			float nearestDistance = float.MaxValue;
			while (heap.Count > 0)
			{
				int cell = Pop();
				if (closed[cell])
				{
					continue;
				}
				int z = cell / width;
				int x = cell % width;
				float distance = Sq(x - gx) + Sq(z - gz);
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = cell;
				}
				closed[cell] = true;
				if (cell == goalCell)
				{
					end = goalCell;
					break;
				}
				for (int k = 0; k < 8; k++)
				{
					int nx = x + dx[k];
					int nz = z + dz[k];
					if (!Open(nx, nz) || (k >= 4 && (!Open(x + dx[k], z) || !Open(x, z + dz[k]))))
					{
						continue;
					}
					int next = nx + width * nz;
					if (closed[next])
					{
						continue;
					}
					float g = (k < 4 ? 1f : 1.414f) * (near[next] == 0 ? 1f : 1.6f) + cost[cell];
					if (g < cost[next])
					{
						cost[next] = g;
						from[next] = cell;
						Push(g + Mathf.Sqrt(Sq(nx - gx) + Sq(nz - gz)), next);
					}
				}
			}
			Vector3 last = target;
			if (end < 0)
			{
				if (nearest == startCell)
				{
					return;
				}
				end = nearest;
				last = CellCentre(nearest % width, nearest / width);
			}
			List<Vector3> route = new List<Vector3>();
			for (int c = end; c >= 0; c = from[c])
			{
				route.Add(CellCentre(c % width, c / width));
			}
			route.Reverse();
			route[route.Count - 1] = last;
			Vector3 at = begin;
			for (int i = 0; i < route.Count - 1;)
			{
				int j = route.Count - 1;
				while (j > i + 1 && !Clear(at, route[j]))
				{
					j--;
				}
				path.Add(route[j]);
				at = route[j];
				i = j;
			}
			if (path.Count == 0)
			{
				path.Add(last);
			}
		}

		private static float Sq(int v)
		{
			// Rebuilt from native code (token 0x060005A1, RVA 0x27E05A4)
			return v * v;
		}

		private static void Push(float f, int cell)
		{
			// Rebuilt from native code (token 0x060005A2, RVA 0x27E0028)
			heap.Add(new KeyValuePair<float, int>(f, cell));
			int i = heap.Count - 1;
			while (i > 0)
			{
				int parent = (i - 1) / 2;
				if (heap[parent].Key <= heap[i].Key)
				{
					break;
				}
				(heap[parent], heap[i]) = (heap[i], heap[parent]);
				i = parent;
			}
		}

		private static int Pop()
		{
			// Rebuilt from native code (token 0x060005A3, RVA 0x27E0294)
			int top = heap[0].Value;
			int last = heap.Count - 1;
			heap[0] = heap[last];
			heap.RemoveAt(last);
			int i = 0;
			while (true)
			{
				int smallest = i;
				int left = i * 2 + 1;
				int right = i * 2 + 2;
				if (left < heap.Count && heap[left].Key < heap[smallest].Key)
				{
					smallest = left;
				}
				if (right < heap.Count && heap[right].Key < heap[smallest].Key)
				{
					smallest = right;
				}
				if (smallest == i)
				{
					break;
				}
				(heap[smallest], heap[i]) = (heap[i], heap[smallest]);
				i = smallest;
			}
			return top;
		}
	}
}
