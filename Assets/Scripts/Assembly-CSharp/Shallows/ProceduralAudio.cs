using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Shallows
{
	public static class ProceduralAudio
	{
		private const int Rate = 22050;

		private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

		public const int SplashVariants = 4;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060002E5, RVA 0x278E030)
			cache.Clear();
		}

		public static AudioClip Splash(int variant)
		{
			// Rebuilt from native code (token 0x060002E6, RVA 0x278E0A8)
			variant = Mathf.Abs(variant) % SplashVariants;
			return Cached("Splash" + variant, () =>
			{
				// Rebuilt from native code (token 0x0600030E, RVA 0x2791CA8)
				Random random = new Random(1234 + variant * 97);
				float[] d = new float[(int)(Rate * 0.95f)];
				float a = MathF.Exp(-2f * MathF.PI * (1800f + variant * 350f) / Rate);
				float hissK = 1f - MathF.Exp(-2f * MathF.PI * 5000f / Rate);
				float hiss = 0f;
				float rumble = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float crack = MathF.Exp(t / -0.025f);
					float slosh = MathF.Sin(t * 37f + variant);
					float attack = Attack(t, 0.008f);
					float bright = hiss + (white - hiss) * hissK;
					rumble += (1f - a) * (white - rumble);
					d[i] = crack * ((bright - hiss) * 3f) + (0.75f + 0.25f * slosh) * (rumble * attack * MathF.Exp(t / -0.22f)) * 1.6f;
					hiss = bright;
				}
				int bubbles = variant * 2 + 7;
				for (int b = 0; b < bubbles; b++)
				{
					AddBubble(d, (float)random.NextDouble() * 0.45f + 0.02f, (float)random.NextDouble() * 1300f + 450f, (float)random.NextDouble() * 0.05f + 0.025f, (float)random.NextDouble() * 0.2f + 0.12f);
				}
				return d;
			});
		}

		public static AudioClip Roar()
		{
			// Rebuilt from native code (token 0x060002E7, RVA 0x278E428)
			return Cached("Roar", () =>
			{
				// Rebuilt from native code (token 0x060002FE, RVA 0x278F884)
				Random random = new Random(77);
				float[] d = new float[Rate * 2];
				float noiseK = 1f - MathF.Exp(-2f * MathF.PI * 1400f / Rate);
				float throatK = 1f - MathF.Exp(-2f * MathF.PI * 900f / Rate);
				float noise = 0f;
				float phase2 = 0f;
				float throat = 0f;
				float phase1 = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					float swell = MathF.Sin(Mathf.Clamp01(t * 0.5f) * MathF.PI);
					float vibrato = MathF.Sin(t * 31f);
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float tremolo = 0.6f + 0.4f * MathF.Sin(t * 2f * MathF.PI * 27f);
					float f = 58f * (1f + 0.35f * swell) * (1f + 0.02f * vibrato);
					phase1 += f / Rate;
					phase2 += f * 1.06f / Rate;
					noise += (white - noise) * noiseK;
					float saw1 = 2f * (phase1 - Mathf.Floor(phase1)) - 1f;
					float saw2 = 2f * (phase2 - Mathf.Floor(phase2)) - 1f;
					float x = (saw1 + saw2 * 0.7f) * tremolo + noise * 1.5f * tremolo;
					throat += (x - throat) * throatK;
					d[i] = Attack(t, 0.25f) * Attack(2f - t, 0.6f) * Tanh(throat * 2.8f);
				}
				return d;
			});
		}

		public static AudioClip Footstep()
		{
			// Rebuilt from native code (token 0x060002E8, RVA 0x278E540)
			return Cached("Footstep", () =>
			{
				// Rebuilt from native code (token 0x060002FF, RVA 0x278FBF0)
				Random random = new Random(5);
				float[] d = new float[(int)(Rate * 0.8f)];
				float scuffK = 1f - MathF.Exp(-2f * MathF.PI * 700f / Rate);
				float phase = 0f;
				float scuff = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += Mathf.Lerp(52f, 30f, Attack(t, 0.25f)) / Rate;
					float thump = MathF.Sin(phase * 2f * MathF.PI) * MathF.Exp(t / -0.13f);
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					scuff += (white - scuff) * scuffK;
					d[i] = Attack(t, 0.004f) * thump * 1.2f + Attack(t, 0.03f) * (scuff * 2.2f) * MathF.Exp(t / -0.2f);
				}
				AddBubble(d, 0.08f, 380f, 0.06f, 0.15f);
				AddBubble(d, 0.17f, 520f, 0.05f, 0.1f);
				return d;
			});
		}

		public static AudioClip Smash()
		{
			// Rebuilt from native code (token 0x060002E9, RVA 0x278E658)
			return Cached("Smash", () =>
			{
				// Rebuilt from native code (token 0x06000300, RVA 0x278FED4)
				Random random = new Random(9);
				float[] d = new float[(int)(Rate * 0.6f)];
				float crunchK = 1f - MathF.Exp(-2f * MathF.PI * 2500f / Rate);
				float phase = 0f;
				float crunch = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += Mathf.Lerp(90f, 40f, Attack(t, 0.2f)) / Rate;
					float thud = MathF.Sin(phase * 2f * MathF.PI) * MathF.Exp(t / -0.12f);
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float decay = MathF.Exp(t / -0.06f);
					crunch += (white - crunch) * crunchK;
					d[i] = Tanh(thud * 2f + crunch * 1.5f * decay);
				}
				return d;
			});
		}

		public static AudioClip Tick()
		{
			// Rebuilt from native code (token 0x060002EA, RVA 0x278E770)
			return Cached("Tick", () =>
			{
				// Rebuilt from native code (token 0x06000301, RVA 0x2790144)
				float[] d = new float[(int)(Rate * 0.04f)];
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					d[i] = MathF.Sin(2f * MathF.PI * 1700f * t) * MathF.Exp(t / -0.008f);
				}
				return d;
			});
		}

		public static AudioClip Chime()
		{
			// Rebuilt from native code (token 0x060002EB, RVA 0x278E888)
			return Cached("Chime", () =>
			{
				// Rebuilt from native code (token 0x06000302, RVA 0x2790228)
				float[] d = new float[(int)(Rate * 2.2f)];
				float[] notes = new float[4] { 1046.5f, 1318.5f, 1568f, 2093f };
				for (int k = 0; k < notes.Length; k++)
				{
					int start = (int)(k * 0.13f * Rate);
					for (int i = 0; start + i < d.Length; i++)
					{
						float t = (float)i / Rate;
						float env = Attack(t, 0.005f) * MathF.Exp(t / -0.55f);
						float w = 2f * MathF.PI * notes[k];
						d[start + i] += env * (MathF.Sin(w * t) + 0.3f * MathF.Sin(w * 2.01f * t)) * 0.3f;
					}
				}
				return d;
			});
		}

		public static AudioClip Tap()
		{
			// Rebuilt from native code (token 0x060002EC, RVA 0x278E9A0)
			return Cached("Tap", () =>
			{
				// Rebuilt from native code (token 0x06000303, RVA 0x279047C)
				Random random = new Random(3);
				float[] d = new float[(int)(Rate * 0.18f)];
				float phase = 0f;
				float click = 0f;
				float last = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += Mathf.Lerp(230f, 150f, Attack(t, 0.05f)) / Rate;
					float knock = MathF.Sin(phase * 2f * MathF.PI) * MathF.Exp(t / -0.028f);
					float ping = MathF.Sin(2f * MathF.PI * 640f * t) * MathF.Exp(t / -0.012f) * 0.35f;
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float snap = MathF.Exp(t / -0.004f);
					click = (click + white - last) * 0.6f;
					last = white;
					d[i] = Attack(t, 0.0015f) * (knock + ping + snap * click * 0.8f);
				}
				return d;
			});
		}

		public static AudioClip Clunk()
		{
			// Rebuilt from native code (token 0x060002ED, RVA 0x278EAB8)
			return Cached("Clunk", () =>
			{
				// Rebuilt from native code (token 0x06000304, RVA 0x279075C)
				float[] d = new float[(int)(Rate * 0.25f)];
				Random random = new Random(5);
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					float ring = MathF.Sin(2f * MathF.PI * 523f * t) * 0.5f + MathF.Sin(2f * MathF.PI * 1372f * t) * 0.3f;
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float knock = white * MathF.Exp(t / -0.006f);
					d[i] = Attack(t, 0.001f) * (ring * MathF.Exp(t / -0.05f) + knock);
				}
				return d;
			});
		}

		public static AudioClip Alarm()
		{
			// Rebuilt from native code (token 0x060002EE, RVA 0x278EBD0)
			return Cached("Alarm", () =>
			{
				// Rebuilt from native code (token 0x06000305, RVA 0x279097C)
				float[] d = new float[(int)(Rate * 0.5f)];
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					float gate = (t < 0.2f || (t > 0.24f && t < 0.44f)) ? 1f : 0f;
					float f = t < 0.22f ? 880f : 660f;
					d[i] = Mathf.Sign(MathF.Sin(2f * MathF.PI * f * t)) * 0.3f * gate;
				}
				return d;
			});
		}

		public static AudioClip TileLight()
		{
			// Rebuilt from native code (token 0x060002EF, RVA 0x278ECE8)
			return Cached("TileLight", () =>
			{
				// Rebuilt from native code (token 0x06000306, RVA 0x2790AB8)
				float[] d = new float[(int)(Rate * 0.45f)];
				float phase = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += 523.25f * Mathf.Lerp(0.82f, 1f, Attack(t, 0.035f)) / Rate;
					float tone = MathF.Sin(phase * 2f * MathF.PI) + MathF.Sin(phase * MathF.PI) * 0.35f + MathF.Sin(phase * 4f * MathF.PI) * 0.12f;
					d[i] = Attack(t, 0.006f) * tone * MathF.Exp(t / -0.11f);
				}
				return d;
			});
		}

		public static AudioClip Zap()
		{
			// Rebuilt from native code (token 0x060002F0, RVA 0x278EE00)
			return Cached("Zap", () =>
			{
				// Rebuilt from native code (token 0x06000307, RVA 0x2790CD8)
				float[] d = new float[(int)(Rate * 0.9f)];
				Random random = new Random(9);
				float phase = 0f;
				float crackle = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += Mathf.Lerp(180f, 50f, Attack(t, 0.9f)) / Rate;
					float buzz = (Mathf.Repeat(phase, 1f) * 2f - 1f) * 0.5f;
					if (random.NextDouble() < 0.02)
					{
						crackle = (float)(random.NextDouble() * 2.0 - 1.0);
					}
					float spark = random.NextDouble() < 0.3 ? 1f : 0f;
					d[i] = Attack(t, 0.002f) * (MathF.Exp(t / -0.3f) * (buzz + crackle * spark));
				}
				return d;
			});
		}

		private static void AddBubble(float[] d, float start, float f0, float tau, float amp)
		{
			// Rebuilt from native code (token 0x060002F1, RVA 0x278EF18)
			int length = (int)(tau * 6f * Rate);
			int at = (int)(start * Rate);
			float phase = 0f;
			for (int i = 0; i < length; i++)
			{
				if (at + i >= d.Length)
				{
					break;
				}
				float t = (float)i / Rate;
				phase += f0 * (1f + t * 0.8f / tau) / Rate;
				float wave = MathF.Sin(phase * 2f * MathF.PI);
				d[at + i] += Attack(t, 0.002f) * (MathF.Exp(-t / tau) * wave) * amp;
			}
		}

		public static AudioClip Whirr()
		{
			// Rebuilt from native code (token 0x060002F2, RVA 0x278F11C)
			return Cached("Whirr", () =>
			{
				// Rebuilt from native code (token 0x06000308, RVA 0x2790F64)
				float[] d = new float[Rate];
				Random random = new Random(11);
				float rumble = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float t = (float)i / Rate;
					float hum = (MathF.Sin(2f * MathF.PI * 110f * t) * 0.5f + Mathf.Sign(MathF.Sin(2f * MathF.PI * 220f * t)) * 0.12f) * 0.6f;
					rumble += (white - rumble) * 0.2f;
					d[i] = hum + (MathF.Sin(2f * MathF.PI * 12f * t) * 0.4f + 0.6f) * rumble * 0.5f;
				}
				return d;
			});
		}

		public static AudioClip Sparkle()
		{
			// Rebuilt from native code (token 0x060002F3, RVA 0x278F234)
			return Cached("Sparkle", () =>
			{
				// Rebuilt from native code (token 0x06000309, RVA 0x2791144)
				float[] d = new float[(int)(Rate * 0.35f)];
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					float tone = MathF.Sin(2f * MathF.PI * 1568f * t) + MathF.Sin(2f * MathF.PI * 3136f * t * 1.003f) * 0.5f + MathF.Sin(2f * MathF.PI * 4704f * t) * 0.25f;
					d[i] = MathF.Exp(t / -0.08f) * Attack(t, 0.002f) * tone;
				}
				return d;
			});
		}

		public static AudioClip Pearl()
		{
			// Rebuilt from native code (token 0x060002F4, RVA 0x278F34C)
			return Cached("Pearl", () =>
			{
				// Rebuilt from native code (token 0x0600030A, RVA 0x27912E8)
				float[] d = new float[(int)(Rate * 0.3f)];
				float phase = 0f;
				float smooth = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					phase += 2f * MathF.PI * (520f + 140f * (1f - MathF.Exp(t / -0.035f))) / Rate;
					float tone = MathF.Sin(phase) + MathF.Sin(phase * 2f) * 0.12f;
					float a = Attack(t, 0.018f);
					float x = MathF.Exp(t / -0.075f) * (a * a * (3f - 2f * a)) * tone;
					smooth += (x - smooth) * 0.35f;
					d[i] = smooth;
				}
				return d;
			});
		}

		public static AudioClip Creak()
		{
			// Rebuilt from native code (token 0x060002F5, RVA 0x278F464)
			return Cached("Creak", () =>
			{
				// Rebuilt from native code (token 0x0600030B, RVA 0x27914B4)
				const float r = 0.993f;
				float[] d = new float[(int)(Rate * 0.5f)];
				Random random = new Random(21);
				float[] modes = new float[3] { 310f, 740f, 1450f };
				float[] gains = new float[3] { 1f, 0.6f, 0.25f };
				float[] y1 = new float[3];
				float[] y2 = new float[3];
				float pulse = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float t = (float)i / Rate;
					pulse += (55f + 70f * MathF.Sin(2f * MathF.PI * t) + 15f * MathF.Sin(t * 37f)) / Rate;
					float kick = 0f;
					if (pulse >= 1f)
					{
						pulse -= 1f;
						kick = 0.6f + 0.4f * (float)random.NextDouble();
					}
					float env = Attack(t, 0.04f) * Attack(0.5f - t, 0.12f);
					float sum = 0f;
					for (int k = 0; k < 3; k++)
					{
						float y = 2f * r * MathF.Cos(2f * MathF.PI * modes[k] / Rate) * y1[k] - r * r * y2[k] + kick;
						y2[k] = y1[k];
						y1[k] = y;
						sum += y * gains[k];
					}
					d[i] = env * sum;
				}
				return d;
			});
		}

		public static AudioClip Snap()
		{
			// Rebuilt from native code (token 0x060002F6, RVA 0x278F57C)
			return Cached("Snap", () =>
			{
				// Rebuilt from native code (token 0x0600030C, RVA 0x2791854)
				float[] d = new float[(int)(Rate * 0.7f)];
				Random random = new Random(17);
				float[] cracks = new float[4] { 0f, 0.018f, 0.041f, 0.07f };
				float phase = 0f;
				float rumble = 0f;
				float last = 0f;
				float click = 0f;
				for (int i = 0; i < d.Length; i++)
				{
					float white = (float)(random.NextDouble() * 2.0 - 1.0);
					float t = (float)i / Rate;
					float crack = 0f;
					for (int k = 0; k < cracks.Length; k++)
					{
						if (t < cracks[k])
						{
							continue;
						}
						crack += (k == 0 ? 1f : 0.5f) * MathF.Exp(-(t - cracks[k]) / (k == 0 ? 0.012f : 0.005f));
					}
					phase += Mathf.Lerp(160f, 70f, Attack(t, 0.08f)) / Rate;
					float body = MathF.Sin(phase * 2f * MathF.PI) * MathF.Exp(t / -0.05f) * 0.6f;
					float grit = Attack(t - 0.03f, 0.06f);
					float fade = MathF.Exp((t - 0.03f) / -0.22f);
					float buzz = 0.6f + 0.4f * MathF.Sin(t * 90f);
					click = (click + white - last) * 0.75f;
					rumble += (white - rumble) * 0.25f;
					d[i] = grit * rumble * fade * buzz * 0.5f + Tanh(body + click * crack * 2.5f);
					last = white;
				}
				return d;
			});
		}

		private static float Attack(float t, float time)
		{
			// Rebuilt from native code (token 0x060002F7, RVA 0x278F0EC)
			if (time <= 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(t / time);
		}

		private static float Tanh(float x)
		{
			// Rebuilt from native code (token 0x060002F8, RVA 0x278F694)
			float e = MathF.Exp(2f * Mathf.Clamp(x, -10f, 10f));
			return (e - 1f) / (e + 1f);
		}

		public static bool Made(AudioClip clip)
		{
			// Rebuilt from native code (token 0x060002F9, RVA 0x278F6D8)
			if (!clip)
			{
				return false;
			}
			return clip.name.StartsWith("Shallows ");
		}

		private static AudioClip Cached(string name, Func<float[]> make)
		{
			// Rebuilt from native code (token 0x060002FA, RVA 0x278E21C)
			if (cache.TryGetValue(name, out AudioClip clip) && clip != null)
			{
				return clip;
			}
			float[] d = make();
			float peak = 0.0001f;
			foreach (float sample in d)
			{
				peak = Mathf.Max(peak, Mathf.Abs(sample));
			}
			float gain = 0.9f / peak;
			for (int i = 0; i < d.Length; i++)
			{
				d[i] *= gain;
			}
			clip = AudioClip.Create("Shallows " + name, d.Length, 1, Rate, false);
			clip.SetData(d, 0);
			cache[name] = clip;
			return clip;
		}
	}
}
