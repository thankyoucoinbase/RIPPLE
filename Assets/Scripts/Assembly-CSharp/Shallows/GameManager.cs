using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(-30)]
	public class GameManager : MonoBehaviour
	{
		private enum Phase
		{
			Starting = 0,
			Playing = 1,
			Dying = 2,
			Escaping = 3
		}

		public PlayerRig player;

		public Transform spawnPoint;

		[Tooltip("The banana bunch. Grip it and yank it off the branch to escape (BananaRip).")]
		public Transform goal;

		[Tooltip("How close a hand has to be to the middle of the bunch to grab it.")]
		public float goalTouchDistance = 0.45f;

		[Tooltip("The dry spawn deck. Monsters can't catch you here, and the timer starts when you leave it.")]
		public Bounds safeZone = new Bounds(new Vector3(-27f, 1.5f, 0f), new Vector3(6f, 4f, 28f));

		[Tooltip("The levels in order. Leave empty for a single room where you always respawn at the spawn point.")]
		public LevelInfo[] levels;

		public HandGrabber[] grabbers;

		public TextMesh wristText;

		public TextMesh messageText;

		[Tooltip("2D source for sounds that play in your head, like the escape chime.")]
		public AudioSource localAudio;

		private Phase phase;

		private BananaRip rip;

		private MonsterAI caughtBy;

		private bool skipped;

		private int checkpoint = -1;

		private float runStart = -1f;

		private float best = -1f;

		private float messageUntil;

		private float smoothedFrame = 0.013888889f;

		private static readonly Color EscapeWhite = new Color(1f, 0.97f, 0.88f, 1f);

		private int escapePaid = -1;

		private string escapeNote = "";

		private float escapeShownUntil = -1f;

		private float silencedSince = -1f;

		private TextMesh[] messageOutline = new TextMesh[0];

		private static readonly Color MessageOutlineColor = new Color(0.06f, 0.05f, 0.08f, 1f);

		public static GameManager I { get; private set; }

		public bool InSafeZone
		{
			get
			{
				// Rebuilt from native code (token 0x0600018C, RVA 0x27295AC)
				if (!player)
				{
					return false;
				}
				return IsSafe(player.BodyBottom + Vector3.up * 0.1f);
			}
		}

		public bool CanBeCaught
		{
			get
			{
				// Rebuilt from native code (token 0x0600018E, RVA 0x2729764)
				return phase == Phase.Playing && !InSafeZone;
			}
		}

		public int Checkpoint
		{
			get
			{
				// Rebuilt from native code (token 0x0600018F, RVA 0x2729790)
				return checkpoint;
			}
		}

		public bool Playing
		{
			get
			{
				// Rebuilt from native code (token 0x06000190, RVA 0x2724614)
				return phase == Phase.Playing;
			}
		}

		public bool Dying
		{
			get
			{
				// Rebuilt from native code (token 0x06000191, RVA 0x2729798)
				return phase == Phase.Dying && caughtBy;
			}
		}

		public float RunTime
		{
			get
			{
				// Rebuilt from native code (token 0x06000192, RVA 0x2729810)
				return runStart >= 0f ? Time.time - runStart : 0f;
			}
		}

		public int Bananas
		{
			get
			{
				// Rebuilt from native code (token 0x06000193, RVA 0x2729840)
				return Wallet.Bananas;
			}
		}

		public bool IsSafe(Vector3 p)
		{
			// Rebuilt from native code (token 0x0600018D, RVA 0x27296B4)
			return safeZone.Contains(p) || RippleShop.Contains(p);
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000194, RVA 0x27298C8)
			I = this;
			Time.fixedDeltaTime = 0.013888889f;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000195, RVA 0x2729970)
			NetBridge.Silence(false);
		}

		private IEnumerator Start()
		{
			// Rebuilt from native code (token 0x06000196, RVA 0x27299C4; state machine 0x060001DB, RVA 0x277AE10)
			if (messageText)
			{
				messageText.gameObject.SetActive(false);
				OutlineMessage();
			}
			if (goal)
			{
				rip = goal.GetComponent<BananaRip>();
				if (!rip)
				{
					rip = goal.gameObject.AddComponent<BananaRip>();
				}
				rip.reach = goalTouchDistance;
			}
			BuildMirror();
			player.fader.SetInstant(1f, Color.black);
			yield return new WaitForSeconds(0.3f);
			Respawn();
			yield return player.fader.FadeTo(0f, 0.8f);
			phase = Phase.Playing;
		}

		private void BuildMirror()
		{
			// Rebuilt from native code (token 0x06000197, RVA 0x2729A38)
			Mirror mirror = Object.FindAnyObjectByType<Mirror>(FindObjectsInactive.Include);
			if (!mirror)
			{
				mirror = PlaceMirror(this);
			}
			if (mirror && !Object.FindAnyObjectByType<PlayerBoard>(FindObjectsInactive.Include))
			{
				PlayerBoard.Place(this, mirror);
			}
		}

		public static Mirror PlaceMirror(GameManager game)
		{
			// Rebuilt from native code (token 0x06000198, RVA 0x2729B80)
			if (!game || !game.spawnPoint || !GameObject.Find("Spawn Room"))
			{
				return null;
			}
			Vector3 center = new Vector3(game.safeZone.center.x - game.safeZone.extents.x + 0.04f, game.spawnPoint.position.y + 0.08f + 1.05f, game.safeZone.center.z);
			return Mirror.Create(center, Quaternion.Euler(0f, 90f, 0f), new Vector2(3f, 2.1f));
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000199, RVA 0x2729D60)
			if (phase == Phase.Playing)
			{
				if (InSafeZone)
				{
					if (runStart >= 0f || checkpoint >= 0)
					{
						ResetRun();
					}
				}
				else if (runStart < 0f)
				{
					runStart = Time.time;
					Wallet.RunStarted();
				}
				UpdateCheckpoint();
				if (player.head.position.y < -5f)
				{
					StartCoroutine(FellOut());
				}
				else if (rip && rip.Ripped)
				{
					// TODO(recon): Escape ignores its hand argument, so the native call drops it; RippedBy is the evident source value (token 0x06000199)
					StartCoroutine(Escape(rip.RippedBy));
				}
			}
			KeepMicHonest();
			UpdateWrist();
			UpdateMessage();
		}

		public void PlayerCaught(MonsterAI monster)
		{
			// Rebuilt from native code (token 0x0600019A, RVA 0x272A828)
			if (phase != Phase.Playing || InSafeZone)
			{
				monster.ReleaseGrab();
				return;
			}
			StartCoroutine(Death(monster));
		}

		[ContextMenu("Test: Get Caught (random kill)")]
		private void TestGetCaught()
		{
			// Rebuilt from native code (token 0x0600019B, RVA 0x272A914)
			TestKill(Kill.Random());
		}

		[ContextMenu("Test: Kill - Bite and Throw")]
		private void TestKillShake()
		{
			// Rebuilt from native code (token 0x0600019C, RVA 0x272AD68)
			TestKill(new Kill(KillMove.Shake, 2));
		}

		[ContextMenu("Test: Kill - Pin and Chomp")]
		private void TestKillPin()
		{
			// Rebuilt from native code (token 0x0600019D, RVA 0x272ADA4)
			TestKill(new Kill(KillMove.Pin, Random.Range(2, 5)));
		}

		[ContextMenu("Test: Kill - Consume")]
		private void TestKillConsume()
		{
			// Rebuilt from native code (token 0x0600019E, RVA 0x272ADF0)
			TestKill(new Kill(KillMove.Consume, 2));
		}

		public MonsterAI TestKill(Kill kill)
		{
			// Rebuilt from native code (token 0x0600019F, RVA 0x272A974)
			if (phase != Phase.Playing || !player)
			{
				return null;
			}
			MonsterAI nearest = null;
			foreach (MonsterAI monster in Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
			{
				if (!nearest || (monster.transform.position - player.head.position).sqrMagnitude < (nearest.transform.position - player.head.position).sqrMagnitude)
				{
					nearest = monster;
				}
			}
			if (!nearest)
			{
				return null;
			}
			Vector3 feet = player.BodyBottom;
			Vector3 forward = player.head.forward;
			Vector3 flat = new Vector3(forward.x, 0f, forward.z);
			if (flat.sqrMagnitude < 0.0001f)
			{
				flat = Vector3.forward;
			}
			nearest.TestGrab(feet, feet + flat.normalized * (nearest.headReach > 0f ? nearest.headReach : 1.5f) * 0.95f, kill);
			StartCoroutine(Death(nearest));
			return nearest;
		}

		private IEnumerator Death(MonsterAI monster)
		{
			// Rebuilt from native code (token 0x060001A0, RVA 0x272A88C; state machine 0x060001BD, RVA 0x272C31C)
			phase = Phase.Dying;
			caughtBy = monster;
			DropAll();
			player.Freeze(true);
			XRHands.HapticBoth(1f, 0.5f);
			if (!DeathSequence.I)
			{
				yield return PlaceholderDeath(monster);
				yield break;
			}
			float caughtAt = Time.time;
			AudioClip scare = Sfx.JumpScare();
			PlayLocal(scare, 1f, 1f);
			monster.Hush(scare ? scare.length + 0.3f : 2f);
			Kill kill = monster.CurrentKill;
			GorillaRagdoll body = DeathSequence.I.SpawnLocal(monster, player.BodyBottom, kill);
			SetBodyVisible(false);
			Vector3 face = monster.FacePosition;
			Vector3 head = player.head.position;
			player.TurnToward(MathF.Atan2(face.x - head.x, face.z - head.z) * Mathf.Rad2Deg, 1f);
			Vector3 eye = player.head.position;
			if (body)
			{
				body.ShowHead(false);
				body.ShowArms(false);
			}
			bool silenced = false;
			bool darkened = false;
			float holdUntil = Mathf.Min(kill.move == KillMove.Shake ? 0.6f : kill.Release + 0.05f, kill.End);
			while (body && body.Held && Time.time < caughtAt + holdUntil)
			{
				Vector3 target = body.Head.position;
				WaterVolume water = WaterVolume.At(target);
				if (water && kill.move == KillMove.Pin)
				{
					target.y = Mathf.Max(target.y, water.SurfaceY + 0.08f);
				}
				eye = Vector3.Lerp(eye, target, 1f - MathF.Exp(Time.deltaTime * -30f));
				player.MoveEyeTo(eye);
				if (kill.move == KillMove.Pin)
				{
					Vector3 mouth = monster.FacePosition;
					Vector3 from = player.head.position;
					float share = 1f - MathF.Exp(Time.deltaTime * -6f);
					player.TurnToward(MathF.Atan2(mouth.x - from.x, mouth.z - from.z) * Mathf.Rad2Deg, share);
				}
				if (!silenced && Time.time >= caughtAt + kill.Dead)
				{
					silenced = true;
					NetBridge.Silence(true);
				}
				if (kill.move == KillMove.Consume && !darkened && Time.time >= caughtAt + Kill.ConsumeSmash + 0.05f)
				{
					darkened = true;
					player.fader.FadeTo(1f, 0.3f, new Color(0.22f, 0f, 0.02f, 1f));
				}
				yield return null;
			}
			while (Time.time < caughtAt + kill.Dead)
			{
				yield return null;
			}
			NetBridge.Silence(true);
			if (body && kill.move != KillMove.Consume)
			{
				body.ShowArms(true);
			}
			if (kill.move == KillMove.Pin)
			{
				while (Time.time < caughtAt + kill.End + 1f)
				{
					if (body)
					{
						Vector3 target = body.Head.position;
						WaterVolume water = WaterVolume.At(target);
						if (water)
						{
							target.y = Mathf.Max(target.y, water.SurfaceY + 0.06f);
						}
						eye = Vector3.Lerp(eye, target, 1f - MathF.Exp(Time.deltaTime * -30f));
						player.MoveEyeTo(eye);
					}
					if (Time.time > caughtAt + kill.End && monster && monster.Current == MonsterAI.State.Grab)
					{
						monster.ReleaseGrab();
					}
					yield return null;
				}
			}
			else if (kill.move == KillMove.Consume)
			{
				if (!darkened)
				{
					player.fader.FadeTo(1f, 0.3f, new Color(0.22f, 0f, 0.02f, 1f));
				}
				while (Time.time < caughtAt + kill.End + 0.2f)
				{
					yield return null;
				}
			}
			else if (body)
			{
				Vector3 flung = body.FlungDirection;
				Vector3 away = flung.sqrMagnitude > 0.01f ? flung : monster.transform.forward;
				float yaw = MathF.Atan2(away.x, away.z) * Mathf.Rad2Deg;
				float swung = 0f;
				float swing = Mathf.DeltaAngle(player.head.eulerAngles.y, yaw);
				float thrownAt = Time.time;
				while (Time.time < caughtAt + 1.6f + 1.3f)
				{
					if (body)
					{
						Vector3 target = body.Head.position;
						WaterVolume water = WaterVolume.At(target);
						if (water)
						{
							target.y = Mathf.Max(target.y, water.SurfaceY + 0.06f);
						}
						eye = Vector3.Lerp(eye, target, 1f - MathF.Exp(Time.deltaTime * -30f));
						player.MoveEyeTo(eye);
					}
					float t = Mathf.Clamp01((Time.time - thrownAt) * 2f);
					float turned = swing * t * t * (3f - 2f * t);
					player.TurnBy(turned - swung);
					swung = turned;
					if (Time.time > caughtAt + kill.End && monster && monster.Current == MonsterAI.State.Grab)
					{
						monster.ReleaseGrab();
					}
					yield return null;
				}
			}
			else
			{
				while (Time.time < caughtAt + 1.6f + 1.3f)
				{
					yield return null;
				}
			}
			if (monster && monster.Current == MonsterAI.State.Grab)
			{
				monster.ReleaseGrab();
			}
			yield return player.fader.FadeTo(1f, 0.6f, Color.black);
			if (body)
			{
				body.ShowHead(true);
				body.ShowArms(true);
			}
			Blood.ClearLens();
			Respawn(true);
			SetBodyVisible(true);
			player.Freeze(false);
			caughtBy = null;
			yield return player.fader.FadeTo(0f, 0.8f);
			phase = Phase.Playing;
		}

		private IEnumerator PlaceholderDeath(MonsterAI monster)
		{
			// Rebuilt from native code (token 0x060001A1, RVA 0x272AE54; state machine 0x060001D5, RVA 0x277AA78)
			yield return player.fader.FadeTo(1f, 0.25f, Color.black);
			for (int i = 0; i < 3; i++)
			{
				yield return new WaitForSeconds(i == 0 ? 0.35f : 0.55f);
				XRHands.HapticBoth(1f, 0.18f);
				PlayLocal(Sfx.DeathSlam(), 1f, 1f - i * 0.08f);
			}
			yield return new WaitForSeconds(0.8f);
			Respawn(true);
			player.Freeze(false);
			monster.ReleaseGrab();
			caughtBy = null;
			yield return player.fader.FadeTo(0f, 0.8f);
			phase = Phase.Playing;
		}

		private void SetBodyVisible(bool visible)
		{
			// Rebuilt from native code (token 0x060001A2, RVA 0x272AEE4)
			foreach (Transform hand in new Transform[2] { player.leftHand, player.rightHand })
			{
				if (!hand)
				{
					continue;
				}
				foreach (Renderer renderer in hand.GetComponentsInChildren<Renderer>(true))
				{
					renderer.enabled = visible;
				}
			}
			if (ApeBody.Local)
			{
				ApeBody.Local.SetVisible(visible);
			}
			if (wristText)
			{
				wristText.gameObject.SetActive(visible);
			}
		}

		private IEnumerator Escape(Transform hand)
		{
			// Rebuilt from native code (token 0x060001A3, RVA 0x272A194; state machine 0x060001C3, RVA 0x2779900)
			phase = Phase.Escaping;
			float time = runStart >= 0f ? Time.time - runStart : 0f;
			if (best < 0f || time < best)
			{
				best = time;
			}
			bool counts = time > 0f && !skipped;
			escapeNote = "";
			escapePaid = Wallet.OnServer ? -1 : 1000;
			escapeShownUntil = -1f;
			Wallet.Escape(time, counts, (int paid, string note) =>
			{
				// Rebuilt from native code (token 0x060001BA, RVA 0x272C280)
				escapeNote = note;
				escapePaid = paid;
				if (Time.time < escapeShownUntil)
				{
					ShowMessage(EscapeText(time), escapeShownUntil - Time.time);
				}
			});
			if (counts)
			{
				Leaderboard.Submit(PlayerProfile.Name, time);
				NetBridge.ReportEscape(PlayerProfile.Name, time);
			}
			float burst = BananaBurst.Play(goal, player.head);
			goal.gameObject.SetActive(false);
			PlayLocal(Sfx.Escape(), 0.9f, 1f);
			StartCoroutine(HapticRamp(burst));
			yield return new WaitForSeconds(Mathf.Max(0.6f, burst - 0.4f));
			yield return player.fader.FadeTo(1f, 1f, EscapeWhite);
			goal.gameObject.SetActive(true);
			DropAll();
			player.Freeze(true);
			yield return new WaitForSeconds(0.5f);
			checkpoint = -1;
			Respawn();
			player.Freeze(false);
			ShowMessage(EscapeText(time), 6f);
			escapeShownUntil = Time.time + 6f;
			yield return player.fader.FadeTo(0f, 1f);
			phase = Phase.Playing;
		}

		private string EscapeText(float time)
		{
			// Rebuilt from native code (token 0x060001A4, RVA 0x272B1F4)
			string text = "Escaped in " + Format(time) + "!";
			if (escapePaid > 0)
			{
				return text + string.Format("\n+{0:N0} bananas", escapePaid);
			}
			if (escapePaid == 0 && escapeNote.Length > 0)
			{
				return text + "\n" + escapeNote;
			}
			return text;
		}

		private IEnumerator FellOut()
		{
			// Rebuilt from native code (token 0x060001A5, RVA 0x272A120; state machine 0x060001C9, RVA 0x277A6D4)
			phase = Phase.Dying;
			yield return player.fader.FadeTo(1f, 0.3f, Color.black);
			Respawn(true);
			yield return player.fader.FadeTo(0f, 0.5f);
			phase = Phase.Playing;
		}

		private IEnumerator HapticRamp(float seconds)
		{
			// Rebuilt from native code (token 0x060001A6, RVA 0x272B450; state machine 0x060001CF, RVA 0x277A8C8)
			for (float t = 0f; t < seconds; t += 0.08f)
			{
				XRHands.HapticBoth(Mathf.Lerp(0.1f, 0.8f, t / seconds), 0.1f);
				yield return new WaitForSeconds(0.08f);
			}
		}

		private void Respawn(bool keepRun = false)
		{
			// Rebuilt from native code (token 0x060001A7, RVA 0x272B4C0)
			NetBridge.Silence(false);
			Transform spawn = checkpoint >= 0 && levels[checkpoint].spawn ? levels[checkpoint].spawn : spawnPoint;
			player.TeleportFeetTo(spawn.position, spawn.eulerAngles.y);
			if (!keepRun || checkpoint < 0)
			{
				ResetRun();
			}
		}

		private void KeepMicHonest()
		{
			// Rebuilt from native code (token 0x060001A8, RVA 0x272A208)
			if (!NetBridge.Silenced)
			{
				silencedSince = -1f;
				return;
			}
			if (silencedSince < 0f)
			{
				silencedSince = Time.time;
			}
			if (phase != Phase.Dying || Time.time - silencedSince > 15f)
			{
				NetBridge.Silence(false);
			}
		}

		private void ResetRun()
		{
			// Rebuilt from native code (token 0x060001A9, RVA 0x2729EB0)
			if (runStart >= 0f)
			{
				Wallet.RunEnded();
			}
			skipped = false;
			checkpoint = -1;
			runStart = -1f;
		}

		private void UpdateCheckpoint()
		{
			// Rebuilt from native code (token 0x060001AA, RVA 0x2729F24)
			if (levels == null)
			{
				return;
			}
			Vector3 feet = player.BodyBottom + Vector3.up * 0.1f;
			for (int i = 0; i < levels.Length; i++)
			{
				if (i == checkpoint || !levels[i].startDeck.Contains(feet))
				{
					continue;
				}
				int previous = checkpoint;
				checkpoint = i;
				if (previous < i)
				{
					if (runStart >= 0f)
					{
						Wallet.ReachedLevel(i);
					}
					PlayLocal(Sfx.Checkpoint(), 0.45f, 1.5f);
					XRHands.HapticBoth(0.3f, 0.1f);
				}
				break;
			}
		}

		public void GoToLevel(int level)
		{
			// Rebuilt from native code (token 0x060001AB, RVA 0x272B648)
			if (phase != Phase.Playing || levels == null)
			{
				return;
			}
			checkpoint = Mathf.Clamp(level, -1, levels.Length - 1);
			DropAll();
			Respawn(true);
			if (level >= 0)
			{
				skipped = true;
			}
		}

		public void WatchRespawn()
		{
			// Rebuilt from native code (token 0x060001AC, RVA 0x272B770)
			if (phase != Phase.Playing)
			{
				return;
			}
			DropAll();
			ResetRun();
			StartCoroutine(FellOut());
		}

		public void OtherEscaped(string name, float time)
		{
			// Rebuilt from native code (token 0x060001AD, RVA 0x272B7B0)
			Leaderboard.Submit(name, time);
			PlayFabClient.RefreshBoard(4f);
		}

		private void DropAll()
		{
			// Rebuilt from native code (token 0x060001AE, RVA 0x272B6B4)
			if (grabbers == null)
			{
				return;
			}
			foreach (HandGrabber grabber in grabbers)
			{
				if (grabber)
				{
					grabber.Drop();
				}
			}
		}

		public void PlayLocal(AudioClip clip, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x060001AF, RVA 0x2724F28)
			if (!localAudio || !clip)
			{
				return;
			}
			localAudio.pitch = Sfx.PitchFor(clip, pitch);
			localAudio.PlayOneShot(clip, volume);
		}

		public void ShowMessage(string text, float seconds)
		{
			// Rebuilt from native code (token 0x060001B0, RVA 0x272B848)
			if (!messageText)
			{
				return;
			}
			messageText.text = text;
			foreach (TextMesh outline in messageOutline)
			{
				outline.text = text;
			}
			messageUntil = Time.time + seconds;
			messageText.gameObject.SetActive(true);
			FadeMessage(1f);
			PlaceMessage(1f);
		}

		private void OutlineMessage()
		{
			// Rebuilt from native code (token 0x060001B1, RVA 0x272BD48)
			PixelFonts.Use(messageText, PixelFonts.Sign, 0.045f, 1.4f);
			messageText.anchor = TextAnchor.MiddleCenter;
			messageText.alignment = TextAlignment.Center;
			messageText.color = Color.white;
			messageOutline = new TextMesh[8];
			for (int i = 0; i < 8; i++)
			{
				GameObject outlineObject = new GameObject("Outline " + i);
				outlineObject.transform.SetParent(messageText.transform, false);
				float angle = i * MathF.PI * 0.25f;
				outlineObject.transform.localPosition = new Vector3(MathF.Cos(angle) * 0.0035f, MathF.Sin(angle) * 0.0035f, 0.002f);
				TextMesh outline = outlineObject.AddComponent<TextMesh>();
				outline.font = messageText.font;
				outline.fontSize = messageText.fontSize;
				outline.characterSize = messageText.characterSize;
				outline.lineSpacing = messageText.lineSpacing;
				outline.anchor = messageText.anchor;
				outline.alignment = messageText.alignment;
				outline.color = MessageOutlineColor;
				outlineObject.GetComponent<MeshRenderer>().sharedMaterial = messageText.GetComponent<MeshRenderer>().sharedMaterial;
				messageOutline[i] = outline;
			}
		}

		private void FadeMessage(float alpha)
		{
			// Rebuilt from native code (token 0x060001B2, RVA 0x272B980)
			Color color = messageText.color;
			messageText.color = new Color(color.r, color.g, color.b, alpha);
			foreach (TextMesh outline in messageOutline)
			{
				outline.color = new Color(MessageOutlineColor.r, MessageOutlineColor.g, MessageOutlineColor.b, alpha);
			}
		}

		private void UpdateMessage()
		{
			// Rebuilt from native code (token 0x060001B3, RVA 0x272A70C)
			if (!messageText || !messageText.gameObject.activeSelf)
			{
				return;
			}
			float left = messageUntil - Time.time;
			if (left <= 0f)
			{
				messageText.gameObject.SetActive(false);
				return;
			}
			FadeMessage(Mathf.Clamp01(left * 2f));
			PlaceMessage(Time.deltaTime * 4f);
		}

		private void PlaceMessage(float follow)
		{
			// Rebuilt from native code (token 0x060001B4, RVA 0x272BA74)
			Vector3 ahead = player.head.forward;
			if (ahead.x * ahead.x + ahead.z * ahead.z < 0.001f)
			{
				ahead = player.head.up;
			}
			ahead.y = 0f;
			Vector3 target = player.head.position + ahead.normalized * 1.3f + Vector3.up * 0.1f;
			Transform messageTransform = messageText.transform;
			messageTransform.position = Vector3.Lerp(messageTransform.position, target, Mathf.Clamp01(follow));
			messageTransform.rotation = Quaternion.LookRotation(messageTransform.position - player.head.position, Vector3.up);
		}

		private void UpdateWrist()
		{
			// Rebuilt from native code (token 0x060001B5, RVA 0x272A304)
			if (!wristText)
			{
				return;
			}
			string text = runStart >= 0f ? Format(Time.time - runStart) : "ready";
			if (best >= 0f)
			{
				text = text + "\nbest " + Format(best);
			}
			if (Wallet.Bananas > 0)
			{
				text += string.Format("\nbananas {0}", Wallet.Bananas);
			}
			if (checkpoint >= 0 && levels != null && levels.Length >= 2)
			{
				text = string.Format("L{0} {1}\n", checkpoint + 1, levels[checkpoint].name) + text;
			}
			smoothedFrame += (Time.unscaledDeltaTime - smoothedFrame) * 0.05f;
			if (ShallowsTuning.I && ShallowsTuning.I.showDebug)
			{
				text += string.Format("\n{0:0} fps", 1f / Mathf.Max(0.0001f, smoothedFrame));
			}
			wristText.text = text;
		}

		private static string Format(float seconds)
		{
			// Rebuilt from native code (token 0x060001B6, RVA 0x272B350)
			int minutes = Mathf.FloorToInt(seconds / 60f);
			return string.Format("{0}:{1:00.0}", minutes, seconds - minutes * 60f);
		}
	}
}
