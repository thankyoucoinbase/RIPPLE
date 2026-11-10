using System;
using System.Text;
using UnityEngine;

namespace Shallows
{
	public static class PlayerProfile
	{
		public enum MicMode
		{
			Open = 0,
			PushToTalk = 1,
			PushToMute = 2,
			Muted = 3
		}

		public enum TurnMode
		{
			Smooth = 0,
			Snap = 1,
			None = 2
		}

		public const int MaxNameLength = 12;

		public static readonly string[] MicModeNames = new string[] { "OPEN MIC", "PUSH TO TALK", "PUSH TO MUTE", "MUTED" };

		public static readonly string[] TurnModeNames = new string[] { "SMOOTH", "SNAP", "NO TURN" };

		private static readonly float[] SmoothSpeeds = new float[] { 30f, 45f, 60f, 90f, 120f, 150f, 180f, 240f, 300f, 360f };

		private static readonly float[] SnapAngles = new float[] { 15f, 20f, 25f, 30f, 40f, 45f, 60f, 75f, 90f, 120f };

		public const int DefaultTurnSpeed = 5;

		private static bool loaded;

		private static readonly string[] Blocked = new string[]
		{
			"FUCK", "SHIT", "CUNT", "BITCH", "NIGG", "NIGA", "FAG", "RETARD", "WHORE", "SLUT", "DICK", "COCK", "PUSSY",
			"PENIS", "VAGINA", "RAPE", "NAZI", "HITLER", "KKK", "SEX", "PORN", "ASSHOLE", "TWAT", "WANK", "KYS"
		};

		public static string Name { get; private set; }

		// Rebuilt from native code (token 0x060006A8, RVA 0x2801748): compiler-generated auto-property getter
		// Rebuilt from native code (token 0x060006A9, RVA 0x28017A4): compiler-generated auto-property setter
		public static Vector3Int ColorSteps { get; private set; }

		public static MicMode Mic { get; private set; } = MicMode.Open;

		public static Color Color
		{
			get
			{
				// Rebuilt from native code (token 0x060006AC, RVA 0x27F6F68)
				return new Color(ColorSteps.x / 9f, ColorSteps.y / 9f, ColorSteps.z / 9f, 1f);
			}
		}

		public static TurnMode Turn { get; private set; } = TurnMode.Snap;

		public static int TurnSpeed { get; private set; } = DefaultTurnSpeed;

		public static float SmoothTurnSpeed
		{
			get
			{
				// Rebuilt from native code (token 0x060006B1, RVA 0x2801A54)
				return SmoothSpeeds[Mathf.Clamp(TurnSpeed, 0, 9)];
			}
		}

		public static float SnapTurnAngle
		{
			get
			{
				// Rebuilt from native code (token 0x060006B2, RVA 0x2801B14)
				return SnapAngles[Mathf.Clamp(TurnSpeed, 0, 9)];
			}
		}

		public static string TurnSpeedText
		{
			get
			{
				// Rebuilt from native code (token 0x060006B3, RVA 0x2801BD4)
				if (Turn == TurnMode.Smooth)
				{
					return string.Format("{0:0} DEG A SECOND", SmoothTurnSpeed);
				}
				if (Turn == TurnMode.Snap)
				{
					return string.Format("{0:0} DEG A SNAP", SnapTurnAngle);
				}
				return "";
			}
		}

		public static event Action Changed;

		public static bool MicLive(bool talkHeld)
		{
			// Rebuilt from native code (token 0x060006B4, RVA 0x27F720C)
			if (Mic == MicMode.Open || (Mic == MicMode.PushToTalk && talkHeld))
			{
				return true;
			}
			return Mic == MicMode.PushToMute && !talkHeld;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060006B7, RVA 0x2801D54)
			Changed = null;
			loaded = false;
		}

