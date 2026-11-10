using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public static class WorldText
	{
		private const int FontSize = 64;

		private static Font font;

		private static Shader shader;

		private static bool shaderLoaded;

		private static bool listening;

		private static readonly Dictionary<Font, Material> materials = new Dictionary<Font, Material>();

		public static Font Font
		{
			get
			{
				// Rebuilt from native code (token 0x060003DC, RVA 0x279803C)
				if (!font)
				{
					font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				}
				return font;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060003DD, RVA 0x27A0D7C)
			materials.Clear();
			Font.textureRebuilt -= OnFontRebuilt;
			shader = null;
			shaderLoaded = false;
			listening = false;
		}

		public static Material MaterialFor(Font f)
		{
			// Rebuilt from native code (token 0x060003DE, RVA 0x27A0E5C)
			Font key = f ? f : Font;
			if (!shaderLoaded)
			{
				shaderLoaded = true;
				shader = Resources.Load<Shader>("ShallowsWorldText");
				if (shader && !shader.isSupported)
				{
					shader = null;
				}
			}
			if (!shader)
			{
				return key.material;
			}
			if (materials.TryGetValue(key, out Material material) && material)
			{
				return material;
			}
			material = new Material(shader);
			material.name = "World Text " + key.name;
			material.mainTexture = key.material.mainTexture;
			materials[key] = material;
			if (!listening)
			{
				Font.textureRebuilt += OnFontRebuilt;
			}
			listening = true;
			return material;
		}

		private static void OnFontRebuilt(Font f)
		{
			// Rebuilt from native code (token 0x060003DF, RVA 0x27A124C)
			if (materials.TryGetValue(f, out Material material) && material)
			{
				material.mainTexture = f.material.mainTexture;
			}
		}

		internal static bool SeesThroughWalls(Material m)
		{
			// Rebuilt from native code (token 0x060003E0, RVA 0x27A1350)
			if (!m || !m.shader) return false;
			return m.shader.name == "GUI/Text Shader";
		}

		public static TextMesh Create(string name, Transform parent, Vector3 localPosition, string text, float height, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, TextAlignment alignment = TextAlignment.Center)
		{
			// Rebuilt from native code (token 0x060003E1, RVA 0x27A1430)
			GameObject go = new GameObject(name);
			go.transform.SetParent(parent, false);
			go.transform.localPosition = localPosition;
			TextMesh textMesh = go.AddComponent<TextMesh>();
			textMesh.font = Font;
			textMesh.fontSize = FontSize;
			textMesh.characterSize = height / 6.4f;
			textMesh.anchor = anchor;
			textMesh.alignment = alignment;
			textMesh.color = color;
			textMesh.text = text;
			go.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(Font);
			return textMesh;
		}
	}
}
