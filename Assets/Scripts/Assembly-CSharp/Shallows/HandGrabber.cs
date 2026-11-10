using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(110)]
	public class HandGrabber : MonoBehaviour
	{
		public bool left;

		[Tooltip("The tracked controller, used for throw velocity.")]
		public Transform controller;

		public float grabRadius = 0.14f;

		[Tooltip("Grip above this grabs.")]
		public float grabThreshold = 0.65f;

		[Tooltip("Grip below this always lets go, however you squeezed.")]
		public float releaseThreshold = 0.35f;

		[Tooltip("Letting go starts as soon as the grip eases this much below the hardest you squeezed while holding, so a throw leaves the hand the moment your fingers start to open, not once they're nearly open.")]
		public float releaseDrop = 0.15f;

		[Tooltip("After letting go, the grip has to rise this much again (and pass the grab threshold) to grab, so the item you just threw isn't caught straight back while your fingers are still half closed.")]
		public float regripRise = 0.15f;

		[Tooltip("The throw uses the fastest the hand moved in this many recent frames (~14 ms each), so the hand slowing down as you let go doesn't sap the throw.")]
		public int throwLookBack = 5;

		[Tooltip("Most speed a wrist flick can add on top of the arm's (m/s).")]
		public float maxFlick = 6f;

		[Tooltip("A hand moving faster than this against your body (swinging your arms to move) can't take things out of your belt, so a squeezed grip while you run doesn't pull them out (m/s).")]
		public float beltTakeMaxSpeed = 1.5f;

		[Tooltip("Something taken out of your belt and let go again (not thrown) within this long goes straight back in (s).")]
		public float beltOopsTime = 1f;

		[Tooltip("...and only while the hand is still this close to a floatie (m).")]
		public float beltOopsReach = 0.3f;

		private const int History = 10;

		private const int Span = 2;

		private readonly Vector3[] positions = new Vector3[History];

		private readonly Vector3[] bodyPositions = new Vector3[History];

		private readonly Quaternion[] rotations = new Quaternion[History];

		private readonly float[] frameTimes = new float[History];

		private int newest;

		private bool gripping;

		private bool wasAway;

		private float peakGrip;

		private float lowGrip;

		private float tookFromBeltAt = -1f;

		public Throwable Held { get; private set; }

		private static Vector3 BodyPosition
		{
			get
			{
				// Rebuilt from native code (token 0x060007FB, RVA 0x281F524)
				if (!PlayerRig.I)
				{
					return Vector3.zero;
				}
				return PlayerRig.I.transform.position;
			}
		}

		private static int Index(int i)
		{
			// Rebuilt from native code (token 0x060007F1, RVA 0x281EE14)
			int index = i % History;
			return index < 0 ? index + History : index;
		}

		public void Turned(Vector3 pivot, Quaternion turn)
		{
			// Rebuilt from native code (token 0x060007F2, RVA 0x281EE44)
			for (int i = 0; i < History; i++)
			{
				positions[i] = pivot + turn * (positions[i] - pivot);
				bodyPositions[i] = turn * bodyPositions[i];
				rotations[i] = turn * rotations[i];
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060007F3, RVA 0x281EFF4)
			Transform hand = controller ? controller : transform;
			newest = Index(newest + 1);
			positions[newest] = hand.position;
			bodyPositions[newest] = hand.position - BodyPosition;
			rotations[newest] = hand.rotation;
			frameTimes[newest] = PhysicsClock.DeltaTime;
			if (ApeBody.Local)
			{
				ApeBody.Local.Hold(left, Held ? Held.Grip : null);
			}
			bool away = HandPresence.Away(left);
			if (away || wasAway)
			{
				for (int i = 0; i < History; i++)
				{
					positions[i] = hand.position;
					bodyPositions[i] = hand.position - BodyPosition;
					rotations[i] = hand.rotation;
				}
				wasAway = away;
				if (away)
				{
					return;
				}
			}
			float grip = XRHands.Grip(left);
			if (!gripping)
			{
				lowGrip = Mathf.Min(lowGrip, grip);
				if (grip > grabThreshold && grip > lowGrip + regripRise)
				{
					peakGrip = grip;
					gripping = true;
					TryGrab();
				}
				return;
			}
			peakGrip = Mathf.Max(peakGrip, grip);
			if (grip < releaseThreshold || grip < peakGrip - releaseDrop)
			{
				gripping = false;
				lowGrip = grip;
				Throw();
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060007F4, RVA 0x28201D4)
			if (Held)
			{
				Held.FollowHand(transform, left);
			}
		}

		private void TryGrab()
		{
			// Rebuilt from native code (token 0x060007F5, RVA 0x281F638)
			if (Held || (PlayerRig.I && PlayerRig.I.Frozen) || RippleCamera.Holds(left))
			{
				return;
			}
			float best = grabRadius;
			Throwable target = null;
			float bodySpeed = AverageVelocity(bodyPositions).magnitude;
			foreach (Throwable item in Throwable.All)
			{
				if (!item.CanGrab || (bodySpeed > beltTakeMaxSpeed && item.Stowed))
				{
					continue;
				}
				float distance = Vector3.Distance(item.transform.position, transform.position) - item.Radius;
				if (item.Stowed && Belt.I)
				{
					distance = Mathf.Min(distance, Belt.I.Reach(item, transform.position));
				}
				if (distance < best)
				{
					best = distance;
					target = item;
				}
			}
			if (!target)
			{
				return;
			}
			bool fromBelt = target.Stowed;
			Held = target;
			target.Grab(this);
			if (fromBelt)
			{
				tookFromBeltAt = Time.time;
				if (Belt.I)
				{
					Belt.I.Took(target);
					Debug.Log("Shallows: " + (left ? "left" : "right") + " hand took " + target.name + " out of the belt");
				}
			}
			else
			{
				tookFromBeltAt = -1f;
			}
			XRHands.Haptic(left, 0.3f, 0.05f);
		}

		private void Throw()
		{
			// Rebuilt from native code (token 0x060007F6, RVA 0x281FD88)
			if (!Held)
			{
				return;
			}
			Throwable item = Held;
			Vector3 velocity = ThrowVelocity(item.transform.position);
			Held = null;
			Vector3 bodyVelocity = AverageVelocity(bodyPositions);
			bool oops = tookFromBeltAt >= 0f && Time.time - tookFromBeltAt < beltOopsTime;
			tookFromBeltAt = -1f;
			Fuse fuse = item.GetComponent<Fuse>();
			if (fuse && fuse.SlotOnRelease())
			{
				return;
			}
			if (Belt.I && Belt.I.TryStash(item, transform.position, bodyVelocity))
			{
				return;
			}
			if (oops && Belt.I && bodyVelocity.magnitude < Belt.I.stashMaxSpeed && Belt.I.NearBelt(transform.position, beltOopsReach) && Belt.I.PutBack(item))
			{
				Debug.Log("Shallows: " + item.name + " went back in the belt (let go right after taking it out)");
				return;
			}
			item.Release(velocity);
		}

		public void Drop()
		{
			// Rebuilt from native code (token 0x060007F7, RVA 0x28208EC)
			gripping = false;
			lowGrip = XRHands.Grip(left);
			if (!Held)
			{
				return;
			}
			tookFromBeltAt = -1f;
			Held.Release(Vector3.zero);
			Held = null;
		}

		public void Forget(Throwable item)
		{
			// Rebuilt from native code (token 0x060007F8, RVA 0x2820A10)
			if (Held == item)
			{
				Held = null;
			}
		}

		private Vector3 ThrowVelocity(Vector3 itemPosition)
		{
			// Rebuilt from native code (token 0x060007F9, RVA 0x28203B4)
			int lookBack = Mathf.Max(Mathf.Min(throwLookBack, 7), 1);
			Vector3 best = Vector3.zero;
			float bestTime = 0f;
			float bestSpeed = -1f;
			int bestAt = newest;
			for (int k = 0; k < lookBack; k++)
			{
				int at = Index(newest - k);
				int from = Index(at - Span);
				float time = 0f;
				for (int j = 0; j < Span; j++)
				{
					time += frameTimes[Index(at - j)];
				}
				if (time < 0.0001f)
				{
					continue;
				}
				Vector3 velocity = (positions[at] - positions[from]) / time;
				float speed = velocity.sqrMagnitude;
				if (speed > bestSpeed)
				{
					best = velocity;
					bestTime = time;
					bestSpeed = speed;
					bestAt = at;
				}
			}
			if (bestSpeed < 0f)
			{
				return best;
			}
			Quaternion delta = rotations[bestAt] * Quaternion.Inverse(rotations[Index(bestAt - Span)]);
			delta.ToAngleAxis(out float angle, out Vector3 axis);
			if (angle > 180f)
			{
				angle -= 360f;
			}
			if (Mathf.Abs(angle) <= 0.5f || !float.IsFinite(axis.x))
			{
				return best;
			}
			Vector3 offset = itemPosition - positions[newest];
			if (offset.sqrMagnitude >= 0.25f)
			{
				return best;
			}
			Vector3 spin = axis.normalized * (angle * Mathf.Deg2Rad / bestTime);
			return best + Vector3.ClampMagnitude(Vector3.Cross(spin, offset), maxFlick);
		}

		private Vector3 AverageVelocity(Vector3[] from)
		{
			// Rebuilt from native code (token 0x060007FA, RVA 0x282026C)
			float time = 0f;
			for (int k = 0; k < 4; k++)
			{
				time += frameTimes[Index(newest - k)];
			}
			if (time > 0.0001f)
			{
				return (from[newest] - from[Index(newest - 4)]) / time;
			}
			return Vector3.zero;
		}
	}
}
