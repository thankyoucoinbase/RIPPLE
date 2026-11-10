using UnityEngine;

namespace Shallows
{
	public class FuseHusk : MonoBehaviour
	{
		public bool loud;

		private Rigidbody rb;

		private bool wasUnder;

		private float born;

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000146, RVA 0x2720D90)
			rb = GetComponent<Rigidbody>();
			born = Time.time;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000147, RVA 0x2720DF8)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000148, RVA 0x2720E9C)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x06000149, RVA 0x2720F40)
			bool under = WaterVolume.IsUnderwater(transform.position, out WaterVolume water);
			if (under && !wasUnder && water != null && SplashSystem.I)
			{
				Vector3 surface = new Vector3(transform.position.x, water.SurfaceY, transform.position.z);
				if (loud)
				{
					SplashSystem.I.Loud(surface, 0.7f, SplashKind.Object, true);
				}
				else
				{
					SplashSystem.I.Quiet(surface, 0.8f);
				}
			}
			wasUnder = under;
			if (rb)
			{
				rb.linearDamping = under ? 4f : 0.05f;
				rb.angularDamping = under ? 3f : 0.05f;
			}
			if (Time.time - born > 12f || transform.position.y < -5f)
			{
				Destroy(gameObject);
			}
		}
	}
}
