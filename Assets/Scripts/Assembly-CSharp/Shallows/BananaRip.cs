using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(120)]
	public class BananaRip : MonoBehaviour
	{
		[Tooltip("How close a hand has to be to the middle of the bunch to grab it.")]
		public float reach = 0.45f;

		[Tooltip("Grip above this grabs (squeezing as your hand reaches it, like picking anything up).")]
		public float grabThreshold = 0.65f;

		[Tooltip("Grip below this lets go.")]
		public float releaseThreshold = 0.35f;

		[Tooltip("Pulling less than this past where you grabbed is just holding it.")]
		public float slack = 0.08f;

		[Tooltip("A steady pull this far past where you grabbed rips it off.")]
		public float ripPull = 0.45f;

		[Tooltip("A sharp yank rips it sooner: past this pull, with your hand going away at yankSpeed or faster.")]
		public float yankPull = 0.22f;

		public float yankSpeed = 2.6f;

		[Tooltip("How far the bunch follows your hand before the stalk is pulled tight.")]
		public float give = 0.2f;

		[Tooltip("Degrees a second it turns on its stalk while nobody holds it.")]
		public float spinSpeed = 25f;

		private static readonly float[] CreakAt = new float[] { 0.3f, 0.55f, 0.8f };

		private Vector3 basePos;

		private Vector3 pivot;

		private Quaternion baseRot;

		private Transform leaves;

		private Vector3 leavesBase;

		private float spin;

		private float tension;

		private float buzzAt;

		private float rustle;

		private int creaks;

		private Vector3 bend;

		private Vector3 bendVelocity;

		private readonly bool[] gripping = new bool[2];

		private readonly bool[] held = new bool[2];

		private readonly Vector3[] grabOffset = new Vector3[2];

		private readonly float[] pull = new float[2];

		private readonly float[] pullSpeed = new float[2];

		public bool Ripped { get; private set; }

		public Transform RippedBy { get; private set; }

		public bool Touched { get; private set; }

		public bool Held
		{
			get
			{
				// Rebuilt from native code (token 0x0600015C, RVA 0x2723248)
				return held[0] || held[1];
			}
		}

		private Quaternion Spin
		{
			get
			{
				// Rebuilt from native code (token 0x06000160, RVA 0x27237F4)
				return Quaternion.Euler(0f, spin, 0f);
			}
		}

		private Vector3 RestCentre
		{
			get
			{
				// Rebuilt from native code (token 0x06000161, RVA 0x2723830)
				return pivot + Spin * (basePos - pivot);
			}
		}

		private Quaternion RestRotation
		{
			get
			{
				// Rebuilt from native code (token 0x06000162, RVA 0x27238B8)
				return Spin * baseRot;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600015D, RVA 0x2723290)
			basePos = transform.position;
			baseRot = transform.rotation;
			Transform stalkTop = LifeguardProps.Find(transform, "Stalk Top");
			pivot = stalkTop ? stalkTop.position : basePos + Vector3.up * 0.35f;
			leaves = transform.parent ? LifeguardProps.Find(transform.parent, "Tree Leaves") : null;
			if (leaves)
			{
				leavesBase = leaves.localPosition;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600015E, RVA 0x2723514)
			RippedBy = null;
			Ripped = false;
			Touched = false;
			rustle = 0f;
			creaks = 0;
			tension = 0f;
			bend = bendVelocity = Vector3.zero;
			for (int h = 0; h < 2; h++)
			{
				held[h] = false;
			}
			Pose(Vector3.zero);
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600015F, RVA 0x272376C)
			if (leaves)
			{
				leaves.localPosition = leavesBase;
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000163, RVA 0x272397C)
			if (Ripped)
			{
				return;
			}
			PlayerRig rig = PlayerRig.I;
			bool live = rig && !rig.Frozen && GameManager.I && GameManager.I.Playing;
			float dt = Mathf.Max(Time.deltaTime, 0.0001f);
			Touched = false;
			float follow = 1f - MathF.Exp(-25f * dt);
			int strongest = -1;
			for (int h = 0; h < 2; h++)
			{
				Transform hand = rig ? (h == 0 ? rig.leftHand : rig.rightHand) : null;
				float grip = XRHands.Grip(h == 0);
				bool squeezed = false;
				if (!gripping[h])
				{
					if (grip > grabThreshold)
					{
						gripping[h] = true;
						squeezed = true;
					}
				}
				else if (grip < releaseThreshold)
				{
					gripping[h] = false;
				}
				if (!hand || !live)
				{
					held[h] = false;
					continue;
				}
				float near = (hand.position - transform.position).sqrMagnitude;
				float reachSq = reach * reach;
				if (near < reachSq && !held[h] && squeezed)
				{
					held[h] = true;
					grabOffset[h] = Quaternion.Inverse(RestRotation) * (hand.position - RestCentre);
					pull[h] = pullSpeed[h] = 0f;
					XRHands.Haptic(h == 0, 0.35f, 0.05f);
					rustle = Mathf.Max(rustle, 0.3f);
				}
				else if (held[h] && !gripping[h])
				{
					held[h] = false;
					if (tension > 0.3f)
					{
						XRHands.Haptic(h == 0, 0.3f, 0.04f);
					}
				}
				if (held[h])
				{
					Vector3 stretch = hand.position - (RestCentre + RestRotation * grabOffset[h]);
					if (stretch.y > 0f)
					{
						stretch.y *= 0.3f;
					}
					float distance = stretch.magnitude;
					pullSpeed[h] = Mathf.Lerp(pullSpeed[h], (distance - pull[h]) / dt, follow);
					pull[h] = distance;
					if (strongest < 0 || distance > pull[strongest])
					{
						strongest = h;
					}
				}
				else if (near < reachSq && !gripping[h])
				{
					Touched = true;
				}
			}
			float before = tension;
			Vector3 lean = Vector3.zero;
			if (strongest < 0)
			{
				creaks = 0;
				spin += dt * spinSpeed;
				tension = Mathf.MoveTowards(tension, 0f, dt * 3f);
			}
			else
			{
				int other = 1 - strongest;
				tension = Mathf.Clamp01((pull[strongest] + (held[other] ? pull[other] * 0.5f : 0f) - slack) / (ripPull - slack));
				Transform hand = strongest == 0 ? rig.leftHand : rig.rightHand;
				Vector3 stretch = hand.position - (RestCentre + RestRotation * grabOffset[strongest]);
				if (stretch.y > 0f)
				{
					stretch.y *= 0.3f;
				}
				float length = stretch.magnitude;
				if (length > 0.0001f)
				{
					lean = stretch / length * (give * (1f - MathF.Exp(-length / give)));
				}
				Feedback(before);
				bool yank = pull[strongest] > yankPull && pullSpeed[strongest] > yankSpeed;
				if (tension >= 1f || yank)
				{
					Rip(strongest, hand);
					return;
				}
			}
			bendVelocity += ((lean - bend) * 220f - bendVelocity * 16f) * dt;
			bend += bendVelocity * dt;
			float shake = tension * tension * 0.014f;
			Vector3 jitter = new Vector3(Mathf.PerlinNoise(Time.time * 23f, 0.1f) - 0.5f, Mathf.PerlinNoise(0.7f, Time.time * 23f) - 0.5f, Mathf.PerlinNoise(Time.time * 19f, 3.3f) - 0.5f) * shake * 2f;
			Pose(bend + jitter);
			rustle = Mathf.Max(rustle - dt * 1.5f, tension * 0.6f);
			if (!leaves)
			{
				return;
			}
			float sway = rustle * 0.025f;
			leaves.localPosition = leavesBase + new Vector3(Mathf.PerlinNoise(Time.time * 9f, 5f) - 0.5f, Mathf.PerlinNoise(8f, Time.time * 9f) - 0.5f, Mathf.PerlinNoise(Time.time * 7f, 1.7f) - 0.5f) * sway * 2f;
		}

		private void Pose(Vector3 offset)
		{
			// Rebuilt from native code (token 0x06000164, RVA 0x2723604)
			Vector3 arm = RestCentre - pivot;
			Vector3 bent = offset + arm;
			transform.rotation = Quaternion.FromToRotation(arm, bent) * RestRotation;
			transform.position = bent + pivot;
		}

		private void Feedback(float before)
		{
			// Rebuilt from native code (token 0x06000165, RVA 0x2724624)
			while (creaks < CreakAt.Length && tension >= CreakAt[creaks])
			{
				Play(Sfx.BranchCreak(), 0.35f + creaks * 0.25f, 1.1f - creaks * 0.12f);
				for (int h = 0; h < 2; h++)
				{
					if (held[h])
					{
						XRHands.Haptic(h == 0, 0.55f + creaks * 0.15f, 0.05f);
					}
				}
				rustle = Mathf.Max(rustle, 0.5f + creaks * 0.2f);
				creaks++;
			}
			if (tension < before - 0.25f)
			{
				creaks = 0;
			}
			if (tension > 0.5f && Random.value < tension * tension * Time.deltaTime * 6f)
			{
				Play(Sfx.BranchSnap(), 0.1f, 1.5f + Random.value * 0.3f);
				for (int h = 0; h < 2; h++)
				{
					if (held[h])
					{
						XRHands.Haptic(h == 0, 0.7f, 0.025f);
					}
				}
			}
			if (Time.time < buzzAt)
			{
				return;
			}
			buzzAt = Time.time + 0.05f;
			float buzz = 0.06f + MathF.Pow(tension, 1.5f) * 0.6f;
			for (int h = 0; h < 2; h++)
			{
				if (held[h])
				{
					XRHands.Haptic(h == 0, buzz, 0.07f, false);
				}
			}
		}

		private void Rip(int strongest, Transform hand)
		{
			// Rebuilt from native code (token 0x06000166, RVA 0x2724A10)
			RippedBy = hand;
			Ripped = true;
			Play(Sfx.BranchSnap(), 1f, 1f);
			for (int h = 0; h < 2; h++)
			{
				if (held[h])
				{
					XRHands.Haptic(h == 0, 1f, h == strongest ? 0.22f : 0.12f);
				}
			}
			Vector3 grabPoint = transform.position + transform.rotation * grabOffset[strongest];
			transform.position += hand.position - grabPoint;
			if (leaves)
			{
				leaves.localPosition = leavesBase;
			}
			held[0] = false;
			held[1] = false;
		}

		private void Play(AudioClip clip, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x06000167, RVA 0x2724CCC)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(clip, pivot, volume, pitch);
			}
			else if (GameManager.I)
			{
				GameManager.I.PlayLocal(clip, volume, pitch);
			}
		}
	}
}
