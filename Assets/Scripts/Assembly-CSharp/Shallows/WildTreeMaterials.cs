using UnityEngine;

namespace Shallows
{
	public static class WildTreeMaterials
	{
		public struct Set
		{
			public Material bark;

			public Material leaves;

			public Material banana;

			public Material tiles;
		}

		private static Material bark;

		private static Material leaves;

		private static Material banana;

		private static Material fallbackTiles;

		private static bool looked;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060003BA, RVA 0x279DA28)
			bark = leaves = banana = fallbackTiles = null;
			looked = false;
		}

		public static Set Get(Vector3 floor)
		{
			// Rebuilt from native code (token 0x060003BB, RVA 0x279A4A8)
			if (!looked || !bark || !leaves || !banana)
			{
				bark = leaves = banana = fallbackTiles = null;
				looked = true;
				MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				foreach (MeshRenderer renderer in renderers)
				{
					Material material = renderer.sharedMaterial;
					if (!material)
					{
						continue;
					}
					if (!bark && material.name == "Bark")
					{
						bark = material;
					}
					else if (!leaves && material.name == "Leaves")
					{
						leaves = material;
					}
					else if (!banana && material.name == "Banana Bunch")
					{
						banana = material;
					}
					else if (!fallbackTiles && material.shader && material.shader.name == "Shallows/Tile")
					{
						fallbackTiles = material;
					}
				}
				if (!bark)
				{
					bark = Textured("Wild Bark", "BananaHill/Bark", 0.1f, null);
				}
				if (!leaves)
				{
					leaves = Textured("Wild Leaves", "BananaHill/Leaves", 0.15f, null);
				}
				if (!banana)
				{
					banana = Textured("Wild Banana", "BananaHill/Banana", 0.45f, new Color(0.62f, 0.5f, 0.12f, 1f));
				}
			}
			Material tiles = FloorAt(floor);
			if (!tiles)
			{
				tiles = fallbackTiles;
			}
			return new Set
			{
				bark = bark,
				leaves = leaves,
				banana = banana,
				tiles = tiles ? tiles : bark
			};
		}

		private static Material FloorAt(Vector3 floor)
		{
			// Rebuilt from native code (token 0x060003BC, RVA 0x279DD88)
			RaycastHit[] hits = Physics.RaycastAll(floor + Vector3.up * 0.5f, Vector3.down, 1.5f, ~4, QueryTriggerInteraction.Ignore);
			Material best = null;
			float bestDistance = float.MaxValue;
			foreach (RaycastHit hit in hits)
			{
				Renderer renderer = hit.collider.GetComponent<Renderer>();
				if (!renderer)
				{
					renderer = hit.collider.GetComponentInParent<Renderer>();
				}
				Material material = renderer ? renderer.sharedMaterial : null;
				if (!material || !material.shader || material.shader.name != "Shallows/Tile")
				{
					continue;
				}
				if (hit.distance < bestDistance)
				{
					bestDistance = hit.distance;
					best = material;
				}
			}
			return best;
		}

		private static Material Textured(string name, string texture, float smoothness, Color? glow)
		{
			// Rebuilt from native code (token 0x060003BD, RVA 0x279DAC8)
			Shader shader = Shader.Find("Universal Render Pipeline/Lit");
			if (!shader)
			{
				return null;
			}
			Texture2D map = Resources.Load<Texture2D>(texture);
			Material material = new Material(shader);
			material.name = name;
			material.SetColor("_BaseColor", Color.white);
			if (map)
			{
				material.SetTexture("_BaseMap", map);
				material.mainTexture = map;
			}
			material.SetFloat("_Smoothness", smoothness);
			material.SetFloat("_Metallic", 0f);
			if (glow.HasValue)
			{
				if (map)
				{
					material.SetTexture("_EmissionMap", map);
				}
				material.SetColor("_EmissionColor", glow.Value);
				material.EnableKeyword("_EMISSION");
			}
			return material;
		}
	}
}
