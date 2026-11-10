using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Photon.Realtime;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Android;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(-20)]
	public class NetworkManager : MonoBehaviour
	{
		public int maxPlayers = 10;

		public const string PrivateRoomRegion = "us";

		public const string NetVersion = "18";

		public const string UpdateMessage = "UPDATE NEEDED!\nTHIS VERSION OF RIPPLE\nIS TOO OLD TO PLAY ONLINE.\nUPDATE IT IN THE META STORE.";

		[Tooltip("How often the master client sends the monsters' state (per second).")]
		public float monsterSendRate = 10f;

		public Material pebbleMaterial;

		public NetworkObject avatarPrefab;

		public static NetworkManager I { get; private set; }

		public string Status { get; private set; } = "OFFLINE";

		public string RoomCode { get; private set; } = "";

		public bool Connected { get; private set; }

		public int PlayerCount { get; private set; } = 1;

		public string Region { get; private set; } = "";

		private const string VersionKey = "v";

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private const float MasterSilentAfter = 3f;

		private NetworkRunner runner;

		private Recorder recorder;

		private MicLoudness micLoudness;

		private bool joining;

		private string wantedCode = "";

		private float retryAt = -1f;

		private float nextMonsterSend;

		private float nextClockSend;

		private int lastPlayerCount = 1;

		private bool blocked;

		private AuthenticationValues auth;

		public static bool UpdateNeeded
		{
			get
			{
				// Rebuilt from native code (token 0x06000647, RVA 0x27F4EAC)
				return PlayFabClient.MinNetVersion > int.Parse(NetVersion);
			}
		}

		public float MicLevel
		{
			get
			{
				// Rebuilt from native code (token 0x0600064A, RVA 0x27F504C)
				if (!recorder || !recorder.TransmitEnabled || !micLoudness)
				{
					return 0f;
				}
				return micLoudness.Loudness;
			}
		}

		public NetworkRunner Runner
		{
			get
			{
				// Rebuilt from native code (token 0x06000654, RVA 0x27F7AD8)
				return runner;
			}
		}

		private bool Live
		{
			get
			{
				// Rebuilt from native code (token 0x0600065C, RVA 0x27F664C)
				return runner && runner.IsRunning && Connected;
			}
		}

		public static string PhotonName(string code)
		{
			// Rebuilt from native code (token 0x06000648, RVA 0x27F4F5C)
			if (code == null)
			{
				return null;
			}
			return NetVersion + "-" + code;
		}

		private static string CodeOf(string photonName)
		{
			// Rebuilt from native code (token 0x06000649, RVA 0x27F4FBC)
			if (string.IsNullOrEmpty(photonName))
			{
				return "";
			}
			int dash = photonName.IndexOf('-');
			return dash < 0 ? photonName : photonName.Substring(dash + 1);
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600064B, RVA 0x27F5104)
			I = this;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x0600064C, RVA 0x27F519C)
			PlayerProfile.Load();
			PlayerProfile.Changed += OnProfileChanged;
			NetBridge.SilencedChanged += OnProfileChanged;
			OnProfileChanged();
			NetBridge.LocalSplash += SendSplash;
			NetBridge.LocalCaught += SendCaught;
			NetBridge.LocalThrow += SendThrow;
			NetBridge.LocalItem += SendItem;
			NetBridge.LocalItemTaken += SendItemTaken;
			NetBridge.LocalSponge += SendSponge;
			NetBridge.LocalTap += SendTap;
			NetBridge.LocalEscape += SendEscape;
			NetBridge.LocalFuseRequest += SendFuseRequest;
			NetBridge.FuseStateChanged += SendFuses;
			NetBridge.LocalFuseThrow += SendFuseThrow;
			NetBridge.LocalPebble += SendPebble;
			NetBridge.PebblesShared += SendPebbles;
			NetBridge.TreeShared += SendTree;
			NetBridge.LocalTreeRip += SendTreeRip;
			NetBridge.PlayerInRoom = InRoom;
			if (!GetComponent<CheatWatch>())
			{
				gameObject.AddComponent<CheatWatch>();
			}
			if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
			{
				Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
			}
			Join("");
		}

		private void OnApplicationPause(bool paused)
		{
			// Rebuilt from native code (token 0x0600064D, RVA 0x27F60B0)
			if (!paused || !Live || !runner.IsSharedModeMasterClient || PlayerCount < 2)
			{
				return;
			}
			PlayerRef heir = PlayerRef.None;
			foreach (PlayerRef player in runner.ActivePlayers)
			{
				if (player == runner.LocalPlayer)
				{
					continue;
				}
				if (heir == PlayerRef.None || player.PlayerId < heir.PlayerId)
				{
					heir = player;
				}
			}
			if (heir == PlayerRef.None)
			{
				return;
			}
			try
			{
				runner.SetMasterClient(heir);
				Debug.Log($"Shallows: pausing, so player {heir.PlayerId} runs the monsters now.");
			}
			catch (Exception e)
			{
				Debug.LogWarning("Shallows: couldn't hand the monsters over before pausing: " + e.Message);
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x0600064E, RVA 0x27F66DC)
			PlayerProfile.Changed -= OnProfileChanged;
			NetBridge.SilencedChanged -= OnProfileChanged;
			NetBridge.LocalSplash -= SendSplash;
			NetBridge.LocalCaught -= SendCaught;
			NetBridge.LocalThrow -= SendThrow;
			NetBridge.LocalItem -= SendItem;
			NetBridge.LocalItemTaken -= SendItemTaken;
			NetBridge.LocalSponge -= SendSponge;
			NetBridge.LocalTap -= SendTap;
			NetBridge.LocalEscape -= SendEscape;
			NetBridge.LocalFuseRequest -= SendFuseRequest;
			NetBridge.FuseStateChanged -= SendFuses;
			NetBridge.LocalFuseThrow -= SendFuseThrow;
			NetBridge.LocalPebble -= SendPebble;
			NetBridge.PebblesShared -= SendPebbles;
			NetBridge.TreeShared -= SendTree;
			NetBridge.LocalTreeRip -= SendTreeRip;
			NetBridge.MonsterAuthority = true;
			NetBridge.MasterSilent = false;
			NetBridge.PlayerInRoom = id => NetBridge.LocalPlayerId == id;
			NetBridge.RemoteFeet.Clear();
			NetBridge.RemoteHands.Clear();
			NetBridge.RemoteVoices.Clear();
			if (runner && runner.IsRunning)
			{
				runner.Shutdown(true, ShutdownReason.Ok, false);
			}
			else if (runner)
			{
				Destroy(runner.gameObject);
			}
		}

		private void OnProfileChanged()
		{
			// Rebuilt from native code (token 0x0600064F, RVA 0x27F5D18)
			PlayerRig rig = PlayerRig.I;
			if (rig)
			{
				MaterialPropertyBlock block = new MaterialPropertyBlock();
				block.SetColor(BaseColorId, Color.Lerp(PlayerProfile.Color, Color.black, 0.35f));
				foreach (Transform hand in new Transform[] { rig.leftHand, rig.rightHand })
				{
					if (hand && hand.TryGetComponent(out Renderer renderer))
					{
						renderer.SetPropertyBlock(block);
					}
				}
			}
			UpdateMic();
		}

		private void UpdateMic()
		{
			// Rebuilt from native code (token 0x06000650, RVA 0x27F7014)
			if (!recorder)
			{
				return;
			}
			bool transmit = PlayerProfile.MicLive(XRHands.TalkHeld()) && !NetBridge.Silenced;
			if (transmit != recorder.TransmitEnabled)
			{
				recorder.TransmitEnabled = transmit;
			}
			bool detect = PlayerProfile.Mic != PlayerProfile.MicMode.PushToTalk;
			if (detect != recorder.VoiceDetection)
			{
				recorder.VoiceDetection = detect;
			}
		}

		public void JoinPublic()
		{
			// Rebuilt from native code (token 0x06000651, RVA 0x27F7348)
			Join("");
		}

		public void JoinPrivate(string code)
		{
			// Rebuilt from native code (token 0x06000652, RVA 0x27F7390)
			code = PlayerProfile.Filter(code);
			if (code.Length > 0)
			{
				Join(code);
			}
		}

		public GameObject SpawnGhost(int kind, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000653, RVA 0x27F75BC)
			GameObject go;
			Collider collider;
			if (LoungeCan.Is(kind))
			{
				go = LoungeCan.Visual();
				go.transform.position = position;
				collider = LoungeCan.GhostCollider(go);
			}
			else if (kind >= 0 && kind <= 3 && (kind != 0 || ItemModels.Has("Pebble_A")))
			{
				go = Items.Visual((ItemKind)kind);
				go.transform.position = position;
				collider = Items.GhostCollider(go, (ItemKind)kind);
				switch ((ItemKind)kind)
				{
					case ItemKind.GlowStick:
						go.AddComponent<GlowStick>().ghost = true;
						break;
					case ItemKind.Sponge:
						go.AddComponent<Sponge>().ghost = true;
						break;
					case ItemKind.Duck:
						go.AddComponent<RubberDuck>().ghost = true;
						break;
				}
			}
			else
			{
				go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				go.layer = 2;
				go.transform.position = position;
				go.transform.localScale = new Vector3(0.075f, 0.06f, 0.07f);
				if (pebbleMaterial)
				{
					go.GetComponent<Renderer>().sharedMaterial = pebbleMaterial;
				}
				collider = go.GetComponent<Collider>();
			}
			go.name = "Thrown by another player";
			if (PlayerRig.I)
			{
				foreach (Collider body in PlayerRig.I.Colliders)
				{
					if (body)
					{
						Physics.IgnoreCollision(collider, body, true);
					}
				}
			}
			Rigidbody rb = go.AddComponent<Rigidbody>();
			rb.mass = 0.15f;
			rb.linearVelocity = velocity;
			rb.angularVelocity = Random.insideUnitSphere * 6f;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			float life;
			switch ((ItemKind)kind)
			{
				case ItemKind.GlowStick:
					life = 61f;
					break;
				case ItemKind.Duck:
					life = 6f;
					break;
				case ItemKind.Sponge:
					life = 10f;
					break;
				default:
					life = 3f;
					break;
			}
			Destroy(go, life);
			return go;
		}

		private async void Join(string code)
		{
			// Rebuilt from native code (token 0x06000655, RVA 0x27F5FEC; state machine 0x06000677, RVA 0x27FAC9C)
			wantedCode = code;
			if (joining)
			{
				return;
			}
			joining = true;
			retryAt = -1f;
			try
			{
				string target;
				bool started;
				do
				{
					target = wantedCode;
					await Leave();
					Status = target.Length > 0 ? "JOINING " + target + "..." : "CONNECTING...";
					started = await StartRunner(target);
				}
				while (target != wantedCode);
				if (!started)
				{
					retryAt = Time.time + 5f;
				}
			}
			finally
			{
				joining = false;
			}
		}

		private async Task Leave()
		{
			// Rebuilt from native code (token 0x06000656, RVA 0x27F7AE0; state machine 0x06000679, RVA 0x27FB18C)
			Connected = false;
			PlayerCount = 1;
			NetBridge.MonsterAuthority = true;
			NetBridge.MasterSilent = false;
			if (!runner)
			{
				return;
			}
			NetworkRunner old = runner;
			runner = null;
			if (old.IsRunning)
			{
				await old.Shutdown(true, ShutdownReason.Ok, false);
			}
			if (old)
			{
				Destroy(old.gameObject);
			}
		}

		private async Task<bool> StartRunner(string code)
		{
			// Rebuilt from native code (token 0x06000657, RVA 0x27F7BBC; state machine 0x0600067B, RVA 0x27FB574)
#if UNITY_EDITOR
			// OfflineGuard (Tools/Recovery/add_offline_guard.py): keep when rebuilding this body.
			if (System.Environment.GetEnvironmentVariable("RIPPLE_ALLOW_ONLINE") != "1")
			{
				UnityEngine.Debug.LogWarning("[OfflineGuard] blocked Fusion StartRunner; set RIPPLE_ALLOW_ONLINE=1 to allow");
				return false;
			}
#endif
			NetGuard.Clear();
			auth = await Auth();
			if (UpdateNeeded)
			{
				Status = UpdateMessage;
				return false;
			}
			bool started;
			if (code.Length == 0)
			{
				started = await TryStart(null, true, false) || await TryStart(NewPublicCode(), true, true);
			}
			else
			{
				started = await TryStart(code, false, true);
			}
			if (!started)
			{
				Status = "OFFLINE, RETRYING...";
				return false;
			}
			if (runner.SessionInfo.Properties != null && runner.SessionInfo.Properties.TryGetValue(VersionKey, out SessionProperty version) && version.PropertyValue is string roomVersion && roomVersion != NetVersion)
			{
				Debug.LogWarning("Shallows: room " + runner.SessionInfo.Name + " is for net version " + roomVersion + ", not 18. Leaving it.");
				await Leave();
				Status = "OFFLINE, RETRYING...";
				return false;
			}
			RoomCode = CodeOf(runner.SessionInfo.Name);
			Connected = true;
			NetBridge.LocalPlayerId = runner.LocalPlayer.PlayerId;
			Region = runner.SessionInfo.Region;
			PlayerRig rig = PlayerRig.I;
			if (avatarPrefab)
			{
				runner.Spawn(avatarPrefab, rig ? rig.head.position : Vector3.zero, Quaternion.identity, runner.LocalPlayer);
			}
			bool master = runner.IsSharedModeMasterClient;
			NetBridge.MonsterAuthority = master;
			lastPlayerCount = 1;
			NetBridge.LastMonsterUpdate = Time.time;
			NetBridge.ReportRoomJoined(master);
			Debug.Log("Shallows: joined room " + RoomCode + " (" + (runner.SessionInfo.IsVisible ? "public" : "private") + ") in region " + Region + ".");
			return true;
		}

		private async Task<AuthenticationValues> Auth()
		{
			// Rebuilt from native code (token 0x06000658, RVA 0x27F7CDC; state machine 0x06000675, RVA 0x27FA2C8)
			for (int i = 0; i < 32 && !PlayFabClient.LoggedIn; i++)
			{
				await Task.Delay(250);
			}
			if (!PlayFabClient.LoggedIn)
			{
				return null;
			}
			TaskCompletionSource<string> answer = new TaskCompletionSource<string>();
			PlayFabClient.PhotonToken(PhotonAppSettings.Global.AppSettings.AppIdFusion, token => answer.TrySetResult(token));
			string photonToken = await answer.Task;
			string playFabId = PlayFabClient.PlayFabId;
			if (string.IsNullOrEmpty(photonToken) || string.IsNullOrEmpty(playFabId))
			{
				return null;
			}
			AuthenticationValues values = new AuthenticationValues(playFabId);
			values.AuthType = CustomAuthenticationType.Custom;
			values.AddAuthParameter("username", playFabId);
			values.AddAuthParameter("token", photonToken);
			return values;
		}

		private async Task<bool> TryStart(string sessionName, bool visible, bool create)
		{
			// Rebuilt from native code (token 0x06000659, RVA 0x27F7DC8; state machine 0x0600067D, RVA 0x27FC560)
			GameObject go = new GameObject("Network Runner");
			runner = go.AddComponent<NetworkRunner>();
			runner.ProvideInput = false;
			go.AddComponent<FusionVoiceClient>().UseFusionAuthValues = true;
			recorder = go.AddComponent<Recorder>();
			recorder.InputFactory = () => new SharedMicrophoneSource();
			recorder.SourceType = Recorder.InputSourceType.Factory;
			UpdateMic();
			micLoudness = go.AddComponent<MicLoudness>();
			go.GetComponent<FusionVoiceClient>().PrimaryRecorder = recorder;
			StartGameArgs args = new StartGameArgs
			{
				GameMode = GameMode.Shared,
				SessionName = PhotonName(sessionName),
				SessionProperties = new Dictionary<string, SessionProperty> { [VersionKey] = NetVersion },
				IsVisible = visible,
				IsOpen = true,
				PlayerCount = maxPlayers,
				EnableClientSessionCreation = create,
				AuthValues = auth
			};
			FusionAppSettings settings = PhotonAppSettings.Global.AppSettings.GetCopy();
			settings.AppVersion = NetVersion;
			if (!visible)
			{
				settings.FixedRegion = PrivateRoomRegion;
			}
			args.CustomPhotonAppSettings = settings;
			StartGameResult result = await runner.StartGame(args);
			if (result.Ok && runner)
			{
				return true;
			}
			if (auth != null && result.ShutdownReason is ShutdownReason.InvalidAuthentication or ShutdownReason.CustomAuthenticationFailed or ShutdownReason.AuthenticationTicketExpired)
			{
				Debug.LogWarning(string.Format("Shallows: Photon refused the PlayFab sign-in ({0}). Trying again without it.", result.ShutdownReason));
				auth = null;
			}
			if (create)
			{
				Debug.LogWarning(string.Format("Shallows: couldn't connect ({0}). Playing offline, retrying in 5 s.", result.ShutdownReason));
			}
			if (runner)
			{
				Destroy(runner.gameObject);
			}
			runner = null;
			return false;
		}

		private static string NewPublicCode()
		{
			// Rebuilt from native code (token 0x0600065A, RVA 0x27F7F04)
			const string Letters = "BCDFGHJKLMNPQRSTVWXZ";
			string code;
			do
			{
				char[] chars = new char[4];
				for (int i = 0; i < chars.Length; i++)
				{
					chars[i] = Letters[Random.Range(0, Letters.Length)];
				}
				code = new string(chars);
			}
			while (PlayerProfile.Filter(code) != code);
			return code;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600065B, RVA 0x27F8038)
			UpdateMic();
			if (UpdateNeeded)
			{
				if (runner && !joining)
				{
					_ = Leave();
				}
				Status = UpdateMessage;
				retryAt = -1f;
				blocked = true;
				NetBridge.MonsterAuthority = true;
				NetBridge.MasterSilent = false;
				NetBridge.ResetClock();
				NetBridge.RemoteFeet.Clear();
				NetBridge.RemoteHands.Clear();
				NetBridge.RemoteVoices.Clear();
				return;
			}
			if (blocked)
			{
				blocked = false;
				if (!joining)
				{
					Join(wantedCode);
				}
			}
			bool live = Live;
			bool master = !live || runner.IsSharedModeMasterClient;
			NetBridge.MonsterAuthority = master;
			NetBridge.MasterSilent = !master && PlayerCount >= 2 && Time.time - NetBridge.LastMonsterUpdate > MasterSilentAfter;
			if (NetBridge.MonsterAuthority)
			{
				NetBridge.ResetClock();
			}
			else
			{
				NetBridge.TickClock(Time.deltaTime);
			}
			if (!live)
			{
				if (Connected)
				{
					Debug.LogWarning("Shallows: lost the connection. Playing offline and reconnecting.");
					Connected = false;
					Status = "RECONNECTING...";
					retryAt = Time.time + 3f;
				}
				if (!joining && retryAt > 0f && Time.time >= retryAt)
				{
					Join(wantedCode);
				}
				NetBridge.RemoteFeet.Clear();
				NetBridge.RemoteHands.Clear();
				NetBridge.RemoteVoices.Clear();
				return;
			}
			int count = 0;
			foreach (PlayerRef player in runner.ActivePlayers)
			{
				count++;
			}
			PlayerCount = Mathf.Max(count, 1);
			if (PlayerCount > lastPlayerCount && runner.IsSharedModeMasterClient)
			{
				NetBridge.ReportRoomGrew();
			}
			lastPlayerCount = PlayerCount;
			Status = string.Format("CURRENT ROOM: {0}\nPLAYERS: {1}/{2}", RoomCode, PlayerCount, maxPlayers);
			NetBridge.RemoteFeet.Clear();
			NetBridge.RemoteHands.Clear();
			NetBridge.RemoteVoices.Clear();
			foreach (NetAvatar avatar in NetAvatar.Remotes)
			{
				if (!avatar || !avatar.head)
				{
					continue;
				}
				NetBridge.RemoteFeet.Add(avatar.Feet);
				NetBridge.RemoteVoices.Add(avatar.VoiceLoudness);
				if (avatar.leftHand)
				{
					NetBridge.RemoteHands.Add(avatar.leftHand.position);
				}
				if (avatar.rightHand)
				{
					NetBridge.RemoteHands.Add(avatar.rightHand.position);
				}
			}
			if (runner.IsSharedModeMasterClient && PlayerCount >= 2 && Time.time >= nextClockSend)
			{
				nextClockSend = Time.time + 2f;
				NetAvatar.Rpc_Clock(runner, NetBridge.SharedTime);
			}
			if (runner.IsSharedModeMasterClient && PlayerCount >= 2 && Time.time >= nextMonsterSend)
			{
				nextMonsterSend = Time.time + 1f / Mathf.Max(monsterSendRate, 1f);
				float sentAt = NetBridge.SharedTime;
				foreach (MonsterAI monster in MonsterAI.All)
				{
					Transform t = monster.transform;
					NetAvatar.Rpc_Monster(runner, monster.index, t.position, t.eulerAngles.y, (int)monster.Current, monster.Target, monster.Speed, sentAt);
				}
			}
		}

		private bool InRoom(int id)
		{
			// Rebuilt from native code (token 0x0600065D, RVA 0x27F8EF4)
			if (!Live)
			{
				return NetBridge.LocalPlayerId == id;
			}
			foreach (PlayerRef player in runner.ActivePlayers)
			{
				if (player.PlayerId == id)
				{
					return true;
				}
			}
			return false;
		}

		private void SendSplash(SplashEvent e)
		{
			// Rebuilt from native code (token 0x0600065E, RVA 0x27F9248)
			if (Live)
			{
				NetAvatar.Rpc_Splash(runner, e.position, e.intensity, (int)e.kind, e.heading);
			}
		}

		private void SendCaught(int monster, int kill)
		{
			// Rebuilt from native code (token 0x0600065F, RVA 0x27F9320)
			if (!Live || !PlayerRig.I)
			{
				return;
			}
			Color fur = PlayerProfile.Color;
			NetAvatar.Rpc_Caught(runner, monster, PlayerRig.I.BodyBottom, new Vector3(fur.r, fur.g, fur.b), NetBridge.LocalPlayerId, kill);
		}

		private void SendThrow(int kind, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000660, RVA 0x27F9508)
			if (Live)
			{
				NetAvatar.Rpc_Throw(runner, kind, position, velocity);
			}
		}

		private void SendItem(int kind, int id, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000661, RVA 0x27F95D8)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_Item(runner, kind, id, position, velocity);
			}
		}

		private void SendItemTaken(int id, bool gone, Vector3 position)
		{
			// Rebuilt from native code (token 0x06000662, RVA 0x27F96C4)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_ItemTaken(runner, id, gone);
			}
		}

		private void SendSponge(Vector3 position, float yaw)
		{
			// Rebuilt from native code (token 0x06000663, RVA 0x27F9768)
			if (Live)
			{
				NetAvatar.Rpc_Sponge(runner, position, yaw);
			}
		}

		private void SendTap(Vector3 position, float hit)
		{
			// Rebuilt from native code (token 0x06000664, RVA 0x27F9818)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_Tap(runner, position, hit);
			}
		}

		private void SendEscape(string name, float time)
		{
			// Rebuilt from native code (token 0x06000665, RVA 0x27F98D4)
			if (Live)
			{
				NetAvatar.Rpc_Escape(runner, name, time);
			}
		}

		private void SendFuseRequest(int action, int fuse, Vector3 position)
		{
			// Rebuilt from native code (token 0x06000666, RVA 0x27F996C)
			if (Live)
			{
				NetAvatar.Rpc_FuseRequest(runner, action, fuse, position, NetBridge.LocalPlayerId);
			}
		}

		private void SendFuseThrow(int fuse, Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000667, RVA 0x27F9A64)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_FuseThrow(runner, fuse, position, velocity, NetBridge.LocalPlayerId);
			}
		}

		private void SendPebble(int action, int pebble, Vector3 position)
		{
			// Rebuilt from native code (token 0x06000668, RVA 0x27F9B78)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_Pebble(runner, action, pebble, position);
			}
		}

		private void SendPebbles(PebbleSnapshot s)
		{
			// Rebuilt from native code (token 0x06000669, RVA 0x27F9C44)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_Pebbles(runner, s.seed, (int)s.taken, (int)(s.taken >> 32));
			}
		}

		private void SendTree(TreeSnapshot s)
		{
			// Rebuilt from native code (token 0x0600066A, RVA 0x27F9CEC)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_Tree(runner, s.id, s.level, s.position, s.yaw, s.state, s.winner, s.age);
			}
		}

		private void SendTreeRip(int id)
		{
			// Rebuilt from native code (token 0x0600066B, RVA 0x27F9DDC)
			if (Live && PlayerCount >= 2)
			{
				NetAvatar.Rpc_TreeRip(runner, id);
			}
		}

		private void SendFuses(FuseSnapshot s)
		{
			// Rebuilt from native code (token 0x0600066C, RVA 0x27F9E70)
			if (Live && PlayerCount >= 2)
			{
				int holders = ((s.holder0 + 1) & 0xFF) | (((s.holder1 + 1) & 0xFF) << 8) | (((s.holder2 + 1) & 0xFF) << 16);
				NetAvatar.Rpc_Fuses(runner, s.packed, holders, s.loose0, s.loose1, s.loose2, s.openLeft, s.round);
			}
		}
	}
}
