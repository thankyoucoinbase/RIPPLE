using System;
using System.Collections;
using UnityEngine;

namespace Shallows
{
	internal class WalletTimer : MonoBehaviour
	{
		private static WalletTimer instance;

		public static void After(float seconds, Action then)
		{
			// Rebuilt from native code (token 0x06000A97, RVA 0x275D0F0)
			if (!instance)
			{
				GameObject go = new GameObject("Wallet Timer");
				go.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(go);
				instance = go.AddComponent<WalletTimer>();
			}
			instance.StartCoroutine(instance.Wait(seconds, then));
		}

		private IEnumerator Wait(float seconds, Action then)
		{
			// Rebuilt from native code (token 0x06000A98, RVA 0x275D25C; MoveNext 0x275D514)
			yield return new WaitForSecondsRealtime(seconds);
			then?.Invoke();
		}
	}
}
