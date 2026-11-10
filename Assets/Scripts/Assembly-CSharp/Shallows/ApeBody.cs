using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	[DefaultExecutionOrder(100)]
	public class ApeBody : MonoBehaviour
	{
		private class Arm
		{
			public Transform shoulder;

			public Transform upperPivot;

			public Transform lowerPivot;

			public Transform hand;

			public float upperLength;

			public float lowerLength;

			public float outward;

			public Vector3 knuckles;

			public Vector3 thumbSide;

			public Vector3 palm;

			public Vector3 fistCenter;

			public readonly List<(Transform bone, Vector3 axis, float degrees, int finger)> joints = new List<(Transform bone, Vector3 axis, float degrees, int finger)>();

			public Vector3 curl;

			public Vector3 palmPoint;

			public float handLength;

			public readonly (Transform distal, Vector3 tip)[] tips = new (Transform distal, Vector3 tip)[3];

			public HeldGrip grip;

			public Transform item;

			public float hold;

			public float[] wrap;

			public HeldGrip wrapFor;

			public float wrapScale;

			public float restTwist;
		}

		internal const string ModelPath = "Ape/ShallowsPlayer";

		private const string AlbedoPath = "Ape/ShallowsPlayer_Albedo";

		[Tooltip("Where the eyes go (the headset, or a remote player's head).")]
		public Transform headTarget;

		[Tooltip("Where the hands go (the hand followers); their rotation is the controllers'.")]
		public Transform leftTarget;

		[Tooltip("Where the hands go (the hand followers); their rotation is the controllers'.")]
		public Transform rightTarget;

		[Tooltip("Mouth opening from the voice. Empty for your own gorilla, which has no head.")]
		public VoiceLevel voice;

		[Tooltip("How far each hand's fingers curl, 0 open to 1 closed: x index (trigger), y middle (grip), z thumb (on the face buttons). Your own gorilla reads them from your controllers; remote ones get them from the network.")]
		public Vector3 leftFingers;

		[Tooltip("How far each hand's fingers curl, 0 open to 1 closed: x index (trigger), y middle (grip), z thumb (on the face buttons). Your own gorilla reads them from your controllers; remote ones get them from the network.")]
		public Vector3 rightFingers;

		private Func<float> eyeHeight;

		private SkinnedMeshRenderer skin;

		private Material material;

		private Renderer[] hiddenRenderers = Array.Empty<Renderer>();

		private int screamShape = -1;

		private Color shownFur = new Color(-1f, 0f, 0f, 1f);

		private float modelEye;

		private Transform neck;

		private Transform head;

		private Quaternion neckRest;

		private Quaternion headRest;

		private Vector3 eyeOffset;

		private Vector3 headScale;

		private Arm leftArm;

		private Arm rightArm;

		private Vector3 waist;

		private Transform chest;

		private Vector3 tagSpot;

		private Quaternion tagTurn;

		private readonly List<(Transform bone, Vector3 position, Quaternion rotation)> pose = new List<(Transform bone, Vector3 position, Quaternion rotation)>();

		private float bodyYaw;

		private float bodyHold;

		private bool placed;

		private ApeBody reflection;

		private bool isReflection;

		private float micLevel;

		private const float RelaxedCurl = 0.12f;

		private const float MaxForearmTwist = 60f;

		private const float HoldFrom = 35f;

		private const float HoldFull = 50f;

		private static readonly Dictionary<Color32, Texture2D> furTextures = new Dictionary<Color32, Texture2D>();

		private static Color32[] albedoPixels;

		private static int albedoWidth;

		private static int albedoHeight;

		private const float DefaultFurLuminance = 0.0937f;

		public BeltLook belt;

		private const float FingerThickness = 0.13f;

		private const int WrapSteps = 20;

		private static readonly List<int> chainJoints = new List<int>();

		private static readonly List<Quaternion> chainRest = new List<Quaternion>();

		private static readonly List<float> chainClosed = new List<float>();

		public static ApeBody Local { get; private set; }

		internal SkinnedMeshRenderer Skin
		{
			get
			{
				// Rebuilt from native code (token 0x0600079B, RVA 0x281031C)
				return skin;
			}
		}

		public float NameTagHeight
		{
			get
			{
				// Rebuilt from native code (token 0x060007A1, RVA 0x28130FC)
				return modelEye * 0.03f;
			}
		}

		public float NameTagWidth
		{
			get
			{
				// Rebuilt from native code (token 0x060007A2, RVA 0x2813110)
				return modelEye * 0.3f;
			}
		}

		public Vector3 WaistPoint
		{
			get
			{
				// Rebuilt from native code (token 0x060007B2, RVA 0x2814B4C)
				return waist;
			}
		}

		public bool Visible
		{
			get
			{
				// Rebuilt from native code (token 0x060007B3, RVA 0x2814B58)
				return skin && skin.enabled;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600079A, RVA 0x2810244)
			Local = null;
			furTextures.Clear();
			albedoPixels = null;
		}

		public static ApeBody CreateLocal(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x0600079C, RVA 0x2810324)
			ApeBody body = Create(null, rig.head, rig.leftHand, rig.rightHand, () => rig.eyeHeight, TemplateMaterial(rig.leftHand));
			if (!body)
			{
				return null;
			}
			Local = body;
			body.name = "Your Gorilla";
			body.head.localScale = body.headScale * 0.0001f;
			body.skin.shadowCastingMode = ShadowCastingMode.Off;
			body.hiddenRenderers = new Renderer[] { rig.leftHand.GetComponent<Renderer>(), rig.rightHand.GetComponent<Renderer>() };
			SetLayer(body.gameObject, 30);
			body.reflection = Create(null, rig.head, rig.leftHand, rig.rightHand, () => rig.eyeHeight, TemplateMaterial(rig.leftHand));
			if (body.reflection)
			{
				body.reflection.name = "Your Reflection";
				body.reflection.isReflection = true;
				body.reflection.skin.shadowCastingMode = ShadowCastingMode.Off;
				SetLayer(body.reflection.gameObject, 31);
				Camera view = rig.head.GetComponentInChildren<Camera>();
				Camera camera = view ? view : Camera.main;
				if (camera)
				{
					camera.cullingMask &= ~(1 << 31);
				}
			}
			body.SetVisible(true);
			RippleWatch.Create(body);
			body.belt = BeltLook.Wear(body, true);
			if (body.reflection)
			{
				body.reflection.belt = BeltLook.Wear(body.reflection, true);
			}
			CosmeticLook.FollowWallet(body);
			if (body.reflection)
			{
				CosmeticLook.FollowWallet(body.reflection);
			}
			PlayerProfile.Load();
			body.SetFur(PlayerProfile.Color);
			PlayerProfile.Changed += body.ProfileChanged;
			return body;
		}

		public static ApeBody Create(Transform parent, Transform head, Transform leftHand, Transform rightHand, Func<float> eyeHeight, Material template)
		{
			// Rebuilt from native code (token 0x0600079D, RVA 0x2810AB8)
			GameObject prefab = Resources.Load<GameObject>(ModelPath);
			if (!prefab || !head || !leftHand || !rightHand)
			{
				return null;
			}
			GameObject go = new GameObject("Gorilla Model");
			go.transform.SetParent(parent, false);
			ApeBody body = go.AddComponent<ApeBody>();
			body.headTarget = head;
			body.leftTarget = leftHand;
			body.rightTarget = rightHand;
			body.eyeHeight = eyeHeight;
			if (body.Build(prefab, template))
			{
				return body;
			}
			Destroy(go);
			return null;
		}

		private static Material TemplateMaterial(Transform t)
		{
			// Rebuilt from native code (token 0x0600079E, RVA 0x28109EC)
			Renderer renderer = t ? t.GetComponent<Renderer>() : null;
			return renderer ? renderer.sharedMaterial : null;
		}

		private bool Build(GameObject prefab, Material template)
		{
			// Rebuilt from native code (token 0x0600079F, RVA 0x28110E8)
			Transform facing = new GameObject("Facing").transform;
			facing.SetParent(transform, false);
			GameObject model = Instantiate(prefab, facing, false);
			model.name = "ShallowsPlayer";
			SetLayer(gameObject, 2);
			skin = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
			Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
			foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
			{
				bones[t.name] = t;
			}
			neck = Bone("Neck");
			head = Bone("Head");
			Transform eyeLeft = Bone("Eye.L");
			Transform eyeRight = Bone("Eye.R");
			if (!skin || !neck || !head || !eyeLeft || !eyeRight)
			{
				Debug.LogWarning("Shallows: the gorilla model is missing its skin or head bones, so players keep the sphere hands.");
				return false;
			}
			transform.localScale = Vector3.one;
			Vector3 eyes = (eyeLeft.position + eyeRight.position) * 0.5f;
			Vector3 look = transform.InverseTransformDirection(eyes - head.position);
			facing.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.ProjectOnPlane(look, Vector3.up), Vector3.forward, Vector3.up), Vector3.up);
			eyes = (eyeLeft.position + eyeRight.position) * 0.5f;
			modelEye = transform.InverseTransformPoint(eyes).y;
			if (modelEye < 0.1f)
			{
				Debug.LogWarning("Shallows: the gorilla model's eyes aren't above its feet (wrong axes on import?), so players keep the sphere hands.");
				return false;
			}
			Quaternion unturn = Quaternion.Inverse(transform.rotation);
			neckRest = unturn * neck.rotation;
			headRest = unturn * head.rotation;
			headScale = head.localScale;
			eyeOffset = Quaternion.Inverse(head.rotation) * (eyes - head.position) / transform.lossyScale.x;
			leftArm = BuildArm(Bone, "L");
			rightArm = BuildArm(Bone, "R");
			Transform hips = Bone("Hips");
			Transform spine = Bone("Spine");
			waist = hips && spine ? transform.InverseTransformPoint(Vector3.Lerp(hips.position, spine.position, 0.5f)) : new Vector3(0f, 0.15f, 0f);
			if (leftArm == null || rightArm == null)
			{
				Debug.LogWarning("Shallows: the gorilla model is missing arm bones, so players keep the sphere hands.");
				return false;
			}
			chest = Bone("Chest");
			if (chest)
			{
				MeasureNameTagSpot();
			}
			foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
			{
				pose.Add((t, t.localPosition, t.localRotation));
			}
			screamShape = skin.sharedMesh.GetBlendShapeIndex("Scream");
			Bounds bounds = skin.localBounds;
			bounds.Expand(bounds.size.magnitude * 2f);
			skin.localBounds = bounds;
			material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
			material.name = "Gorilla Fur";
			material.color = Color.white;
			if (material.HasProperty("_Smoothness"))
			{
				material.SetFloat("_Smoothness", 0.1f);
			}
			skin.sharedMaterial = material;
			SetFur(new Color(0.13333334f, 0.43137255f, 0.41960785f, 1f));
			return true;

			Transform Bone(string name)
			{
				// Rebuilt from native code (token 0x060007C9, RVA 0x28120B0)
				return bones.TryGetValue(name, out Transform bone) ? bone : null;
			}
		}

		private void MeasureNameTagSpot()
		{
			// Rebuilt from native code (token 0x060007A0, RVA 0x2812C84)
			Vector3 chestLocal = transform.InverseTransformPoint(chest.position);
			float eye = modelEye;
			Mesh baked = new Mesh();
			skin.BakeMesh(baked, true);
			Vector3[] vertices = baked.vertices;
			float height = chestLocal.y + eye * 0.04f;
			float front = chestLocal.z;
			float band = eye * 0.05f;
			foreach (Vector3 vertex in vertices)
			{
				Vector3 p = transform.InverseTransformPoint(skin.transform.position + skin.transform.rotation * vertex);
				if (Mathf.Abs(p.y - height) < band && Mathf.Abs(p.x - chestLocal.x) < band)
				{
					front = Mathf.Max(front, p.z);
				}
			}
			Destroy(baked);
			Vector3 spot = transform.TransformPoint(new Vector3(chestLocal.x, height, front + modelEye * 0.006f));
			tagSpot = chest.InverseTransformPoint(spot);
			tagTurn = Quaternion.Inverse(chest.rotation) * transform.rotation * Quaternion.LookRotation(Vector3.back, Vector3.up);
		}

		public void Turned(float degrees)
		{
			// Rebuilt from native code (token 0x060007A3, RVA 0x2813124)
			bodyYaw += degrees;
		}

		public bool WearNameTag(Transform tag)
		{
			// Rebuilt from native code (token 0x060007A4, RVA 0x2813134)
			if (!chest)
			{
				return false;
			}
			tag.SetParent(chest, false);
			tag.localPosition = tagSpot;
			tag.localRotation = tagTurn;
			tag.localScale = Vector3.one * (transform.lossyScale.x / chest.lossyScale.x);
			return true;
		}

		private Arm BuildArm(Func<string, Transform> bone, string side)
		{
			// Rebuilt from native code (token 0x060007A5, RVA 0x2812128)
			Arm arm = new Arm();
			arm.shoulder = bone("Shoulder." + side);
			arm.hand = bone("Hand." + side);
			Transform upper = bone("UpperArm." + side);
			Transform lower = bone("LowerArm." + side);
			Transform index = bone("IndexProximal." + side);
			Transform middle = bone("MiddleProximal." + side);
			if (!arm.shoulder || !upper || !lower || !arm.hand || !index || !middle)
			{
				return null;
			}
			Vector3 forward = transform.forward;
			Vector3 elbow = lower.position;
			Vector3 wrist = arm.hand.position;
			float scale = transform.lossyScale.x;
			arm.upperLength = Vector3.Distance(upper.position, elbow) / scale;
			arm.lowerLength = Vector3.Distance(elbow, wrist) / scale;
			arm.outward = transform.InverseTransformPoint(upper.position).x < 0f ? -1f : 1f;
			Vector3 knuckles = (middle.position - wrist).normalized;
			Vector3 thumbSide = Vector3.ProjectOnPlane(index.position - middle.position, knuckles).normalized;
			Vector3 palm = Vector3.Cross(knuckles, thumbSide).normalized;
			if (Vector3.Dot(palm, -transform.right * arm.outward) < 0f)
			{
				palm = -palm;
			}
			arm.knuckles = arm.hand.InverseTransformDirection(knuckles);
			arm.thumbSide = arm.hand.InverseTransformDirection(thumbSide);
			arm.palm = arm.hand.InverseTransformDirection(palm);
			arm.fistCenter = arm.hand.InverseTransformPoint(Vector3.Lerp(wrist, middle.position, 0.7f) + palm * 0.01f * scale);
			float reach = Vector3.Distance(wrist, middle.position);
			arm.handLength = arm.hand.InverseTransformVector(knuckles * reach).magnitude;
			arm.palmPoint = arm.hand.InverseTransformPoint(Vector3.Lerp(wrist, middle.position, 0.85f) + (thumbSide * 0.2f + palm * 0.196f) * reach);
			Finger(arm, side, "Index", 0, palm, 70f, 95f, 65f);
			Finger(arm, side, "Middle", 1, palm, 75f, 100f, 70f);
			Finger(arm, side, "Thumb", 2, palm, 35f, 50f, 45f);
			arm.wrap = new float[arm.joints.Count];
			arm.upperPivot = Pivot("Upper Arm Pivot " + side, arm.shoulder, upper.position, elbow - upper.position, forward);
			arm.lowerPivot = Pivot("Lower Arm Pivot " + side, arm.shoulder, elbow, wrist - elbow, forward);
			arm.restTwist = Vector3.SignedAngle(arm.lowerPivot.forward, Vector3.ProjectOnPlane(thumbSide, arm.lowerPivot.up), arm.lowerPivot.up);
			arm.hand.SetParent(arm.shoulder, true);
			lower.SetParent(arm.lowerPivot, true);
			upper.SetParent(arm.upperPivot, true);
			return arm;
		}

		private static Transform Pivot(string name, Transform parent, Vector3 position, Vector3 along, Vector3 forward)
		{
			// Rebuilt from native code (token 0x060007A6, RVA 0x2813A0C)
			Transform pivot = new GameObject(name).transform;
			pivot.SetParent(parent, false);
			pivot.gameObject.layer = parent.gameObject.layer;
			Vector3 up = along.normalized;
			pivot.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, up).normalized, up));
			pivot.localScale = Vector3.one;
			return pivot;
		}

		private static void Finger(Arm arm, string side, string finger, int index, Vector3 palm, params float[] degrees)
		{
			// Rebuilt from native code (token 0x060007A7, RVA 0x2813344)
			List<Transform> bones = new List<Transform>();
			foreach (string part in new string[] { "Proximal", "Intermediate", "Distal" })
			{
				Transform bone = FindChild(arm.hand, finger + part + "." + side);
				if (!bone)
				{
					return;
				}
				bones.Add(bone);
			}
			for (int i = 0; i < bones.Count; i++)
			{
				Vector3 along = i + 1 < bones.Count ? bones[i + 1].position - bones[i].position : bones[i].position - bones[i - 1].position;
				Vector3 axis = Vector3.Cross(along, palm).normalized;
				arm.joints.Add((bones[i], Quaternion.Inverse(bones[i].rotation) * axis, degrees[i], index));
			}
			Transform distal = bones[bones.Count - 1];
			arm.tips[index] = (distal, distal.InverseTransformPoint(distal.position + (distal.position - bones[bones.Count - 2].position)));
		}

		public static Vector3 ReadFingers(bool left)
		{
			// Rebuilt from native code (token 0x060007A8, RVA 0x2813E4C)
			return new Vector3(XRHands.Trigger(left), XRHands.Grip(left), XRHands.ThumbDown(left) ? 1f : 0f);
		}

		public static int PackFingers(Vector3 left, Vector3 right)
		{
			// Rebuilt from native code (token 0x060007A9, RVA 0x2813EE8)
			return Q(left.x) | Q(left.y) << 5 | Q(left.z) << 10 | Q(right.x) << 15 | Q(right.y) << 20 | Q(right.z) << 25;

			static int Q(float v)
			{
				// Rebuilt from native code (token 0x060007C2, RVA 0x2813FC4)
				return Mathf.Clamp(Mathf.RoundToInt(v * 31f), 0, 31);
			}
		}

		public static void UnpackFingers(int packed, out Vector3 left, out Vector3 right)
		{
			// Rebuilt from native code (token 0x060007AA, RVA 0x28140B4)
			left = new Vector3(D(0), D(5), D(10));
			right = new Vector3(D(15), D(20), D(25));

			float D(int shift)
			{
				// Rebuilt from native code (token 0x060007C3, RVA 0x281417C)
				return ((packed >> shift) & 31) / 31f;
			}
		}

		private static Transform FindChild(Transform root, string name)
		{
			// Rebuilt from native code (token 0x060007AB, RVA 0x2813D84)
			foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
			{
				if (t.name == name)
				{
					return t;
				}
			}
			return null;
		}

		private static void SetLayer(GameObject go, int layer)
		{
			// Rebuilt from native code (token 0x060007AC, RVA 0x2810CFC)
			foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = layer;
			}
		}

		public void SetFur(Color fur)
		{
			// Rebuilt from native code (token 0x060007AD, RVA 0x2810F24)
			if (fur == shownFur || !material)
			{
				return;
			}
			shownFur = fur;
			Texture2D texture = FurTexture(fur);
			if (texture)
			{
				material.mainTexture = texture;
			}
			if (material.HasProperty("_BaseMap"))
			{
				material.SetTexture("_BaseMap", texture);
			}
			if (reflection)
			{
				reflection.SetFur(fur);
			}
		}

		private static Texture2D FurTexture(Color fur)
		{
			// Rebuilt from native code (token 0x060007AE, RVA 0x2814198)
			Color32 key = fur;
			if (furTextures.TryGetValue(key, out Texture2D cached) && cached)
			{
				return cached;
			}
			if (albedoPixels == null)
			{
				Texture2D albedo = Resources.Load<Texture2D>(AlbedoPath);
				if (!albedo || !albedo.isReadable)
				{
					return albedo;
				}
				albedoPixels = albedo.GetPixels32();
				albedoWidth = albedo.width;
				albedoHeight = albedo.height;
			}
			float r = Mathf.GammaToLinearSpace(fur.r);
			float g = Mathf.GammaToLinearSpace(fur.g);
			float b = Mathf.GammaToLinearSpace(fur.b);
			Color32[] pixels = new Color32[albedoPixels.Length];
			for (int i = 0; i < pixels.Length; i++)
			{
				Color32 c = albedoPixels[i];
				if (c.a > 0)
				{
					float lr = Mathf.GammaToLinearSpace(c.r / 255f);
					float lg = Mathf.GammaToLinearSpace(c.g / 255f);
					float lb = Mathf.GammaToLinearSpace(c.b / 255f);
					float luminance = (lr * 0.2126f + lg * 0.7152f + lb * 0.0722f) / DefaultFurLuminance;
					float a = c.a / 255f;
					c.r = ToByte(Mathf.Lerp(lr, r * luminance, a));
					c.g = ToByte(Mathf.Lerp(lg, g * luminance, a));
					c.b = ToByte(Mathf.Lerp(lb, b * luminance, a));
				}
				c.a = byte.MaxValue;
				pixels[i] = c;
			}
			Texture2D texture = new Texture2D(albedoWidth, albedoHeight, TextureFormat.RGBA32, true, false);
			texture.name = "Gorilla Fur " + key;
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.SetPixels32(pixels);
			texture.Apply(true, true);
			furTextures[key] = texture;
			return texture;
		}

		private static byte ToByte(float linear)
		{
			// Rebuilt from native code (token 0x060007AF, RVA 0x2814740)
			return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(Mathf.Clamp01(linear)) * 255f), 0, 255);
		}

		internal static Material NewFurMaterial(Color fur)
		{
			// Rebuilt from native code (token 0x060007B0, RVA 0x2814854)
			Material template = Local ? Local.material : null;
			Material material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
			material.name = "Gorilla Fur";
			Texture2D texture = FurTexture(fur);
			if (texture)
			{
				material.mainTexture = texture;
				if (material.HasProperty("_BaseMap"))
				{
					material.SetTexture("_BaseMap", texture);
				}
			}
			return material;
		}

		private void ProfileChanged()
		{
			// Rebuilt from native code (token 0x060007B1, RVA 0x2814AF0)
			SetFur(PlayerProfile.Color);
		}

		public Transform HandFrame(bool left, out Vector3 wrist, out Vector3 knuckles, out Vector3 thumbSide, out Vector3 back, out float unit)
		{
			// Rebuilt from native code (token 0x060007B4, RVA 0x2814BDC)
			Arm arm = left ? leftArm : rightArm;
			unit = transform.lossyScale.x;
			if (arm == null || !arm.hand)
			{
				wrist = default;
				knuckles = default;
				thumbSide = default;
				back = default;
				return null;
			}
			wrist = arm.hand.position;
			knuckles = arm.hand.TransformDirection(arm.knuckles).normalized;
			thumbSide = arm.hand.TransformDirection(arm.thumbSide).normalized;
			back = -arm.hand.TransformDirection(arm.palm).normalized;
			return arm.hand;
		}

		public void SetVisible(bool visible)
		{
			// Rebuilt from native code (token 0x060007B5, RVA 0x2810DB8)
			if (skin)
			{
				skin.enabled = visible;
			}
			foreach (Renderer renderer in hiddenRenderers)
			{
				if (renderer)
				{
					renderer.enabled = false;
				}
			}
			if (belt)
			{
				belt.SetVisible(visible);
			}
			if (reflection)
			{
				reflection.SetVisible(visible);
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060007B6, RVA 0x2814F78)
			if (Local == this)
			{
				Local = null;
				PlayerProfile.Changed -= ProfileChanged;
			}
			if (reflection)
			{
				Destroy(reflection.gameObject);
			}
			if (material)
			{
				Destroy(material);
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060007B7, RVA 0x28151D0)
			if (!headTarget || !leftTarget || !rightTarget)
			{
				if (Local == this)
				{
					Destroy(gameObject);
				}
				return;
			}
			ShallowsTuning tuning = ShallowsTuning.I;
			float lean = tuning ? tuning.bodyLean : 20f;
			float eye = Mathf.Max(0.1f, eyeHeight != null ? eyeHeight() : 0.55f);
			float size = tuning ? tuning.bodySize : 1.15f;
			Vector3 facing = Vector3.ProjectOnPlane(headTarget.forward, Vector3.up);
			if (facing.sqrMagnitude < 0.04f)
			{
				facing = Vector3.ProjectOnPlane(headTarget.forward.y < 0f ? headTarget.up : -headTarget.up, Vector3.up);
			}
			float yaw = MathF.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
			float pitch = MathF.Asin(Mathf.Clamp(-headTarget.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
			float hold = Mathf.Clamp01((pitch - HoldFrom) / (HoldFull - HoldFrom));
			if (!placed)
			{
				bodyYaw = yaw;
				bodyHold = hold;
			}
			bodyHold = Mathf.MoveTowards(bodyHold, hold, Time.deltaTime * (hold > bodyHold ? 8f : 3f));
			float maxTwist = Mathf.Lerp(40f, 90f, bodyHold);
			bodyYaw += (1f - MathF.Exp(Time.deltaTime * ((1f - bodyHold) * -6f))) * Mathf.DeltaAngle(bodyYaw, yaw);
			placed = true;
			bodyYaw = yaw - Mathf.Clamp(Mathf.DeltaAngle(bodyYaw, yaw), -maxTwist, maxTwist);
			Quaternion turn = Quaternion.Euler(0f, bodyYaw, 0f);
			float scale = eye / modelEye * size;
			transform.localScale = Vector3.one * scale;
			transform.rotation = turn * Quaternion.Euler(lean, 0f, 0f);
			foreach ((Transform bone, Vector3 position, Quaternion rotation) in pose)
			{
				bone.SetLocalPositionAndRotation(position, rotation);
			}
			if (Local == this)
			{
				head.localScale = tuning && tuning.showOwnHead ? headScale : headScale * 0.0001f;
			}
			Quaternion look = headTarget.rotation;
			neck.rotation = Quaternion.Slerp(neck.rotation, look * neckRest, 0.4f);
			head.rotation = look * headRest;
			Vector3 eyes = headTarget.position - head.rotation * eyeOffset * scale;
			transform.position += eyes - head.position;
			float handPitch = tuning ? tuning.handPitch : 0f;
			Reach(leftArm, leftTarget, turn, scale, handPitch);
			Reach(rightArm, rightTarget, turn, scale, handPitch);
			if (Local == this || isReflection)
			{
				leftFingers = ReadFingers(true);
				rightFingers = ReadFingers(false);
			}
			Grip(leftArm);
			Grip(rightArm);
			Curl(leftArm, leftFingers);
			Curl(rightArm, rightFingers);
			if (isReflection)
			{
				micLevel = VoiceLevel.Follow(micLevel, NetworkManager.I ? NetworkManager.I.MicLevel : 0f);
			}
			if (screamShape < 0 || (!voice && !isReflection))
			{
				return;
			}
			skin.SetBlendShapeWeight(screamShape, (voice ? voice.Level : micLevel) * 100f);
		}

		private static void Curl(Arm arm, Vector3 target)
		{
			// Rebuilt from native code (token 0x060007B8, RVA 0x28171B4)
			arm.curl = Vector3.Lerp(arm.curl, target, 1f - MathF.Exp(Time.deltaTime * -20f));
			for (int i = 0; i < arm.joints.Count; i++)
			{
				(Transform bone, Vector3 axis, float degrees, int finger) = arm.joints[i];
				float curl = Mathf.Lerp(RelaxedCurl, 1f, arm.curl[finger]);
				if (arm.hold > 0f)
				{
					curl = Mathf.Lerp(curl, arm.wrap[i], arm.hold);
				}
				bone.localRotation = bone.localRotation * Quaternion.AngleAxis(degrees * curl, axis);
			}
		}

		public void Hold(bool left, HeldGrip grip, Transform item = null)
		{
			// Rebuilt from native code (token 0x060007B9, RVA 0x28174A4)
			Arm arm = left ? leftArm : rightArm;
			if (arm == null)
			{
				return;
			}
			arm.grip = grip;
			arm.item = grip != null ? item : null;
			if (reflection)
			{
				reflection.Hold(left, grip);
			}
		}

		public bool GripPose(bool left, HeldGrip grip, out Vector3 position, out Quaternion rotation)
		{
			// Rebuilt from native code (token 0x060007BA, RVA 0x281757C)
			Arm arm = left ? leftArm : rightArm;
			if (arm == null || !arm.hand || grip == null)
			{
				position = default;
				rotation = Quaternion.identity;
				return false;
			}
			Place(arm, grip, out position, out rotation, out _, out _);
			return true;
		}

		private static void Place(Arm arm, HeldGrip grip, out Vector3 position, out Quaternion rotation, out Vector3 center, out Vector3 thumbSide)
		{
			// Rebuilt from native code (token 0x060007BB, RVA 0x28176C0)
			Vector3 knuckles = arm.hand.TransformDirection(arm.knuckles).normalized;
			thumbSide = arm.hand.TransformDirection(arm.thumbSide).normalized;
			Vector3 palm = arm.hand.TransformDirection(arm.palm).normalized;
			grip.Place(arm.hand.TransformPoint(arm.palmPoint), knuckles, thumbSide, palm, out position, out rotation, out center);
		}

		private void Grip(Arm arm)
		{
			// Rebuilt from native code (token 0x060007BC, RVA 0x2816F7C)
			arm.hold = Mathf.MoveTowards(arm.hold, arm.grip != null ? 1f : 0f, Time.deltaTime * 8f);
			if (arm.grip == null)
			{
				return;
			}
			Place(arm, arm.grip, out Vector3 position, out Quaternion rotation, out Vector3 center, out Vector3 thumbSide);
			if (arm.item)
			{
				arm.item.SetPositionAndRotation(position, rotation);
			}
			Vector3 scale = arm.hand.lossyScale;
			if (arm.wrapFor == arm.grip && Mathf.Abs(arm.wrapScale - scale.x) < scale.x * 0.01f)
			{
				return;
			}
			arm.wrapFor = arm.grip;
			arm.wrapScale = scale.x;
			Wrap(arm, arm.grip, center, thumbSide, scale.x * (arm.handLength * FingerThickness));
		}

		private static void Wrap(Arm arm, HeldGrip grip, Vector3 center, Vector3 thumbSide, float fingerRadius)
		{
			// Rebuilt from native code (token 0x060007BD, RVA 0x2817B98)
			for (int finger = 0; finger < 3; finger++)
			{
				chainJoints.Clear();
				chainRest.Clear();
				chainClosed.Clear();
				for (int j = 0; j < arm.joints.Count; j++)
				{
					if (arm.joints[j].finger != finger)
					{
						continue;
					}
					chainJoints.Add(j);
					chainRest.Add(arm.joints[j].bone.localRotation);
					chainClosed.Add(RelaxedCurl);
				}
				if (chainJoints.Count == 0)
				{
					continue;
				}
				for (int k = 0; k < chainJoints.Count; k++)
				{
					for (int step = 1; step <= WrapSteps; step++)
					{
						float open = chainClosed[k];
						chainClosed[k] = Mathf.Lerp(RelaxedCurl, 1f, (float)step / WrapSteps);
						if (Touches(arm, finger, k, grip, center, thumbSide, fingerRadius))
						{
							chainClosed[k] = open;
							break;
						}
					}
				}
				for (int k = 0; k < chainJoints.Count; k++)
				{
					arm.wrap[chainJoints[k]] = chainClosed[k];
					arm.joints[chainJoints[k]].bone.localRotation = chainRest[k];
				}
			}
		}

		private static bool Touches(Arm arm, int finger, int j, HeldGrip grip, Vector3 center, Vector3 thumbSide, float fingerRadius)
		{
			// Rebuilt from native code (token 0x060007BE, RVA 0x2818190)
			for (int k = 0; k < chainJoints.Count; k++)
			{
				(Transform bone, Vector3 axis, float degrees, int _) = arm.joints[chainJoints[k]];
				bone.localRotation = chainRest[k] * Quaternion.AngleAxis(degrees * chainClosed[k], axis);
			}
			Vector3 from = arm.joints[chainJoints[j]].bone.position;
			for (int k = j + 1; k <= chainJoints.Count; k++)
			{
				Vector3 to;
				if (k < chainJoints.Count)
				{
					to = arm.joints[chainJoints[k]].bone.position;
				}
				else
				{
					if (!arm.tips[finger].distal)
					{
						return false;
					}
					to = arm.tips[finger].distal.TransformPoint(arm.tips[finger].tip);
				}
				if (grip.Distance(to, center, thumbSide) < fingerRadius || grip.Distance((from + to) * 0.5f, center, thumbSide) < fingerRadius)
				{
					return true;
				}
				from = to;
			}
			return false;
		}

		private void Reach(Arm arm, Transform target, Quaternion yaw, float scale, float pitch)
		{
			// Rebuilt from native code (token 0x060007BF, RVA 0x28160C0)
			Quaternion rotation = target.rotation;
			Vector3 forward = rotation * Vector3.forward;
			Quaternion hand = Quaternion.LookRotation(Quaternion.AngleAxis(-(arm.outward * pitch), forward) * (rotation * Vector3.down), forward) * Quaternion.Inverse(Quaternion.LookRotation(arm.knuckles, arm.thumbSide));
			Vector3 wrist = target.position - hand * Vector3.Scale(arm.fistCenter, arm.hand.lossyScale);
			Vector3 toWrist = wrist - arm.upperPivot.position;
			Quaternion shrug = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(arm.upperPivot.up, toWrist), 0.3f);
			arm.shoulder.rotation = Quaternion.RotateTowards(Quaternion.identity, shrug, 30f) * arm.shoulder.rotation;
			Vector3 shoulder = arm.upperPivot.position;
			Vector3 span = wrist - shoulder;
			float distance = span.magnitude;
			Vector3 along = distance > 1e-05f ? span / distance : Vector3.down;
			float upper = arm.upperLength * scale;
			float lower = arm.lowerLength * scale;
			float reach = Mathf.Clamp(distance, Mathf.Abs(upper - lower) + 0.0001f, (upper + lower) * 0.9999f);
			float cos = Mathf.Clamp((upper * upper + reach * reach - lower * lower) / (2f * upper * reach), -1f, 1f);
			Vector3 bend = Vector3.ProjectOnPlane(yaw * new Vector3(arm.outward * 0.6f, -0.4f, -1f), along);
			if (bend.sqrMagnitude < 1e-06f)
			{
				bend = Vector3.ProjectOnPlane(Vector3.down, along);
			}
			Vector3 bendDir = bend.normalized;
			Vector3 elbow = shoulder + along * (upper * cos) + bendDir * (upper * Mathf.Sqrt(1f - cos * cos));
			Vector3 upperDir = (elbow - shoulder).normalized;
			Vector3 upperForward = Vector3.ProjectOnPlane(-bendDir, upperDir).normalized;
			arm.upperPivot.rotation = Quaternion.LookRotation(upperForward, upperDir);
			wrist = shoulder + along * reach;
			Vector3 lowerDir = (wrist - elbow).normalized;
			Vector3 lowerForward = Vector3.Cross(Vector3.Cross(upperDir, upperForward), lowerDir).normalized;
			Vector3 twisted = Vector3.ProjectOnPlane(forward, lowerDir);
			if (twisted.sqrMagnitude > 0.0001f)
			{
				float twist = Mathf.Clamp(Mathf.DeltaAngle(arm.restTwist, Vector3.SignedAngle(lowerForward, twisted, lowerDir)) * 0.6f, -MaxForearmTwist, MaxForearmTwist);
				lowerForward = Quaternion.AngleAxis(twist, lowerDir) * lowerForward;
			}
			arm.lowerPivot.SetPositionAndRotation(elbow, Quaternion.LookRotation(lowerForward, lowerDir));
			arm.hand.SetPositionAndRotation(wrist, hand);
		}
	}
}
