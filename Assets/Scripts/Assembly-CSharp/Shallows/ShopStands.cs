using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class ShopStands : MonoBehaviour
	{
		private class Stand
		{
			public string cosmetic;

			public string sku;

			public PokeButton button;

			public TextMesh face;

			public Renderer plate;

			public bool painted;

			public bool gold;

			public float textSize;

			public float sayUntil;

			public float confirmUntil;

			public bool waiting;
		}

		private const string Prefix = "Anchor Buy ";

		private const float KeyDepth = 0.018f;

		private const float ConfirmTime = 3f;

		private static readonly Color Pressed = new Color(1f, 0.85f, 0.45f, 1f);

		private static readonly Color Bar = C(98, 188, 108);

		private static readonly Color BarBorder = C(46, 112, 58);

		private static readonly Color BarSide = C(56, 116, 64);

		private static readonly Color BarInk = C(14, 40, 22);

		private static readonly Color Gold = C(208, 174, 76);

		private static readonly Color GoldBorder = C(128, 92, 30);

		private static readonly Color GoldSide = C(132, 104, 40);

		private static readonly Color GoldInk = C(22, 28, 50);

		private static readonly Color Sure = C(250, 214, 70);

		private static readonly Color Wear = C(150, 206, 236);

		private static readonly Color Off = C(196, 204, 200);

		private static readonly Color Done = C(150, 158, 154);

		private static readonly Color OffInk = C(28, 34, 32);

		private static readonly Color DoneInk = C(34, 38, 36);

		private static readonly Color GreyEdge = C(14, 16, 18);

		private static readonly Color LabelColor = new Color(0.95f, 0.98f, 0.92f, 1f);

		private static readonly Dictionary<string, string> Cosmetic = new Dictionary<string, string>
		{
			["sign drip 0"] = "lifeguard_set",
			["sign drip 1"] = "snorkel_set",
			["sign drip 2"] = "luau_set",
			["sign drip 3"] = "retro_swim_set",
			["sign shack 0"] = "ducky_set",
			["sign shack 1"] = "party_set",
			["sign shack 2"] = "captain_set",
			["sign shack 3"] = "coconut_armor_set",
			["sign acc 0"] = "pineapple_hat",
			["sign acc 1"] = "ice_cream_cone",
			["sign acc 2"] = "sandcastle_hat",
			["sign acc 3"] = "coral_crown",
			["sign acc 4"] = "crab_buddy",
			["sign acc 5"] = "jellyfish_hat",
			["sign ban 0"] = "pirate_eyepatch",
			["sign ban 1"] = "wet_floor_sign",
			["sign ban 2"] = "star_sunglasses",
			["sign ban 3"] = "pool_noodle_crown"
		};

		private static readonly Dictionary<string, string> Product = new Dictionary<string, string>
		{
			["sign clam 0"] = "pearls_1000",
			["sign clam 1"] = "pearls_2200",
			["sign clam 2"] = "pearls_5000",
			["sign clam 3"] = "pearls_11000",
			["sign starter"] = "starter_pack",
			["sign outfit"] = "deep_diver_bundle",
			["sign deep"] = "monsters_of_the_deep",
			["sign deep 0"] = "kraken_set",
			["sign deep 1"] = "anglerfish_set",
			["sign deep 2"] = "megalodon_set"
		};

		private readonly List<Stand> stands = new List<Stand>();

		private static Material rimMaterial;

		private static Material barSide;

		private static Material goldSide;

		private static Material pixelPearl;

		private static Mesh pearlMesh;

		private const int PearlR = 11;

		private static Color C(int r, int g, int b)
		{
			// Rebuilt from native code (token 0x0600099F, RVA 0x2740114)
			return new Color(r / 255f, g / 255f, b / 255f, 1f);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060009A0, RVA 0x274013C)
			pixelPearl = null;
			goldSide = null;
			barSide = null;
			rimMaterial = null;
			pearlMesh = null;
		}

		public static void Build(RippleShop shop, Transform model)
		{
			// Rebuilt from native code (token 0x060009A1, RVA 0x2738994)
			if (!Application.isPlaying || !model || shop.GetComponent<ShopStands>())
			{
				return;
			}
			ShopStands added = shop.gameObject.AddComponent<ShopStands>();
			foreach (Transform mark in model.GetComponentsInChildren<Transform>(true))
			{
				if (mark.name.StartsWith(Prefix))
				{
					added.Add(shop.transform, mark);
				}
			}
			if (added.stands.Count == 0)
			{
				Debug.LogWarning("Shallows: the shop model has no BUY spots yet (re-export RippleShop.fbx from shop.py).");
			}
		}

		private void Add(Transform shop, Transform mark)
		{
			// Rebuilt from native code (token 0x060009A2, RVA 0x27401FC)
			List<string> words = new List<string>(mark.name.Substring(Prefix.Length).Split(' '));
			float width = 0.16f;
			float height = 0.06f;
			float tilt = 0f;
			float yaw = 0f;
			Rect uv = default(Rect);
			bool painted = false;
			while (words.Count > 1)
			{
				string tag = words[words.Count - 1];
				if (tag.Length >= 2 && tag[0] == 'u')
				{
					string[] corners = tag.Substring(1).Split('_');
					if (corners.Length == 4 && float.TryParse(corners[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float xMin) && float.TryParse(corners[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float yMin) && float.TryParse(corners[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float xMax) && float.TryParse(corners[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float yMax))
					{
						uv = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
						painted = true;
						words.RemoveAt(words.Count - 1);
						continue;
					}
				}
				if (tag.Length < 2 || "rtwh".IndexOf(tag[0]) < 0 || !float.TryParse(tag.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
				{
					break;
				}
				if (tag[0] == 'r')
				{
					yaw = value;
				}
				else if (tag[0] == 't')
				{
					tilt = value;
				}
				else if (tag[0] == 'w')
				{
					width = value;
				}
				else
				{
					height = value;
				}
				words.RemoveAt(words.Count - 1);
			}
			string key = string.Join(" ", words);
			Stand stand = new Stand();
			if (Cosmetic.TryGetValue(key, out string cosmetic))
			{
				stand.cosmetic = cosmetic;
			}
			else if (Product.TryGetValue(key, out string sku))
			{
				stand.sku = sku;
			}
			else
			{
				return;
			}
			stand.gold = key.StartsWith("sign deep");
			stand.painted = painted;
			stand.textSize = height * 0.82f;
			Transform root = new GameObject("Buy " + (stand.cosmetic ?? stand.sku)).transform;
			root.SetParent(transform, false);
			root.SetPositionAndRotation(mark.position, shop.rotation * Quaternion.Euler(0f, yaw + RippleShop.MovedTurn(shop, mark), 0f) * Quaternion.Euler(tilt, 0f, 0f));
			GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(rim.GetComponent<Collider>());
			rim.name = "Button Rim";
			rim.transform.SetParent(root, false);
			rim.transform.localPosition = new Vector3(0f, 0f, -0.0054f);
			rim.transform.localScale = new Vector3(width + 0.006f, height + 0.006f, 0.0108f);
			Renderer rimRenderer = rim.GetComponent<Renderer>();
			rimRenderer.sharedMaterial = RimMaterial();
			MaterialPropertyBlock rimBlock = new MaterialPropertyBlock();
			rimBlock.SetColor("_BaseColor", stand.gold ? GoldBorder : BarBorder);
			rimRenderer.SetPropertyBlock(rimBlock);
			GameObject keyObject = new GameObject("Button", typeof(MeshFilter), typeof(MeshRenderer));
			keyObject.transform.SetParent(root, false);
			keyObject.transform.localPosition = new Vector3(0f, 0f, -0.0085f);
			keyObject.GetComponent<MeshFilter>().sharedMesh = KeyMesh(new Vector3(width, height, KeyDepth), painted ? uv : default(Rect));
			MeshRenderer keyRenderer = keyObject.GetComponent<MeshRenderer>();
			Material shopMaterial = LifeguardProps.MaterialFor("RippleShop", null);
			keyRenderer.sharedMaterials = new Material[]
			{
				painted ? shopMaterial : SideMaterial(stand.gold),
				SideMaterial(stand.gold)
			};
			keyRenderer.shadowCastingMode = ShadowCastingMode.Off;
			stand.button = LifeguardProps.Button(keyObject.transform, root.forward * 0.0099f);
			stand.button.repeat = false;
			stand.button.color = Color.white;
			stand.button.pressedColor = Pressed;
			stand.button.onPress = () => Press(stand);
			GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Destroy(plate.GetComponent<Collider>());
			plate.name = "Plate";
			plate.transform.SetParent(keyObject.transform, false);
			plate.transform.localPosition = new Vector3(0f, 0f, -0.0098f);
			plate.transform.localScale = new Vector3(width, height, 0.0012f);
			stand.plate = plate.GetComponent<Renderer>();
			stand.plate.sharedMaterial = RimMaterial();
			stand.plate.shadowCastingMode = ShadowCastingMode.Off;
			stand.face = WorldText.Create("Text", keyObject.transform, new Vector3(0f, 0f, -0.0112f), "", stand.textSize, BarInk, TextAnchor.MiddleCenter, TextAlignment.Center);
			PixelFonts.Use(stand.face, PixelFonts.Screen, stand.textSize, 1f);
			stand.face.transform.localRotation = Quaternion.identity;
			stands.Add(stand);
		}

		private static TextMesh[] Outline(TextMesh text, float edge, Color color)
		{
			// Rebuilt from native code (token 0x060009A3, RVA 0x2741DF4)
			TextMesh[] edges = new TextMesh[8];
			for (int i = 0; i < 8; i++)
			{
				GameObject outline = new GameObject("Outline " + i);
				outline.transform.SetParent(text.transform, false);
				float angle = i * MathF.PI * 0.25f;
				outline.transform.localPosition = edge / Mathf.Max(text.transform.lossyScale.x, 1E-05f) * new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f) + new Vector3(0f, 0f, 0.0004f);
				TextMesh copy = outline.AddComponent<TextMesh>();
				copy.font = text.font;
				copy.fontSize = text.fontSize;
				copy.characterSize = text.characterSize;
				copy.lineSpacing = text.lineSpacing;
				copy.anchor = text.anchor;
				copy.alignment = text.alignment;
				copy.color = color;
				copy.text = text.text;
				outline.GetComponent<MeshRenderer>().sharedMaterial = text.GetComponent<MeshRenderer>().sharedMaterial;
				edges[i] = copy;
			}
			return edges;
		}

		private static void SetText(TextMesh text, TextMesh[] edges, string value)
		{
			// Rebuilt from native code (token 0x060009A4, RVA 0x2742190)
			if (text.text == value)
			{
				return;
			}
			text.text = value;
			if (edges == null)
			{
				return;
			}
			foreach (TextMesh edge in edges)
			{
				edge.text = value;
			}
		}

		private static Mesh KeyMesh(Vector3 size, Rect uv)
		{
			// Rebuilt from native code (token 0x060009A5, RVA 0x27416A8)
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Mesh source = cube.GetComponent<MeshFilter>().sharedMesh;
			Vector3[] vertices = source.vertices;
			Vector3[] normals = source.normals;
			int[] triangles = source.triangles;
			Destroy(cube);
			Vector2[] uvs = new Vector2[vertices.Length];
			for (int i = 0; i < vertices.Length; i++)
			{
				uvs[i] = Rect.NormalizedToPoint(uv, new Vector2(vertices[i].x + 0.5f, vertices[i].y + 0.5f));
				vertices[i] = Vector3.Scale(vertices[i], size);
			}
			List<int> back = new List<int>();
			List<int> front = new List<int>();
			for (int i = 0; i < triangles.Length; i += 3)
			{
				(normals[triangles[i]].z < -0.5f ? back : front).AddRange(new int[] { triangles[i], triangles[i + 1], triangles[i + 2] });
			}
			Mesh mesh = new Mesh();
			mesh.name = "Shop Price Key";
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.uv = uvs;
			mesh.subMeshCount = 2;
			mesh.SetTriangles(back, 0);
			mesh.SetTriangles(front, 1);
			mesh.RecalculateBounds();
			return mesh;
		}

		internal static Transform PixelPearl(Transform parent, Vector3 localPosition, float diameter)
		{
			// Rebuilt from native code (token 0x060009A6, RVA 0x2742230)
			GameObject pearl = new GameObject("Pearl", typeof(MeshFilter), typeof(MeshRenderer));
			pearl.transform.SetParent(parent, false);
			pearl.transform.localPosition = localPosition;
			pearl.transform.localScale = Vector3.one * diameter;
			pearl.GetComponent<MeshFilter>().sharedMesh = PearlMesh();
			MeshRenderer renderer = pearl.GetComponent<MeshRenderer>();
			renderer.sharedMaterial = PixelPearlMaterial();
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			return pearl.transform;
		}

		private static Mesh PearlMesh()
		{
			// Rebuilt from native code (token 0x060009A7, RVA 0x2742544)
			if (!pearlMesh)
			{
				const int Size = PearlR * 2 + 1;
				List<Vector3> vertices = new List<Vector3>();
				List<Vector2> uvs = new List<Vector2>();
				List<int> triangles = new List<int>();
				for (int y = 0; y < Size; y++)
				{
					float top = 0.5f - y / (float)Size;
					float bottom = 0.5f - (y + 1) / (float)Size;
					float v = 1f - (y + 0.5f) / Size;
					for (int x = 0; x < Size; x++)
					{
						if (Mathf.Sqrt((x - PearlR) * (x - PearlR) + (y - PearlR) * (y - PearlR)) > PearlR)
						{
							continue;
						}
						float left = x / (float)Size - 0.5f;
						float right = (x + 1) / (float)Size - 0.5f;
						int first = vertices.Count;
						vertices.Add(new Vector3(left, bottom, 0f));
						vertices.Add(new Vector3(left, top, 0f));
						vertices.Add(new Vector3(right, top, 0f));
						vertices.Add(new Vector3(right, bottom, 0f));
						float u = (x + 0.5f) / Size;
						for (int corner = 0; corner < 4; corner++)
						{
							uvs.Add(new Vector2(u, v));
						}
						triangles.AddRange(new int[] { first, first + 1, first + 2, first, first + 2, first + 3 });
					}
				}
				pearlMesh = new Mesh
				{
					name = "Pixel Pearl"
				};
				pearlMesh.SetVertices(vertices);
				pearlMesh.SetUVs(0, uvs);
				pearlMesh.SetTriangles(triangles, 0);
				pearlMesh.RecalculateNormals();
				pearlMesh.RecalculateBounds();
			}
			return pearlMesh;
		}

		private static Material PixelPearlMaterial()
		{
			// Rebuilt from native code (token 0x060009A8, RVA 0x2742B74)
			if (!pixelPearl)
			{
				const int Size = PearlR * 2 + 1;
				Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
				texture.name = "Pixel Pearl";
				texture.filterMode = FilterMode.Point;
				texture.wrapMode = TextureWrapMode.Clamp;
				Color face = C(236, 230, 244);
				Color rim = C(178, 166, 196);
				for (int y = 0; y < Size; y++)
				{
					for (int x = 0; x < Size; x++)
					{
						int dx = x - PearlR;
						int dy = y - PearlR;
						float glint = Mathf.Sqrt((y - 7.15f) * (y - 7.15f) + (x - 7.15f) * (x - 7.15f));
						Color color = glint <= PearlR * 0.3f ? Color.white : (Mathf.Sqrt(dy * dy + dx * dx) <= PearlR - 1 ? face : rim);
						texture.SetPixel(x, Size - 1 - y, color);
					}
				}
				texture.Apply();
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
				pixelPearl = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = "Pixel Pearl"
				};
				if (pixelPearl.HasProperty("_BaseMap"))
				{
					pixelPearl.SetTexture("_BaseMap", texture);
				}
				pixelPearl.mainTexture = texture;
			}
			return pixelPearl;
		}

		private static Material SideMaterial(bool gold)
		{
			// Rebuilt from native code (token 0x060009A9, RVA 0x2741B04)
			ref Material slot = ref gold ? ref goldSide : ref barSide;
			if (!slot)
			{
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
				slot = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = gold ? "Shop Key Side Gold" : "Shop Key Side"
				};
				Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
				texture.name = slot.name;
				texture.filterMode = FilterMode.Point;
				texture.SetPixel(0, 0, gold ? GoldSide : BarSide);
				texture.Apply();
				if (slot.HasProperty("_BaseMap"))
				{
					slot.SetTexture("_BaseMap", texture);
				}
				slot.mainTexture = texture;
			}
			return slot;
		}

		private static Material RimMaterial()
		{
			// Rebuilt from native code (token 0x060009AA, RVA 0x274150C)
			if (!rimMaterial)
			{
				Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
				rimMaterial = new Material(shader ? shader : Shader.Find("Universal Render Pipeline/Lit"))
				{
					name = "Shop Price Key Rim"
				};
			}
			return rimMaterial;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060009AB, RVA 0x2742F30)
			Wallet.Changed += Show;
			MetaIAP.PricesChanged += Show;
			MetaIAP.FetchPrices();
			Show();
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060009AC, RVA 0x2743188)
			Wallet.Changed -= Show;
			MetaIAP.PricesChanged -= Show;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060009AD, RVA 0x2743280)
			bool changed = false;
			foreach (Stand stand in stands)
			{
				if (stand.sayUntil > 0f && Time.time >= stand.sayUntil)
				{
					changed = true;
				}
				if (stand.confirmUntil > 0f && Time.time >= stand.confirmUntil)
				{
					stand.confirmUntil = 0f;
					changed = true;
				}
			}
			if (changed)
			{
				Show();
			}
		}

		private void Press(Stand stand)
		{
			// Rebuilt from native code (token 0x060009AE, RVA 0x2743424)
			if (stand.waiting)
			{
				return;
			}
			if (stand.cosmetic == null)
			{
				if (MetaIAP.Owned(stand.sku))
				{
					Denied(stand);
					return;
				}
				stand.waiting = true;
				// TODO(recon): Say's message and seconds are dead arguments in the shipped build (Say ignores them), so the literals passed in Press and BuyNow are inferred, not read (token 0x060009AE).
				Say(stand, "OPENING STORE...");
				MetaIAP.Buy(stand.sku, (ok, message) =>
				{
					stand.waiting = false;
					Show();
					if (ok)
					{
						Bought(stand.button.transform.position, stand.sku);
					}
					else if (!string.IsNullOrEmpty(message))
					{
						Denied(stand);
					}
				});
				return;
			}
			Cosmetics.TryGet(stand.cosmetic, out Cosmetics.Entry entry);
			if (entry.model == null && !Wallet.HasCosmetic(stand.cosmetic))
			{
				Denied(stand);
				return;
			}
			if (Wallet.HasCosmetic(stand.cosmetic))
			{
				if (Wallet.Wearing(stand.cosmetic))
				{
					Wallet.TakeOff(stand.cosmetic);
					Say(stand, "TOOK IT OFF");
				}
				else
				{
					Wallet.Equip(stand.cosmetic);
					Say(stand, "WEARING IT!");
				}
				return;
			}
			bool pearls = entry.pearls > 0;
			int need = (pearls ? entry.pearls : entry.bananas) - (pearls ? Wallet.Pearls : Wallet.Bananas);
			if (need > 0)
			{
				stand.confirmUntil = 0f;
				if (!pearls)
				{
					Denied(stand);
					return;
				}
				stand.waiting = true;
				string pack = MetaIAP.PearlPackFor(need);
				MetaIAP.Buy(pack, (ok, message) =>
				{
					if (!ok)
					{
						stand.waiting = false;
						if (!string.IsNullOrEmpty(message))
						{
							Denied(stand);
						}
						Show();
						return;
					}
					StartCoroutine(After(PearlVendingMachine.Shower(stand.button.transform.position, MetaIAP.PearlsIn(pack)), () => BuyNow(stand)));
				});
				return;
			}
			if (Time.time < stand.confirmUntil)
			{
				stand.confirmUntil = 0f;
				BuyNow(stand);
				return;
			}
			foreach (Stand other in stands)
			{
				other.confirmUntil = 0f;
			}
			stand.confirmUntil = Time.time + ConfirmTime;
			Say(stand, "TAP AGAIN\nTO BUY", ConfirmTime);
		}

		public static void Bought(Vector3 at, string sku)
		{
			// Rebuilt from native code (token 0x060009AF, RVA 0x2743DB8)
			if (PearlVendingMachine.Shower(at, MetaIAP.PearlsIn(sku)) > 0f)
			{
				return;
			}
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ShopBuy(), at, 0.8f, 1f);
			}
		}

		private static IEnumerator After(float seconds, Action then)
		{
			// Rebuilt from native code (token 0x060009B0, RVA 0x2743FC8; MoveNext RVA 0x2745794)
			if (seconds > 0f)
			{
				yield return new WaitForSeconds(seconds);
			}
			then();
		}

		private void BuyNow(Stand stand)
		{
			// Rebuilt from native code (token 0x060009B1, RVA 0x2743C8C)
			stand.waiting = true;
			Say(stand, "BUYING...");
			Wallet.BuyCosmetic(stand.cosmetic, (ok, message) =>
			{
				stand.waiting = false;
				Show();
				if (!ok)
				{
					Denied(stand);
					return;
				}
				Burst(stand);
			});
		}

		private static void Burst(Stand stand)
		{
			// Rebuilt from native code (token 0x060009B2, RVA 0x274404C)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ShopBuy(), stand.button.transform.position, 0.8f, 1f);
			}
		}

		private static void Denied(Stand stand)
		{
			// Rebuilt from native code (token 0x060009B3, RVA 0x2743A90)
			if (SplashSystem.I)
			{
				SplashSystem.I.PlaySound(Sfx.ShopDenied(), stand.button.transform.position, 0.6f, 0.8f);
			}
		}

		private void Say(Stand stand, string message, float seconds = 3.5f)
		{
			// Rebuilt from native code (token 0x060009B4, RVA 0x2743C2C)
			stand.sayUntil = 0f;
			ShowButton(stand);
		}

		private void Show()
		{
			// Rebuilt from native code (token 0x060009B5, RVA 0x2743038)
			foreach (Stand stand in stands)
			{
				ShowButton(stand);
			}
		}

		private static void ShowButton(Stand s)
		{
			// Rebuilt from native code (token 0x060009B6, RVA 0x27441E4)
			Color fill = s.gold ? Gold : Bar;
			Color ink = s.gold ? GoldInk : BarInk;
			string label;
			if (s.cosmetic != null)
			{
				Cosmetics.TryGet(s.cosmetic, out Cosmetics.Entry entry);
				if (Wallet.HasCosmetic(s.cosmetic))
				{
					if (Wallet.Wearing(s.cosmetic))
					{
						label = "TAKE OFF";
						fill = Off;
						ink = OffInk;
					}
					else
					{
						label = "WEAR";
						fill = Wear;
					}
				}
				else if (entry.model == null)
				{
					label = "SOON";
					fill = Done;
					ink = DoneInk;
				}
				else if (Time.time < s.confirmUntil)
				{
					label = "SURE?";
					fill = Sure;
					ink = GoldInk;
				}
				else
				{
					label = s.painted ? null : (entry.pearls > 0 ? entry.pearls : entry.bananas).ToString("N0");
				}
			}
			else if (MetaIAP.Owned(s.sku))
			{
				label = "OWNED";
				fill = Done;
				ink = DoneInk;
			}
			else
			{
				label = s.painted ? null : MetaIAP.Price(s.sku);
			}
			s.plate.gameObject.SetActive(label != null);
			s.face.gameObject.SetActive(label != null);
			if (label == null)
			{
				return;
			}
			SetText(s.face, null, label);
			s.face.color = ink;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			block.SetColor("_BaseColor", fill);
			s.plate.SetPropertyBlock(block);
		}
	}
}
