using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace Shallows
{
	public static class MetaIAP
	{
		public struct Product
		{
			public string sku;

			public string name;

			public string price;

			public bool consumable;

			public int pearls;

			public int bananas;

			public int beltSlots;

			public string[] cosmetics;
		}

		public static readonly Product[] Products = new Product[]
		{
			new Product { sku = "pearls_1000", name = "1,000 PEARLS", price = "$4.99", consumable = true, pearls = 1000 },
			new Product { sku = "pearls_2200", name = "2,200 PEARLS", price = "$9.99", consumable = true, pearls = 2200 },
			new Product { sku = "pearls_5000", name = "5,000 PEARLS", price = "$19.99", consumable = true, pearls = 5000 },
			new Product { sku = "pearls_11000", name = "11,000 PEARLS", price = "$39.99", consumable = true, pearls = 11000 },
			new Product { sku = "starter_pack", name = "STARTER PACK", price = "$2.99", pearls = 1500, bananas = 5000, cosmetics = new string[] { "banana_crown" } },
			new Product { sku = "deep_diver_bundle", name = "DEEP DIVER BUNDLE", price = "$14.99", pearls = 2000, cosmetics = new string[] { "deep_diver_set" } },
			new Product { sku = "monsters_of_the_deep", name = "MONSTERS OF THE DEEP BUNDLE", price = "$49.99", pearls = 15000, cosmetics = new string[] { "kraken_set", "anglerfish_set", "megalodon_set" } },
			new Product { sku = "kraken_set", name = "KRAKEN BUNDLE", price = "$24.99", pearls = 5000, cosmetics = new string[] { "kraken_set" } },
			new Product { sku = "anglerfish_set", name = "ANGLERFISH BUNDLE", price = "$24.99", pearls = 5000, cosmetics = new string[] { "anglerfish_set" } },
			new Product { sku = "megalodon_set", name = "MEGALODON BUNDLE", price = "$24.99", pearls = 5000, cosmetics = new string[] { "megalodon_set" } }
		};

		public static event Action PricesChanged;

		private static readonly Dictionary<string, string> metaPrices = new Dictionary<string, string>();

		private static bool buying;

		private static bool fetched;

		public static bool Real
		{
			get
			{
				// Rebuilt from native code (token 0x060006FA, RVA 0x2806E5C)
				return MetaPlatform.UserId != null;
			}
		}

		public static bool Fake
		{
			get
			{
				// Rebuilt from native code (token 0x060006FB, RVA 0x2806EA4)
				return MetaPlatform.UserId == null && (UnityEngine.Application.isEditor || Debug.isDebugBuild);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060006F4, RVA 0x2806900)
			metaPrices.Clear();
			PricesChanged = null;
			buying = false;
			fetched = false;
		}

		public static bool TryGet(string sku, out Product product)
		{
			// Rebuilt from native code (token 0x060006F5, RVA 0x2806990)
			foreach (Product p in Products)
			{
				if (p.sku == sku)
				{
					product = p;
					return true;
				}
			}
			product = default;
			return false;
		}

		public static string Price(string sku)
		{
			// Rebuilt from native code (token 0x060006F6, RVA 0x2806AC0)
			if (metaPrices.TryGetValue(sku, out string price) && !string.IsNullOrEmpty(price))
			{
				return price;
			}
			if (!TryGet(sku, out Product product))
			{
				return "";
			}
			return product.price;
		}

		public static int PearlsIn(string sku)
		{
			// Rebuilt from native code (token 0x060006F7, RVA 0x2806BBC)
			return TryGet(sku, out Product product) ? product.pearls : 0;
		}

		public static string PearlPackFor(int need)
		{
			// Rebuilt from native code (token 0x060006F8, RVA 0x2806C38)
			string best = null;
			int bestPearls = int.MaxValue;
			string biggest = null;
			int most = 0;
			foreach (Product p in Products)
			{
				if (!p.consumable || p.pearls <= 0)
				{
					continue;
				}
				if (p.pearls > most)
				{
					most = p.pearls;
					biggest = p.sku;
				}
				if (p.pearls >= need && p.pearls < bestPearls)
				{
					bestPearls = p.pearls;
					best = p.sku;
				}
			}
			return best ?? biggest;
		}

		public static bool Owned(string sku)
		{
			// Rebuilt from native code (token 0x060006F9, RVA 0x2806D30)
			if (!TryGet(sku, out Product product) || product.consumable)
			{
				return false;
			}
			if (Wallet.HasPurchase(sku))
			{
				return true;
			}
			if (product.cosmetics == null || product.cosmetics.Length == 0)
			{
				return false;
			}
			foreach (string cosmetic in product.cosmetics)
			{
				if (!Wallet.HasCosmetic(cosmetic))
				{
					return false;
				}
			}
			return true;
		}

		public static void FetchPrices()
		{
			// Rebuilt from native code (token 0x060006FC, RVA 0x2806F98)
			if (fetched || MetaPlatform.UserId == null)
			{
				return;
			}
			fetched = true;
			string[] skus = new string[Products.Length];
			for (int i = 0; i < skus.Length; i++)
			{
				skus[i] = Products[i].sku;
			}
			IAP.GetProductsBySKU(skus).OnComplete(OnProducts);
		}

		private static void OnProducts(Message<ProductList> message)
		{
			// Rebuilt from native code (token 0x060006FD, RVA 0x28071AC)
			if (message.IsError || message.Data == null)
			{
				fetched = false;
				Debug.LogWarning("Shallows: couldn't get prices from Meta (" + (message.IsError ? message.GetError().Message : "none") + ").");
				return;
			}
			foreach (Oculus.Platform.Models.Product product in message.Data)
			{
				metaPrices[product.Sku] = product.FormattedPrice;
			}
			if (message.Data.HasNextPage)
			{
				IAP.GetNextProductListPage(message.Data).OnComplete(OnProducts);
			}
			PricesChanged?.Invoke();
		}

		public static void Buy(string sku, Action<bool, string> done)
		{
			// Rebuilt from native code (token 0x060006FE, RVA 0x2807640)
			if (!TryGet(sku, out _))
			{
				done?.Invoke(false, "THAT'S NOT FOR SALE");
				return;
			}
			if (Owned(sku))
			{
				done?.Invoke(false, "YOU ALREADY HAVE THAT");
				return;
			}
			if (buying)
			{
				done?.Invoke(false, "ONE MOMENT...");
				return;
			}
			if (MetaPlatform.UserId == null)
			{
				if (Fake)
				{
					Debug.Log("Shallows: fake checkout for " + sku + " (editor / development build).");
					Wallet.ClaimPurchase(sku, done);
					return;
				}
				done?.Invoke(false, "STORE ISN'T OPEN\ntry again later");
				return;
			}
			buying = true;
			IAP.LaunchCheckoutFlow(sku).OnComplete(message =>
			{
				// Rebuilt from native code (token 0x06000702, RVA 0x28083D0)
				buying = false;
				if (message.IsError)
				{
					Debug.Log("Shallows: checkout for " + sku + " ended: " + message.GetError().Message);
					done?.Invoke(false, "");
					return;
				}
				Wallet.ClaimPurchase(sku, done);
			});
		}

		public static void ClaimMissed()
		{
			// Rebuilt from native code (token 0x060006FF, RVA 0x28079A4)
			if (MetaPlatform.UserId != null)
			{
				Wallet.ClaimAll();
			}
		}
	}
}
