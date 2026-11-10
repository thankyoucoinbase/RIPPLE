using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public static class WildTreeSpots
	{
		private const float TrunkRadius = 0.5f;

		private const float CanopyRadius = 0.95f;

		private const float MinFromPlatform = 1.1f;

		private const float MaxFromPlatform = 2.4f;

		private const float DeckGap = 3.5f;

		private const float WallInset = 1.3f;

		private static SortedDictionary<int, List<BoxCollider>> platforms;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060003D7, RVA 0x27A016C)
			platforms = null;
		}

		public static bool Find(GameManager game, int level, out Vector3 at, out Vector3 branch)
		{
			// Rebuilt from native code (token 0x060003D8, RVA 0x279EDD4)
			at = branch = Vector3.zero;
			Bounds deck = game.levels[level].startDeck;
			float deckEnd = deck.max.x;
			WaterVolume water = WaterVolume.At(new Vector3(deckEnd + 1.5f, deck.center.y, deck.center.z));
			if (!water)
			{
				return false;
			}
			Vector3 centre = water.transform.position;
			float minX = Mathf.Max(centre.x - water.size.x * 0.5f + WallInset, deckEnd + DeckGap);
			float maxX = centre.x + water.size.x * 0.5f - WallInset;
			if (level + 1 < game.levels.Length)
			{
				maxX = Mathf.Min(maxX, game.levels[level + 1].startDeck.min.x - DeckGap);
			}
			float minZ = centre.z - water.size.y * 0.5f + WallInset;
			float maxZ = centre.z + water.size.y * 0.5f - WallInset;
			if (maxX <= minX || maxZ <= minZ)
			{
				return false;
			}
			if (platforms == null || platforms.Count == 0 || AnyDestroyed())
			{
				platforms = PoolPlatforms.ByLevel();
			}
			platforms.TryGetValue(level + 1, out List<BoxCollider> boxes);
			bool open = boxes == null || boxes.Count < 1;
			float floorY = water.FloorY;
			for (int i = 0; i < 80; i++)
			{
				Vector3 p = new Vector3(Random.Range(minX, maxX), floorY, Random.Range(minZ, maxZ));
				if (!Clear(p))
				{
					continue;
				}
				Vector3 away = Random.insideUnitSphere;
				if (!open)
				{
					if (!NearestPlatform(boxes, p, out Vector3 edge, out float distance) || distance < MinFromPlatform || distance > (i < 60 ? MaxFromPlatform : MaxFromPlatform * 2f))
					{
						continue;
					}
					away = edge - p;
				}
				Vector3 flat = new Vector3(away.x, 0f, away.z);
				if (away.x * away.x + away.z * away.z < 0.0001f)
				{
					flat = Vector3.right;
				}
				at = p;
				branch = flat.normalized;
				return true;
			}
			return false;
		}

		private static bool AnyDestroyed()
		{
			// Rebuilt from native code (token 0x060003D9, RVA 0x27A01C0)
			foreach (List<BoxCollider> boxes in platforms.Values)
			{
				foreach (BoxCollider box in boxes)
				{
					if (!box)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool Clear(Vector3 p)
		{
			// Rebuilt from native code (token 0x060003DA, RVA 0x27A04C8)
			if (Physics.CheckCapsule(p + Vector3.up * 0.62f, p + Vector3.up * 1.6f, TrunkRadius, ~4, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			if (Physics.CheckCapsule(p + Vector3.up * 1.9f, p + Vector3.up * 2.75f, CanopyRadius, ~4, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			if (!Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 0.6f, ~4, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			if (Mathf.Abs(hit.point.y - p.y) > 0.15f)
			{
				return false;
			}
			foreach (MovingPlatform platform in MovingPlatform.All)
			{
				if (platform && platform.ReachDistance(p) < 1.35f)
				{
					return false;
				}
			}
			return true;
		}

		private static bool NearestPlatform(List<BoxCollider> boxes, Vector3 p, out Vector3 edge, out float distance)
		{
			// Rebuilt from native code (token 0x060003DB, RVA 0x27A0AE4)
			edge = p;
			distance = float.MaxValue;
			foreach (BoxCollider box in boxes)
			{
				if (!box)
				{
					continue;
				}
				Bounds bounds = box.bounds;
				Vector3 closest = bounds.ClosestPoint(new Vector3(p.x, bounds.center.y, p.z));
				float dx = closest.x - p.x;
				float dz = closest.z - p.z;
				float d = Mathf.Sqrt(dx * dx + dz * dz);
				if (d < distance)
				{
					distance = d;
					edge = closest;
				}
			}
			return distance < float.MaxValue;
		}
	}
}
