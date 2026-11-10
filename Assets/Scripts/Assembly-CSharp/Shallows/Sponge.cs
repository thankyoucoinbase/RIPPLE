using UnityEngine;

namespace Shallows
{
	public class Sponge : MonoBehaviour
	{
		public bool ghost;

		private Throwable item;

		private Rigidbody rb;

		private bool loose;

		private bool swollen;

		private void Awake()
		{
			// Rebuilt from native code (token 0x060009C9, RVA 0x27458A4)
			item = GetComponent<Throwable>();
			if (!item)
			{
				return;
			}
			item.Released += _ => loose = true;
			item.Grabbed += () => loose = false;
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x060009CA, RVA 0x2745A0C)
			if (!ghost)
			{
				return;
			}
			loose = true;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060009CB, RVA 0x2745A20)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060009CC, RVA 0x2745AC4)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x060009CD, RVA 0x2745B68)
			if (swollen || !loose)
			{
				return;
			}
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
			swollen = true;
			if (!ghost)
			{
				Vector3 centre = water.ClampXZ(new Vector3(position.x, water.SurfaceY, position.z), SpongeRaft.Size.z * 0.5f + 0.1f);
				centre.y = water.SurfaceY;
				float yaw = transform.eulerAngles.y;
				SpongeRaft.Open(centre, yaw, false);
				NetBridge.ReportSponge(centre, yaw);
			}
			Destroy(gameObject);
		}
	}
}
