using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	[RequireComponent(typeof(Throwable))]
	public class Fuse : MonoBehaviour
	{
		public enum Where
		{
			Spot = 0,
			Held = 1,
			Loose = 2,
			Slotted = 3
		}

		[Tooltip("The glowing middle, which pulses so you can spot it.")]
		public Renderer core;

		public Color glow = new Color(1f, 0.75f, 0.2f, 1f);

		[Header("Crackle")]
		[Tooltip("Now and then a fuse lying somewhere crackles with electricity (and its core flickers), so you can find it by ear if you're close. Seconds between crackles, picked at random each time: keep it rare, or the fuses are too easy to find.")]
		public Vector2 buzzEvery = new Vector2(14f, 26f);

		[Tooltip("How far away you can hear the crackle (m), about half a level. It fades out to nothing at this distance. (Renamed from buzzRange so the scene's old 9 m doesn't stick.)")]
		public float hearRange = 24f;

		[Tooltip("Up to this distance (m) the crackle plays at full volume, then it fades out to hearRange.")]
		public float fullVolumeRange = 3f;

		[Range(0f, 1f)]
		public float buzzVolume = 0.6f;

		public Throwable Item { get; private set; }

		public int Id { get; internal set; }

		internal FuseDoor door;

		private bool flying;

		private float actedAt = -10f;

		private float releasedAt;

		private float stillFor;

		private float nextStillHere;

		private float nextBuzz;

		private float buzzUntil;

		private AudioSource buzz;

		private MaterialPropertyBlock block;

		private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

		private const float StillHereEvery = 30f;

		public bool Busy
		{
			get
			{
				// Rebuilt from native code (token 0x06000100, RVA 0x2718724)
				if (Item.IsHeld || (Item.Stowed && door && door.HeldByMe(Id)) || flying)
				{
					return true;
				}
				return Time.time - actedAt < 1.5f;
			}
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000101, RVA 0x27187F4)
			Item = GetComponent<Throwable>();
			Item.returnsHome = false;
			Item.shareThrow = false;
			Item.canGrab = () => door && (door.CanGrab(Id) || Item.Stowed);
			Item.Grabbed += OnGrabbed;
			Item.Released += OnReleased;
			if (!GetComponent<FuseBeacon>())
			{
				gameObject.AddComponent<FuseBeacon>();
			}
			block = new MaterialPropertyBlock();
			nextBuzz = Time.time + Random.Range(3f, Mathf.Max(3f, buzzEvery.y));
		}

		internal bool Lying
		{
			get
			{
				// Rebuilt from native code (token 0x06000102, RVA 0x2718A74)
				if (!door || door.Open || Item.IsHeld || Item.Stowed)
				{
					return false;
				}
				return door.State(Id) == Where.Spot || door.State(Id) == Where.Loose;
			}
		}

		private void Crackle()
		{
			// Rebuilt from native code (token 0x06000103, RVA 0x2718B58)
			AudioClip clip = Sfx.FuseBuzz();
			if (!clip)
			{
				return;
			}
			if (!buzz)
			{
				GameObject go = new GameObject("Fuse Crackle");
				go.transform.SetParent(transform, false);
				buzz = go.AddComponent<AudioSource>();
				buzz.playOnAwake = false;
				buzz.spatialBlend = 1f;
				buzz.rolloffMode = AudioRolloffMode.Linear;
				buzz.minDistance = 1f;
				buzz.dopplerLevel = 0f;
			}
			buzz.maxDistance = Mathf.Max(1.5f, hearRange);
			buzz.minDistance = Mathf.Clamp(fullVolumeRange, 0.5f, buzz.maxDistance - 0.5f);
			buzz.clip = clip;
			buzz.volume = buzzVolume;
			buzz.pitch = Random.Range(0.92f, 1.08f);
			buzz.Play();
			buzzUntil = Time.time + clip.length / buzz.pitch;
		}

		private void OnGrabbed()
		{
			// Rebuilt from native code (token 0x06000104, RVA 0x2718E34)
			flying = false;
			actedAt = Time.time;
			nextStillHere = Time.time + StillHereEvery;
			if (door)
			{
				door.Grabbed(Id, transform.position);
			}
		}

		private void OnReleased(Vector3 velocity)
		{
			// Rebuilt from native code (token 0x06000105, RVA 0x2718EFC)
			actedAt = releasedAt = Time.time;
			flying = true;
			stillFor = 0f;
			NetBridge.ReportFuseThrow(Id, transform.position, velocity);
		}

		internal void Abandon()
		{
			// Rebuilt from native code (token 0x06000106, RVA 0x2718FD4)
			if (Item.IsHeld || Item.Stowed)
			{
				Item.ForceDrop();
			}
			flying = false;
			actedAt = -10f;
		}

		internal bool SlotOnRelease()
		{
			// Rebuilt from native code (token 0x06000107, RVA 0x271902C)
			if (!door || !Item.IsHeld)
			{
				return false;
			}
			return door.TrySlot(this, door.releaseReach);
		}

		internal void MarkActed()
		{
			// Rebuilt from native code (token 0x06000108, RVA 0x27190CC)
			actedAt = Time.time;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000109, RVA 0x27190E8)
			if (Item.IsHeld && door)
			{
				door.TrySlot(this);
			}
			if ((Item.IsHeld || Item.Stowed) && door && door.HeldByMe(Id) && Time.time >= nextStillHere)
			{
				nextStillHere = Time.time + StillHereEvery;
				door.Send(0, Id, transform.position);
			}
			if (Time.time >= nextBuzz)
			{
				nextBuzz = Time.time + Random.Range(Mathf.Min(buzzEvery.x, buzzEvery.y), Mathf.Max(buzzEvery.x, buzzEvery.y));
				if (Lying)
				{
					Crackle();
				}
			}
			if (!core)
			{
				return;
			}
			float pulse = MathF.Sin(Time.time * 4f + Id) * 0.4f + 0.6f;
			if (Time.time < buzzUntil)
			{
				pulse *= Random.value < 0.5f ? 0.3f : 2.4f;
			}
			Material material = core.sharedMaterial;
			if (!material || !material.GetTexture(EmissionMapId))
			{
				block.SetColor(BaseColorId, glow);
			}
			block.SetColor(EmissionId, glow * (pulse * 1.5f));
			core.SetPropertyBlock(block);
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600010A, RVA 0x2719490)
			PhysicsClock.Step += PhysicsStep;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600010B, RVA 0x2719534)
			PhysicsClock.Step -= PhysicsStep;
		}

		private void PhysicsStep(float dt)
		{
			// Rebuilt from native code (token 0x0600010C, RVA 0x27195D8)
			if (!flying)
			{
				return;
			}
			stillFor = Item.Moving ? 0f : stillFor + dt;
			bool lost = FuseDoor.Lost(transform.position);
			if (stillFor > 0.4f || Time.time - releasedAt > 5f || lost)
			{
				flying = false;
				actedAt = Time.time;
				if (door)
				{
					door.Send(1, Id, transform.position);
				}
			}
		}
	}
}
