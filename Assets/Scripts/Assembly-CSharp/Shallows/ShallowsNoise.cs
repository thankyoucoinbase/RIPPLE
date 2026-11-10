using UnityEngine;

namespace Shallows
{
	public static class ShallowsNoise
	{
		public const int Size = 64;

		private static readonly int NoiseId = Shader.PropertyToID("_ShallowsNoiseTex");

		private static Texture2D texture;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			// Rebuilt from native code (token 0x06000523, RVA 0x27CD498)
			if (!texture)
			{
				System.Random random = new System.Random(20261027);
				ushort[] data = new ushort[Size * Size * 4];
				for (int i = 0; i < data.Length; i++)
				{
					data[i] = Mathf.FloatToHalf((float)random.NextDouble());
				}
				texture = new Texture2D(Size, Size, TextureFormat.RGBAHalf, false, true)
				{
					name = "Shallows Noise",
					wrapMode = TextureWrapMode.Repeat,
					filterMode = FilterMode.Bilinear,
					hideFlags = HideFlags.DontSave
				};
				texture.SetPixelData(data, 0);
				texture.Apply(false, true);
			}
			Shader.SetGlobalTexture(NoiseId, texture);
		}
	}
}
