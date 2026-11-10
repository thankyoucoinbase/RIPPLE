using System;
using UnityEngine;

namespace Shallows
{
	public class RewardPop : MonoBehaviour
	{
		private const float Height = 0.07f;

		private const float Edge = 0.005f;

		private const float InTime = 0.5f;

		private const float HoldTime = 1.7f;

		private const float OutTime = 0.32f;

		private const float Rise = 0.16f;

		private static readonly Color Yellow = new Color(1f, 0.86f, 0.18f, 1f);

		private static readonly Color Grey = new Color(0.78f, 0.78f, 0.8f, 1f);

		private static readonly Color Outline = new Color(0.06f, 0.05f, 0.08f, 1f);

		public static Transform ViewerOverride;

		private TextMesh text;

		private TextMesh[] outline;

		private Vector3 start;

		private float born;

		private float hold = HoldTime;

		private float shakeAt = -1f;

		private Color color = Yellow;

		private static Transform Viewer
		{
			get
			{
				// Rebuilt from native code (token 0x06000314, RVA 0x2792524)
				if (ViewerOverride)
				{
					return ViewerOverride;
				}
				if (PlayerRig.I && PlayerRig.I.head)
				{
					return PlayerRig.I.head;
				}
				return Camera.main ? Camera.main.transform : null;
			}
		}

		public static RewardPop Show(Vector3 bunch, int amount)
		{
			// Rebuilt from native code (token 0x06000315, RVA 0x2792710)
			Transform viewer = Viewer;
			Vector3 position = bunch + Vector3.up * 0.28f;
			if (viewer)
			{
				Vector3 eye = viewer.position;
				Vector3 toPop = position - eye;
				float distance = toPop.magnitude;
				Vector3 direction = distance > 0.01f ? toPop / distance : viewer.forward;
				position = eye + direction * Mathf.Max(0.85f, distance - 0.45f);
			}
			RewardPop pop = new GameObject("Reward Pop").AddComponent<RewardPop>();
			pop.Build("+" + amount.ToString("N0") + " BANANAS");
			pop.start = position;
			pop.born = Time.time;
			pop.Place(0f, 0f);
			return pop;
		}

		public void Refuse(string note)
		{
			// Rebuilt from native code (token 0x06000316, RVA 0x2793218)
			if (!this)
			{
				return;
			}
			SetText(string.IsNullOrEmpty(note) ? "NO BANANAS" : note.ToUpperInvariant());
			color = Grey;
			shakeAt = Time.time;
			hold = Mathf.Max(hold, Time.time - born - InTime + 2.2f);
		}

		public void Paid(int amount)
		{
			// Rebuilt from native code (token 0x06000317, RVA 0x27933D4)
			if (this)
			{
				SetText("+" + amount.ToString("N0") + " BANANAS");
			}
		}

		private void Build(string words)
		{
			// Rebuilt from native code (token 0x06000318, RVA 0x2792A10)
			text = gameObject.AddComponent<TextMesh>();
			PixelFonts.Use(text, PixelFonts.Sign, Height, 1.35f);
			text.anchor = TextAnchor.MiddleCenter;
			text.alignment = TextAlignment.Center;
			outline = new TextMesh[8];
			Material material = text.GetComponent<MeshRenderer>().sharedMaterial;
			for (int i = 0; i < 8; i++)
			{
				GameObject copy = new GameObject("Outline " + i);
				copy.transform.SetParent(transform, false);
				float angle = i * Mathf.PI / 4f;
				copy.transform.localPosition = new Vector3(MathF.Cos(angle) * Edge, MathF.Sin(angle) * Edge, 0.002f);
				TextMesh mesh = copy.AddComponent<TextMesh>();
				mesh.font = text.font;
				mesh.fontSize = text.fontSize;
				mesh.characterSize = text.characterSize;
				mesh.lineSpacing = text.lineSpacing;
				mesh.anchor = text.anchor;
				mesh.alignment = text.alignment;
				copy.GetComponent<MeshRenderer>().sharedMaterial = material;
				outline[i] = mesh;
			}
			SetText(words);
		}

		private void SetText(string words)
		{
			// Rebuilt from native code (token 0x06000319, RVA 0x2793350)
			text.text = words;
			foreach (TextMesh mesh in outline)
			{
				mesh.text = words;
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x0600031A, RVA 0x27934B0)
			float t = Time.time - born;
			float scale;
			float alpha = 1f;
			if (t < InTime)
			{
				scale = Spring(t / InTime);
			}
			else if (t < InTime + hold)
			{
				scale = 1f + MathF.Sin((t - InTime) / hold * Mathf.PI * 2f) * 0.025f;
			}
			else if (t < InTime + hold + OutTime)
			{
				float u = (t - InTime - hold) / OutTime;
				if (u < 0.3f)
				{
					scale = Mathf.Lerp(1f, 1.18f, Smooth(u / 0.3f));
				}
				else
				{
					float k = Smooth((u - 0.3f) / 0.7f);
					scale = Mathf.Lerp(1.18f, 0f, k * k);
				}
				alpha = 1f - Smooth(Mathf.Clamp01((u - 0.35f) / 0.65f));
			}
			else
			{
				Destroy(gameObject);
				return;
			}
			Place(t / (InTime + hold + OutTime), scale);
			text.color = new Color(color.r, color.g, color.b, alpha);
			foreach (TextMesh mesh in outline)
			{
				mesh.color = new Color(Outline.r, Outline.g, Outline.b, alpha);
			}
		}

		private void Place(float life, float scale)
		{
			// Rebuilt from native code (token 0x0600031B, RVA 0x2792DD8)
			Vector3 position = start + Vector3.up * ((1f - (1f - life) * (1f - life)) * Rise);
			if (shakeAt >= 0f)
			{
				float shake = Time.time - shakeAt;
				if (shake < 0.45f)
				{
					position += transform.right * MathF.Sin(shake * 60f) * 0.012f * (1f - shake / 0.45f);
				}
			}
			transform.position = position;
			Transform viewer = Viewer;
			if (viewer)
			{
				Vector3 away = position - viewer.position;
				if (away.sqrMagnitude > 1E-06f)
				{
					transform.rotation = Quaternion.LookRotation(away, Vector3.up);
				}
			}
			float tilt = born > 0f ? MathF.Sin(Mathf.Clamp01((Time.time - born) / InTime) * Mathf.PI) * 6f : 0f;
			transform.rotation *= Quaternion.Euler(0f, 0f, tilt);
			transform.localScale = Vector3.one * Mathf.Max(0.0001f, scale);
		}

		private static float Smooth(float u)
		{
			// Rebuilt from native code (token 0x0600031C, RVA 0x2793868)
			return u * u * (3f - 2f * u);
		}

		private static float Spring(float u)
		{
			// Rebuilt from native code (token 0x0600031D, RVA 0x2793804)
			u = Mathf.Clamp01(u);
			return 1f - (1f - u) * (MathF.Exp(u * -4f) * MathF.Cos(u * 9.5f));
		}
	}
}
