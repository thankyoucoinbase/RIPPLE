using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class PlayerBoard : MonoBehaviour
	{
		private struct Entry
		{
			public string key;

			public string id;

			public string name;

			public Color color;

			public float voice;

			public bool you;

			public NetAvatar avatar;
		}

		private class Row
		{
			public Renderer swatch;

			public TextMesh name;

			public TextMesh you;

			public Transform mic;

			public PokeButton mute;

			public PokeButton report;

			public float talkUntil;

			public float pop;

			public float popSpeed;

			public string shownName;

			public string key;

			public Color shownColor = Color.clear;
		}

		public const string Name = "Player Board";

		private const string Model = "PlayerBoard";

		public const int Rows = 10;

		public static readonly string[] Categories = new string[] { "CHEATING", "BULLYING", "HATE SPEECH", "BAD NAME", "TROLLING" };

		public static readonly Vector3 BesideMirror = new Vector3(3.7f, 0f, -0.04f);

		private const string MutedPref = "shallows.muted";

		private const int MutedKept = 200;

		private const float TalkLevel = 0.2f;

		private const float TalkHold = 0.3f;

		private const float PanelReach = 4f;

		private static readonly Color Ink = new Color(0.16f, 0.11f, 0.08f, 1f);

		private static readonly Color Faded = new Color(0.16f, 0.11f, 0.08f, 0.4f);

		private static readonly Color MutedTint = new Color(1f, 0.5f, 0.4f, 1f);

		private static readonly Color DoneTint = new Color(0.5f, 0.5f, 0.5f, 1f);

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly HashSet<string> muted = new HashSet<string>();

		private static readonly HashSet<string> reported = new HashSet<string>();

		private static bool mutedLoaded;

		private readonly List<Entry> entries = new List<Entry>();

		private Row[] rows;

		private MaterialPropertyBlock block;

		private Transform panel;

		private PokeButton[] categoryKeys;

		private TextMesh panelText;

		private Entry target;

		private bool panelOpen;

		private bool sending;

		private float panelShown;

		private float panelSpeed;

		private float closeAt;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600067F, RVA 0x27FD120)
			muted.Clear();
			reported.Clear();
			mutedLoaded = false;
		}

		public static PlayerBoard Place(GameManager game, Mirror mirror)
		{
			// Rebuilt from native code (token 0x06000680, RVA 0x27FD1C0)
			if (!mirror)
			{
				return null;
			}
			if (!Resources.Load<GameObject>("Lifeguard/" + Model))
			{
				return null;
			}
			Vector3 at = mirror.transform.TransformPoint(BesideMirror);
			if (game && game.spawnPoint)
			{
				at.y = game.spawnPoint.position.y;
			}
			GameObject gameObject = new GameObject(Name);
			gameObject.transform.SetPositionAndRotation(at, mirror.transform.rotation * Quaternion.Euler(0f, 180f, 0f));
			PlayerBoard board = gameObject.AddComponent<PlayerBoard>();
			board.WearModel();
			return board;
		}

		public bool Dress()
		{
			// Rebuilt from native code (token 0x06000681, RVA 0x27FD678)
			int childCount = transform.childCount;
			if (!WearModel())
			{
				return false;
			}
			return transform.childCount != childCount;
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x06000682, RVA 0x27FD4D8)
			return LifeguardProps.Dress(transform, Model, Vector3.zero, Quaternion.identity);
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000683, RVA 0x27FD730)
			PlayerProfile.Load();
			LoadMuted();
			NetBridge.RoomJoined += ForgetRoom;
			Transform model = WearModel();
			if (!model)
			{
				return;
			}
			Transform parent = model.parent;
			Vector3 sink = parent.TransformVector(Vector3.forward * 0.012f);
			block = new MaterialPropertyBlock();
			rows = new Row[Rows];
			for (int i = 0; i < Rows; i++)
			{
				Row row = new Row();
				rows[i] = row;
				int index = i;
				Transform swatch = LifeguardProps.Find(model, "Swatch " + i);
				row.swatch = swatch ? swatch.GetComponent<Renderer>() : null;
				row.name = LifeguardProps.Text(transform, model, "Name " + i, "Name " + i, PixelFonts.Sign, 0.026f, Ink, TextAnchor.MiddleLeft);
				row.you = LifeguardProps.Text(transform, model, "You " + i, "You " + i, PixelFonts.Sign, 0.024f, Ink, TextAnchor.MiddleCenter);
				row.you.text = "YOU";
				row.mic = Pivot(LifeguardProps.Find(model, "Mic " + i), "Mic Pivot " + i);
				row.mute = Key(model, "Mute " + i, sink, () => ToggleMute(index));
				row.report = Key(model, "Report " + i, sink, () => OpenReport(index));
				Clear(row);
			}
			Transform hinge = LifeguardProps.Find(model, "Report Hinge");
			panel = new GameObject("Report Panel").transform;
			panel.SetParent(model, false);
			if (hinge)
			{
				panel.position = hinge.position;
			}
			panel.rotation = parent.rotation;
			List<Transform> moving = new List<Transform>();
			foreach (Transform child in model)
			{
				if (child.name.StartsWith("Report Panel") || child.name.StartsWith("Category") || child.name == "Cancel")
				{
					moving.Add(child);
				}
			}
			foreach (Transform part in moving)
			{
				part.SetParent(panel, true);
			}
			categoryKeys = new PokeButton[Categories.Length];
			for (int i = 0; i < Categories.Length; i++)
			{
				int category = i;
				categoryKeys[i] = Key(model, "Category " + i, sink, () => Send(category));
			}
			Key(model, "Cancel", sink, () => panelOpen = false);
			panelText = LifeguardProps.Text(transform, model, "Report Text", "Report Text", PixelFonts.Sign, 0.026f, Ink, TextAnchor.MiddleCenter, 1.35f);
			panelText.transform.SetParent(panel, true);
			panel.localScale = new Vector3(0f, 1f, 1f);
			panel.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000684, RVA 0x27FEEC0)
			NetBridge.RoomJoined -= ForgetRoom;
		}

		private static void ForgetRoom(bool fresh)
		{
			// Rebuilt from native code (token 0x06000685, RVA 0x27FEF60)
			muted.RemoveWhere(k => k.StartsWith("#"));
			reported.RemoveWhere(k => k.StartsWith("#"));
		}

		private static PokeButton Key(Transform model, string part, Vector3 sink, Action press)
		{
			// Rebuilt from native code (token 0x06000686, RVA 0x27FEC0C)
			Transform found = LifeguardProps.Find(model, part);
			if (!found)
			{
				return null;
			}
			PokeButton button = LifeguardProps.Button(found, sink);
			button.onPress = press;
			button.repeat = false;
			return button;
		}

		private static Transform Pivot(Transform part, string name)
		{
			// Rebuilt from native code (token 0x06000687, RVA 0x27FEA0C)
			if (!part)
			{
				return null;
			}
			Renderer renderer = part.GetComponent<Renderer>();
			Transform pivot = new GameObject(name).transform;
			pivot.SetParent(part.parent, false);
			pivot.position = renderer ? renderer.bounds.center : part.position;
			pivot.rotation = part.parent.rotation;
			part.SetParent(pivot, true);
			pivot.localScale = Vector3.zero;
			pivot.gameObject.SetActive(false);
			return pivot;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000688, RVA 0x27FF150)
			if (rows == null)
			{
				return;
			}
			Gather();
			for (int i = 0; i < Rows; i++)
			{
				if (i < entries.Count)
				{
					Show(rows[i], entries[i]);
				}
				else
				{
					Clear(rows[i]);
				}
				Pop(rows[i]);
			}
			UpdatePanel();
		}

		private void Gather()
		{
			// Rebuilt from native code (token 0x06000689, RVA 0x27FF288)
			entries.Clear();
			NetworkManager network = NetworkManager.I;
			float voice = network && !NetBridge.Silenced ? VoiceLevel.Opening(network.MicLevel) : 0f;
			entries.Add(new Entry
			{
				key = "you",
				name = PlayerProfile.Name,
				color = PlayerProfile.Color,
				voice = voice,
				you = true
			});
			int first = entries.Count;
			foreach (NetAvatar avatar in NetAvatar.Remotes)
			{
				if (!avatar || !avatar.Object || !avatar.Object.IsValid)
				{
					continue;
				}
				string id = avatar.UserId;
				string key = string.IsNullOrEmpty(id) ? "#" + avatar.PlayerId : id;
				avatar.Muted = muted.Contains(key);
				entries.Add(new Entry
				{
					key = key,
					id = id,
					name = PlayerProfile.Filter(avatar.PlayerName),
					color = avatar.FurColor,
					voice = avatar.VoiceLoudness,
					avatar = avatar
				});
			}
			entries.Sort(first, entries.Count - first, Comparer<Entry>.Create((x, y) => x.avatar.PlayerId.CompareTo(y.avatar.PlayerId)));
			if (entries.Count > Rows)
			{
				entries.RemoveRange(Rows, entries.Count - Rows);
			}
		}

		private void Show(Row row, Entry e)
		{
			// Rebuilt from native code (token 0x0600068A, RVA 0x27FFBA8)
			bool isMuted = !e.you && muted.Contains(e.key);
			if (row.shownName != e.name || row.key != e.key)
			{
				row.shownName = e.name;
				row.name.text = e.name;
			}
			row.key = e.key;
			row.name.color = isMuted ? Faded : Ink;
			if (row.swatch)
			{
				if (!row.swatch.enabled)
				{
					row.swatch.enabled = true;
				}
				if (row.shownColor != e.color)
				{
					row.shownColor = e.color;
					row.swatch.GetPropertyBlock(block);
					block.SetColor(BaseColorId, e.color);
					row.swatch.SetPropertyBlock(block);
				}
			}
			if (row.you.gameObject.activeSelf != e.you)
			{
				row.you.gameObject.SetActive(e.you);
			}
			SetKey(row.mute, !e.you, isMuted ? MutedTint : Color.white);
			SetKey(row.report, !e.you, reported.Contains(e.key) ? DoneTint : Color.white);
			if (e.voice > TalkLevel)
			{
				row.talkUntil = Time.time + TalkHold;
			}
		}

		private void Clear(Row row)
		{
			// Rebuilt from native code (token 0x0600068B, RVA 0x27FED20)
			row.key = null;
			if (row.shownName != "")
			{
				row.shownName = "";
				row.name.text = "";
			}
			if (row.swatch && row.swatch.enabled)
			{
				row.swatch.enabled = false;
			}
			if (row.you.gameObject.activeSelf)
			{
				row.you.gameObject.SetActive(false);
			}
			SetKey(row.mute, false, Color.white);
			SetKey(row.report, false, Color.white);
			row.talkUntil = 0f;
		}

		private static void SetKey(PokeButton key, bool shown, Color idle)
		{
			// Rebuilt from native code (token 0x0600068C, RVA 0x28004F8)
			if (!key)
			{
				return;
			}
			if (key.gameObject.activeSelf != shown)
			{
				key.gameObject.SetActive(shown);
			}
			if (shown && key.color != idle)
			{
				key.SetIdleColor(idle);
			}
		}

		private void Pop(Row row)
		{
			// Rebuilt from native code (token 0x0600068D, RVA 0x27FFFF8)
			if (!row.mic)
			{
				return;
			}
			float target = Time.time < row.talkUntil ? 1f : 0f;
			float dt = Mathf.Min(Time.deltaTime, 0.05f);
			row.popSpeed = (row.popSpeed + (target - row.pop) * 700f * dt) * MathF.Exp(-22f * dt);
			row.pop = Mathf.Max(0f, row.pop + row.popSpeed * dt);
			if (target == 0f && row.pop < 0.02f)
			{
				row.pop = 0f;
				row.popSpeed = 0f;
			}
			bool shown = row.pop > 0f;
			if (row.mic.gameObject.activeSelf != shown)
			{
				row.mic.gameObject.SetActive(shown);
			}
			if (shown)
			{
				row.mic.localScale = Vector3.one * row.pop;
			}
		}

		private void ToggleMute(int i)
		{
			// Rebuilt from native code (token 0x0600068E, RVA 0x2800648)
			if (i >= entries.Count || entries[i].you)
			{
				return;
			}
			string key = entries[i].key;
			if (!muted.Remove(key))
			{
				muted.Add(key);
			}
			SaveMuted();
		}

		private static void LoadMuted()
		{
			// Rebuilt from native code (token 0x0600068F, RVA 0x27FE89C)
			if (mutedLoaded)
			{
				return;
			}
			mutedLoaded = true;
			foreach (string key in PlayerPrefs.GetString(MutedPref, "").Split(','))
			{
				if (key.Length > 0)
				{
					muted.Add(key);
				}
			}
		}

		private static void SaveMuted()
		{
			// Rebuilt from native code (token 0x06000690, RVA 0x2800798)
			List<string> kept = new List<string>();
			foreach (string key in muted)
			{
				if (!key.StartsWith("#"))
				{
					kept.Add(key);
				}
			}
			if (kept.Count > MutedKept)
			{
				kept.RemoveRange(0, kept.Count - MutedKept);
			}
			PlayerPrefs.SetString(MutedPref, string.Join(",", kept));
			PlayerPrefs.Save();
		}

		private void OpenReport(int i)
		{
			// Rebuilt from native code (token 0x06000691, RVA 0x2800A8C)
			if (i >= entries.Count || entries[i].you || sending)
			{
				return;
			}
			target = entries[i];
			panelOpen = true;
			closeAt = 0f;
			bool done = reported.Contains(target.key);
			panelText.text = (done ? "YOU REPORTED\n" : "REPORT\n") + target.name + (done ? "\nTHANKS!" : "\nFOR...");
			foreach (PokeButton key in categoryKeys)
			{
				if (key)
				{
					key.SetIdleColor(done ? DoneTint : Color.white);
				}
			}
			if (done)
			{
				closeAt = Time.time + 2.5f;
			}
		}

		private void Send(int category)
		{
			// Rebuilt from native code (token 0x06000692, RVA 0x2800D30)
			if (!panelOpen || sending || closeAt > 0f || string.IsNullOrEmpty(target.key) || reported.Contains(target.key))
			{
				return;
			}
			sending = true;
			panelText.text = "SENDING...";
			string key = target.key;
			string name = target.name;
			string room = NetworkManager.I ? NetworkManager.I.RoomCode : "";
			Wallet.ReportPlayer(target.id, name, Categories[category].ToLowerInvariant(), room, delegate(bool ok)
			{
				// Rebuilt from native code (token 0x060006A5, RVA 0x280153C)
				sending = false;
				if (ok)
				{
					reported.Add(key);
				}
				if (panelText)
				{
					panelText.text = ok ? "THANKS!\nWE'LL LOOK\nINTO IT" : "COULDN'T SEND\nTRY AGAIN\nIN A BIT";
					closeAt = Time.time + (ok ? 3f : 2.5f);
				}
			});
		}

		private void ClosePanel(float after)
		{
			// Rebuilt from native code (token 0x06000693, RVA 0x2801008)
			if (after <= 0f)
			{
				panelOpen = false;
				return;
			}
			closeAt = Time.time + after;
		}

		private void UpdatePanel()
		{
			// Rebuilt from native code (token 0x06000694, RVA 0x28001DC)
			if (!panel)
			{
				return;
			}
			float want = 0f;
			if (panelOpen)
			{
				if (closeAt > 0f && Time.time >= closeAt)
				{
					panelOpen = false;
				}
				PlayerRig rig = PlayerRig.I;
				if (rig && (rig.head.position - transform.position).sqrMagnitude > PanelReach * PanelReach)
				{
					panelOpen = false;
				}
				if (!sending && closeAt <= 0f && !entries.Exists(e => e.key == target.key))
				{
					panelOpen = false;
				}
				if (panelOpen)
				{
					want = 1f;
				}
				else
				{
					closeAt = 0f;
				}
			}
			float dt = Mathf.Min(Time.deltaTime, 0.05f);
			panelSpeed = (panelSpeed + (want - panelShown) * 320f * dt) * MathF.Exp(-16f * dt);
			panelShown = Mathf.Clamp(panelShown + panelSpeed * dt, 0f, 1.2f);
			if (!panelOpen && panelShown < 0.03f)
			{
				panelShown = 0f;
				panelSpeed = 0f;
			}
			bool shown = panelShown > 0f;
			if (panel.gameObject.activeSelf != shown)
			{
				panel.gameObject.SetActive(shown);
			}
			if (shown)
			{
				panel.localScale = new Vector3(panelShown, 1f, 1f);
			}
		}
	}
}
