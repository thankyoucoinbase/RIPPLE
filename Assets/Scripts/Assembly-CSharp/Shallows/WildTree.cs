using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(130)]
	public class WildTree : MonoBehaviour
	{
		public const float RumbleAt = 0f;

		public const float HeaveAt = 0.35f;

		public const float HeaveEnd = 0.8f;

		public const float TrunkAt = 0.55f;

		public const float TrunkEnd = 2.25f;

		public const float LeavesAt = 1.85f;

		public const float LeavesEnd = 2.6f;

		public const float BunchAt = 2.55f;

		public const float BunchEnd = 3.1f;

		public const float GrowTime = 3.3f;

		public const float SinkTime = 1.6f;

		public const float SwayTime = 1.2f;

		private const string Folder = "WildTree/";

		private const string Model = "WildBananaTree";

		private static readonly Color LeafGreen = new Color(0.35f, 0.62f, 0.2f, 1f);

		private static readonly Color LeafLight = new Color(0.55f, 0.78f, 0.3f, 1f);

		private static readonly Color Gold = new Color(1f, 0.8f, 0.3f, 1f);

		public Action LocalRip;

		private Transform trunkPivot;

		private Transform leavesPivot;

		private Transform tilesPivot;

		private Transform bunchGrow;

		private Transform bunch;

		private Transform halo;

		private Vector3 leavesScale = Vector3.one;

		private BananaRip rip;

		private Transform hand;

		private Vector3 handOffset;

		private Quaternion handTurn;

		private AudioSource voice;

		private ParticleSystem bits;

		private Material haloMaterial;

		private float grown;

		private float sinking = -1f;

		private float swayAt = -100f;

		private float ripAt;

		private bool bunchGone;

		private bool sinkDone;

		private bool crackPlayed;

		private bool grewPlayed;

		private bool leavesPlayed;

		private bool bloomPlayed;

		private bool rumbled;

		private Vector3 water;

		public int Level { get; private set; }

		public Vector3 BunchPosition
		{
			get
			{
				// Rebuilt from native code (token 0x06000395, RVA 0x27984F4)
				if (!bunch)
				{
					return transform.position + Vector3.up;
				}
				return bunch.position;
			}
		}

		public bool Ripe
		{
			get
			{
				// Rebuilt from native code (token 0x06000396, RVA 0x27985F4)
				return grown >= GrowTime && sinking < 0f && rip && rip.enabled && !rip.Ripped && !bunchGone;
			}
		}

		public bool RippedHere
		{
			get
			{
				// Rebuilt from native code (token 0x06000397, RVA 0x27986B4)
				return rip && rip.Ripped;
			}
		}

		public bool Sinking
		{
			get
			{
				// Rebuilt from native code (token 0x060003A7, RVA 0x279D1B0)
				return sinking >= 0f;
			}
		}

		public static WildTree Grow(int level, Vector3 floor, Vector3 branchToward, float age, bool withBunch)
		{
			// Rebuilt from native code (token 0x06000398, RVA 0x2798738)
			GameObject prefab = Resources.Load<GameObject>(Folder + Model);
			if (!prefab)
			{
				Debug.LogWarning("Shallows: Resources/WildTree/WildBananaTree is missing, so no wild tree can grow.");
				return null;
			}
			GameObject go = new GameObject("Wild Banana Tree");
			go.transform.position = floor;
			WildTree tree = go.AddComponent<WildTree>();
			tree.Level = level;
			tree.Build(prefab, branchToward);
			tree.grown = Mathf.Max(0f, age);
			bool grownUp = age > 0.5f;
			tree.crackPlayed = grownUp;
			tree.grewPlayed = grownUp;
			tree.leavesPlayed = grownUp;
			tree.bloomPlayed = grownUp;
			tree.rumbled = grownUp;
			if (!withBunch)
			{
				tree.HideBunch(false);
			}
			tree.Pose();
			return tree;
		}

		private void Build(GameObject prefab, Vector3 branchToward)
		{
			// Rebuilt from native code (token 0x06000399, RVA 0x2798964)
			GameObject model = Instantiate(prefab, transform);
			model.name = Model;
			model.transform.localPosition = Vector3.zero;
			foreach (Collider collider in model.GetComponentsInChildren<Collider>())
			{
				Destroy(collider);
			}
			Transform anchor = LifeguardProps.Find(model.transform, "Bunch Anchor");
			Vector3 reach = anchor ? anchor.position - transform.position : transform.right;
			Vector3 from = new Vector3(reach.x, 0f, reach.z);
			Vector3 to = new Vector3(branchToward.x, 0f, branchToward.z);
			if (from.sqrMagnitude > 0.0001f && to.sqrMagnitude > 0.0001f)
			{
				transform.rotation = Quaternion.FromToRotation(from.normalized, to.normalized) * transform.rotation;
			}
			Transform trunk = LifeguardProps.Find(model.transform, "Wild Tree Trunk");
			Transform leaves = LifeguardProps.Find(model.transform, "Wild Tree Leaves");
			Transform tiles = LifeguardProps.Find(model.transform, "Wild Tree Tiles");
			WildTreeMaterials.Set materials = WildTreeMaterials.Get(transform.position);
			trunkPivot = Pivot("Trunk", transform.position);
			if (trunk)
			{
				trunk.SetParent(trunkPivot, true);
				Paint(trunk, materials.bark);
			}
			Vector3 leavesAt = transform.position + Vector3.up * 2.3f;
			if (leaves)
			{
				Renderer renderer = leaves.GetComponent<Renderer>();
				if (renderer)
				{
					leavesAt = renderer.bounds.center;
				}
				leaves.name = "Tree Leaves";
			}
			leavesPivot = Pivot("Leaves", leavesAt);
			leavesPivot.SetParent(trunkPivot, true);
			if (leaves)
			{
				leaves.SetParent(leavesPivot, true);
				Paint(leaves, materials.leaves);
			}
			tilesPivot = Pivot("Tiles", transform.position);
			if (tiles)
			{
				tiles.SetParent(tilesPivot, true);
				Paint(tiles, materials.tiles);
			}
			Vector3 bunchAt = anchor ? anchor.position : transform.position + transform.right * 0.7f + Vector3.up;
			bunchGrow = Pivot("Bunch Grow", bunchAt);
			bunchGrow.SetParent(trunkPivot, true);
			GameObject bunchPrefab = Resources.Load<GameObject>("BananaHill/BananaBunch");
			if (bunchPrefab)
			{
				bunch = new GameObject("Wild Bunch").transform;
				bunch.SetParent(bunchGrow, false);
				bunch.position = bunchAt;
				GameObject bananas = Instantiate(bunchPrefab, bunch);
				bananas.name = "BananaBunch";
				bananas.transform.localPosition = Vector3.zero;
				foreach (Collider collider in bananas.GetComponentsInChildren<Collider>())
				{
					Destroy(collider);
				}
				Transform stalk = LifeguardProps.Find(bananas.transform, "Stalk Top");
				if (stalk)
				{
					bunch.position += bunchAt - stalk.position;
				}
				Paint(bananas.transform, materials.banana);
				Material haloSource = Resources.Load<Material>("WelcomeGift/WelcomeGift Halo");
				if (haloSource)
				{
					GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
					Destroy(quad.GetComponent<Collider>());
					quad.name = "Halo";
					quad.layer = 2;
					halo = quad.transform;
					halo.SetParent(bunch, false);
					haloMaterial = new Material(haloSource);
					if (haloMaterial.HasProperty("_Size"))
					{
						haloMaterial.SetFloat("_Size", 0.9f);
					}
					MeshRenderer haloRenderer = quad.GetComponent<MeshRenderer>();
					haloRenderer.sharedMaterial = haloMaterial;
					haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
				}
			}
			voice = gameObject.AddComponent<AudioSource>();
			voice.playOnAwake = false;
			voice.spatialBlend = 1f;
			voice.minDistance = 2.5f;
			voice.maxDistance = 32f;
			voice.rolloffMode = AudioRolloffMode.Linear;
			voice.dopplerLevel = 0f;
			bits = MakeBits();
			WaterVolume volume = WaterVolume.At(transform.position);
			water = new Vector3(transform.position.x, volume ? volume.SurfaceY : transform.position.y + 0.28f, transform.position.z);
		}

		private Transform Pivot(string name, Vector3 at)
		{
			// Rebuilt from native code (token 0x0600039A, RVA 0x279ABAC)
			Transform pivot = new GameObject(name).transform;
			pivot.SetParent(transform, false);
			pivot.position = at;
			pivot.rotation = transform.rotation;
			return pivot;
		}

		private static void Paint(Transform root, Material mat)
		{
			// Rebuilt from native code (token 0x0600039B, RVA 0x279ACA8)
			if (!mat)
			{
				return;
			}
			foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
			{
				Material[] materials = new Material[Mathf.Max(renderer.sharedMaterials.Length, 1)];
				for (int i = 0; i < materials.Length; i++)
				{
					materials[i] = mat;
				}
				renderer.sharedMaterials = materials;
				renderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600039C, RVA 0x279B318)
			if (sinking >= 0f)
			{
				sinking += Time.deltaTime;
				PoseSinking(sinking / SinkTime);
				if (sinking >= SinkTime && !sinkDone)
				{
					sinkDone = true;
					Destroy(gameObject, 0.1f);
				}
				return;
			}
			if (grown < GrowTime)
			{
				float before = grown;
				grown += Time.deltaTime;
				Cues(before, grown);
				Pose();
				if (grown >= GrowTime)
				{
					Ready();
				}
				return;
			}
			Sway();
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x0600039D, RVA 0x279BF78)
			if (!rip || bunchGone || !rip.Ripped)
			{
				return;
			}
			if (!hand)
			{
				hand = rip.RippedBy;
				if (hand)
				{
					handOffset = hand.InverseTransformPoint(bunch.position);
					handTurn = Quaternion.Inverse(hand.rotation) * bunch.rotation;
				}
				ripAt = Time.time;
				swayAt = Time.time;
				Play(Sfx.TreeRustle(), 0.7f, 1f);
				if (SplashSystem.I)
				{
					SplashSystem.I.Loud(water, 0.35f, SplashKind.Object);
				}
				Chips(BunchPosition, 6, LeafGreen, 1.2f);
				LocalRip?.Invoke();
			}
			if (hand)
			{
				bunch.SetPositionAndRotation(hand.TransformPoint(handOffset), hand.rotation * handTurn);
			}
		}

		private void Cues(float before, float now)
		{
			// Rebuilt from native code (token 0x0600039E, RVA 0x279B724)
			if (now >= RumbleAt && !rumbled)
			{
				rumbled = true;
				Play(Sfx.TreeRumble(), 0.9f, 1f);
			}
			if (now < HeaveEnd && SplashSystem.I && Mathf.FloorToInt(before * 8f) != Mathf.FloorToInt(now * 8f))
			{
				SplashSystem.I.Ripple(water, Mathf.Lerp(0.25f, 0.6f, now / HeaveEnd));
			}
			if (now >= HeaveAt && !crackPlayed)
			{
				crackPlayed = true;
				Play(Sfx.TreeCrack(), 1f, 1f);
				if (SplashSystem.I)
				{
					SplashSystem.I.Visual(water, 0.75f);
				}
				Chips(water, 26, new Color(0.85f, 0.9f, 0.88f, 1f), 2.2f);
			}
			if (now >= TrunkAt && !grewPlayed)
			{
				grewPlayed = true;
				Play(Sfx.TreeGrow(), 0.85f, 1f);
				if (SplashSystem.I)
				{
					SplashSystem.I.Visual(water, 0.45f);
				}
			}
			if (now >= 1.95f && !leavesPlayed)
			{
				leavesPlayed = true;
				Play(Sfx.TreeLeaves(), 0.9f, 1f);
				Leaves(leavesPivot.position, 40);
			}
			if (now >= BunchAt && !bloomPlayed)
			{
				bloomPlayed = true;
				Play(Sfx.TreeBloom(), 0.75f, 1f);
				if (bunch)
				{
					Sparkle(bunchGrow.position, 24);
				}
			}
		}

		private void Pose()
		{
			// Rebuilt from native code (token 0x0600039F, RVA 0x2799B20)
			float t = grown;
			float heave = Phase(t, HeaveAt, HeaveEnd);
			float rise = BackOut(heave, 2.2f);
			float shake = t < TrunkEnd ? MathF.Sin(t * 70f) * 0.012f * (1f - Phase(t, HeaveAt, TrunkEnd)) : 0f;
			tilesPivot.localPosition = new Vector3(0f, Mathf.Lerp(-0.18f, 0f, rise) + shake, 0f);
			tilesPivot.gameObject.SetActive(heave > 0f);
			float trunk = Phase(t, TrunkAt, TrunkEnd);
			float height = BackOut(Mathf.Clamp01(trunk * 1.15f), 1.4f);
			float girth = Mathf.Lerp(0.18f, 1f, Smooth(Phase(t, HeaveEnd, 2.3f))) * Mathf.Min(1f, height * 3f);
			trunkPivot.localScale = new Vector3(Mathf.Max(0.001f, girth), Mathf.Max(0.001f, height), Mathf.Max(0.001f, girth));
			trunkPivot.localRotation = Quaternion.Euler(0f, (1f - Smooth(trunk)) * 70f, 0f);
			trunkPivot.gameObject.SetActive(trunk > 0f);
			float leaves = BackOut(Phase(t, LeavesAt, LeavesEnd), 2.6f);
			leavesPivot.localScale = leavesScale * Mathf.Max(0.001f, leaves);
			leavesPivot.gameObject.SetActive(leaves > 0f);
			if (bunchGrow && !bunchGone)
			{
				float grow = BackOut(Phase(t, BunchAt, BunchEnd), 3f);
				bunchGrow.localScale = Vector3.one * Mathf.Max(0.001f, grow);
				bunchGrow.gameObject.SetActive(grow > 0f);
			}
			if (haloMaterial && haloMaterial.HasProperty("_Size"))
			{
				haloMaterial.SetFloat("_Size", Smooth(Phase(t, 2.75f, GrowTime)) * 0.9f);
			}
		}

		private void Ready()
		{
			// Rebuilt from native code (token 0x060003A0, RVA 0x279BD58)
			grown = GrowTime;
			Pose();
			swayAt = Time.time;
		}

		private void Sway()
		{
			// Rebuilt from native code (token 0x060003A1, RVA 0x279BD84)
			float t = Time.time - swayAt;
			if (t > SwayTime)
			{
				trunkPivot.localRotation = Quaternion.identity;
				if (bunch && !bunchGone && !rip)
				{
					rip = bunch.gameObject.AddComponent<BananaRip>();
					rip.spinSpeed = 0f;
				}
				return;
			}
			float angle = 3.5f * MathF.Exp(t * -3.5f) * MathF.Sin(t * 11f) * (1f - t / SwayTime);
			trunkPivot.localRotation = Quaternion.Euler(angle * 0.6f, 0f, angle);
		}

		private void PoseSinking(float u)
		{
			// Rebuilt from native code (token 0x060003A2, RVA 0x279B464)
			u = Mathf.Clamp01(u);
			float sink = Smooth(u);
			trunkPivot.localScale = new Vector3(Mathf.Lerp(1f, 0.3f, sink), Mathf.Max(0.001f, 1f - sink), Mathf.Lerp(1f, 0.3f, sink));
			leavesPivot.localScale = leavesScale * Mathf.Max(0.001f, 1f - Smooth(Mathf.Clamp01(u * 1.6f)));
			tilesPivot.localPosition = new Vector3(0f, Mathf.Lerp(0f, -0.2f, Smooth(Phase(u, TrunkAt, 1f))), 0f);
			if (bunchGrow && !bunchGone)
			{
				bunchGrow.localScale = Vector3.one * Mathf.Max(0.001f, 1f - Smooth(Mathf.Clamp01(u * 2.5f)));
			}
		}

		public void Taken(bool mine)
		{
			// Rebuilt from native code (token 0x060003A3, RVA 0x279CD28)
			if (bunchGone)
			{
				return;
			}
			Vector3 at = BunchPosition;
			if (mine)
			{
				Sparkle(at, 40);
				Leaves(at, 10);
				XRHands.HapticBoth(0.6f, 0.15f);
				StartCoroutine(Collect(at));
			}
			else
			{
				if (!RippedHere)
				{
					Play(Sfx.TreeRustle(), 0.7f, 1.05f);
				}
				Sparkle(at, 18);
				Leaves(at, 14);
			}
			swayAt = Time.time;
			HideBunch(false);
		}

		private IEnumerator Collect(Vector3 at)
		{
			// Rebuilt from native code (token 0x060003A4, RVA 0x279CEC4; MoveNext token 0x060003B6, RVA 0x279D4F4)
			PlayerRig rig = PlayerRig.I;
			for (int i = 0; i < 5; i++)
			{
				Vector3 to = rig && rig.head ? rig.head.position + Vector3.down * 0.25f : at;
				Sparkle(Vector3.Lerp(at, to, (i + 1) / 5f), 6);
				if (GameManager.I)
				{
					GameManager.I.PlayLocal(Sfx.GiftBananaCollect(), 0.5f, 0.9f + i * 0.06f);
				}
				XRHands.HapticBoth(0.25f + i * 0.08f, 0.04f);
				yield return new WaitForSeconds(0.09f);
			}
			if (GameManager.I)
			{
				GameManager.I.PlayLocal(Sfx.BananaCollect(), 0.8f, 1f);
			}
		}

		private void HideBunch(bool now)
		{
			// Rebuilt from native code (token 0x060003A5, RVA 0x2799A50)
			bunchGone = true;
			if (rip)
			{
				rip.enabled = false;
			}
			if (bunchGrow)
			{
				bunchGrow.gameObject.SetActive(false);
			}
		}

		public void Sink(bool quietly = false)
		{
			// Rebuilt from native code (token 0x060003A6, RVA 0x279CF7C)
			if (sinking >= 0f)
			{
				return;
			}
			if (quietly)
			{
				Destroy(gameObject);
				return;
			}
			if (grown < GrowTime)
			{
				grown = GrowTime;
			}
			Pose();
			sinking = 0f;
			Play(Sfx.TreeRumble(), 0.55f, 1.1f);
			Play(Sfx.TreeLeaves(), 0.5f, 0.9f);
			if (SplashSystem.I)
			{
				SplashSystem.I.Visual(water, 0.5f);
			}
			if (!bunchGone)
			{
				HideBunch(false);
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060003A8, RVA 0x279D1C0)
			if (haloMaterial)
			{
				Destroy(haloMaterial);
			}
		}

		private void Play(AudioClip clip, float volume, float pitch)
		{
			// Rebuilt from native code (token 0x060003A9, RVA 0x279C3B4)
			if (!clip || !voice)
			{
				return;
			}
			voice.pitch = Sfx.PitchFor(clip, pitch);
			voice.PlayOneShot(clip, volume);
		}

		private ParticleSystem MakeBits()
		{
			// Rebuilt from native code (token 0x060003AA, RVA 0x279AE60)
			Material pixel = Resources.Load<Material>("WelcomeGift/WelcomeGift Pixel");
			if (!pixel)
			{
				return null;
			}
			GameObject go = new GameObject("Bits");
			go.transform.SetParent(transform, false);
			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			ParticleSystem.MainModule main = system.main;
			main.playOnAwake = false;
			main.loop = false;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 300;
			main.gravityModifier = 0.6f;
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = system.shape;
			shape.enabled = false;
			ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));
			ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
			limit.enabled = true;
			limit.drag = 1.5f;
			ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
			rotation.enabled = true;
			rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
			ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
			renderer.sharedMaterial = pixel;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			system.Play();
			return system;
		}

		private void Emit(Vector3 at, Vector3 velocity, float size, Color colour, float life, float gravity = 1f)
		{
			// Rebuilt from native code (token 0x060003AB, RVA 0x279D24C)
			if (!bits)
			{
				return;
			}
			ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
			{
				applyShapeToPosition = false,
				position = at,
				velocity = velocity,
				startSize = size,
				startColor = colour,
				startLifetime = life,
				rotation = Random.Range(0f, 360f)
			};
			bits.Emit(emit, 1);
		}

		private void Chips(Vector3 at, int n, Color colour, float speed)
		{
			// Rebuilt from native code (token 0x060003AC, RVA 0x279C4C4)
			for (int i = 0; i < n; i++)
			{
				Vector3 dir = Random.insideUnitSphere;
				Color tint = Color.Lerp(colour, colour * 0.7f, Random.value);
				tint.a = 1f;
				dir.y = Mathf.Abs(dir.y) + 0.7f;
				Emit(at + Random.insideUnitSphere * 0.3f, dir.normalized * Random.Range(0.5f, 1f) * speed, Random.Range(0.025f, 0.05f), tint, Random.Range(0.5f, 0.9f));
			}
		}

		private void Leaves(Vector3 at, int n)
		{
			// Rebuilt from native code (token 0x060003AD, RVA 0x279C770)
			for (int i = 0; i < n; i++)
			{
				Vector3 dir = Random.insideUnitSphere;
				dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
				Emit(at + Random.insideUnitSphere * 0.5f, dir.normalized * Random.Range(0.8f, 2.4f), Random.Range(0.035f, 0.06f), Color.Lerp(LeafGreen, LeafLight, Random.value), Random.Range(1.2f, 2f));
			}
		}

		private void Sparkle(Vector3 at, int n)
		{
			// Rebuilt from native code (token 0x060003AE, RVA 0x279CA1C)
			for (int i = 0; i < n; i++)
			{
				Emit(at + Random.insideUnitSphere * 0.15f, Random.insideUnitSphere.normalized * Random.Range(0.4f, 1.4f), Random.Range(0.02f, 0.04f), Random.value < 0.4f ? Color.white : Gold, Random.Range(0.4f, 0.8f));
			}
		}

		private static float Phase(float t, float a, float b)
		{
			// Rebuilt from native code (token 0x060003AF, RVA 0x279CCA4)
			return Mathf.Clamp01((t - a) / (b - a));
		}

		private static float Smooth(float u)
		{
			// Rebuilt from native code (token 0x060003B0, RVA 0x279CD10)
			return u * u * (3f - 2f * u);
		}

		private static float BackOut(float u, float overshoot)
		{
			// Rebuilt from native code (token 0x060003B1, RVA 0x279CCCC)
			if (u <= 0f)
			{
				return 0f;
			}
			if (u >= 1f)
			{
				return 1f;
			}
			float k = u - 1f;
			return k * k * (k * (overshoot + 1f) + overshoot) + 1f;
		}
	}
}