		public static void Load()
		{
			// Rebuilt from native code (token 0x060006B8, RVA 0x27F5854)
			if (loaded)
			{
				return;
			}
			loaded = true;
			string name = PlayerPrefs.GetString("shallows.name", "");
			Name = name.Length > 0 ? Filter(name) : RandomName();
			ColorSteps = new Vector3Int(PlayerPrefs.GetInt("shallows.r", UnityEngine.Random.Range(2, 10)), PlayerPrefs.GetInt("shallows.g", UnityEngine.Random.Range(2, 10)), PlayerPrefs.GetInt("shallows.b", UnityEngine.Random.Range(2, 10)));
			int legacyMic = PlayerPrefs.GetInt("shallows.mic", 1);
			Mic = (MicMode)Mathf.Clamp(PlayerPrefs.GetInt("shallows.micmode", legacyMic == 1 ? (int)MicMode.Open : (int)MicMode.Muted), 0, 3);
			Turn = (TurnMode)Mathf.Clamp(PlayerPrefs.GetInt("shallows.turn", (int)TurnMode.Snap), 0, 2);
			TurnSpeed = Mathf.Clamp(PlayerPrefs.GetInt("shallows.turnspeed", DefaultTurnSpeed), 0, 9);
			Save();
		}

		public static void SetName(string name)
		{
			// Rebuilt from native code (token 0x060006B9, RVA 0x28020F4)
			Load();
			Name = Filter(name);
			Save();
			Changed?.Invoke();
		}

		public static void SetColorStep(int channel, int value)
		{
			// Rebuilt from native code (token 0x060006BA, RVA 0x28021C4)
			Load();
			Vector3Int steps = ColorSteps;
			steps[Mathf.Clamp(channel, 0, 2)] = Mathf.Clamp(value, 0, 9);
			ColorSteps = steps;
			Save();
			Changed?.Invoke();
		}

		public static void SetMic(MicMode mode)
		{
			// Rebuilt from native code (token 0x060006BB, RVA 0x2802340)
			Load();
			Mic = mode;
			Save();
			Changed?.Invoke();
		}

		public static void SetTurn(TurnMode mode)
		{
			// Rebuilt from native code (token 0x060006BC, RVA 0x28023FC)
			Load();
			Turn = mode;
			Save();
			Changed?.Invoke();
		}

		public static void SetTurnSpeed(int step)
		{
			// Rebuilt from native code (token 0x060006BD, RVA 0x28024B8)
			Load();
			TurnSpeed = Mathf.Clamp(step, 0, 9);
			Save();
			Changed?.Invoke();
		}

		public static Color StepsToColor(Vector3Int steps)
		{
			// Rebuilt from native code (token 0x060006BE, RVA 0x28018C4)
			return new Color(steps.x / 9f, steps.y / 9f, steps.z / 9f, 1f);
		}

		public static string Filter(string name)
		{
			// Rebuilt from native code (token 0x060006BF, RVA 0x27F7420)
			StringBuilder builder = new StringBuilder();
			foreach (char c in (name ?? "").ToUpperInvariant())
			{
				if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
				{
					builder.Append(c);
				}
			}
			string filtered = builder.Length > MaxNameLength ? builder.ToString(0, MaxNameLength) : builder.ToString();
			if (filtered.Length == 0 || IsRude(filtered))
			{
				return RandomName();
			}
			return filtered;
		}

		private static bool IsRude(string name)
		{
			// Rebuilt from native code (token 0x060006C0, RVA 0x2802584)
			string plain = name.Replace('0', 'O').Replace('1', 'I').Replace('3', 'E').Replace('4', 'A').Replace('5', 'S').Replace('7', 'T').Replace('8', 'B');
			foreach (string word in Blocked)
			{
				if (name.Contains(word) || plain.Contains(word))
				{
					return true;
				}
			}
			return false;
		}

		private static string RandomName()
		{
			// Rebuilt from native code (token 0x060006C1, RVA 0x2801DB4)
			return "GORILLA" + UnityEngine.Random.Range(1000, 10000);
		}

		private static void Save()
		{
			// Rebuilt from native code (token 0x060006C2, RVA 0x2801E28)
			PlayerPrefs.SetString("shallows.name", Name);
			PlayerPrefs.SetInt("shallows.r", ColorSteps.x);
			PlayerPrefs.SetInt("shallows.g", ColorSteps.y);
			PlayerPrefs.SetInt("shallows.b", ColorSteps.z);
			PlayerPrefs.SetInt("shallows.micmode", (int)Mic);
			PlayerPrefs.SetInt("shallows.turn", (int)Turn);
			PlayerPrefs.SetInt("shallows.turnspeed", TurnSpeed);
			PlayerPrefs.Save();
		}
	}
}
