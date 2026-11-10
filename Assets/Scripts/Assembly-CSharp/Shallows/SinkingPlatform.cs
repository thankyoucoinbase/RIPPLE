using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	[RequireComponent(typeof(Rigidbody))]
	public class SinkingPlatform : MonoBehaviour
	{
		private enum Phase
		{
			Up = 0,
			Shaking = 1,
			Falling = 2,
			Down = 3,
			Rising = 4
		}

		[Tooltip("Seconds of shuddering after someone lands before it drops.")]
		public float warning = 0.55f;

		[Tooltip("How fast it falls (m/s²). Fast enough that whoever rides it down splashes.")]
		public float dropAcceleration = 9f;

		[Tooltip("How far it drops (m). Its top ends up below the water.")]
		public float dropDistance = 0.34f;

		[Tooltip("Seconds it stays down after the last touch.")]
		public float stayDown = 3f;

		public float riseSpeed = 0.35f;

		[Tooltip("Off: it holds like solid ground. The memory bridge turns this on for its wrong tiles.")]
		public bool armed = true;

		[Tooltip("Breaks the moment it's touched: no shudder, no fall, it's just gone from under you (the memory bridge's wrong tiles).")]
		public bool instant;

		[Tooltip("Hairline cracks across the top so you can tell it'll crumble; they split wider while it shudders and drops. Never on the memory bridge, where every tile has to look the same.")]
		public bool cracked = true;

		private Phase phase;

		private Rigidbody rb;

		private Vector3 home;

		private float phaseTime;

		private float fallSpeed;

		private float lastTouched;

		private Bounds top;

		private float topOffset;

		private GameObject wideCracks;

		private static Material crackMaterial;

		public bool Idle
		{
			get
			{
				// Rebuilt from native code (token 0x06000525, RVA 0x27CD77C)
				return phase == Phase.Up && Time.time - lastTouched > 0.3f;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000526, RVA 0x27CD7BC)
			rb = GetComponent<Rigidbody>();
			rb.isKinematic = true;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			home = transform.position;
			Collider collider = GetComponent<Collider>();
			Vector3 size = collider ? collider.bounds.size : transform.lossyScale;
			top = new Bounds(Vector3.zero, new Vector3(size.x + 0.1f, 0.3f, size.z + 0.1f));
			topOffset = size.y * 0.5f;
			FoamCaster.Ensure(gameObject);
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000527, RVA 0x27CD9DC)
			if (cracked && !instant && !GetComponentInParent<MemoryBridge>())
			{
				BuildCracks();
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000528, RVA 0x27CDF64)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000529, RVA 0x27CE008)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x0600052A, RVA 0x27CE0AC)
			phaseTime += dt;
			bool touched = SomeoneOnTop();
			if (touched)
			{
				lastTouched = Time.time;
			}
			Vector3 target = home;
			switch (phase)
			{
			case Phase.Up:
				if (touched && armed)
				{
					if (instant)
					{
						Enter(Phase.Down);
						target = home + Vector3.down * dropDistance;
						rb.position = target;
						transform.position = target;
						if (SplashSystem.I)
						{
							SplashSystem.I.PlaySound(Sfx.PlatformSink(), home, 0.5f, 1.6f);
						}
					}
					else
					{
						Enter(Phase.Shaking);
					}
				}
				break;
			case Phase.Shaking:
				target = home + new Vector3(MathF.Sin(phaseTime * 70f), MathF.Sin(phaseTime * 53f) * 0.5f - 0.5f, MathF.Cos(phaseTime * 61f)) * 0.012f;
				if (phaseTime >= warning)
				{
					fallSpeed = 0f;
					Enter(Phase.Falling);
				}
				break;
			case Phase.Falling:
			{
				fallSpeed += dropAcceleration * dt;
				float drop = Mathf.Min(dropDistance, home.y - rb.position.y + fallSpeed * dt);
				target = home + Vector3.down * drop;
				if (drop >= dropDistance)
				{
					if (SplashSystem.I)
					{
						SplashSystem.I.PlaySound(Sfx.PlatformSink(), transform.position, 0.35f, 1.6f);
					}
					Enter(Phase.Down);
				}
				break;
			}
			case Phase.Down:
				target = home + Vector3.down * dropDistance;
				if (Time.time - lastTouched > stayDown)
				{
					Enter(Phase.Rising);
				}
				break;
			case Phase.Rising:
			{
				float sunk = Mathf.Max(0f, home.y - rb.position.y - riseSpeed * dt);
				target = home + Vector3.down * sunk;
				if (touched)
				{
					Enter(Phase.Down);
				}
				else if (sunk <= 0f)
				{
					Enter(Phase.Up);
				}
				break;
			}
			}
			if (phase == Phase.Shaking && phaseTime < dt * 1.5f && SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.PlatformShake(), transform.position, 0.6f, 0.45f);
			}
			rb.MovePosition(target);
		}

		private void Enter(Phase next)
		{
			// Rebuilt from native code (token 0x0600052B, RVA 0x27CEB90)
			phase = next;
			phaseTime = 0f;
			if (wideCracks)
			{
				wideCracks.SetActive(next != Phase.Up && next != Phase.Rising);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600052C, RVA 0x27CEC30)
			crackMaterial = null;
		}

		private void BuildCracks()
		{
			// Rebuilt from native code (token 0x0600052D, RVA 0x27CDA84)
			BoxCollider box = GetComponent<BoxCollider>();
			if (!box)
			{
				return;
			}
			Vector3 scale = transform.lossyScale;
			if (scale.x < 0.001f || scale.y < 0.001f || scale.z < 0.001f)
			{
				return;
			}
			Vector2 half = new Vector2(scale.x * box.size.x, scale.z * box.size.z) * 0.5f - Vector2.one * 0.06f;
			if (half.x <= 0f || half.y <= 0f)
			{
				return;
			}
			if (!crackMaterial)
			{
				crackMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = "Platform Cracks"
				};
				crackMaterial.SetColor("_BaseColor", new Color(0.13f, 0.12f, 0.11f, 1f));
				crackMaterial.SetFloat("_Smoothness", 0f);
			}
			Vector3 position = transform.position;
			System.Random rng = new System.Random(Mathf.RoundToInt(position.x * 37f) * 7919 + Mathf.RoundToInt(position.z * 53f));
			float area = half.x * half.y;
			float topY = box.center.y + box.size.y * 0.5f;
			CrackLayer("Cracks", rng, half, topY, 0.006f, 0.011f, Mathf.Clamp(Mathf.RoundToInt(area * 3f), 2, 6), scale);
			wideCracks = CrackLayer("Cracks (splitting)", rng, half, topY, 0.012f, 0.02f, Mathf.Clamp(Mathf.RoundToInt(area * 5f), 3, 9), scale);
			if (wideCracks)
			{
				wideCracks.SetActive(false);
			}
		}

		private GameObject CrackLayer(string name, System.Random rng, Vector2 half, float top, float minWidth, float maxWidth, int count, Vector3 scale)
		{
			// Rebuilt from native code (token 0x0600052E, RVA 0x27CEC84)
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			for (int i = 0; i < count; i++)
			{
				Vector2 from = new Vector2(R(-half.x, half.x), R(-half.y, half.y));
				if (rng.NextDouble() < 0.6)
				{
					if (rng.NextDouble() < 0.5)
					{
						from.x = Mathf.Sign(R(-1f, 1f)) * half.x;
					}
					else
					{
						from.y = Mathf.Sign(R(-1f, 1f)) * half.y;
					}
				}
				Wander(from, MathF.Atan2(-from.y, -from.x) + R(-0.9f, 0.9f), R(minWidth, maxWidth), rng.Next(4, 9), 0);
			}
			if (vertices.Count == 0)
			{
				return null;
			}
			Mesh mesh = new Mesh
			{
				name = name
			};
			mesh.SetVertices(vertices);
			mesh.SetTriangles(triangles, 0);
			Vector3[] normals = new Vector3[vertices.Count];
			for (int i = 0; i < normals.Length; i++)
			{
				normals[i] = Vector3.up;
			}
			mesh.normals = normals;
			mesh.RecalculateBounds();
			GameObject layer = new GameObject(name);
			layer.layer = 2;
			layer.transform.SetParent(transform, false);
			layer.transform.localPosition = new Vector3(0f, top + 0.003f / scale.y, 0f);
			layer.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
			layer.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer meshRenderer = layer.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = crackMaterial;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			return layer;

			float R(float a, float b)
			{
				// Rebuilt from native code (token 0x06000531, RVA 0x27CF250)
				return a + (b - a) * (float)rng.NextDouble();
			}

			void Wander(Vector2 from, float heading, float width, int segments, int depth)
			{
				// Rebuilt from native code (token 0x06000532, RVA 0x27CF294)
				for (int step = 0; step < segments && width > 0.002f; step++)
				{
					heading += R(-0.6f, 0.6f);
					float length = R(0.08f, 0.22f);
					Vector2 end = new Vector2(Mathf.Clamp(from.x + MathF.Cos(heading) * length, -half.x, half.x), Mathf.Clamp(from.y + MathF.Sin(heading) * length, -half.y, half.y));
					float next = width * 0.85f;
					Segment(from, end, width, next);
					if (depth <= 1 && rng.NextDouble() < 0.25)
					{
						float turn = R(0.6f, 1.2f);
						Wander(end, heading + (rng.NextDouble() < 0.5 ? -turn : turn), next * 0.8f, rng.Next(2, 5), depth + 1);
					}
					from = end;
					width = next;
				}
			}

			void Segment(Vector2 a, Vector2 b, float wa, float wb)
			{
				// Rebuilt from native code (token 0x06000533, RVA 0x27CF530)
				Vector2 along = b - a;
				if (along.sqrMagnitude < 1E-06f)
				{
					return;
				}
				Vector2 side = new Vector2(along.y, -along.x).normalized * 0.5f;
				int first = vertices.Count;
				vertices.Add(new Vector3(a.x - side.x * wa, 0f, a.y - side.y * wa));
				vertices.Add(new Vector3(a.x + side.x * wa, 0f, a.y + side.y * wa));
				vertices.Add(new Vector3(b.x - side.x * wb, 0f, b.y - side.y * wb));
				vertices.Add(new Vector3(b.x + side.x * wb, 0f, b.y + side.y * wb));
				triangles.AddRange(new int[12]
				{
					first,
					first + 2,
					first + 3,
					first,
					first + 3,
					first + 1,
					first,
					first + 3,
					first + 2,
					first,
					first + 1,
					first + 3
				});
			}
		}

		private bool SomeoneOnTop()
		{
			// Rebuilt from native code (token 0x0600052F, RVA 0x27CE868)
			top.center = rb.position + Vector3.up * (topOffset + 0.1f);
			PlayerRig rig = PlayerRig.I;
			if (rig && !rig.Frozen)
			{
				if (top.Contains(rig.BodyBottom))
				{
					return true;
				}
				if (rig.leftHand && top.Contains(rig.leftHand.position))
				{
					return true;
				}
				if (rig.rightHand && top.Contains(rig.rightHand.position))
				{
					return true;
				}
			}
			List<Vector3> feet = NetBridge.RemoteFeet;
			for (int i = 0; i < feet.Count; i++)
			{
				if (top.Contains(feet[i]))
				{
					return true;
				}
			}
			List<Vector3> hands = NetBridge.RemoteHands;
			for (int i = 0; i < hands.Count; i++)
			{
				if (top.Contains(hands[i]))
				{
					return true;
				}
			}
			return false;
		}
	}
}
