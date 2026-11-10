using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public static class TryOn
	{
		// Rebuilt from native code (token 0x060009EA, RVA 0x274907C)
		private static readonly Dictionary<string, string> picks = new Dictionary<string, string>();

		private static bool active;

		public static bool Active
		{
			get
			{
				// Rebuilt from native code (token 0x060009DB, RVA 0x27471FC)
				return active;
			}
			set
			{
				// Rebuilt from native code (token 0x060009DC, RVA 0x2747254)
				if (active != value)
				{
					active = value;
					Changed?.Invoke();
				}
			}
		}

		// Rebuilt from native code (token 0x060009D8, RVA 0x2746FB4; token 0x060009D9, RVA 0x2747090)
		public static event Action Changed;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060009DA, RVA 0x274716C)
			picks.Clear();
			Changed = null;
			active = false;
		}

		public static string Shown(string slot)
		{
			// Rebuilt from native code (token 0x060009DD, RVA 0x27472F4)
			if (!active)
			{
				return Wallet.Equipped(slot);
			}
			return Look(slot);
		}

		public static string Look(string slot)
		{
			// Rebuilt from native code (token 0x060009DE, RVA 0x274743C)
			Prune();
			if (picks.TryGetValue(slot, out string id))
			{
				return id;
			}
			return Wallet.Equipped(slot);
		}

		public static string Pick(string slot)
		{
			// Rebuilt from native code (token 0x060009DF, RVA 0x2747924)
			Prune();
			if (picks.TryGetValue(slot, out string id))
			{
				return id;
			}
			return null;
		}

		public static bool Wearing(string id)
		{
			// Rebuilt from native code (token 0x060009E0, RVA 0x27479C8)
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length != 0)
			{
				foreach (string piece in pieces)
				{
					if (!Wearing(piece))
					{
						return false;
					}
				}
				return true;
			}
			if (!Cosmetics.TryGet(id, out Cosmetics.Entry entry))
			{
				return false;
			}
			return Look(entry.slot) == id;
		}

		public static void Toggle(string id)
		{
			// Rebuilt from native code (token 0x060009E1, RVA 0x2747B20)
			if (!Cosmetics.TryGet(id, out Cosmetics.Entry entry))
			{
				return;
			}
			if (Wearing(id))
			{
				TakeOffItem(id);
				return;
			}
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length != 0)
			{
				TakeOffOtherSets(id);
			}
			if (Wallet.HasCosmetic(id))
			{
				if (pieces.Length == 0)
				{
					picks.Remove(entry.slot);
				}
				foreach (string piece in pieces)
				{
					if (Cosmetics.TryGet(piece, out Cosmetics.Entry part))
					{
						picks.Remove(part.slot);
					}
				}
				Wallet.Equip(id);
			}
			else if (pieces.Length == 0)
			{
				picks[entry.slot] = id;
			}
			else
			{
				foreach (string piece in pieces)
				{
					if (Cosmetics.TryGet(piece, out Cosmetics.Entry part))
					{
						picks[part.slot] = piece;
					}
				}
			}
			Changed?.Invoke();
		}

		private static void TakeOffOtherSets(string set)
		{
			// Rebuilt from native code (token 0x060009E2, RVA 0x27480F4)
			foreach (string slot in Cosmetics.Slots)
			{
				if (picks.TryGetValue(slot, out string picked) && PieceOfOtherSet(picked, set))
				{
					picks.Remove(slot);
				}
				if (!picks.ContainsKey(slot))
				{
					string worn = Wallet.Equipped(slot);
					if (worn != null && PieceOfOtherSet(worn, set))
					{
						Wallet.Unequip(slot);
					}
				}
			}
		}

		private static bool PieceOfOtherSet(string id, string set)
		{
			// Rebuilt from native code (token 0x060009E3, RVA 0x2748708)
			if (Cosmetics.TryGet(id, out Cosmetics.Entry entry) && !string.IsNullOrEmpty(entry.set))
			{
				return entry.set != set;
			}
			return false;
		}

		public static void TakeOff(string slot)
		{
			// Rebuilt from native code (token 0x060009E4, RVA 0x2748958)
			if (picks.Remove(slot))
			{
				Changed?.Invoke();
			}
			else if (Wallet.Equipped(slot) != null)
			{
				Wallet.Unequip(slot);
			}
		}

		public static void TakeOffItem(string id)
		{
			// Rebuilt from native code (token 0x060009E5, RVA 0x2747E74)
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length == 0)
			{
				if (Cosmetics.TryGet(id, out Cosmetics.Entry entry))
				{
					TakeOff(entry.slot);
				}
				return;
			}
			bool removed = false;
			foreach (string piece in pieces)
			{
				if (Cosmetics.TryGet(piece, out Cosmetics.Entry part) && picks.TryGetValue(part.slot, out string picked) && picked == piece)
				{
					picks.Remove(part.slot);
					removed = true;
				}
			}
			if (removed)
			{
				Changed?.Invoke();
			}
			else
			{
				Wallet.TakeOff(id);
			}
		}

		public static void ClearCart()
		{
			// Rebuilt from native code (token 0x060009E6, RVA 0x2748C28)
			Prune();
			if (picks.Count != 0)
			{
				picks.Clear();
				Changed?.Invoke();
			}
		}

		public static List<string> Cart()
		{
			// Rebuilt from native code (token 0x060009E7, RVA 0x2748D10)
			Prune();
			List<string> cart = new List<string>();
			foreach (string slot in Cosmetics.Slots)
			{
				if (!picks.TryGetValue(slot, out string id))
				{
					continue;
				}
				if (Cosmetics.TryGet(id, out Cosmetics.Entry entry) && !string.IsNullOrEmpty(entry.set))
				{
					id = entry.set;
				}
				if (!cart.Contains(id))
				{
					cart.Add(id);
				}
			}
			return cart;
		}

		public static string SkuFor(string id)
		{
			// Rebuilt from native code (token 0x060009E8, RVA 0x2748F68)
			string fallback = null;
			foreach (MetaIAP.Product product in MetaIAP.Products)
			{
				if (product.cosmetics != null && Array.IndexOf(product.cosmetics, id) >= 0)
				{
					if (product.cosmetics.Length == 1)
					{
						return product.sku;
					}
					if (fallback == null)
					{
						fallback = product.sku;
					}
				}
			}
			return fallback;
		}

		private static void Prune()
		{
			// Rebuilt from native code (token 0x060009E9, RVA 0x274750C)
			if (picks.Count == 0)
			{
				return;
			}
			List<string> owned = null;
			foreach (KeyValuePair<string, string> pick in picks)
			{
				if (Wallet.HasCosmetic(pick.Value))
				{
					if (owned == null)
					{
						owned = new List<string>();
					}
					owned.Add(pick.Key);
				}
			}
			if (owned == null)
			{
				return;
			}
			foreach (string slot in owned)
			{
				picks.Remove(slot);
			}
		}
	}
}
