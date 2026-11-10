using UnityEngine;

namespace Shallows
{
	public static class DeathTimeline
	{
		public const float TossStart = 0.4f;

		public const float Throw = 0.5f;

		public const float TossEnd = 0.6f;

		public const float End = 1.6f;

		public const float ThrowYaw = -40f;

		// Rebuilt from native code (token 0x06000180, RVA 0x2728940)
		public static readonly float[] Whips = new float[] { 0.18f, 0.38f };

		private static readonly Vector2[] ShakeKeys = new Vector2[]
		{
			new Vector2(0f, 0f),
			new Vector2(0.18f, -1f),
			new Vector2(0.38f, 1f),
			new Vector2(0.6f, -1.3f),
			new Vector2(1.1f, -0.3f),
			new Vector2(1.6f, 0f)
		};

		private static readonly Vector2[] LiftKeys = new Vector2[]
		{
			new Vector2(0f, 0f),
			new Vector2(0.18f, 0.6f),
			new Vector2(0.38f, 0.6f),
			new Vector2(0.6f, 1f),
			new Vector2(1.6f, 0f)
		};

		public static float Lift(float t, out float shake)
		{
			// Rebuilt from native code (token 0x0600017E, RVA 0x27287E0)
			shake = Key(ShakeKeys, t);
			return Key(LiftKeys, t);
		}

		private static float Key(Vector2[] keys, float t)
		{
			// Rebuilt from native code (token 0x0600017F, RVA 0x2728868)
			if (t <= keys[0].x)
			{
				return keys[0].y;
			}
			for (int i = 0; i < keys.Length - 1; i++)
			{
				if (keys[i + 1].x > t)
				{
					float u = (t - keys[i].x) / (keys[i + 1].x - keys[i].x);
					return Mathf.Lerp(keys[i].y, keys[i + 1].y, u * u * (3f - 2f * u));
				}
			}
			return keys[keys.Length - 1].y;
		}
	}
}
