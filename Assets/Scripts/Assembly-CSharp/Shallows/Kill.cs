using UnityEngine;

namespace Shallows
{
	public struct Kill
	{
		public KillMove move;

		public int chomps;

		public const int MoveCount = 3;

		public const float ShakeBite = 0.13f;

		public const float PinDown = 0.3f;

		public const float PinFirstChomp = 0.8f;

		public const float PinGap = 0.5f;

		public const float ConsumeBite = 0.13f;

		public const float ConsumeLift = 0.25f;

		public const float ConsumeLifted = 0.6f;

		public const float ConsumeRaise = 0.55f;

		public const float ConsumeSmash = 0.88f;

		public const float ConsumeSwallowed = 1.25f;

		public const float ConsumeGulp = 1.3f;

		private static int last = -1;

		public int Code
		{
			get
			{
				// Rebuilt from native code (token 0x060001F6, RVA 0x277E848)
				return (int)move | (Mathf.Clamp(chomps, 2, 4) << 4);
			}
		}

		public float[] Hits
		{
			get
			{
				// Rebuilt from native code (token 0x060001FA, RVA 0x277BC98)
				if (move == KillMove.Consume)
				{
					return new float[] { ConsumeBite, ConsumeSmash };
				}
				if (move != KillMove.Pin)
				{
					return new float[] { ShakeBite };
				}
				float[] hits = new float[chomps];
				for (int i = 0; i < chomps; i++)
				{
					hits[i] = PinChomp(i);
				}
				return hits;
			}
		}

		public float Release
		{
			get
			{
				// Rebuilt from native code (token 0x060001FB, RVA 0x277CA60)
				if (move == KillMove.Pin)
				{
					return PinChomp(chomps - 1) + 0.4f;
				}
				return move == KillMove.Consume ? float.MaxValue : 0.5f;
			}
		}

		public float Dead
		{
			get
			{
				// Rebuilt from native code (token 0x060001FC, RVA 0x277E9AC)
				return move == KillMove.Pin ? PinFirstChomp : (move == KillMove.Consume ? ConsumeSmash : 0.5f);
			}
		}

		public float End
		{
			get
			{
				// Rebuilt from native code (token 0x060001FD, RVA 0x277BE04)
				if (move == KillMove.Pin)
				{
					return PinChomp(chomps - 1) + 0.8f;
				}
				return move == KillMove.Consume ? 1.75f : 1.6f;
			}
		}

		public Kill(KillMove move, int chomps)
		{
			// Rebuilt from native code (token 0x060001F5, RVA 0x277B5E0)
			this.move = move;
			this.chomps = Mathf.Clamp(chomps, 2, 4);
		}

		public static Kill FromCode(int code)
		{
			// Rebuilt from native code (token 0x060001F7, RVA 0x277E86C)
			int move = code & 0xF;
			return new Kill(move < MoveCount ? (KillMove)move : KillMove.Shake, (code >> 4) & 0xF);
		}

		public static Kill Random()
		{
			// Rebuilt from native code (token 0x060001F8, RVA 0x277E89C)
			int move = UnityEngine.Random.Range(0, MoveCount);
			if (move == last)
			{
				move = (move + UnityEngine.Random.Range(1, MoveCount)) % MoveCount;
			}
			last = move;
			return new Kill((KillMove)move, UnityEngine.Random.Range(2, 5));
		}

		public static float PinChomp(int i)
		{
			// Rebuilt from native code (token 0x060001F9, RVA 0x277E998)
			return PinFirstChomp + i * PinGap;
		}

		public float PinHead(float t, out float jaw)
		{
			// Rebuilt from native code (token 0x060001FE, RVA 0x277E9D8)
			jaw = PinDown;
			if (t < PinDown)
			{
				jaw = 0.55f;
				return Mathf.Lerp(-0.2f, 0.3f, t / PinDown);
			}
			int count = Mathf.Clamp(chomps, 2, 4);
			for (int i = 0; i < count; i++)
			{
				float chomp = PinChomp(i);
				if (t > chomp + 0.3f)
				{
					continue;
				}
				float rise = chomp - 0.24f;
				float bite = chomp - 0.09f;
				if (t < rise)
				{
					float settle = i == 0 ? Mathf.Clamp01((t - PinDown) * 4f) : 1f;
					jaw = 0.35f;
					return Mathf.Lerp(0.3f, 0.2f, settle);
				}
				if (t < bite)
				{
					float e = Ease((t - rise) / (bite - rise));
					jaw = Mathf.Lerp(0.35f, 1f, e);
					return Mathf.Lerp(0.2f, -0.35f, e);
				}
				if (t < chomp)
				{
					float x = (t - bite) / (chomp - bite);
					float x2 = x * x;
					jaw = Mathf.Lerp(1f, 0f, x * x2);
					return Mathf.Lerp(-0.35f, 1f, x2);
				}
				if (t < chomp + 0.12f)
				{
					jaw = 0f;
					return 1f;
				}
				float open = Ease((t - chomp - 0.12f) / 0.18f);
				jaw = Mathf.Lerp(0f, 0.35f, open);
				return Mathf.Lerp(1f, 0.2f, open);
			}
			float done = Mathf.Clamp01((t - PinChomp(count - 1) - 0.3f) / 0.4f);
			jaw = Mathf.Lerp(0.35f, 0.1f, done);
			return Mathf.Lerp(0.2f, -0.1f, done);
		}

		private static float Ease(float x)
		{
			// Rebuilt from native code (token 0x060001FF, RVA 0x277ED7C)
			x = Mathf.Clamp01(x);
			return x * x * (3f - 2f * x);
		}
	}
}
