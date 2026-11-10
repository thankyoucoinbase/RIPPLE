using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Shallows
{
	[DefaultExecutionOrder(-40)]
	public class PlayerRig : MonoBehaviour
	{
		public Transform head;

		[Tooltip("The tracked controllers.")]
		public Transform leftController;

		[Tooltip("The tracked controllers.")]
		public Transform rightController;

		[Tooltip("The visible hands (Gorilla Locomotion's hand followers).")]
		public Transform leftHand;

		[Tooltip("The visible hands (Gorilla Locomotion's hand followers).")]
		public Transform rightHand;

		public GorillaLocomotion locomotion;

		public Rigidbody body;

		public CapsuleCollider bodyCollider;

		public SplashSource bodySplash;

		public SplashSource leftHandSplash;

		public SplashSource rightHandSplash;

		public ScreenFader fader;

		[Tooltip("Eyes to the bottom of the body collider. Must match the body collider.")]
		// Rebuilt from native code (token 0x0600082E, RVA 0x2823F18)
		public float eyeHeight = 0.55f;

		private const float BodyRadius = 0.2f;

		private const float HeadGap = 0.05f;

		public const float MinBodyHeight = 0.45000002f;

		private Collider[] colliders;

		public static PlayerRig I { get; private set; }

		public bool Frozen { get; private set; }

		public bool DesktopMode { get; set; }

		public Vector3 BodyBottom
		{
			get
			{
				// Rebuilt from native code (token 0x06000819, RVA 0x2822A18)
				return head.position + Vector3.down * eyeHeight;
			}
		}

		public bool InWater
		{
			get
			{
				// Rebuilt from native code (token 0x0600081A, RVA 0x281E280)
				return WaterVolume.IsUnderwater(BodyBottom + Vector3.up * 0.05f, out _);
			}
		}

		public bool Wet { get; private set; }

		public bool HandInWater
		{
			get
			{
				// Rebuilt from native code (token 0x0600081D, RVA 0x281E368)
				if (leftHand && !HandPresence.Away(true) && WaterVolume.IsUnderwater(leftHand.position, out _))
				{
					return true;
				}
				return rightHand && !HandPresence.Away(false) && WaterVolume.IsUnderwater(rightHand.position, out _);
			}
		}

		public Collider[] Colliders
		{
			get
			{
				// Rebuilt from native code (token 0x0600081E, RVA 0x2822AC4)
				return colliders ?? (colliders = GetComponentsInChildren<Collider>(true));
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600081F, RVA 0x2822B38)
			I = this;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000820, RVA 0x2822B90)
			List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
			SubsystemManager.GetSubsystems(subsystems);
			foreach (XRInputSubsystem subsystem in subsystems)
			{
				subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
			}
			ApeBody.CreateLocal(this);
			if (!GetComponent<StickTurn>())
			{
				gameObject.AddComponent<StickTurn>();
			}
		}

		public static void FitBody(CapsuleCollider capsule, float height)
		{
			// Rebuilt from native code (token 0x06000821, RVA 0x2822E08)
			capsule.direction = 1;
			capsule.radius = BodyRadius;
			capsule.height = Mathf.Max(MinBodyHeight, height) - HeadGap;
			capsule.center = new Vector3(0f, -(capsule.height * 0.5f + HeadGap), 0f);
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000822, RVA 0x2822EA4)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000823, RVA 0x2822F48)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x06000824, RVA 0x2822FEC)
			ShallowsTuning tuning = ShallowsTuning.I;
			if (tuning && bodyCollider && !Mathf.Approximately(eyeHeight, tuning.bodyHeight))
			{
				eyeHeight = Mathf.Max(MinBodyHeight, tuning.bodyHeight);
				FitBody(bodyCollider, eyeHeight);
			}
			if (Frozen || body.isKinematic)
			{
				return;
			}
			float drag = ShallowsTuning.I ? ShallowsTuning.I.waterDrag : 3f;
			bool inWater = InWater;
			body.linearDamping = inWater ? drag : 0f;
			if (inWater || !HandInWater)
			{
				return;
			}
			Vector3 velocity = body.linearVelocity;
			float keep = MathF.Exp(-drag * dt);
			body.linearVelocity = new Vector3(velocity.x * keep, velocity.y, velocity.z * keep);
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000825, RVA 0x28232A0)
			if (InWater || HandInWater)
			{
				Wet = true;
			}
			else if (TouchingDry())
			{
				Wet = false;
			}
		}

		private bool TouchingDry()
		{
			// Rebuilt from native code (token 0x06000826, RVA 0x28232E0)
			if (locomotion && locomotion.enabled && (locomotion.LeftTouching || locomotion.RightTouching))
			{
				return true;
			}
			return Physics.SphereCast(BodyBottom + Vector3.up * 0.22f, BodyRadius * 0.9f, Vector3.down, out _, 0.08f, -5, QueryTriggerInteraction.Ignore);
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000827, RVA 0x28234A4)
			Quaternion bodyRotation = (bodyCollider ? bodyCollider.transform : transform).rotation;
			if (leftHand && leftController)
			{
				leftHand.rotation = HandPresence.Away(true) ? bodyRotation : leftController.rotation;
			}
			if (rightHand && rightController)
			{
				rightHand.rotation = HandPresence.Away(false) ? bodyRotation : rightController.rotation;
			}
		}

		public void Freeze(bool frozen)
		{
			// Rebuilt from native code (token 0x06000828, RVA 0x28236BC)
			Frozen = frozen;
			locomotion.frozen = frozen;
			if (frozen)
			{
				if (!body.isKinematic)
				{
					body.linearVelocity = Vector3.zero;
				}
				body.isKinematic = true;
				return;
			}
			body.isKinematic = DesktopMode;
			if (!body.isKinematic)
			{
				body.linearVelocity = Vector3.zero;
			}
			locomotion.ResetPositions();
		}

		public void Carry(Vector3 delta, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000829, RVA 0x28237E4)
			if (delta.sqrMagnitude > 1e-12f)
			{
				transform.position += delta;
			}
			if (locomotion && locomotion.enabled)
			{
				locomotion.Shift(delta, velocity);
			}
		}

		public void MoveEyeTo(Vector3 eye)
		{
			// Rebuilt from native code (token 0x0600082A, RVA 0x2823920)
			transform.position += eye - head.position;
		}

		public void TurnToward(float yaw, float share)
		{
			// Rebuilt from native code (token 0x0600082B, RVA 0x28239B4)
			Vector3 pivot = head.position;
			transform.RotateAround(pivot, Vector3.up, Mathf.Clamp01(share) * Mathf.DeltaAngle(head.eulerAngles.y, yaw));
		}

		public void TurnBy(float degrees)
		{
			// Rebuilt from native code (token 0x0600082C, RVA 0x2823AF4)
			Vector3 pivot = head.position;
			transform.RotateAround(pivot, Vector3.up, degrees);
		}

		public void TeleportFeetTo(Vector3 feet, float yaw)
		{
			// Rebuilt from native code (token 0x0600082D, RVA 0x2823BA4)
			Vector3 pivot = head.position;
			transform.RotateAround(pivot, Vector3.up, Mathf.DeltaAngle(head.eulerAngles.y, yaw));
			transform.position += feet + Vector3.up * 0.02f - BodyBottom;
			if (!body.isKinematic)
			{
				body.linearVelocity = Vector3.zero;
			}
			Physics.SyncTransforms();
			locomotion.ResetPositions();
			Wet = false;
			if (bodySplash)
			{
				bodySplash.ResetTracking(0.6f);
			}
			if (leftHandSplash)
			{
				leftHandSplash.ResetTracking(0.6f);
			}
			if (rightHandSplash)
			{
				rightHandSplash.ResetTracking(0.6f);
			}
		}
	}
}
