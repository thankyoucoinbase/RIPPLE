using System;
using System.Collections.Generic;

namespace Shallows
{
	public static class Cosmetics
	{
		public struct Entry
		{
			public string id;

			public string name;

			public string slot;

			public int pearls;

			public int bananas;

			public string model;

			public string set;

			public bool PaidOnly
			{
				get
				{
					// Rebuilt from native code (token 0x060008C5, RVA 0x2836090)
					if (pearls != 0 || bananas != 0)
					{
						return false;
					}
					if (string.IsNullOrEmpty(set))
					{
						return true;
					}
					return TryGet(set, out Entry setEntry) && setEntry.PaidOnly;
				}
			}
		}

		public const string Outfit = "outfit";

		public const string Head = "head";

		public const string Face = "face";

		public const string Neck = "neck";

		public static readonly string[] Slots = new string[4] { Outfit, Head, Face, Neck };

		// Rebuilt from native code (token 0x060008C4, RVA 0x2833AE4)
		public static readonly Entry[] All = new Entry[48]
		{
			new Entry { id = "lifeguard_set", name = "LIFEGUARD SET", slot = Outfit, pearls = 1820, model = "LifeguardSet" },
			new Entry { id = "snorkel_set", name = "SNORKEL SET", slot = Outfit, pearls = 1950, model = "SnorkelSet" },
			new Entry { id = "luau_set", name = "LUAU SET", slot = Outfit, pearls = 2080, model = "LuauSet" },
			new Entry { id = "retro_swim_set", name = "RETRO SWIM SET", slot = Outfit, pearls = 2080, model = "RetroSwimSet" },
			new Entry { id = "ducky_set", name = "DUCKY SET", slot = Outfit, pearls = 2900, model = "DuckySet" },
			new Entry { id = "party_set", name = "PARTY SET", slot = Outfit, pearls = 3000, model = "PartySet" },
			new Entry { id = "captain_set", name = "CAPTAIN SET", slot = Outfit, pearls = 3100, model = "CaptainSet" },
			new Entry { id = "coconut_armor_set", name = "COCONUT ARMOR SET", slot = Outfit, pearls = 3400, model = "CoconutArmorSet" },
			new Entry { id = "kraken_set", name = "KRAKEN SET", slot = Outfit, model = "KrakenSet" },
			new Entry { id = "anglerfish_set", name = "ANGLERFISH SET", slot = Outfit, model = "AnglerfishSet" },
			new Entry { id = "megalodon_set", name = "MEGALODON SET", slot = Outfit, model = "MegalodonSet" },
			new Entry { id = "deep_diver_set", name = "DEEP DIVER SET", slot = Outfit, model = "DeepDiverSet" },
			new Entry { id = "banana_crown", name = "BANANA CROWN", slot = Head, model = "BananaCrown" },
			new Entry { id = "pirate_eyepatch", name = "PIRATE EYEPATCH", slot = Face, pearls = 250, model = "PirateEyepatch" },
			new Entry { id = "wet_floor_sign", name = "WET FLOOR SIGN", slot = Head, pearls = 350, model = "WetFloorSign" },
			new Entry { id = "star_sunglasses", name = "STAR SUNGLASSES", slot = Face, pearls = 450, model = "StarSunglasses" },
			new Entry { id = "pool_noodle_crown", name = "POOL NOODLE CROWN", slot = Head, pearls = 500, model = "PoolNoodleCrown" },
			new Entry { id = "pineapple_hat", name = "PINEAPPLE HAT", slot = Head, pearls = 600, model = "PineappleHat" },
			new Entry { id = "ice_cream_cone", name = "ICE CREAM CONE", slot = Head, pearls = 650, model = "IceCreamCone" },
			new Entry { id = "sandcastle_hat", name = "SANDCASTLE HAT", slot = Head, pearls = 750, model = "SandcastleHat" },
			new Entry { id = "coral_crown", name = "CORAL CROWN", slot = Head, pearls = 900, model = "CoralCrown" },
			new Entry { id = "crab_buddy", name = "CRAB BUDDY", slot = Head, pearls = 1100, model = "CrabBuddy" },
			new Entry { id = "jellyfish_hat", name = "JELLYFISH HAT", slot = Head, pearls = 1400, model = "JellyfishHat" },
			new Entry { id = "lifeguard_cap", name = "LIFEGUARD CAP", slot = Head, model = "LifeguardSetHead", set = "lifeguard_set" },
			new Entry { id = "lifeguard_sunscreen", name = "LIFEGUARD SUNSCREEN", slot = Face, model = "LifeguardSetFace", set = "lifeguard_set" },
			new Entry { id = "lifeguard_outfit", name = "LIFEGUARD OUTFIT", slot = Outfit, model = "LifeguardSetBody", set = "lifeguard_set" },
			new Entry { id = "snorkel_mask", name = "SNORKEL MASK", slot = Face, model = "SnorkelSetFace", set = "snorkel_set" },
			new Entry { id = "snorkel_outfit", name = "SNORKEL OUTFIT", slot = Outfit, model = "SnorkelSetBody", set = "snorkel_set" },
			new Entry { id = "luau_hat", name = "LUAU HAT", slot = Head, model = "LuauSetHead", set = "luau_set" },
			new Entry { id = "luau_outfit", name = "LUAU OUTFIT", slot = Outfit, model = "LuauSetBody", set = "luau_set" },
			new Entry { id = "retro_swim_cap", name = "RETRO SWIM CAP", slot = Head, model = "RetroSwimSetHead", set = "retro_swim_set" },
			new Entry { id = "retro_swimsuit", name = "RETRO SWIMSUIT", slot = Outfit, model = "RetroSwimSetBody", set = "retro_swim_set" },
			new Entry { id = "ducky_hat", name = "DUCKY HAT", slot = Head, model = "DuckySetHead", set = "ducky_set" },
			new Entry { id = "ducky_floatie", name = "DUCKY FLOATIE", slot = Outfit, model = "DuckySetBody", set = "ducky_set" },
			new Entry { id = "party_hat", name = "PARTY HAT", slot = Head, model = "PartySetHead", set = "party_set" },
			new Entry { id = "party_outfit", name = "PARTY OUTFIT", slot = Outfit, model = "PartySetBody", set = "party_set" },
			new Entry { id = "captain_hat", name = "CAPTAIN HAT", slot = Head, model = "CaptainSetHead", set = "captain_set" },
			new Entry { id = "captain_coat", name = "CAPTAIN COAT", slot = Outfit, model = "CaptainSetBody", set = "captain_set" },
			new Entry { id = "coconut_helmet", name = "COCONUT HELMET", slot = Head, model = "CoconutArmorSetHead", set = "coconut_armor_set" },
			new Entry { id = "coconut_armor", name = "COCONUT ARMOR", slot = Outfit, model = "CoconutArmorSetBody", set = "coconut_armor_set" },
			new Entry { id = "diving_helmet", name = "DIVING HELMET", slot = Head, model = "DeepDiverSetHead", set = "deep_diver_set" },
			new Entry { id = "diving_suit", name = "DIVING SUIT", slot = Outfit, model = "DeepDiverSetBody", set = "deep_diver_set" },
			new Entry { id = "kraken_head", name = "KRAKEN HEAD", slot = Head, model = "KrakenSetHead", set = "kraken_set" },
			new Entry { id = "kraken_armor", name = "KRAKEN ARMOR", slot = Outfit, model = "KrakenSetBody", set = "kraken_set" },
			new Entry { id = "anglerfish_head", name = "ANGLERFISH HEAD", slot = Head, model = "AnglerfishSetHead", set = "anglerfish_set" },
			new Entry { id = "anglerfish_armor", name = "ANGLERFISH ARMOR", slot = Outfit, model = "AnglerfishSetBody", set = "anglerfish_set" },
			new Entry { id = "megalodon_head", name = "MEGALODON HEAD", slot = Head, model = "MegalodonSetHead", set = "megalodon_set" },
			new Entry { id = "megalodon_armor", name = "MEGALODON ARMOR", slot = Outfit, model = "MegalodonSetBody", set = "megalodon_set" }
		};

