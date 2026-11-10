using System;
using UnityEngine;

namespace Shallows
{
	public class LobbySpeaker : MonoBehaviour
	{
		public const string Model = "LobbySpeakerPost";

		private const string MutedPref = "shallows.lobbymusic.muted";

		[Tooltip("How loud the music is at the post (0-1). It's background music: keep it low.")]
		[Range(0f, 1f)]
		public float volume = 0.3f;

		[Tooltip("How far out of the spawn room you stop hearing it (m). 0 = halfway through Level 1, worked out from the level decks.")]
		public float fadeOut;

		[Tooltip("How far out of the spawn room (m) the big hall echo takes over.")]
		public float echoRamp = 4f;

		private AudioSource music;

		private AudioReverbFilter hall;

		private AudioLowPassFilter muffle;

		private PokeButton button;

		private Renderer slash;

		private float level;

		private float away = -1f;

		private float fadeDistance = 55f;

		private Vector3 postAt;

		public static bool Muted
		{
			get
			{
				// Rebuilt from native code (token 0x06000443, RVA 0x27AD20C)
				return PlayerPrefs.GetInt(MutedPref, 0) == 1;
			}
			set
			{
				// Rebuilt from native code (token 0x06000444, RVA 0x27AD260)
				PlayerPrefs.SetInt(MutedPref, value ? 1 : 0);
				PlayerPrefs.Save();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void EnsureOne()
		{
			// Rebuilt from native code (token 0x06000445, RVA 0x27AD2B4)
			if (FindAnyObjectByType<LobbySpeaker>(FindObjectsInactive.Include))
			{
				return;
			}
			GameManager game = FindAnyObjectByType<GameManager>();
			if (game)
			{
				Place(game);
			}
		}

		public static LobbySpeaker Place(GameManager game)
		{
			// Rebuilt from native code (token 0x06000446, RVA 0x27AD378)
			float x = game.safeZone.center.x;
			float z = game.safeZone.max.z - 0.45f;
			float y = Physics.Raycast(new Vector3(x, game.safeZone.center.y + 1f, z), Vector3.down, out RaycastHit hit, 6f, -1, QueryTriggerInteraction.Ignore) ? hit.point.y : 0.38f;
			GameObject gameObject = new GameObject("Lobby Speaker");
			gameObject.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.identity);
			LobbySpeaker speaker = gameObject.AddComponent<LobbySpeaker>();
			speaker.WearModel();
			return speaker;
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x06000447, RVA 0x27AD580)
			return LifeguardProps.Dress(transform, Model, Vector3.zero, Quaternion.identity);
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000448, RVA 0x27AD720)
			Transform model = WearModel();
			Transform part = model ? LifeguardProps.Find(model, "Mute Button") : null;
			if (part)
			{
				button = LifeguardProps.Button(part, transform.forward * 0.012f);
				button.repeat = false;
				button.onPress += () => SetMuted(!Muted);
				Transform cap = LifeguardProps.Find(model, "Mute Cap Muted");
				if (cap)
				{
					cap.SetParent(part, true);
					slash = cap.GetComponent<Renderer>();
				}
			}
			AudioClip clip = Resources.Load<AudioClip>("Music/LobbyMusic");
			if (!clip)
			{
				return;
			}
			Transform source = model ? LifeguardProps.Find(model, "Music Source") : null;
			GameObject gameObject = new GameObject("Music");
			gameObject.transform.SetParent(transform, false);
			gameObject.transform.position = source ? source.position : transform.position + Vector3.up * 2.85f;
			music = gameObject.AddComponent<AudioSource>();
			music.clip = clip;
			music.loop = true;
			music.playOnAwake = false;
			music.volume = 0f;
			music.priority = 0;
			music.ignoreListenerPause = true;
			music.dopplerLevel = 0f;
			music.rolloffMode = AudioRolloffMode.Custom;
			music.maxDistance = 1000f;
			music.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f)));
			postAt = gameObject.transform.position;
			fadeDistance = fadeOut > 0f ? fadeOut : HalfOfLevelOne();
			muffle = gameObject.AddComponent<AudioLowPassFilter>();
			hall = gameObject.AddComponent<AudioReverbFilter>();
			hall.reverbPreset = AudioReverbPreset.User;
			hall.decayTime = 6.5f;
			hall.decayHFRatio = 0.45f;
			hall.reflectionsDelay = 0.09f;
			hall.reverbDelay = 0.1f;
			hall.diffusion = 100f;
			hall.density = 100f;
			hall.roomHF = -600f;
			hall.roomLF = 0f;
			hall.hfReference = 5000f;
			hall.lfReference = 250f;
			level = Muted ? 0f : 1f;
			ShowMuted();
			Update();
			if (!Muted)
			{
				music.Play();
			}
		}

		public void SetMuted(bool muted)
		{
			// Rebuilt from native code (token 0x06000449, RVA 0x27AE734)
			Muted = muted;
			ShowMuted();
			if (!muted && music && !music.isPlaying)
			{
				if (music.time > 0f)
				{
					music.UnPause();
				}
				else
				{
					music.Play();
				}
			}
		}

		private void ShowMuted()
		{
			// Rebuilt from native code (token 0x0600044A, RVA 0x27AE1B0)
			bool muted = Muted;
			if (slash)
			{
				slash.enabled = muted;
			}
			if (button)
			{
				button.SetIdleColor(muted ? new Color(0.8f, 0.72f, 0.7f, 1f) : new Color(1f, 1f, 1f, 1f));
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600044B, RVA 0x27AE2B0)
			if (!music)
			{
				return;
			}
			float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
			level = Mathf.MoveTowards(level, Muted ? 0f : 1f, dt / 0.6f);
			if (level <= 0f && music.isPlaying)
			{
				music.Pause();
			}
			PlayerRig rig = PlayerRig.I;
			GameManager game = GameManager.I;
			Vector3 heard = postAt;
			float outOfRoom = 0f;
			float echo = 0f;
			if (rig && rig.head && game)
			{
				Vector3 head = rig.head.position;
				outOfRoom = RippleShop.Contains(head) ? 0f : Mathf.Sqrt(game.safeZone.SqrDistance(head));
				echo = Mathf.Clamp01(outOfRoom / Mathf.Max(echoRamp, 0.01f));
				if (outOfRoom > 0f)
				{
					heard = game.safeZone.ClosestPoint(head);
				}
				music.volume = volume * level * Loudness(Vector3.Distance(head, postAt), outOfRoom);
			}
			else
			{
				music.volume = volume * level;
			}
			away = away < 0f ? echo : Mathf.MoveTowards(away, echo, dt / 1.5f);
			music.transform.position = Vector3.Lerp(postAt, heard, Mathf.Clamp01(outOfRoom / 1.5f));
			Apply(away);
		}

		private float Loudness(float fromPost, float outOfRoom)
		{
			// Rebuilt from native code (token 0x0600044C, RVA 0x27AE80C)
			float loud = Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(5f, 15f, fromPost));
			if (outOfRoom > 0f)
			{
				loud *= MathF.Pow(1f - Mathf.Clamp01(outOfRoom / Mathf.Max(fadeDistance, 1f)), 1.6f);
			}
			return loud;
		}

		private static float HalfOfLevelOne()
		{
			// Rebuilt from native code (token 0x0600044D, RVA 0x27ADF94)
			GameManager game = GameManager.I ? GameManager.I : FindAnyObjectByType<GameManager>();
			if (game && game.levels != null && game.levels.Length >= 2)
			{
				Bounds zone = game.safeZone;
				Vector3 deck = game.levels[1].startDeck.ClosestPoint(zone.center);
				float distance = Mathf.Sqrt(zone.SqrDistance(deck));
				if (distance > 10f)
				{
					return distance * 0.5f;
				}
			}
			return 55f;
		}

		private void Apply(float a)
		{
			// Rebuilt from native code (token 0x0600044E, RVA 0x27AE89C)
			music.spatialBlend = Mathf.Lerp(0.8f, 0.9f, a);
			music.spread = Mathf.Lerp(90f, 120f, a);
			muffle.cutoffFrequency = Mathf.Lerp(22000f, 3500f, a);
			float echo = ShallowsTuning.I ? ShallowsTuning.I.echo : 1f;
			if (echo * a < 0.02f)
			{
				hall.dryLevel = 0f;
				hall.room = -10000f;
				return;
			}
			float db = MathF.Log10(echo * a) * 2000f;
			hall.dryLevel = Mathf.Lerp(0f, -900f, a);
			hall.room = Mathf.Clamp(db + -300f, -10000f, 0f);
			hall.reflectionsLevel = Mathf.Clamp(db + -900f, -10000f, 1000f);
			hall.reverbLevel = Mathf.Clamp(db + 200f, -10000f, 2000f);
		}
	}
}
