using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Shallows
{
	public static class PillarColliders
	{
		private const int Sides = 32;

		private static Mesh cylinder;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			// Rebuilt from native code (token 0x060004AF, RVA 0x27B998C)
			SceneManager.sceneLoaded -= OnLoaded;
			SceneManager.sceneLoaded += OnLoaded;
			Fix(SceneManager.GetActiveScene());
		}

		private static void OnLoaded(Scene scene, LoadSceneMode mode)
		{
			// Rebuilt from native code (token 0x060004B0, RVA 0x27B9C70)
			Fix(scene);
		}

		private static void Fix(Scene scene)
		{
			// Rebuilt from native code (token 0x060004B1, RVA 0x27B9A64)
			if (!scene.isLoaded)
			{
				return;
			}
			int fitted = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
				{
					GameObject go = filter.gameObject;
					if (go.name.StartsWith("Pillar") && Fit(go, filter.sharedMesh))
					{
						fitted++;
					}
				}
			}
			if (fitted > 0)
			{
				Debug.Log(string.Format("Shallows: fitted {0} pillar collider(s) in {1} to their shapes.", fitted, scene.name));
			}
		}

		private static bool Fit(GameObject go, Mesh mesh)
		{
			// Rebuilt from native code (token 0x060004B2, RVA 0x27B9C78)
			Collider collider = go.GetComponent<Collider>();
			if (collider && collider.isTrigger)
			{
				return false;
			}
			if (collider is MeshCollider existing && existing.sharedMesh == Cylinder())
			{
				return false;
			}
			PhysicsMaterial material = collider ? collider.sharedMaterial : null;
			string shape = mesh ? mesh.name : "";
			Collider fitted;
			if (collider is BoxCollider || shape.StartsWith("Cube"))
			{
				BoxCollider box = collider as BoxCollider;
				if (!box)
				{
					Remove(collider);
					box = go.AddComponent<BoxCollider>();
				}
				box.center = Vector3.zero;
				box.size = Vector3.one;
				fitted = box;
			}
			else if (collider is CapsuleCollider || shape.StartsWith("Cylinder"))
			{
				MeshCollider meshCollider = collider as MeshCollider;
				if (!meshCollider)
				{
					Remove(collider);
					meshCollider = go.AddComponent<MeshCollider>();
				}
				meshCollider.sharedMesh = null;
				meshCollider.convex = true;
				meshCollider.sharedMesh = Cylinder();
				fitted = meshCollider;
			}
			else
			{
				return false;
			}
			fitted.sharedMaterial = material;
			return true;
		}

		private static Mesh Cylinder()
		{
			// Rebuilt from native code (token 0x060004B3, RVA 0x27BA148)
			if (!cylinder)
			{
				Vector3[] vertices = new Vector3[Sides * 2];
				for (int i = 0; i < Sides; i++)
				{
					float angle = i * MathF.PI * 2f / Sides;
					float x = MathF.Cos(angle) * 0.5f;
					float z = MathF.Sin(angle) * 0.5f;
					vertices[i] = new Vector3(x, -1f, z);
					vertices[i + Sides] = new Vector3(x, 1f, z);
				}
				List<int> triangles = new List<int>();
				for (int i = 0; i < Sides; i++)
				{
					int next = (i + 1) % Sides;
					triangles.AddRange(new int[6] { i, i + Sides, next, next, i + Sides, next + Sides });
					if (i != 0 && i != Sides - 1)
					{
						triangles.AddRange(new int[3] { 0, i, i + 1 });
						triangles.AddRange(new int[3] { Sides, i + Sides + 1, i + Sides });
					}
				}
				Mesh mesh = new Mesh();
				mesh.name = "Pillar Collider Cylinder";
				mesh.vertices = vertices;
				mesh.triangles = triangles.ToArray();
				cylinder = mesh;
				cylinder.RecalculateBounds();
			}
			return cylinder;
		}

		private static void Remove(Collider collider)
		{
			// Rebuilt from native code (token 0x060004B4, RVA 0x27BA4FC)
			if (!collider)
			{
				return;
			}
			collider.enabled = false;
			Object.Destroy(collider);
		}
	}
}
