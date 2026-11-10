using UnityEngine;

namespace Shallows
{
	public class RubberDuck : MonoBehaviour
	{
		public bool ghost;

		[Tooltip("How long it keeps splashing after it lands (s).")]
		public float splashFor = 4f;

		[Tooltip("Time between its splashes (s).")]
		public float every = 0.7f;

		private Throwable item;

		private Rigidbody rb;

		private bool landed;

		private float splashUntil;

		private float nextSplash;

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600096D, RVA 0x273C35C)
			item = GetComponent<Throwable>();
			if (!item)
			{
				return;
			}
			item.Released += _ => landed = false;
			item.Grabbed += () => landed = false;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600096E, RVA 0x273C4C4)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600096F, RVA 0x273C568)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x06000970, RVA 0x273C60C)
			if (!rb)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (!rb || rb.isKinematic)
			{
				return;
			}
			Vector3 position = rb.position;
			if (!WaterVolume.IsUnderwater(position + Vector3.down * 0.02f, out WaterVolume water))
			{
				return;
			}
			float depth = (water.SurfaceY + 0.015f - position.y) / 0.03f;
			rb.AddForce(Vector3.up * (-Physics.gravity.y * (Mathf.Clamp(depth, -1f, 2f) + 1f)), ForceMode.Acceleration);
			Vector3 flat = Vector3.ProjectOnPlane(rb.rotation * Vector3.forward, Vector3.up);
			if (flat.sqrMagnitude > 0.0001f)
			{
				rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(flat), dt * 6f));
			}
			rb.angularVelocity *= 1f - Mathf.Clamp01(dt * 4f);
			if (ghost)
			{
				return;
			}
			if (!landed)
			{
				landed = true;
				splashUntil = Time.time + splashFor;
				nextSplash = Time.time + every;
			}
			if (Time.time < splashUntil && Time.time >= nextSplash)
			{
				nextSplash += every;
				Vector2 kick = Random.insideUnitCircle;
				rb.AddForce(new Vector3(kick.x * 0.35f, 1.1f, kick.y * 0.35f), ForceMode.VelocityChange);
				if (SplashSystem.I)
				{
					SplashSystem.I.Loud(new Vector3(position.x, water.SurfaceY, position.z), 0.5f, SplashKind.Object);
				}
			}
		}
	}
}
