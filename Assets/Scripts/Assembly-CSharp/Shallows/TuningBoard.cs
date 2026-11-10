using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class TuningBoard : MonoBehaviour
	{
		private class Row
		{
			public Func<string> value;

			public TextMesh text;
		}

		public ShallowsTuning tuning;

		public Material buttonMaterial;

		public float width = 0.95f;

		public float rowHeight = 0.085f;

		private readonly List<Row> rows = new List<Row>();

		private void Start()
		{
			// Rebuilt from native code (token 0x060000B1, RVA 0x27141C0)
			if (!tuning)
			{
				tuning = ShallowsTuning.I;
			}
			ShallowsTuning t = tuning;
			WorldText.Create("Title", transform, new Vector3(0f, 0.08f, -0.02f), "TUNING  (push - / + with your hand)", 0.04f, Color.white);
			Number("Monster wander speed", () => t.roamSpeed, v => t.roamSpeed = v, 0.25f, 0.5f, 8f, "0.00", "m/s");
			Number("Monster sprint speed", () => t.chargeSpeed, v => t.chargeSpeed = v, 1f, 2f, 40f, "0", "m/s");
			Number("Monster grab reach", () => t.grabReach, v => t.grabReach = v, 0.25f, 1f, 8f, "0.00", "m");
			Number("Voice hearing range", () => t.voiceHearRange, v => t.voiceHearRange = v, 2f, 0f, 60f, "0", "m");
			Number("Voice heard chance", () => t.voiceHearChance, v => t.voiceHearChance = v, 0.05f, 0f, 1f, "0%", "");
			Number("Monster creep speed", () => t.stalkSpeed, v => t.stalkSpeed = v, 0.1f, 0.2f, 5f, "0.0", "m/s");
			Number("Search time", () => t.searchTime, v => t.searchTime = v, 0.5f, 0.5f, 15f, "0.0", "s");
			Number("Hand splash speed", () => t.handSplashSpeed, v => t.handSplashSpeed = v, 0.1f, 0.3f, 5f, "0.0", "m/s");
			Number("Hand churn speed", () => t.handChurnSpeed, v => t.handChurnSpeed = v, 0.1f, 0.3f, 5f, "0.0", "m/s");
			Number("Silent wade speed", () => t.wadeQuietSpeed, v => t.wadeQuietSpeed = v, 0.05f, 0.05f, 3f, "0.00", "m/s");
			Number("Body splash speed", () => t.bodySplashSpeed, v => t.bodySplashSpeed = v, 0.1f, 0.2f, 5f, "0.0", "m/s");
			Number("Max hearing radius", () => t.hearRadiusMax, v => t.hearRadiusMax = v, 2.5f, 5f, 80f, "0", "m");
			Number("Body height", () => t.bodyHeight, v => t.bodyHeight = v, 0.05f, PlayerRig.MinBodyHeight, 1f, "0.00", "m");
			Number("Jump multiplier", () => t.jumpMultiplier, v => t.jumpMultiplier = v, 0.05f, 0.5f, 2.5f, "0.00", "x");
			Number("Jump from water", () => t.waterJumpSpeed, v => t.waterJumpSpeed = v, 0.25f, 1f, 10f, "0.00", "m/s");
			Number("Room echo", () => t.echo, v => t.echo = v, 0.1f, 0f, 2f, "0.0", "x");
			Number("Hand tap volume", () => t.tapVolume, v => t.tapVolume = v, 0.1f, 0f, 1f, "0.0", "");
			Number("Ambience volume", () => t.ambienceVolume, v => t.ambienceVolume = v, 0.05f, 0f, 1f, "0.00", "");
			Number("Wading volume", () => t.wadingVolume, v => t.wadingVolume = v, 0.1f, 0f, 1f, "0.0", "");
			Number("Monster breath volume", () => t.monsterBreathVolume, v => t.monsterBreathVolume = v, 0.1f, 0f, 1.5f, "0.0", "");
			Number("Voice boost", () => t.voiceBoostDb, v => t.voiceBoostDb = v, 1f, 0f, 18f, "0", " dB");
			Number("Video mic volume", () => t.recordingMicVolume, v => t.recordingMicVolume = v, 0.1f, 0f, 4f, "0.0", "x");
			Toggle("Debug rings + labels", () => t.showDebug, v => t.showDebug = v);
			FitPanel();
		}

		private void FitPanel()
		{
			// Rebuilt from native code (token 0x060000B2, RVA 0x271567C)
			Transform panel = transform.Find("Panel");
			if (!panel)
			{
				return;
			}
			float height = rowHeight * rows.Count + 0.2f;
			panel.localScale = new Vector3(panel.localScale.x, height, panel.localScale.z);
			panel.localPosition = new Vector3(panel.localPosition.x, height * -0.5f + 0.15f, panel.localPosition.z);
		}

		private void Number(string name, Func<float> get, Action<float> set, float step, float min, float max, string format, string unit)
		{
			// Rebuilt from native code (token 0x060000B3, RVA 0x27152E8)
			Row row = AddRow(name, () => get().ToString(format) + " " + unit);
			AddButton(row, "-", -0.33f, () => set(Mathf.Clamp(Round(get() - step, step), min, max)));
			AddButton(row, "+", -0.07f, () => set(Mathf.Clamp(Round(get() + step, step), min, max)));
		}

		private void Toggle(string name, Func<bool> get, Action<bool> set)
		{
			// Rebuilt from native code (token 0x060000B4, RVA 0x2715508)
			Row row = AddRow(name, () => get() ? "ON" : "OFF");
			AddButton(row, "", -0.07f, () => set(!get()));
		}

		private static float Round(float v, float step)
		{
			// Rebuilt from native code (token 0x060000B5, RVA 0x2715E2C)
			return Mathf.Round(v / step) * step;
		}

		private Row AddRow(string name, Func<string> value)
		{
			// Rebuilt from native code (token 0x060000B6, RVA 0x27157D8)
			int index = rows.Count;
			float y = rowHeight * -index;
			Row row = new Row();
			row.value = value;
			WorldText.Create(name, transform, new Vector3(width * -0.5f + 0.03f, y, -0.02f), name, 0.032f, new Color(0.9f, 0.95f, 1f, 1f), TextAnchor.MiddleLeft, TextAlignment.Left);
			row.text = WorldText.Create(name + " Value", transform, new Vector3(width * 0.5f - 0.2f, y, -0.02f), value(), 0.032f, new Color(1f, 0.92f, 0.5f, 1f));
			rows.Add(row);
			return row;
		}

		private void AddButton(Row row, string symbol, float xFromRight, Action action)
		{
			// Rebuilt from native code (token 0x060000B7, RVA 0x2715A68)
			int count = rows.Count;
			GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
			button.name = "Button " + symbol;
			button.layer = 2;
			button.transform.SetParent(transform, false);
			button.transform.localPosition = new Vector3(width * 0.5f + xFromRight, rowHeight * (1 - count), -0.035f);
			button.transform.localScale = new Vector3(0.075f, 0.075f, 0.05f);
			button.GetComponent<BoxCollider>().isTrigger = true;
			if (buttonMaterial)
			{
				button.GetComponent<MeshRenderer>().sharedMaterial = buttonMaterial;
			}
			button.AddComponent<PokeButton>().onPress = () =>
			{
				action();
				row.text.text = row.value();
			};
			if (symbol.Length > 0)
			{
				WorldText.Create("Label", transform, button.transform.localPosition + new Vector3(0f, 0f, -0.03f), symbol, 0.05f, Color.white);
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060000B8, RVA 0x2715ED0)
			if (Time.frameCount % 15 != 0)
			{
				return;
			}
			foreach (Row row in rows)
			{
				row.text.text = row.value();
			}
		}
	}
}
