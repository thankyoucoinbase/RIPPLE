namespace Shallows
{
	public static class ShopCatalogue
	{
		public struct Entry
		{
			public ItemKind kind;

			public string id;

			public string name;

			public string blurb;

			public int price;

			public int count;
		}

		// Rebuilt from native code (token 0x06000977, RVA 0x273CEC0)
		public static readonly Entry[] Items = new Entry[4]
		{
			new Entry
			{
				kind = ItemKind.Pebble,
				id = "pebble",
				name = "PEBBLE",
				blurb = "small splash,\nthe basic decoy",
				price = 100,
				count = 1
			},
			new Entry
			{
				kind = ItemKind.GlowStick,
				id = "glowstick",
				name = "GLOW STICK",
				blurb = "lights the dark\nfor a minute, silent",
				price = 200,
				count = 1
			},
			new Entry
			{
				kind = ItemKind.Duck,
				id = "duck",
				name = "RUBBER DUCK",
				blurb = "bobs and splashes\nfor 4 seconds",
				price = 600,
				count = 1
			},
			new Entry
			{
				kind = ItemKind.Sponge,
				id = "sponge",
				name = "SPONGE",
				blurb = "swells into a raft\nyou can stand on",
				price = 800,
				count = 1
			}
		};

		public static Entry Get(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000974, RVA 0x273CC78)
			foreach (Entry entry in Items)
			{
				if (entry.kind == kind)
				{
					return entry;
				}
			}
			return Items[0];
		}

		public static string Id(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000975, RVA 0x273CD7C)
			return Get(kind).id;
		}

		public static bool TryParse(string id, out ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000976, RVA 0x273CDE4)
			foreach (Entry entry in Items)
			{
				if (entry.id == id)
				{
					kind = entry.kind;
					return true;
				}
			}
			kind = ItemKind.Pebble;
			return false;
		}
	}
}
