using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(110)]
	public class CosmeticLook : MonoBehaviour
	{
		private class SlotLook
		{
			public string model;

			public readonly List<(SkinnedMeshRenderer renderer, int scream)> worn = new List<(SkinnedMeshRenderer renderer, int scream)>();
		}

		private const string Folder = "Cosmetics/";

		private ApeBody ape;

		private SkinnedMeshRenderer body;

		private int bodyScream = -1;

		private readonly SlotLook[] slots = new SlotLook[Cosmetics.Slots.Length];

		private bool followsWallet;

		public static CosmeticLook On(ApeBody ape)
		{
			// Rebuilt from native code (token 0x060008B4, RVA 0x28318F0)
			if (!ape || !ape.Skin)
			{
				return null;
			}
			CosmeticLook look = ape.GetComponent<CosmeticLook>();
			if (look)
			{
				return look;
			}
			look = ape.gameObject.AddComponent<CosmeticLook>();
			look.ape = ape;
			look.body = ape.Skin;
			look.bodyScream = look.body.sharedMesh ? look.body.sharedMesh.GetBlendShapeIndex("Scream") : -1;
			return look;
		}

		public static void FollowWallet(ApeBody ape)
		{
			// Rebuilt from native code (token 0x060008B5, RVA 0x2831A9C)
			CosmeticLook look = On(ape);
			if (!look || look.followsWallet)
			{
				return;
			}
			look.followsWallet = true;
			Wallet.Changed += look.ShowWallet;
			TryOn.Changed += look.ShowWallet;
			look.ShowWallet();
		}

		private void ShowWallet()
		{
			// Rebuilt from native code (token 0x060008B6, RVA 0x2831C00)
			foreach (string slot in Cosmetics.Slots)
			{
				Wear(slot, TryOn.Shown(slot));
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060008B7, RVA 0x2832B30)
			if (followsWallet)
			{
				Wallet.Changed -= ShowWallet;
				TryOn.Changed -= ShowWallet;
			}
		}

		public static int Pack()
		{
			// Rebuilt from native code (token 0x060008B8, RVA 0x2832C44)
			int packed = 0;
			for (int i = 0; i < Cosmetics.Slots.Length; i++)
			{
				packed |= ((Cosmetics.Index(TryOn.Shown(Cosmetics.Slots[i])) + 1) & 0xFF) << (i * 8);
			}
			return packed;
		}

		public static string Unpack(int packed, string slot)
		{
			// Rebuilt from native code (token 0x060008B9, RVA 0x2832E3C)
			int index = Array.IndexOf(Cosmetics.Slots, slot);
			if (index < 0)
			{
				return null;
			}
			return Cosmetics.IdAt(((packed >> (index * 8)) & 0xFF) - 1);
		}

		public void WearPacked(int packed)
		{
			// Rebuilt from native code (token 0x060008BA, RVA 0x2832FB4)
			WearPacked(packed, null);
		}

		public void WearPacked(int packed, Func<string, bool> allowed)
		{
			// Rebuilt from native code (token 0x060008BB, RVA 0x2832FBC)
			foreach (string slot in Cosmetics.Slots)
			{
				string id = Unpack(packed, slot);
				if (id != null && allowed != null && !allowed(id))
				{
					id = null;
				}
				Wear(slot, id);
			}
		}

		public void Wear(string slot, string id)
		{
			// Rebuilt from native code (token 0x060008BC, RVA 0x2831CEC)
			int index = Array.IndexOf(Cosmetics.Slots, slot);
			if (index < 0)
			{
				return;
			}
			string model = null;
			if (!string.IsNullOrEmpty(id) && Cosmetics.TryGet(id, out Cosmetics.Entry entry) && entry.slot == slot)
			{
				model = entry.model;
			}
			SlotLook look = slots[index] ?? (slots[index] = new SlotLook());
			if (model == look.model)
			{
				return;
			}
			look.model = model;
			foreach ((SkinnedMeshRenderer renderer, int scream) worn in look.worn)
			{
				if (worn.renderer)
				{
					Destroy(worn.renderer.gameObject);
				}
			}
			look.worn.Clear();
			if (model == null || !body)
			{
				return;
			}
			GameObject prefab = Resources.Load<GameObject>(Folder + model);
			if (!prefab)
			{
				Debug.LogWarning("Shallows: no cosmetic model at Resources/" + Folder + model);
				return;
			}
			Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
			foreach (Transform bone in body.bones)
			{
				if (bone)
				{
					bones[bone.name] = bone;
				}
			}
			if (body.rootBone)
			{
				bones[body.rootBone.name] = body.rootBone;
			}
			GameObject spawned = Instantiate(prefab);
			foreach (SkinnedMeshRenderer part in spawned.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				Transform[] mapped = new Transform[part.bones.Length];
				bool complete = true;
				for (int i = 0; i < mapped.Length; i++)
				{
					if (!part.bones[i] || !bones.TryGetValue(part.bones[i].name, out mapped[i]))
					{
						complete = false;
					}
				}
				if (!complete)
				{
					Debug.LogWarning("Shallows: " + model + "/" + part.name + " has bones the gorilla doesn't; skipped.");
					continue;
				}
				GameObject wearing = new GameObject("Wearing " + part.name);
				wearing.layer = body.gameObject.layer;
				wearing.transform.SetParent(body.transform.parent, false);
				wearing.transform.localPosition = body.transform.localPosition;
				wearing.transform.localRotation = body.transform.localRotation;
				wearing.transform.localScale = body.transform.localScale;
				SkinnedMeshRenderer skin = wearing.AddComponent<SkinnedMeshRenderer>();
				skin.sharedMesh = part.sharedMesh;
				skin.bones = mapped;
				skin.rootBone = part.rootBone && bones.TryGetValue(part.rootBone.name, out Transform root) ? root : body.rootBone;
				skin.localBounds = body.localBounds;
				skin.updateWhenOffscreen = body.updateWhenOffscreen;
				skin.shadowCastingMode = body.shadowCastingMode;
				Material[] materials = new Material[Mathf.Max(part.sharedMaterials.Length, 1)];
				for (int i = 0; i < materials.Length; i++)
				{
					materials[i] = RippleShop.OutfitMaterial(part.name);
				}
				skin.sharedMaterials = materials;
				skin.enabled = body.enabled;
				look.worn.Add((skin, skin.sharedMesh ? skin.sharedMesh.GetBlendShapeIndex("Scream") : -1));
			}
			Destroy(spawned);
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060008BD, RVA 0x2833278)
			if (!body)
			{
				return;
			}
			float weight = bodyScream >= 0 ? body.GetBlendShapeWeight(bodyScream) : 0f;
			foreach (SlotLook look in slots)
			{
				if (look == null)
				{
					continue;
				}
				foreach ((SkinnedMeshRenderer renderer, int scream) in look.worn)
				{
					if (!renderer)
					{
						continue;
					}
					if (renderer.enabled != body.enabled)
					{
						renderer.enabled = body.enabled;
					}
					if (scream >= 0)
					{
						renderer.SetBlendShapeWeight(scream, weight);
					}
				}
			}
		}
	}
}
