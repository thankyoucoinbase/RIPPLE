using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shallows
{
	public class TextDepth : MonoBehaviour
	{
		private const float Every = 4f;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x060003E3, RVA 0x27A16E0)
			if (!FindAnyObjectByType<TextDepth>())
			{
				var gameObject = new GameObject("Text Depth");
				gameObject.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<TextDepth>();
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060003E4, RVA 0x27A17F0)
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060003E5, RVA 0x27A1894)
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// Rebuilt from native code (token 0x060003E6, RVA 0x27A1938)
			Fix();
		}

		private IEnumerator Start()
		{
			// Rebuilt from native code (token 0x060003E7, RVA 0x27A1AD4; MoveNext 0x060003EC, RVA 0x27A1B60)
			Fix();
			yield return null;
			Fix();
			var wait = new WaitForSeconds(Every);
			while (true)
			{
				yield return wait;
				Fix();
			}
		}

		private static void Fix()
		{
			// Rebuilt from native code (token 0x060003E8, RVA 0x27A193C)
			foreach (TextMesh text in FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
			{
				MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
				if (meshRenderer && WorldText.SeesThroughWalls(meshRenderer.sharedMaterial))
				{
					meshRenderer.sharedMaterial = WorldText.MaterialFor(text.font);
				}
			}
		}
	}
}
