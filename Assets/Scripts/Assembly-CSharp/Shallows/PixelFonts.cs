using UnityEngine;

namespace Shallows
{
	public static class PixelFonts
	{
		private static Font screen;

		private static Font sign;

		// Rebuilt from native code (token 0x060002E1, RVA 0x277FEAC)
		public static Font Screen => Load(ref screen, "Fonts/VT323-Regular");

		// Rebuilt from native code (token 0x060002E2, RVA 0x278DFD0)
		public static Font Sign => Load(ref sign, "Fonts/PressStart2P-Regular");

		private static Font Load(ref Font font, string path)
		{
			// Rebuilt from native code (token 0x060002E3, RVA 0x278DED0)
			if (!font)
			{
				font = Resources.Load<Font>(path);
			}
			if (!font)
			{
				return WorldText.Font;
			}
			return font;
		}

		public static void Use(TextMesh mesh, Font font, float height, float lineSpacing = 1f)
		{
			// Rebuilt from native code (token 0x060002E4, RVA 0x277FF08)
			mesh.font = font;
			mesh.fontSize = font == screen ? 100 : 64;
			mesh.characterSize = height / (mesh.fontSize * 0.1f);
			mesh.lineSpacing = lineSpacing;
			mesh.GetComponent<MeshRenderer>().sharedMaterial = WorldText.MaterialFor(font);
		}
	}
}
