using System;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(Renderer))]
	public class PokeButton : MonoBehaviour
	{
		public Action onPress;

		public Color color = new Color(0.25f, 0.6f, 0.9f, 1f);

		public Color pressedColor = new Color(1f, 0.9f, 0.3f, 1f);

		[Tooltip("Keep pressing while the hand stays in. Off for buttons that spend bananas.")]
		public bool repeat = true;

		[Tooltip("Where it moves (local) while pressed, so a key sinks in.")]
		public Vector3 pushLocal;

		[Tooltip("No click sound: whatever it presses plays its own.")]
		public bool quiet;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private Renderer rend;

		private MaterialPropertyBlock block;

		private bool latched;

		private bool held;

		private Vector3 back;

		private Vector3 prevLeft;

		private Vector3 prevRight;

		private bool hadLeft;

		private bool hadRight;

		private float nextRepeat;

		private float flashUntil;

		private bool lastColorPressed = true;

		private Vector3 restPosition;

		private bool pushed;

		private const float Cooldown = 0.1f;

		private const float LatchDepth = 0.3f;

		private static float leftReady;

		private static float rightReady;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060000A4, RVA 0x27132A0)
			leftReady = 0f;
			rightReady = 0f;
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x060000A5, RVA 0x27132F8)
			rend = GetComponent<Renderer>();
			block = new MaterialPropertyBlock();
			restPosition = transform.localPosition;
			SetColor(false);
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x060000A6, RVA 0x27134C4)
			lastColorPressed = true;
			SetColor(false);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060000A7, RVA 0x27134D4)
			Vector3 left = default;
			Vector3 right = default;
			if (PlayerRig.I)
			{
				Bounds bounds = rend.bounds;
				if (pushed)
				{
					bounds.center -= transform.parent ? transform.parent.TransformVector(pushLocal) : pushLocal;
				}
				bounds.Expand(0.016f);
				bool hasLeft = Fingertips.Get(true, out left);
				bool hasRight = Fingertips.Get(false, out right);
				bool inLeft = hasLeft && bounds.Contains(left);
				bool inRight = hasRight && bounds.Contains(right);
				if (latched)
				{
					if ((hasLeft && InLatch(left, bounds)) || (hasRight && InLatch(right, bounds)))
					{
						if ((inLeft || inRight) && repeat && Time.time >= nextRepeat)
						{
							Press(inLeft);
							nextRepeat = Time.time + 0.15f;
						}
					}
					else
					{
						latched = false;
					}
				}
				else if (inLeft || inRight)
				{
					bool useLeft = inLeft;
					if (inLeft && inRight)
					{
						useLeft = Time.time >= leftReady;
					}
					if (Time.time >= (useLeft ? leftReady : rightReady))
					{
						Vector3 from = useLeft ? (hadLeft ? prevLeft : left) : (hadRight ? prevRight : right);
						back = PressDirection(bounds.center, from);
						latched = true;
						Press(useLeft);
						nextRepeat = Time.time + 0.45f;
						if (useLeft)
						{
							leftReady = Time.time + Cooldown;
						}
						else
						{
							rightReady = Time.time + Cooldown;
						}
					}
				}
				prevLeft = left;
				prevRight = right;
				hadLeft = hasLeft;
				hadRight = hasRight;
				held = latched && (inLeft || inRight);
			}
			else
			{
				latched = false;
				held = false;
			}
			bool pressed = Time.time < flashUntil || held;
			SetColor(Time.time < flashUntil);
			if (!(pushLocal == Vector3.zero) && pushed != pressed)
			{
				pushed = pressed;
				transform.localPosition = restPosition + (pressed ? pushLocal : Vector3.zero);
			}
		}

		private bool InLatch(Vector3 p, Bounds b)
		{
			// Rebuilt from native code (token 0x060000A8, RVA 0x2713AEC)
			float depth = Vector3.Dot(p - b.center, back);
			if (depth > 0f)
			{
				p -= back * Mathf.Min(depth, LatchDepth);
				b.Expand(0.04f);
			}
			return b.Contains(p);
		}

		private Vector3 PressDirection(Vector3 center, Vector3 from)
		{
			// Rebuilt from native code (token 0x060000A9, RVA 0x2713BAC)
			Vector3 axis = pushLocal == Vector3.zero ? ThinAxis() : (transform.parent ? transform.parent.TransformVector(pushLocal) : pushLocal);
			Vector3 dir = axis.sqrMagnitude > 1E-12f ? axis.normalized : transform.forward;
			return Vector3.Dot(from - center, dir) > 0.0005f ? -dir : dir;
		}

		private Vector3 ThinAxis()
		{
			// Rebuilt from native code (token 0x060000AA, RVA 0x2713E70)
			MeshFilter filter = GetComponent<MeshFilter>();
			Vector3 size;
			bool worldSpace;
			if (filter && filter.sharedMesh)
			{
				Vector3 scale = transform.lossyScale;
				Vector3 meshSize = filter.sharedMesh.bounds.size;
				size = new Vector3(Mathf.Abs(scale.x) * meshSize.x, Mathf.Abs(scale.y) * meshSize.y, Mathf.Abs(scale.z) * meshSize.z);
				worldSpace = false;
			}
			else
			{
				size = rend.bounds.size;
				worldSpace = true;
			}
			Vector3 axis = size.x <= size.y && size.x <= size.z ? Vector3.right : (size.y <= size.z ? Vector3.up : Vector3.forward);
			return worldSpace ? axis : transform.TransformDirection(axis);
		}

		public void SetIdleColor(Color idle)
		{
			// Rebuilt from native code (token 0x060000AB, RVA 0x271410C)
			color = idle;
			bool pressed = lastColorPressed;
			lastColorPressed = !lastColorPressed;
			SetColor(pressed);
		}

		[ContextMenu("Press (test)")]
		private void PressFromMenu()
		{
			// Rebuilt from native code (token 0x060000AC, RVA 0x2714124)
			Press(false);
		}

		public void Press(bool leftHand)
		{
			// Rebuilt from native code (token 0x060000AD, RVA 0x2712CF0)
			onPress?.Invoke();
			flashUntil = Time.time + 0.12f;
			XRHands.Haptic(leftHand, 0.25f, 0.03f);
			if (!quiet && SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ButtonPress(), transform.position, 0.5f, 1f);
			}
		}

		private void SetColor(bool pressed)
		{
			// Rebuilt from native code (token 0x060000AE, RVA 0x27133C0)
			if (lastColorPressed != pressed)
			{
				lastColorPressed = pressed;
				rend.GetPropertyBlock(block);
				block.SetColor(BaseColorId, pressed ? pressedColor : color);
				rend.SetPropertyBlock(block);
			}
		}
	}
}