		private static Dictionary<string, string[]> pieces;

		public static string[] PiecesOf(string id)
		{
			// Rebuilt from native code (token 0x060008C0, RVA 0x28335CC)
			if (pieces == null)
			{
				Dictionary<string, List<string>> bySet = new Dictionary<string, List<string>>();
				foreach (Entry entry in All)
				{
					if (string.IsNullOrEmpty(entry.set))
					{
						continue;
					}
					if (!bySet.TryGetValue(entry.set, out List<string> list))
					{
						list = new List<string>();
						bySet[entry.set] = list;
					}
					list.Add(entry.id);
				}
				pieces = new Dictionary<string, string[]>();
				foreach (KeyValuePair<string, List<string>> pair in bySet)
				{
					pieces[pair.Key] = pair.Value.ToArray();
				}
			}
			if (id != null && pieces.TryGetValue(id, out string[] result))
			{
				return result;
			}
			return Array.Empty<string>();
		}

		public static bool TryGet(string id, out Entry entry)
		{
			// Rebuilt from native code (token 0x060008C1, RVA 0x28330C0)
			foreach (Entry candidate in All)
			{
				if (candidate.id == id)
				{
					entry = candidate;
					return true;
				}
			}
			entry = default;
			return false;
		}

		public static int Index(string id)
		{
			// Rebuilt from native code (token 0x060008C2, RVA 0x2832D58)
			if (!string.IsNullOrEmpty(id))
			{
				for (int i = 0; i < All.Length; i++)
				{
					if (All[i].id == id)
					{
						return i;
					}
				}
			}
			return -1;
		}

		public static string IdAt(int index)
		{
			// Rebuilt from native code (token 0x060008C3, RVA 0x2832F00)
			if (index < 0 || index >= All.Length)
			{
				return null;
			}
			return All[index].id;
		}
	}
}
