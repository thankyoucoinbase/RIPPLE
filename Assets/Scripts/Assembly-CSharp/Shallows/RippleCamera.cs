using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shallows
{
	[DefaultExecutionOrder(-170)]
	public class RippleCamera : MonoBehaviour
	{
		private enum Action
		{
			FovDown = 0,
			FovUp = 1,
			SmoothDown = 2,
			SmoothUp = 3,
			Mode = 4,
			Record = 5,
			Flip = 6
		}

		private class Key
		{
			public Action action;

			public Transform frame;

			public Vector2 size;

			public Transform part;

			public Vector3 rest;

			public Vector3 push;

			public float pressedUntil;

			public bool Contains(Vector3 p, float margin)
			{
				// Rebuilt from native code (token 0x06000056, RVA 0x270C338)
				return Mathf.Abs(p.x) <= size.x * 0.5f + margin && Mathf.Abs(p.y) <= size.y * 0.5f + margin;
			}
		}

		private class Finger
		{
			public Vector3 previous;

			public bool ready;

			public Key held;
		}

		public const string DockModel = "RippleCameraDock";

		public const string CameraModel = "RippleCamera";

		private const string TabletName = "Ripple Camera";

		public const int ToolLayer = 29;

		private const int IgnoreRaycast = 2;

		private const string FovPref = "shallows.camera.fov";

		private const string SmoothingPref = "shallows.camera.smoothing";

		public const int MinFov = 40;

		public const int MaxFov = 110;

		public const int DefaultFov = 65;

		public const int FovStep = 5;

		public const int MaxSmoothing = 10;

		public const int DefaultSmoothing = 3;

		private static readonly Vector3 GrabMin = new Vector3(-0.315f, -0.22f, -0.035f);

		private static readonly Vector3 GrabMax = new Vector3(0.315f, 0.22f, 0.04f);

		private const float GrabReach = 0.08f;

		private const float GripOn = 0.65f;

		private const float GripOff = 0.35f;

		private static readonly Color Cream = new Color(0.93333334f, 0.9019608f, 0.8156863f, 1f);

		private static readonly Color Paint = new Color(0.5882353f, 0.101960786f, 0.08627451f, 1f);

		private static readonly Color Ink = new Color(0.3529412f, 0.23529412f, 0.15686275f, 1f);

		private static readonly Color Green = new Color(0.47058824f, 1f, 0.54901963f, 1f);

		private static readonly Color RecRed = new Color(1f, 0.25f, 0.2f, 1f);

		public static int ProbeRender;

		private Transform tablet;

		private Transform home;

		private Transform lens;

		private Renderer screen;

		private Renderer recLight;

		private Material screenMaterial;

		private Material recMaterial;

		private TextMesh modeText;

		private TextMesh statusText;

		private TextMesh fovText;

		private TextMesh smoothText;

		private TextMesh modeKeyText;

		private TextMesh recordKeyText;

		private TextMesh flipKeyText;

		private Key[] keys = new Key[0];

		private readonly bool[] gripping = new bool[2];

		private Vector3 grabPosition;

		private Quaternion grabRotation;

		private float nextFrame;

		private bool viewReady;

		private bool built;

		private Renderer[] shown = new Renderer[0];

		private Vector3 tabletScale;

		private float popStart = -1f;

		private const float PopTime = 0.18f;

		private AudioSource sounds;

		private AudioClip recordStart;

		private AudioClip recordStop;

		private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();

		private readonly Finger[] fingers = new Finger[2]
		{
			new Finger(),
			new Finger()
		};

		public static RippleCamera Local { get; private set; }

		public int FieldOfView { get; private set; } = DefaultFov;

		public int Smoothing { get; private set; } = DefaultSmoothing;

		public bool FirstPerson { get; private set; }

		public bool Flipped { get; private set; }

		public bool FacingOut
		{
			get
			{
				// Rebuilt from native code (token 0x0600000D, RVA 0x2704BD4)
				return Flipped && !FirstPerson;
			}
		}

		public int HeldHand { get; private set; } = -1;

		public Camera PreviewCamera { get; private set; }

		public RenderTexture Preview { get; private set; }

		public RippleCameraRecorder Recorder { get; private set; }

		public RippleCameraDesktop Desktop { get; private set; }

		public bool DesktopFeed
		{
			get
			{
				// Rebuilt from native code (token 0x06000018, RVA 0x2704C44)
				return Desktop && Desktop.isActiveAndEnabled && Desktop.Active;
			}
		}

		public bool Capturing
		{
			get
			{
				// Rebuilt from native code (token 0x06000019, RVA 0x2704D70)
				return Recorder && Recorder.State == RippleCameraRecorder.Status.Recording;
			}
		}

		public bool Hidden { get; private set; }

		public bool Docked { get; private set; } = true;

		public bool Out
		{
			get
			{
				// Rebuilt from native code (token 0x0600001E, RVA 0x2704E24)
				return built && !Hidden && !Docked;
			}
		}

		private bool FullPicture
		{
			get
			{
				// Rebuilt from native code (token 0x0600001F, RVA 0x2704E4C)
				return Capturing || ProbeRender == 2;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000020, RVA 0x2704EC4)
			Local = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void EnsureOne()
		{
			// Rebuilt from native code (token 0x06000021, RVA 0x2704F50)
			if (FindAnyObjectByType<RippleCamera>(FindObjectsInactive.Include))
			{
				return;
			}
			GameManager game = FindAnyObjectByType<GameManager>();
			if (game)
			{
				Place(game);
			}
		}

		public static RippleCamera Place(GameManager game)
		{
			// Rebuilt from native code (token 0x06000022, RVA 0x2705038)
			Bounds zone = game.safeZone;
			LobbySpeaker speaker = FindAnyObjectByType<LobbySpeaker>(FindObjectsInactive.Include);
			float x;
			float z;
			if (speaker)
			{
				Vector3 near = speaker.transform.position;
				x = near.x + 2.3f;
				z = near.z - 0.15f;
			}
			else
			{
				x = zone.center.x + 2.5f;
				z = zone.max.z - 0.6f;
			}
			x = Mathf.Clamp(x, zone.min.x + 0.6f, zone.max.x - 0.6f);
			z = Mathf.Clamp(z, zone.min.z + 0.6f, zone.max.z - 0.6f);
			float y;
			if (Physics.Raycast(new Vector3(x, zone.center.y + 1f, z), Vector3.down, out RaycastHit hit, 6f, ~(1 << IgnoreRaycast), QueryTriggerInteraction.Ignore))
			{
				y = hit.point.y;
			}
			else
			{
				y = speaker ? speaker.transform.position.y : zone.min.y;
			}
			GameObject dock = new GameObject("Ripple Camera Dock");
			dock.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.identity);
			RippleCamera camera = dock.AddComponent<RippleCamera>();
			camera.Dress();
			return camera;
		}

		public bool Dress()
		{
			// Rebuilt from native code (token 0x06000023, RVA 0x2705374)
			int before = CountChildren();
			Transform body = transform.Find(TabletName);
			if (!body)
			{
				body = new GameObject(TabletName).transform;
				body.SetParent(transform, false);
			}
			Transform dock = LifeguardProps.Dress(transform, DockModel, Vector3.zero, Quaternion.identity, body);
			if (!dock)
			{
				return false;
			}
			Transform homeMark = LifeguardProps.Find(dock, "Home");
			bool placed = body.Find(CameraModel);
			Transform model = LifeguardProps.Dress(body, CameraModel, Vector3.zero, Quaternion.identity);
			if (!placed && homeMark)
			{
				body.SetPositionAndRotation(homeMark.position, transform.rotation);
			}
			if (model)
			{
				SetLayer(body.gameObject, ToolLayer);
			}
			return CountChildren() != before;
		}

		private int CountChildren()
		{
			// Rebuilt from native code (token 0x06000024, RVA 0x27058C8)
			return GetComponentsInChildren<Transform>(true).Length;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000025, RVA 0x27059E0)
			FieldOfView = Mathf.Clamp(PlayerPrefs.GetInt(FovPref, DefaultFov), MinFov, MaxFov);
			Smoothing = Mathf.Clamp(PlayerPrefs.GetInt(SmoothingPref, DefaultSmoothing), 0, MaxSmoothing);
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000026, RVA 0x2705A8C)
			Dress();
			built = Build();
			if (!built)
			{
				Debug.LogWarning("Shallows: the Ripple camera models are missing, so there's no filming camera.");
				return;
			}
			Local = this;
			if (!GetComponent<RippleCameraPresenter>())
			{
				gameObject.AddComponent<RippleCameraPresenter>().owner = this;
			}
			RefreshLabels();
			UpdateRecordingLabels();
		}

		private bool Build()
		{
			// Rebuilt from native code (token 0x06000027, RVA 0x2705C3C)
			tablet = transform.Find(TabletName);
			Transform dock = transform.Find("RippleCameraDock/RippleCameraDock Model");
			Transform model = tablet ? tablet.Find("RippleCamera/RippleCamera Model") : null;
			if (!tablet || !dock || !model)
			{
				return false;
			}
			Transform homeMark = LifeguardProps.Find(dock, "Home");
			home = new GameObject("Camera Home").transform;
			home.SetParent(transform, false);
			home.SetPositionAndRotation((homeMark ? homeMark : tablet).position, transform.rotation);
			Transform returnKey = LifeguardProps.Find(dock, "Key Return");
			if (returnKey)
			{
				PokeButton button = LifeguardProps.Button(returnKey, transform.forward * 0.006f);
				button.repeat = false;
				button.onPress += ReturnHome;
			}
			Transform lensPoint = LifeguardProps.Find(model, "Lens Point");
			lens = new GameObject("Lens (screen facing)").transform;
			lens.SetParent(tablet, false);
			lens.SetPositionAndRotation((lensPoint ? lensPoint : tablet).position, Quaternion.LookRotation(-tablet.forward, tablet.up));
			Transform screenPart = LifeguardProps.Find(model, "Screen");
			if (!screenPart)
			{
				return false;
			}
			screen = LiveScreen(screenPart);
			Transform light = LifeguardProps.Find(model, "Rec Light");
			recLight = light ? light.GetComponent<Renderer>() : null;
			keys = new Key[6]
			{
				MakeKey(model, "Key FOV -", Action.FovDown),
				MakeKey(model, "Key FOV +", Action.FovUp),
				MakeKey(model, "Key Smooth -", Action.SmoothDown),
				MakeKey(model, "Key Smooth +", Action.SmoothUp),
				MakeKey(model, "Key Mode", Action.Mode),
				MakeKey(model, "Key Record", Action.Record)
			};
			keys = Array.FindAll(keys, k => k != null);
			Key flip = MakeFlipKey(model);
			if (flip != null)
			{
				Array.Resize(ref keys, keys.Length + 1);
				keys[keys.Length - 1] = flip;
			}
			modeText = Label(model, "Mode Text", "", 0.011f, Paint, PixelFonts.Sign);
			statusText = Label(model, "Status Text", "", 0.009f, Ink, PixelFonts.Sign);
			fovText = Label(model, "FOV Text", "", 0.026f, Green, PixelFonts.Screen);
			smoothText = Label(model, "Smooth Text", "", 0.026f, Green, PixelFonts.Screen);
			modeKeyText = Label(model, "Mode Key Text", "", 0.0115f, Cream, PixelFonts.Sign);
			recordKeyText = Label(model, "Record Key Text", "", 0.0105f, new Color(0.47058824f, 0.0627451f, 0.05490196f, 1f), PixelFonts.Sign);
			SetLayer(tablet.gameObject, ToolLayer);
			sounds = tablet.gameObject.AddComponent<AudioSource>();
			sounds.playOnAwake = false;
			sounds.spatialBlend = 0.8f;
			sounds.minDistance = 0.5f;
			sounds.maxDistance = 8f;
			recordStart = Resources.Load<AudioClip>("RippleCamera/RecordStart");
			recordStop = Resources.Load<AudioClip>("RippleCamera/RecordStop");
			shown = Array.FindAll(tablet.GetComponentsInChildren<Renderer>(true), r => r.enabled);
			tabletScale = tablet.localScale;
			return true;
		}

		private Renderer LiveScreen(Transform part)
		{
			// Rebuilt from native code (token 0x06000028, RVA 0x2707188)
			MeshFilter filter = part.GetComponent<MeshFilter>();
			Renderer original = part.GetComponent<Renderer>();
			if (!filter || !filter.sharedMesh || !original)
			{
				return null;
			}
			Bounds bounds = filter.sharedMesh.bounds;
			Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = bounds.center + new Vector3((i & 1) == 0 ? -bounds.extents.x : bounds.extents.x, (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y, i < 4 ? -bounds.extents.z : bounds.extents.z);
				Vector3 p = tablet.InverseTransformPoint(part.TransformPoint(corner));
				lo = Vector3.Min(lo, p);
				hi = Vector3.Max(hi, p);
			}
			GameObject live = new GameObject("Live Screen");
			live.transform.SetParent(tablet, false);
			live.transform.localPosition = new Vector3((lo.x + hi.x) * 0.5f, (lo.y + hi.y) * 0.5f, lo.z - 0.0003f);
			float w = hi.x - lo.x;
			float h = hi.y - lo.y;
			Mesh mesh = new Mesh();
			mesh.name = "Ripple Camera Screen";
			mesh.vertices = new Vector3[4]
			{
				new Vector3(w * -0.5f, h * -0.5f, 0f),
				new Vector3(w * 0.5f, h * -0.5f, 0f),
				new Vector3(w * 0.5f, h * 0.5f, 0f),
				new Vector3(w * -0.5f, h * 0.5f, 0f)
			};
			mesh.uv = new Vector2[4]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};
			mesh.normals = new Vector3[4] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
			mesh.triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
			mesh.RecalculateBounds();
			live.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer renderer = live.AddComponent<MeshRenderer>();
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			screenMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			screenMaterial.name = "Ripple Camera Screen";
			screenMaterial.SetColor("_BaseColor", Color.white);
			screenMaterial.SetTexture("_BaseMap", Texture2D.blackTexture);
			renderer.sharedMaterial = screenMaterial;
			original.enabled = false;
			return renderer;
		}

		private Key MakeKey(Transform model, string name, Action action)
		{
			// Rebuilt from native code (token 0x06000029, RVA 0x270787C)
			return KeyFor(LifeguardProps.Find(model, name), name, action);
		}

		private Key MakeFlipKey(Transform model)
		{
			// Rebuilt from native code (token 0x0600002A, RVA 0x2707908)
			Transform mode = LifeguardProps.Find(model, "Key Mode");
			Transform fovUp = LifeguardProps.Find(model, "Key FOV +");
			Transform smoothDown = LifeguardProps.Find(model, "Key Smooth -");
			if (!mode || !fovUp || !smoothDown || !mode.GetComponent<MeshFilter>())
			{
				return null;
			}
			if (!TabletBox(mode, out Vector3 modeLo, out Vector3 modeHi) || !TabletBox(fovUp, out Vector3 fovLo, out Vector3 fovHi) || !TabletBox(smoothDown, out Vector3 smoothLo, out Vector3 _))
			{
				return null;
			}
			float gap = smoothLo.x - fovHi.x;
			if (gap < 0.03f)
			{
				return null;
			}
			float width = Mathf.Min(0.07f, gap - 0.016f);
			Transform flip = Instantiate(mode.gameObject, mode.parent, false).transform;
			Vector3 center = new Vector3((smoothLo.x + fovHi.x) * 0.5f, (fovHi.y + fovLo.y) * 0.5f, (modeHi.z + modeLo.z) * 0.5f);
			Vector3 modeSize = modeHi - modeLo;
			float height = fovHi.y - fovLo.y;
			flip.name = "Key Flip";
			foreach (Transform child in flip)
			{
				Destroy(child.gameObject);
			}
			flip.localPosition = mode.localPosition;
			flip.localRotation = mode.localRotation;
			Vector3 scale = ScaleAlong(flip, flip.localScale, tablet.right, width / Mathf.Max(0.0001f, modeSize.x));
			flip.localScale = ScaleAlong(flip, scale, tablet.up, height / Mathf.Max(0.0001f, modeSize.y));
			Vector3 pivot = flip.TransformPoint(flip.GetComponent<MeshFilter>().sharedMesh.bounds.center);
			flip.position += tablet.TransformPoint(center) - pivot;
			Key key = KeyFor(flip, "Key Flip", Action.Flip);
			if (key == null)
			{
				return null;
			}
			TextMesh label = LifeguardProps.Text(tablet, model, "Key Flip Text", "Flip Key Text", PixelFonts.Sign, 0.0115f, Cream, TextAnchor.MiddleCenter, 1f);
			label.transform.SetParent(tablet, true);
			label.transform.position = tablet.TransformPoint(new Vector3(center.x, center.y, modeLo.z - 0.0008f));
			label.transform.rotation = tablet.rotation;
			Vector3 lossy = tablet.lossyScale;
			label.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
			label.text = "FLIP";
			label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
			flipKeyText = label;
			return key;
		}

		private static Vector3 ScaleAlong(Transform part, Vector3 scale, Vector3 worldAxis, float factor)
		{
			// Rebuilt from native code (token 0x0600002B, RVA 0x2708A98)
			Vector3 axis = part.InverseTransformDirection(worldAxis);
			if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y) && Mathf.Abs(axis.x) >= Mathf.Abs(axis.z))
			{
				scale.x *= factor;
			}
			else if (Mathf.Abs(axis.y) >= Mathf.Abs(axis.z))
			{
				scale.y *= factor;
			}
			else
			{
				scale.z *= factor;
			}
			return scale;
		}

		private bool TabletBox(Transform part, out Vector3 lo, out Vector3 hi)
		{
			// Rebuilt from native code (token 0x0600002C, RVA 0x2708868)
			lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			MeshFilter filter = part ? part.GetComponent<MeshFilter>() : null;
			if (!filter || !filter.sharedMesh)
			{
				return false;
			}
			Bounds bounds = filter.sharedMesh.bounds;
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = bounds.center + new Vector3((i & 1) == 0 ? -bounds.extents.x : bounds.extents.x, (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y, i < 4 ? -bounds.extents.z : bounds.extents.z);
				Vector3 p = tablet.InverseTransformPoint(part.TransformPoint(corner));
				lo = Vector3.Min(lo, p);
				hi = Vector3.Max(hi, p);
			}
			return true;
		}

		private Key KeyFor(Transform part, string name, Action action)
		{
			// Rebuilt from native code (token 0x0600002D, RVA 0x2708468)
			if (!TabletBox(part, out Vector3 lo, out Vector3 hi))
			{
				return null;
			}
			Transform frame = new GameObject(name + " (press)").transform;
			frame.SetParent(tablet, false);
			frame.localPosition = new Vector3((lo.x + hi.x) * 0.5f, (lo.y + hi.y) * 0.5f, lo.z);
			frame.localRotation = Quaternion.identity;
			return new Key
			{
				action = action,
				frame = frame,
				part = part,
				size = new Vector2(hi.x - lo.x, hi.y - lo.y),
				rest = part.localPosition,
				push = part.localPosition + part.parent.InverseTransformVector(tablet.forward * 0.003f)
			};
		}

		private TextMesh Label(Transform model, string anchor, string text, float height, Color color, Font font)
		{
			// Rebuilt from native code (token 0x0600002E, RVA 0x270824C)
			if (!LifeguardProps.Find(model, anchor))
			{
				return null;
			}
			TextMesh label = LifeguardProps.Text(tablet, model, anchor, anchor, font, height, color, TextAnchor.MiddleCenter, 1f);
			label.transform.SetParent(tablet, true);
			label.transform.rotation = tablet.rotation;
			Vector3 lossy = tablet.lossyScale;
			label.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
			label.text = text;
			label.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
			return label;
		}

		private static void SetLayer(GameObject go, int layer)
		{
			// Rebuilt from native code (token 0x0600002F, RVA 0x2705924)
			foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = layer;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000030, RVA 0x2708B28)
			Application.onBeforeRender += BeforeRender;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000031, RVA 0x2708BCC)
			Application.onBeforeRender -= BeforeRender;
			if (Recorder)
			{
				Recorder.Stop();
			}
			HeldHand = -1;
			foreach (Finger finger in fingers)
			{
				finger.held = null;
				finger.ready = false;
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000032, RVA 0x2708DD8)
			DisposePreview();
			if (screenMaterial)
			{
				Destroy(screenMaterial);
			}
			if (recMaterial)
			{
				Destroy(recMaterial);
			}
			if (Local == this)
			{
				Local = null;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000033, RVA 0x270912C)
			if (!built)
			{
				return;
			}
			PlayerRig rig = PlayerRig.I;
			if (!rig)
			{
				return;
			}
			if (HeldHand >= 0 && Blocked(rig))
			{
				HeldHand = -1;
			}
			PollGrip(rig, true);
			PollGrip(rig, false);
		}

		private static bool Blocked(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x06000034, RVA 0x2709230)
			return rig.Frozen || (GameManager.I && GameManager.I.Dying);
		}

		private void PollGrip(PlayerRig rig, bool left)
		{
			// Rebuilt from native code (token 0x06000035, RVA 0x270936C)
			float grip = XRHands.Grip(left);
			int hand = left ? 0 : 1;
			if (!gripping[hand] && grip > GripOn)
			{
				gripping[hand] = true;
				TryGrab(rig, left);
			}
			else if (gripping[hand] && grip < GripOff)
			{
				gripping[hand] = false;
				if (HeldHand == hand)
				{
					FollowHeld();
					HeldHand = -1;
				}
			}
		}

		public static bool Holds(bool left)
		{
			// Rebuilt from native code (token 0x06000036, RVA 0x2709A7C)
			return Local && Local.HeldHand == (left ? 0 : 1);
		}

		private bool TryGrab(PlayerRig rig, bool left)
		{
			// Rebuilt from native code (token 0x06000037, RVA 0x2709470)
			if (Hidden || Blocked(rig) || HandBusy(left))
			{
				return false;
			}
			Transform hand = left ? rig.leftController : rig.rightController;
			if (!hand)
			{
				return false;
			}
			Vector3 local = tablet.InverseTransformPoint(hand.position);
			Vector3 nearest = Vector3.Max(GrabMin, Vector3.Min(GrabMax, local));
			if (Vector3.Distance(local, nearest) > GrabReach)
			{
				return false;
			}
			int mask = (rig.locomotion ? (int)rig.locomotion.locomotionEnabledLayers : ~(1 << IgnoreRaycast)) & ~(1 << ToolLayer);
			if (Physics.Linecast(rig.head.position, hand.position, mask, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			Docked = false;
			HeldHand = left ? 0 : 1;
			grabPosition = hand.InverseTransformPoint(tablet.position);
			grabRotation = Quaternion.Inverse(hand.rotation) * tablet.rotation;
			foreach (Finger finger in fingers)
			{
				finger.held = null;
				finger.ready = false;
			}
			XRHands.Haptic(left, 0.16f, 0.04f);
			return true;
		}

		private static bool HandBusy(bool left)
		{
			// Rebuilt from native code (token 0x06000038, RVA 0x2709BB4)
			GameManager game = GameManager.I;
			if (!game || game.grabbers == null)
			{
				return false;
			}
			foreach (HandGrabber grabber in game.grabbers)
			{
				if (grabber && grabber.left == left && grabber.Held)
				{
					return true;
				}
			}
			return false;
		}

		private void Release()
		{
			// Rebuilt from native code (token 0x06000039, RVA 0x2708DCC)
			HeldHand = -1;
		}

		private void FollowHeld()
		{
			// Rebuilt from native code (token 0x0600003A, RVA 0x2709884)
			if (HeldHand < 0)
			{
				return;
			}
			PlayerRig rig = PlayerRig.I;
			if (!rig || !tablet)
			{
				return;
			}
			Transform hand = HeldHand == 0 ? rig.leftController : rig.rightController;
			if (hand)
			{
				tablet.SetPositionAndRotation(hand.TransformPoint(grabPosition), hand.rotation * grabRotation);
			}
		}

		[BeforeRenderOrder(250)]
		private void BeforeRender()
		{
			// Rebuilt from native code (token 0x0600003B, RVA 0x2709D34)
			FollowHeld();
		}

		public void ReturnHome()
		{
			// Rebuilt from native code (token 0x0600003C, RVA 0x2709D38)
			if (!built)
			{
				return;
			}
			HeldHand = -1;
			tablet.SetPositionAndRotation(home.position, home.rotation);
			FirstPerson = false;
			Flipped = false;
			viewReady = false;
			nextFrame = 0f;
			RefreshLabels();
			SetHidden(false);
			Docked = true;
			foreach (Finger finger in fingers)
			{
				finger.held = null;
				finger.ready = false;
			}
		}

		public void WatchToggle()
		{
			// Rebuilt from native code (token 0x0600003D, RVA 0x2709F68)
			if (!built)
			{
				return;
			}
			if (Hidden || Docked)
			{
				Summon();
			}
			else
			{
				Hide();
			}
		}

		public void Summon()
		{
			// Rebuilt from native code (token 0x0600003E, RVA 0x270A028)
			if (!built)
			{
				return;
			}
			PlayerRig rig = PlayerRig.I;
			if (!rig || !rig.head)
			{
				return;
			}
			Transform head = rig.head;
			HeldHand = -1;
			float size = rig.eyeHeight / 0.55f;
			Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
			if (flat.sqrMagnitude < 0.0001f)
			{
				flat = Vector3.ProjectOnPlane(head.up, Vector3.up);
			}
			flat = flat.normalized;
			Vector3 target = head.position + flat * (size * 0.55f) + Vector3.down * (size * 0.12f);
			int mask = (rig.locomotion ? (int)rig.locomotion.locomotionEnabledLayers : ~(1 << IgnoreRaycast)) & ~(1 << ToolLayer);
			Vector3 reach = target - head.position;
			if (Physics.SphereCast(head.position, 0.1f, reach.normalized, out RaycastHit hit, reach.magnitude, mask, QueryTriggerInteraction.Ignore))
			{
				target = head.position + reach.normalized * Mathf.Max(0.2f, hit.distance - 0.05f);
			}
			tablet.SetPositionAndRotation(target, Quaternion.LookRotation(target - head.position));
			Docked = false;
			SetHidden(false);
			popStart = Time.unscaledTime;
			tablet.localScale = tabletScale * 0.01f;
			viewReady = false;
			nextFrame = 0f;
			foreach (Finger finger in fingers)
			{
				finger.held = null;
				finger.ready = false;
			}
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ButtonPress(), target, 0.5f, 1.4f);
			}
		}

		public void Hide()
		{
			// Rebuilt from native code (token 0x0600003F, RVA 0x2709F8C)
			if (!built)
			{
				return;
			}
			HeldHand = -1;
			foreach (Finger finger in fingers)
			{
				finger.held = null;
				finger.ready = false;
			}
			SetHidden(true);
		}

		private void SetHidden(bool value)
		{
			// Rebuilt from native code (token 0x06000040, RVA 0x2709E68)
			Hidden = value;
			if (value)
			{
				popStart = -1f;
				tablet.localScale = tabletScale;
			}
			foreach (Renderer renderer in shown)
			{
				if (renderer)
				{
					renderer.enabled = !value;
				}
			}
		}

		private void StepPop()
		{
			// Rebuilt from native code (token 0x06000041, RVA 0x270A928)
			if (popStart < 0f)
			{
				return;
			}
			float t = Mathf.Clamp01((Time.unscaledTime - popStart) / PopTime);
			float k = t - 1f;
			float pop = t >= 1f ? 1f : 1f + 2.2f * MathF.Pow(k, 3f) + 1.2f * MathF.Pow(k, 2f);
			tablet.localScale = tabletScale * Mathf.Max(0.01f, pop);
			if (t >= 1f)
			{
				popStart = -1f;
			}
		}

		public void Present(float dt)
		{
			// Rebuilt from native code (token 0x06000042, RVA 0x270AA20)
			if (!built)
			{
				return;
			}
			FollowHeld();
			StepPop();
			if (Fingertips.Get(true, out Vector3 leftTip))
			{
				SampleFinger(true, leftTip);
			}
			if (Fingertips.Get(false, out Vector3 rightTip))
			{
				SampleFinger(false, rightTip);
			}
			float now = Time.unscaledTime;
			float follow = 1f - MathF.Exp(dt * -35f);
			foreach (Key key in keys)
			{
				bool down = now < key.pressedUntil || fingers[0].held == key || fingers[1].held == key;
				key.part.localPosition = Vector3.Lerp(key.part.localPosition, down ? key.push : key.rest, follow);
			}
			UpdateRecLight(now);
			EnsurePreview();
			StepView(dt);
			if (NeedsFrame(now))
			{
				RenderPreview();
			}
		}

		private void SampleFinger(bool left, Vector3 tip)
		{
			// Rebuilt from native code (token 0x06000043, RVA 0x270AC44)
			Finger finger = fingers[left ? 0 : 1];
			PlayerRig rig = PlayerRig.I;
			if (Hidden || popStart >= 0f || HeldHand == (left ? 0 : 1) || (rig && Blocked(rig)))
			{
				finger.held = null;
				finger.ready = false;
				return;
			}
			if (!finger.ready)
			{
				finger.previous = tip;
				finger.ready = true;
				return;
			}
			Vector3 previous = finger.previous;
			finger.previous = tip;
			if (finger.held != null)
			{
				Vector3 p = finger.held.frame.InverseTransformPoint(tip);
				if (p.z >= -0.032f && finger.held.Contains(p, 0.025f))
				{
					return;
				}
				finger.held = null;
			}
			if (Vector3.Distance(tip, previous) > 0.3f)
			{
				return;
			}
			foreach (Key key in keys)
			{
				Vector3 a = key.frame.InverseTransformPoint(previous);
				Vector3 b = key.frame.InverseTransformPoint(tip);
				if (a.z >= -0.006f || b.z < -0.006f || b.z > 0.045f)
				{
					continue;
				}
				Vector3 hit = Vector3.Lerp(a, b, (-0.006f - a.z) / (b.z - a.z));
				if (!key.Contains(hit, 0.002f))
				{
					continue;
				}
				finger.held = key;
				if (fingers[left ? 1 : 0].held == key || Time.unscaledTime < key.pressedUntil)
				{
					return;
				}
				Press(key);
				XRHands.Haptic(left, 0.18f, 0.04f);
				return;
			}
		}

		private void Press(Key key)
		{
			// Rebuilt from native code (token 0x06000044, RVA 0x270C378)
			key.pressedUntil = Time.unscaledTime + 0.12f;
			if (key.action == Action.Record)
			{
				if (!ToggleRecording())
				{
					Click();
				}
				return;
			}
			Click();
			switch (key.action)
			{
			case Action.FovDown:
				SetSettings(FieldOfView - FovStep, Smoothing);
				break;
			case Action.FovUp:
				SetSettings(FieldOfView + FovStep, Smoothing);
				break;
			case Action.SmoothDown:
				SetSettings(FieldOfView, Smoothing - 1);
				break;
			case Action.SmoothUp:
				SetSettings(FieldOfView, Smoothing + 1);
				break;
			case Action.Mode:
				SetFirstPerson(!FirstPerson);
				break;
			case Action.Flip:
				SetFlipped(!Flipped);
				break;
			}
		}

		private void Click()
		{
			// Rebuilt from native code (token 0x06000045, RVA 0x270C5C4)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ButtonPress(), tablet.position, 0.5f, 1f);
			}
		}

		public bool ToggleRecording()
		{
			// Rebuilt from native code (token 0x06000046, RVA 0x270C4A0)
			if (!Recorder)
			{
				Recorder = RippleCameraRecorder.Create(this);
			}
			RippleCameraRecorder.Status before = Recorder.State;
			bool toggled = Recorder.Toggle();
			if (toggled)
			{
				AudioClip clip = before == RippleCameraRecorder.Status.Recording ? recordStop : recordStart;
				if (clip && sounds)
				{
					sounds.PlayOneShot(clip, 0.8f);
				}
			}
			UpdateRecordingLabels();
			return toggled;
		}

		public void SetFirstPerson(bool value)
		{
			// Rebuilt from native code (token 0x06000047, RVA 0x2709E58)
			FirstPerson = value;
			viewReady = false;
			nextFrame = 0f;
			RefreshLabels();
		}

		public void SetFlipped(bool value)
		{
			// Rebuilt from native code (token 0x06000048, RVA 0x270C808)
			if (FirstPerson)
			{
				FirstPerson = false;
				Flipped = true;
			}
			else
			{
				Flipped = value;
			}
			viewReady = false;
			nextFrame = 0f;
			RefreshLabels();
		}

		public void SetSettings(int fov, int smoothing)
		{
			// Rebuilt from native code (token 0x06000049, RVA 0x270C74C)
			FieldOfView = Mathf.Clamp(fov, MinFov, MaxFov);
			Smoothing = Mathf.Clamp(smoothing, 0, MaxSmoothing);
			PlayerPrefs.SetInt(FovPref, FieldOfView);
			PlayerPrefs.SetInt(SmoothingPref, Smoothing);
			PlayerPrefs.Save();
			RefreshLabels();
		}

		private void RefreshLabels()
		{
			// Rebuilt from native code (token 0x0600004A, RVA 0x2706C24)
			if (fovText)
			{
				fovText.text = FieldOfView.ToString();
			}
			if (smoothText)
			{
				smoothText.text = Smoothing.ToString();
			}
			if (modeText)
			{
				modeText.text = FirstPerson ? "FIRST PERSON" : (Flipped ? "FACING OUT" : "FREE CAMERA");
			}
			if (flipKeyText)
			{
				flipKeyText.color = FacingOut ? Green : Cream;
			}
			if (modeKeyText)
			{
				modeKeyText.text = FirstPerson ? "FREE CAMERA" : "FIRST PERSON";
			}
		}

		public void UpdateRecordingLabels()
		{
			// Rebuilt from native code (token 0x0600004B, RVA 0x2706EC4)
			if (recordKeyText)
			{
				recordKeyText.text = Recorder ? Recorder.ButtonText : "RECORD";
			}
			if (!statusText)
			{
				return;
			}
			Color colour;
			if (!Recorder || Recorder.State == RippleCameraRecorder.Status.Ready)
			{
				statusText.text = Application.platform == RuntimePlatform.Android ? "QUEST VIDEO" : (DesktopFeed ? "PC FEED ON  F8" : "PC FEED OFF  F8");
				colour = Ink;
			}
			else
			{
				statusText.text = Recorder.StatusText;
				colour = Recorder.State == RippleCameraRecorder.Status.Recording ? RecRed : Ink;
			}
			statusText.color = colour;
		}

		private void UpdateRecLight(float now)
		{
			// Rebuilt from native code (token 0x0600004C, RVA 0x270B100)
			if (!recLight)
			{
				return;
			}
			Material material;
			if (Capturing && Mathf.Repeat(now, 1f) < 0.6f)
			{
				if (!recMaterial)
				{
					recMaterial = new Material(recLight.sharedMaterial)
					{
						name = "Ripple Camera Rec Light"
					};
					recMaterial.SetColor("_BaseColor", new Color(1f, 0.5f, 0.45f, 1f));
					recMaterial.SetColor("_EmissionColor", new Color(2.5f, 0.2f, 0.15f, 1f));
					recMaterial.EnableKeyword("_EMISSION");
				}
				material = recMaterial;
			}
			else
			{
				material = LifeguardProps.MaterialFor(CameraModel, null);
			}
			if (material && recLight.sharedMaterial != material)
			{
				recLight.sharedMaterial = material;
			}
			if (!Recorder && Time.frameCount % 30 == 0)
			{
				UpdateRecordingLabels();
			}
		}

		private bool NeedsFrame(float now)
		{
			// Rebuilt from native code (token 0x0600004D, RVA 0x270BAA0)
			if (now + 0.0001f < nextFrame)
			{
				return false;
			}
			PlayerRig rig = PlayerRig.I;
			bool seen = false;
			if (rig && !Hidden && Vector3.Distance(rig.head.position, tablet.position) < 12f && Vector3.Dot(rig.head.forward, (tablet.position - rig.head.position).normalized) > 0.15f)
			{
				seen = Vector3.Dot(-tablet.forward, rig.head.position - tablet.position) > 0f;
			}
			if (!DesktopFeed && !FullPicture && ProbeRender == 0 && !seen)
			{
				return false;
			}
			float rate = FullPicture ? 60f : (Application.platform == RuntimePlatform.Android ? 30f : 60f);
			nextFrame += 1f / rate;
			if (nextFrame <= now)
			{
				nextFrame = now + 1f / rate;
			}
			return true;
		}

		private void EnsurePreview()
		{
			// Rebuilt from native code (token 0x0600004E, RVA 0x270B428)
			int width = FullPicture || DesktopFeed ? 1920 : (Application.platform == RuntimePlatform.Android ? 640 : 960);
			if (PreviewCamera && Preview && Preview.width == width)
			{
				return;
			}
			DisposePreview();
			Preview = new RenderTexture(width, width * 9 / 16, 24, RenderTextureFormat.ARGB32)
			{
				name = "Ripple camera picture",
				antiAliasing = 1,
				useMipMap = false,
				autoGenerateMips = false
			};
			Preview.Create();
			GameObject go = new GameObject("Ripple camera (mono, renders on demand)");
			go.transform.SetParent(transform, false);
			PreviewCamera = go.AddComponent<Camera>();
			PreviewCamera.enabled = false;
			PreviewCamera.nearClipPlane = 0.04f;
			PreviewCamera.farClipPlane = 300f;
			PreviewCamera.aspect = 16f / 9f;
			PreviewCamera.allowMSAA = false;
			PreviewCamera.targetTexture = Preview;
			go.AddComponent<UniversalAdditionalCameraData>().allowXRRendering = false;
			SyncViewRendering();
			if (screenMaterial)
			{
				screenMaterial.SetTexture("_BaseMap", Preview);
			}
			viewReady = false;
		}

		private void StepView(float dt)
		{
			// Rebuilt from native code (token 0x0600004F, RVA 0x270B7B0)
			if (!PreviewCamera)
			{
				return;
			}
			Transform eye = lens;
			if (FirstPerson)
			{
				PlayerRig rig = PlayerRig.I;
				if (rig)
				{
					eye = rig.head;
				}
			}
			Quaternion want = FacingOut ? eye.rotation * Quaternion.Euler(0f, 180f, 0f) : eye.rotation;
			float follow = 1f;
			if (Smoothing != 0 && viewReady)
			{
				follow = 1f - MathF.Exp(Mathf.Max(0f, dt) / (Smoothing * -0.06f));
			}
			PreviewCamera.transform.SetPositionAndRotation(eye.position, Quaternion.Slerp(PreviewCamera.transform.rotation, want, follow));
			PreviewCamera.fieldOfView = FieldOfView;
			viewReady = true;
		}

		private Camera HeadCamera()
		{
			// Rebuilt from native code (token 0x06000050, RVA 0x270DA94)
			PlayerRig rig = PlayerRig.I;
			Camera camera = rig && rig.head ? rig.head.GetComponent<Camera>() : null;
			return camera ? camera : Camera.main;
		}

		private void SyncViewRendering()
		{
			// Rebuilt from native code (token 0x06000051, RVA 0x270D804)
			if (!PreviewCamera)
			{
				return;
			}
			UniversalAdditionalCameraData data = PreviewCamera.GetComponent<UniversalAdditionalCameraData>();
			data.allowXRRendering = false;
			Camera head = HeadCamera();
			int mask = (head ? head.cullingMask : -1) & ~(1 << ToolLayer);
			PreviewCamera.cullingMask = FirstPerson ? mask & ~(1 << Mirror.ReflectionLayer) : (mask & ~(1 << Mirror.FirstPersonLayer)) | (1 << Mirror.ReflectionLayer);
			if (!head)
			{
				data.renderShadows = true;
				return;
			}
			PreviewCamera.allowHDR = head.allowHDR;
			PreviewCamera.clearFlags = head.clearFlags;
			PreviewCamera.backgroundColor = head.backgroundColor;
			PreviewCamera.farClipPlane = head.farClipPlane;
			if (!head.TryGetComponent(out UniversalAdditionalCameraData source))
			{
				data.renderShadows = true;
				return;
			}
			data.renderShadows = source.renderShadows;
			data.requiresDepthOption = source.requiresDepthOption;
			data.requiresColorOption = source.requiresColorOption;
			data.renderPostProcessing = source.renderPostProcessing;
			data.volumeLayerMask = source.volumeLayerMask;
			data.volumeTrigger = source.volumeTrigger ? source.volumeTrigger : head.transform;
			data.antialiasing = source.antialiasing;
			data.antialiasingQuality = source.antialiasingQuality;
			data.stopNaN = source.stopNaN;
			data.dithering = source.dithering;
		}

		private bool RenderPreview()
		{
			// Rebuilt from native code (token 0x06000052, RVA 0x270BEAC)
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				return false;
			}
			if (!Preview.IsCreated())
			{
				Preview.Create();
			}
			SyncViewRendering();
			PreviewCamera.aspect = (float)Preview.width / Preview.height;
			PreviewCamera.projectionMatrix = Matrix4x4.Perspective(FieldOfView, PreviewCamera.aspect, PreviewCamera.nearClipPlane, PreviewCamera.farClipPlane);
			PlayerRig rig = PlayerRig.I;
			Transform fader = rig && rig.fader && !FirstPerson ? rig.fader.transform : null;
			Transform parent = fader ? fader.parent : null;
			Vector3 localPosition = fader ? fader.localPosition : default;
			Quaternion localRotation = fader ? fader.localRotation : default;
			try
			{
				if (fader)
				{
					fader.SetParent(PreviewCamera.transform, false);
				}
				request.destination = Preview;
				if (!RenderPipeline.SupportsRenderRequest(PreviewCamera, request))
				{
					return false;
				}
				RenderPipeline.SubmitRenderRequest(PreviewCamera, request);
				if (Recorder)
				{
					Recorder.Capture(Preview);
				}
				return true;
			}
			finally
			{
				if (fader)
				{
					fader.SetParent(parent, false);
					fader.SetLocalPositionAndRotation(localPosition, localRotation);
				}
			}
		}

		private void DisposePreview()
		{
			// Rebuilt from native code (token 0x06000053, RVA 0x2708F98)
			if (PreviewCamera)
			{
				PreviewCamera.targetTexture = null;
				Destroy(PreviewCamera.gameObject);
			}
			PreviewCamera = null;
			if (screenMaterial)
			{
				screenMaterial.SetTexture("_BaseMap", Texture2D.blackTexture);
			}
			if (Preview)
			{
				Preview.Release();
				Destroy(Preview);
			}
			Preview = null;
			viewReady = false;
		}
	}
}
