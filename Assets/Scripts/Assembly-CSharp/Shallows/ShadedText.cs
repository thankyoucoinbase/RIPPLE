using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(TextMesh))]
	public class ShadedText : MonoBehaviour
	{
		public Shader shader;

		private static readonly Dictionary<Font, Material> materials = new Dictionary<Font, Material>();

		private static bool listening;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000388, RVA 0x2797BEC)
			materials.Clear();
			Font.textureRebuilt -= OnFontRebuilt;
			listening = false;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000389, RVA 0x2797CC4)
			TextMesh textMesh = GetComponent<TextMesh>();
			LifeguardSign.Dress(textMesh);
			if (!shader || !shader.isSupported)
			{
				return;
			}
			Font font = textMesh.font ? textMesh.font : WorldText.Font;
			if (!materials.TryGetValue(font, out Material material) || !material)
			{
				material = new Material(shader);
				material.name = "Sign Text " + font.name;
				material.mainTexture = font.material.mainTexture;
				materials[font] = material;
				if (!listening)
				{
					Font.textureRebuilt += OnFontRebuilt;
				}
				listening = true;
			}
			GetComponent<MeshRenderer>().sharedMaterial = material;
		}

		private static void OnFontRebuilt(Font font)
		{
			// Rebuilt from native code (token 0x0600038A, RVA 0x2798158)
			if (materials.TryGetValue(font, out Material material) && material)
			{
				material.mainTexture = font.material.mainTexture;
			}
		}
	}
}
