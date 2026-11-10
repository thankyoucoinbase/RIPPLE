using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	[RequireComponent(typeof(Rigidbody))]
	public class Throwable : MonoBehaviour
	{
		public static readonly List<Throwable> All = new List<Throwable>();

		[Tooltip("Seconds in the water before it goes back to its starting spot.")]
		public float respawnAfterWater;

		[Tooltip("Where it sits in the hand when the gorilla model is missing. With the model, HeldGrip places it on the palm.")]
		public Vector3 holdOffset;

		[Tooltip("Goes back to its starting spot after a while in the water. Off for fuses, which stay where they land.")]
		public bool returnsHome = true;

		[Tooltip("Sinks away for good after a while in the water (or falling out of the world) instead of going back. On for the loose pebbles.")]
		public bool sinksAway;

		[Tooltip("Other players see a copy of it fly when you throw it.")]
		public bool shareThrow;

		[HideInInspector]
		public int netKind;

		public Func<bool> canGrab;

		// Rebuilt from native code (token 0x06000847, RVA 0x2829000; token 0x06000848, RVA 0x282909C)
		public event Action Grabbed;

		// Rebuilt from native code (token 0x06000849, RVA 0x2829138; token 0x0600084A, RVA 0x28291E8)
		public event Action<Vector3> Released;

		// Rebuilt from native code (token 0x0600084B, RVA 0x2829298; token 0x0600084C, RVA 0x2829334)
		public event Action Sank;

		private Rigidbody rb;

		private Collider col;

		private SplashSource splash;

		private HandGrabber holder;

		private Vector3 home;

		private Quaternion homeRotation;

		private float inWaterSince;

		private HeldGrip grip;

		public bool IsHeld
		{
			get
			{
				// Rebuilt from native code (token 0x0600084D, RVA 0x28293D0)
				return holder != null;
			}
		}

		public bool Stowed { get; private set; }

		public bool CanGrab
		{
			get
			{
				// Rebuilt from native code (token 0x06000850, RVA 0x2829440)
				if (IsHeld)
				{
					return false;
				}
				return canGrab == null || canGrab();
			}
		}

		public HandGrabber Holder
		{
			get
			{
				// Rebuilt from native code (token 0x06000851, RVA 0x2829480)
				return holder;
			}
		}

		public float Radius { get; private set; }

		public HeldGrip Grip
		{
			get
			{
				// Rebuilt from native code (token 0x0600085B, RVA 0x2829C28)
				if (grip == null)
				{
					grip = HeldGrip.For(this);
				}
				return grip;
			}
		}

		public bool Moving
		{
			get
			{
				// Rebuilt from native code (token 0x06000863, RVA 0x282A6E8)
				if (!rb || rb.isKinematic)
				{
					return false;
				}
				return rb.linearVelocity.sqrMagnitude > 0.01f;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000854, RVA 0x2829498)
			All.Clear();
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000855, RVA 0x2829530)
			rb = GetComponent<Rigidbody>();
			col = GetComponent<Collider>();
			splash = GetComponent<SplashSource>();
			home = transform.position;
			homeRotation = transform.rotation;
			Vector3 lossyScale = transform.lossyScale;
			float scale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
			Radius = col switch
			{
				SphereCollider sphere => scale * sphere.radius,
				CapsuleCollider capsule => scale * capsule.radius + 0.02f,
				_ => 0.04f
			};
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000856, RVA 0x282972C)
			All.Add(this);
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000857, RVA 0x2829878)
			All.Remove(this);
			PhysicsClock.Step -= PhysicsStep;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000858, RVA 0x2829974)
			IgnorePlayer();
		}

		private void IgnorePlayer()
		{
			// Rebuilt from native code (token 0x06000859, RVA 0x2829978)
			if (!col || !PlayerRig.I)
			{
				return;
			}
			foreach (Collider body in PlayerRig.I.Colliders)
			{
				if (body)
				{
					Physics.IgnoreCollision(col, body, true);
				}
			}
		}

		public void Grab(HandGrabber hand)
		{
			// Rebuilt from native code (token 0x0600085A, RVA 0x2829B1C)
			holder = hand;
			Stowed = false;
			rb.isKinematic = true;
			if (col)
			{
				col.enabled = false;
			}
			if (splash)
			{
				splash.enabled = false;
			}
			inWaterSince = -1f;
			Grabbed?.Invoke();
		}

		public void FollowHand(Transform hand, bool left)
		{
			// Rebuilt from native code (token 0x0600085C, RVA 0x2829CA8)
			ApeBody ape = ApeBody.Local;
			Vector3 position;
			Quaternion rotation;
			if (ape && ape.GripPose(left, Grip, out position, out rotation))
			{
				transform.SetPositionAndRotation(position, rotation);
				return;
			}
			HeldGrip.Fallback(hand, holdOffset, out position, out rotation);
			transform.SetPositionAndRotation(position, rotation);
		}

		public void Release(Vector3 velocity)
		{
			// Rebuilt from native code (token 0x0600085D, RVA 0x2829E44)
			holder = null;
			if (col)
			{
				col.enabled = true;
			}
			IgnorePlayer();
			rb.isKinematic = false;
			rb.linearVelocity = velocity;
			rb.angularVelocity = Random.insideUnitSphere * 6f;
			if (splash)
			{
				splash.enabled = true;
			}
			if (shareThrow && velocity.sqrMagnitude > 0.25f)
			{
				NetBridge.ReportThrow(netKind, transform.position, velocity);
			}
			Released?.Invoke(velocity);
		}

		public void Stow()
		{
			// Rebuilt from native code (token 0x0600085E, RVA 0x282A064)
			if (holder)
			{
				holder.Forget(this);
			}
			holder = null;
			if (!rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
			rb.isKinematic = true;
			if (col)
			{
				col.enabled = false;
			}
			if (splash)
			{
				splash.enabled = false;
			}
			inWaterSince = -1f;
			Stowed = true;
		}

		public void ForceDrop()
		{
			// Rebuilt from native code (token 0x0600085F, RVA 0x282A230)
			if (holder)
			{
				holder.Forget(this);
			}
			holder = null;
			Stowed = false;
			if (col)
			{
				col.enabled = true;
			}
			IgnorePlayer();
			if (splash)
			{
				splash.enabled = true;
			}
		}

		public void Place(Vector3 position, Quaternion rotation, bool pinned)
		{
			// Rebuilt from native code (token 0x06000860, RVA 0x282A350)
			ForceDrop();
			transform.SetPositionAndRotation(position, rotation);
			rb.position = position;
			rb.rotation = rotation;
			if (!rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
			rb.isKinematic = pinned;
			inWaterSince = -1f;
			if (splash)
			{
				splash.ResetTracking(0.3f);
			}
		}

		public void Launch(Vector3 position, Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000861, RVA 0x282A568)
			ForceDrop();
			transform.position = position;
			rb.position = position;
			rb.isKinematic = false;
			rb.linearVelocity = velocity;
			rb.angularVelocity = Random.insideUnitSphere * 6f;
			inWaterSince = -1f;
			if (splash)
			{
				splash.enabled = false;
			}
		}

		public void SetHome(Vector3 position, Quaternion rotation)
		{
			// Rebuilt from native code (token 0x06000862, RVA 0x282A6D4)
			home = position;
			homeRotation = rotation;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x06000864, RVA 0x282A7A0)
			if (IsHeld || rb.isKinematic)
			{
				return;
			}
			bool underwater = WaterVolume.IsUnderwater(transform.position, out _);
			rb.linearDamping = underwater ? 4f : 0.05f;
			rb.angularDamping = underwater ? 3f : 0.05f;
			if (underwater)
			{
				if (inWaterSince < 0f)
				{
					inWaterSince = Time.time;
				}
			}
			else
			{
				inWaterSince = -1f;
			}
			if (sinksAway && ((inWaterSince >= 0f && Time.time - inWaterSince > respawnAfterWater) || transform.position.y < -5f))
			{
				inWaterSince = -1f;
				Sank?.Invoke();
				gameObject.SetActive(false);
				return;
			}
			if ((returnsHome && inWaterSince >= 0f && Time.time - inWaterSince > respawnAfterWater) || transform.position.y < -5f)
			{
				Respawn();
			}
		}

		public void Respawn()
		{
			// Rebuilt from native code (token 0x06000865, RVA 0x282A9D0)
			rb.isKinematic = false;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			transform.SetPositionAndRotation(home, homeRotation);
			rb.position = home;
			inWaterSince = -1f;
			if (splash)
			{
				splash.ResetTracking(0.3f);
			}
		}
	}
}
