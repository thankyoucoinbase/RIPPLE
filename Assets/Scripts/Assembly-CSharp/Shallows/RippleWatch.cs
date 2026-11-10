using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	[DefaultExecutionOrder(110)]
	public class RippleWatch : MonoBehaviour
	{
		private const string Folder = "Watch/";

		private const string Model = "RippleWatch";

		private const bool OnLeftHand = true;

		private static readonly Vector3 OnHand = new Vector3(0.035f, 0f, 0.028f);

		private static readonly string[] FaceParts = new string[15]
		{
			"Case", "Bezel", "Screen", "Respawn Ring", "Respawn Button", "Camera Key", "Camera Press", "Hold Frame", "Hold Bar", "Banana Icon",
			"Pearl Icon", "Timer Text", "Banana Text", "Pearl Text", "Button Press"
		};

		private const float LookScale = 1.6f;

		private const float GrowTime = 0.15f;

		private const float HoldTime = 1.2f;

		private static readonly Color Green = new Color(0.47058824f, 1f, 0.54901963f, 1f);

		private ApeBody ape;

		private Transform face;

		private Transform button;

		private Transform bar;

		private Vector3 faceScale;

		private Vector3 buttonRest;

		private Vector3 buttonPush;

		private Vector3 barRestPosition;

		private Vector3 barRestScale;

		private Vector3 barLeft;

		private int barAxis;

		private Quaternion faceRest;

		private Renderer screen;

		private Renderer holdFrame;

		private Renderer holdBar;

		private Renderer bananaIcon;

		private Renderer pearlIcon;

		private Renderer[] renderers;

		private TextMesh timerText;

		private TextMesh bananaText;

		private TextMesh pearlText;

		private Material material;

		private Material glow;

		private Bounds buttonBounds;

		private float shown;

		private bool looking;

		private bool holdLocked;

		private float held;

		private Transform cameraKey;

		private Vector3 keyFront;

		private Vector2 keyHalf;

		private Vector3 cameraRest;

		private Vector3 cameraPush;

		private bool cameraLocked;

		private bool keyReady;

		private float keyDepth;

		private string lastTimer;

		private string lastBananas;

		private string lastPearls;

		public static RippleWatch Local { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000831, RVA 0x2824390)
			Local = null;
		}

		public static RippleWatch Create(ApeBody ape)
		{
			// Rebuilt from native code (token 0x06000832, RVA 0x2824424)
			GameObject prefab = Resources.Load<GameObject>(Folder + Model);
			if (!prefab || !ape)
			{
				return null;
			}
			Transform hand = ape.HandFrame(OnLeftHand, out Vector3 wrist, out Vector3 knuckles, out Vector3 thumbSide, out Vector3 back, out float unit);
			if (!hand)
			{
				return null;
			}
			Transform root = new GameObject("Ripple Watch").transform;
			root.SetPositionAndRotation(wrist + (knuckles * OnHand.x + thumbSide * OnHand.y + back * OnHand.z) * unit, Quaternion.LookRotation(-back, Vector3.Cross(knuckles, back)));
			root.localScale = Vector3.one * unit;
			root.SetParent(hand, true);
			RippleWatch watch = root.gameObject.AddComponent<RippleWatch>();
			watch.ape = ape;
			Transform model = Instantiate(prefab, root, false).transform;
			model.name = Model;
			LifeguardProps.Align(root, model);
			if (!watch.Build(model))
			{
				Destroy(root.gameObject);
				return null;
			}
			SetLayer(root.gameObject, ape.gameObject.layer);
			Local = watch;
			return watch;
		}

		private bool Build(Transform model)
		{
			// Rebuilt from native code (token 0x06000833, RVA 0x2824960)
			Transform faceAnchor = LifeguardProps.Find(model, "Face");
			if (faceAnchor)
			{
				face = new GameObject("Face Pivot").transform;
				face.SetParent(model, false);
				face.SetPositionAndRotation(faceAnchor.position, transform.rotation);
				foreach (string part in FaceParts)
				{
					Transform found = LifeguardProps.Find(model, part);
					if (found)
					{
						found.SetParent(face, true);
					}
				}
			}
			button = LifeguardProps.Find(model, "Respawn Button");
			Transform frame = LifeguardProps.Find(model, "Hold Frame");
			Transform screenPart = LifeguardProps.Find(model, "Screen");
			bar = LifeguardProps.Find(model, "Hold Bar");
			if (!face || !button || !bar || !frame || !screenPart)
			{
				Debug.LogWarning("Shallows: the watch model is missing parts, so there's no watch.");
				return false;
			}
			material = MakeMaterial(false);
			glow = MakeMaterial(true);
			renderers = model.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer renderer in renderers)
			{
				renderer.sharedMaterial = material;
				renderer.shadowCastingMode = ShadowCastingMode.Off;
				renderer.receiveShadows = false;
			}
			screen = screenPart.GetComponent<Renderer>();
			holdFrame = frame.GetComponent<Renderer>();
			holdBar = bar.GetComponent<Renderer>();
			bananaIcon = Part(model, "Banana Icon");
			pearlIcon = Part(model, "Pearl Icon");
			foreach (Renderer renderer in new Renderer[] { holdFrame, holdBar, bananaIcon, pearlIcon })
			{
				if (renderer)
				{
					renderer.sharedMaterial = glow;
				}
			}
			faceScale = face.localScale;
			faceRest = face.localRotation;
			buttonRest = button.localPosition;
			Transform press = LifeguardProps.Find(model, "Button Press");
			buttonPush = button.parent.InverseTransformVector(transform.forward * 0.003f * transform.lossyScale.x);
			buttonBounds = button.GetComponent<MeshFilter>().sharedMesh.bounds;
			if (press)
			{
				buttonBounds.Encapsulate(button.InverseTransformPoint(press.position));
			}
			cameraKey = LifeguardProps.Find(model, "Camera Key");
			MeshFilter keyMesh = cameraKey ? cameraKey.GetComponent<MeshFilter>() : null;
			if (!keyMesh || !keyMesh.sharedMesh)
			{
				cameraKey = null;
			}
			else
			{
				cameraRest = cameraKey.localPosition;
				cameraPush = buttonPush;
				Bounds keyBounds = keyMesh.sharedMesh.bounds;
				Vector3 centre = keyBounds.center;
				Vector3 extents = keyBounds.extents;
				float minX = float.MaxValue;
				float minY = float.MaxValue;
				float maxX = float.MinValue;
				float maxY = float.MinValue;
				float near = float.MaxValue;
				for (int i = 0; i < 8; i++)
				{
					Vector3 corner = centre + new Vector3((i & 1) == 0 ? -extents.x : extents.x, (i & 2) == 0 ? -extents.y : extents.y, (i & 4) == 0 ? -extents.z : extents.z);
					Vector3 point = face.InverseTransformPoint(cameraKey.TransformPoint(corner));
					minX = Mathf.Min(minX, point.x);
					minY = Mathf.Min(minY, point.y);
					maxX = Mathf.Max(maxX, point.x);
					maxY = Mathf.Max(maxY, point.y);
					near = Mathf.Min(near, point.z);
				}
				keyFront = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, near);
				keyHalf = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
			}
			barRestPosition = bar.localPosition;
			barRestScale = bar.localScale;
			Bounds barBounds = bar.GetComponent<MeshFilter>().sharedMesh.bounds;
			Vector3 size = barBounds.size;
			barAxis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);
			Vector3 axis = Vector3.zero;
			axis[barAxis] = 1f;
			float length = Vector3.Dot(bar.TransformDirection(axis), transform.right) > 0f ? -size[barAxis] : size[barAxis];
			barLeft = barBounds.center + axis * (length * 0.5f);
			Font font = PixelFonts.Sign;
			float height = transform.lossyScale.x * 0.006f;
			timerText = Text(model, "Timer Text", font, height, TextAnchor.MiddleCenter);
			bananaText = Text(model, "Banana Text", font, height, TextAnchor.MiddleLeft);
			pearlText = Text(model, "Pearl Text", font, height, TextAnchor.MiddleLeft);
			Show(false, false);
			return true;
		}

		private static Renderer Part(Transform model, string name)
		{
			// Rebuilt from native code (token 0x06000834, RVA 0x2825DFC)
			Transform part = LifeguardProps.Find(model, name);
			return part ? part.GetComponent<Renderer>() : null;
		}

		private TextMesh Text(Transform model, string anchor, Font font, float height, TextAnchor alignment)
		{
			// Rebuilt from native code (token 0x06000835, RVA 0x2825EDC)
			Transform point = LifeguardProps.Find(model, anchor);
			TextMesh text = WorldText.Create(anchor, face, Vector3.zero, "", height, Green, alignment, alignment == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center);
			PixelFonts.Use(text, font, height, 1f);
			Transform textTransform = text.transform;
			textTransform.SetPositionAndRotation((point ? point : face).position, transform.rotation);
			Vector3 faceLossy = face.lossyScale;
			textTransform.localScale = new Vector3(1f / faceLossy.x, 1f / faceLossy.y, 1f / faceLossy.z);
			return text;
		}

		private static Material MakeMaterial(bool lit)
		{
			// Rebuilt from native code (token 0x06000836, RVA 0x2825B88)
			Texture2D albedo = Resources.Load<Texture2D>(Folder + Model + "_Albedo");
			var made = new Material(Shader.Find("Universal Render Pipeline/Lit"));
			made.name = lit ? "Watch Screen" : "Watch";
			made.SetTexture("_BaseMap", albedo);
			made.mainTexture = albedo;
			made.SetColor("_BaseColor", Color.white);
			made.SetFloat("_Smoothness", lit ? 0.5f : 0.15f);
			made.SetFloat("_Metallic", 0f);
			if (lit)
			{
				made.SetTexture("_EmissionMap", albedo);
				made.SetColor("_EmissionColor", Color.white * 0.9f);
				made.EnableKeyword("_EMISSION");
			}
			return made;
		}

		private static void SetLayer(GameObject go, int layer)
		{
			// Rebuilt from native code (token 0x06000837, RVA 0x2825890)
			go.layer = layer;
			foreach (Transform child in go.transform)
			{
				SetLayer(child.gameObject, layer);
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000838, RVA 0x282683C)
			if (Local == this)
			{
				Local = null;
			}
			if (material)
			{
				Destroy(material);
			}
			if (glow)
			{
				Destroy(glow);
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000839, RVA 0x28269FC)
			PlayerRig rig = PlayerRig.I;
			if (!ape || !rig)
			{
				return;
			}
			TextMesh wristText = GameManager.I ? GameManager.I.wristText : null;
			bool debug = ShallowsTuning.I && ShallowsTuning.I.showDebug;
			if (wristText && wristText.gameObject.activeSelf != debug && ape.Visible)
			{
				wristText.gameObject.SetActive(debug);
			}
			if (!ape.Visible)
			{
				SetRenderers(false);
				looking = false;
				held = 0f;
				shown = 0f;
				Show(false, false);
				return;
			}
			SetRenderers(true);
			Transform head = rig.head;
			float scale = rig.eyeHeight / 0.55f;
			Vector3 toEye = head.position - transform.position;
			float distance = toEye.magnitude;
			Vector3 direction = toEye.normalized;
			float watchFacing = Vector3.Dot(transform.forward, -direction);
			float headFacing = Vector3.Dot(head.forward, -direction);
			looking = headFacing > (looking ? 0.65f : 0.8f) && watchFacing > (looking ? 0.45f : 0.65f) && distance / scale < (looking ? 0.65f : 0.5f) && toEye.y / scale < (looking ? 0.38f : 0.28f);
			shown = Mathf.MoveTowards(shown, looking ? 1f : 0f, Time.deltaTime / GrowTime);
			face.localRotation = faceRest;
			float grow = shown * shown * (3f - 2f * shown);
			face.localScale = faceScale * Mathf.Lerp(1f, LookScale, grow);
			if (grow > 0f)
			{
				Vector3 forward = transform.forward;
				Vector3 up = Vector3.ProjectOnPlane(head.up, forward);
				if (up.sqrMagnitude > 0.0001f)
				{
					face.rotation = Quaternion.AngleAxis(grow * Vector3.SignedAngle(transform.up, up, -forward), -forward) * face.rotation;
				}
			}
			GameManager game = GameManager.I;
			bool safe = !game || game.InSafeZone;
			bool keyPressed = UpdateCameraKey(grow > 0.6f);
			UpdateButton(grow > 0.6f && !safe && game && game.Playing && !keyPressed);
			Show(grow > 0.6f, safe);
		}

		private void SetRenderers(bool enabled)
		{
			// Rebuilt from native code (token 0x0600083A, RVA 0x28272F8)
			foreach (Renderer renderer in renderers)
			{
				if (renderer && renderer != holdFrame && renderer != holdBar && renderer != bananaIcon && renderer != pearlIcon)
				{
					renderer.enabled = enabled;
				}
			}
		}

		private bool Touching(Transform part, Bounds bounds, out float distance)
		{
			// Rebuilt from native code (token 0x0600083B, RVA 0x2827C34)
			distance = float.MaxValue;
			if (!Fingertips.Get(false, out Vector3 tip))
			{
				return false;
			}
			Bounds reach = bounds;
			float scale = Mathf.Max(part.lossyScale.x, 1e-05f);
			reach.Expand(0.016f / scale);
			Vector3 local = part.InverseTransformPoint(tip);
			if (!reach.Contains(local))
			{
				return false;
			}
			distance = scale * Mathf.Sqrt(bounds.SqrDistance(local));
			return true;
		}

		private bool UpdateCameraKey(bool usable)
		{
			// Rebuilt from native code (token 0x0600083C, RVA 0x282746C)
			if (!cameraKey)
			{
				return false;
			}
			RippleCamera rippleCamera = RippleCamera.Local;
			bool over = false;
			bool pressed = false;
			if (usable && rippleCamera && Fingertips.Get(false, out Vector3 tip))
			{
				float scale = Mathf.Max(face.lossyScale.x, 1e-05f);
				Vector3 local = face.InverseTransformPoint(tip);
				float depth = scale * (keyFront.z - local.z);
				if (depth > -0.03f && depth < 0.012f)
				{
					float outsideX = (Mathf.Abs(local.x - keyFront.x) - keyHalf.x) * scale;
					float outsideY = (Mathf.Abs(local.y - keyFront.y) - keyHalf.y) * scale;
					if (outsideX <= 0.008f && outsideY <= 0.008f)
					{
						over = !Touching(button, buttonBounds, out float buttonDistance) || buttonDistance >= new Vector3(Mathf.Max(outsideX, 0f), Mathf.Max(outsideY, 0f), Mathf.Max(depth, 0f)).magnitude;
					}
				}
				pressed = over && depth < 0.008f;
				if (depth > 0.018f)
				{
					cameraLocked = false;
				}
				if (keyReady && depth > -0.03f && depth < 0.008f && keyDepth >= 0.008f && keyDepth - depth < 0.1f && over && !cameraLocked)
				{
					cameraLocked = true;
					XRHands.Haptic(false, 0.3f, 0.03f, true);
					if (SplashSystem.I)
					{
						SplashSystem.I.PlaySound(Sfx.ButtonPress(), cameraKey.position, 0.4f, 1.1f);
					}
					rippleCamera.WatchToggle();
				}
				keyDepth = depth;
				keyReady = true;
			}
			else
			{
				keyReady = false;
			}
			float push = pressed ? 1f : (rippleCamera && rippleCamera.Out ? 0.5f : 0f);
			cameraKey.localPosition = cameraRest + cameraPush * push;
			return over;
		}

		private void UpdateButton(bool usable)
		{
			// Rebuilt from native code (token 0x0600083D, RVA 0x28278F8)
			bool pressing = usable && Touching(button, buttonBounds, out _);
			float before = held;
			bool counting = pressing && !holdLocked;
			if (pressing)
			{
				held = holdLocked ? 0f : held + Time.deltaTime;
			}
			else
			{
				holdLocked = false;
				held = 0f;
			}
			button.localPosition = buttonRest + (pressing ? buttonPush : Vector3.zero);
			if (!counting)
			{
				return;
			}
			if (before == 0f)
			{
				Click(0.8f);
			}
			if (Mathf.FloorToInt(before / HoldTime * 4f) != Mathf.FloorToInt(held / HoldTime * 4f))
			{
				XRHands.Haptic(false, 0.2f, 0.02f, true);
			}
			if (held >= HoldTime)
			{
				held = 0f;
				holdLocked = true;
				Click(1.3f);
				XRHands.HapticBoth(0.5f, 0.08f, true);
				GameManager.I.WatchRespawn();
			}
		}

		private void Click(float pitch)
		{
			// Rebuilt from native code (token 0x0600083E, RVA 0x28281FC)
			XRHands.Haptic(false, 0.3f, 0.03f, true);
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ButtonPress(), button.position, 0.4f, pitch);
			}
		}

		private void Show(bool on, bool safe)
		{
			// Rebuilt from native code (token 0x0600083F, RVA 0x2826190)
			if (screen)
			{
				screen.sharedMaterial = on ? glow : material;
			}
			bool timer = on && !safe;
			if (holdFrame)
			{
				holdFrame.enabled = timer;
			}
			if (holdBar)
			{
				holdBar.enabled = timer && held > 0f;
			}
			bool wallet = on && safe;
			if (bananaIcon)
			{
				bananaIcon.enabled = wallet;
			}
			if (pearlIcon)
			{
				pearlIcon.enabled = wallet;
			}
			timerText.gameObject.SetActive(timer);
			bananaText.gameObject.SetActive(wallet);
			pearlText.gameObject.SetActive(wallet);
			if (timer)
			{
				GameManager game = GameManager.I;
				float time = game ? game.RunTime : 0f;
				int minutes = Mathf.FloorToInt(time / 60f);
				string text = string.Format("{0}:{1:00}", minutes, Mathf.FloorToInt(time - minutes * 60f));
				if (text != lastTimer)
				{
					lastTimer = text;
					timerText.text = text;
				}
				Vector3 scale = barRestScale;
				scale[barAxis] *= Mathf.Max(Mathf.Clamp01(held / HoldTime), 0.001f);
				bar.localScale = scale;
				bar.localPosition = barRestPosition + bar.localRotation * (Vector3.Scale(barRestScale, barLeft) - Vector3.Scale(scale, barLeft));
			}
			if (!wallet)
			{
				return;
			}
			string bananas = Wallet.Bananas.ToString();
			string pearls = Wallet.Pearls.ToString();
			if (bananas != lastBananas)
			{
				lastBananas = bananas;
				bananaText.text = bananas;
			}
			if (pearls != lastPearls)
			{
				lastPearls = pearls;
				pearlText.text = pearls;
			}
		}
	}
}
