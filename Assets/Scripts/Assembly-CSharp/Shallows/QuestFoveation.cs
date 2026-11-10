using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Shallows
{
	public class QuestFoveation : MonoBehaviour
	{
		public static float Level = 1f;

		private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

		private float nextCheck;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x0600030F, RVA 0x2792054)
			GameObject gameObject = new GameObject("Quest foveation");
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<QuestFoveation>();
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000310, RVA 0x2792120)
			if (Time.unscaledTime < nextCheck)
			{
				return;
			}
			nextCheck = Time.unscaledTime + 1f;
			Apply();
		}

		public static float Apply()
		{
			// Rebuilt from native code (token 0x06000311, RVA 0x27921A4)
			SubsystemManager.GetSubsystems(displays);
			float result = -1f;
			foreach (XRDisplaySubsystem display in displays)
			{
				if (!display.running)
				{
					continue;
				}
				if (!Mathf.Approximately(display.foveatedRenderingLevel, Level))
				{
					display.foveatedRenderingLevel = Level;
				}
				result = display.foveatedRenderingLevel;
			}
			return result;
		}
	}
}
