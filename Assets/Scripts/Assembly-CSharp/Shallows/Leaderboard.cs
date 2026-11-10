using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Shallows
{
	public class Leaderboard : MonoBehaviour
	{
		private struct Entry
		{
			public string name;

			public float time;
		}

		public TextMesh board;

		public int size = 10;

		private const string Key = "shallows.leaderboard";

		private static readonly List<Entry> entries = new List<Entry>();

		private static bool loaded;

		private bool onTV;

		private static readonly string[] Ordinals = new string[] { "1ST", "2ND", "3RD", "4TH", "5TH", "6TH", "7TH", "8TH", "9TH", "10TH" };

		public static Leaderboard I { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000203, RVA 0x277EEB8)
			entries.Clear();
			loaded = false;
			I = null;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000204, RVA 0x277EF98)
			I = this;
			onTV = WearModel();
			Load();
			Draw();
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x06000205, RVA 0x277F084)
			Transform spot = transform.Find("Board");
			if (!spot || !board)
			{
				return null;
			}
			Vector3 scale = spot.localScale;
			Quaternion rotation = spot.localRotation * (scale.x < scale.z ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.identity);
			Vector3 position = spot.localPosition + rotation * Vector3.forward * (Mathf.Min(scale.x, scale.z) * 0.5f);
			Transform model = LifeguardProps.Dress(transform, "LifeguardLeaderboard", position, rotation, board.transform);
			if (!model)
			{
				return null;
			}
			TextMesh title = transform.Find("Title") ? transform.Find("Title").GetComponent<TextMesh>() : null;
			if (!title)
			{
				title = WorldText.Create("Title", transform, Vector3.zero, "", 0.1f, Color.white, TextAnchor.UpperCenter, TextAlignment.Center);
			}
			title.text = "FASTEST ESCAPES";
			PixelFonts.Use(title, PixelFonts.Screen, 0.11f, 1f);
			title.anchor = TextAnchor.UpperCenter;
			title.color = new Color(0.75f, 1f, 0.7f, 1f);
			Transform titleSpot = LifeguardProps.Find(model, "Title Text");
			if (titleSpot)
			{
				title.transform.position = titleSpot.position;
			}
			title.transform.rotation = model.parent.rotation;
			Transform timesSpot = LifeguardProps.Find(model, "Times Text");
			if (timesSpot)
			{
				board.transform.position = timesSpot.position;
			}
			board.transform.rotation = model.parent.rotation;
			board.anchor = TextAnchor.UpperLeft;
			PixelFonts.Use(board, PixelFonts.Screen, 0.058f, 1.12f);
			board.color = new Color(0.55f, 1f, 0.6f, 1f);
			return model;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000206, RVA 0x2780084)
			PlayFabClient.BoardChanged += Draw;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000207, RVA 0x2780128)
			PlayFabClient.BoardChanged -= Draw;
		}

		private List<Entry> World()
		{
			// Rebuilt from native code (token 0x06000208, RVA 0x27801CC)
			List<PlayFabClient.BoardEntry> remote = PlayFabClient.Board;
			if (remote == null)
			{
				return null;
			}
			List<Entry> list = new List<Entry>();
			foreach (PlayFabClient.BoardEntry e in remote)
			{
				if (!e.mine)
				{
					list.Add(new Entry { name = e.name, time = e.time });
				}
			}
			if (PlayFabClient.MyBest > 0f)
			{
				list.Add(new Entry { name = PlayFabClient.BoardName ?? PlayerProfile.Name, time = PlayFabClient.MyBest });
			}
			list.Sort((a, b) => a.time.CompareTo(b.time));
			if (list.Count > size)
			{
				list.RemoveRange(size, list.Count - size);
			}
			return list;
		}

		public static void Submit(string name, float time)
		{
			// Rebuilt from native code (token 0x06000209, RVA 0x2779FAC)
			Load();
			name = string.IsNullOrWhiteSpace(name) ? "GORILLA" : name.Trim().Replace("|", "").Replace(";", "");
			if (time <= 0f)
			{
				return;
			}
			int index = entries.FindIndex(e => e.name == name);
			if (index >= 0)
			{
				if (entries[index].time <= time)
				{
					return;
				}
				entries.RemoveAt(index);
			}
			entries.Add(new Entry { name = name, time = time });
			entries.Sort((a, b) => a.time.CompareTo(b.time));
			int keep = I ? I.size : 10;
			if (entries.Count > keep)
			{
				entries.RemoveRange(keep, entries.Count - keep);
			}
			Save();
			if (I)
			{
				I.Draw();
			}
		}

		public static int Rank(string name)
		{
			// Rebuilt from native code (token 0x0600020A, RVA 0x27809C8)
			if (I && PlayFabClient.Board != null)
			{
				List<Entry> world = I.World();
				float mine = PlayFabClient.MyBest;
				if (mine <= 0f)
				{
					return 0;
				}
				return world.FindIndex(e => e.time == mine) + 1;
			}
			Load();
			return entries.FindIndex(e => e.name == name) + 1;
		}

		private static void Load()
		{
			// Rebuilt from native code (token 0x0600020B, RVA 0x277F850)
			if (loaded)
			{
				return;
			}
			loaded = true;
			entries.Clear();
			foreach (string part in PlayerPrefs.GetString(Key, "").Split(';'))
			{
				int bar = part.IndexOf('|');
				if (bar > 0 && float.TryParse(part.Substring(bar + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float time))
				{
					entries.Add(new Entry { name = part.Substring(0, bar), time = time });
				}
			}
			entries.Sort((a, b) => a.time.CompareTo(b.time));
		}

		private static void Save()
		{
			// Rebuilt from native code (token 0x0600020C, RVA 0x27806C0)
			List<string> parts = new List<string>();
			foreach (Entry e in entries)
			{
				parts.Add(e.name + "|" + e.time.ToString("0.00", CultureInfo.InvariantCulture));
			}
			PlayerPrefs.SetString(Key, string.Join(";", parts));
			PlayerPrefs.Save();
		}

		private void Draw()
		{
			// Rebuilt from native code (token 0x0600020D, RVA 0x277FBE0)
			if (!board)
			{
				return;
			}
			List<Entry> world = World();
			List<Entry> shown = world ?? entries;
			if (onTV)
			{
				board.text = Places(shown);
				return;
			}
			StringBuilder sb = new StringBuilder(world != null ? "WORLD'S FASTEST ESCAPES\n\n" : "FASTEST ESCAPES\n(this headset)\n\n");
			if (shown.Count == 0)
			{
				sb.Append("nobody yet\nreach the bananas!");
			}
			for (int i = 0; i < shown.Count; i++)
			{
				sb.Append(string.Format("{0,2}. {1,-12} {2}\n", i + 1, shown[i].name, Format(shown[i].time)));
			}
			board.text = sb.ToString();
		}

		private string Places(List<Entry> shown)
		{
			// Rebuilt from native code (token 0x0600020E, RVA 0x2780CB4)
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < size && i < Ordinals.Length; i++)
			{
				if (i > 0)
				{
					sb.Append('\n');
				}
				string line;
				if (i < shown.Count)
				{
					string name = shown[i].name.Length > 12 ? shown[i].name.Substring(0, 12) : shown[i].name;
					line = string.Format("{0,-8}{1,-12}{2,21}", Ordinals[i], name, Format(shown[i].time));
				}
				else
				{
					line = string.Format("{0,-8}---", Ordinals[i]);
				}
				sb.Append(line);
			}
			return sb.ToString();
		}

		public static string Format(float seconds)
		{
			// Rebuilt from native code (token 0x0600020F, RVA 0x2780F2C)
			int minutes = Mathf.FloorToInt(seconds / 60f);
			return string.Format("{0}:{1:00.0}", minutes, seconds - minutes * 60f);
		}
	}
}
