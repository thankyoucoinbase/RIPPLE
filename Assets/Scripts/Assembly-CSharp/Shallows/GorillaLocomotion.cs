using System;
using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(Rigidbody))]
	public class GorillaLocomotion : MonoBehaviour
	{
		[Header("Rig")]
		public SphereCollider headCollider;

		public CapsuleCollider bodyCollider;

		[Tooltip("Collision-resolved hand positions (what you see).")]
		public Transform leftHandFollower;

		[Tooltip("Collision-resolved hand positions (what you see).")]
		public Transform rightHandFollower;

		[Tooltip("The tracked controllers.")]
		public Transform leftHandTransform;

		[Tooltip("The tracked controllers.")]
		public Transform rightHandTransform;

		// Rebuilt from native code (token 0x060007ED, RVA 0x281ED90)
		public Vector3 leftHandOffset = new Vector3(0f, -0.01f, 0.03f);

		public Vector3 rightHandOffset = new Vector3(0f, -0.01f, 0.03f);

		[Tooltip("Surfaces your hands can push on. Leave out the player's own layer (Ignore Raycast).")]
		public LayerMask locomotionEnabledLayers = -5;

		[Header("Feel")]
		public int velocityHistorySize = 8;

		public float maxArmLength = 1.5f;

		public float unStickDistance = 1f;

		public float velocityLimit = 0.4f;

		public float minimumRaycastDistance = 0.05f;

		public float defaultSlideFactor = 0.03f;

		public float defaultPrecision = 0.995f;

		[HideInInspector]
		public bool disableMovement;

		[Tooltip("While frozen your hands can't move the body at all (used while a monster has you).")]
		public bool frozen;

		// Rebuilt from native code (token 0x060007D8, RVA 0x281BDF0; token 0x060007D9, RVA 0x281BEA0)
		public event Action<bool, Vector3, float> HandTouched;

		private Rigidbody playerRigidBody;

		private float lastInWater = -100f;

		private Vector3 lastLeftHandPosition;

		private Vector3 lastRightHandPosition;

		private Vector3 lastHeadPosition;

		private Vector3 lastPosition;

		private Vector3[] velocityHistory;

		private int velocityIndex;

		private Vector3 currentVelocity;

		private Vector3 denormalizedVelocityAverage;

		private bool wasLeftHandTouching;

		private bool wasRightHandTouching;

		private Vector3 prevLeftHand;

		private Vector3 prevRightHand;

		private bool leftAway;

		private bool rightAway;

		private int awayChanges = -1;

		private Vector3 lastLeftLocal;

		private Vector3 lastRightLocal;

		private const float JumpDistance = 0.6f;

		private Vector3 carryVelocity;

		private int carriedFrame = -10;

		private bool wasCarried;

		public bool LeftTouching
		{
			get
			{
				// Rebuilt from native code (token 0x060007DA, RVA 0x281BF50)
				return wasLeftHandTouching;
			}
		}

		public bool RightTouching
		{
			get
			{
				// Rebuilt from native code (token 0x060007DB, RVA 0x281BF58)
				return wasRightHandTouching;
			}
		}

		private float JumpMultiplier
		{
			get
			{
				// Rebuilt from native code (token 0x060007DC, RVA 0x281BF60)
				return ShallowsTuning.I ? ShallowsTuning.I.jumpMultiplier : 1.1f;
			}
		}

		private float MaxJumpSpeed
		{
			get
			{
				// Rebuilt from native code (token 0x060007DD, RVA 0x281C030)
				return ShallowsTuning.I ? ShallowsTuning.I.maxJumpSpeed : 6.5f;
			}
		}

		private float LaunchLimit
		{
			get
			{
				// Rebuilt from native code (token 0x060007DE, RVA 0x281C0F8)
				float sinceWater = Time.time - lastInWater;
				float limit = MaxJumpSpeed;
				if (sinceWater < 0.35f)
				{
					limit = Mathf.Min(limit, ShallowsTuning.I ? ShallowsTuning.I.waterJumpSpeed : 4f);
				}
				return limit;
			}
		}

		public Vector3 BodyMiddle
		{
			get
			{
				// Rebuilt from native code (token 0x060007E8, RVA 0x281EC34)
				return headCollider.transform.position + Vector3.down * 0.22f;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x060007DF, RVA 0x281C200)
			playerRigidBody = GetComponent<Rigidbody>();
			velocityHistory = new Vector3[Mathf.Max(velocityHistorySize, 1)];
			ResetPositions();
		}

		public void ResetPositions()
		{
			// Rebuilt from native code (token 0x060007E0, RVA 0x281C2A0)
			if (velocityHistory == null)
			{
				velocityHistory = new Vector3[Mathf.Max(velocityHistorySize, 1)];
			}
			Array.Clear(velocityHistory, 0, velocityHistory.Length);
			velocityIndex = 0;
			denormalizedVelocityAverage = Vector3.zero;
			lastLeftHandPosition = prevLeftHand = CurrentLeftHandPosition();
			lastRightHandPosition = prevRightHand = CurrentRightHandPosition();
			lastHeadPosition = headCollider.transform.position;
			lastPosition = transform.position;
			wasLeftHandTouching = false;
			wasRightHandTouching = false;
			leftHandFollower.position = lastLeftHandPosition;
			rightHandFollower.position = lastRightHandPosition;
			carriedFrame = -10;
			wasCarried = false;
			carryVelocity = Vector3.zero;
		}

		public void Shift(Vector3 delta, Vector3 platformVelocity)
		{
			// Rebuilt from native code (token 0x060007E1, RVA 0x281C4D0)
			carryVelocity = new Vector3(platformVelocity.x, 0f, platformVelocity.z);
			carriedFrame = Time.frameCount;
			lastLeftHandPosition += delta;
			lastRightHandPosition += delta;
			lastHeadPosition += delta;
			lastPosition += delta;
			prevLeftHand += delta;
			prevRightHand += delta;
			leftHandFollower.position += delta;
			rightHandFollower.position += delta;
		}

		public void Turned(Vector3 pivot, Quaternion turn)
		{
			// Rebuilt from native code (token 0x060007E2, RVA 0x281C5F8)
			lastLeftHandPosition = Spin(lastLeftHandPosition);
			lastRightHandPosition = Spin(lastRightHandPosition);
			lastHeadPosition = Spin(lastHeadPosition);
			lastPosition = Spin(lastPosition);
			prevLeftHand = Spin(prevLeftHand);
			prevRightHand = Spin(prevRightHand);
			leftHandFollower.position = Spin(leftHandFollower.position);
			rightHandFollower.position = Spin(rightHandFollower.position);

			// Rebuilt from native code (token 0x060007EE, RVA 0x281C73C)
			Vector3 Spin(Vector3 p)
			{
				return pivot + turn * (p - pivot);
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060007E3, RVA 0x281C78C)
			bool leftHandColliding = false;
			bool rightHandColliding = false;
			Vector3 finalPosition;
			RaycastHit hitInfo;

			if (Time.deltaTime <= 0f)
			{
				return;
			}
			float dt = PhysicsClock.DeltaTime;
			CheckHandsAway();
			if (frozen)
			{
				lastLeftHandPosition = prevLeftHand = CurrentLeftHandPosition();
				lastRightHandPosition = prevRightHand = CurrentRightHandPosition();
				lastHeadPosition = headCollider.transform.position;
				lastPosition = transform.position;
				leftHandFollower.position = lastLeftHandPosition;
				rightHandFollower.position = lastRightHandPosition;
				wasLeftHandTouching = false;
				wasRightHandTouching = false;
				return;
			}

			Vector3 firstIterationLeftHand = Vector3.zero;
			Vector3 firstIterationRightHand = Vector3.zero;

			bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);

			Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition + Vector3.down * 2f * 9.8f * dt * dt;
			if (!leftAway && IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
			{
				if (wasLeftHandTouching)
				{
					firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
				}
				else
				{
					firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();
				}
				playerRigidBody.linearVelocity = Vector3.zero;
				leftHandColliding = true;
			}

			distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition + Vector3.down * 2f * 9.8f * dt * dt;
			if (!rightAway && IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, true))
			{
				if (wasRightHandTouching)
				{
					firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
				}
				else
				{
					firstIterationRightHand = finalPosition - CurrentRightHandPosition();
				}
				playerRigidBody.linearVelocity = Vector3.zero;
				rightHandColliding = true;
			}

			Vector3 rigidBodyMovement;
			bool bothHands = (leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching);
			if (bothHands)
			{
				rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2f;
			}
			else
			{
				rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
			}

			if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision, out finalPosition, false))
			{
				rigidBodyMovement = finalPosition - lastHeadPosition;
				Vector3 headMovement = headCollider.transform.position - lastHeadPosition + rigidBodyMovement;
				if (Physics.Raycast(lastHeadPosition, headMovement, out hitInfo, headMovement.magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
				{
					rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
				}
			}

			if (rigidBodyMovement != Vector3.zero)
			{
				transform.position = transform.position + rigidBodyMovement;
			}

			lastHeadPosition = headCollider.transform.position;

			distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;
			if (!leftAway && IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !bothHands))
			{
				lastLeftHandPosition = finalPosition;
				leftHandColliding = true;
			}
			else
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
			}

			distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;
			if (!rightAway && IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out finalPosition, !bothHands))
			{
				lastRightHandPosition = finalPosition;
				rightHandColliding = true;
			}
			else
			{
				lastRightHandPosition = CurrentRightHandPosition();
			}

			StoreVelocities(dt);

			bool carried = carriedFrame == Time.frameCount;
			if (!playerRigidBody.isKinematic && wasCarried != carried)
			{
				playerRigidBody.linearVelocity += carried ? -carryVelocity : carryVelocity;
			}
			wasCarried = carried;

			if (PlayerRig.I && (PlayerRig.I.InWater || PlayerRig.I.HandInWater))
			{
				lastInWater = Time.time;
			}

			if ((leftHandColliding || rightHandColliding) && !disableMovement && denormalizedVelocityAverage.magnitude > velocityLimit)
			{
				float launchLimit = LaunchLimit;
				if (denormalizedVelocityAverage.magnitude * JumpMultiplier > launchLimit)
				{
					playerRigidBody.linearVelocity = denormalizedVelocityAverage.normalized * launchLimit;
				}
				else
				{
					playerRigidBody.linearVelocity = JumpMultiplier * denormalizedVelocityAverage;
				}
			}

			Vector3 headPosition = headCollider.transform.position;
			if (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headPosition, minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headPosition, out hitInfo, (CurrentLeftHandPosition() - headPosition).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				lastLeftHandPosition = CurrentLeftHandPosition();
				leftHandColliding = false;
			}

			if (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headPosition, minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headPosition, out hitInfo, (CurrentRightHandPosition() - headPosition).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
			{
				lastRightHandPosition = CurrentRightHandPosition();
				rightHandColliding = false;
			}

			leftHandFollower.position = lastLeftHandPosition;
			rightHandFollower.position = lastRightHandPosition;

			if (leftHandColliding && !wasLeftHandTouching)
			{
				HandTouched?.Invoke(true, lastLeftHandPosition, (CurrentLeftHandPosition() - prevLeftHand).magnitude / dt);
			}
			if (rightHandColliding && !wasRightHandTouching)
			{
				HandTouched?.Invoke(false, lastRightHandPosition, (CurrentRightHandPosition() - prevRightHand).magnitude / dt);
			}

			prevLeftHand = CurrentLeftHandPosition();
			prevRightHand = CurrentRightHandPosition();
			wasLeftHandTouching = leftHandColliding;
			wasRightHandTouching = rightHandColliding;
		}

		private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
		{
			// Rebuilt from native code (token 0x060007E4, RVA 0x281DD18)
			RaycastHit hitInfo;
			if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
			{
				Vector3 firstPosition = endPosition;
				Surface surface = hitInfo.collider.GetComponent<Surface>();
				float slipPercentage = surface != null ? surface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
				Vector3 movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
				if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
				{
					return true;
				}
				if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition, out hitInfo))
				{
					return true;
				}
				endPosition = firstPosition;
				return true;
			}
			if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
			{
				endPosition = startPosition;
				return true;
			}
			endPosition = Vector3.zero;
			return false;
		}

		private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
		{
			// Rebuilt from native code (token 0x060007E5, RVA 0x281E528)
			int layers = locomotionEnabledLayers.value;
			RaycastHit innerHit;
			if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1f - precision), layers, QueryTriggerInteraction.Ignore))
			{
				finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;
				if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * (1f - precision * precision), layers, QueryTriggerInteraction.Ignore))
				{
					finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0f, hitInfo.distance - sphereRadius * (1f - precision * precision));
					hitInfo = innerHit;
				}
				else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, layers, QueryTriggerInteraction.Ignore))
				{
					finalPosition = startPosition;
					hitInfo = innerHit;
					return true;
				}
				return true;
			}
			if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, layers, QueryTriggerInteraction.Ignore))
			{
				finalPosition = startPosition;
				return true;
			}
			finalPosition = Vector3.zero;
			return false;
		}

		public Vector3 CurrentLeftHandPosition()
		{
			// Rebuilt from native code (token 0x060007E6, RVA 0x281C460)
			if (leftAway)
			{
				return BodyMiddle;
			}
			return ClampToArm(PositionWithOffset(leftHandTransform, leftHandOffset));
		}

		public Vector3 CurrentRightHandPosition()
		{
			// Rebuilt from native code (token 0x060007E7, RVA 0x281C498)
			if (rightAway)
			{
				return BodyMiddle;
			}
			return ClampToArm(PositionWithOffset(rightHandTransform, rightHandOffset));
		}

		private void CheckHandsAway()
		{
			// Rebuilt from native code (token 0x060007E9, RVA 0x281D8F4)
			Vector3 left = transform.InverseTransformPoint(leftHandTransform.position);
			Vector3 right = transform.InverseTransformPoint(rightHandTransform.position);
			bool jumped = (left - lastLeftLocal).sqrMagnitude > JumpDistance * JumpDistance || (right - lastRightLocal).sqrMagnitude > JumpDistance * JumpDistance;
			lastLeftLocal = left;
			lastRightLocal = right;
			if (!jumped && awayChanges == HandPresence.Changes)
			{
				return;
			}
			awayChanges = HandPresence.Changes;
			leftAway = HandPresence.Away(true);
			rightAway = HandPresence.Away(false);
			Array.Clear(velocityHistory, 0, velocityHistory.Length);
			denormalizedVelocityAverage = Vector3.zero;
			lastLeftHandPosition = prevLeftHand = CurrentLeftHandPosition();
			lastRightHandPosition = prevRightHand = CurrentRightHandPosition();
			leftHandFollower.position = lastLeftHandPosition;
			rightHandFollower.position = lastRightHandPosition;
			wasLeftHandTouching = false;
			wasRightHandTouching = false;
			PlayerRig rig = PlayerRig.I;
			if (rig && rig.leftHandSplash)
			{
				rig.leftHandSplash.enabled = !leftAway;
			}
			if (rig && rig.rightHandSplash)
			{
				rig.rightHandSplash.enabled = !rightAway;
			}
		}

		private Vector3 ClampToArm(Vector3 hand)
		{
			// Rebuilt from native code (token 0x060007EA, RVA 0x281EA9C)
			Vector3 head = headCollider.transform.position;
			Vector3 reach = hand - head;
			if (reach.magnitude < maxArmLength)
			{
				return hand;
			}
			return head + reach.normalized * maxArmLength;
		}

		private static Vector3 PositionWithOffset(Transform t, Vector3 offset)
		{
			// Rebuilt from native code (token 0x060007EB, RVA 0x281EA20)
			return t.position + t.rotation * offset;
		}

		private void StoreVelocities(float dt)
		{
			// Rebuilt from native code (token 0x060007EC, RVA 0x281E15C)
			velocityIndex = (velocityIndex + 1) % velocityHistory.Length;
			Vector3 oldestVelocity = velocityHistory[velocityIndex];
			currentVelocity = (transform.position - lastPosition) / dt;
			denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / velocityHistory.Length;
			velocityHistory[velocityIndex] = currentVelocity;
			lastPosition = transform.position;
		}
	}
}
