using UnityEngine;

namespace Shallows
{
	public static class ExitDoorProps
	{
		private const string Folder = "ExitDoor/";

		private const float WallHalf = 0.25f;

		private const float LampAbove = 0.2f;

		private static Material plain;

		private static Material glowing;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060000F3, RVA 0x27167CC)
			glowing = null;
			plain = null;
		}

		public static bool Dress(FuseDoor fuseDoor)
		{
			// Rebuilt from native code (token 0x060000F4, RVA 0x2716834)
			if (!fuseDoor || !fuseDoor.door || fuseDoor.slots == null || fuseDoor.slots.Length == 0)
			{
				return false;
			}
			if (!Resources.Load<GameObject>(Folder + "ExitDoor"))
			{
				return false;
			}
			Transform root = fuseDoor.transform;
			Transform door = fuseDoor.door;
			bool changed = false;
			Transform metal;
			if (door.name == "Exit Door" && door.Find("Metal Door"))
			{
				metal = door.Find("Metal Door");
			}
			else
			{
				metal = fuseDoor.door;
				door = new GameObject("Exit Door").transform;
				door.SetParent(metal.parent, false);
				door.SetPositionAndRotation(metal.position, metal.rotation);
				metal.SetParent(door, true);
				fuseDoor.door = door;
				changed = true;
			}
			Vector3 scale = metal.lossyScale;
			Vector3 axis = scale.x <= scale.z ? metal.right : metal.forward;
			Vector3 center = Vector3.zero;
			foreach (Transform slot in fuseDoor.slots)
			{
				center += slot.position;
			}
			center /= fuseDoor.slots.Length;
			float side = Vector3.Dot(axis, center - metal.position) >= 0f ? 1f : -1f;
			Vector3 up = metal.up;
			Quaternion rotation = Quaternion.LookRotation(-(axis * side), up);
			changed |= Hide(metal.GetComponent<MeshRenderer>());
			foreach (Transform child in metal)
			{
				changed |= Hide(child.gameObject);
			}
			foreach (Transform child in root)
			{
				if (child.name == "Fuse Box" || child.name == "Socket" || child.name == "Slot Lamp" || child.name == "Alarm Lamp")
				{
					changed |= Hide(child.gameObject);
				}
				TextMesh label = child.GetComponent<TextMesh>();
				if (label && label.text == "FUSES")
				{
					changed |= Hide(child.gameObject);
				}
			}
			Vector3 outward = axis * side;
			Transform shell = Wear(door, "ExitDoor", metal.position, rotation, ref changed);
			Transform frame = Wear(root, "ExitDoorFrame", metal.position + outward * WallHalf - up * (scale.y * 0.5f), rotation, ref changed);
			Transform box = Wear(root, "FuseBox", center, rotation, ref changed);
			if (!shell || !frame || !box)
			{
				return changed;
			}
			Vector3 sum = Vector3.zero;
			int found = 0;
			foreach (string name in new string[] { "Slot A", "Slot B", "Slot C" })
			{
				Transform socket = LifeguardProps.Find(box, name);
				if (socket)
				{
					sum += socket.position;
					found++;
				}
			}
			if (found > 0)
			{
				changed |= MoveBy(box.parent, center - sum / found);
			}
			if (fuseDoor.slotLamps == null || fuseDoor.slotLamps.Length != fuseDoor.slots.Length)
			{
				fuseDoor.slotLamps = new Renderer[fuseDoor.slots.Length];
				changed = true;
			}
			for (int i = 0; i < fuseDoor.slots.Length; i++)
			{
				Renderer best = null;
				float bestDistance = 0.1f;
				foreach (MeshRenderer lamp in box.GetComponentsInChildren<MeshRenderer>(true))
				{
					if (IsLamp(lamp.name))
					{
						float distance = Vector3.Distance(lamp.bounds.center, fuseDoor.slots[i].position + up * LampAbove);
						if (distance < bestDistance)
						{
							bestDistance = distance;
							best = lamp;
						}
					}
				}
				if (best && fuseDoor.slotLamps[i] != best)
				{
					fuseDoor.slotLamps[i] = best;
					changed = true;
				}
			}
			Transform countdownAt = LifeguardProps.Find(frame, "Countdown Text");
			if (countdownAt && fuseDoor.countdown)
			{
				changed |= MoveTo(fuseDoor.countdown.transform, countdownAt.position);
				if (PixelFonts.Screen && fuseDoor.countdown.font != PixelFonts.Screen)
				{
					PixelFonts.Use(fuseDoor.countdown, PixelFonts.Screen, 0.13f, 1f);
					changed = true;
				}
			}
			Transform lens = LifeguardProps.Find(frame, "Alarm Lens");
			if (lens)
			{
				Renderer alarmLamp = lens.GetComponent<Renderer>();
				if (fuseDoor.alarmLamp != alarmLamp)
				{
					fuseDoor.alarmLamp = alarmLamp;
					changed = true;
				}
				if (fuseDoor.alarm)
				{
					changed |= MoveTo(fuseDoor.alarm.transform, alarmLamp.bounds.center + outward * 0.35f);
				}
			}
			return changed;
		}

		private static bool IsLamp(string name)
		{
			// Rebuilt from native code (token 0x060000F5, RVA 0x2718220)
			return name == "Alarm Lens" || (name.StartsWith("Lamp ") && !name.StartsWith("Lamp Bezel"));
		}

		private static Transform Wear(Transform parent, string model, Vector3 position, Quaternion rotation, ref bool changed)
		{
			// Rebuilt from native code (token 0x060000F6, RVA 0x2717C0C)
			Transform holder = parent.Find(model);
			Transform instance = holder ? holder.Find(model + " Model") : null;
			if (!instance)
			{
				GameObject prefab = Resources.Load<GameObject>(Folder + model);
				if (!prefab)
				{
					return null;
				}
				if (holder)
				{
					Object.DestroyImmediate(holder.gameObject);
				}
				holder = new GameObject(model).transform;
				holder.SetParent(parent, false);
				holder.SetPositionAndRotation(position, rotation);
				instance = Object.Instantiate(prefab, holder, false).transform;
				instance.name = model + " Model";
				LifeguardProps.Align(holder, instance);
				changed = true;
			}
			int layer = parent.gameObject.layer;
			foreach (MeshRenderer part in instance.GetComponentsInChildren<MeshRenderer>(true))
			{
				Material material = Material(IsLamp(part.name));
				if (part.sharedMaterial != material)
				{
					part.sharedMaterial = material;
					changed = true;
				}
				if (part.gameObject.layer != layer)
				{
					part.gameObject.layer = layer;
				}
				if (part.name.StartsWith("Solid") && !part.GetComponent<BoxCollider>())
				{
					Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
					BoxCollider collider = part.gameObject.AddComponent<BoxCollider>();
					if (mesh)
					{
						collider.center = mesh.bounds.center;
						collider.size = mesh.bounds.size;
					}
					changed = true;
				}
			}
			return instance;
		}

		private static bool Hide(GameObject go)
		{
			// Rebuilt from native code (token 0x060000F7, RVA 0x2717B78)
			if (!go || !go.activeSelf)
			{
				return false;
			}
			go.SetActive(false);
			return true;
		}

		private static bool Hide(Renderer r)
		{
			// Rebuilt from native code (token 0x060000F8, RVA 0x2717AE4)
			if (!r || !r.enabled)
			{
				return false;
			}
			r.enabled = false;
			return true;
		}

		private static bool MoveTo(Transform t, Vector3 position)
		{
			// Rebuilt from native code (token 0x060000F9, RVA 0x27182E0)
			if ((t.position - position).sqrMagnitude < 1E-06f)
			{
				return false;
			}
			t.position = position;
			return true;
		}

		private static bool MoveBy(Transform t, Vector3 delta)
		{
			// Rebuilt from native code (token 0x060000FA, RVA 0x27181D4)
			return MoveTo(t, t.position + delta);
		}

		private static Material Material(bool lamp)
		{
			// Rebuilt from native code (token 0x060000FB, RVA 0x2718374)
			ref Material slot = ref lamp ? ref glowing : ref plain;
			if (!slot)
			{
				string name = lamp ? "ExitDoor Lamp Material" : "ExitDoor Material";
				slot = Resources.Load<Material>(Folder + name);
				if (!slot)
				{
					slot = new Material(Shader.Find("Universal Render Pipeline/Lit"))
					{
						name = name
					};
					Texture2D albedo = Resources.Load<Texture2D>(Folder + "ExitDoor_Albedo");
					slot.SetTexture("_BaseMap", albedo);
					slot.mainTexture = albedo;
					slot.SetColor("_BaseColor", Color.white);
					slot.SetFloat("_Smoothness", lamp ? 0.6f : 0.15f);
					slot.SetFloat("_Metallic", 0f);
					if (lamp)
					{
						slot.EnableKeyword("_EMISSION");
						slot.SetTexture("_EmissionMap", albedo);
						slot.SetColor("_EmissionColor", Color.black);
						slot.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
					}
				}
			}
			return slot;
		}
	}
}
