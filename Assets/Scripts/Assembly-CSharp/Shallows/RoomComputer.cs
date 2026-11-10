using System;
using UnityEngine;

namespace Shallows
{
	public class RoomComputer : MonoBehaviour
	{
		private enum Mode
		{
			Room = 0,
			Name = 1,
			Color = 2,
			Mic = 3,
			Turn = 4
		}

		public Material buttonMaterial;

		[Tooltip("A cube that shows your fur color.")]
		public Renderer swatch;

		public float keySize = 0.062f;

		public float keySpacing = 0.078f;

		[Tooltip("How far below this object the floor is (where the lifeguard model stands).")]
		public float floorBelow = 0.62f;

		private const float KeyboardTilt = 17.5f;

		private static readonly Color ModeOn = new Color(1f, 0.95f, 0.55f, 1f);

		private static readonly string[] ModeNames = new string[] { "ROOM", "NAME", "COLOR", "MIC", "TURN" };

		private Mode mode;

		private string typed = "";

		private int colorChannel;

		private TextMesh screen;

		private TextMesh typedLine;

		private Vector3 typedHome;

		private Vector3 joinSpot;

		private bool movesTypedLine;

		private PokeButton[] modeButtons;

		private MaterialPropertyBlock block;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly string[] Rows = new string[] { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

		private void Start()
		{
			// Rebuilt from native code (token 0x060006C4, RVA 0x2802F08)
			PlayerProfile.Load();
			block = new MaterialPropertyBlock();
			if (!WearLifeguardModel())
			{
				BuildBlockKeys();
			}
			PlayerProfile.Changed += Refresh;
			Refresh();
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060006C5, RVA 0x2804CB8)
			PlayerProfile.Changed -= Refresh;
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x060006C6, RVA 0x2804D58)
			return LifeguardProps.Dress(transform, "LifeguardComputer", Vector3.down * floorBelow, Quaternion.identity);
		}

		private bool WearLifeguardModel()
		{
			// Rebuilt from native code (token 0x060006C7, RVA 0x2803010)
			Transform model = WearModel();
			if (!model)
			{
				return false;
			}
			screen = LifeguardProps.Text(transform, model, "Screen Text", "Screen Text", PixelFonts.Screen, 0.03f, new Color(0.55f, 1f, 0.6f, 1f), TextAnchor.UpperLeft);
			typedLine = LifeguardProps.Text(transform, model, "Typed Text", "Typed", PixelFonts.Screen, 0.034f, new Color(0.85f, 1f, 0.85f, 1f), TextAnchor.MiddleLeft);
			typedHome = typedLine.transform.localPosition;
			movesTypedLine = true;
			joinSpot = Vector3.Lerp(typedHome, screen.transform.localPosition, 0.5f);
			Vector3 sink = model.parent.TransformVector(Quaternion.Euler(-KeyboardTilt, 0f, 0f) * Vector3.up * -0.008f);
			string[] names = ModeNames;
			modeButtons = new PokeButton[names.Length];
			for (int i = 0; i < names.Length; i++)
			{
				Mode m = (Mode)i;
				Wire(names[i], () => SetMode(m));
				Transform key = LifeguardProps.Find(model, "Key " + names[i]);
				if (key)
				{
					modeButtons[i] = key.GetComponent<PokeButton>();
				}
			}
			foreach (string row in Rows)
			{
				foreach (char c in row)
				{
					Wire(c.ToString(), () => Type(c));
				}
			}
			Wire("DEL", Delete);
			Wire("ENTER", Enter);
			Wire("PUBLIC", GoPublic);
			Transform swatchPart = LifeguardProps.Find(model, "Swatch");
			if (swatchPart)
			{
				swatch = swatchPart.GetComponent<Renderer>();
			}
			return true;

			void Wire(string label, Action action)
			{
				// Rebuilt from native code (token 0x060006D4, RVA 0x2804F14)
				Transform part = LifeguardProps.Find(model, "Key " + label);
				if (!part)
				{
					Debug.LogWarning("Shallows: the lifeguard computer has no key " + label);
					return;
				}
				LifeguardProps.Button(part, sink).onPress = action;
			}
		}

