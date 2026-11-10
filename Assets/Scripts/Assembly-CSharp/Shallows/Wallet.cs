using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public static class Wallet
	{
		private struct Job
		{
			public string function;

			public Dictionary<string, object> args;

			public Action<Dictionary<string, object>, string> done;

			public int retries;

			public bool retried;
		}

		public const int EscapeReward = 1000;

		public const int TreeReward = 250;

		private const int BananaScale = 10;

		public const int DailyPearls = 50;

		public const int WelcomeBananas = 1000;

		public const int DailySlots = 16;

		public static int Bananas { get; private set; }

		public static int Pearls { get; private set; }

		public static bool OnServer { get; private set; }

		// Rebuilt from native code (token 0x06000A57, RVA 0x2751AFC; token 0x06000A58, RVA 0x2751DD4)
		public static event Action Changed;

		// Rebuilt from native code (token 0x06000A80, RVA 0x275BBA0)
		private static readonly int[] stock = new int[4];

		private static readonly HashSet<string> cosmetics = new HashSet<string>();

		private static readonly Dictionary<string, string> equipped = new Dictionary<string, string>();

		private static readonly HashSet<string> purchases = new HashSet<string>();

		private static int beltBonus;

		private static bool loaded;

		private static bool checking;

		private static bool runOpen;

		private static bool runReported;

		private static int runLevel = -1;

		private static int lastDaily = -1;

		private static double clockOffset;

		private static int welcome = -1;

		private static bool welcomePending;

		private const string WelcomePendingKey = "shallows.welcomePending";

		private const int RunRetries = 3;

		private static Job? waiting;

		private static readonly Queue<Job> jobs = new Queue<Job>();

		private static bool busy;

		private const string BeltFull = "YOUR BELT IS FULL\nuse something first";

		public static int BeltSlots
		{
			get
			{
				// Rebuilt from native code (token 0x06000A47, RVA 0x27564F0)
				return Mathf.Min(3 + beltBonus, 3);
			}
		}

		public static IEnumerable<string> OwnedCosmetics
		{
			get
			{
				// Rebuilt from native code (token 0x06000A4C, RVA 0x2756FF8)
				return cosmetics;
			}
		}

		public static int Owned
		{
			get
			{
				// Rebuilt from native code (token 0x06000A54, RVA 0x27572B4)
				int total = 0;
				foreach (int count in stock)
				{
					total += count;
				}
				return total;
			}
		}

		public static DateTime UtcNow
		{
			get
			{
				// Rebuilt from native code (token 0x06000A6C, RVA 0x275A0B0)
				return DateTime.UtcNow.AddMilliseconds(clockOffset);
			}
		}

		public static int Today
		{
			get
			{
				// Rebuilt from native code (token 0x06000A6D, RVA 0x275A14C)
				return (int)Math.Floor((UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays);
			}
		}

		public static bool DailyReady
		{
			get
			{
				// Rebuilt from native code (token 0x06000A6E, RVA 0x275A28C)
				Load();
				return lastDaily < Today;
			}
		}

		public static TimeSpan UntilNextDaily
		{
			get
			{
				// Rebuilt from native code (token 0x06000A6F, RVA 0x275A2F4)
				return UtcNow.Date.AddDays(1.0) - UtcNow;
			}
		}

		private static bool EditorTest
		{
			get
			{
				// Rebuilt from native code (token 0x06000A71, RVA 0x275A3D8)
				return Application.isEditor;
			}
		}

		public static bool WelcomeWaiting
		{
			get
			{
				// Rebuilt from native code (token 0x06000A73, RVA 0x275A7D8)
				return EditorTest ? welcome != 1 : welcome == 0;
			}
		}

		private static int Found
		{
			get
			{
				// Rebuilt from native code (token 0x06000A77, RVA 0x2759344)
				return Belt.I ? Belt.I.Found : 0;
			}
		}

		public static bool HasCosmetic(string id)
		{
			// Rebuilt from native code (token 0x06000A48, RVA 0x274832C)
			Load();
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			if (cosmetics.Contains(id))
			{
				return true;
			}
			if (!Cosmetics.TryGet(id, out Cosmetics.Entry entry))
			{
				return false;
			}
			if (!string.IsNullOrEmpty(entry.set) && cosmetics.Contains(entry.set))
			{
				return true;
			}
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length == 0)
			{
				return false;
			}
			foreach (string piece in pieces)
			{
				if (!cosmetics.Contains(piece))
				{
					return false;
				}
			}
			return true;
		}

		public static bool Wearing(string id)
		{
			// Rebuilt from native code (token 0x06000A49, RVA 0x2756CCC)
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length == 0)
			{
				return Cosmetics.TryGet(id, out Cosmetics.Entry entry) && Equipped(entry.slot) == id;
			}
			foreach (string piece in pieces)
			{
				if (!Cosmetics.TryGet(piece, out Cosmetics.Entry part) || Equipped(part.slot) != piece)
				{
					return false;
				}
			}
			return true;
		}

		public static void TakeOff(string id)
		{
			// Rebuilt from native code (token 0x06000A4A, RVA 0x2748A68)
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length == 0)
			{
				if (Cosmetics.TryGet(id, out Cosmetics.Entry entry) && Equipped(entry.slot) == id)
				{
					Unequip(entry.slot);
				}
				return;
			}
			foreach (string piece in pieces)
			{
				if (Cosmetics.TryGet(piece, out Cosmetics.Entry part) && Equipped(part.slot) == piece)
				{
					Unequip(part.slot);
				}
			}
		}

		private static void Wear(string id, Cosmetics.Entry entry)
		{
			// Rebuilt from native code (token 0x06000A4B, RVA 0x2756E60)
			string[] pieces = Cosmetics.PiecesOf(id);
			if (pieces.Length == 0)
			{
				equipped[entry.slot] = id;
				return;
			}
			foreach (string piece in pieces)
			{
				if (Cosmetics.TryGet(piece, out Cosmetics.Entry part))
				{
					equipped[part.slot] = piece;
				}
			}
		}

		public static string Equipped(string slot)
		{
			// Rebuilt from native code (token 0x06000A4D, RVA 0x2747398)
			Load();
			return equipped.TryGetValue(slot, out string id) ? id : null;
		}

		public static bool HasPurchase(string sku)
		{
			// Rebuilt from native code (token 0x06000A4E, RVA 0x2757050)
			return purchases.Contains(sku);
		}

		public static int Count(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000A53, RVA 0x2757238)
			return stock[(int)kind];
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000A59, RVA 0x275740C)
			loaded = false;
			checking = false;
			waiting = null;
			OnServer = false;
			busy = false;
			runOpen = false;
			runReported = false;
			runLevel = -1;
			jobs.Clear();
			Changed = null;
			Bananas = 0;
			Pearls = 0;
			lastDaily = -1;
			clockOffset = 0.0;
			welcome = -1;
			welcomePending = false;
			Array.Clear(stock, 0, stock.Length);
			cosmetics.Clear();
			equipped.Clear();
			purchases.Clear();
			beltBonus = 0;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Begin()
		{
			// Rebuilt from native code (token 0x06000A5A, RVA 0x2757614)
			Load();
			PlayFabClient.SignedIn -= OnSignedIn;
			PlayFabClient.SignedIn += OnSignedIn;
			if (PlayFabClient.LoggedIn)
			{
				OnSignedIn();
			}
		}

		public static void RunStarted()
		{
			// Rebuilt from native code (token 0x06000A5B, RVA 0x2757970)
			runOpen = true;
			runLevel = -1;
			runReported = OnServer;
			if (OnServer)
			{
				Run("runStart", null, null, false, RunRetries);
			}
		}

		public static void ReachedLevel(int level)
		{
			// Rebuilt from native code (token 0x06000A5C, RVA 0x2757C0C)
			runLevel = level;
			if (OnServer && runReported)
			{
				Run("runLevel", new Dictionary<string, object> { ["level"] = level }, null, false, RunRetries);
			}
		}

		public static void RunEnded()
		{
			// Rebuilt from native code (token 0x06000A5D, RVA 0x2757D9C)
			runOpen = false;
			runReported = false;
			runLevel = -1;
		}

		public static void Escape(float time, bool counts, Action<int, string> answered = null)
		{
			// Rebuilt from native code (token 0x06000A5E, RVA 0x2757DFC)
			Load();
			int level = runReported ? runLevel : -1;
			runOpen = false;
			runReported = false;
			if (!OnServer)
			{
				Bananas += EscapeReward;
				Save();
				PlayFabClient.SaveBananas(Bananas);
				answered?.Invoke(EscapeReward, "");
				return;
			}
			Dictionary<string, object> args = new Dictionary<string, object>
			{
				["time"] = time,
				["counts"] = counts
			};
			if (level >= 0)
			{
				args["level"] = level;
			}
			Run("escape", args, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A88, RVA 0x275C584)
				if (result == null)
				{
					answered?.Invoke(-1, "");
					return;
				}
				string message = MiniJson.Str(result, "message");
				if (MiniJson.Num(result, "paid") <= 0.0 && (message ?? "") == "NO RUN TO COUNT" && IsTrue(result, "retried"))
				{
					answered?.Invoke(-1, "");
					return;
				}
				answered?.Invoke((int)MiniJson.Num(result, "paid"), message ?? "");
			}, false, RunRetries);
		}

		public static void Tree(int level, Action<int, string> answered = null)
		{
			// Rebuilt from native code (token 0x06000A5F, RVA 0x2758670)
			Load();
			if (!OnServer)
			{
				Bananas += TreeReward;
				Save();
				answered?.Invoke(TreeReward, "");
				return;
			}
			Run("tree", new Dictionary<string, object> { ["level"] = level }, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A8A, RVA 0x275C718)
				if (result == null)
				{
					answered?.Invoke(-1, "");
					return;
				}
				answered?.Invoke((int)MiniJson.Num(result, "paid"), MiniJson.Str(result, "message") ?? "");
			});
		}

		public static void Report(string playFabId, string reason)
		{
			// Rebuilt from native code (token 0x06000A60, RVA 0x2758944)
			if (OnServer && !string.IsNullOrEmpty(playFabId))
			{
				Run("report", new Dictionary<string, object>
				{
					["player"] = playFabId,
					["reason"] = reason
				}, null);
			}
		}

		public static void ReportPlayer(string playFabId, string name, string category, string room, Action<bool> done)
		{
			// Rebuilt from native code (token 0x06000A61, RVA 0x2758AE0)
			if (!OnServer)
			{
				done?.Invoke(false);
				return;
			}
			Dictionary<string, object> args = new Dictionary<string, object>
			{
				["player"] = playFabId ?? "",
				["name"] = name ?? "",
				["category"] = category,
				["room"] = room ?? ""
			};
			Run("report", args, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A8C, RVA 0x275C828)
				done?.Invoke(result != null && IsTrue(result, "ok"));
			});
		}

		public static void Buy(ItemKind kind, Action<bool, string> done)
		{
			// Rebuilt from native code (token 0x06000A62, RVA 0x2758DDC)
			Load();
			ShopCatalogue.Entry entry = ShopCatalogue.Get(kind);
			if (Found + Owned + entry.count > BeltSlots)
			{
				done?.Invoke(false, BeltFull);
				return;
			}
			if (OnServer)
			{
				Run("buy", new Dictionary<string, object> { ["item"] = entry.id }, (result, error) =>
				{
					// Rebuilt from native code (token 0x06000A8E, RVA 0x275C8D0)
					if (result == null)
					{
						done?.Invoke(false, "SHOP IS OFFLINE\ntry again in a moment");
						return;
					}
					if (!IsTrue(result, "ok"))
					{
						done?.Invoke(false, MiniJson.Str(result, "message") ?? "COULDN'T BUY THAT");
						return;
					}
					done?.Invoke(true, Bought(entry));
				});
				return;
			}
			if (Bananas < entry.price)
			{
				done?.Invoke(false, "NOT ENOUGH BANANAS");
				return;
			}
			Bananas -= entry.price;
			stock[(int)kind] = Mathf.Min(stock[(int)kind] + entry.count, 99);
			Save();
			PlayFabClient.SaveBananas(Bananas);
			done?.Invoke(true, Bought(entry));
		}

		public static void Use(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000A63, RVA 0x275951C)
			Load();
			if (stock[(int)kind] < 1)
			{
				return;
			}
			stock[(int)kind]--;
			Save();
			if (OnServer)
			{
				Run("use", new Dictionary<string, object> { ["item"] = ShopCatalogue.Id(kind) }, null);
			}
		}

		public static void Unuse(ItemKind kind)
		{
			// Rebuilt from native code (token 0x06000A64, RVA 0x2759714)
			Load();
			stock[(int)kind]++;
			Save();
			if (OnServer)
			{
				Run("unuse", new Dictionary<string, object> { ["item"] = ShopCatalogue.Id(kind) }, null);
			}
		}

		public static void BuyCosmetic(string id, Action<bool, string> done)
		{
			// Rebuilt from native code (token 0x06000A65, RVA 0x274FB3C)
			Load();
			if (!Cosmetics.TryGet(id, out Cosmetics.Entry entry) || entry.PaidOnly)
			{
				done?.Invoke(false, "THAT'S NOT FOR SALE");
				return;
			}
			if (!string.IsNullOrEmpty(entry.set) && !HasCosmetic(id))
			{
				done?.Invoke(false, "COMES WITH THE SET");
				return;
			}
			if (HasCosmetic(id))
			{
				Equip(id);
				done?.Invoke(true, "YOU'RE WEARING IT");
				return;
			}
			if (OnServer)
			{
				Run("cosmetic", new Dictionary<string, object> { ["id"] = id }, (result, error) =>
				{
					// Rebuilt from native code (token 0x06000A90, RVA 0x275CA48)
					if (result == null)
					{
						done?.Invoke(false, "SHOP IS OFFLINE\ntry again in a moment");
						return;
					}
					if (!IsTrue(result, "ok"))
					{
						done?.Invoke(false, MiniJson.Str(result, "message") ?? "COULDN'T BUY THAT");
						return;
					}
					done?.Invoke(true, "BOUGHT THE " + entry.name + "!\nyou're wearing it");
				});
				return;
			}
			if (entry.pearls > 0 && Pearls < entry.pearls)
			{
				done?.Invoke(false, string.Format("NOT ENOUGH PEARLS\nyou need {0:N0}", entry.pearls));
				return;
			}
			if (entry.bananas > 0 && Bananas < entry.bananas)
			{
				done?.Invoke(false, string.Format("NOT ENOUGH BANANAS\nyou need {0:N0}", entry.bananas));
				return;
			}
			Pearls -= entry.pearls;
			Bananas -= entry.bananas;
			cosmetics.Add(id);
			Wear(id, entry);
			Save();
			if (entry.bananas > 0)
			{
				PlayFabClient.SaveBananas(Bananas);
			}
			done?.Invoke(true, "BOUGHT THE " + entry.name + "!\nyou're wearing it");
		}

		public static void Equip(string id)
		{
			// Rebuilt from native code (token 0x06000A66, RVA 0x2748514)
			Load();
			if (!HasCosmetic(id) || !Cosmetics.TryGet(id, out Cosmetics.Entry entry) || Wearing(id))
			{
				return;
			}
			Wear(id, entry);
			Save();
			if (OnServer)
			{
				Run("equip", new Dictionary<string, object> { ["id"] = id }, null);
			}
		}

		public static void Unequip(string slot)
		{
			// Rebuilt from native code (token 0x06000A67, RVA 0x27487B8)
			Load();
			if (!equipped.Remove(slot))
			{
				return;
			}
			Save();
			if (OnServer)
			{
				Run("equip", new Dictionary<string, object> { ["slot"] = slot }, null);
			}
		}

		public static void ClaimPurchase(string sku, Action<bool, string> done)
		{
			// Rebuilt from native code (token 0x06000A68, RVA 0x27598E4)
			Load();
			if (!MetaIAP.TryGet(sku, out MetaIAP.Product product))
			{
				done?.Invoke(false, "THAT'S NOT FOR SALE");
				return;
			}
			if (OnServer)
			{
				Run("claim", new Dictionary<string, object> { ["sku"] = sku }, (result, error) =>
				{
					// Rebuilt from native code (token 0x06000A92, RVA 0x275CBB8)
					if (result == null)
					{
						done?.Invoke(false, "COULDN'T CLAIM IT YET\nit'll arrive next time you play");
						return;
					}
					if (!IsTrue(result, "ok"))
					{
						done?.Invoke(false, MiniJson.Str(result, "message") ?? "COULDN'T CLAIM THAT");
						return;
					}
					done?.Invoke(true, Thanks(product));
				});
				return;
			}
			if (!Application.isEditor)
			{
				done?.Invoke(false, "COULDN'T CLAIM IT YET\nit'll arrive next time you play");
				return;
			}
			Grant(product);
			done?.Invoke(true, Thanks(product));
		}

		public static void ClaimAll()
		{
			// Rebuilt from native code (token 0x06000A69, RVA 0x2759F2C)
			if (!OnServer)
			{
				return;
			}
			Run("claim", null, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A83, RVA 0x275BDC8)
				List<object> granted = MiniJson.List(result, "granted");
				if (granted != null && granted.Count > 0)
				{
					Debug.Log("Shallows: claimed " + granted.Count + " purchase(s) from Meta that hadn't arrived.");
				}
			});
		}

		private static string Thanks(MetaIAP.Product product)
		{
			// Rebuilt from native code (token 0x06000A6A, RVA 0x2759E98)
			if (product.pearls > 0)
			{
				return string.Format("THANK YOU!\n+{0:N0} PEARLS", product.pearls);
			}
			return "THANK YOU!\nit's yours";
		}

		private static void Grant(MetaIAP.Product product)
		{
			// Rebuilt from native code (token 0x06000A6B, RVA 0x2759C48)
			Pearls += product.pearls;
			Bananas += product.bananas;
			if (product.cosmetics != null)
			{
				foreach (string id in product.cosmetics)
				{
					cosmetics.Add(id);
				}
			}
			beltBonus = Mathf.Min(beltBonus + product.beltSlots, 1);
			if (!product.consumable)
			{
				purchases.Add(product.sku);
			}
			Save();
		}

		public static int DailySlot(int day)
		{
			// Rebuilt from native code (token 0x06000A70, RVA 0x275A3AC)
			uint h = (uint)day;
			h = (h ^ 61) ^ (h >> 16);
			h += h << 3;
			h ^= h >> 4;
			h *= 0x27D4EB2D;
			h ^= h >> 15;
			return (int)(h % (uint)DailySlots);
		}

		public static void ClaimDaily(int slot, Action<bool, string> done)
		{
			// Rebuilt from native code (token 0x06000A72, RVA 0x275A428)
			Load();
			if (OnServer && !EditorTest)
			{
				Run("daily", new Dictionary<string, object> { ["slot"] = slot }, (result, error) =>
				{
					// Rebuilt from native code (token 0x06000A94, RVA 0x275CD30)
					if (result == null)
					{
						done?.Invoke(false, "NO SIGNAL\nTRY AGAIN");
						return;
					}
					if (!IsTrue(result, "ok"))
					{
						done?.Invoke(false, MiniJson.Str(result, "message") ?? "SOLD OUT");
						return;
					}
					done?.Invoke(true, "");
				});
				return;
			}
			if (!DailyReady)
			{
				done?.Invoke(false, "SOLD OUT");
				return;
			}
			if (DailySlot(Today) != slot)
			{
				done?.Invoke(false, "EMPTY");
				return;
			}
			lastDaily = Today;
			Pearls += DailyPearls;
			Save();
			done?.Invoke(true, "");
		}

		public static void ClaimWelcome()
		{
			// Rebuilt from native code (token 0x06000A74, RVA 0x275A874)
			Load();
			if (welcome == 1)
			{
				return;
			}
			welcome = 1;
			Bananas += WelcomeBananas;
			if (EditorTest || !OnServer)
			{
				Save();
				return;
			}
			Changed?.Invoke();
			SetWelcomePending(true);
			Run("welcome", null, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A84, RVA 0x275BEC8)
				if (result == null)
				{
					Debug.LogWarning("Shallows: couldn't reach PlayFab to claim the welcome gift (" + error + "); it'll ask again next time you sign in.");
					return;
				}
				SetWelcomePending(false);
				if (!IsTrue(result, "ok"))
				{
					Debug.Log("Shallows: welcome gift: " + MiniJson.Str(result, "message"));
				}
			});
		}

		private static void SetWelcomePending(bool pending)
		{
			// Rebuilt from native code (token 0x06000A75, RVA 0x275AB18)
			welcomePending = pending;
			if (pending)
			{
				PlayerPrefs.SetInt(WelcomePendingKey, 1);
			}
			else
			{
				PlayerPrefs.DeleteKey(WelcomePendingKey);
			}
			PlayerPrefs.Save();
		}

		private static void ReadWelcome(Dictionary<string, object> result)
		{
			// Rebuilt from native code (token 0x06000A76, RVA 0x275ABB8)
			if (result == null || EditorTest)
			{
				return;
			}
			if (result.TryGetValue("welcome", out object value) && value is bool claimed)
			{
				welcome = claimed || welcomePending ? 1 : 0;
			}
		}

		private static string Bought(ShopCatalogue.Entry entry)
		{
			// Rebuilt from native code (token 0x06000A78, RVA 0x2759418)
			string what = entry.count > 1 ? entry.name : "A " + entry.name;
			return string.Format("BOUGHT {0}!\nit's on your belt ({1}/{2} slots)", what, Found + Owned, BeltSlots);
		}

		private static void OnSignedIn()
		{
			// Rebuilt from native code (token 0x06000A79, RVA 0x2757734)
			if (checking)
			{
				return;
			}
			checking = true;
			Run("wallet", new Dictionary<string, object> { ["local"] = Bananas }, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A85, RVA 0x275C048)
				checking = false;
				if (result == null)
				{
					if (!OnServer)
					{
						Debug.LogWarning("Shallows: the shop script isn't on PlayFab yet (" + error + "), so bananas stay on this headset. Upload PlayFab/cloudscript.js in the PlayFab dashboard: Automation > Revisions (Legacy) > Upload new revision, then Deploy.");
					}
					return;
				}
				if (!OnServer)
				{
					Debug.Log("Shallows: PlayFab keeps your bananas and items now.");
				}
				OnServer = true;
				ReadWelcome(result);
				if (welcomePending && !EditorTest)
				{
					Run("welcome", null, (answer, why) =>
					{
						// Rebuilt from native code (token 0x06000A86, RVA 0x275C520)
						if (answer != null)
						{
							SetWelcomePending(false);
						}
					});
				}
				MetaIAP.ClaimMissed();
				if (!runOpen || runReported || runLevel > 0)
				{
					return;
				}
				runReported = true;
				Run("runStart", null, null, false, RunRetries);
				if (runLevel == 0)
				{
					Run("runLevel", new Dictionary<string, object> { ["level"] = 0 }, null, false, RunRetries);
				}
			}, true);
		}

		private static void Run(string function, Dictionary<string, object> args, Action<Dictionary<string, object>, string> done, bool force = false, int retries = 0)
		{
			// Rebuilt from native code (token 0x06000A7A, RVA 0x2757A9C)
			if (!OnServer && !force)
			{
				return;
			}
			jobs.Enqueue(new Job
			{
				function = function,
				args = args,
				done = done,
				retries = retries
			});
			if (!busy)
			{
				Next();
			}
		}

		private static void Next()
		{
			// Rebuilt from native code (token 0x06000A7B, RVA 0x275ACC8)
			Job job;
			if (waiting == null)
			{
				if (jobs.Count == 0)
				{
					busy = false;
					return;
				}
				job = jobs.Dequeue();
			}
			else
			{
				job = waiting.Value;
				waiting = null;
			}
			busy = true;
			Dictionary<string, object> args = job.args;
			if (job.retried)
			{
				args = job.args == null ? new Dictionary<string, object>() : new Dictionary<string, object>(job.args);
				args["retried"] = true;
			}
			PlayFabClient.RunScript(job.function, args, (result, error) =>
			{
				// Rebuilt from native code (token 0x06000A96, RVA 0x275CE78)
				if (result == null)
				{
					if (job.retries > 0)
					{
						float delay = 2f * (1 << (RunRetries - job.retries));
						Debug.LogWarning(string.Format("Shallows: PlayFab didn't get {0} ({1}), trying again in {2:0} s", job.function, error, delay));
						job.retried = true;
						job.retries--;
						waiting = job;
						WalletTimer.After(delay, Next);
						return;
					}
				}
				else if (jobs.Count == 0)
				{
					Apply(result);
				}
				job.done?.Invoke(result, error);
				Next();
			});
		}

		private static void Apply(Dictionary<string, object> result)
		{
			// Rebuilt from native code (token 0x06000A7C, RVA 0x275AFF8)
			if (result.TryGetValue("bananas", out object bananasValue) && bananasValue is double bananas)
			{
				Bananas = (int)bananas;
			}
			Dictionary<string, object> items = MiniJson.Obj(result, "items");
			if (items != null)
			{
				foreach (ShopCatalogue.Entry item in ShopCatalogue.Items)
				{
					stock[(int)item.kind] = (int)MiniJson.Num(items, item.id);
				}
			}
			if (result.TryGetValue("pearls", out object pearlsValue) && pearlsValue is double pearls)
			{
				Pearls = (int)pearls;
			}
			List<object> owned = MiniJson.List(result, "cosmetics");
			if (owned != null)
			{
				cosmetics.Clear();
				foreach (object o in owned)
				{
					if (o is string id)
					{
						cosmetics.Add(id);
					}
				}
			}
			Dictionary<string, object> worn = MiniJson.Obj(result, "equipped");
			if (worn != null)
			{
				equipped.Clear();
				foreach (KeyValuePair<string, object> pair in worn)
				{
					if (pair.Value is string id)
					{
						equipped[pair.Key] = id;
					}
				}
			}
			List<object> iap = MiniJson.List(result, "iapOwned");
			if (iap != null)
			{
				purchases.Clear();
				foreach (object o in iap)
				{
					if (o is string sku)
					{
						purchases.Add(sku);
					}
				}
			}
			if (result.TryGetValue("beltSlots", out object beltValue) && beltValue is double belt)
			{
				beltBonus = Mathf.Max(0, (int)belt - 3);
			}
			if (!EditorTest && result.TryGetValue("lastDaily", out object dailyValue) && dailyValue is double daily)
			{
				lastDaily = (int)daily;
			}
			if (result.TryGetValue("now", out object nowValue) && nowValue is double now)
			{
				clockOffset = now - (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
			}
			ReadWelcome(result);
			if (result.TryGetValue("best", out object bestValue) && bestValue is double best && best > 0.0)
			{
				PlayFabClient.SubmitEscape((float)best);
			}
			Save();
		}

		private static bool IsTrue(Dictionary<string, object> d, string key)
		{
			// Rebuilt from native code (token 0x06000A7D, RVA 0x275BAF4)
			return d.TryGetValue(key, out object value) && value is bool b && b;
		}

		private static void Load()
		{
			// Rebuilt from native code (token 0x06000A7E, RVA 0x2756558)
			if (loaded)
			{
				return;
			}
			loaded = true;
			Bananas = PlayerPrefs.GetInt("shallows.bananas", 0);
			if (PlayerPrefs.GetInt("shallows.bananaScale", 1) != BananaScale)
			{
				Bananas *= BananaScale;
				PlayerPrefs.SetInt("shallows.bananaScale", BananaScale);
				PlayerPrefs.SetInt("shallows.bananas", Bananas);
			}
			Pearls = PlayerPrefs.GetInt("shallows.pearls", 0);
			lastDaily = EditorTest ? -1 : PlayerPrefs.GetInt("shallows.lastDaily", -1);
			foreach (ShopCatalogue.Entry item in ShopCatalogue.Items)
			{
				stock[(int)item.kind] = PlayerPrefs.GetInt("shallows.item." + item.id, 0);
			}
			cosmetics.Clear();
			foreach (string id in PlayerPrefs.GetString("shallows.cosmetics", "").Split(','))
			{
				if (id.Length > 0)
				{
					cosmetics.Add(id);
				}
			}
			purchases.Clear();
			foreach (string sku in PlayerPrefs.GetString("shallows.purchases", "").Split(','))
			{
				if (sku.Length > 0)
				{
					purchases.Add(sku);
				}
			}
			equipped.Clear();
			foreach (string slot in Cosmetics.Slots)
			{
				string id = PlayerPrefs.GetString("shallows.wear." + slot, "");
				if (id.Length > 0)
				{
					equipped[slot] = id;
				}
			}
			beltBonus = PlayerPrefs.GetInt("shallows.beltBonus", 0);
			welcomePending = !EditorTest && PlayerPrefs.GetInt(WelcomePendingKey, 0) == 1;
			if (welcomePending)
			{
				welcome = 1;
			}
		}

		private static void Save()
		{
			// Rebuilt from native code (token 0x06000A7F, RVA 0x275825C)
			PlayerPrefs.SetInt("shallows.bananas", Bananas);
			PlayerPrefs.SetInt("shallows.pearls", Pearls);
			PlayerPrefs.SetInt("shallows.lastDaily", lastDaily);
			foreach (ShopCatalogue.Entry item in ShopCatalogue.Items)
			{
				PlayerPrefs.SetInt("shallows.item." + item.id, stock[(int)item.kind]);
			}
			PlayerPrefs.SetString("shallows.cosmetics", string.Join(",", cosmetics));
			PlayerPrefs.SetString("shallows.purchases", string.Join(",", purchases));
			foreach (string slot in Cosmetics.Slots)
			{
				PlayerPrefs.SetString("shallows.wear." + slot, Equipped(slot) ?? "");
			}
			PlayerPrefs.SetInt("shallows.beltBonus", beltBonus);
			PlayerPrefs.Save();
			Changed?.Invoke();
		}
	}
}
