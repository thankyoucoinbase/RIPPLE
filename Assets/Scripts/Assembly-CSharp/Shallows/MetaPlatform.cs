using System;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace Shallows
{
	public class MetaPlatform : MonoBehaviour
	{
		public const string AppId = "";

		public static bool Checking { get; private set; }

		public static string UserId { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000707, RVA 0x2808698)
			Checking = false;
			UserId = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Begin()
		{
#if UNITY_EDITOR
			// OfflineGuard (Tools/Recovery/add_offline_guard.py): keep when rebuilding this body.
			if (System.Environment.GetEnvironmentVariable("RIPPLE_ALLOW_ONLINE") != "1")
			{
				UnityEngine.Debug.LogWarning("[OfflineGuard] blocked Meta Platform init; set RIPPLE_ALLOW_ONLINE=1 to allow");
				return;
			}
#endif
			// Rebuilt from native code (token 0x06000708, RVA 0x280870C)
			if (string.IsNullOrEmpty(AppId))
			{
				return;
			}
			Checking = true;
			GameObject gameObject = new GameObject("Meta Platform");
			gameObject.hideFlags = HideFlags.HideInHierarchy;
			DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<MetaPlatform>();
			try
			{
				Core.AsyncInitialize(AppId).OnComplete(OnStarted);
			}
			catch (Exception ex)
			{
				Refuse(ex.Message);
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000709, RVA 0x2808AA8)
			Request.RunCallbacks();
		}

		private static void OnStarted(Message<PlatformInitialize> message)
		{
			// Rebuilt from native code (token 0x0600070A, RVA 0x2808AB4)
			if (message.IsError)
			{
				Refuse(message.GetError().Message);
				return;
			}
			Entitlements.IsUserEntitledToApplication().OnComplete(OnOwnershipChecked);
		}

		private static void OnOwnershipChecked(Message message)
		{
			// Rebuilt from native code (token 0x0600070B, RVA 0x2808B84)
			if (message.IsError)
			{
				Refuse(message.GetError().Message);
				return;
			}
			Users.GetLoggedInUser().OnComplete(OnUser);
		}

		private static void OnUser(Message<User> message)
		{
			// Rebuilt from native code (token 0x0600070C, RVA 0x2808C68)
			if (message.IsError || message.Data == null || message.Data.ID == 0)
			{
				Gave(message.IsError ? message.GetError().Message : "no user");
				return;
			}
			UserId = message.Data.ID.ToString();
			Checking = false;
		}

		private static void Refuse(string why)
		{
			// Rebuilt from native code (token 0x0600070D, RVA 0x2808988)
			Debug.LogError("Shallows: couldn't confirm this Meta account owns Ripple (" + why + ").");
			Checking = false;
			if (!Debug.isDebugBuild)
			{
				UnityEngine.Application.Quit();
			}
		}

		private static void Gave(string why)
		{
			// Rebuilt from native code (token 0x0600070E, RVA 0x2808D90)
			Debug.LogWarning("Shallows: couldn't get the Meta account (" + why + "), signing in by headset.");
			Checking = false;
		}

		public static void Proof(Action<string> done)
		{
			// Rebuilt from native code (token 0x0600070F, RVA 0x2808E6C)
			if (UserId == null)
			{
				done?.Invoke(null);
				return;
			}
			Users.GetUserProof().OnComplete(delegate(Message<UserProof> message)
			{
				done?.Invoke(message.IsError ? null : message.Data?.Value);
			});
		}
	}
}
