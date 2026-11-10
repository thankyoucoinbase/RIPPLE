using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Shallows
{
	public static class PoolPlatforms
	{
		// Rebuilt from native code (token 0x060004BB, RVA 0x27BA958)
		private static readonly Regex LayoutName = new Regex(@"^Level (\d+) Layout$");

		public static SortedDictionary<int, List<BoxCollider>> ByLevel()
		{
			// Rebuilt from native code (token 0x060004B5, RVA 0x27AF564)
			SortedDictionary<int, List<BoxCollider>> levels = new SortedDictionary<int, List<BoxCollider>>();
			foreach (Transform layout in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				Match match = LayoutName.Match(layout.name);
				if (!match.Success)
				{
					continue;
				}
				List<BoxCollider> still = new List<BoxCollider>();
				List<BoxCollider> moving = new List<BoxCollider>();
				foreach (BoxCollider box in layout.GetComponentsInChildren<BoxCollider>())
				{
					string name = box.name;
					if (name != "Platform" && name != "Plank" && name != "Safe Tile" && name != "Raft" && name != "Shuttle")
					{
						continue;
					}
					if (box.GetComponent<SinkingPlatform>())
					{
						continue;
					}
					if (box.GetComponent<MovingPlatform>())
					{
						moving.Add(box);
					}
					else if (name != "Raft" && name != "Shuttle")
					{
						still.Add(box);
					}
				}
				List<BoxCollider> tops = still.Count > 0 ? still : moving;
				if (tops.Count == 0)
				{
					continue;
				}
				tops.Sort(Compare);
				levels[int.Parse(match.Groups[1].Value)] = tops;
			}
			return levels;
		}

		private static int Compare(BoxCollider a, BoxCollider b)
		{
			// Rebuilt from native code (token 0x060004B6, RVA 0x27BA598)
			Vector3 pa = a.transform.position;
			Vector3 pb = b.transform.position;
			int byX = Mathf.RoundToInt(pa.x * 100f).CompareTo(Mathf.RoundToInt(pb.x * 100f));
			if (byX != 0)
			{
				return byX;
			}
			return Mathf.RoundToInt(pa.z * 100f).CompareTo(Mathf.RoundToInt(pb.z * 100f));
		}

		public static Vector3 RandomTop(BoxCollider box, System.Random rng, float inset)
		{
			// Rebuilt from native code (token 0x060004B7, RVA 0x27B0A1C)
			Transform t = box.transform;
			Vector3 size = Vector3.Scale(box.size, t.lossyScale);
			float x = Reach(size.x, inset) * ((float)rng.NextDouble() * 2f - 1f);
			float z = Reach(size.z, inset) * ((float)rng.NextDouble() * 2f - 1f);
			Vector3 local = box.center + new Vector3(x / Mathf.Max(0.0001f, t.lossyScale.x), box.size.y * 0.5f, z / Mathf.Max(0.0001f, t.lossyScale.z));
			return t.TransformPoint(local);
		}

		private static float Reach(float size, float inset)
		{
			// Rebuilt from native code (token 0x060004B8, RVA 0x27BA93C)
			return Mathf.Max(0f, size * 0.5f - inset);
		}

		public static int NearestLevel(SortedDictionary<int, List<BoxCollider>> levels, Vector3 p)
		{
			// Rebuilt from native code (token 0x060004B9, RVA 0x27AFB0C)
			float best = float.MaxValue;
			int nearest = 0;
			foreach (KeyValuePair<int, List<BoxCollider>> level in levels)
			{
				foreach (BoxCollider box in level.Value)
				{
					float distance = (box.bounds.ClosestPoint(p) - p).sqrMagnitude;
					if (distance < best)
					{
						best = distance;
						nearest = level.Key;
					}
				}
			}
			return nearest;
		}

		public static List<BoxCollider> Shuffled(List<BoxCollider> list, System.Random rng)
		{
			// Rebuilt from native code (token 0x060004BA, RVA 0x27B08CC)
			List<BoxCollider> shuffled = new List<BoxCollider>(list);
			for (int i = shuffled.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
			}
			return shuffled;
		}
	}
}
