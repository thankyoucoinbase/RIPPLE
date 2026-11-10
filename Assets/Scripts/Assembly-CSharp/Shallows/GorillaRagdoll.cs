using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	public class GorillaRagdoll : MonoBehaviour
	{
		[Tooltip("The chest. The monster holds this.")]
		public Rigidbody torso;

		[Tooltip("Every part, the chest included.")]
		public Rigidbody[] parts;

		public Renderer[] furRenderers;

		[Tooltip("How hard the throw is (m/s, forward and up).")]
		public float throwForward = 7f;

		[Tooltip("How hard the throw is (m/s, forward and up).")]
		public float throwUp = 4.5f;

		[Tooltip("How strongly parts float.")]
		public float buoyancy = 14f;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private MonsterAI monster;

		private bool local;

		private bool held;

		private bool landed;

		private bool hidden;

		private bool gulped;

		private float t;

		private float dieAt;

		private float gripHeight = 0.28f;

		private float jolt;

		private int whips;

		private int hits;

		private Kill kill;

		private float[] hitTimes;

		private Vector3 startPosition;

		private Vector3 lastGrip;

		private Vector3 gripVelocity;

		private Vector3 flung;

		private Transform head;

		private bool following;

		private Vector3 followPosition;

		private Vector3 followVelocity;

		private Quaternion followRotation;

		public Transform Head
		{
			get
			{
				// Rebuilt from native code (token 0x060001E1, RVA 0x277B218)
				if (head)
				{
					return head;
				}
				return torso ? torso.transform : transform;
			}
		}

		public bool Held
		{
			get
			{
				// Rebuilt from native code (token 0x060001E2, RVA 0x277B2C4)
				return held;
			}
		}

		public Vector3 FlungDirection
		{
			get
			{
				// Rebuilt from native code (token 0x060001E3, RVA 0x277B2CC)
				return flung;
			}
		}

		public Vector3 Position
		{
			get
			{
				// Rebuilt from native code (token 0x060001E4, RVA 0x277B2D8)
				return torso ? torso.position : transform.position;
			}
		}

		public Quaternion Rotation
		{
			get
			{
				// Rebuilt from native code (token 0x060001E5, RVA 0x277B36C)
				return torso ? torso.rotation : transform.rotation;
			}
		}

		public void Follow(Vector3 position, Quaternion rotation)
		{
			// Rebuilt from native code (token 0x060001E6, RVA 0x277B400)
			followPosition = position;
			followRotation = rotation;
			following = true;
		}

		public void ShowArms(bool show)
		{
			// Rebuilt from native code (token 0x060001E7, RVA 0x277B41C)
			if (hidden && show)
			{
				return;
			}
			if (TryGetComponent(out ApeRagdoll ape))
			{
				ape.ShowArms(show);
			}
		}

		public void ShowHead(bool show)
		{
			// Rebuilt from native code (token 0x060001E8, RVA 0x277B4A4)
			if (hidden && show)
			{
				return;
			}
			if (TryGetComponent(out ApeRagdoll ape))
			{
				ape.ShowHead(show);
			}
			if (!head)
			{
				return;
			}
			foreach (Renderer renderer in head.GetComponentsInChildren<Renderer>(true))
			{
				renderer.enabled = show;
			}
		}

		public void Begin(MonsterAI holder, Color fur, bool isLocal, float lifetime)
		{
			// Rebuilt from native code (token 0x060001E9, RVA 0x277B5D8)
			Begin(holder, fur, isLocal, lifetime, new Kill(KillMove.Shake, 2));
		}

		public void Begin(MonsterAI holder, Color fur, bool isLocal, float lifetime, Kill how)
		{
			// Rebuilt from native code (token 0x060001EA, RVA 0x277B600)
			monster = holder;
			kill = how;
			local = isLocal;
			held = true;
			t = 0f;
			hitTimes = how.Hits;
			hits = 0;
			dieAt = how.End + (how.move == KillMove.Consume ? 0.5f : lifetime);
			startPosition = Position;
			lastGrip = holder.GripPosition;
			if (holder.water)
			{
				gripHeight = holder.water.SurfaceY;
			}
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			block.SetColor(BaseColorId, Color.Lerp(fur, Color.black, 0.2f));
			foreach (Renderer renderer in furRenderers)
			{
				if (renderer)
				{
					renderer.SetPropertyBlock(block);
				}
			}
			foreach (Rigidbody part in parts)
			{
				if (part && part.name == "Head")
				{
					head = part.transform;
				}
			}
			if (local && PlayerRig.I)
			{
				foreach (Collider body in PlayerRig.I.Colliders)
				{
					foreach (Rigidbody part in parts)
					{
						if (!body || !part)
						{
							continue;
						}
						foreach (Collider collider in part.GetComponents<Collider>())
						{
							Physics.IgnoreCollision(body, collider, true);
						}
					}
				}
			}
			torso.isKinematic = true;
			torso.interpolation = RigidbodyInterpolation.Interpolate;
			foreach (Rigidbody part in parts)
			{
				if (part && part != torso)
				{
					part.linearVelocity = Vector3.zero;
					part.interpolation = RigidbodyInterpolation.Interpolate;
				}
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060001EB, RVA 0x277BE94)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060001EC, RVA 0x277C024)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x060001ED, RVA 0x277C1B4)
			t += dt;
			if (held)
			{
				if (!monster || t >= kill.Release)
				{
					Release();
				}
				else
				{
					Vector3 grip = monster.GripPosition;
					gripVelocity = (grip - lastGrip) / dt;
					lastGrip = grip;
					if (kill.move == KillMove.Consume)
					{
						HoldSwallowing(grip);
					}
					else if (kill.move == KillMove.Pin)
					{
						HoldPinned(dt);
					}
					else
					{
						float k = Mathf.Clamp01(t / 0.18f);
						torso.MovePosition(Vector3.Lerp(torso.position, grip, k));
						torso.MoveRotation(Quaternion.Slerp(torso.rotation, monster.GripRotation * Quaternion.Euler(0f, 0f, 90f), k));
						if (whips < DeathTimeline.Whips.Length && t >= DeathTimeline.Whips[whips])
						{
							whips++;
							if (local)
							{
								XRHands.HapticBoth(0.9f, 0.12f, true);
							}
						}
					}
				}
				Hits();
			}
			else
			{
				Vector3 velocity = torso.linearVelocity;
				if (following && !local)
				{
					if (!torso.isKinematic)
					{
						torso.isKinematic = true;
					}
					Vector3 at = torso.position;
					Vector3 to = (followPosition - at).sqrMagnitude > 9f ? followPosition : Vector3.Lerp(at, followPosition, 1f - MathF.Exp(-12f * dt));
					torso.MovePosition(to);
					torso.MoveRotation(Quaternion.Slerp(torso.rotation, followRotation, 1f - MathF.Exp(-10f * dt)));
					followVelocity = (to - at) / Mathf.Max(0.0001f, dt);
					velocity = followVelocity;
				}
				Float();
				if (!landed && torso.position.y < gripHeight && velocity.y < -1f)
				{
					landed = true;
					Vector3 at = new Vector3(torso.position.x, gripHeight, torso.position.z);
					if (SplashSystem.I && WaterVolume.At(at))
					{
						SplashSystem.I.Visual(at, 1.3f);
						SplashSystem.I.PlaySound(Sfx.SplashBig(2), at, 1f, 0.8f);
						SplashSystem.I.PlaySound(Sfx.BodySlam(), at, 1f, 0.9f);
					}
				}
			}
			if (t > dieAt - 3f)
			{
				foreach (Rigidbody part in parts)
				{
					if (part)
					{
						part.detectCollisions = false;
					}
				}
			}
			if (t > dieAt || torso.position.y < -10f)
			{
				Destroy(gameObject);
			}
		}

		private void HoldPinned(float dt)
		{
			// Rebuilt from native code (token 0x060001EE, RVA 0x277CFD0)
			Vector3 pin = monster.PinPoint + Vector3.up * 0.04f;
			Vector3 forward = monster.transform.forward;
			Vector3 flat = forward.x * forward.x + forward.z * forward.z < 0.0001f ? Vector3.forward : new Vector3(forward.x, 0f, forward.z);
			Quaternion lying = Quaternion.LookRotation(Vector3.up, flat.normalized);
			if (t < 0.3f)
			{
				float k = t / 0.3f;
				torso.MovePosition(Vector3.Lerp(startPosition, pin, k * k) + Vector3.up * MathF.Sin(k * MathF.PI) * 0.45f);
				torso.MoveRotation(Quaternion.Slerp(torso.rotation, lying, Mathf.Clamp01(k * 1.5f)));
				return;
			}
			if (!landed)
			{
				landed = true;
				if (SplashSystem.I)
				{
					SplashSystem.I.Visual(pin, 1.1f);
					SplashSystem.I.PlaySound(Sfx.SplashBig(1), pin, 1f, 0.9f);
					SplashSystem.I.PlaySound(Sfx.BodySlam(), pin, 1f, 1f);
				}
				if (local)
				{
					XRHands.HapticBoth(1f, 0.15f, true);
				}
			}
			jolt = Mathf.MoveTowards(jolt, 0f, dt * 0.9f);
			float shake = 0f;
			if (jolt > 0f)
			{
				shake = jolt * MathF.Sin(t * 60f) * 40f;
			}
			torso.MovePosition(pin - Vector3.up * jolt);
			torso.MoveRotation(lying * Quaternion.Euler(shake, 0f, shake * 0.5f));
		}

		private void HoldSwallowing(Vector3 grip)
		{
			// Rebuilt from native code (token 0x060001EF, RVA 0x277D750)
			Quaternion gripRotation = monster.GripRotation;
			Vector3 along = gripRotation * Vector3.forward;
			Vector3 up = gripRotation * Vector3.up;
			Quaternion target = Quaternion.LookRotation(up, along);
			float reach = 0.2f;
			if (t > 0.88f)
			{
				float s = Mathf.Clamp01((t - 0.88f) / 0.37f);
				reach = Mathf.Lerp(0.2f, -0.55f, s * 0.4f + s * s * (3f - 2f * s) * 0.6f);
			}
			float follow = Mathf.Clamp01(t / 0.18f);
			Vector3 to = t > 1.25f ? monster.ThroatPosition : grip + along * reach;
			torso.MovePosition(Vector3.Lerp(torso.position, to, Mathf.Max(follow, t > 0.88f ? 1f : 0f)));
			torso.MoveRotation(Quaternion.Slerp(torso.rotation, target, follow));
			if (!hidden && t >= 1.25f)
			{
				hidden = true;
				foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
				{
					renderer.enabled = false;
				}
				foreach (Rigidbody part in parts)
				{
					if (part)
					{
						part.detectCollisions = false;
					}
				}
			}
			if (gulped || t < 1.3f)
			{
				return;
			}
			gulped = true;
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.Gulp(), monster.ThroatPosition, 1f, Random.Range(0.92f, 1.02f));
			}
			if (local)
			{
				XRHands.HapticBoth(0.7f, 0.25f, true);
			}
		}

		private void Hits()
		{
			// Rebuilt from native code (token 0x060001F0, RVA 0x277DD6C)
			if (hitTimes == null)
			{
				return;
			}
			while (hits < hitTimes.Length && t >= hitTimes[hits])
			{
				int hit = hits++;
				Vector3 point = Position;
				Vector3 direction = Vector3.up;
				float size = 1f;
				float lens = 1f;
				bool chomp = true;
				if (kill.move == KillMove.Consume)
				{
					if (hit == 0)
					{
						if (monster)
						{
							point = monster.GripPosition;
						}
					}
					else
					{
						if (monster)
						{
							point = monster.GripPosition + monster.GripRotation * Vector3.forward * 0.15f;
						}
						size = 0.55f;
						lens = 0.6f;
						chomp = false;
					}
				}
				else if (kill.move == KillMove.Pin)
				{
					if (monster)
					{
						point = monster.PinPoint + Vector3.up * 0.12f;
					}
					jolt = 0.12f;
					size = 1.3f;
					lens = 1.2f;
				}
				else
				{
					if (monster)
					{
						point = monster.GripPosition;
					}
					direction = (Vector3.up + (monster ? monster.transform.forward : Vector3.zero) * -0.5f).normalized;
				}
				Blood.Burst(point, direction, size);
				if (SplashSystem.I)
				{
					SplashSystem.I.PlaySound(chomp ? Sfx.Chomp() : Sfx.HandSmash(), point, 1f, Random.Range(0.92f, 1.06f));
					SplashSystem.I.PlaySound(Sfx.BloodSplat(), point, 0.9f, Random.Range(0.9f, 1.1f));
				}
				if (local)
				{
					Blood.Lens(lens);
					XRHands.HapticBoth(1f, chomp ? 0.14f : 0.2f, true);
				}
			}
		}

		private void Release()
		{
			// Rebuilt from native code (token 0x060001F1, RVA 0x277CAF8)
			held = false;
			if (!torso)
			{
				return;
			}
			torso.isKinematic = false;
			if (kill.move == KillMove.Pin)
			{
				foreach (Rigidbody part in parts)
				{
					if (part)
					{
						part.linearVelocity = Vector3.down * 0.3f;
						part.angularVelocity = Random.insideUnitSphere * 0.5f;
					}
				}
				landed = true;
				flung = Vector3.zero;
				return;
			}
			Vector3 forward = (monster ? monster.transform : transform).forward;
			Vector3 velocity = Quaternion.AngleAxis(-40f, Vector3.up) * forward * throwForward + Vector3.up * throwUp + gripVelocity * 0.15f;
			flung = velocity.normalized;
			foreach (Rigidbody part in parts)
			{
				if (part)
				{
					part.linearVelocity = velocity;
					part.angularVelocity = Random.insideUnitSphere * 4f;
				}
			}
			if (local)
			{
				XRHands.HapticBoth(0.6f, 0.15f, true);
			}
		}

		private void Float()
		{
			// Rebuilt from native code (token 0x060001F2, RVA 0x277E55C)
			foreach (Rigidbody part in parts)
			{
				if (!part || part.isKinematic)
				{
					continue;
				}
				if (WaterVolume.IsUnderwater(part.worldCenterOfMass, out WaterVolume water))
				{
					float depth = Mathf.Clamp01((water.SurfaceY - part.worldCenterOfMass.y) / 0.15f);
					part.AddForce(Vector3.up * (t > dieAt - 3f ? 0f : buoyancy) * depth, ForceMode.Acceleration);
					part.linearDamping = 3f;
					part.angularDamping = 2f;
				}
				else
				{
					part.linearDamping = 0.05f;
					part.angularDamping = 0.2f;
				}
			}
		}
	}
}
