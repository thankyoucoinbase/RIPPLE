using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class PlayFabClient : MonoBehaviour
	{
		public struct BoardEntry
		{
			public string name;

			public float time;

			public bool mine;
		}

		public const string TitleId = "";

		public const string EscapeStatistic = "Escape2";

		private const string BananaKey = "bananas";

		private const double OffBoard = 86400000.0;

		public const string MinNetVersionKey = "minNetVersion";

		public static event Action SignedIn;

		public static event Action BoardChanged;

		private static PlayFabClient instance;

		private static string ticket;

		private static string playFabId;

		private static string displayName;

		private static int pendingBananas = -1;

		private static float nameChangedAt = -1f;

		private static float boardDueAt = -1f;

		public static bool LoggedIn
		{
			get
			{
				// Rebuilt from native code (token 0x06000721, RVA 0x27FA920)
				return ticket != null;
			}
		}

		public static int CloudBananas { get; private set; } = -1;

		public static float BestTime { get; private set; }

		public static float MyBest
		{
			get
			{
				// Rebuilt from native code (token 0x06000726, RVA 0x280A848)
				return BestTime;
			}
		}

		public static string PlayFabId
		{
			get
			{
				// Rebuilt from native code (token 0x06000727, RVA 0x280A8D0)
				return playFabId;
			}
		}

		public static string Session { get; private set; }

		public static string BoardName
		{
			get
			{
				// Rebuilt from native code (token 0x0600072A, RVA 0x280A9E0)
				return displayName;
			}
		}

		public static List<BoardEntry> Board { get; private set; }

		public static int MinNetVersion { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000733, RVA 0x280AF14)
			instance = null;
			displayName = null;
			playFabId = null;
			ticket = null;
			Session = null;
			pendingBananas = -1;
			nameChangedAt = -1f;
			boardDueAt = -1f;
			CloudBananas = -1;
			MinNetVersion = 0;
			BestTime = 0f;
			Board = null;
			SignedIn = null;
			BoardChanged = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Begin()
		{
			// Rebuilt from native code (token 0x06000734, RVA 0x280B11C)
			if (!instance)
			{
				GameObject go = new GameObject("PlayFab");
				go.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(go);
				instance = go.AddComponent<PlayFabClient>();
			}
		}

		public static void SaveBananas(int count)
		{
			// Rebuilt from native code (token 0x06000735, RVA 0x280B274)
			pendingBananas = count;
		}

		public static void SubmitEscape(float seconds)
		{
			// Rebuilt from native code (token 0x06000736, RVA 0x280B2D0)
			if (seconds <= 0f || (BestTime > 0f && BestTime <= seconds))
			{
				return;
			}
			BestTime = seconds;
			RefreshBoard(2f);
		}

		public static void PhotonToken(string photonAppId, Action<string> done)
		{
			// Rebuilt from native code (token 0x06000737, RVA 0x27FA980)
			if (!LoggedIn || !instance || string.IsNullOrEmpty(photonAppId))
			{
				done?.Invoke(null);
				return;
			}
			Dictionary<string, object> body = new Dictionary<string, object> { ["PhotonApplicationId"] = photonAppId };
			instance.StartCoroutine(Call("GetPhotonAuthenticationToken", body, true, d =>
			{
				// Rebuilt from native code (token 0x0600074F, RVA 0x280C1E8)
				done?.Invoke(MiniJson.Str(d, "PhotonCustomAuthenticationToken"));
			}, e =>
			{
				// Rebuilt from native code (token 0x06000750, RVA 0x280C25C)
				done?.Invoke(null);
			}));
		}

		public static void RunScript(string function, Dictionary<string, object> args, Action<Dictionary<string, object>, string> done)
		{
			// Rebuilt from native code (token 0x06000738, RVA 0x280B59C)
			if (!LoggedIn || !instance)
			{
				done?.Invoke(null, "Offline");
				return;
			}
			instance.StartCoroutine(instance.Script(function, args, done));
		}

		public static void RefreshBoard(float delay = 0f)
		{
			// Rebuilt from native code (token 0x06000739, RVA 0x280B41C)
			if (boardDueAt < 0f || Time.unscaledTime + delay < boardDueAt)
			{
				boardDueAt = Time.unscaledTime + delay;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600073A, RVA 0x280B788)
			PlayerProfile.Changed += OnProfileChanged;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600073B, RVA 0x280B828)
			PlayerProfile.Changed -= OnProfileChanged;
		}

		private void OnProfileChanged()
		{
			// Rebuilt from native code (token 0x0600073C, RVA 0x280B8C8)
			nameChangedAt = Time.unscaledTime;
		}

		private IEnumerator Start()
		{
			// Rebuilt from native code (token 0x0600073D, RVA 0x280B934; state machine 0x06000794, RVA 0x280FC74)
			float wait = 10f;
			while (!LoggedIn)
			{
				yield return Login();
				if (LoggedIn)
				{
					break;
				}
				yield return new WaitForSecondsRealtime(wait);
				wait = Mathf.Min(wait * 2f, 300f);
			}
			float nextBoard = 0f;
			while (true)
			{
				float now = Time.unscaledTime;
				if (nameChangedAt >= 0f && now - nameChangedAt > 3f)
				{
					nameChangedAt = -1f;
					yield return PushName();
				}
				if (pendingBananas >= 0)
				{
					yield return PushBananas();
				}
				if (now >= nextBoard || (boardDueAt >= 0f && now >= boardDueAt))
				{
					boardDueAt = -1f;
					nextBoard = now + 120f;
					yield return FetchBoard();
					yield return FetchMinNetVersion();
				}
				if (!LoggedIn)
				{
					yield return Login();
					if (!LoggedIn)
					{
						yield return new WaitForSecondsRealtime(30f);
					}
				}
				yield return new WaitForSecondsRealtime(1f);
			}
		}

		private IEnumerator Login()
		{
			// Rebuilt from native code (token 0x0600073E, RVA 0x280B9A8; state machine 0x06000776, RVA 0x280D914)
			float waitUntil = Time.realtimeSinceStartup + 15f;
			while (MetaPlatform.Checking && Time.realtimeSinceStartup < waitUntil)
			{
				yield return null;
			}
			string meta = MetaPlatform.UserId;
			Dictionary<string, object> info = new Dictionary<string, object>
			{
				["GetUserData"] = true,
				["UserDataKeys"] = new List<object> { BananaKey },
				["GetPlayerStatistics"] = true,
				["PlayerStatisticNames"] = new List<object> { EscapeStatistic },
				["GetPlayerProfile"] = true,
				["ProfileConstraints"] = new Dictionary<string, object> { ["ShowDisplayName"] = true },
				["GetTitleData"] = true,
				["TitleDataKeys"] = new List<object> { MinNetVersionKey }
			};
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["TitleId"] = TitleId,
				["CustomId"] = meta != null ? "meta-" + meta : DeviceId(),
				["CreateAccount"] = true,
				["InfoRequestParameters"] = info
			};
			object data = null;
			yield return Call("LoginWithCustomID", body, false, d =>
			{
				// Rebuilt from native code (token 0x06000752, RVA 0x280C284)
				data = d;
			});
			if (data == null)
			{
				yield break;
			}
			ticket = MiniJson.Str(data, "SessionTicket");
			playFabId = MiniJson.Str(data, "PlayFabId");
			Dictionary<string, object> payload = MiniJson.Obj(data, "InfoResultPayload");
			CloudBananas = int.TryParse(MiniJson.Str(MiniJson.Obj(MiniJson.Obj(payload, "UserData"), BananaKey), "Value"), out int bananas) ? bananas : -1;
			List<object> statistics = MiniJson.List(payload, "PlayerStatistics");
			if (statistics != null)
			{
				foreach (object statistic in statistics)
				{
					if (MiniJson.Str(statistic, "StatisticName") == EscapeStatistic && MiniJson.Num(statistic, "Value") < 0.0 && MiniJson.Num(statistic, "Value") > -OffBoard)
					{
						BestTime = (float)(MiniJson.Num(statistic, "Value") / -1000.0);
					}
				}
			}
			displayName = MiniJson.Str(MiniJson.Obj(payload, "PlayerProfile"), "DisplayName");
			ReadMinNetVersion(MiniJson.Obj(payload, "TitleData"));
			Debug.Log("Shallows: signed in to PlayFab as " + playFabId + (data is Dictionary<string, object> reply && reply.TryGetValue("NewlyCreated", out object created) && created is bool yes && yes ? " (new account)" : "") + ".");
			Session = Guid.NewGuid().ToString("N");
			if (meta != null)
			{
				yield return ProveMeta(meta);
			}
			PlayerProfile.Load();
			if (displayName != PlayerProfile.Name)
			{
				nameChangedAt = 0f;
			}
			SignedIn?.Invoke();
		}

		private IEnumerator FetchMinNetVersion()
		{
			// Rebuilt from native code (token 0x0600073F, RVA 0x280BA1C; state machine 0x06000770, RVA 0x280D5A0)
			Dictionary<string, object> body = new Dictionary<string, object> { ["Keys"] = new List<object> { MinNetVersionKey } };
			object data = null;
			yield return Call("GetTitleData", body, true, d =>
			{
				// Rebuilt from native code (token 0x06000754, RVA 0x280C294)
				data = d;
			});
			if (data != null)
			{
				ReadMinNetVersion(MiniJson.Obj(data, "Data"));
			}
		}

		private static void ReadMinNetVersion(Dictionary<string, object> titleData)
		{
			// Rebuilt from native code (token 0x06000740, RVA 0x280BA74)
			if (titleData == null)
			{
				return;
			}
			int version = int.TryParse(MiniJson.Str(titleData, MinNetVersionKey), out int parsed) ? parsed : 0;
			if (version != MinNetVersion)
			{
				Debug.Log(string.Format("Shallows: oldest network version allowed online is now {0}.", version));
			}
			MinNetVersion = version;
		}

		private IEnumerator PushBananas()
		{
			// Rebuilt from native code (token 0x06000741, RVA 0x280BC18; state machine 0x06000782, RVA 0x280ECB8)
			int count = pendingBananas;
			if (count < 0 || !LoggedIn)
			{
				yield break;
			}
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["Data"] = new Dictionary<string, object> { [BananaKey] = count.ToString() },
				["Permission"] = "Private"
			};
			bool ok = false;
			yield return Call("UpdateUserData", body, true, _ =>
			{
				// Rebuilt from native code (token 0x06000756, RVA 0x280C2A4)
				ok = true;
			});
			if (ok && pendingBananas == count)
			{
				pendingBananas = -1;
			}
		}

		private IEnumerator ProveMeta(string meta)
		{
			// Rebuilt from native code (token 0x06000742, RVA 0x280BC70; state machine 0x0600077C, RVA 0x280E8C0)
			string nonce = null;
			bool answered = false;
			MetaPlatform.Proof(n =>
			{
				// Rebuilt from native code (token 0x06000758, RVA 0x280C2B8)
				nonce = n;
				answered = true;
			});
			float waitUntil = Time.realtimeSinceStartup + 10f;
			while (!answered && Time.realtimeSinceStartup < waitUntil)
			{
				yield return null;
			}
			if (string.IsNullOrEmpty(nonce))
			{
				Debug.LogWarning("Shallows: Meta didn't give a proof, so the shop script can't check this account.");
				yield break;
			}
			yield return Script("meta", new Dictionary<string, object> { ["user"] = meta, ["nonce"] = nonce }, (result, error) =>
			{
				// Rebuilt from native code (token 0x0600074C, RVA 0x280C028)
				if (result != null && result.TryGetValue("ok", out object ok) && ok is bool yes && yes)
				{
					Debug.Log("Shallows: PlayFab checked the Meta account.");
					return;
				}
				Debug.LogWarning("Shallows: PlayFab couldn't check the Meta account (" + (error ?? MiniJson.Str(result, "message")) + ").");
			});
		}

		private IEnumerator Script(string function, Dictionary<string, object> args, Action<Dictionary<string, object>, string> done)
		{
			// Rebuilt from native code (token 0x06000743, RVA 0x280B6E4; state machine 0x0600078E, RVA 0x280F54C)
			Dictionary<string, object> parameter = args == null ? new Dictionary<string, object>() : new Dictionary<string, object>(args);
			if (Session != null)
			{
				parameter["session"] = Session;
			}
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["FunctionName"] = function,
				["FunctionParameter"] = parameter,
				["RevisionSelection"] = "Live"
			};
			object data = null;
			string error = null;
			yield return Call("ExecuteCloudScript", body, true, d =>
			{
				// Rebuilt from native code (token 0x0600075A, RVA 0x280C2E4)
				data = d ?? new Dictionary<string, object>();
			}, e =>
			{
				// Rebuilt from native code (token 0x0600075B, RVA 0x280C370)
				error = e;
			});
			if (data == null)
			{
				done?.Invoke(null, error ?? "Offline");
				yield break;
			}
			Dictionary<string, object> failure = MiniJson.Obj(data, "Error");
			if (failure != null)
			{
				string code = MiniJson.Str(failure, "Error");
				Debug.LogWarning("Shallows: shop script " + function + " failed: " + (code ?? "ScriptError") + " (" + MiniJson.Str(failure, "Message") + ")");
				done?.Invoke(null, code ?? "ScriptError");
				yield break;
			}
			Dictionary<string, object> result = MiniJson.Obj(data, "FunctionResult");
			done?.Invoke(result, result == null ? "NoResult" : null);
		}

		private IEnumerator PushName()
		{
			// Rebuilt from native code (token 0x06000744, RVA 0x280BD00; state machine 0x06000788, RVA 0x280F038)
			if (!LoggedIn)
			{
				yield break;
			}
			PlayerProfile.Load();
			string name = PlayerProfile.Name;
			while (name.Length < 3)
			{
				name += "0";
			}
			for (int attempt = 0; attempt < 3; attempt++)
			{
				if (name == displayName)
				{
					yield break;
				}
				string error = null;
				bool ok = false;
				yield return Call("UpdateUserTitleDisplayName", new Dictionary<string, object> { ["DisplayName"] = name }, true, _ =>
				{
					// Rebuilt from native code (token 0x0600075D, RVA 0x280C380)
					ok = true;
				}, e =>
				{
					// Rebuilt from native code (token 0x0600075E, RVA 0x280C38C)
					error = e;
				});
				if (ok)
				{
					displayName = name;
					RefreshBoard(1f);
					yield break;
				}
				if (error != "NameNotAvailable")
				{
					yield break;
				}
				name = PlayerProfile.Name + Random.Range(10, 100);
			}
		}

		private IEnumerator FetchBoard()
		{
			// Rebuilt from native code (token 0x06000745, RVA 0x280BD58; state machine 0x0600076A, RVA 0x280CD18)
			if (!LoggedIn)
			{
				yield break;
			}
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["StatisticName"] = EscapeStatistic,
				["StartPosition"] = 0,
				["MaxResultsCount"] = 10,
				["ProfileConstraints"] = new Dictionary<string, object> { ["ShowDisplayName"] = true }
			};
			object data = null;
			yield return Call("GetLeaderboard", body, true, d =>
			{
				// Rebuilt from native code (token 0x06000760, RVA 0x280C39C)
				data = d;
			});
			List<object> rows = MiniJson.List(data, "Leaderboard");
			if (rows == null)
			{
				yield break;
			}
			List<BoardEntry> board = new List<BoardEntry>();
			foreach (object row in rows)
			{
				double value = MiniJson.Num(row, "StatValue");
				if (value >= 0.0 || value <= -OffBoard)
				{
					continue;
				}
				string name = MiniJson.Str(row, "DisplayName") ?? MiniJson.Str(MiniJson.Obj(row, "Profile"), "DisplayName");
				board.Add(new BoardEntry
				{
					name = string.IsNullOrEmpty(name) ? "GORILLA" : PlayerProfile.Filter(name),
					time = (float)(value / -1000.0),
					mine = MiniJson.Str(row, "PlayFabId") == playFabId
				});
			}
			board.Sort((a, b) =>
			{
				// Rebuilt from native code (token 0x0600074D, RVA 0x280C1BC)
				return a.time.CompareTo(b.time);
			});
			Board = board;
			BoardChanged?.Invoke();
		}

		private static IEnumerator Call(string api, Dictionary<string, object> body, bool signedIn, Action<object> ok, Action<string> failed = null)
		{
			// Rebuilt from native code (token 0x06000746, RVA 0x280B4D8; state machine 0x06000763, RVA 0x280C3C0)
#if UNITY_EDITOR
			// OfflineGuard (Tools/Recovery/add_offline_guard.py): keep when rebuilding this body.
			if (System.Environment.GetEnvironmentVariable("RIPPLE_ALLOW_ONLINE") != "1")
			{
				UnityEngine.Debug.LogWarning("[OfflineGuard] blocked PlayFab Call; set RIPPLE_ALLOW_ONLINE=1 to allow");
				failed?.Invoke("offline"); yield break;
			}
#endif
			if (signedIn && !LoggedIn)
			{
				yield break;
			}
			using (UnityWebRequest request = new UnityWebRequest("https://" + TitleId.ToLowerInvariant() + ".playfabapi.com/Client/" + api, "POST"))
			{
				request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(MiniJson.Write(body)));
				request.downloadHandler = new DownloadHandlerBuffer();
				request.SetRequestHeader("Content-Type", "application/json");
				if (signedIn)
				{
					request.SetRequestHeader("X-Authorization", ticket);
				}
				request.timeout = 15;
				yield return request.SendWebRequest();
				object reply = MiniJson.Parse(request.downloadHandler != null ? request.downloadHandler.text : null);
				if (request.result != UnityWebRequest.Result.Success || MiniJson.Num(reply, "code") != 200.0)
				{
					string error = MiniJson.Str(reply, "error") ?? request.error ?? "Unknown";
					string message = MiniJson.Str(reply, "errorMessage") ?? request.error;
					if (error == "NotAuthenticated" || error == "InvalidSessionTicket" || error == "ExpiredAuthToken")
					{
						ticket = null;
					}
					Debug.LogWarning("Shallows: PlayFab " + api + " failed: " + error + " (" + message + ")");
					failed?.Invoke(error);
					yield break;
				}
				ok?.Invoke(MiniJson.Obj(reply, "data"));
			}
		}

		private static string DeviceId()
		{
			// Rebuilt from native code (token 0x06000747, RVA 0x280BDD8)
			string id = SystemInfo.deviceUniqueIdentifier;
			if (string.IsNullOrEmpty(id) || id == "n/a")
			{
				id = PlayerPrefs.GetString("shallows.deviceId", "");
				if (id.Length == 0)
				{
					id = Guid.NewGuid().ToString("N");
					PlayerPrefs.SetString("shallows.deviceId", id);
					PlayerPrefs.Save();
				}
			}
			return (Application.isEditor ? "editor-" : "device-") + id;
		}
	}
}
