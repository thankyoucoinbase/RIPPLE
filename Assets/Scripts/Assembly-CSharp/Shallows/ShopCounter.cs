using UnityEngine;

namespace Shallows
{
	public class ShopCounter : MonoBehaviour
	{
		[Tooltip("One per item, in the same order as the kinds.")]
		public PokeButton[] buttons;

		public ItemKind[] kinds;

		[Tooltip("Where each sample turns, in the same order.")]
		public Transform[] stands;

		public TextMesh balance;

		public TextMesh status;

		private Transform[] samples;

		private bool waiting;

		private float statusUntil;

		private bool lifeguard;

		private const string Idle = "push a button to buy";

		private void Start()
		{
			// Rebuilt from native code (token 0x06000978, RVA 0x273D238)
			WearLifeguardModel();
			WallPaint.EnsureOn(this);
			RemovePearlTrade(this);
			samples = new Transform[stands.Length];
			for (int i = 0; i < stands.Length && i < kinds.Length; i++)
			{
				Transform sample = Items.Visual(kinds[i]).transform;
				sample.SetParent(stands[i], false);
				sample.localScale = Vector3.one * (kinds[i] == ItemKind.GlowStick ? 1.15f : 1.6f);
				samples[i] = sample;
			}
			for (int i = 0; i < buttons.Length && i < kinds.Length; i++)
			{
				if (!buttons[i])
				{
					continue;
				}
				ItemKind kind = kinds[i];
				buttons[i].repeat = false;
				buttons[i].onPress = () => Buy(kind);
			}
			Wallet.Changed += Show;
			Show();
		}

		public static bool RemovePearlTrade(ShopCounter shop)
		{
			// Rebuilt from native code (token 0x06000979, RVA 0x273DCD0)
			if (shop)
			{
				Transform trade = shop.transform.Find("Pearl Trade");
				if (trade)
				{
					if (Application.isPlaying)
					{
						Destroy(trade.gameObject);
					}
					else
					{
						DestroyImmediate(trade.gameObject);
					}
					return true;
				}
			}
			return false;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x0600097A, RVA 0x273DFCC)
			Wallet.Changed -= Show;
		}

		public Transform WearModel()
		{
			// Rebuilt from native code (token 0x0600097B, RVA 0x273E070)
			return LifeguardProps.Dress(transform, "LifeguardShop", Vector3.zero, Quaternion.identity);
		}

		private void WearLifeguardModel()
		{
			// Rebuilt from native code (token 0x0600097C, RVA 0x273D644)
			Transform model = WearModel();
			if (!model)
			{
				return;
			}
			lifeguard = true;
			Transform parent = model.parent;
			ShopCatalogue.Entry[] items = ShopCatalogue.Items;
			kinds = new ItemKind[items.Length];
			buttons = new PokeButton[items.Length];
			stands = new Transform[items.Length];
			for (int i = 0; i < items.Length; i++)
			{
				kinds[i] = items[i].kind;
				Transform buy = LifeguardProps.Find(model, "Buy " + i.ToString());
				if (buy)
				{
					buttons[i] = LifeguardProps.Button(buy, parent.TransformVector(Vector3.down * 0.012f));
				}
				Transform stand = LifeguardProps.Find(model, "Stand " + i.ToString());
				Transform sample = new GameObject("Sample " + items[i].name).transform;
				sample.SetParent(parent, false);
				if (stand)
				{
					sample.position = stand.position;
				}
				stands[i] = sample;
			}
			balance = LifeguardProps.Text(transform, model, "Balance Text", "Balance", PixelFonts.Sign, 0.045f, Color.white, TextAnchor.MiddleCenter, 1f);
			status = LifeguardProps.Text(transform, model, "Status Text", "Status", PixelFonts.Sign, 0.03f, new Color(0.55f, 0.08f, 0.06f, 1f), TextAnchor.MiddleCenter, 1.1f);
			status.text = Idle;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600097D, RVA 0x273E210)
			float spin = Time.time * 40f;
			for (int i = 0; i < samples.Length; i++)
			{
				if (samples[i])
				{
					samples[i].localRotation = Quaternion.Euler(0f, spin + i * 50f, 0f);
				}
			}
			if (status && statusUntil > 0f && Time.time > statusUntil)
			{
				statusUntil = 0f;
				status.text = Idle;
			}
		}

		private void Buy(ItemKind kind)
		{
			// Rebuilt from native code (token 0x0600097E, RVA 0x273E3AC)
			if (waiting)
			{
				return;
			}
			waiting = true;
			Say("BUYING...", 0f);
			Wallet.Buy(kind, (ok, message) =>
			{
				// Rebuilt from native code (token 0x06000982, RVA 0x273E548)
				waiting = false;
				Say(message, 4f);
				if (SplashSystem.I)
				{
					SplashSystem.I.PlaySound(ok ? Sfx.ShopBuy() : Sfx.ShopDenied(), transform.position, 0.6f, ok ? 1.2f : 0.8f);
				}
			});
		}

		private void Say(string text, float seconds)
		{
			// Rebuilt from native code (token 0x0600097F, RVA 0x273E490)
			if (!status)
			{
				return;
			}
			status.text = text;
			statusUntil = seconds > 0f ? Time.time + seconds : 0f;
		}

		private void Show()
		{
			// Rebuilt from native code (token 0x06000980, RVA 0x273DE40)
			if (!balance)
			{
				return;
			}
			balance.text = lifeguard ? Wallet.Bananas.ToString() : string.Format("YOU HAVE {0} BANANAS", Wallet.Bananas);
		}
	}
}
