using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class MonsterAI : MonoBehaviour
	{
		public enum State
		{
			Wander = 0,
			Investigate = 1,
			Search = 2,
			Grab = 3,
			Stalk = 4
		}

		private struct Sample
		{
			public float time;

			public float yaw;

			public Vector3 position;
		}

		private class Leg
		{
			public Transform hip;

			public Transform knee;

			public Transform contact;

			public Quaternion hipRest;

			public Quaternion kneeRest;

			public Vector3 home;

			public Vector3 pole;

			public float upper;

			public float lower;

			public Vector3 planted;

			public Vector3 from;

			public Vector3 to;

			public bool stepping;

			public float t;

			public float duration;

			public float dig;

			public int group;
		}

		private struct Spring
		{
			public float x;

			public float v;

			public float Step(float target, float frequency, float damping, float dt)
			{
				// Rebuilt from native code (token 0x0600058D, RVA 0x27DD670)
				int steps = Mathf.Max(Mathf.CeilToInt(dt / 0.004f), 1);
				float h = dt / steps;
				float omega = 2f * Mathf.PI * frequency;
				for (int i = 0; i < steps; i++)
				{
					v += h * (omega * omega * (target - x) - 2f * damping * omega * v);
					x += h * v;
				}
				return x;
			}
		}

		[Header("Parts (set by the scene builder)")]
		[Tooltip("Everything above the hips. Bobs and leans.")]
		public Transform body;

		public Transform head;

		[Tooltip("Pivots at the hips and shoulders.")]
		public Transform leftLeg;

		[Tooltip("Pivots at the hips and shoulders.")]
		public Transform rightLeg;

		[Tooltip("Pivots at the hips and shoulders.")]
		public Transform leftArm;

		[Tooltip("Pivots at the hips and shoulders.")]
		public Transform rightArm;

		public TextMesh label;

		public LineRenderer targetLine;

		public AudioSource voice;

		public WaterVolume water;

		[Tooltip("Which monster this is, so the network can tell them apart.")]
		public int index;

		[Header("Four-legged body (Test 3). Leave empty for the two-legged greybox.")]
		[Tooltip("Pivots at the front of the body. Pitches down to slam and up to lift.")]
		public Transform neck;

		[Tooltip("Hip pivots, in the order front left, front right, back left, back right.")]
		public Transform[] hips;

		[Tooltip("Knee pivots, same order as the hips.")]
		public Transform[] knees;

		[Tooltip("Where each foot touches the floor, same order as the hips.")]
		public Transform[] feet;

		[Tooltip("The flaps of the split jaw.")]
		public Transform[] jawFlaps;

		[Tooltip("How far each flap swings open (local Euler angles), same order as the flaps.")]
		public Vector3[] jawOpen;

		[Tooltip("How far its neck is already bent down in the model's own pose (degrees), so standing still looks like the model.")]
		public float neckRestPitch;

		[Tooltip("Inside the mouth: where it holds you.")]
		public Transform grip;

		[Tooltip("How far in front of its hips its mouth is (m). It stops this far short of a splash so its head lands right on it.")]
		public float headReach;

		[Tooltip("Its body runs from bodyBack to bodyFront along its length (m). Touching anywhere along it counts as leg contact.")]
		public float bodyFront;

		[Tooltip("Its body runs from bodyBack to bodyFront along its length (m). Touching anywhere along it counts as leg contact.")]
		public float bodyBack;

		[Tooltip("Keeps its hips this far from the pool edge (m).")]
		public float edgeInset = 0.8f;

		[Tooltip("Finger and toe bones, curled when a foot digs in (set by the scene builder).")]
		public Transform[] claws;

		[Tooltip("Which leg each claw belongs to (0 to 3, same order as the hips).")]
		public int[] clawLegs;

		[Header("Four-legged animation")]
		[Tooltip("Seconds a foot takes to step when it's walking, and when it's charging flat out.")]
		public float stepTimeSlow = 0.24f;

		[Header("Four-legged animation")]
		[Tooltip("Seconds a foot takes to step when it's walking, and when it's charging flat out.")]
		public float stepTimeFast = 0.085f;

		[Tooltip("How high a foot lifts, as a share of its leg's length.")]
		public float stepHeight = 0.32f;

		[Tooltip("0 = calm, 1 = very twitchy: how often and how far its head snaps about.")]
		[Range(0f, 1f)]
		public float twitchiness = 0.75f;

		[Tooltip("Degrees each finger and toe bone curls when a foot digs in.")]
		public float clawCurl = 14f;

		[Header("Movement")]
		public float turnSpeed = 320f;

		[Tooltip("While wandering it eases into and out of turns: its top turn speed (degrees a second), and how quickly it gets up to it and slows down again.")]
		public float wanderTurnSpeed = 55f;

		[Tooltip("While wandering it eases into and out of turns: its top turn speed (degrees a second), and how quickly it gets up to it and slows down again.")]
		public float wanderTurnAcceleration = 80f;

		[Tooltip("How wide its body is from its middle line (m), for keeping monsters out of each other. 0 = work it out from where its feet are.")]
		public float bodyRadius;

		public float speedUp = 34f;

		[Tooltip("How hard it slows down. It starts braking early enough to stop right on the splash.")]
		public float braking = 36f;

		[Tooltip("Meters per footstep.")]
		public float stride = 2.6f;

		public float arriveDistance = 1.2f;

		[Tooltip("How close it gets to a splash before it starts searching.")]
		public float splashArriveDistance = 0.4f;

		[Tooltip("Gives up on reaching a splash after this long (s) and searches where it is.")]
		public float investigateTimeout = 15f;

		[Tooltip("When someone's landings come one after another (big jumps), it runs past the newest one by this share of the gap between the last two, to be waiting where they land next.")]
		public float leadAmount = 0.8f;

		[Tooltip("A single jump's landing: it runs on past the splash, the way the jumper was going, as far as they'd carry on in this long (s).")]
		public float headingLead = 0.8f;

		[Tooltip("The furthest past a splash it leads (m).")]
		public float maxLead = 6f;

		[Tooltip("After a player splashes, it homes in on that player for this long (s) while they're still in the water or in the air from it. Every splash starts it again, so splashing along never shakes it off; getting onto something dry or keeping quiet does.")]
		public float trackTime = 2f;

		[Tooltip("Hearing someone talk out of nowhere, it freezes with its head on them this long (s) before it starts creeping over.")]
		public float stalkPause = 0.8f;

		[Tooltip("At the spot it heard the voice, it stands and listens this long (s) before wandering off again.")]
		public float stalkListenTime = 3.5f;

		[Tooltip("Gives up on a voice it hasn't heard again for this long (s), even if it never got there.")]
		public float stalkTimeout = 15f;

		public float avoidRadius = 0.7f;

		[Tooltip("How far it keeps its middle from platforms, pillars and walls (m), so its legs stay off them. 0 = work it out from where its feet are.")]
		public float clearance;

		[Header("Grabbing")]
		[Tooltip("It grabs anyone wet who touches its legs, even while wandering. Its arm reach is on ShallowsTuning.")]
		public float legContactRange = 1.3f;

		public static bool NoCatching;

		public static readonly List<MonsterAI> All = new List<MonsterAI>();

		private Vector3 target;

		private float stateTime;

		private float idleUntil;

		private float speed;

		private float walkPhase;

		private float lastRoar = -100f;

		private Vector3 lastLanding;

		private float lastLandingTime = -100f;

		private bool tracking;

		private Vector3 tracked;

		private float trackUntil;

		private float retargetAt;

		private float listenAt;

		private float lastVoiceAt = -100f;

		private float stalkGoAt;

		private float stalkArrivedAt = -1f;

		private readonly List<float> sentenceHeardAt = new List<float>();

		private readonly List<bool> sentenceGotThrough = new List<bool>();

		private Vector3 lookedAt;

		private float flickAt = -10f;

		private float glanceAt;

		private float glanceYaw;

		private float glancePitch;

		private int lastStep;

		private Vector3 bodyBase;

		private Quaternion bodyBaseRotation;

		private Quaternion neckBase;

		private Quaternion[] hipBase;

		private Quaternion[] kneeBase;

		private Quaternion[] jawBase;

		private float neckYaw;

		private Leg[] legs;

		private Quaternion headBase;

		private Quaternion[] clawBase;

		private float[] clawSign;

		private Spring bodyY;

		private Spring bodyPitch;

		private Spring bodyRoll;

		private Spring neckPitchS;

		private Spring neckYawS;

		private Spring headRoll;

		private Spring jawS;

		private Vector3 lastPosition;

		private Vector3 velocity;

		private Vector3 lastVelocity;

		private float lastYaw;

		private float yawRate;

		private float twitchAt;

		private float twitchYaw;

		private float twitchPitch;

		private float tiltAt;

		private float tilt;

		private float chatterUntil;

		private float chatterAt;

		private float startle = -10f;

		private float settleAt;

		private float lastStepSound;

		private float lastStepSplash;

		private float gallop;

		private float twitchSharp = 1f;

		private State animState;

		private readonly Vector3[] want = new Vector3[4];

		private readonly float[] error = new float[4];

		private Camera viewer;

		private const int Obstacles = -5;

		private WaterNav nav;

		private readonly List<Vector3> path = new List<Vector3>();

		private Vector3 pathGoal = new Vector3(float.NaN, 0f, 0f);

		private float replanAt;

		private float turnVelocity;

		private float radius;

		private float crowdTime;

		private float stuckFor;

		private AudioSource breath;

		private float breathAt;

		private float mouthBusyUntil;

		private float hushUntil;

		private float voiceVolume = 1f;

		private int grabCalls;

		private static readonly float[] GrabGrowls = new float[0];

		private int turnedFrame;

		private Vector3 pinLocal;

		private bool pinReady;

		private float pinPitch = 60f;

		private float holdKillUntil;

		private readonly Vector3[] handTarget = new Vector3[2];

		private readonly float[] handWeight = new float[2];

		private const float PinCrouch = 0.18f;

		private const float PinBodyPitch = 10f;

		private const float GrabGiveUp = 4f;

		private bool hasRemote;

		private bool wasAuthority = true;

		private bool remoteGrab;

		private Vector3 grabFacing;

		private float remoteSpeed;

		private State remoteState;

		private readonly Sample[] samples = new Sample[8];

		private int sampleCount;

		private float remoteJumpAt = -100f;

		private const float RemoteJumpGrace = 1.5f;

		private const float PlaybackDelay = 0.22f;

		private const float MaxExtrapolate = 0.25f;

		public State Current { get; private set; }

		public Vector3 Target
		{
			get
			{
				// Rebuilt from native code (token 0x0600053D, RVA 0x27D0194)
				return target;
			}
		}

		public float Speed
		{
			get
			{
				// Rebuilt from native code (token 0x0600053E, RVA 0x27D01A4)
				return speed;
			}
		}

		public float GrabTime
		{
			get
			{
				// Rebuilt from native code (token 0x0600053F, RVA 0x27D01AC)
				return Current == State.Grab ? stateTime : -1f;
			}
		}

		public Vector3 GripPosition
		{
			get
			{
				// Rebuilt from native code (token 0x06000540, RVA 0x27D01C4)
				if (grip)
				{
					return grip.position;
				}
				if (rightArm)
				{
					return rightArm.TransformPoint(0f, -2.1f, 0.1f);
				}
				return transform.position + Vector3.up * 2f;
			}
		}

		public Quaternion GripRotation
		{
			get
			{
				// Rebuilt from native code (token 0x06000541, RVA 0x27D0328)
				return grip ? grip.rotation : transform.rotation;
			}
		}

		public Kill CurrentKill { get; private set; }

		public Vector3 PinPoint
		{
			get
			{
				// Rebuilt from native code (token 0x06000544, RVA 0x27D03BC)
				if (!pinReady)
				{
					CalibratePin();
				}
				return transform.TransformPoint(pinLocal);
			}
		}

		public Vector3 ThroatPosition
		{
			get
			{
				// Rebuilt from native code (token 0x06000545, RVA 0x27D090C)
				if (neck && head)
				{
					return Vector3.Lerp(head.position, neck.position, 0.5f);
				}
				return GripPosition - transform.forward * 0.4f;
			}
		}

		public Vector3 FacePosition
		{
			get
			{
				// Rebuilt from native code (token 0x06000546, RVA 0x27D0A4C)
				if (head)
				{
					return head.position;
				}
				return transform.position + Vector3.up * 3f;
			}
		}

		private bool FourLegged
		{
			get
			{
				// Rebuilt from native code (token 0x06000547, RVA 0x27D0B5C)
				return hips != null && hips.Length == 4 && neck;
			}
		}

		private Vector3 Mouth
		{
			get
			{
				// Rebuilt from native code (token 0x06000577, RVA 0x27D2CF8)
				if (headReach > 0f)
				{
					return transform.position + transform.forward * headReach;
				}
				return transform.position;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000548, RVA 0x27D0BDC)
			All.Clear();
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000549, RVA 0x27D0C74)
			SplashSystem.Heard += OnHeard;
			NetBridge.RoomJoined += OnRoomJoined;
			All.Add(this);
			All.Sort((a, b) => a.index.CompareTo(b.index));
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600054A, RVA 0x27D0F14)
			SplashSystem.Heard -= OnHeard;
			NetBridge.RoomJoined -= OnRoomJoined;
			All.Remove(this);
		}

		private void OnRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x0600054B, RVA 0x27D108C)
			hasRemote = false;
			sampleCount = 0;
			remoteJumpAt = -100f;
			if (fresh && Current != State.Grab)
			{
				tracking = false;
				Enter(State.Wander);
				PickWanderTarget();
			}
		}

		public static MonsterAI ByIndex(int index)
		{
			// Rebuilt from native code (token 0x0600054C, RVA 0x27D1358)
			foreach (MonsterAI monster in All)
			{
				if (monster.index == index)
				{
					return monster;
				}
			}
			return null;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x0600054D, RVA 0x27D14CC)
			bodyBase = body.localPosition;
			bodyBaseRotation = body.localRotation;
			if (FourLegged)
			{
				neckBase = neck.localRotation;
				hipBase = Bases(hips);
				kneeBase = Bases(knees);
				jawBase = Bases(jawFlaps);
				SetUpLegs();
			}
			viewer = Camera.main;
			radius = bodyRadius > 0f ? bodyRadius : Mathf.Max(0.3f, FootClearance() - 0.06f);
			SetUpBreath();
			if (water)
			{
				nav = WaterNav.For(water, clearance > 0f ? clearance : FootClearance(), edgeInset);
				Vector3 open = nav.NearestOpen(transform.position);
				transform.position = new Vector3(open.x, water.FloorY, open.z);
			}
			PickWanderTarget();
		}

		private float FootClearance()
		{
			// Rebuilt from native code (token 0x0600054E, RVA 0x27D24A8)
			if (feet == null || feet.Length == 0)
			{
				return 0.56f;
			}
			float widest = 0.5f;
			foreach (Transform foot in feet)
			{
				if (foot)
				{
					widest = Mathf.Max(widest, Mathf.Abs(transform.InverseTransformPoint(foot.position).x) * transform.lossyScale.x);
				}
			}
			return widest + 0.06f;
		}

		private static Quaternion[] Bases(Transform[] parts)
		{
			// Rebuilt from native code (token 0x0600054F, RVA 0x27D1758)
			Quaternion[] bases = new Quaternion[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				bases[i] = parts[i] ? parts[i].localRotation : Quaternion.identity;
			}
			return bases;
		}

		private void OnHeard(SplashEvent e)
		{
			// Rebuilt from native code (token 0x06000550, RVA 0x27D27A8)
			if (Current == State.Grab || !NetBridge.RunsMonsters)
			{
				return;
			}
			if (water && !water.ContainsXZ(e.position))
			{
				return;
			}
			if (FlatDistance(e.position, Mouth) > e.hearingRadius)
			{
				return;
			}
			if (SplashSystem.I)
			{
				SplashSystem.I.ReportHeard();
			}
			State previous = Current;
			Vector3 goal = e.position;
			if (e.kind == SplashKind.Body)
			{
				float sinceLanding = Time.time - lastLandingTime;
				float headingSpeed = e.heading.magnitude;
				if (headingSpeed > 1f)
				{
					goal += e.heading / headingSpeed * Mathf.Min(headingSpeed * headingLead, maxLead);
				}
				else
				{
					float gap = FlatDistance(goal, lastLanding);
					if (gap < 12f && gap > 1.5f && sinceLanding < 6f)
					{
						goal += new Vector3(goal.x - lastLanding.x, 0f, goal.z - lastLanding.z) / gap * Mathf.Min(gap * leadAmount, maxLead);
					}
				}
				lastLanding = e.position;
				lastLandingTime = Time.time;
			}
			target = water ? water.ClampXZ(goal, 0.8f) : goal;
			tracking = false;
			if (e.kind <= SplashKind.Body && NearestPlayer(e.position, 2.5f, out Vector3 feet))
			{
				tracking = true;
				tracked = feet;
				trackUntil = Time.time + trackTime;
				retargetAt = Time.time + 0.15f;
			}
			Enter(State.Investigate);
			if (previous != State.Investigate && Time.time - lastRoar > 4f)
			{
				Roar();
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000551, RVA 0x27D30E4)
			ShallowsTuning tuning = ShallowsTuning.I;
			if (!tuning)
			{
				return;
			}
			float dt = Time.deltaTime;
			stateTime += dt;
			bool authority = NetBridge.RunsMonsters;
			if (!authority)
			{
				wasAuthority = authority;
				float followed = hasRemote ? FollowRemote(dt) : 0f;
				CheckGrab();
				Animate(followed, tuning);
				UpdateDebug(tuning);
				return;
			}
			if (!wasAuthority)
			{
				TakeOverBrain();
			}
			wasAuthority = authority;
			ListenForVoices(tuning);
			float moved = 0f;
			if (Current == State.Grab || !Unstick(dt, out moved))
			{
				switch (Current)
				{
				case State.Wander:
					if (Time.time < idleUntil)
					{
						moved = Brake(dt);
					}
					else if (MoveTowards(target, tuning.roamSpeed * Lurch(), arriveDistance, dt, out moved))
					{
						idleUntil = Time.time + UnityEngine.Random.Range(1f, 3f);
						PickWanderTarget();
					}
					else if (crowdTime > 1.2f)
					{
						crowdTime = 0f;
						PickWanderTarget();
					}
					break;
				case State.Investigate:
				{
					Track();
					bool crowded = crowdTime > 0.3f && FlatDistance(transform.position, target) - headReach < 2.5f;
					bool arrived = MoveTowards(target, tuning.chargeSpeed, splashArriveDistance, dt, out moved) && !tracking;
					if (arrived || crowded || stateTime > investigateTimeout)
					{
						speed = 0f;
						Enter(State.Search);
					}
					break;
				}
				case State.Search:
					moved = Brake(dt);
					if (stateTime > tuning.searchTime)
					{
						Enter(State.Wander);
						PickWanderTarget();
					}
					break;
				case State.Grab:
					moved = Brake(dt);
					FacePlayer(dt);
					if (remoteGrab && stateTime > CurrentKill.End)
					{
						ReleaseGrab();
					}
					else if (stateTime > CurrentKill.End + GrabGiveUp)
					{
						LetGo();
					}
					break;
				case State.Stalk:
					if (Time.time < stalkGoAt || stalkArrivedAt >= 0f)
					{
						moved = Brake(dt);
					}
					else if (MoveTowards(target, tuning.stalkSpeed * (Mathf.PerlinNoise(Time.time * 0.5f, index * 3.1f) * 0.4f + 0.8f), arriveDistance, dt, out moved) || crowdTime > 1.2f)
					{
						stalkArrivedAt = Time.time;
					}
					if ((stalkArrivedAt >= 0f && Time.time - stalkArrivedAt > stalkListenTime) || Time.time - lastVoiceAt > stalkTimeout)
					{
						Enter(State.Wander);
						idleUntil = Time.time + UnityEngine.Random.Range(0.5f, 1.5f);
						PickWanderTarget();
					}
					break;
				}
			}
			moved += KeepApart(dt);
			CheckGrab();
			Animate(moved, tuning);
			UpdateDebug(tuning);
		}

		private bool Unstick(float dt, out float moved)
		{
			// Rebuilt from native code (token 0x06000552, RVA 0x27D4C5C)
			moved = 0f;
			if (nav == null)
			{
				return false;
			}
			Vector3 position = transform.position;
			if (nav.InMainRegion(position))
			{
				stuckFor = 0f;
				return false;
			}
			stuckFor += dt;
			if (stuckFor < 0.1f)
			{
				return false;
			}
			Vector3 open = nav.NearestOpen(position);
			float distance = FlatDistance(open, position);
			if (distance < 0.01f)
			{
				return false;
			}
			Vector3 toward = new Vector3(open.x - position.x, 0f, open.z - position.z) / distance;
			Vector3 next = position + toward * Mathf.Min(distance + 0.05f, Mathf.Max(speed, 4f) * dt);
			if (water)
			{
				next = water.ClampXZ(next, edgeInset);
			}
			next.y = water ? Mathf.MoveTowards(position.y, water.FloorY, 2f * dt) : position.y;
			transform.position = next;
			replanAt = 0f;
			moved = FlatDistance(position, next);
			return true;
		}

		private void Track()
		{
			// Rebuilt from native code (token 0x06000553, RVA 0x27D5B34)
			if (!tracking)
			{
				return;
			}
			if (Time.time <= trackUntil && NearestPlayer(tracked, 3f, out Vector3 feet) && Exposed(feet))
			{
				Vector3 previous = tracked;
				tracked = feet;
				if (Time.time < retargetAt)
				{
					return;
				}
				retargetAt = Time.time + 0.15f;
				Vector3 velocity = new Vector3(feet.x - previous.x, 0f, feet.z - previous.z) / Mathf.Max(Time.deltaTime, 0.005f);
				Vector3 goal = feet + Vector3.ClampMagnitude(velocity * 0.25f, 2f);
				target = water ? water.ClampXZ(goal, 0.8f) : goal;
				return;
			}
			tracking = false;
		}

		private void ListenForVoices(ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x06000554, RVA 0x27D4AC4)
			if (Current != State.Wander && Current != State.Stalk)
			{
				return;
			}
			if (Time.time < listenAt)
			{
				return;
			}
			listenAt = Time.time + 0.2f;
			if (!LoudestVoice(tuning, out Vector3 speaker))
			{
				return;
			}
			float sinceVoice = Time.time - lastVoiceAt;
			lastVoiceAt = Time.time;
			target = water ? water.ClampXZ(speaker, 0.8f) : speaker;
			stalkArrivedAt = -1f;
			if (Current != State.Stalk)
			{
				Enter(State.Stalk);
				stalkGoAt = Time.time + stalkPause;
			}
			else if (sinceVoice > 1.2f)
			{
				stalkGoAt = Mathf.Max(stalkGoAt, Time.time + stalkPause * 0.5f);
			}
		}

		private bool LoudestVoice(ShallowsTuning tuning, out Vector3 speaker)
		{
			// Rebuilt from native code (token 0x06000555, RVA 0x27D6A84)
			speaker = default;
			float best = 0f;
			PlayerRig rig = PlayerRig.I;
			if (rig && !rig.Frozen && NetworkManager.I && !NetBridge.Silenced)
			{
				HearVoice(0, rig.BodyBottom, VoiceLevel.Opening(NetworkManager.I.MicLevel), tuning, ref best, ref speaker);
			}
			List<Vector3> remoteFeet = NetBridge.RemoteFeet;
			List<float> remoteVoices = NetBridge.RemoteVoices;
			for (int i = 0; i < remoteFeet.Count && i < remoteVoices.Count; i++)
			{
				HearVoice(i + 1, remoteFeet[i], remoteVoices[i], tuning, ref best, ref speaker);
			}
			return best > 0f;
		}

		private void HearVoice(int who, Vector3 feet, float level, ShallowsTuning tuning, ref float best, ref Vector3 speaker)
		{
			// Rebuilt from native code (token 0x06000556, RVA 0x27D6DE4)
			if (level < tuning.voiceHearLevel)
			{
				return;
			}
			if (water && !water.ContainsXZ(feet))
			{
				return;
			}
			float range = Mathf.Lerp(tuning.voiceHearRangeMin, tuning.voiceHearRange, Mathf.InverseLerp(tuning.voiceHearLevel, 1f, level));
			float distance = FlatDistance(feet, Mouth);
			if (distance > range || !SentenceGetsThrough(who, tuning))
			{
				return;
			}
			float score = 1f - distance / Mathf.Max(range, 0.01f);
			if (score <= best)
			{
				return;
			}
			best = score;
			speaker = feet;
		}

		private bool SentenceGetsThrough(int who, ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x06000557, RVA 0x27D6FC4)
			while (sentenceHeardAt.Count <= who)
			{
				sentenceHeardAt.Add(-100f);
				sentenceGotThrough.Add(false);
			}
			if (Time.time - sentenceHeardAt[who] > 1.5f)
			{
				sentenceGotThrough[who] = UnityEngine.Random.value < tuning.voiceHearChance;
			}
			sentenceHeardAt[who] = Time.time;
			return sentenceGotThrough[who];
		}

		private static bool NearestPlayer(Vector3 p, float range, out Vector3 feet)
		{
			// Rebuilt from native code (token 0x06000558, RVA 0x27D2D94)
			feet = default;
			float best = range * range;
			bool found = false;
			PlayerRig rig = PlayerRig.I;
			if (rig && !rig.Frozen)
			{
				Consider(rig.BodyBottom, p, ref best, ref feet, ref found);
			}
			for (int i = 0; i < NetBridge.RemoteFeet.Count; i++)
			{
				Consider(NetBridge.RemoteFeet[i], p, ref best, ref feet, ref found);
			}
			return found;
		}

		private static void Consider(Vector3 candidate, Vector3 p, ref float best, ref Vector3 feet, ref bool found)
		{
			// Rebuilt from native code (token 0x06000559, RVA 0x27D71F4)
			float dx = candidate.x - p.x;
			float dz = candidate.z - p.z;
			float distance = dx * dx + dz * dz;
			if (distance >= best)
			{
				return;
			}
			best = distance;
			feet = candidate;
			found = true;
		}

		private bool Exposed(Vector3 feet)
		{
			// Rebuilt from native code (token 0x0600055A, RVA 0x27D67CC)
			if (water && !water.ContainsXZ(feet))
			{
				return false;
			}
			PlayerRig rig = PlayerRig.I;
			if (rig && (rig.BodyBottom - feet).sqrMagnitude < 0.0001f)
			{
				return rig.Wet;
			}
			float surface = water ? water.SurfaceY : feet.y;
			if (!Physics.Raycast(feet + Vector3.up * 0.05f, Vector3.down, out RaycastHit hit, 0.4f, -1, QueryTriggerInteraction.Ignore))
			{
				return true;
			}
			return hit.point.y <= surface - 0.05f;
		}

		public void ReleaseGrab()
		{
			// Rebuilt from native code (token 0x0600055B, RVA 0x27D5FF0)
			if (!NetBridge.RunsMonsters)
			{
				return;
			}
			LetGo();
		}

		private void LetGo()
		{
			// Rebuilt from native code (token 0x0600055C, RVA 0x27D607C)
			remoteGrab = false;
			Enter(State.Wander);
			idleUntil = Time.time + 1.5f;
			PickWanderTarget();
		}

		public void RemoteCaught(Vector3 victim, int killCode)
		{
			// Rebuilt from native code (token 0x0600055D, RVA 0x27D722C)
			if (!NetBridge.RunsMonsters || Current == State.Grab)
			{
				return;
			}
			CurrentKill = Kill.FromCode(killCode);
			pinReady = false;
			remoteGrab = true;
			grabFacing = victim;
			Enter(State.Grab);
			Roar();
		}

		public void SetKill(Kill kill)
		{
			// Rebuilt from native code (token 0x0600055E, RVA 0x27D7330)
			CurrentKill = kill;
			pinReady = false;
		}

		public void WatchKill(Vector3 victim, int killCode)
		{
			// Rebuilt from native code (token 0x0600055F, RVA 0x27D733C)
			if (NetBridge.RunsMonsters)
			{
				return;
			}
			CurrentKill = Kill.FromCode(killCode);
			pinReady = false;
			grabFacing = victim;
			if (Current != State.Grab)
			{
				Enter(State.Grab);
			}
			holdKillUntil = Time.time + CurrentKill.End;
		}

		public void TestGrab(Vector3 victim, Vector3 standAt, Kill kill)
		{
			// Rebuilt from native code (token 0x06000560, RVA 0x27D7460)
			Vector3 facing = new Vector3(victim.x - standAt.x, 0f, victim.z - standAt.z);
			transform.SetPositionAndRotation(new Vector3(standAt.x, transform.position.y, standAt.z), facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing, Vector3.up) : transform.rotation);
			if (legs != null)
			{
				float floor = water ? water.FloorY : transform.position.y;
				foreach (Leg leg in legs)
				{
					leg.stepping = false;
					Vector3 home = transform.TransformPoint(leg.home);
					leg.planted = new Vector3(home.x, floor, home.z);
				}
			}
			lastPosition = transform.position;
			lastYaw = transform.eulerAngles.y;
			speed = 0f;
			velocity = Vector3.zero;
			CurrentKill = kill;
			pinReady = false;
			grabFacing = victim;
			remoteGrab = false;
			Enter(State.Grab);
			holdKillUntil = Time.time + kill.End;
		}

		private void CalibratePin()
		{
			// Rebuilt from native code (token 0x06000561, RVA 0x27D0400)
			pinReady = true;
			float surface = water ? water.SurfaceY : transform.position.y + 0.5f;
			Vector3 spot = transform.position + transform.forward * Mathf.Max(0.8f, headReach * 0.8f);
			spot.y = surface;
			pinLocal = transform.InverseTransformPoint(spot);
			if (!neck || !grip || !body)
			{
				return;
			}
			Vector3 savedBodyPosition = body.localPosition;
			Quaternion savedBodyRotation = body.localRotation;
			Quaternion savedNeckRotation = neck.localRotation;
			body.localPosition = bodyBase + Vector3.up * -PinCrouch;
			body.localRotation = bodyBaseRotation * Quaternion.Euler(PinBodyPitch, 0f, 0f);
			float aimY = surface + 0.12f;
			float bestError = float.MaxValue;
			Vector3 best = spot;
			for (float pitch = 10f; pitch <= 100f; pitch += 2.5f)
			{
				neck.localRotation = neckBase * Quaternion.Euler(pitch - neckRestPitch, 0f, 0f);
				Vector3 held = grip.position;
				float miss = Mathf.Abs(held.y - aimY);
				if (miss < bestError - 0.01f)
				{
					bestError = miss;
					pinPitch = pitch;
					best = held;
				}
			}
			body.localPosition = savedBodyPosition;
			body.localRotation = savedBodyRotation;
			neck.localRotation = savedNeckRotation;
			pinLocal = transform.InverseTransformPoint(new Vector3(best.x, surface, best.z));
		}

		public void SetRemote(Vector3 position, float yaw, State state, Vector3 remoteTarget, float remoteSpeedValue, float sentAt)
		{
			// Rebuilt from native code (token 0x06000562, RVA 0x27D77B8)
			if (!hasRemote)
			{
				transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
			}
			hasRemote = true;
			remoteState = state;
			target = remoteTarget;
			remoteSpeed = remoteSpeedValue;
			if (sampleCount > 0)
			{
				if (samples[0].time >= sentAt)
				{
					return;
				}
				Sample last = samples[sampleCount - 1];
				if (Mathf.Abs(sentAt - last.time) > 5f)
				{
					sampleCount = 0;
				}
				else if (last.time < sentAt)
				{
					float reach = ShallowsTuning.I ? ShallowsTuning.I.chargeSpeed * 1.6f : 43.2f;
					float jump = reach * (sentAt - last.time) + 2.5f;
					if ((position - last.position).sqrMagnitude > jump * jump)
					{
						remoteJumpAt = Time.time;
					}
				}
			}
			int insert = sampleCount;
			while (insert > 0 && samples[insert - 1].time > sentAt)
			{
				insert--;
			}
			if (insert > 0 && Mathf.Approximately(samples[insert - 1].time, sentAt))
			{
				return;
			}
			if (sampleCount == samples.Length)
			{
				if (insert == 0)
				{
					return;
				}
				Array.Copy(samples, 1, samples, 0, sampleCount - 1);
				sampleCount--;
				insert--;
			}
			Array.Copy(samples, insert, samples, insert + 1, sampleCount - insert);
			samples[insert] = new Sample
			{
				time = sentAt,
				yaw = yaw,
				position = position
			};
			sampleCount++;
		}

		private void Playback(float t, out Vector3 position, out float yaw)
		{
			// Rebuilt from native code (token 0x06000563, RVA 0x27D7B70)
			if (sampleCount == 1 || samples[0].time >= t)
			{
				position = samples[0].position;
				yaw = samples[0].yaw;
				return;
			}
			for (int i = 0; i < sampleCount - 1; i++)
			{
				Sample a = samples[i];
				Sample b = samples[i + 1];
				if (b.time >= t)
				{
					float k = (t - a.time) / Mathf.Max(b.time - a.time, 0.0001f);
					position = Vector3.Lerp(a.position, b.position, k);
					yaw = Mathf.LerpAngle(a.yaw, b.yaw, k);
					return;
				}
			}
			Sample newest = samples[sampleCount - 1];
			Sample before = samples[sampleCount - 2];
			float ahead = Mathf.Min(t - newest.time, MaxExtrapolate) / Mathf.Max(newest.time - before.time, 0.0001f);
			position = newest.position + (newest.position - before.position) * ahead;
			yaw = newest.yaw + Mathf.DeltaAngle(before.yaw, newest.yaw) * ahead;
		}

		private float FollowRemote(float dt)
		{
			// Rebuilt from native code (token 0x06000564, RVA 0x27D3740)
			if (remoteState != Current && !(Current == State.Grab && Time.time < holdKillUntil))
			{
				bool calm = Current == State.Wander || Current == State.Search || Current == State.Stalk;
				if ((calm && remoteState == State.Investigate && Time.time - lastRoar > 4f) || remoteState == State.Grab)
				{
					Roar();
				}
				Enter(remoteState);
			}
			Vector3 position = transform.position;
			if (sampleCount == 0)
			{
				return 0f;
			}
			Playback(NetBridge.SharedTime - PlaybackDelay, out Vector3 sampled, out float yaw);
			Vector3 next = (sampled - position).sqrMagnitude > 25f ? sampled : Vector3.Lerp(position, sampled, 1f - MathF.Exp(dt * -30f));
			transform.position = next;
			Quaternion facing = Quaternion.Euler(0f, yaw, 0f);
			transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - MathF.Exp(dt * -20f));
			speed = Mathf.MoveTowards(speed, remoteSpeed, speedUp * 2f * dt);
			return FlatDistance(next, position);
		}

		private void TakeOverBrain()
		{
			// Rebuilt from native code (token 0x06000565, RVA 0x27D36FC)
			remoteGrab = Current == State.Grab;
			if (Current == State.Wander)
			{
				PickWanderTarget();
			}
			if (Current == State.Stalk)
			{
				lastVoiceAt = Time.time;
			}
		}

		private void Enter(State next)
		{
			// Rebuilt from native code (token 0x06000566, RVA 0x27D10BC)
			Current = next;
			stateTime = 0f;
		}

		private void Roar()
		{
			// Rebuilt from native code (token 0x06000567, RVA 0x27D2FBC)
			if (Time.time < hushUntil)
			{
				return;
			}
			lastRoar = Time.time;
			AudioClip clip = Sfx.MonsterRoar();
			if (clip)
			{
				mouthBusyUntil = Time.time + clip.length;
			}
			if (voice)
			{
				voice.PlayOneShot(clip);
			}
		}

		private float Lurch()
		{
			// Rebuilt from native code (token 0x06000568, RVA 0x27D505C)
			return Mathf.PerlinNoise(Time.time * 0.6f, index * 7.3f) * 0.8f + 0.6f;
		}

		private void PickWanderTarget()
		{
			// Rebuilt from native code (token 0x06000569, RVA 0x27D10C8)
			float bestScore = float.MinValue;
			for (int i = 0; i < 4; i++)
			{
				Vector3 candidate = nav != null ? nav.RandomOpen(3f) : (water ? water.RandomPoint(3f) : transform.position + UnityEngine.Random.insideUnitSphere * 10f);
				Vector3 away = candidate - transform.position;
				away.y = 0f;
				float facing = away.sqrMagnitude > 1f ? Vector3.Dot(transform.forward, away.normalized) : -1f;
				float score = facing + UnityEngine.Random.value * 0.9f;
				if (score > bestScore)
				{
					bestScore = score;
					target = candidate;
				}
			}
		}

		private bool MoveTowards(Vector3 goal, float topSpeed, float arrive, float dt, out float moved)
		{
			// Rebuilt from native code (token 0x0600056A, RVA 0x27D50B4)
			Vector3 position = transform.position;
			Vector3 flat = new Vector3(goal.x - position.x, 0f, goal.z - position.z);
			float distance = flat.magnitude;
			float gap = Mathf.Max(0f, distance - headReach);
			float off = 0f;
			if (distance > 0.01f && headReach > 0f)
			{
				off = Mathf.Abs(Vector3.SignedAngle(transform.forward, flat, Vector3.up));
			}
			if (gap < arrive && off < 30f)
			{
				moved = Brake(dt);
				return true;
			}
			bool direct = true;
			Vector3 aim = goal;
			float remaining = gap;
			if (nav != null)
			{
				if (goal != pathGoal || Time.time >= replanAt)
				{
					nav.FindPath(position, goal, path);
					pathGoal = goal;
					replanAt = Time.time + 1f;
				}
				while (path.Count > 1 && (FlatDistance(position, path[0]) < 0.05f || nav.Clear(position, path[1])))
				{
					path.RemoveAt(0);
				}
				if (path.Count > 0 && !nav.Clear(position, path[0]))
				{
					nav.FindPath(position, goal, path);
					replanAt = Time.time + 1f;
				}
				if (path.Count == 0)
				{
					moved = Brake(dt);
					return true;
				}
				Vector3 last = path[path.Count - 1];
				float lastToGoal = FlatDistance(last, goal);
				if (path.Count <= 1 && lastToGoal < 0.5f)
				{
					aim = last;
				}
				else
				{
					if (path.Count == 1 && FlatDistance(position, last) < 0.5f)
					{
						float angle = Vector3.SignedAngle(transform.forward, flat, Vector3.up);
						TurnBy(angle, dt);
						moved = Brake(dt);
						return Mathf.Abs(angle) < 30f;
					}
					aim = path[0];
					remaining = FlatDistance(position, path[0]);
					for (int i = 0; i < path.Count - 1; i++)
					{
						remaining += FlatDistance(path[i], path[i + 1]);
					}
					if (lastToGoal < 0.5f)
					{
						remaining = Mathf.Max(0f, remaining - headReach);
					}
					direct = false;
				}
			}
			Vector3 toAim = new Vector3(aim.x - position.x, 0f, aim.z - position.z);
			float aimDistance = toAim.magnitude;
			Vector3 heading;
			if (aimDistance < 0.01f)
			{
				heading = transform.forward;
			}
			else
			{
				heading = toAim / aimDistance;
				if (!(direct && gap < 3f) && nav == null)
				{
					heading = SteerAround(heading);
				}
			}
			bool arriving = direct && gap < arrive;
			Vector3 face = heading;
			if (distance > 0.01f && arriving)
			{
				face = flat / distance;
			}
			float turn = Vector3.SignedAngle(transform.forward, face, Vector3.up);
			float turned = TurnBy(turn, dt);
			if (arriving)
			{
				moved = Brake(dt);
				return false;
			}
			float behind = Mathf.Abs(turn - turned);
			float pace = behind < 20f ? 1f : (behind < 60f ? 0.4f : 0.15f);
			bool alongHeading = true;
			if (Current == State.Wander || Current == State.Stalk)
			{
				float lined = Mathf.InverseLerp(0.1f, 1f, MathF.Cos(behind * Mathf.Deg2Rad));
				pace = 0.12f + 0.88f * lined * lined;
				alongHeading = false;
			}
			float stopDistance = Mathf.Max(0f, remaining - (direct ? arrive : 0f));
			float wanted = Mathf.Min(pace * topSpeed, Mathf.Max(Mathf.Sqrt(2f * braking * stopDistance), 1f));
			speed = Mathf.MoveTowards(speed, wanted, (wanted < speed ? braking : speedUp) * dt);
			moved = Mathf.Min(speed * dt, direct ? gap : aimDistance);
			Step((alongHeading ? heading : transform.forward) * moved, dt);
			return false;
		}

		private float TurnBy(float angle, float dt)
		{
			// Rebuilt from native code (token 0x0600056B, RVA 0x27D7DB0)
			float step;
			if (Current == State.Wander || Current == State.Stalk)
			{
				float limit = Current == State.Stalk ? wanderTurnSpeed * 0.8f : wanderTurnSpeed;
				turnVelocity = Mathf.MoveTowards(Mathf.Clamp(turnVelocity, -limit, limit), Mathf.Clamp(angle * 1.3f, -limit, limit), wanderTurnAcceleration * dt);
				step = turnVelocity * dt;
			}
			else
			{
				float most = turnSpeed * dt;
				step = Mathf.Clamp(angle, -most, most);
				turnVelocity = step / Mathf.Max(dt, 0.0001f);
			}
			transform.Rotate(0f, step, 0f);
			turnedFrame = Time.frameCount;
			return step;
		}

		private float Brake(float dt)
		{
			// Rebuilt from native code (token 0x0600056C, RVA 0x27D4F34)
			if ((Current == State.Wander || Current == State.Stalk) && turnVelocity != 0f && turnedFrame != Time.frameCount)
			{
				turnVelocity = Mathf.MoveTowards(turnVelocity, 0f, wanderTurnAcceleration * dt);
				transform.Rotate(0f, turnVelocity * dt, 0f);
			}
			speed = Mathf.MoveTowards(speed, 0f, braking * dt);
			float moved = speed * dt;
			if (moved > 0f)
			{
				Step(transform.forward * moved, dt);
			}
			return moved;
		}

		private void Step(Vector3 delta, float dt)
		{
			// Rebuilt from native code (token 0x0600056D, RVA 0x27D80B8)
			Vector3 position = transform.position;
			Vector3 next = position + delta;
			if (water)
			{
				next = water.ClampXZ(next, edgeInset);
			}
			if (nav != null && nav.IsOpen(position) && !nav.Clear(position, next))
			{
				if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) && nav.Clear(position, new Vector3(next.x, next.y, position.z)))
				{
					next.z = position.z;
				}
				else if (nav.Clear(position, new Vector3(position.x, next.y, next.z)))
				{
					next.x = position.x;
				}
				else if (nav.Clear(position, new Vector3(next.x, next.y, position.z)))
				{
					next.z = position.z;
				}
				else
				{
					speed = 0f;
					replanAt = 0f;
					return;
				}
			}
			if (water)
			{
				next.y = Mathf.MoveTowards(next.y, water.FloorY, 2f * dt);
			}
			transform.position = next;
		}

		private Vector3 SteerAround(Vector3 dir)
		{
			// Rebuilt from native code (token 0x0600056E, RVA 0x27D7EBC)
			Vector3 origin = transform.position + Vector3.up * 1.6f;
			if (!Blocked(origin, dir))
			{
				return dir;
			}
			float[] angles = new float[] { 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };
			foreach (float angle in angles)
			{
				Vector3 turned = Quaternion.Euler(0f, angle, 0f) * dir;
				if (!Blocked(origin, turned))
				{
					return turned;
				}
			}
			return dir;
		}

		private bool Blocked(Vector3 origin, Vector3 dir)
		{
			// Rebuilt from native code (token 0x0600056F, RVA 0x27D8354)
			return Physics.SphereCast(origin, avoidRadius, dir, out RaycastHit _, 2.5f, Obstacles, QueryTriggerInteraction.Ignore);
		}

		private float KeepApart(float dt)
		{
			// Rebuilt from native code (token 0x06000570, RVA 0x27D60B0)
			if (Current == State.Grab)
			{
				crowdTime = 0f;
				return 0f;
			}
			BodyLine(out Vector3 tail, out Vector3 nose);
			Vector3 push = Vector3.zero;
			bool crowded = false;
			float give = Mathf.Min(dt * 5f, 1f);
			for (int i = 0; i < All.Count; i++)
			{
				MonsterAI other = All[i];
				if (other == this || !other || !other.isActiveAndEnabled || other.water != water)
				{
					continue;
				}
				other.BodyLine(out Vector3 otherTail, out Vector3 otherNose);
				ClosestPoints(tail, nose, otherTail, otherNose, out Vector3 mine, out Vector3 theirs);
				Vector3 apart = mine - theirs;
				float distance = apart.magnitude;
				float touch = radius + other.radius;
				float reach = touch + 0.35f;
				if (distance >= reach)
				{
					continue;
				}
				Vector3 away = distance > 0.001f ? apart / distance : (index < other.index ? transform.right : -transform.right);
				float share = other.Current == State.Grab ? 1f : 0.5f;
				if (distance < touch + 0.15f)
				{
					crowded = true;
				}
				push += away * (Mathf.Max(0f, touch - distance) * share + give * ((reach - Mathf.Max(distance, touch)) * share));
			}
			crowdTime = crowded ? crowdTime + dt : 0f;
			if (push == Vector3.zero)
			{
				return 0f;
			}
			Vector3 position = transform.position;
			Vector3 next = position + push;
			if (water)
			{
				next = water.ClampXZ(next, edgeInset);
			}
			if (nav != null && !(nav.IsOpen(position) ? nav.Clear(position, next) : nav.IsOpen(next)))
			{
				bool open = nav.IsOpen(position);
				Vector3 slideX = new Vector3(next.x, position.y, position.z);
				Vector3 slideZ = new Vector3(position.x, position.y, next.z);
				if (open ? nav.Clear(position, slideX) : nav.IsOpen(slideX))
				{
					next.z = position.z;
				}
				else if (open ? nav.Clear(position, slideZ) : nav.IsOpen(slideZ))
				{
					next.x = position.x;
				}
				else
				{
					return 0f;
				}
			}
			Vector3 moved = new Vector3(next.x, position.y, next.z);
			transform.position = moved;
			return FlatDistance(position, moved);
		}

		private void BodyLine(out Vector3 tail, out Vector3 nose)
		{
			// Rebuilt from native code (token 0x06000571, RVA 0x27D842C)
			Vector3 forward = transform.forward;
			forward.y = 0f;
			Vector3 along = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
			Vector3 position = transform.position;
			tail = position + along * bodyBack;
			nose = position + along * Mathf.Max(bodyFront, headReach * 0.85f);
		}

		private static void ClosestPoints(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1, out Vector3 c2)
		{
			// Rebuilt from native code (token 0x06000572, RVA 0x27D8604)
			Vector3 d1 = q1 - p1;
			Vector3 d2 = q2 - p2;
			Vector3 r = p1 - p2;
			float a = Vector3.Dot(d1, d1);
			float e = Vector3.Dot(d2, d2);
			float f = Vector3.Dot(d2, r);
			float s;
			float t;
			if (a < 1E-06f && e < 1E-06f)
			{
				s = 0f;
				t = 0f;
			}
			else if (a < 1E-06f)
			{
				s = 0f;
				t = Mathf.Clamp01(f / e);
			}
			else
			{
				float c = Vector3.Dot(d1, r);
				if (e < 1E-06f)
				{
					t = 0f;
					s = Mathf.Clamp01(-c / a);
				}
				else
				{
					float b = Vector3.Dot(d1, d2);
					float denom = a * e - b * b;
					s = denom > 1E-06f ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
					t = (f + b * s) / e;
					if (t < 0f)
					{
						t = 0f;
						s = Mathf.Clamp01(-c / a);
					}
					else if (t > 1f)
					{
						t = 1f;
						s = Mathf.Clamp01((b - c) / a);
					}
				}
			}
			c1 = p1 + d1 * s;
			c2 = p2 + d2 * t;
		}

		private void FacePlayer(float dt)
		{
			// Rebuilt from native code (token 0x06000573, RVA 0x27D5E14)
			Vector3 toward = grabFacing - transform.position;
			toward.y = 0f;
			if (toward.sqrMagnitude < 0.01f)
			{
				return;
			}
			Quaternion look = Quaternion.LookRotation(toward);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
		}

		private void CheckGrab()
		{
			// Rebuilt from native code (token 0x06000574, RVA 0x27D3AB4)
			PlayerRig rig = PlayerRig.I;
			if (Current == State.Grab)
			{
				return;
			}
			GameManager game = GameManager.I;
			if (!rig || !game || !game.CanBeCaught)
			{
				return;
			}
			if (NoCatching || (!NetBridge.RunsMonsters && Time.time - remoteJumpAt < RemoteJumpGrace) || !rig.Wet)
			{
				return;
			}
			Vector3 feet = rig.BodyBottom;
			float reach = ShallowsTuning.I ? ShallowsTuning.I.grabReach : 2.2f;
			bool inMouth = (Current == State.Investigate || Current == State.Search) && FlatDistance(feet, Mouth) < reach;
			if (!inMouth && !(BodyDistance(feet) < legContactRange))
			{
				return;
			}
			grabFacing = feet;
			Kill kill = Kill.Random();
			CurrentKill = kill;
			pinReady = false;
			if (NetBridge.RunsMonsters)
			{
				remoteGrab = false;
				Enter(State.Grab);
			}
			else
			{
				Enter(State.Grab);
				holdKillUntil = Time.time + kill.End;
			}
			NetBridge.ReportCaught(index, kill.Code);
			game.PlayerCaught(this);
		}

		public bool WouldCatch(Vector3 feet, float slack)
		{
			// Rebuilt from native code (token 0x06000575, RVA 0x27D8894)
			if (Current == State.Grab)
			{
				return false;
			}
			float reach = ShallowsTuning.I ? ShallowsTuning.I.grabReach : 2.2f;
			if ((Current == State.Investigate || Current == State.Search) && FlatDistance(feet, Mouth) < reach * slack)
			{
				return true;
			}
			return BodyDistance(feet) < legContactRange * slack;
		}

		public bool IsExposed(Vector3 feet)
		{
			// Rebuilt from native code (token 0x06000576, RVA 0x27D8A50)
			return Exposed(feet);
		}

		private static float FlatDistance(Vector3 a, Vector3 b)
		{
			// Rebuilt from native code (token 0x06000578, RVA 0x27D5D98)
			float dx = a.x - b.x;
			float dz = a.z - b.z;
			return Mathf.Sqrt(dz * dz + dx * dx);
		}

		private float BodyDistance(Vector3 p)
		{
			// Rebuilt from native code (token 0x06000579, RVA 0x27D87D4)
			Vector3 local = transform.InverseTransformPoint(p);
			float along = local.z - Mathf.Clamp(local.z, bodyBack, bodyFront);
			return Mathf.Sqrt(local.x * local.x + along * along);
		}

		private void SetUpBreath()
		{
			// Rebuilt from native code (token 0x0600057A, RVA 0x27D25D8)
			if (!voice)
			{
				return;
			}
			voiceVolume = voice.volume;
			breath = voice.gameObject.AddComponent<AudioSource>();
			breath.loop = false;
			breath.playOnAwake = false;
			breath.spatialBlend = voice.spatialBlend;
			breath.rolloffMode = voice.rolloffMode;
			breath.minDistance = voice.minDistance * 0.6f;
			breath.maxDistance = voice.maxDistance * 0.5f;
			breath.dopplerLevel = 0f;
			breathAt = Time.time + UnityEngine.Random.Range(0.5f, 4f);
		}

		private void Breathe(ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x0600057B, RVA 0x27D8A54)
			float now = Time.time;
			float dt = Time.deltaTime;
			if (voice)
			{
				if (now < hushUntil)
				{
					voice.volume = Mathf.MoveTowards(voice.volume, 0f, dt / 0.12f);
					if (voice.volume <= 0f)
					{
						voice.Stop();
					}
				}
				else
				{
					voice.volume = voiceVolume;
				}
			}
			GrabVoice();
			if (!breath)
			{
				return;
			}
			if (now < mouthBusyUntil || now < hushUntil)
			{
				breath.volume = Mathf.MoveTowards(breath.volume, 0f, dt / 0.15f);
				if (breath.volume <= 0f && breath.isPlaying)
				{
					breath.Stop();
				}
				breathAt = Mathf.Max(breathAt, Mathf.Max(mouthBusyUntil, hushUntil) + UnityEngine.Random.Range(0.6f, 1.5f));
				return;
			}
			State state = Current;
			if ((state != State.Wander && state != State.Search) || now < breathAt)
			{
				return;
			}
			AudioClip clip = Sfx.MonsterBreath();
			if (!clip)
			{
				return;
			}
			float loudness = tuning ? tuning.monsterBreathVolume : 0.8f;
			breath.clip = clip;
			breath.volume = Mathf.Min((state == State.Search ? 1f : 0.6f) * loudness, 1f);
			breath.pitch = UnityEngine.Random.Range(0.92f, 1.04f) - index % 3 * 0.03f;
			breath.Play();
			breathAt = now + clip.length / breath.pitch + (state == State.Search ? UnityEngine.Random.Range(0.1f, 0.5f) : UnityEngine.Random.Range(1.5f, 4f));
		}

		private void GrabVoice()
		{
			// Rebuilt from native code (token 0x0600057C, RVA 0x27D8E50)
			if (Current != State.Grab)
			{
				grabCalls = 0;
				return;
			}
			if (!voice)
			{
				return;
			}
			float t = stateTime;
			while (grabCalls < GrabGrowls.Length && t >= GrabGrowls[grabCalls])
			{
				grabCalls++;
				if (Time.time < hushUntil)
				{
					continue;
				}
				AudioClip clip = Sfx.MonsterGrowl();
				if (clip)
				{
					voice.PlayOneShot(clip, 1f);
				}
				mouthBusyUntil = Mathf.Max(mouthBusyUntil, Time.time + (clip ? clip.length : 0.5f));
			}
			if (grabCalls == GrabGrowls.Length && CurrentKill.move == KillMove.Shake && t >= 0.4f)
			{
				grabCalls++;
				Roar();
			}
		}

		public void Hush(float seconds)
		{
			// Rebuilt from native code (token 0x0600057D, RVA 0x27D90B8)
			hushUntil = Mathf.Max(hushUntil, Time.time + seconds);
		}

		private void Animate(float moved, ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x0600057E, RVA 0x27D3EF0)
			Breathe(tuning);
			if (FourLegged)
			{
				AnimateFourLegs(moved, tuning);
				return;
			}
			float charge = Mathf.Clamp01(speed / Mathf.Max(tuning.chargeSpeed, 0.1f));
			float walk = Mathf.Clamp01(speed / 1.5f);
			walkPhase += moved / Mathf.Max(stride, 0.1f) * Mathf.PI;
			int step = Mathf.FloorToInt(walkPhase / Mathf.PI);
			if (step != lastStep)
			{
				lastStep = step;
				Footstep((step & 1) == 0, charge);
			}
			float swing = walk * (MathF.Sin(walkPhase) * 28f);
			leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
			rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
			float lean;
			float leftArmPitch;
			float rightArmPitch;
			float headYaw;
			if (Current == State.Grab)
			{
				float lift = DeathTimeline.Lift(stateTime, out float shake);
				lean = 35f - 20f * lift;
				leftArmPitch = 85f;
				rightArmPitch = Mathf.Lerp(85f, 170f, lift);
				headYaw = shake * 50f;
			}
			else if (Current == State.Search)
			{
				float wave = MathF.Sin(stateTime * 2.4f) * 30f;
				leftArmPitch = wave + 45f;
				rightArmPitch = 45f - wave;
				headYaw = MathF.Sin(stateTime * 1.7f) * 55f;
				lean = 22f;
			}
			else
			{
				leftArmPitch = swing * -0.7f;
				rightArmPitch = swing * 0.7f;
				headYaw = 0f;
				lean = Current == State.Investigate ? charge * 14f : 0f;
			}
			float bob = walk * ((Mathf.Abs(MathF.Cos(walkPhase)) - 0.5f) * 0.14f);
			body.localPosition = bodyBase + Vector3.up * bob;
			body.localRotation = Quaternion.Slerp(body.localRotation, bodyBaseRotation * Quaternion.Euler(lean, 0f, 0f), Time.deltaTime * 6f);
			leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(-leftArmPitch, 0f, 0f), Time.deltaTime * 8f);
			rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-rightArmPitch, 0f, 0f), Time.deltaTime * 8f);
			if (head)
			{
				head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(0f, headYaw, 0f), Time.deltaTime * 5f);
			}
		}

		private void SetUpLegs()
		{
			// Rebuilt from native code (token 0x0600057F, RVA 0x27D18D4)
			legs = new Leg[4];
			for (int i = 0; i < 4; i++)
			{
				if (!hips[i] || knees == null || i >= knees.Length || !knees[i] || feet == null || i >= feet.Length || !feet[i])
				{
					legs = null;
					return;
				}
				Transform contact = new GameObject("Foot Contact").transform;
				contact.SetParent(knees[i], false);
				contact.position = feet[i].position;
				feet[i] = contact;
				Vector3 hipLocal = transform.InverseTransformPoint(hips[i].position);
				Vector3 kneeLocal = transform.InverseTransformPoint(knees[i].position);
				Vector3 footLocal = transform.InverseTransformPoint(contact.position);
				Vector3 down = (footLocal - hipLocal).normalized;
				Vector3 toKnee = kneeLocal - hipLocal;
				Vector3 bend = toKnee - down * Vector3.Dot(toKnee, down);
				legs[i] = new Leg
				{
					hip = hips[i],
					knee = knees[i],
					contact = contact,
					hipRest = hips[i].localRotation,
					kneeRest = knees[i].localRotation,
					home = footLocal,
					pole = bend.sqrMagnitude > 1E-06f ? bend.normalized : (i < 2 ? Vector3.back : Vector3.forward),
					upper = Vector3.Distance(hips[i].position, knees[i].position),
					lower = Vector3.Distance(knees[i].position, contact.position),
					planted = contact.position,
					group = (i == 0 || i == 3) ? 0 : 1
				};
			}
			if (head)
			{
				headBase = head.localRotation;
			}
			if (claws != null)
			{
				clawBase = new Quaternion[claws.Length];
				clawSign = new float[claws.Length];
				for (int i = 0; i < claws.Length; i++)
				{
					Transform claw = claws[i];
					if (!claw)
					{
						continue;
					}
					clawBase[i] = claw.localRotation;
					clawSign[i] = 1f;
					int parent = Array.IndexOf(claws, claw.parent);
					if (claw.childCount > 0)
					{
						float before = claw.GetChild(0).position.y;
						claw.localRotation = clawBase[i] * Quaternion.Euler(10f, 0f, 0f);
						clawSign[i] = claw.GetChild(0).position.y < before ? 1f : -1f;
						claw.localRotation = clawBase[i];
					}
					else if (parent >= 0 && parent < i)
					{
						clawSign[i] = clawSign[parent];
					}
				}
			}
			lastPosition = transform.position;
			lastYaw = transform.eulerAngles.y;
		}

		private void AnimateFourLegs(float moved, ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x06000580, RVA 0x27D90F0)
			float dt = Time.deltaTime;
			float now = Time.time;
			float charge = Mathf.Clamp01(speed / Mathf.Max(tuning.chargeSpeed, 0.1f));
			float step = Mathf.Max(dt, 0.0001f);
			Vector3 position = transform.position;
			Vector3 measured = new Vector3((position.x - lastPosition.x) / step, 0f, (position.z - lastPosition.z) / step);
			Vector3 previousVelocity = lastVelocity;
			velocity = Vector3.Lerp(velocity, measured, 1f - MathF.Exp(step * -20f));
			lastVelocity = velocity;
			lastPosition = transform.position;
			float yaw = transform.eulerAngles.y;
			float turning = Mathf.DeltaAngle(lastYaw, yaw) / step;
			lastYaw = yaw;
			yawRate = Mathf.Lerp(yawRate, turning, 1f - MathF.Exp(step * -15f));
			Vector3 forward = transform.forward;
			if (Current != animState)
			{
				if (Current == State.Stalk)
				{
					flickAt = now;
				}
				else if (Current == State.Investigate)
				{
					startle = now;
				}
				animState = Current;
				twitchAt = 0f;
			}
			float sinceStartle = now - startle;
			float jolt = sinceStartle < 0.5f ? MathF.Sin(Mathf.Clamp01(sinceStartle * 2f) * Mathf.PI) : 0f;
			if (Current == State.Stalk && FlatDistance(target, lookedAt) > 1.5f)
			{
				flickAt = now;
			}
			lookedAt = target;
			float twitchScale = 1f;
			float twitchMin = 0.5f;
			float jaw = 0.05f;
			float twitchMax = 2.2f;
			float tiltChance = 0.25f;
			float neckFrequency = 4.5f;
			float neckPitch = 15f;
			float lean = 0f;
			float crouch = 0f;
			float neckYawAim = 0f;
			float neckDamping = 0.5f;
			float headRollAim = 0f;
			float roll = 0f;
			switch (Current)
			{
			case State.Wander:
				neckYawAim = Mathf.Clamp(yawRate * 0.4f, -22f, 22f);
				twitchScale = 0.45f;
				twitchMin = 1.5f;
				twitchMax = 4f;
				neckPitch = 18f;
				crouch = 0.04f;
				neckFrequency = 2.2f;
				neckDamping = 0.7f;
				tiltChance = 0.15f;
				break;
			case State.Investigate:
				crouch = charge * 0.12f + 0.08f;
				lean = charge * 6f;
				neckYawAim = AimYaw(target, 40f);
				neckPitch = 2f;
				neckFrequency = 7f;
				tiltChance = 0f;
				neckDamping = 0.6f;
				jaw = 0.55f;
				twitchMin = 0.15f;
				twitchScale = 0.25f;
				twitchMax = 0.5f;
				break;
			case State.Search:
				lean = 7f;
				jaw = 0.2f;
				twitchScale = 2.2f;
				twitchMax = 0.45f;
				twitchMin = 0.12f;
				neckFrequency = 9f;
				neckDamping = 0.42f;
				tiltChance = 0.4f;
				if (now > chatterUntil && UnityEngine.Random.value < step * 2.5f)
				{
					chatterUntil = now + UnityEngine.Random.Range(0.25f, 0.6f);
				}
				neckPitch = 40f;
				crouch = 0.2f;
				break;
			case State.Grab:
			{
				float t = stateTime;
				if (CurrentKill.move == KillMove.Consume)
				{
					float gulp = Mathf.Clamp01((t - Kill.ConsumeGulp - 0.1f) / 0.35f);
					float lift = Mathf.Clamp01((t - Kill.ConsumeLift) / (Kill.ConsumeLifted - Kill.ConsumeLift));
					float gulpEase = gulp * gulp * (3f - 2f * gulp);
					float liftEase = lift * lift * (3f - 2f * lift);
					neckPitch = Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(15f, 0f, t / 0.18f), -55f, liftEase), 15f, gulpEase);
					if (t > Kill.ConsumeSmash && t < 1.03f)
					{
						neckPitch += 12f;
					}
					jaw = t < Kill.ConsumeRaise ? 0.25f : (t < Kill.ConsumeSwallowed ? 0.5f : (t < 1.4f ? 0f : 0.08f));
					if (grip)
					{
						Vector3 gripPosition = grip.position;
						Vector3 gripForward = grip.forward;
						float raise = (t - Kill.ConsumeRaise) * 4f;
						float bring = Mathf.Clamp01((t - 0.8f) / 0.08f);
						float swallow = Mathf.Clamp01((t - Kill.ConsumeSmash) / (Kill.ConsumeSwallowed - Kill.ConsumeSmash));
						Vector3 right = transform.right;
						Vector3 bodyForward = transform.forward;
						Vector3 mouth = gripPosition + gripForward * 0.22f;
						Vector3 hand;
						if (swallow > 0f)
						{
							hand = Vector3.Lerp(mouth, gripPosition + gripForward * -0.05f, swallow);
						}
						else
						{
							Vector3 reach = gripPosition + gripForward * 0.7f + right * 0.15f + bodyForward * -0.1f;
							hand = Vector3.Lerp(reach, mouth, bring * bring);
						}
						handTarget[1] = hand;
						float hold;
						if (t < Kill.ConsumeRaise)
						{
							hold = 0f;
						}
						else if (t < Kill.ConsumeSwallowed)
						{
							hold = Mathf.Clamp01(Mathf.Clamp01(raise) * 3f);
						}
						else
						{
							hold = 1f - Mathf.Clamp01((t - Kill.ConsumeSwallowed) / 0.3f);
						}
						handWeight[1] = hold * hold * (3f - 2f * hold);
					}
					handWeight[0] = 0f;
					lean = -10f * liftEase * (1f - gulp);
					twitchScale = 0f;
					tiltChance = 0f;
					headRollAim = 0f;
					crouch = 0f;
					neckYawAim = 0f;
					neckFrequency = 14f;
					neckDamping = 0.65f;
				}
				else if (CurrentKill.move == KillMove.Pin)
				{
					if (!pinReady)
					{
						CalibratePin();
					}
					float dip = CurrentKill.PinHead(t, out float pinJaw);
					neckYawAim = 0f;
					neckPitch = pinPitch;
					if (dip >= 0.99f)
					{
						neckYawAim = MathF.Sin(t * 47f) * 9f;
						headRollAim = MathF.Sin(t * 31f) * 12f;
					}
					Vector3 pin = PinPoint;
					Vector3 bodyForward = transform.forward;
					float dipLean = (dip - 1f) * 40f;
					Vector3 right = transform.right;
					float press;
					float bob;
					if (t < Kill.PinDown)
					{
						press = Mathf.Clamp01(t / Kill.PinDown);
						bob = MathF.Sin(Mathf.Clamp01(t / Kill.PinDown) * Mathf.PI) * 0.35f;
					}
					else
					{
						bob = 0f;
						press = 1f;
						if (t >= CurrentKill.Release)
						{
							press = 1f - Mathf.Clamp01((t - CurrentKill.Release) / Kill.PinDown);
						}
					}
					float hold = press * press * (3f - 2f * press);
					Vector3 centre = pin + bodyForward * 0.2f;
					neckPitch += dipLean;
					crouch = PinCrouch;
					for (int i = 0; i < 2; i++)
					{
						handTarget[i] = centre + right * (i == 0 ? -0.2f : 0.2f) + Vector3.up * (bob + 0.08f);
						handWeight[i] = hold;
					}
					jaw = pinJaw;
					tiltChance = 0f;
					lean = PinBodyPitch;
					neckFrequency = 22f;
					twitchScale = 0f;
					neckDamping = 0.75f;
				}
				else
				{
					float lift = DeathTimeline.Lift(t, out float shake);
					twitchScale = 0f;
					jaw = 0.25f;
					neckPitch = Mathf.Lerp(15f, -35f, lift);
					lean = Mathf.Clamp01(lift - 0.8f) * -32f;
					neckYawAim = shake * 60f;
					headRollAim = shake * 30f;
					roll = shake * -8f;
					tiltChance = 0f;
					crouch = 0f;
					neckFrequency = 18f;
					neckDamping = 0.6f;
				}
				break;
			}
			case State.Stalk:
			{
				bool still = Time.time < stalkGoAt || stalkArrivedAt >= 0f || speed < 0.2f;
				float sinceFlick = now - flickAt;
				if (sinceFlick < 0.9f)
				{
					glanceYaw = 0f;
					glancePitch = 0f;
				}
				else if (now >= glanceAt)
				{
					float pick = UnityEngine.Random.value;
					bool small = pick < 0.35f;
					glanceAt = now + UnityEngine.Random.Range(small ? 0.8f : 1.1f, small ? 1.6f : 2.6f);
					if (small)
					{
						glanceYaw = UnityEngine.Random.Range(-6f, 6f);
					}
					else
					{
						float swing = UnityEngine.Random.Range(18f, 50f);
						glanceYaw = UnityEngine.Random.value < 0.5f ? -swing : swing;
					}
					glancePitch = UnityEngine.Random.Range(-6f, 9f);
				}
				neckYawAim = Mathf.Clamp(AimYaw(target, 75f) * 0.8f + glanceYaw, -80f, 80f);
				chatterUntil = 0f;
				tiltChance = still ? 0.6f : 0.1f;
				twitchScale = still ? 0.2f : 0.08f;
				neckPitch = glancePitch + 10f;
				bool flicking = sinceFlick < 0.35f;
				neckFrequency = flicking ? 11f : 2.5f;
				neckDamping = flicking ? 0.55f : 0.8f;
				twitchMax = 3f;
				jaw = 0.02f;
				twitchMin = 1.2f;
				lean = 3f;
				crouch = 0.12f;
				break;
			}
			}
			if (Current != State.Grab)
			{
				for (int i = 0; i < 2; i++)
				{
					handWeight[i] = Mathf.MoveTowards(handWeight[i], 0f, step * 4f);
				}
			}
			float surge = Vector3.Dot((velocity - previousVelocity) / step, forward);
			float sharp;
			if (now >= twitchAt)
			{
				float amount = twitchScale * twitchiness;
				twitchAt = now + UnityEngine.Random.Range(twitchMin, twitchMax) * Mathf.Lerp(1.6f, 0.7f, twitchiness);
				twitchYaw = amount * UnityEngine.Random.Range(-30f, 30f);
				twitchPitch = amount * UnityEngine.Random.Range(-10f, 12f);
				sharp = 1f;
				if (Current != State.Wander && Current != State.Stalk && UnityEngine.Random.value < 0.35f)
				{
					sharp = 1.7f;
				}
				twitchSharp = sharp;
			}
			else
			{
				sharp = twitchSharp;
			}
			if (now >= tiltAt)
			{
				tiltAt = now + UnityEngine.Random.Range(0.8f, 2.5f);
				tilt = UnityEngine.Random.value < tiltChance * twitchiness ? UnityEngine.Random.Range(-28f, 28f) : 0f;
			}
			float flatSpeed = velocity.magnitude;
			if (Current == State.Wander && now >= chatterAt)
			{
				chatterAt = now + UnityEngine.Random.Range(3f, 8f);
				chatterUntil = now + UnityEngine.Random.Range(0.3f, 0.6f);
			}
			if (now < chatterUntil && Current != State.Grab)
			{
				jaw = Mathf.Max(jaw, Mathf.Repeat(now * 13f, 1f) < 0.5f ? 0.35f : 0.05f);
			}
			float surgePitch = Mathf.Clamp(surge * -0.25f, -7f, 7f);
			float turnRoll = Mathf.Clamp(Mathf.Clamp01(flatSpeed * 0.5f + 0.3f) * (yawRate * -0.02f), -9f, 9f);
			gallop += step * flatSpeed / Mathf.Max(stride * 0.5f, 0.3f) * Mathf.PI;
			float bounce = Mathf.Clamp01(charge * 2f - 0.3f) * MathF.Sin(gallop);
			float breathing = MathF.Sin(now * 1.7f + index);
			float height = bodyY.Step(breathing * 0.012f - crouch + Mathf.Abs(bounce) * -0.05f, Current == State.Grab ? 8f : 3.2f, 0.35f, step);
			body.localPosition = bodyBase + Vector3.up * height;
			float pitch = bodyPitch.Step(surgePitch + lean + bounce * 5f + jolt * -9f + breathing * 0.8f, Current == State.Grab ? 9f : 3.5f, 0.45f, step);
			float bank = bodyRoll.Step(roll + turnRoll, Current == State.Grab ? 7f : 2.5f, 0.4f, step);
			body.localRotation = bodyBaseRotation * Quaternion.Euler(pitch, 0f, bank);
			float frequency = neckFrequency * sharp;
			float nod = neckPitchS.Step(neckPitch + jolt * -30f + twitchPitch, frequency, neckDamping, step);
			neckYaw = neckYawS.Step(neckYawAim + twitchYaw, frequency, neckDamping, step);
			neck.localRotation = neckBase * Quaternion.Euler(0f, neckYaw, 0f) * Quaternion.Euler(nod - neckRestPitch, 0f, 0f);
			if (head)
			{
				float headTilt = headRoll.Step(Current == State.Grab ? headRollAim : tilt, Current == State.Grab ? 13f : 5f, 0.5f, step);
				head.localRotation = headBase;
				head.rotation = Quaternion.AngleAxis(headTilt, neck.forward) * head.rotation;
			}
			float open = Mathf.Max(jawS.Step(jolt * 0.7f + jaw, 11f, 0.45f, step), -0.05f);
			if (jawFlaps != null && jawOpen != null)
			{
				for (int i = 0; i < jawFlaps.Length && i < jawOpen.Length; i++)
				{
					if (jawFlaps[i])
					{
						jawFlaps[i].localRotation = jawBase[i] * Quaternion.Euler(jawOpen[i] * open);
					}
				}
			}
			if (legs != null)
			{
				AnimateLegs(step, charge, flatSpeed);
			}
			else
			{
				SwingLegs(moved, charge);
			}
		}

		private float AimYaw(Vector3 at, float limit)
		{
			// Rebuilt from native code (token 0x06000581, RVA 0x27DB2D0)
			Vector3 local = transform.InverseTransformPoint(at);
			if (local.sqrMagnitude < 0.01f)
			{
				return 0f;
			}
			return Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -limit, limit);
		}

		private void AnimateLegs(float dt, float pace, float flatSpeed)
		{
			// Rebuilt from native code (token 0x06000582, RVA 0x27DB788)
			float now = Time.time;
			float hurry = Mathf.Min(Mathf.Sqrt(pace), 1f);
			float floor = water ? water.FloorY : transform.position.y;
			float stepTime = Mathf.Lerp(stepTimeSlow, stepTimeFast, hurry);
			Quaternion turnAhead = Quaternion.Euler(0f, yawRate * stepTime * 0.6f, 0f);
			float lead = stepTime * 0.9f + 0.03f;
			int stepping = 0;
			int groupA = 0;
			int groupB = 0;
			for (int i = 0; i < 4; i++)
			{
				Leg leg = legs[i];
				Vector3 home = transform.TransformPoint(leg.home);
				Vector3 root = transform.position;
				Vector3 drift = Vector3.ClampMagnitude(velocity * lead, (leg.upper + leg.lower) * 0.6f);
				Vector3 spot = root + turnAhead * (home - root) + drift;
				want[i] = new Vector3(spot.x, floor, spot.z);
				error[i] = FlatDistance(leg.planted, want[i]) / (leg.upper + leg.lower);
				if (leg.stepping)
				{
					stepping++;
					if (leg.group == 0)
					{
						groupA++;
					}
					else
					{
						groupB++;
					}
				}
			}
			float threshold = Mathf.Lerp(0.3f, 0.45f, pace);
			int allowed = Mathf.Max(0, (pace > 0.22f ? 4 : 2) - stepping);
			for (int started = 0; started < 2; started++)
			{
				float worst = 0f;
				int pick = -1;
				for (int i = 0; i < 4; i++)
				{
					Leg leg = legs[i];
					if (leg.stepping || (i < 2 && handWeight[i] > 0.02f))
					{
						continue;
					}
					if (pace <= 0.22f && (leg.group == 0 ? groupB : groupA) > 0)
					{
						continue;
					}
					bool settle = false;
					if (flatSpeed < 0.3f && error[i] > 0.07f && stepping == 0)
					{
						settle = now > settleAt;
					}
					if ((error[i] > threshold || settle) && error[i] > worst)
					{
						worst = error[i];
						pick = i;
					}
				}
				if (pick < 0 || started == allowed)
				{
					break;
				}
				Leg chosen = legs[pick];
				chosen.stepping = true;
				chosen.t = 0f;
				chosen.from = chosen.planted;
				chosen.duration = stepTime * UnityEngine.Random.Range(0.85f, 1.15f);
				if (chosen.group != 0)
				{
					groupB++;
				}
				else
				{
					groupA++;
				}
				if (flatSpeed < 0.3f)
				{
					settleAt = now + UnityEngine.Random.Range(0.12f, 0.5f);
				}
				stepping++;
			}
			float settleRate = dt / 0.35f;
			float lift = Mathf.Lerp(0.7f, 1.15f, pace);
			for (int i = 0; i < 4; i++)
			{
				Leg leg = legs[i];
				Vector3 foot;
				if (leg.stepping)
				{
					leg.t += dt / Mathf.Max(leg.duration, 0.02f);
					float k = Mathf.Clamp01(leg.t);
					leg.to = want[i];
					float rest = 1f - k;
					float glide = 1f - rest * rest * rest;
					float arc = MathF.Sin(MathF.Pow(k, 0.7f) * Mathf.PI);
					if (leg.t < 1f)
					{
						foot = Vector3.Lerp(leg.from, leg.to, glide);
						foot.y = floor + arc * stepHeight * (leg.upper + leg.lower) * lift;
					}
					else
					{
						foot = new Vector3(leg.to.x, floor, leg.to.z);
						leg.stepping = false;
						leg.planted = foot;
						leg.dig = 1f;
						Landed(leg, pace);
					}
				}
				else
				{
					foot = leg.planted;
				}
				leg.dig = Mathf.MoveTowards(leg.dig, 0f, settleRate);
				if (i < 2 && handWeight[i] > 0.001f)
				{
					if (leg.stepping)
					{
						leg.stepping = false;
						leg.planted = new Vector3(foot.x, floor, foot.z);
					}
					float hold = handWeight[i];
					leg.dig = Mathf.Max(leg.dig, hold);
					foot = Vector3.Lerp(foot, handTarget[i], hold);
				}
				Reach(leg, foot);
			}
			if (claws == null || clawBase == null || clawLegs == null)
			{
				return;
			}
			for (int i = 0; i < claws.Length && i < clawLegs.Length; i++)
			{
				if (!claws[i])
				{
					continue;
				}
				Leg leg = legs[Mathf.Clamp(clawLegs[i], 0, 3)];
				float curl = leg.stepping ? MathF.Sin(leg.t * Mathf.PI) * -0.35f : leg.dig * 0.75f + 0.25f;
				claws[i].localRotation = clawBase[i] * Quaternion.Euler(curl * clawCurl * clawSign[i], 0f, 0f);
			}
		}

		private void Reach(Leg leg, Vector3 target)
		{
			// Rebuilt from native code (token 0x06000583, RVA 0x27DC5EC)
			leg.hip.localRotation = leg.hipRest;
			leg.knee.localRotation = leg.kneeRest;
			Vector3 root = leg.hip.position;
			Vector3 toTarget = target - root;
			float distance = toTarget.magnitude;
			if (distance < 0.0001f)
			{
				return;
			}
			float upper = leg.upper;
			float lower = leg.lower;
			float reach = Mathf.Clamp(distance, upper + 0.001f, (upper + lower) * 0.999f);
			Vector3 dir = toTarget / distance;
			Vector3 pole = transform.TransformDirection(leg.pole);
			Vector3 bend = pole - dir * Vector3.Dot(dir, pole);
			if (bend.sqrMagnitude < 1E-06f)
			{
				bend = transform.forward - dir * Vector3.Dot(dir, transform.forward);
			}
			bend = bend.normalized;
			float along = (upper * upper - lower * lower + reach * reach) / (2f * reach);
			float side = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
			Vector3 kneeAt = root + dir * along + bend * side;
			leg.hip.rotation = Quaternion.FromToRotation(leg.knee.position - root, kneeAt - root) * leg.hip.rotation;
			Vector3 knee = leg.knee.position;
			leg.knee.rotation = Quaternion.FromToRotation(leg.contact.position - knee, root + dir * reach - knee) * leg.knee.rotation;
		}

		private void Landed(Leg leg, float pace)
		{
			// Rebuilt from native code (token 0x06000584, RVA 0x27DC23C)
			bodyY.v -= Mathf.Lerp(0.18f, 0.5f, pace);
			if (!SplashSystem.I || !NearListener())
			{
				return;
			}
			float now = Time.time;
			float surface = water ? water.SurfaceY : transform.position.y;
			Vector3 at = new Vector3(leg.planted.x, surface, leg.planted.z);
			if (water && transform.position.y < water.SurfaceY - 0.1f && water.ContainsXZ(at) && now - lastStepSplash > 0.05f)
			{
				lastStepSplash = now;
				SplashSystem.I.Visual(at, Mathf.Lerp(0.22f, 0.7f, pace));
			}
			if (now - lastStepSound > Mathf.Lerp(0.16f, 0.08f, pace))
			{
				lastStepSound = now;
				SplashSystem.I.PlaySound(Sfx.MonsterStep(), at, Mathf.Lerp(0.45f, 1f, pace), UnityEngine.Random.Range(0.85f, 1.05f));
			}
		}

		private void SwingLegs(float moved, float pace)
		{
			// Rebuilt from native code (token 0x06000585, RVA 0x27DB374)
			float walk = Mathf.Clamp01(speed / 1.5f);
			walkPhase += moved / Mathf.Max(stride, 0.1f) * Mathf.PI;
			int step = Mathf.FloorToInt(walkPhase / Mathf.PI);
			if (step != lastStep)
			{
				lastStep = step;
				FootstepPair((step & 1) == 0, pace);
			}
			float swing = walk * 26f;
			for (int i = 0; i < 4; i++)
			{
				float phase = (i == 0 || i == 3 ? 0f : Mathf.PI) + walkPhase;
				if (hips[i])
				{
					hips[i].localRotation = hipBase[i] * Quaternion.Euler(swing * MathF.Sin(phase), 0f, 0f);
				}
				float lift = Mathf.Max(0f, MathF.Cos(phase));
				if (knees != null && i < knees.Length && knees[i])
				{
					float bend = walk * (lift * 35f);
					knees[i].localRotation = kneeBase[i] * Quaternion.Euler(i < 2 ? -bend : bend, 0f, 0f);
				}
			}
		}

		private void FootstepPair(bool pairA, float pace)
		{
			// Rebuilt from native code (token 0x06000586, RVA 0x27DCC68)
			if (feet == null || feet.Length < 4 || !SplashSystem.I || !NearListener())
			{
				return;
			}
			Transform first = pairA ? feet[0] : feet[1];
			Transform second = pairA ? feet[3] : feet[2];
			bool wet = water && transform.position.y < water.SurfaceY - 0.1f;
			float surface = water ? water.SurfaceY : transform.position.y;
			foreach (Transform foot in new Transform[] { first, second })
			{
				if (!foot)
				{
					continue;
				}
				Vector3 at = new Vector3(foot.position.x, surface, foot.position.z);
				if (wet && water.ContainsXZ(at))
				{
					SplashSystem.I.Visual(at, Mathf.Lerp(0.3f, 0.8f, pace));
				}
			}
			Vector3 centre = (first && second) ? (first.position + second.position) * 0.5f : transform.position;
			SplashSystem.I.PlaySound(Sfx.MonsterStep(), new Vector3(centre.x, surface, centre.z), Mathf.Lerp(0.55f, 1f, pace), UnityEngine.Random.Range(0.85f, 1f));
		}

		private bool NearListener()
		{
			// Rebuilt from native code (token 0x06000587, RVA 0x27DCB50)
			PlayerRig rig = PlayerRig.I;
			if (!rig)
			{
				return true;
			}
			return (rig.head.position - transform.position).sqrMagnitude < 2500f;
		}

		private void Footstep(bool leftFoot, float pace)
		{
			// Rebuilt from native code (token 0x06000588, RVA 0x27DAEC4)
			if (!NearListener())
			{
				return;
			}
			Vector3 at = transform.position + (leftFoot ? -0.45f : 0.45f) * transform.right + transform.forward * 0.3f;
			bool dry = !water || !water.ContainsXZ(at) || !(transform.position.y < water.SurfaceY - 0.1f);
			if (!SplashSystem.I)
			{
				return;
			}
			if (water)
			{
				at.y = water.SurfaceY;
			}
			if (!dry)
			{
				SplashSystem.I.Visual(at, Mathf.Lerp(0.35f, 0.9f, pace));
			}
			SplashSystem.I.PlaySound(Sfx.MonsterStep(), at, Mathf.Lerp(0.55f, 1f, pace), UnityEngine.Random.Range(0.9f, 1.05f));
		}

		private void UpdateDebug(ShallowsTuning tuning)
		{
			// Rebuilt from native code (token 0x06000589, RVA 0x27D45CC)
			bool show = tuning.showDebug;
			if (label)
			{
				if (label.gameObject.activeSelf != show)
				{
					label.gameObject.SetActive(show);
				}
				if (show)
				{
					Color color;
					switch (Current)
					{
					case State.Wander:
						label.text = Time.time < idleUntil ? "idle" : "wandering";
						color = new Color(0.8f, 0.9f, 1f, 1f);
						break;
					case State.Investigate:
						label.text = "HEARD YOU!";
						color = new Color(1f, 0.35f, 0.25f, 1f);
						break;
					case State.Search:
						label.text = string.Format("searching {0:0.0}s", tuning.searchTime - stateTime);
						color = new Color(1f, 0.8f, 0.3f, 1f);
						break;
					case State.Stalk:
						label.text = "listening...";
						color = new Color(0.7f, 0.6f, 1f, 1f);
						break;
					default:
						label.text = "GOT YOU";
						color = new Color(1f, 0f, 0f, 1f);
						break;
					}
					label.color = color;
					if (!viewer)
					{
						viewer = Camera.main;
					}
					if (viewer)
					{
						label.transform.rotation = Quaternion.LookRotation(label.transform.position - viewer.transform.position);
					}
				}
			}
			if (!targetLine)
			{
				return;
			}
			bool showLine = show && (Current == State.Investigate || Current == State.Search || Current == State.Stalk);
			targetLine.enabled = showLine;
			if (!showLine)
			{
				return;
			}
			float y = water ? water.SurfaceY + 0.05f : 0.5f;
			targetLine.SetPosition(0, new Vector3(Mouth.x, y, Mouth.z));
			targetLine.SetPosition(1, new Vector3(target.x, y, target.z));
		}
	}
}
