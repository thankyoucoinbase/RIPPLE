using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class PearlVendingMachine : MonoBehaviour
	{
		private struct Pose
		{
			public Vector3 position;

			public Quaternion rotation;
		}

		public const string Model = "PearlVendingMachine";

		private const string Folder = "Lifeguard/";

		private const string Rows = "ABCD";

		private const int Columns = 4;

		// Rebuilt from native code (token 0x06000933, RVA 0x27338F8)
		public static readonly Vector3 InShop = new Vector3(2.9f, 0f, 0.87f);

		private static readonly Color ScreenGreen = new Color(0.55f, 1f, 0.6f, 1f);

		private static readonly Color ScreenYellow = new Color(1f, 0.87f, 0.35f, 1f);

		private static readonly int[] Pentatonic = new int[5] { 0, 2, 4, 7, 9 };

		[Header("Drop")]
		[Tooltip("Seconds for a coil to turn once.")]
		public float coilTurn = 1.3f;

		[Tooltip("How far the machine jolts when the bag lands (m).")]
		public float shake = 0.012f;

		[Header("Pearls")]
		[Tooltip("Pearls that fly out (each one dings).")]
		public int pearls = 50;

		[Tooltip("How long they swirl in the air before the first one comes to you.")]
		public float hover = 1.1f;

		[Tooltip("Seconds for one pearl to reach you from the swirl.")]
		public float flyTime = 0.45f;

		private static Material glassMaterial;

		private static Material lampMaterial;

		private static Material pearlMaterial;

		private static Material sparkMaterial;

		private static Material trailMaterial;

		private static PearlVendingMachine instance;

		private Transform holder;

		private Transform model;

		private TextMesh screen;

		private Transform bag;

		private Transform flap;

		private Transform[] coils = new Transform[16];

		private Vector3[] slots = new Vector3[16];

		private Vector3[] axes = new Vector3[16];

		private Pose bagRest;

		private Pose flapRest;

		private Pose[] coilRest = new Pose[16];

		private Vector3 bagCentre;

		private Vector3 hinge;

		private Vector3 binSpot;

		private Vector3 pearlOut;

		private Renderer pearlPart;

		private ParticleSystem sparks;

		private AudioSource motor;

		private AudioSource thing;

		private AudioSource[] tings;

		private int nextTing;

		private string typed = "";

		private bool busy;

		private bool ready;

		private int shownSlot = -1;

		private string message;

		private float messageUntil;

		private float nextRefresh;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600090B, RVA 0x272E938)
			trailMaterial = null;
			sparkMaterial = null;
			pearlMaterial = null;
			lampMaterial = null;
			glassMaterial = null;
		}

		public static bool EnsureIn(RippleShop shop)
		{
			// Rebuilt from native code (token 0x0600090C, RVA 0x272E9E4)
			if (!shop || FindAnyObjectByType<PearlVendingMachine>(FindObjectsInactive.Include))
			{
				return false;
			}
			GameObject go = new GameObject("Pearl Vending Machine");
			go.transform.SetParent(shop.transform, false);
			go.transform.localPosition = InShop;
			go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
			go.AddComponent<PearlVendingMachine>().Dress();
			return true;
		}

		public bool Dress()
		{
			// Rebuilt from native code (token 0x0600090D, RVA 0x272EBD4)
			bool dressed = transform.Find(Model);
			Transform root = LifeguardProps.Dress(transform, Model, Vector3.zero, Quaternion.identity);
			if (!root)
			{
				return false;
			}
			foreach (MeshRenderer part in root.GetComponentsInChildren<MeshRenderer>(true))
			{
				if (part.name == "Glass")
				{
					part.sharedMaterial = GlassMaterial();
					part.shadowCastingMode = ShadowCastingMode.Off;
					if (!part.GetComponent<BoxCollider>())
					{
						part.gameObject.AddComponent<BoxCollider>();
					}
				}
				else if (part.name == "Lamp")
				{
					part.sharedMaterial = LampMaterial();
					part.shadowCastingMode = ShadowCastingMode.Off;
				}
				else if (part.name == "Pearl")
				{
					part.enabled = false;
				}
			}
			return !dressed;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600090E, RVA 0x272F3BC)
			instance = this;
			Dress();
			holder = transform.Find(Model);
			model = holder ? holder.Find("PearlVendingMachine Model") : null;
			if (!model)
			{
				return;
			}
			for (int row = 0; row < Rows.Length; row++)
			{
				for (int column = 0; column < Columns; column++)
				{
					int slot = row * Columns + column;
					string code = Code(slot);
					coils[slot] = LifeguardProps.Find(model, "Coil " + code);
					if (coils[slot])
					{
						coilRest[slot] = Rest(coils[slot]);
					}
					slots[slot] = Local(LifeguardProps.Find(model, "Slot " + code));
					axes[slot] = Local(LifeguardProps.Find(model, "Axis " + code));
				}
			}
			bag = LifeguardProps.Find(model, "Bag");
			if (bag)
			{
				bagRest = Rest(bag);
				bagCentre = holder.InverseTransformPoint(bag.GetComponent<Renderer>().bounds.center);
			}
			flap = LifeguardProps.Find(model, "Flap");
			if (flap)
			{
				flapRest = Rest(flap);
			}
			hinge = Local(LifeguardProps.Find(model, "Flap Hinge"));
			binSpot = Local(LifeguardProps.Find(model, "Bin"));
			pearlOut = Local(LifeguardProps.Find(model, "Pearl Out"));
			Transform pearl = LifeguardProps.Find(model, "Pearl");
			pearlPart = pearl ? pearl.GetComponent<Renderer>() : null;
			foreach (char k in "ABCD1234")
			{
				Key(k.ToString(), () => Type(k));
			}
			Key("CLR", Clear);
			screen = LifeguardProps.Text(transform, model, "Screen Text", "Screen Text", PixelFonts.Screen, 0.026f, ScreenGreen, TextAnchor.MiddleCenter, 1.05f);
			screen.transform.SetParent(holder, true);
			motor = Source("Motor", 1f);
			motor.loop = true;
			thing = Source("Machine", 1f);
			tings = new AudioSource[8];
			for (int i = 0; i < tings.Length; i++)
			{
				tings[i] = Source("Ting " + i, 0.6f);
			}
			sparks = MakeSparks();
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600090F, RVA 0x2730928)
			Wallet.Changed += Refresh;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000910, RVA 0x27309CC)
			Wallet.Changed -= Refresh;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000911, RVA 0x2730A70)
			Refresh();
		}

		private void Key(string label, Action press)
		{
			// Rebuilt from native code (token 0x06000912, RVA 0x27301EC)
			Transform part = LifeguardProps.Find(model, "Key " + label);
			if (!part)
			{
				return;
			}
			PokeButton button = LifeguardProps.Button(part, holder.TransformVector(Vector3.forward * 0.01f));
			button.onPress = press;
			button.repeat = false;
		}

		private AudioSource Source(string name, float spatial)
		{
			// Rebuilt from native code (token 0x06000913, RVA 0x27303B0)
			GameObject go = new GameObject(name);
			go.transform.SetParent(holder, false);
			go.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
			AudioSource source = go.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = spatial;
			source.minDistance = 1.5f;
			source.maxDistance = 25f;
			source.rolloffMode = AudioRolloffMode.Linear;
			return source;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000914, RVA 0x2730C34)
			if (!model)
			{
				return;
			}
			if (Time.unscaledTime >= nextRefresh)
			{
				Refresh();
			}
			if (!screen)
			{
				return;
			}
			string cursor = typed.Length < 2 ? (Time.unscaledTime % 1f < 0.5f ? "_" : " ") : "";
			string prompt = "> " + typed + cursor;
			if (messageUntil > Time.time || (busy && message != null))
			{
				screen.text = message;
			}
			else if (ready)
			{
				screen.text = "FIND THE BAG\nTYPE ITS CODE\n\n" + prompt;
			}
			else
			{
				TimeSpan wait = Wallet.UntilNextDaily;
				screen.text = string.Format("SOLD OUT\nNEW BAG IN\n{0:00}:{1:00}:{2:00}\n", (int)wait.TotalHours, wait.Minutes, wait.Seconds) + prompt;
			}
			screen.color = messageUntil > Time.time || busy ? ScreenYellow : ScreenGreen;
		}

		private void Refresh()
		{
			// Rebuilt from native code (token 0x06000915, RVA 0x2730A74)
			nextRefresh = Time.unscaledTime + 1f;
			if (busy || !bag)
			{
				return;
			}
			ready = Wallet.DailyReady;
			int slot = Wallet.DailySlot(Wallet.Today);
			if (ready != bag.gameObject.activeSelf)
			{
				bag.gameObject.SetActive(ready);
			}
			if (slot != shownSlot)
			{
				shownSlot = slot;
				PlaceBag(slots[slot], Quaternion.identity);
			}
		}

		private void Say(string text, float seconds)
		{
			// Rebuilt from native code (token 0x06000916, RVA 0x27310C8)
			message = text;
			messageUntil = Time.time + seconds;
		}

		private void Type(char k)
		{
			// Rebuilt from native code (token 0x06000917, RVA 0x2731100)
			if (busy)
			{
				return;
			}
			if (Rows.IndexOf(k) >= 0)
			{
				typed = k.ToString();
				return;
			}
			if (typed.Length != 1)
			{
				typed = "";
				Say("LETTER FIRST\nTHEN NUMBER", 1.5f);
				return;
			}
			typed += k.ToString();
			int row = Rows.IndexOf(typed[0]);
			StartCoroutine(Vend(row * Columns + (k - '1')));
		}

		private void Clear()
		{
			// Rebuilt from native code (token 0x06000918, RVA 0x2731330)
			if (!busy)
			{
				typed = "";
			}
		}

		private IEnumerator Vend(int slot)
		{
			// Rebuilt from native code (token 0x06000919, RVA 0x27312B4; MoveNext token 0x06000945, RVA 0x2735438)
			busy = true;
			string code = Code(slot);
			message = code + "\nVENDING...";
			messageUntil = 0f;
			bool hasBag = ready && slot == shownSlot && bag && bag.gameObject.activeSelf;
			string why = "";
			bool answered = !hasBag;
			bool ok = false;
			if (hasBag)
			{
				// Rebuilt from native code (token 0x0600093C, RVA 0x2733F90)
				Wallet.ClaimDaily(slot, (yes, text) =>
				{
					why = text;
					answered = true;
					ok = yes;
				});
			}
			motor.clip = Sfx.VendMotor();
			motor.pitch = 1f;
			motor.volume = 0.5f;
			motor.Play();
			float t = 0f;
			float waited = 0f;
			while (true)
			{
				if (t / coilTurn >= 0.85f)
				{
					if (!answered && waited < 8f)
					{
						waited += Time.deltaTime;
						motor.volume = 0.25f;
						yield return null;
						continue;
					}
					if (hasBag && !ok)
					{
						break;
					}
				}
				motor.volume = 0.5f;
				t += Time.deltaTime;
				float u = Mathf.Min(t / coilTurn, 1f);
				TurnCoil(slot, u * 360f);
				if (hasBag)
				{
					PlaceBag(slots[slot] + Vector3.back * (u * 0.07f) + Wobble(u), Quaternion.identity);
				}
				if (u >= 1f)
				{
					break;
				}
				yield return null;
			}
			motor.Stop();
			if (!answered)
			{
				why = "NO SIGNAL\nTRY AGAIN";
			}
			if (!hasBag)
			{
				TurnCoil(slot, 0f);
			}
			else if (!ok)
			{
				for (float b = 0f; b < 0.4f; b += Time.deltaTime)
				{
					float back = 1f - b / 0.4f;
					TurnCoil(slot, back * 306f);
					PlaceBag(slots[slot] + Vector3.back * (back * 0.0595f), Quaternion.identity);
					yield return null;
				}
				TurnCoil(slot, 0f);
				PlaceBag(slots[slot], Quaternion.identity);
			}
			else
			{
				TurnCoil(slot, 0f);
				message = "ENJOY!";
				Vector3 from = slots[slot] + Vector3.back * 0.07f;
				float gap = from.z - 0.013f;
				for (float b = 0f; b < 0.42f; b += Time.deltaTime)
				{
					float k = b / 0.42f;
					float y = Mathf.Lerp(from.y, binSpot.y, k * k);
					float z = k < 0.75f ? Mathf.Lerp(from.z, gap, Mathf.SmoothStep(0f, 1f, k * 4f)) : Mathf.Lerp(gap, binSpot.z, (k - 0.75f) * 4f);
					float x = Mathf.Lerp(from.x, binSpot.x, Mathf.SmoothStep(0f, 1f, (k - 0.5f) * 2f));
					PlaceBag(new Vector3(x, y, z), Quaternion.Euler(Mathf.Lerp(0f, -270f, Mathf.Sqrt(k)), 0f, MathF.Sin(k * 7f) * 12f));
					yield return null;
				}
				PlaceBag(binSpot, Quaternion.Euler(-270f, 0f, MathF.Sin(7f) * 12f));
				Play(thing, Sfx.VendDrop(), 1f, 1.1f);
				XRHands.HapticBoth(0.6f, 0.14f);
				Vector3 rest = holder.localPosition;
				Quaternion restTurn = holder.localRotation;
				for (float b = 0f; b < 0.55f; b += Time.deltaTime)
				{
					float fade = 1f - b / 0.55f;
					fade *= fade;
					float s = b * 55f;
					float sway = MathF.Sin(s * 0.7f) * shake * 0.5f;
					float hop = Mathf.Abs(MathF.Sin(s * 1.3f)) * shake * 0.4f;
					float slide = MathF.Sin(s) * shake;
					holder.localPosition = rest + new Vector3(slide, hop, sway) * fade;
					holder.localRotation = restTurn * Quaternion.Euler(MathF.Sin(s * 0.9f) * 1.2f * fade, 0f, MathF.Sin(s * 1.1f) * 2f * fade);
					yield return null;
				}
				holder.localPosition = rest;
				holder.localRotation = restTurn;
				yield return new WaitForSeconds(0.25f);
				Play(thing, Sfx.VendFlap(), 0.8f, 1f);
				for (float b = 0f; b < 0.35f; b += Time.deltaTime)
				{
					SetFlap(BackOut(b / 0.35f) * 105f);
					yield return null;
				}
				SetFlap(105f);
				bag.gameObject.SetActive(false);
				Spark(holder.TransformPoint(binSpot), 18, 0.5f);
				message = string.Format("+{0} PEARLS!", Wallet.DailyPearls);
				yield return Burst(holder.TransformPoint(pearlOut), -holder.forward, holder.right, pearls, false);
				message = string.Format("+{0} PEARLS!\n\nYOU HAVE\n{1}", Wallet.DailyPearls, Wallet.Pearls);
				for (float b = 0f; b < 0.4f; b += Time.deltaTime)
				{
					SetFlap((1f - Mathf.SmoothStep(0f, 1f, b / 0.4f)) * 105f);
					yield return null;
				}
				SetFlap(0f);
				Play(thing, Sfx.VendFlap(), 0.4f, 1.15f);
				typed = "";
				busy = false;
				shownSlot = -1;
				messageUntil = Time.time + 4f;
				Refresh();
				yield break;
			}
			Play(thing, Sfx.VendEmpty(), 0.7f, 1f);
			string say;
			if (hasBag)
			{
				say = why == "EMPTY" ? code + "\nIS EMPTY" : why;
			}
			else if (ready)
			{
				say = code + "\nIS EMPTY";
			}
			else
			{
				say = "SOLD OUT\nCOME BACK\nTOMORROW";
			}
			typed = "";
			busy = false;
			message = say;
			shownSlot = -1;
			messageUntil = Time.time + 2.2f;
			Refresh();
		}

		private void Wallet_Refresh()
		{
			// Rebuilt from native code (token 0x0600091A, RVA 0x27313B8)
			shownSlot = -1;
			Refresh();
		}

		public static float Shower(Vector3 at, int amount)
		{
			// Rebuilt from native code (token 0x0600091B, RVA 0x27313C4)
			if (amount < 1)
			{
				return 0f;
			}
			PearlVendingMachine machine = instance ? instance : FindAnyObjectByType<PearlVendingMachine>();
			if (!machine || !machine.isActiveAndEnabled || machine.tings == null)
			{
				return 0f;
			}
			int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(amount) * 1.3f + 30f), 40, 180);
			PlayerRig rig = PlayerRig.I;
			Vector3 outward = rig && rig.head ? Vector3.ProjectOnPlane(rig.head.position - at, Vector3.up) : Vector3.zero;
			if (outward.sqrMagnitude < 0.0001f)
			{
				outward = Vector3.forward;
			}
			outward = outward.normalized;
			machine.StartCoroutine(machine.Burst(at, outward, Vector3.Cross(Vector3.up, outward), count, true));
			return machine.Length(count);
		}

		private float Length(int n)
		{
			// Rebuilt from native code (token 0x0600091C, RVA 0x2731984)
			float total = Mathf.Lerp(0.55f, 0.9f, (n - 50f) / 130f) + hover;
			float pace = Mathf.Min(60f / n, 1f);
			for (int i = 0; i < n; i++)
			{
				total += Mathf.Lerp(0.08f, 0.022f, (float)i / n) * pace;
			}
			return total + flyTime;
		}

		private IEnumerator Burst(Vector3 mouth, Vector3 outward, Vector3 side, int count, bool loud)
		{
			// Rebuilt from native code (token 0x0600091D, RVA 0x273189C; MoveNext token 0x0600093F, RVA 0x2733FB0)
			int n = Mathf.Max(count, 1);
			float spread = Mathf.Clamp01((n - 50f) / 130f);
			float pace = Mathf.Min(60f / n, 1f);
			PlayerRig rig = PlayerRig.I;
			Vector3 head = rig && rig.head ? rig.head.position : mouth + outward + Vector3.up * 0.5f;
			Vector3 centre = mouth + (head - mouth) * 0.45f;
			centre.y = Mathf.Max(head.y + 0.35f, mouth.y + 0.7f);
			Transform[] bits = new Transform[n];
			Vector3[] vel = new Vector3[n];
			float[] radius = new float[n];
			float[] angle = new float[n];
			float[] lift = new float[n];
			float[] leave = new float[n];
			Vector3[] flyFrom = new Vector3[n];
			Quaternion[] spin = new Quaternion[n];
			Vector3 scale = pearlPart ? pearlPart.transform.lossyScale : Vector3.one;
			float launch = Mathf.Lerp(0.55f, 0.9f, spread);
			float leaveAt = launch + hover;
			float swirl = Mathf.Lerp(0.3f, 0.5f, spread);
			for (int i = 0; i < n; i++)
			{
				radius[i] = swirl * Mathf.Sqrt((i + 0.5f) / n) + 0.1f;
				angle[i] = i * 137.5f;
				lift[i] = Random.Range(-0.08f, 0.08f);
				spin[i] = Random.rotation;
				leave[i] = leaveAt;
				leaveAt += pace * Mathf.Lerp(0.08f, 0.022f, (float)i / n);
			}
			for (int i = n - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				float swap = leave[i];
				leave[i] = leave[j];
				leave[j] = swap;
			}
			float end = leaveAt + flyTime + 0.1f;
			if (loud && SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.PearlBurst(), mouth, 0.9f, 1f);
			}
			else
			{
				Play(thing, Sfx.PearlBurst(), 0.9f, 1f);
			}
			XRHands.HapticBoth(0.35f, 0.25f);
			Spark(mouth, loud ? 45 : 25, 1.2f);
			int launched = 0;
			int arrived = 0;
			float t = 0f;
			while (arrived < n && t < end + 1f)
			{
				float dt = Time.deltaTime;
				t += dt;
				if (rig && rig.head)
				{
					head = rig.head.position;
				}
				Vector3 ahead = rig && rig.head ? Vector3.ProjectOnPlane(rig.head.forward, Vector3.up).normalized * 0.05f : Vector3.zero;
				Quaternion turn = Quaternion.Euler(0f, t * 75f, 0f);
				while (launched < n && t >= launch * launched / n)
				{
					int i = launched++;
					bits[i] = MakePearl(mouth + Random.insideUnitSphere * 0.04f, scale);
					vel[i] = outward * Random.Range(0.6f, 1.6f) + Vector3.up * Random.Range(2.4f, 3.8f) + side * Random.Range(-1f, 1f);
				}
				Vector3 target = head + Vector3.down * 0.22f + ahead;
				for (int i = 0; i < launched; i++)
				{
					Transform bit = bits[i];
					if (!bit)
					{
						continue;
					}
					bit.rotation = Quaternion.Euler(0f, t * 200f, t * 90f) * spin[i];
					if (t < leave[i])
					{
						float a = angle[i] * Mathf.Deg2Rad;
						Vector3 offset = turn * new Vector3(MathF.Cos(a) * radius[i], MathF.Sin(t * 3f + i) * 0.03f + lift[i], MathF.Sin(a) * radius[i]);
						vel[i] += ((centre + offset - bit.position) * 38f + vel[i] * -6.5f) * dt;
						bit.position += vel[i] * dt;
						bit.localScale = scale * (MathF.Sin(i * 1.7f + t * 9f) * 0.12f + 1f);
						flyFrom[i] = bit.position;
						continue;
					}
					float k = Mathf.Clamp01((t - leave[i]) / flyTime);
					if (k >= 1f)
					{
						Destroy(bit.gameObject);
						bits[i] = null;
						Collected(arrived++, n, target, loud);
						continue;
					}
					bit.position = Vector3.Lerp(flyFrom[i], target, k * k) + Vector3.up * (MathF.Sin(k * Mathf.PI) * 0.12f);
					bit.localScale = Mathf.Lerp(1.1f, 0.5f, k * k) * scale;
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

		private void Collected(int i, int n, Vector3 at, bool loud)
		{
			// Rebuilt from native code (token 0x0600091E, RVA 0x2731AC0)
			Spark(at, 4, 0.6f);
			AudioSource ting = tings[nextTing];
			nextTing = (nextTing + 1) % tings.Length;
			ting.transform.position = at;
			float u = (float)i / Mathf.Max(n - 1, 1);
			ting.pitch = Mathf.Pow(2f, Pentatonic[Mathf.Min(Pentatonic.Length - 1, (int)(u * Pentatonic.Length))] / 12f);
			ting.PlayOneShot(Sfx.PearlCollect(), Mathf.Lerp(0.22f, 0.32f, u));
			XRHands.Haptic(i % 2 == 0, Mathf.Lerp(0.15f, 0.55f, u), 0.03f);
			if (i != n - 1)
			{
				return;
			}
			Spark(at, 30, 1.6f);
			if (loud && SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.PearlsDone(), at, 1f, 1f);
			}
			else
			{
				Play(thing, Sfx.PearlsDone(), 1f, 1f);
			}
			XRHands.HapticBoth(0.9f, 0.3f);
		}

		private Transform MakePearl(Vector3 at, Vector3 scale)
		{
			// Rebuilt from native code (token 0x0600091F, RVA 0x2732348)
			GameObject go = new GameObject("Pearl");
			go.layer = 2;
			go.transform.position = at;
			go.transform.localScale = scale;
			Mesh mesh = pearlPart ? pearlPart.GetComponent<MeshFilter>().sharedMesh : null;
			if (mesh)
			{
				go.AddComponent<MeshFilter>().sharedMesh = mesh;
			}
			else
			{
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				go.AddComponent<MeshFilter>().sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
				Destroy(sphere);
				go.transform.localScale = Vector3.one * 0.04f;
			}
			MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = PearlMaterial();
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			TrailRenderer trail = go.AddComponent<TrailRenderer>();
			trail.sharedMaterial = TrailMaterial();
			trail.time = 0.2f;
			trail.minVertexDistance = 0.02f;
			trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.03f), new Keyframe(1f, 0f));
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(Color.white, 0f),
				new GradientColorKey(new Color(1f, 0.75f, 0.9f, 1f), 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(0.7f, 0f),
				new GradientAlphaKey(0f, 1f)
			});
			trail.colorGradient = gradient;
			trail.shadowCastingMode = ShadowCastingMode.Off;
			return go.transform;
		}

		private ParticleSystem MakeSparks()
		{
			// Rebuilt from native code (token 0x06000920, RVA 0x27304EC)
			GameObject go = new GameObject("Sparkles");
			go.transform.SetParent(transform, false);
			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = system.main;
			main.playOnAwake = false;
			main.loop = false;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 400;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
			main.gravityModifier = -0.05f;
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = false;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = system.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
			ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = system.limitVelocityOverLifetime;
			limitVelocity.enabled = true;
			limitVelocity.drag = 4f;
			ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
			particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
			particleRenderer.sharedMaterial = SparkMaterial();
			particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
			system.Play();
			return system;
		}

		private void Spark(Vector3 at, int count, float speed)
		{
			// Rebuilt from native code (token 0x06000921, RVA 0x2731F00)
			if (!sparks)
			{
				return;
			}
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
			for (int i = 0; i < count; i++)
			{
				emit.position = at;
				emit.velocity = Random.insideUnitSphere.normalized * Random.Range(0.3f, 1f) * speed;
				emit.startColor = Random.value < 0.5f ? Color.white : Color.Lerp(new Color(1f, 0.8f, 0.92f, 1f), new Color(0.8f, 0.92f, 1f, 1f), Random.value);
				emit.rotation = Random.Range(0f, 90f);
				sparks.Emit(emit, 1);
			}
		}

		private Pose Rest(Transform part)
		{
			// Rebuilt from native code (token 0x06000922, RVA 0x272FFBC)
			return new Pose
			{
				position = holder.InverseTransformPoint(part.position),
				rotation = Quaternion.Inverse(holder.rotation) * part.rotation
			};
		}

		private Vector3 Local(Transform mark)
		{
			// Rebuilt from native code (token 0x06000923, RVA 0x273010C)
			if (!mark)
			{
				return Vector3.zero;
			}
			return holder.InverseTransformPoint(mark.position);
		}

		private void Set(Transform part, Pose rest, Vector3 pivot, Quaternion turn, Vector3 move)
		{
			// Rebuilt from native code (token 0x06000924, RVA 0x273309C)
			part.position = holder.TransformPoint(pivot + turn * (rest.position - pivot) + move);
			part.rotation = holder.rotation * turn * rest.rotation;
		}

		private void PlaceBag(Vector3 centre, Quaternion turn)
		{
			// Rebuilt from native code (token 0x06000925, RVA 0x2730FD4)
			if (bag)
			{
				Set(bag, bagRest, bagCentre, turn, centre - bagCentre);
			}
		}

		private void TurnCoil(int slot, float degrees)
		{
			// Rebuilt from native code (token 0x06000926, RVA 0x273329C)
			if (coils[slot])
			{
				Set(coils[slot], coilRest[slot], axes[slot], Quaternion.AngleAxis(-degrees, Vector3.forward), Vector3.zero);
			}
		}

		private void SetFlap(float degrees)
		{
			// Rebuilt from native code (token 0x06000927, RVA 0x273349C)
			if (flap)
			{
				Set(flap, flapRest, hinge, Quaternion.AngleAxis(degrees, Vector3.right), Vector3.zero);
			}
		}

		private static Vector3 Wobble(float u)
		{
			// Rebuilt from native code (token 0x06000928, RVA 0x2733614)
			return new Vector3(MathF.Sin(u * 40f) * 0.002f, 0f, 0f);
		}

		private static float BackOut(float u)
		{
			// Rebuilt from native code (token 0x06000929, RVA 0x2733644)
			float c = Mathf.Clamp01(u) - 1f;
			return c * c * (c * 2.7f + 1.7f) + 1f;
		}

		private static string Code(int slot)
		{
			// Rebuilt from native code (token 0x0600092A, RVA 0x272FEE0)
			return string.Format("{0}{1}", Rows[slot / Columns], slot % Columns + 1);
		}

		private static void Play(AudioSource source, AudioClip clip, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x0600092B, RVA 0x2732240)
			if (source && clip)
			{
				source.pitch = Sfx.PitchFor(clip, pitch);
				source.PlayOneShot(clip, volume);
			}
		}

		private static Material GlassMaterial()
		{
			// Rebuilt from native code (token 0x0600092C, RVA 0x272F014; lambda token 0x06000936, RVA 0x2733A44)
			if (!glassMaterial)
			{
				glassMaterial = Saved("PearlVendingMachine Glass", "Universal Render Pipeline/Lit", m =>
				{
					m.SetFloat("_Surface", 1f);
					m.SetFloat("_Blend", 0f);
					m.SetFloat("_SrcBlend", 5f);
					m.SetFloat("_DstBlend", 10f);
					m.SetFloat("_SrcBlendAlpha", 1f);
					m.SetFloat("_DstBlendAlpha", 10f);
					m.SetFloat("_ZWrite", 0f);
					m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
					m.SetOverrideTag("RenderType", "Transparent");
					m.renderQueue = 3000;
					m.SetColor("_BaseColor", new Color(0.78f, 0.93f, 1f, 0.14f));
					m.SetFloat("_Smoothness", 0.92f);
					m.SetFloat("_Metallic", 0f);
				});
			}
			return glassMaterial;
		}

		private static Material LampMaterial()
		{
			// Rebuilt from native code (token 0x0600092D, RVA 0x272F1E8; lambda token 0x06000937, RVA 0x2733CB0)
			if (!lampMaterial)
			{
				lampMaterial = Saved("PearlVendingMachine Lamp", "Universal Render Pipeline/Unlit", m =>
				{
					Texture2D albedo = Resources.Load<Texture2D>("Lifeguard/PearlVendingMachine_Albedo");
					m.SetTexture("_BaseMap", albedo);
					m.SetColor("_BaseColor", Color.white);
					m.mainTexture = albedo;
				});
			}
			return lampMaterial;
		}

		private static Material PearlMaterial()
		{
			// Rebuilt from native code (token 0x0600092E, RVA 0x2732984; lambda token 0x06000938, RVA 0x2733D8C)
			if (!pearlMaterial)
			{
				pearlMaterial = Saved("PearlVendingMachine Pearl", "Universal Render Pipeline/Lit", m =>
				{
					Texture2D albedo = Resources.Load<Texture2D>("Lifeguard/PearlVendingMachine_Albedo");
					m.SetTexture("_BaseMap", albedo);
					m.mainTexture = albedo;
					m.SetColor("_BaseColor", Color.white);
					m.SetFloat("_Smoothness", 0.85f);
					m.SetFloat("_Metallic", 0.1f);
					m.SetTexture("_EmissionMap", albedo);
					m.SetColor("_EmissionColor", new Color(0.45f, 0.43f, 0.47f, 1f));
					m.EnableKeyword("_EMISSION");
					m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
				});
			}
			return pearlMaterial;
		}

		private static Material SparkMaterial()
		{
			// Rebuilt from native code (token 0x0600092F, RVA 0x2732CF0)
			if (sparkMaterial)
			{
				return sparkMaterial;
			}
			Shader shader = Shader.Find("Shallows/Particle");
			if (!shader)
			{
				shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			}
			sparkMaterial = new Material(shader) { name = "Pearl Sparkle" };
			Texture2D texture = new Texture2D(7, 7, TextureFormat.RGBA32, false);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			for (int y = 0; y < 7; y++)
			{
				int dy = Mathf.Abs(y - 3);
				for (int x = 0; x < 7; x++)
				{
					int dx = Mathf.Abs(x - 3);
					float a = (x == 3 && dy < 4) || (y == 3 && dx < 4) || (dx + dy <= 1 && dx <= 1 && dy <= 1) ? 1f : 0f;
					texture.SetPixel(x, y, new Color(a, a, a, a));
				}
			}
			texture.Apply();
			sparkMaterial.mainTexture = texture;
			if (sparkMaterial.HasProperty("_Cutoff"))
			{
				sparkMaterial.SetFloat("_Cutoff", 0.5f);
			}
			return sparkMaterial;
		}

		private static Material TrailMaterial()
		{
			// Rebuilt from native code (token 0x06000930, RVA 0x2732B58)
			if (!trailMaterial)
			{
				Shader shader = Shader.Find("Shallows/Particle");
				if (!shader)
				{
					shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
				}
				trailMaterial = new Material(shader) { name = "Pearl Trail" };
			}
			return trailMaterial;
		}

		private static Material Saved(string name, string shader, Action<Material> setup)
		{
			// Rebuilt from native code (token 0x06000931, RVA 0x2733688)
			Material saved = Resources.Load<Material>(Folder + name);
			if (saved)
			{
				return saved;
			}
			Material material = new Material(Shader.Find(shader)) { name = name };
			setup(material);
			return material;
		}
	}
}
