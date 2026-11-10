using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class WelcomeGift : MonoBehaviour
	{
		private enum State
		{
			Appearing = 0,
			Idle = 1,
			Held = 2,
			Opening = 3,
			Leaving = 4
		}

		private class Watcher : MonoBehaviour
		{
			private float next;

			// Rebuilt from native code (token 0x06000AE3, RVA 0x2766B24)
			private float playingSince = -1f;

			private bool missing;

			private void Update()
			{
				// Rebuilt from native code (token 0x06000AE2, RVA 0x2766764)
				GameManager gm = GameManager.I;
				bool playing;
				if (!gm || !gm.Playing)
				{
					playingSince = -1f;
					playing = false;
				}
				else
				{
					if (playingSince < 0f)
					{
						playingSince = Time.time;
					}
					playing = true;
				}
				if (Time.time < next || current || missing)
				{
					return;
				}
				next = Time.time + 0.5f;
				if (!playing || Time.time < playingSince + 0.8f || !Wallet.WelcomeWaiting || !gm.spawnPoint || !GameObject.Find("Spawn Room"))
				{
					return;
				}
				if (!Resources.Load<GameObject>(Folder + Model))
				{
					missing = true;
					Debug.LogWarning("Shallows: Resources/WelcomeGift/WelcomeGift is missing, so there's no welcome gift. Copy in the welcome-gift export.");
					return;
				}
				current = new GameObject("Welcome Gift").AddComponent<WelcomeGift>();
			}
		}

		public const string Folder = "WelcomeGift/";

		public const string Model = "WelcomeGift";

		// Rebuilt from native code (token 0x06000AE1, RVA 0x276659C)
		private static readonly int[] Pentatonic = new int[5] { 0, 2, 4, 7, 9 };

		private static readonly Color Gold = new Color(1f, 0.78f, 0.28f);

		private static readonly Color CordYellow = new Color(1f, 0.84f, 0.16f);

		private static readonly Color[] ConfettiColours = new Color[8]
		{
			new Color(1f, 0.33f, 0.55f),
			new Color(0.3f, 0.85f, 1f),
			new Color(1f, 0.88f, 0.2f),
			new Color(0.5f, 1f, 0.35f),
			new Color(1f, 0.55f, 0.15f),
			new Color(0.75f, 0.45f, 1f),
			new Color(1f, 1f, 1f),
			new Color(0.95f, 0.15f, 0.15f)
		};

		// Rebuilt from native code (token 0x06000AE0, RVA 0x2766504)
		[Header("Where")]
		[Tooltip("How far in front of you it sits when you spawn (m).")]
		public float distance = 1.4f;

		[Tooltip("Its size (1 = the Blender model, a 44 cm box).")]
		public float size = 1.25f;

		[Header("Waiting")]
		[Tooltip("Seconds between its little jiggles.")]
		public float shakeEvery = 2.4f;

		[Tooltip("How far it tips in a jiggle (degrees).")]
		public float shakeAngle = 7f;

		[Tooltip("How high it hops in a jiggle (m).")]
		public float hop = 0.025f;

		[Header("Pull")]
		[Tooltip("How close a hand has to be to the ring to grab it (m).")]
		public float grabReach = 0.16f;

		[Tooltip("How far you pull the ring to open it (m).")]
		public float openPull = 0.22f;

		[Tooltip("The string won't stretch further than this (m).")]
		public float maxStretch = 0.5f;

		[Tooltip("Seconds it shakes, harder and harder, between the bow coming off and the pop.")]
		public float charge = 0.7f;

		[Header("Bananas")]
		[Tooltip("How long they swirl over you before the first one comes in.")]
		public float hover = 1.1f;

		[Tooltip("Seconds for one banana to reach you from the swirl.")]
		public float flyTime = 0.45f;

		[Tooltip("Length of one flying banana (m).")]
		public float bananaLength = 0.17f;

		private static WelcomeGift current;

		private static Material giftMaterialShared;

		private static Material bananaMaterial;

		private static Material cordMaterial;

		private static Material haloMaterial;

		private static Material softMaterial;

		private static Material pixelMaterial;

		private static Material starMaterial;

		private static Mesh bananaMesh;

		private static bool bananaLooked;

		private State state;

		private Transform wobble;

		private Transform model;

		private Transform lidPivot;

		private Transform bowPivot;

		private Transform ringPivot;

		private Transform handle;

		private Transform knotOut;

		private Transform lidTop;

		private Transform lidEdge;

		private Transform bananaSpawn;

		private Renderer pullString;

		private Renderer bananasPart;

		private Vector3 ringRest;

		private Vector3 centre;

		private Quaternion ringRestTurn;

		private Quaternion bowRestTurn;

		private Vector3 bowRestScale;

		private Vector3 ringRestScale;

		private Material material;

		private LineRenderer cord;

		private Transform halo;

		private Transform beam;

		private Material haloInstance;

		private Material beamInstance;

		private Light glow;

		private ParticleSystem sparks;

		private ParticleSystem confetti;

		private AudioSource voice;

		private AudioSource shimmer;

		private AudioSource pops;

		private AudioSource[] tings;

		private int nextTing;

		private float born;

		private float nextShake;

		private float shakeStart = -10f;

		private float ambient;

		private float appear = 1f;

		private float fadeOut = 1f;

		private Coroutine label;

		private bool boxGone;

		private bool heldLeft;

		private bool stretchSounded;

		private bool nearLeft;

		private bool nearRight;

		private Vector3 grabOffset;

		private Vector3 stretch;

		private Vector3 stretchVelocity;

		private readonly float[] lastGrip = new float[2];

		private static readonly Color LabelYellow = new Color(1f, 0.86f, 0.2f);

		private static readonly Color LabelEdge = new Color(0.16f, 0.09f, 0.02f);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000AA8, RVA 0x275E130)
			current = null;
			starMaterial = null;
			pixelMaterial = null;
			softMaterial = null;
			haloMaterial = null;
			cordMaterial = null;
			bananaMaterial = null;
			giftMaterialShared = null;
			bananaMesh = null;
			bananaLooked = false;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x06000AA9, RVA 0x275E23C)
			if (!FindAnyObjectByType<Watcher>())
			{
				GameObject go = new GameObject("Welcome Gift Watcher");
				go.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(go);
				go.AddComponent<Watcher>();
			}
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000AAA, RVA 0x275E34C)
			Place();
			wobble = new GameObject("Wobble").transform;
			wobble.SetParent(transform, false);
			model = Instantiate(Resources.Load<GameObject>(Folder + Model), wobble, false).transform;
			model.name = "WelcomeGift Model";
			Align(wobble, model);
			foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
			{
				Destroy(c);
			}
			material = new Material(GiftMaterial())
			{
				name = "WelcomeGift (glowing)"
			};
			foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>(true))
			{
				r.sharedMaterial = material;
				r.gameObject.layer = 2;
				r.shadowCastingMode = ShadowCastingMode.Off;
			}
			Transform lid = Find("Lid");
			Transform lidRibbon = Find("Lid Ribbon");
			Transform bow = Find("Bow");
			Transform ring = Find("Pull Ring");
			pullString = Part("Pull String");
			bananasPart = Part("Bananas");
			handle = Find("Pull Handle");
			knotOut = Find("String Knot Out");
			lidTop = Find("String Lid Top");
			lidEdge = Find("String Lid Edge");
			bananaSpawn = Find("Banana Spawn");
			lidPivot = Pivot("Lid Pivot", "Lid Centre", lid, lidRibbon);
			bowPivot = Pivot("Bow Pivot", "String Top", bow);
			ringPivot = Pivot("Ring Pivot", "String Ring Top", ring, handle);
			ringRest = ringPivot.localPosition;
			ringRestTurn = ringPivot.localRotation;
			ringRestScale = ringPivot.localScale;
			bowRestTurn = bowPivot.localRotation;
			bowRestScale = bowPivot.localScale;
			Transform lidCentre = Find("Lid Centre");
			centre = lidCentre ? transform.InverseTransformPoint(lidCentre.position) * size * 0.75f : new Vector3(0f, 0.25f, 0f);
			cord = MakeCord();
			halo = MakeHalo();
			beam = MakeBeam();
			glow = MakeLight();
			sparks = MakeSparks("Sparkles", StarMaterial(), 600);
			confetti = MakeConfetti();
			voice = Source("Voice", 0.9f);
			pops = Source("Pops", 0.8f);
			shimmer = Source("Shimmer", 1f);
			shimmer.loop = true;
			shimmer.clip = Sfx.GiftShimmer();
			shimmer.volume = 0f;
			shimmer.maxDistance = 9f;
			if (shimmer.clip)
			{
				shimmer.Play();
			}
			tings = new AudioSource[8];
			for (int i = 0; i < tings.Length; i++)
			{
				tings[i] = Source("Ting " + i, 0.6f);
			}
			born = Time.time;
			nextShake = born + 1.4f;
			wobble.localScale = Vector3.zero;
			Play(voice, Sfx.GiftAppear(), 0.9f, 1f);
			Spark(transform.TransformPoint(centre), 40, 1.4f, true);
		}

		private void Place()
		{
			// Rebuilt from native code (token 0x06000AAB, RVA 0x275F014)
			Transform spawn = GameManager.I.spawnPoint;
			Vector3 forward = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f) * Vector3.forward;
			Vector3 from = spawn.position;
			PlayerRig rig = PlayerRig.I;
			if (rig && rig.head)
			{
				Vector3 head = rig.head.position;
				head.y = from.y;
				if ((head - from).sqrMagnitude < 2.25f)
				{
					from = head;
				}
			}
			Vector3 at = from + forward * distance;
			at.y = Floor(at, spawn.position.y);
			transform.SetPositionAndRotation(at, Quaternion.LookRotation(-forward));
		}

		private static float Floor(Vector3 at, float near)
		{
			// Rebuilt from native code (token 0x06000AAC, RVA 0x2761578)
			PlayerRig rig = PlayerRig.I;
			RaycastHit[] hits = Physics.RaycastAll(at + Vector3.up * 1.2f, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
			float best = float.MaxValue;
			float y = near;
			foreach (RaycastHit hit in hits)
			{
				if (rig && hit.collider.transform.IsChildOf(rig.transform))
				{
					continue;
				}
				float gap = Mathf.Abs(hit.point.y - near);
				if (gap < best)
				{
					best = gap;
					y = hit.point.y;
				}
			}
			return best < 0.6f ? y : near;
		}

		private Transform Find(string name)
		{
			// Rebuilt from native code (token 0x06000AAD, RVA 0x275F884)
			return LifeguardProps.Find(model, name);
		}

		private Renderer Part(string name)
		{
			// Rebuilt from native code (token 0x06000AAE, RVA 0x275F8F0)
			Transform part = Find(name);
			if (!part)
			{
				return null;
			}
			return part.GetComponent<Renderer>();
		}

		private Transform Pivot(string name, string mark, params Transform[] parts)
		{
			// Rebuilt from native code (token 0x06000AAF, RVA 0x275F9A8)
			Transform at = Find(mark);
			Transform pivot = new GameObject(name).transform;
			pivot.SetParent(wobble, false);
			if (at)
			{
				pivot.position = at.position;
			}
			foreach (Transform part in parts)
			{
				if (part)
				{
					part.SetParent(pivot, true);
				}
			}
			return pivot;
		}

		private static void Align(Transform holder, Transform instance)
		{
			// Rebuilt from native code (token 0x06000AB0, RVA 0x275F378)
			Transform origin = LifeguardProps.Find(instance, "Origin");
			Transform front = LifeguardProps.Find(instance, "Front");
			Transform up = LifeguardProps.Find(instance, "Up");
			Transform right = LifeguardProps.Find(instance, "Right");
			if (!origin || !front || !up)
			{
				return;
			}
			Vector3 o = holder.InverseTransformPoint(origin.position);
			Vector3 f = holder.InverseTransformPoint(front.position);
			Vector3 u = holder.InverseTransformPoint(up.position);
			Quaternion fix = Quaternion.Inverse(Quaternion.LookRotation(f - o, u - o));
			instance.localRotation = fix * instance.localRotation;
			instance.localPosition = fix * instance.localPosition;
			instance.localPosition -= holder.InverseTransformPoint(origin.position);
			if (right && holder.InverseTransformPoint(right.position).x < 0f)
			{
				Vector3 scale = instance.localScale;
				scale.x = -scale.x;
				instance.localScale = scale;
				Vector3 position = instance.localPosition;
				position.x = -position.x;
				instance.localPosition = position;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000AB1, RVA 0x276181C)
			if (state == State.Opening || state == State.Leaving)
			{
				return;
			}
			float time = Time.time;
			float dt = Time.deltaTime;
			if (!Wallet.WelcomeWaiting)
			{
				state = State.Leaving;
				StartCoroutine(Leave());
				return;
			}
			float grow = Mathf.Clamp01((time - born) / 0.8f);
			appear = BackOut(grow);
			if (grow >= 1f && state == State.Appearing)
			{
				state = State.Idle;
			}
			if (state == State.Idle && time >= nextShake)
			{
				shakeStart = time;
				nextShake = time + shakeEvery + Random.Range(-0.4f, 0.5f);
				Play(voice, Sfx.GiftRustle(), 0.55f, Random.Range(0.95f, 1.08f));
				Spark(transform.TransformPoint(centre + Vector3.up * 0.2f), 8, 0.7f, false);
			}
			float jiggle = 0f;
			float lift = 0f;
			float flash = 0f;
			float since = time - shakeStart;
			float u = Mathf.Clamp01(since / 0.6f);
			if (since < 0.6f)
			{
				float a = u * MathF.PI;
				jiggle = (1f - u) * MathF.Sin(a * 6f);
				lift = hop * ((1f - u) * Mathf.Abs(MathF.Sin(a * 3f)));
				flash = 0.35f;
			}
			Vector3 lean = Vector3.right;
			Vector3 ring = wobble.TransformPoint(ringRest);
			float pull = 0f;
			if (state == State.Held)
			{
				pull = Holding(ring, ref lean);
				if (state == State.Opening)
				{
					return;
				}
			}
			else
			{
				stretchVelocity += (stretch * -140f + stretchVelocity * -9f) * dt;
				stretch += stretchVelocity * dt;
				if (stretch.sqrMagnitude < 1E-06f && stretchVelocity.sqrMagnitude < 0.0001f)
				{
					stretch = stretchVelocity = Vector3.zero;
				}
				if (state == State.Idle)
				{
					// TODO(recon): Grabs ignores its argument, so the native call site leaves the value undetermined (token 0x06000AB1)
					Grabs(time);
				}
			}
			float breathe = MathF.Sin(time * 2.2f);
			float pullT = Mathf.Clamp01(pull / openPull);
			float buzz = MathF.Sin(time * 55f) * pullT * 2.5f;
			wobble.localScale = Vector3.one * (size * appear * (1f + breathe * 0.025f) * fadeOut);
			wobble.localPosition = Vector3.up * lift;
			float tilt = jiggle * shakeAngle;
			float spinIn = 1f - grow;
			wobble.localRotation = Quaternion.Euler(buzz + tilt * 0.6f, spinIn * spinIn * 360f, buzz + tilt) * Quaternion.AngleAxis(pullT * 9f, lean);
			float sway = MathF.Sin(time * 1.7f);
			float rock = MathF.Sin(time * 3f);
			bowPivot.localRotation = bowRestTurn * Quaternion.Euler(0f, sway * 6f, rock * 4f + jiggle * 12f);
			float beat = MathF.Sin(time * 4f);
			bowPivot.localScale = bowRestScale * (1f + beat * 0.05f - pullT * 0.08f);
			float pulse = (nearLeft || nearRight || state == State.Held) ? MathF.Sin(time * 10f) * 0.06f + 1.2f : beat * 0.04f + 1f;
			ringPivot.localScale = Vector3.Lerp(ringPivot.localScale, ringRestScale * pulse, dt * 12f);
			ringPivot.position = ring + stretch;
			if (state == State.Held || stretch.sqrMagnitude > 1E-05f)
			{
				ringPivot.localRotation = ringRestTurn;
			}
			else
			{
				ringPivot.localRotation = ringRestTurn * Quaternion.Euler(MathF.Sin(time * 2.5f) * 6f + jiggle * 15f, 0f, MathF.Sin(time * 1.9f) * 8f);
			}
			DrawCord(state == State.Held || stretch.sqrMagnitude > 1E-05f);
			Glow(Mathf.Clamp01(rock * 0.5f + 0.5f) * 0.25f + 0.3f + (1f - u) * flash + pullT * 0.8f, appear * fadeOut);
			ambient += dt * 9f * fadeOut;
			while (ambient >= 1f)
			{
				ambient -= 1f;
				Vector2 spot = Random.insideUnitCircle;
				Vector3 at = transform.TransformPoint(new Vector3(spot.x * 0.35f, Random.Range(0.05f, 0.5f), spot.y * 0.35f) * size);
				Emit(at, Vector3.up * Random.Range(0.15f, 0.4f), Random.Range(0.02f, 0.035f), Random.value < 0.4f ? Color.white : Gold, Random.Range(1f, 1.8f));
			}
			if (shimmer.clip)
			{
				shimmer.volume = Mathf.MoveTowards(shimmer.volume, fadeOut * 0.32f, dt * 0.5f);
			}
		}

		private void Grabs(float t)
		{
			// Rebuilt from native code (token 0x06000AB2, RVA 0x2762BB0)
			PlayerRig rig = PlayerRig.I;
			if (!rig || !handle)
			{
				return;
			}
			for (int i = 0; i < 2; i++)
			{
				bool left = i == 0;
				Transform hand = left ? rig.leftHand : rig.rightHand;
				float grip = XRHands.Grip(left);
				float was = lastGrip[i];
				lastGrip[i] = grip;
				bool free = hand && !HandPresence.Away(left) && !Holding(left);
				float reach = free ? Vector3.Distance(hand.position, handle.position) : 99f;
				bool near = reach < grabReach * 1.6f;
				if (near && !(left ? nearLeft : nearRight))
				{
					XRHands.Haptic(left, 0.2f, 0.03f, true);
				}
				if (left)
				{
					nearLeft = near;
				}
				else
				{
					nearRight = near;
				}
				if (free && was < 0.65f && reach <= grabReach && grip >= 0.65f)
				{
					state = State.Held;
					heldLeft = left;
					stretchSounded = false;
					grabOffset = ringPivot.position - hand.position;
					stretchVelocity = Vector3.zero;
					Play(voice, Sfx.GiftGrab(), 0.7f, 1f);
					XRHands.Haptic(left, 0.5f, 0.06f, true);
					return;
				}
			}
		}

		private static bool Holding(bool left)
		{
			// Rebuilt from native code (token 0x06000AB3, RVA 0x2763734)
			GameManager gm = GameManager.I;
			if (gm && gm.grabbers != null)
			{
				foreach (HandGrabber grabber in gm.grabbers)
				{
					if (grabber && grabber.left == left && grabber.Held)
					{
						return true;
					}
				}
			}
			return false;
		}

		private float Holding(Vector3 rest, ref Vector3 lean)
		{
			// Rebuilt from native code (token 0x06000AB4, RVA 0x27626C4)
			PlayerRig rig = PlayerRig.I;
			Transform hand = rig ? (heldLeft ? rig.leftHand : rig.rightHand) : null;
			float grip = XRHands.Grip(heldLeft);
			lastGrip[heldLeft ? 0 : 1] = grip;
			if (hand && grip >= 0.35f && !HandPresence.Away(heldLeft))
			{
				stretch = Vector3.ClampMagnitude(hand.position + grabOffset - rest, maxStretch);
				float pulled = stretch.magnitude;
				float k = pulled / openPull;
				Vector3 local = transform.InverseTransformDirection(stretch);
				lean = new Vector3(local.z, 0f, -local.x);
				if (lean.sqrMagnitude < 1E-06f)
				{
					lean = Vector3.right;
				}
				XRHands.Haptic(heldLeft, Mathf.Lerp(0.08f, 0.53f, k), 0.03f, false);
				if (pulled > 0.06f && !stretchSounded)
				{
					stretchSounded = true;
					Play(voice, Sfx.GiftStretch(), 0.7f, 1f);
				}
				if (pulled >= openPull)
				{
					StartCoroutine(Open(heldLeft));
				}
				return pulled;
			}
			state = State.Idle;
			Play(voice, Sfx.GiftGrab(), 0.4f, 0.9f);
			return 0f;
		}

		private void DrawCord(bool show)
		{
			// Rebuilt from native code (token 0x06000AB5, RVA 0x27630A0)
			if (pullString)
			{
				pullString.enabled = !show;
			}
			cord.enabled = show;
			if (!show)
			{
				return;
			}
			cord.startWidth = cord.endWidth = size * 0.02f;
			cord.SetPosition(0, (knotOut ? knotOut : bowPivot).position);
			cord.SetPosition(1, (lidTop ? lidTop : bowPivot).position);
			cord.SetPosition(2, (lidEdge ? lidEdge : bowPivot).position);
			cord.SetPosition(3, ringPivot.position);
		}

		private void Glow(float level, float visible)
		{
			// Rebuilt from native code (token 0x06000AB6, RVA 0x27632C4)
			material.SetColor("_EmissionColor", Gold * level);
			if (haloInstance)
			{
				haloInstance.SetColor("_BaseColor", new Color(Gold.r, Gold.g, Gold.b, visible * 0.55f));
				haloInstance.SetFloat("_Size", (level * 0.3f + 0.85f) * (size * 1.3f));
			}
			if (beam)
			{
				beam.localScale = new Vector3(size, appear * fadeOut, size);
			}
			if (beam)
			{
				beam.Rotate(0f, Time.deltaTime * 25f, 0f, Space.Self);
			}
			if (glow)
			{
				glow.intensity = (level * 0.9f + 0.6f) * visible;
			}
		}

		private IEnumerator Open(bool left)
		{
			// Rebuilt from native code (token 0x06000AB7, RVA 0x27638B4; MoveNext token 0x06000B04, RVA 0x27696F0)
			state = State.Opening;
			Wallet.ClaimWelcome();
			nearLeft = false;
			Vector3 mid = transform.TransformPoint(centre);
			Play(voice, Sfx.GiftStringPull(), 1f, 1f);
			XRHands.Haptic(left, 1f, 0.15f, true);
			XRHands.Haptic(!left, 0.4f, 0.1f, true);
			Spark(ringPivot.position, 25, 1.2f, false);
			Spark(bowPivot.position, 30, 1.2f, false);
			cord.enabled = false;
			if (pullString)
			{
				pullString.enabled = false;
			}
			Vector3 away = stretch.sqrMagnitude > 0.0001f ? stretch.normalized : transform.forward;
			StartCoroutine(Fling(ringPivot, away * 1.6f + Vector3.up * 1.8f, Random.onUnitSphere, 720f, 0.9f, 6f));
			StartCoroutine(Fling(bowPivot, Vector3.up * 2.6f + transform.right * Random.Range(-0.6f, 0.6f), Vector3.up, 900f, 1.1f, 4f, 1.5f));
			Play(pops, Sfx.GiftCharge(), 0.9f, 1f);
			float t = 0f;
			while (t < charge)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / charge);
				float amp = Mathf.Lerp(3f, 13f, k);
				wobble.localRotation = Quaternion.Euler(MathF.Sin(t * 47f) * amp * 0.6f, MathF.Sin(t * 31f) * amp * 0.3f, MathF.Sin(t * 61f) * amp);
				wobble.localPosition = Vector3.up * Mathf.Abs(MathF.Sin(t * 38f)) * 0.02f * k;
				wobble.localScale = new Vector3(1f + k * 0.06f, 1f - k * 0.1f, 1f + k * 0.06f) * size;
				Glow(k * 2.2f + 0.8f, 1f);
				XRHands.HapticBoth(k * 0.6f + 0.15f, 0.03f, false);
				if (Random.value < k + 0.5f)
				{
					Spark(mid + Random.insideUnitSphere * 0.25f * size, 2, k + 0.6f, false);
				}
				yield return null;
			}
			Play(pops, Sfx.GiftPop(), 1f, 1f);
			Play(voice, Sfx.GiftConfetti(), 1f, 1f);
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.GiftFanfare(), mid, 0.9f, 1f);
			}
			XRHands.HapticBoth(1f, 0.3f, true);
			Vector3 mouth = bananaSpawn ? bananaSpawn.position : transform.TransformPoint(Vector3.up * 0.35f * size);
			Spark(mouth, 70, 2.2f, true);
			Confetti(mouth, 170);
			Vector3 toYou = YouFrom(mouth);
			StartCoroutine(Fling(lidPivot, Vector3.up * 2.8f - toYou * 0.9f + transform.right * Random.Range(-0.5f, 0.5f), Random.onUnitSphere, 540f, 1.1f, 6f, 0.05f));
			label = StartCoroutine(Label(mouth));
			if (bananasPart)
			{
				bananasPart.enabled = false;
			}
			StartCoroutine(Afterglow());
			Play(voice, Sfx.GiftBananaWhoosh(), 0.9f, 1f);
			// TODO(recon): the banana count's input was folded to the constant 71.109604f by the native compiler; its source expression is not recoverable (token 0x06000B04)
			yield return Bananas(mouth, toYou, Mathf.Clamp(Mathf.RoundToInt(71.109604f), 40, 180));
			Play(pops, Sfx.GiftPoof(), 0.8f, 1f);
			Spark(mid, 50, 1.5f, true);
			boxGone = true;
			yield return Shrink(0.4f);
			while (label != null)
			{
				yield return null;
			}
			current = null;
			Destroy(gameObject);
		}

		private IEnumerator Label(Vector3 top)
		{
			// Rebuilt from native code (token 0x06000AB8, RVA 0x276395C; MoveNext token 0x06000AF8, RVA 0x27688C4)
			Transform root = new GameObject("Welcome Bananas Sign").transform;
			root.SetParent(transform, false);
			Vector3 home = top + Vector3.up * 0.38f;
			root.position = home;
			root.localScale = Vector3.zero;
			List<TextMesh> meshes = new List<TextMesh>();
			List<TextMesh> outlines = new List<TextMesh>();
			LabelLine(root, "+" + Wallet.WelcomeBananas.ToString("N0", CultureInfo.InvariantCulture), 0.085f, 0.045f, meshes, outlines);
			LabelLine(root, "BANANAS", 0.042f, -0.045f, meshes, outlines);
			float t = 0f;
			float popIn = 0.35f;
			float hold = 2.6f;
			float gone = -1f;
			Spark(home, 25, 1f, true);
			while (true)
			{
				t += Time.deltaTime;
				if (gone < 0f && t > popIn + hold && boxGone)
				{
					gone = t;
				}
				float scale = 1f;
				float alpha = 1f;
				if (gone >= 0f)
				{
					float u = Mathf.Clamp01((t - gone) / 0.4f);
					scale = u < 0.25f ? 1f + u * 0.6f : (1f - u) / 0.75f * 1.15f;
					if (u >= 1f)
					{
						break;
					}
					alpha = 1f - u * u;
				}
				else
				{
					float p = Mathf.Clamp01(t / popIn);
					if (p < 1f)
					{
						float a = p * MathF.PI;
						scale = MathF.Sin(a * 0.5f) * (1f + MathF.Sin(a) * 0.25f);
					}
				}
				root.position = home + Vector3.up * (Math.Min(t, 2.5f) * 0.05f + MathF.Sin(t * 2.6f) * 0.012f);
				PlayerRig rig = PlayerRig.I;
				Vector3 look = rig && rig.head ? Vector3.ProjectOnPlane(root.position - rig.head.position, Vector3.up) : -transform.forward;
				if (look.sqrMagnitude > 0.0001f)
				{
					root.rotation = Quaternion.LookRotation(look.normalized) * Quaternion.Euler(0f, 0f, MathF.Sin(t * 1.9f) * 3f);
				}
				root.localScale = Vector3.one * Mathf.Max(0f, scale);
				foreach (TextMesh mesh in meshes)
				{
					mesh.color = new Color(LabelYellow.r, LabelYellow.g, LabelYellow.b, alpha);
				}
				foreach (TextMesh outline in outlines)
				{
					outline.color = new Color(LabelEdge.r, LabelEdge.g, LabelEdge.b, alpha);
				}
				yield return null;
			}
			Spark(root.position, 18, 0.8f, false);
			Destroy(root.gameObject);
			label = null;
		}

		private static void LabelLine(Transform root, string text, float height, float y, List<TextMesh> meshes, List<TextMesh> outlines)
		{
			// Rebuilt from native code (token 0x06000AB9, RVA 0x2763A14)
			TextMesh main = WorldText.Create(text, root, new Vector3(0f, y, 0f), text, height, LabelYellow);
			PixelFonts.Use(main, PixelFonts.Sign, height, 1f);
			meshes.Add(main);
			for (int i = 0; i < 8; i++)
			{
				float a = i * MathF.PI * 0.25f;
				TextMesh edge = WorldText.Create("Outline " + i, main.transform, new Vector3(height * 0.08f * MathF.Cos(a), height * 0.08f * MathF.Sin(a), 0.002f), text, height, LabelEdge);
				PixelFonts.Use(edge, PixelFonts.Sign, height, 1f);
				outlines.Add(edge);
			}
		}

		private IEnumerator Afterglow()
		{
			// Rebuilt from native code (token 0x06000ABA, RVA 0x2763D6C; MoveNext token 0x06000AE6, RVA 0x2766B38)
			float t = 0f;
			while (t < 1.2f)
			{
				if (!this)
				{
					yield break;
				}
				t += Time.deltaTime;
				float u = Mathf.Clamp01(t * 2f);
				float squash = 1f + (1f - u) * MathF.Sin(u * MathF.PI * 3f) * 0.15f;
				float thin = 1f / Mathf.Sqrt(squash);
				wobble.localScale = new Vector3(thin, squash, thin) * size;
				wobble.localRotation = Quaternion.Slerp(wobble.localRotation, Quaternion.identity, Time.deltaTime * 10f);
				wobble.localPosition = Vector3.Lerp(wobble.localPosition, Vector3.zero, Time.deltaTime * 10f);
				Glow(Mathf.Lerp(3.5f, 0.6f, Mathf.Clamp01(t / 1.2f)), 1f);
				if (haloInstance)
				{
					haloInstance.SetFloat("_Size", Mathf.Lerp(2.4f, 1.3f, u) * size);
				}
				yield return null;
			}
		}

		private IEnumerator Fling(Transform part, Vector3 velocity, Vector3 axis, float spin, float life, float gravity, float grow = 1f)
		{
			// Rebuilt from native code (token 0x06000ABB, RVA 0x2763E00; MoveNext token 0x06000AF2, RVA 0x27683E4)
			if (!part)
			{
				yield break;
			}
			part.SetParent(transform, true);
			Vector3 scale = part.localScale;
			float t = 0f;
			while (t < life && part)
			{
				float dt = Time.deltaTime;
				t += dt;
				velocity = (velocity + Vector3.down * gravity * dt) * (1f - dt * 0.8f);
				part.position += velocity * dt;
				part.Rotate(axis, dt * spin, Space.World);
				float k = grow >= 1f ? Mathf.Clamp01(t * 4f) : Mathf.SmoothStep(0f, 1f, t / life);
				part.localScale = scale * Mathf.Lerp(1f, grow, k) * Mathf.Clamp01((life - t) / 0.3f);
				if (Random.value < 0.4f)
				{
					Emit(part.position, Random.insideUnitSphere * 0.3f, 0.03f, Gold, 0.5f);
				}
				yield return null;
			}
			if (part)
			{
				Spark(part.position, 12, 0.8f, false);
				part.gameObject.SetActive(false);
			}
		}

		private IEnumerator Bananas(Vector3 mouth, Vector3 outward, int n)
		{
			// Rebuilt from native code (token 0x06000ABC, RVA 0x2763F0C; MoveNext token 0x06000AEC, RVA 0x2766F1C)
			float busy = Mathf.Clamp01((n - 50f) / 130f);
			float pace = Mathf.Min(60f / n, 1f);
			PlayerRig rig = PlayerRig.I;
			Vector3 side = Vector3.Cross(Vector3.up, outward);
			Vector3 head = rig && rig.head ? rig.head.position : mouth + outward + Vector3.up * 0.5f;
			Vector3 middle = Vector3.Lerp(mouth, head, 0.45f);
			middle.y = Mathf.Max(head.y + 0.35f, mouth.y + 0.8f);
			Transform[] bits = new Transform[n];
			Vector3[] vel = new Vector3[n];
			float[] radius = new float[n];
			float[] angle = new float[n];
			float[] lift = new float[n];
			float[] leave = new float[n];
			Vector3[] flyFrom = new Vector3[n];
			Quaternion[] spin = new Quaternion[n];
			Vector3[] spinRate = new Vector3[n];
			float launch = Mathf.Lerp(0.5f, 0.85f, busy);
			float at = launch + hover;
			float ring = Mathf.Lerp(0.32f, 0.5f, busy);
			for (int i = 0; i < n; i++)
			{
				radius[i] = ring * Mathf.Sqrt((i + 0.5f) / n) + 0.12f;
				angle[i] = i * 137.5f;
				lift[i] = Random.Range(-0.1f, 0.1f);
				spin[i] = Random.rotation;
				spinRate[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)) * 260f;
				leave[i] = at;
				at += pace * Mathf.Lerp(0.08f, 0.022f, (float)i / n);
			}
			for (int i = n - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				float swap = leave[i];
				leave[i] = leave[j];
				leave[j] = swap;
			}
			float end = at + flyTime + 0.1f;
			float t = 0f;
			int launched = 0;
			int arrived = 0;
			while (arrived < n && t < end + 1f)
			{
				float dt = Time.deltaTime;
				t += dt;
				if (rig && rig.head)
				{
					head = rig.head.position;
				}
				Vector3 lean = rig && rig.head ? Vector3.ProjectOnPlane(rig.head.forward, Vector3.up).normalized * 0.05f : Vector3.zero;
				Quaternion swirl = Quaternion.Euler(0f, t * 75f, 0f);
				while (launched < n && t >= launch * launched / n)
				{
					int i = launched++;
					bits[i] = MakeBanana(mouth + Random.insideUnitSphere * 0.06f);
					vel[i] = outward * Random.Range(0.4f, 1.4f) + Vector3.up * Random.Range(2.8f, 4.2f) + side * Random.Range(-1.2f, 1.2f);
				}
				Vector3 catchAt = head + Vector3.down * 0.22f + lean;
				for (int i = 0; i < launched; i++)
				{
					Transform bit = bits[i];
					if (!bit)
					{
						continue;
					}
					bit.rotation = Quaternion.Euler(spinRate[i] * t) * spin[i];
					if (t < leave[i])
					{
						float a = angle[i] * Mathf.Deg2Rad;
						Vector3 orbit = new Vector3(MathF.Cos(a) * radius[i], MathF.Sin(t * 3f + i) * 0.04f + lift[i], MathF.Sin(a) * radius[i]);
						Vector3 goal = middle + swirl * orbit;
						vel[i] += ((goal - bit.position) * 36f + vel[i] * -6f) * dt;
						bit.position += vel[i] * dt;
						bit.localScale = Vector3.one * (1f + MathF.Sin(i * 1.7f + t * 9f) * 0.1f);
						flyFrom[i] = bit.position;
						continue;
					}
					float u = Mathf.Clamp01((t - leave[i]) / flyTime);
					if (u >= 1f)
					{
						Destroy(bit.gameObject);
						bits[i] = null;
						Collected(arrived++, n, catchAt);
					}
					else
					{
						bit.position = Vector3.Lerp(flyFrom[i], catchAt, u * u) + Vector3.up * (MathF.Sin(u * MathF.PI) * 0.14f);
						bit.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.45f, u * u);
					}
				}
				if (launched < n)
				{
					Emit(mouth + Random.insideUnitSphere * 0.08f, Vector3.up * Random.Range(0.8f, 1.6f) + Random.insideUnitSphere * 0.4f, 0.04f, Gold, 0.7f);
				}
				yield return null;
			}
			foreach (Transform bit in bits)
			{
				if (bit)
				{
					Destroy(bit.gameObject);
				}
			}
		}

		private void Collected(int i, int n, Vector3 at)
		{
			// Rebuilt from native code (token 0x06000ABD, RVA 0x2763FEC)
			Spark(at, 4, 0.6f, false);
			AudioSource ting = tings[nextTing];
			nextTing = (nextTing + 1) % tings.Length;
			ting.transform.position = at;
			AudioClip clip = Sfx.GiftBananaCollect();
			float k = (float)i / Mathf.Max(n - 1, 1);
			ting.pitch = Mathf.Pow(2f, Pentatonic[Mathf.Min(Pentatonic.Length - 1, (int)(k * Pentatonic.Length))] / 12f);
			if (clip)
			{
				ting.PlayOneShot(clip, Mathf.Lerp(0.25f, 0.36f, k));
			}
			XRHands.Haptic(i % 2 == 0, Mathf.Lerp(0.15f, 0.55f, k), 0.03f, true);
			if (i == n - 1)
			{
				Spark(at, 40, 1.8f, true);
				XRHands.HapticBoth(0.9f, 0.35f, true);
			}
		}

		private Transform MakeBanana(Vector3 at)
		{
			// Rebuilt from native code (token 0x06000ABE, RVA 0x27642F8)
			GameObject go = new GameObject("Welcome Banana");
			go.layer = 2;
			go.transform.SetParent(transform, true);
			go.transform.position = at;
			Mesh mesh = BananaMesh();
			GameObject part = mesh ? new GameObject("Mesh") : GameObject.CreatePrimitive(PrimitiveType.Capsule);
			part.layer = 2;
			Collider collider = part.GetComponent<Collider>();
			if (collider)
			{
				Destroy(collider);
			}
			part.transform.SetParent(go.transform, false);
			if (mesh)
			{
				part.AddComponent<MeshFilter>().sharedMesh = mesh;
				part.AddComponent<MeshRenderer>();
				Vector3 extent = mesh.bounds.size;
				float longest = Mathf.Max(0.0001f, Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z)));
				float scale = bananaLength / longest;
				part.transform.localScale = Vector3.one * scale;
				part.transform.localPosition = -mesh.bounds.center * scale;
			}
			else
			{
				part.transform.localScale = new Vector3(0.04f, bananaLength * 0.5f, 0.04f);
			}
			MeshRenderer renderer = part.GetComponent<MeshRenderer>();
			renderer.sharedMaterial = BananaMaterial();
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			TrailRenderer trail = go.AddComponent<TrailRenderer>();
			trail.sharedMaterial = SoftMaterial();
			trail.time = 0.22f;
			trail.minVertexDistance = 0.02f;
			trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.045f), new Keyframe(1f, 0f));
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
				new GradientColorKey(Gold, 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(0.75f, 0f),
				new GradientAlphaKey(0f, 1f)
			});
			trail.colorGradient = gradient;
			trail.shadowCastingMode = ShadowCastingMode.Off;
			return go.transform;
		}

		private IEnumerator Shrink(float seconds)
		{
			// Rebuilt from native code (token 0x06000ABF, RVA 0x2764E28; MoveNext token 0x06000B0A, RVA 0x276A8B4)
			float t = 0f;
			Vector3 from = wobble.localScale;
			while (t < seconds)
			{
				t += Time.deltaTime;
				float u = Mathf.Clamp01(t / seconds);
				fadeOut = 1f - u;
				float scale = u < 0.25f ? 1f + u * 0.6f : (1f - u) / 0.75f * 1.15f;
				wobble.localScale = scale * from;
				wobble.Rotate(0f, u * (Time.deltaTime * 500f), 0f, Space.Self);
				Glow(fadeOut * 0.6f, fadeOut);
				if (shimmer)
				{
					shimmer.volume = fadeOut * 0.32f;
				}
				yield return null;
			}
			if (wobble)
			{
				wobble.gameObject.SetActive(false);
			}
		}

		private IEnumerator Leave()
		{
			// Rebuilt from native code (token 0x06000AC0, RVA 0x2762614; MoveNext token 0x06000AFE, RVA 0x2769518)
			Spark(transform.TransformPoint(centre), 30, 1f, true);
			yield return Shrink(0.4f);
			if (current == this)
			{
				current = null;
			}
			Destroy(gameObject);
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000AC1, RVA 0x2764EF4)
			if (current == this)
			{
				current = null;
			}
			if (material)
			{
				Destroy(material);
			}
			if (haloInstance)
			{
				Destroy(haloInstance);
			}
			if (beamInstance)
			{
				Destroy(beamInstance);
			}
		}

		private Vector3 YouFrom(Vector3 at)
		{
			// Rebuilt from native code (token 0x06000AC2, RVA 0x2765090)
			PlayerRig rig = PlayerRig.I;
			Vector3 flat = rig && rig.head ? Vector3.ProjectOnPlane(rig.head.position - at, Vector3.up) : Vector3.zero;
			if (flat.sqrMagnitude < 0.0001f)
			{
				flat = transform.forward;
			}
			return flat.normalized;
		}

		private LineRenderer MakeCord()
		{
			// Rebuilt from native code (token 0x06000AC3, RVA 0x275FB24)
			GameObject go = new GameObject("Pulled String");
			go.transform.SetParent(transform, false);
			LineRenderer line = go.AddComponent<LineRenderer>();
			line.useWorldSpace = true;
			line.positionCount = 4;
			line.numCornerVertices = 2;
			line.numCapVertices = 2;
			line.sharedMaterial = CordMaterial();
			line.shadowCastingMode = ShadowCastingMode.Off;
			line.enabled = false;
			return line;
		}

		private Transform MakeHalo()
		{
			// Rebuilt from native code (token 0x06000AC4, RVA 0x275FC94)
			Material shared = HaloMaterial();
			if (!shared)
			{
				return null;
			}
			GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			Destroy(quad.GetComponent<Collider>());
			quad.name = "Halo";
			quad.layer = 2;
			quad.transform.SetParent(transform, false);
			quad.transform.localPosition = centre;
			MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
			haloInstance = new Material(shared);
			renderer.sharedMaterial = haloInstance;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			return quad.transform;
		}

		private Transform MakeBeam()
		{
			// Rebuilt from native code (token 0x06000AC5, RVA 0x275FEB0)
			Mesh mesh = new Mesh();
			mesh.name = "Welcome Gift Beam";
			Vector3[] vertices = new Vector3[42];
			Color[] colors = new Color[vertices.Length];
			Vector2[] uv = new Vector2[vertices.Length];
			int[] triangles = new int[120];
			for (int i = 0; i <= 20; i++)
			{
				float a = i * MathF.PI * 2f / 20f;
				float x = MathF.Cos(a) * 0.3f;
				float z = MathF.Sin(a) * 0.3f;
				vertices[i * 2] = new Vector3(x, 0f, z);
				vertices[i * 2 + 1] = new Vector3(x * 0.75f, 2.6f, z * 0.75f);
				colors[i * 2] = new Color(1f, 1f, 1f, (i / 2) % 2 == 0 ? 0.32f : 0.144f);
				colors[i * 2 + 1] = new Color(1f, 1f, 1f, 0f);
				uv[i * 2] = new Vector2(i / 20f, 0f);
				uv[i * 2 + 1] = new Vector2(i / 20f, 1f);
			}
			for (int i = 0; i < 20; i++)
			{
				int v = i * 2;
				int k = i * 6;
				triangles[k] = v;
				triangles[k + 1] = v + 1;
				triangles[k + 2] = v + 2;
				triangles[k + 3] = v + 2;
				triangles[k + 4] = v + 1;
				triangles[k + 5] = v + 3;
			}
			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.uv = uv;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
			GameObject go = new GameObject("Beam");
			go.layer = 2;
			go.transform.SetParent(transform, false);
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer renderer = go.AddComponent<MeshRenderer>();
			beamInstance = new Material(SoftMaterial())
			{
				name = "Welcome Gift Beam"
			};
			if (beamInstance.HasProperty("_Color"))
			{
				beamInstance.SetColor("_Color", Gold);
			}
			renderer.sharedMaterial = beamInstance;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			go.transform.localScale = Vector3.zero;
			return go.transform;
		}

		private Light MakeLight()
		{
			// Rebuilt from native code (token 0x06000AC6, RVA 0x2760484)
			GameObject go = new GameObject("Glow");
			go.layer = 2;
			go.transform.SetParent(transform, false);
			go.transform.localPosition = centre + Vector3.up * 0.35f;
			Light light = go.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = Gold;
			light.range = size * 1.8f;
			light.intensity = 0f;
			light.shadows = LightShadows.None;
			return light;
		}

		private ParticleSystem MakeSparks(string name, Material mat, int max)
		{
			// Rebuilt from native code (token 0x06000AC7, RVA 0x2760970)
			GameObject go = new GameObject(name);
			go.transform.SetParent(transform, false);
			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = system.main;
			main.playOnAwake = false;
			main.loop = false;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = max;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
			main.gravityModifier = -0.05f;
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = false;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
			ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
			limit.enabled = true;
			limit.drag = 4f;
			ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
			renderer.sharedMaterial = mat;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			system.Play();
			return system;
		}

		private ParticleSystem MakeConfetti()
		{
			// Rebuilt from native code (token 0x06000AC8, RVA 0x2760D7C)
			ParticleSystem system = MakeSparks("Confetti", PixelMaterial(), 400);
			ParticleSystem.MainModule main = system.main;
			main.gravityModifier = 0.35f;
			ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
			limit.drag = 2.2f;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));
			ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
			rotation.enabled = true;
			rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
			return system;
		}

		private void Confetti(Vector3 at, int count)
		{
			// Rebuilt from native code (token 0x06000AC9, RVA 0x27656FC)
			if (!confetti)
			{
				return;
			}
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
			{
				applyShapeToPosition = false
			};
			for (int i = 0; i < count; i++)
			{
				Vector3 dir = Random.insideUnitSphere;
				emit.position = at + Random.insideUnitSphere * 0.05f;
				emit.velocity = new Vector3(dir.x, Mathf.Abs(dir.y) * 1.6f + 0.6f, dir.z).normalized * Random.Range(1.8f, 4.2f);
				emit.startColor = ConfettiColours[Random.Range(0, ConfettiColours.Length)];
				emit.startSize = Random.Range(0.025f, 0.045f);
				emit.startLifetime = Random.Range(1.6f, 2.8f);
				emit.rotation = Random.Range(0f, 360f);
				confetti.Emit(emit, 1);
			}
		}

		private void Spark(Vector3 at, int count, float speed, bool big)
		{
			// Rebuilt from native code (token 0x06000ACA, RVA 0x27612B0)
			for (int i = 0; i < count; i++)
			{
				Vector3 dir = Random.insideUnitSphere.normalized;
				float pace = Random.Range(0.3f, 1f);
				float startSize = big ? Random.Range(0.035f, 0.07f) : Random.Range(0.025f, 0.05f);
				Color colour = Random.value < 0.5f ? Color.white : Color.Lerp(Gold, new Color(1f, 0.95f, 0.6f), Random.value);
				Emit(at, dir * pace * speed, startSize, colour, Random.Range(0.35f, 0.75f));
			}
		}

		private void Emit(Vector3 at, Vector3 velocity, float startSize, Color colour, float life)
		{
			// Rebuilt from native code (token 0x06000ACB, RVA 0x2763570)
			if (!sparks)
			{
				return;
			}
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
			{
				applyShapeToPosition = false,
				position = at,
				velocity = velocity,
				startSize = startSize,
				startColor = colour,
				startLifetime = life,
				rotation = Random.Range(0f, 90f)
			};
			sparks.Emit(emit, 1);
		}

		private AudioSource Source(string name, float spatial)
		{
			// Rebuilt from native code (token 0x06000ACC, RVA 0x2761064)
			GameObject go = new GameObject(name);
			go.transform.SetParent(transform, false);
			go.transform.localPosition = centre;
			AudioSource source = go.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = spatial;
			source.minDistance = 1.5f;
			source.maxDistance = 25f;
			source.rolloffMode = AudioRolloffMode.Linear;
			return source;
		}

		private static void Play(AudioSource source, AudioClip clip, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x06000ACD, RVA 0x27611A8)
			if (source && clip)
			{
				source.pitch = Sfx.PitchFor(clip, pitch);
				source.PlayOneShot(clip, volume);
			}
		}

		private static float BackOut(float u)
		{
			// Rebuilt from native code (token 0x06000ACE, RVA 0x2762680)
			u = Mathf.Clamp01(u) - 1f;
			return u * u * (u * 2.7f + 1.7f) + 1f;
		}

		private static Material Saved(string name)
		{
			// Rebuilt from native code (token 0x06000ACF, RVA 0x2765A88)
			return Resources.Load<Material>(Folder + name);
		}

		private static Material GiftMaterial()
		{
			// Rebuilt from native code (token 0x06000AD0, RVA 0x275F764)
			if (!giftMaterialShared)
			{
				giftMaterialShared = Saved("WelcomeGift Material") ?? NewGiftMaterial();
			}
			return giftMaterialShared;
		}

		private static Material BananaMaterial()
		{
			// Rebuilt from native code (token 0x06000AD1, RVA 0x2764BE8)
			if (!bananaMaterial)
			{
				bananaMaterial = Saved("WelcomeGift Banana") ?? NewBananaMaterial();
			}
			return bananaMaterial;
		}

		private static Material CordMaterial()
		{
			// Rebuilt from native code (token 0x06000AD2, RVA 0x276539C)
			if (!cordMaterial)
			{
				cordMaterial = Saved("WelcomeGift Cord") ?? NewCordMaterial();
			}
			return cordMaterial;
		}

		private static Material HaloMaterial()
		{
			// Rebuilt from native code (token 0x06000AD3, RVA 0x27654BC)
			if (!haloMaterial)
			{
				haloMaterial = Saved("WelcomeGift Halo") ?? NewHaloMaterial();
			}
			return haloMaterial;
		}

		private static Material SoftMaterial()
		{
			// Rebuilt from native code (token 0x06000AD4, RVA 0x2764D08)
			if (!softMaterial)
			{
				softMaterial = Saved("WelcomeGift Soft") ?? NewSoftMaterial();
			}
			return softMaterial;
		}

		private static Material PixelMaterial()
		{
			// Rebuilt from native code (token 0x06000AD5, RVA 0x27655DC)
			if (!pixelMaterial)
			{
				pixelMaterial = Saved("WelcomeGift Pixel") ?? NewPixelMaterial();
			}
			return pixelMaterial;
		}

		private static Material StarMaterial()
		{
			// Rebuilt from native code (token 0x06000AD6, RVA 0x2760688)
			if (!starMaterial)
			{
				starMaterial = new Material(PixelMaterial())
				{
					name = "WelcomeGift Star"
				};
				Texture2D star = new Texture2D(7, 7, TextureFormat.RGBA32, false);
				star.filterMode = FilterMode.Point;
				star.wrapMode = TextureWrapMode.Clamp;
				for (int y = 0; y < 7; y++)
				{
					for (int x = 0; x < 7; x++)
					{
						int dx = Mathf.Abs(x - 3);
						int dy = Mathf.Abs(y - 3);
						float a = ((dx == 0 && dy < 4) || (dy == 0 && dx < 4) || (dx + dy <= 1 && dx <= 1 && dy <= 1)) ? 1f : 0f;
						star.SetPixel(x, y, new Color(a, a, a, a));
					}
				}
				star.Apply();
				starMaterial.mainTexture = star;
			}
			return starMaterial;
		}

		private static Mesh BananaMesh()
		{
			// Rebuilt from native code (token 0x06000AD7, RVA 0x2764A50)
			if (!bananaLooked)
			{
				bananaLooked = true;
				GameObject prefab = Resources.Load<GameObject>("BananaHill/Banana");
				MeshFilter filter = prefab ? prefab.GetComponentInChildren<MeshFilter>(true) : null;
				bananaMesh = filter ? filter.sharedMesh : null;
			}
			return bananaMesh;
		}

		private static Material Lit(string name)
		{
			// Rebuilt from native code (token 0x06000AD8, RVA 0x27662DC)
			Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			m.name = name;
			m.SetColor("_BaseColor", Color.white);
			m.SetFloat("_Metallic", 0f);
			return m;
		}

		private static void Emissive(Material m, Texture map, Color colour)
		{
			// Rebuilt from native code (token 0x06000AD9, RVA 0x27663DC)
			if (map)
			{
				m.SetTexture("_EmissionMap", map);
			}
			m.SetColor("_EmissionColor", colour);
			m.EnableKeyword("_EMISSION");
			m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
		}

		public static Material NewGiftMaterial()
		{
			// Rebuilt from native code (token 0x06000ADA, RVA 0x2765AF8)
			Texture2D albedo = Resources.Load<Texture2D>("WelcomeGift/WelcomeGift_Albedo");
			Material m = Lit("WelcomeGift Material");
			m.SetTexture("_BaseMap", albedo);
			m.mainTexture = albedo;
			m.SetFloat("_Smoothness", 0.3f);
			Emissive(m, albedo, Gold * 0.4f);
			return m;
		}

		public static Material NewBananaMaterial()
		{
			// Rebuilt from native code (token 0x06000ADB, RVA 0x2765C48)
			Texture2D albedo = Resources.Load<Texture2D>("BananaHill/Banana");
			Material m = Lit("WelcomeGift Banana");
			m.SetTexture("_BaseMap", albedo);
			m.mainTexture = albedo;
			m.SetFloat("_Smoothness", 0.45f);
			Emissive(m, albedo, new Color(0.62f, 0.5f, 0.12f) * 1.3f);
			return m;
		}

		public static Material NewCordMaterial()
		{
			// Rebuilt from native code (token 0x06000ADC, RVA 0x2765D90)
			Material m = Lit("WelcomeGift Cord");
			m.SetColor("_BaseColor", CordYellow);
			m.SetFloat("_Smoothness", 0.2f);
			Emissive(m, null, CordYellow * 0.35f);
			return m;
		}

		public static Material NewHaloMaterial()
		{
			// Rebuilt from native code (token 0x06000ADD, RVA 0x2765E98)
			Shader shader = Shader.Find("Shallows/Halo");
			if (!shader)
			{
				return null;
			}
			Material m = new Material(shader);
			m.name = "WelcomeGift Halo";
			m.SetColor("_BaseColor", new Color(Gold.r, Gold.g, Gold.b, 0.55f));
			m.SetFloat("_Size", 1.3f);
			m.SetFloat("_Steps", 5f);
			m.SetFloat("_Pulse", 0.12f);
			return m;
		}

		public static Material NewSoftMaterial()
		{
			// Rebuilt from native code (token 0x06000ADE, RVA 0x276605C)
			Shader shader = Shader.Find("Shallows/Particle");
			Material m = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Particles/Unlit"));
			m.name = "WelcomeGift Soft";
			if (m.HasProperty("_Cutoff"))
			{
				m.SetFloat("_Cutoff", 0f);
			}
			return m;
		}

		public static Material NewPixelMaterial()
		{
			// Rebuilt from native code (token 0x06000ADF, RVA 0x276619C)
			Shader shader = Shader.Find("Shallows/Particle");
			Material m = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Particles/Unlit"));
			m.name = "WelcomeGift Pixel";
			if (m.HasProperty("_Cutoff"))
			{
				m.SetFloat("_Cutoff", 0.5f);
			}
			return m;
		}
	}
}