		private void BuildBlockKeys()
		{
			// Rebuilt from native code (token 0x060006C8, RVA 0x2803918)
			screen = WorldText.Create("Screen Text", transform, new Vector3(-0.42f, 0.44f, -0.02f), "", 0.034f, new Color(0.55f, 1f, 0.6f, 1f), TextAnchor.UpperLeft, TextAlignment.Left);
			typedLine = WorldText.Create("Typed", transform, new Vector3(-0.42f, 0.1f, -0.02f), "", 0.04f, new Color(1f, 1f, 1f, 1f), TextAnchor.MiddleLeft, TextAlignment.Left);
			string[] names = ModeNames;
			for (int i = 0; i < names.Length; i++)
			{
				Mode m = (Mode)i;
				Key(names[i], new Vector3(-0.36f + i * 0.18f, -0.02f, 0f), new Vector3(0.15f, keySize, 0.04f), () => SetMode(m), new Color(0.95f, 0.6f, 0.2f, 1f));
			}
			for (int r = 0; r < Rows.Length; r++)
			{
				string row = Rows[r];
				float left = r * 0.02f + keySpacing * ((1 - row.Length) / 2f);
				for (int i = 0; i < row.Length; i++)
				{
					char ch = row[i];
					Key(ch.ToString(), new Vector3(left + keySpacing * i, -0.12f - keySpacing * r, 0f), Vector3.one * keySize, () => Type(ch), null);
				}
			}
			float y = -0.12f - keySpacing * Rows.Length;
			Key("DEL", new Vector3(-0.3f, y, 0f), new Vector3(0.16f, keySize, 0.04f), Delete, new Color(0.85f, 0.3f, 0.3f, 1f));
			Key("ENTER", new Vector3(0.04f, y, 0f), new Vector3(0.24f, keySize, 0.04f), Enter, new Color(0.3f, 0.8f, 0.4f, 1f));
			Key("PUBLIC", new Vector3(0.33f, y, 0f), new Vector3(0.2f, keySize, 0.04f), GoPublic, new Color(0.6f, 0.45f, 0.9f, 1f));
		}

		private void Key(string label, Vector3 position, Vector3 size, Action action, Color? color)
		{
			// Rebuilt from native code (token 0x060006C9, RVA 0x28050CC)
			GameObject key = GameObject.CreatePrimitive(PrimitiveType.Cube);
			key.name = "Key " + label;
			key.layer = 2;
			key.transform.SetParent(transform, false);
			key.transform.localPosition = position + new Vector3(0f, 0f, -0.03f);
			key.transform.localScale = new Vector3(size.x, size.y, 0.04f);
			key.GetComponent<BoxCollider>().isTrigger = true;
			if (buttonMaterial)
			{
				key.GetComponent<MeshRenderer>().sharedMaterial = buttonMaterial;
			}
			PokeButton button = key.AddComponent<PokeButton>();
			if (color.HasValue)
			{
				button.color = color.Value;
			}
			button.onPress = action;
			WorldText.Create("Label", transform, key.transform.localPosition + new Vector3(0f, 0f, -0.025f), label, label.Length > 1 ? 0.026f : 0.036f, new Color(1f, 1f, 1f, 1f), TextAnchor.MiddleCenter, TextAlignment.Center);
		}

		private void SetMode(Mode m)
		{
			// Rebuilt from native code (token 0x060006CA, RVA 0x2805414)
			mode = m;
			typed = "";
			Refresh();
		}

		private void Type(char ch)
		{
			// Rebuilt from native code (token 0x060006CB, RVA 0x2805478)
			if (mode == Mode.Turn)
			{
				if (ch >= 'A' && ch <= 'C')
				{
					PlayerProfile.SetTurn((PlayerProfile.TurnMode)(ch - 'A'));
				}
				else if (ch >= '0' && ch <= '9')
				{
					PlayerProfile.SetTurnSpeed(ch - '0');
				}
				return;
			}
			if (mode == Mode.Mic)
			{
				if (ch >= '1' && ch <= '4')
				{
					PlayerProfile.SetMic((PlayerProfile.MicMode)(ch - '1'));
				}
				return;
			}
			if (mode == Mode.Color)
			{
				if (ch == 'R')
				{
					colorChannel = 0;
				}
				else if (ch == 'G')
				{
					colorChannel = 1;
				}
				else if (ch == 'B')
				{
					colorChannel = 2;
				}
				else if (ch >= '0' && ch <= '9')
				{
					PlayerProfile.SetColorStep(colorChannel, ch - '0');
				}
				Refresh();
				return;
			}
			if (typed.Length < PlayerProfile.MaxNameLength)
			{
				typed += ch;
			}
			Refresh();
		}

