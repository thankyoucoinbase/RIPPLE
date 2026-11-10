using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	[DefaultExecutionOrder(105)]
	public class BeltLook : MonoBehaviour
	{
		public enum Hand
		{
			Empty = 0,
			BeltItem = 1,
			Other = 2
		}

		public const int Slots = 3;

		private const float SlotDistance = 0.371f;

		private static readonly float[] SlotAngles = new float[3] { -40f, 0f, 40f };

		private const string RingModel = "LifeguardLeaderboard";

		private const string RingPart = "Life Ring";

		private const float SmallRadius = 0.04f;

		private const float FitRadius = 1.6f;

		private const float FitLength = 0.4f;

		private const float Tip = 35f;

		internal const float HoverFar = 0.3f;

		internal const float HoverNear = 0.15f;

		private const float GrowSpeed = 12f;

		private const float OutlineWidth = 0.22f;

		private static readonly Color Navy = new Color(0.075f, 0.125f, 0.23f, 1f);

		private static readonly Color Glow = new Color(1f, 0.8f, 0.3f, 1f);

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private static Mesh ringMesh;

		private static Mesh outlineMesh;

		private static Material ringMaterial;

		private static Material outlineMaterial;

		private static Quaternion ringTurn;

		private static Vector3 ringCentre;

		private static float ringOuter;

		private static float tubeShare;

		private static bool looked;

		private readonly Transform[] anchors = new Transform[3];

		private readonly Renderer[] outlines = new Renderer[3];

		private readonly Transform[] items = new Transform[3];

		private readonly float[] itemRadius = new float[3];

		private readonly float[] itemLength = new float[3];

		private readonly float[] size = new float[3];

		private readonly float[] target = new float[3];

		private readonly float[] glows = new float[3] { -1f, -1f, -1f };

		private Renderer[] renderers;

		private bool floaties = true;

		private MaterialPropertyBlock block;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060008A3, RVA 0x282FC98)
			looked = false;
		}

		public static BeltLook Wear(ApeBody ape, bool floaties = true)
		{
			// Rebuilt from native code (token 0x060008A4, RVA 0x282FCF0)
			if (!ape || !LoadRing())
			{
				return null;
			}
			GameObject belt = new GameObject("Belt");
			belt.layer = ape.gameObject.layer;
			belt.transform.SetParent(ape.transform, false);
			belt.transform.localPosition = ape.WaistPoint;
			BeltLook look = belt.AddComponent<BeltLook>();
			look.floaties = floaties;
			look.Build();
			look.SetVisible(true);
			return look;
		}

		private void Build()
		{
			// Rebuilt from native code (token 0x060008A5, RVA 0x28305DC)
			block = new MaterialPropertyBlock();
			for (int i = 0; i < Slots; i++)
			{
				float angle = SlotAngles[i] * Mathf.Deg2Rad;
				Vector3 outward = new Vector3(MathF.Sin(angle), 0f, MathF.Cos(angle));
				Transform slot = new GameObject("Slot " + (i + 1)).transform;
				slot.gameObject.layer = gameObject.layer;
				slot.SetParent(transform, false);
				slot.SetLocalPositionAndRotation(outward * SlotDistance, Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(-Tip, 0f, 0f));
				anchors[i] = slot;
				MeshRenderer ring = Part("Floatie", slot, ringMesh, ringMaterial);
				ring.transform.SetLocalPositionAndRotation(-(ringTurn * ringCentre) / ringOuter, ringTurn);
				ring.transform.localScale = Vector3.one / ringOuter;
				outlines[i] = Part("Outline", slot, outlineMesh, outlineMaterial);
				size[i] = target[i] = SmallRadius;
			}
			renderers = GetComponentsInChildren<Renderer>(true);
		}

		private MeshRenderer Part(string name, Transform parent, Mesh mesh, Material material)
		{
			// Rebuilt from native code (token 0x060008A6, RVA 0x2830C30)
			GameObject part = new GameObject(name);
			part.layer = gameObject.layer;
			part.transform.SetParent(parent, false);
			part.AddComponent<MeshFilter>().sharedMesh = mesh;
			MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = material;
			meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			return meshRenderer;
		}

		public Transform Slot(int i)
		{
			// Rebuilt from native code (token 0x060008A7, RVA 0x282F660)
			return anchors[i];
		}

		public void ItemPose(int i, out Vector3 position, out Quaternion rotation)
		{
			// Rebuilt from native code (token 0x060008A8, RVA 0x282CFE0)
			rotation = anchors[i].rotation;
			float lift;
			if (items[i] && itemLength[i] > 0f)
			{
				rotation *= Quaternion.Euler(0f, 0f, 90f);
				lift = size[i] * tubeShare + itemRadius[i];
			}
			else
			{
				lift = Lift(size[i], items[i] ? itemRadius[i] : 0f);
			}
			position = anchors[i].position + anchors[i].up * lift;
		}

		public static float Fit(float radius, float length)
		{
			// Rebuilt from native code (token 0x060008A9, RVA 0x282E474)
			return length > 0f ? length * FitLength : radius * FitRadius;
		}

		private static float Lift(float outer, float radius)
		{
			// Rebuilt from native code (token 0x060008AA, RVA 0x2830D70)
			float tube = tubeShare * outer;
			float middle = outer - tube;
			float reach = tube + radius;
			return reach > middle ? Mathf.Sqrt(reach * reach - middle * middle) : 0f;
		}

		public void Show(int i, Transform item, float radius, float length = 0f)
		{
			// Rebuilt from native code (token 0x060008AB, RVA 0x282E3B0)
			items[i] = item;
			itemRadius[i] = radius;
			itemLength[i] = length;
		}

		public void Reach(Vector3 left, Hand leftHolds, float leftFit, Vector3 right, Hand rightHolds, float rightFit)
		{
			// Rebuilt from native code (token 0x060008AC, RVA 0x282E498)
			for (int i = 0; i < Slots; i++)
			{
				if (items[i])
				{
					continue;
				}
				Vector3 slot = anchors[i].position;
				float fit = 0f;
				float near = 0f;
				if (leftHolds == Hand.BeltItem)
				{
					fit = leftFit;
					near = Mathf.InverseLerp(HoverFar, HoverNear, Vector3.Distance(left, slot));
				}
				if (rightHolds == Hand.BeltItem)
				{
					float rightNear = Mathf.InverseLerp(HoverFar, HoverNear, Vector3.Distance(right, slot));
					if (rightNear > near)
					{
						fit = rightFit;
						near = rightNear;
					}
				}
				target[i] = Mathf.Lerp(SmallRadius, Mathf.Max(SmallRadius, fit), near);
				SetGlow(i, near >= 1f ? 1f : 0f);
			}
		}

		public void SetVisible(bool shown)
		{
			// Rebuilt from native code (token 0x060008AD, RVA 0x2830B50)
			foreach (Renderer r in renderers)
			{
				if (r)
				{
					r.enabled = shown && floaties;
				}
			}
		}

		private void SetGlow(int i, float amount)
		{
			// Rebuilt from native code (token 0x060008AE, RVA 0x2830E00)
			if (Mathf.Abs(glows[i] - amount) < 0.01f)
			{
				return;
			}
			glows[i] = amount;
			block.SetColor(BaseColorId, Color.Lerp(Navy, Glow, amount));
			outlines[i].SetPropertyBlock(block);
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060008AF, RVA 0x2830F3C)
			float blend = 1f - MathF.Exp(Time.deltaTime * -GrowSpeed);
			float scale = Mathf.Max(0.0001f, transform.lossyScale.x);
			for (int i = 0; i < Slots; i++)
			{
				if (items[i])
				{
					target[i] = Fit(itemRadius[i], itemLength[i]);
					SetGlow(i, 0f);
				}
				else if (size[i] > target[i] && target[i] <= SmallRadius)
				{
					SetGlow(i, 0f);
				}
				size[i] = Mathf.Lerp(size[i], target[i], blend);
				anchors[i].localScale = Vector3.one * (size[i] / scale);
				if (items[i])
				{
					ItemPose(i, out Vector3 position, out Quaternion rotation);
					items[i].SetPositionAndRotation(position, rotation);
				}
			}
		}

		private static bool LoadRing()
		{
			// Rebuilt from native code (token 0x060008B0, RVA 0x282FEA0)
			if (looked)
			{
				return ringMesh;
			}
			looked = true;
			GameObject model = Resources.Load<GameObject>("Lifeguard/" + RingModel);
			Transform ring = model ? LifeguardProps.Find(model.transform, RingPart) : null;
			MeshFilter filter = ring ? ring.GetComponent<MeshFilter>() : null;
			if (!filter || !filter.sharedMesh)
			{
				Debug.LogWarning("Shallows: the life ring floatie isn't in the lifeguard leaderboard model, so the belt slots don't show.");
				return false;
			}
			ringMesh = filter.sharedMesh;
			Renderer ringRenderer = ring.GetComponent<Renderer>();
			ringMaterial = LifeguardProps.MaterialFor(RingModel, ringRenderer ? ringRenderer.sharedMaterial : null);
			Bounds bounds = ringMesh.bounds;
			Vector3 extents = bounds.extents;
			int axis = extents.x < extents.y ? (extents.x < extents.z ? 0 : 2) : (extents.y < extents.z ? 1 : 2);
			Vector3 normal = Vector3.zero;
			normal[axis] = 1f;
			ringTurn = Quaternion.FromToRotation(normal, Vector3.up);
			ringCentre = bounds.center;
			ringOuter = Mathf.Max(extents[(axis + 1) % 3], extents[(axis + 2) % 3]);
			tubeShare = extents[axis] / 0.8660254f / ringOuter;
			outlineMesh = Torus(1f - tubeShare, tubeShare * 1.22f, 24, 8);
			outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
			outlineMaterial.name = "Belt Floatie Outline";
			outlineMaterial.SetColor(BaseColorId, Navy);
			outlineMaterial.SetFloat("_Cull", 1f);
			return true;
		}

		private static Mesh Torus(float middle, float tube, int around, int round)
		{
			// Rebuilt from native code (token 0x060008B1, RVA 0x28312D8)
			Vector3[] vertices = new Vector3[around * round];
			int[] triangles = new int[around * round * 6];
			for (int i = 0; i < around; i++)
			{
				float a = i * Mathf.PI * 2f / around;
				Vector3 outward = new Vector3(MathF.Sin(a), 0f, MathF.Cos(a));
				int next = (i + 1) % around;
				for (int j = 0; j < round; j++)
				{
					float b = j * Mathf.PI * 2f / round;
					int up = (j + 1) % round;
					vertices[i * round + j] = outward * (MathF.Cos(b) * tube + middle) + Vector3.up * (MathF.Sin(b) * tube);
					int t = (i * round + j) * 6;
					triangles[t] = i * round + j;
					triangles[t + 1] = next * round + j;
					triangles[t + 2] = next * round + up;
					triangles[t + 3] = i * round + j;
					triangles[t + 4] = next * round + up;
					triangles[t + 5] = i * round + up;
				}
			}
			Mesh mesh = new Mesh();
			mesh.name = "Belt Floatie Outline";
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
