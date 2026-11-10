using System;
using UnityEngine;
using Random = System.Random;

namespace Shallows
{
	public class MemoryBridge : MonoBehaviour
	{
		[Tooltip("Row by row from the start deck, lanes left to right within a row.")]
		public SinkingPlatform[] tiles;

		public int lanes = 3;

		public TextMesh board;

		[Tooltip("Seconds from one showing of the path to the next.")]
		public float showEvery = 30f;

		[Tooltip("Seconds the path stays lit each time.")]
		public float showFor = 6f;

		[Tooltip("Seconds before the path changes (a whole number of showings, so it changes just as it's shown).")]
		public float pathSeconds = 240f;

		[Tooltip("Seconds between one row lighting up (with its note) and the next. Each row stays lit for showFor.")]
		public float rowStep = 0.16f;

		[Tooltip("How loud each tile's note is.")]
		public float noteVolume = 0.55f;

		public Color lit = new Color(0.45f, 1.4f, 1.2f, 1f);

		public int seed = 7;

		// Rebuilt from native code (token 0x06000485, RVA 0x27B4574)
		private static readonly int ColorId = Shader.PropertyToID("_Color");

		private static readonly int[] Scale = { 0, 2, 3, 5, 7, 8, 10, 12, 14, 15, 17, 19, 20, 22, 24, 26 };

		private const float ScaleBottom = 0.42f;

		// Rebuilt from native code (token 0x06000484, RVA 0x27B4530)
		private int path = -1;

		private int[] safeLane;

		private Renderer[] renderers;

		private bool[] glowing;

		private MaterialPropertyBlock block;

		private float nextBoard;

		private TextMesh title;

		private int level = -2;

		private int Rows
		{
			get
			{
				// Rebuilt from native code (token 0x0600047B, RVA 0x27B2DA0)
				if (tiles == null || lanes < 1)
				{
					return 0;
				}
				return tiles.Length / lanes;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600047C, RVA 0x27B2DC8)
			block = new MaterialPropertyBlock();
			renderers = new Renderer[tiles.Length];
			glowing = new bool[tiles.Length];
			for (int i = 0; i < tiles.Length; i++)
			{
				renderers[i] = tiles[i] ? tiles[i].GetComponent<Renderer>() : null;
				if (tiles[i])
				{
					tiles[i].instant = true;
					tiles[i].stayDown = 2.5f;
				}
			}
			WearScreen();
		}

		public Transform WearScreen()
		{
			// Rebuilt from native code (token 0x0600047D, RVA 0x27B3034)
			if (!board || !board.transform.parent)
			{
				return null;
			}
			Transform holder = board.transform.parent;
			if (!title)
			{
				Transform existing = holder.Find("Screen Title");
				title = existing ? existing.GetComponent<TextMesh>() : null;
			}
			Transform model = LifeguardProps.Dress(holder, "PathScreen", Vector3.zero, Quaternion.identity, board.transform, title ? title.transform : null);
			if (!model)
			{
				return null;
			}
			if (!title)
			{
				title = WorldText.Create("Screen Title", holder, Vector3.zero, "", 0.07f, Color.white, TextAnchor.UpperCenter, TextAlignment.Center);
			}
			Place(title, model, "Title Text", TextAnchor.UpperCenter, 0.075f, new Color(0.75f, 1f, 0.7f, 1f));
			Place(board, model, "Count Text", TextAnchor.MiddleCenter, 0.2f, new Color(0.55f, 1f, 0.6f, 1f));
			title.alignment = TextAlignment.Center;
			board.alignment = TextAlignment.Center;
			title.text = "PATH SHOWS IN";
			board.text = Mathf.RoundToInt(showEvery).ToString();
			return model;
		}

		private static void Place(TextMesh text, Transform model, string anchor, TextAnchor alignment, float height, Color color)
		{
			// Rebuilt from native code (token 0x0600047E, RVA 0x27B36D8)
			PixelFonts.Use(text, PixelFonts.Screen, height);
			text.anchor = alignment;
			text.color = color;
			Transform spot = LifeguardProps.Find(model, anchor);
			if (spot)
			{
				text.transform.position = spot.position;
			}
			text.transform.rotation = model.parent.rotation;
		}