		private void Delete()
		{
			// Rebuilt from native code (token 0x060006CC, RVA 0x280566C)
			if (typed.Length > 0)
			{
				typed = typed.Substring(0, typed.Length - 1);
			}
			Refresh();
		}

		private void Enter()
		{
			// Rebuilt from native code (token 0x060006CD, RVA 0x28056C0)
			if (mode == Mode.Name)
			{
				if (typed.Length > 0)
				{
					PlayerProfile.SetName(typed);
				}
			}
			else if (mode == Mode.Room && typed.Length > 0)
			{
				NetworkManager network = NetworkManager.I;
				if (network)
				{
					network.JoinPrivate(typed);
				}
			}
			typed = "";
			Refresh();
		}

		private void GoPublic()
		{
			// Rebuilt from native code (token 0x060006CE, RVA 0x2805828)
			if (NetworkManager.I)
			{
				NetworkManager.I.JoinPublic();
			}
			typed = "";
			Refresh();
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060006CF, RVA 0x2805970)
			if (Time.frameCount % 20 == 0)
			{
				Refresh();
			}
		}

		private void Refresh()
		{
			// Rebuilt from native code (token 0x060006D0, RVA 0x2804104)
			if (!screen)
			{
				return;
			}
			string status = NetworkManager.I ? NetworkManager.I.Status : "SINGLE PLAYER";
			string prompt = "";
			string text;
			switch (mode)
			{
			case Mode.Room:
				text = status;
				prompt = "JOIN: ";
				break;
			case Mode.Name:
				text = "NAME: " + PlayerProfile.Name;
				prompt = "NEW NAME: ";
				break;
			case Mode.Color:
			{
				Vector3Int steps = PlayerProfile.ColorSteps;
				string[] channels = new string[] { "RED", "GREEN", "BLUE" };
				text = string.Format("COLOR\nRED {0}  GREEN {1}  BLUE {2}\n\nPRESS R, G OR B, THEN 0-9.\nCHANGING: {3}", steps.x, steps.y, steps.z, channels[colorChannel]);
				break;
			}
			case Mode.Turn:
			{
				PlayerProfile.TurnMode turn = PlayerProfile.Turn;
				text = "TURN\n";
				for (int i = 0; i < PlayerProfile.TurnModeNames.Length; i++)
				{
					text += string.Format("\n{0} {1} {2}", (int)turn == i ? ">" : " ", (char)('A' + i), PlayerProfile.TurnModeNames[i]);
				}
				if (turn != PlayerProfile.TurnMode.None)
				{
					text += string.Format("\n\nSPEED {0}: {1}", PlayerProfile.TurnSpeed, PlayerProfile.TurnSpeedText);
				}
				typedLine.text = "A/B/C: TURN  0-9: SPEED";
				break;
			}
			default:
			{
				PlayerProfile.MicMode mic = PlayerProfile.Mic;
				text = "MIC\n";
				for (int i = 0; i < PlayerProfile.MicModeNames.Length; i++)
				{
					text += string.Format("\n{0} {1} {2}", (int)mic == i ? ">" : " ", i + 1, PlayerProfile.MicModeNames[i]);
				}
				if (mic == PlayerProfile.MicMode.PushToMute)
				{
					typedLine.text = "HOLD A/B/X/Y TO MUTE";
				}
				else if (mic == PlayerProfile.MicMode.PushToTalk)
				{
					typedLine.text = "HOLD A/B/X/Y TO TALK";
				}
				else
				{
					typedLine.text = "";
				}
				break;
			}
			}
			screen.text = text;
			if (mode == Mode.Room || mode == Mode.Name)
			{
				typedLine.text = prompt + typed + "_";
			}
			else if (mode == Mode.Color)
			{
				typedLine.text = "";
			}
			if (movesTypedLine)
			{
				typedLine.transform.localPosition = mode == Mode.Room ? joinSpot : typedHome;
			}
			if (modeButtons != null)
			{
				for (int i = 0; i < modeButtons.Length; i++)
				{
					if (modeButtons[i])
					{
						modeButtons[i].SetIdleColor(i == (int)mode ? ModeOn : new Color(1f, 1f, 1f, 1f));
					}
				}
			}
			if (!swatch)
			{
				return;
			}
			if (block == null)
			{
				block = new MaterialPropertyBlock();
			}
			swatch.GetPropertyBlock(block);
			block.SetColor(BaseColorId, PlayerProfile.Color);
			swatch.SetPropertyBlock(block);
		}
	}
}
