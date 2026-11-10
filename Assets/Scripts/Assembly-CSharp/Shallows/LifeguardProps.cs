using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shallows
{
	public static class LifeguardProps
	{
		private const string Folder = "Lifeguard/";

		private const int IgnoreRaycast = 2;

		private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x0600041D, RVA 0x27A81A8)
			materials.Clear();
		}

		public static Transform Dress(Transform root, string model, Vector3 localPosition, Quaternion localRotation, params Transform[] keep)
		{
			// Rebuilt from native code (token 0x0600041E, RVA 0x27A3450)
			Transform holder = root.Find(model);
			Transform instance = holder ? holder.Find(model + " Model") : null;
			if (instance)
			{
				Finish(root, instance, MaterialFor(model, null));
				return instance;
			}
			GameObject prefab = Resources.Load<GameObject>(Folder + model);
			if (!prefab)
			{
				return null;
			}
			List<GameObject> replaced = new List<GameObject>();
			Material template = null;
			foreach (Transform child in root)
			{
				if (child == holder || Array.IndexOf(keep, child) >= 0)
				{
					continue;
				}
				Renderer renderer = child.GetComponent<Renderer>();
				if (!template && renderer && renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseMap"))
				{
					template = renderer.sharedMaterial;
				}
				replaced.Add(child.gameObject);
			}
			foreach (GameObject go in replaced)
			{
				go.SetActive(false);
			}
			if (holder)
			{
				Object.DestroyImmediate(holder.gameObject);
			}
			holder = new GameObject(model).transform;
			holder.SetParent(root, false);
			holder.localPosition = localPosition;
			holder.localRotation = localRotation;
			instance = Object.Instantiate(prefab, holder, false).transform;
			instance.name = model + " Model";
			Align(holder, instance);
			Finish(root, instance, MaterialFor(model, template));
			return instance;
		}

		private static void Finish(Transform root, Transform instance, Material material)
		{
			// Rebuilt from native code (token 0x0600041F, RVA 0x27A8220)
			int layer = root.gameObject.layer == IgnoreRaycast ? 0 : root.gameObject.layer;
			foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
			{
				if (renderer.sharedMaterial != material)
				{
					renderer.sharedMaterial = material;
				}
				if (renderer.gameObject.layer != IgnoreRaycast)
				{
					renderer.gameObject.layer = layer;
				}
				if (!renderer.name.StartsWith("Solid"))
				{
					continue;
				}
				Mesh mesh = renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh : null;
				BoxCollider box = renderer.GetComponent<BoxCollider>();
				if (!box)
				{
					box = renderer.gameObject.AddComponent<BoxCollider>();
				}
				if (mesh && (box.center != mesh.bounds.center || box.size != mesh.bounds.size))
				{
					box.center = mesh.bounds.center;
					box.size = mesh.bounds.size;
				}
			}
		}

		public static PokeButton Button(Transform part, Vector3 pushWorld)
		{
			// Rebuilt from native code (token 0x06000420, RVA 0x27A4C78)
			part.gameObject.layer = IgnoreRaycast;
			part.gameObject.AddComponent<BoxCollider>().isTrigger = true;
			PokeButton button = part.gameObject.AddComponent<PokeButton>();
			button.pressedColor = new Color(1f, 0.85f, 0.45f);
			button.color = Color.white;
			button.pushLocal = part.parent ? part.parent.InverseTransformVector(pushWorld) : pushWorld;
			return button;
		}

		public static Transform Find(Transform root, string name)
		{
			// Rebuilt from native code (token 0x06000421, RVA 0x279A168)
			if (root.name == name)
			{
				return root;
			}
			foreach (Transform child in root)
			{
				Transform found = Find(child, name);
				if (found)
				{
					return found;
				}
			}
			return null;
		}

		public static void Align(Transform holder, Transform instance)
		{
			// Rebuilt from native code (token 0x06000422, RVA 0x27A8624)
			Transform origin = Find(instance, "Origin");
			Transform front = Find(instance, "Front");
			Transform up = Find(instance, "Up");
			Transform right = Find(instance, "Right");
			if (!origin || !front || !up)
			{
				return;
			}
			Vector3 o = holder.InverseTransformPoint(origin.position);
			Vector3 f = holder.InverseTransformPoint(front.position);
			Vector3 u = holder.InverseTransformPoint(up.position);
			Quaternion fix = Quaternion.Inverse(Quaternion.LookRotation(-(f - o), u - o));
			instance.localRotation = fix * instance.localRotation;
			instance.localPosition = fix * instance.localPosition;
			instance.localPosition -= holder.InverseTransformPoint(origin.position);
			if (!right || holder.InverseTransformPoint(right.position).x >= 0f)
			{
				return;
			}
			Debug.LogWarning("Shallows: " + instance.name + " came in mirrored, flipping it back.");
			Vector3 scale = instance.localScale;
			instance.localScale = new Vector3(-scale.x, scale.y, scale.z);
			Vector3 position = instance.localPosition;
			instance.localPosition = new Vector3(-position.x, position.y, position.z);
		}

		public static Material MaterialFor(string model, Material template)
		{
			// Rebuilt from native code (token 0x06000423, RVA 0x27A4868)
			if (materials.TryGetValue(model, out Material material) && material)
			{
				return material;
			}
			material = Resources.Load<Material>(Folder + model + " Material");
			if (!material)
			{
				material = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
				material.name = model + " Material";
				Texture2D albedo = Resources.Load<Texture2D>(Folder + model + "_Albedo");
				if (material.HasProperty("_BaseMap"))
				{
					material.SetTexture("_BaseMap", albedo);
				}
				material.mainTexture = albedo;
				if (material.HasProperty("_BaseColor"))
				{
					material.SetColor("_BaseColor", Color.white);
				}
				material.color = Color.white;
				if (material.HasProperty("_Smoothness"))
				{
					material.SetFloat("_Smoothness", 0.1f);
				}
				if (material.HasProperty("_Metallic"))
				{
					material.SetFloat("_Metallic", 0f);
				}
			}
			materials[model] = material;
			return material;
		}

		public static TextMesh Text(Transform root, Transform model, string anchor, string name, Font font, float height, Color color, TextAnchor alignment = TextAnchor.UpperLeft, float lineSpacing = 1f)
		{
			// Rebuilt from native code (token 0x06000424, RVA 0x27A8A90)
			Transform spot = Find(model, anchor);
			TextAlignment lines = alignment == TextAnchor.UpperLeft || alignment == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
			TextMesh text = WorldText.Create(name, root, Vector3.zero, "", height, color, alignment, lines);
			PixelFonts.Use(text, font, height, lineSpacing);
			if (spot)
			{
				text.transform.position = spot.position;
			}
			text.transform.rotation = model.parent.rotation;
			return text;
		}
	}
}