		private int Level()
		{
			// Rebuilt from native code (token 0x0600047F, RVA 0x27B3878)
			if (level != -2)
			{
				return level;
			}
			GameManager game = GameManager.I;
			if (!game)
			{
				return -1;
			}
			level = -1;
			if (game.levels == null || tiles.Length == 0 || !tiles[0])
			{
				return level;
			}
			float best = float.MaxValue;
			for (int i = 0; i < game.levels.Length; i++)
			{
				float distance = game.levels[i].startDeck.SqrDistance(tiles[0].transform.position);
				if (distance < best)
				{
					best = distance;
					level = i;
				}
			}
			return level;
		}

		private bool Heard()
		{
			// Rebuilt from native code (token 0x06000480, RVA 0x27B3A4C)
			GameManager game = GameManager.I;
			int at = Level();
			return !game || at < 0 || game.Checkpoint == at;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000481, RVA 0x27B3B3C)
			float time = NetBridge.SharedTime;
			int shuffle = Mathf.FloorToInt(time / Mathf.Max(10f, pathSeconds));
			if (shuffle != path)
			{
				path = shuffle;
				safeLane = MakePath(shuffle);
			}
			int rows = Rows;
			for (int row = 0; row < rows; row++)
			{
				for (int lane = 0; lane < lanes; lane++)
				{
					SinkingPlatform tile = tiles[row * lanes + lane];
					if (!tile)
					{
						continue;
					}
					bool armed = lane != safeLane[row];
					if (tile.armed != armed && tile.Idle)
					{
						tile.armed = armed;
					}
				}
			}
			float step = Mathf.Max(0f, rowStep);
			float litFor = showFor + step * Mathf.Max(0, rows - 1);
			float cycle = Mathf.Max(litFor + 2f, showEvery);
			float phase = Mathf.Repeat(time, cycle);
			bool heard = Heard();
			for (int row = 0; row < rows; row++)
			{
				int safe = safeLane[row] + lanes * row;
				float start = step * row;
				if (phase >= start)
				{
					bool on = phase < start + showFor;
					if (Glow(safe, on) && on && phase - start < 0.25f && heard && SplashSystem.I)
					{
						SplashSystem.I.PlaySound(Sfx.MemoryTileLight(), tiles[safe].transform.position, noteVolume, MathF.Pow(2f, Scale[row % Scale.Length] / 12f) * ScaleBottom);
					}
				}
				else
				{
					Glow(safe, false);
				}
				for (int lane = 0; lane < lanes; lane++)
				{
					if (lane != safeLane[row])
					{
						Glow(row * lanes + lane, false);
					}
				}
			}
			if (!board || Time.time < nextBoard)
			{
				return;
			}
			nextBoard = Time.time + 0.2f;
			bool showing = phase < litFor;
			string label = showing ? "REMEMBER IT" : "PATH SHOWS IN";
			int seconds = Mathf.CeilToInt((showing ? litFor : cycle) - phase);
			if (title)
			{
				title.text = label;
				board.text = seconds.ToString();
			}
			else
			{
				board.text = string.Format("{0}\n{1}", label, seconds);
			}
		}

		private int[] MakePath(int shuffle)
		{
			// Rebuilt from native code (token 0x06000482, RVA 0x27B424C)
			Random random = new Random(shuffle + seed * 7919);
			int rows = Rows;
			int[] safe = new int[rows];
			int lane = random.Next(lanes);
			for (int row = 0; row < rows; row++)
			{
				safe[row] = lane;
				lane = Mathf.Clamp(lane + random.Next(-1, 2), 0, lanes - 1);
			}
			return safe;
		}

		private bool Glow(int i, bool on)
		{
			// Rebuilt from native code (token 0x06000483, RVA 0x27B4398)
			if (glowing[i] == on || !renderers[i])
			{
				return false;
			}
			glowing[i] = on;
			if (on)
			{
				block.SetColor(ColorId, lit);
			}
			renderers[i].SetPropertyBlock(on ? block : null);
			return true;
		}
	}
}
