using UnityEngine;

namespace Shallows
{
	public class WaterBalloon : MonoBehaviour
	{
		public bool ghost;

		private Throwable item;

		private bool burst;

		private bool Flying
		{
			get
			{
				// Rebuilt from native code (token 0x06000AA3, RVA 0x275D7BC)
				if (ghost)
				{
					return true;
				}
				if (!item || item.IsHeld)
				{
					return false;
				}
				return !item.Stowed;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000AA0, RVA 0x275D61C)
			item = GetComponent<Throwable>();
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000AA1, RVA 0x275D674)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000AA2, RVA 0x275D718)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void OnCollisionEnter(Collision collision)
		{
			// Rebuilt from native code (token 0x06000AA4, RVA 0x275D864)
			if (Flying)
			{
				Vector3 at = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
				Burst(at, null);
			}
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x06000AA5, RVA 0x275DB0C)
			if (Flying && WaterVolume.IsUnderwater(transform.position, out WaterVolume water))
			{
				Burst(transform.position, water);
			}
		}

		private void Burst(Vector3 at, WaterVolume water)
		{
			// Rebuilt from native code (token 0x06000AA6, RVA 0x275D918)
			if (burst)
			{
				return;
			}
			burst = true;
			float y = water != null ? water.SurfaceY : at.y;
			if (!ghost && SplashSystem.I)
			{
				SplashSystem.I.Loud(new Vector3(at.x, y, at.z), 1f, SplashKind.Balloon);
			}
			Destroy(gameObject);
		}
	}
}
