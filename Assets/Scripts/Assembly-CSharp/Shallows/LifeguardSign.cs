using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[ExecuteAlways]
	public class LifeguardSign : MonoBehaviour
	{
		internal readonly struct Strip
		{
			public readonly float top;

			public readonly float height;

			public Strip(float top, float height)
			{
				// Rebuilt from native code (token 0x06000434, RVA 0x27AADAC)
				this.top = top;
				this.height = height;
			}
		}

		internal class Builder
		{
			internal readonly List<Vector3> v = new List<Vector3>();

			internal readonly List<Vector3> n = new List<Vector3>();

			internal readonly List<Vector2> uv = new List<Vector2>();

			internal readonly List<int> tris = new List<int>();

			public void Box(Vector3 a, Vector3 b, Strip strip, bool vertical)
			{
				// Rebuilt from native code (token 0x06000435, RVA 0x27AAF74)
				Face(a, new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), Vector3.back, b.x - a.x, b.y - a.y, strip, vertical);
				Face(new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), b, Vector3.forward, b.x - a.x, b.y - a.y, strip, vertical);
				Face(new Vector3(a.x, a.y, b.z), a, new Vector3(a.x, b.y, a.z), new Vector3(a.x, b.y, b.z), Vector3.left, b.z - a.z, b.y - a.y, strip, vertical);
				Face(new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), b, new Vector3(b.x, b.y, a.z), Vector3.right, b.z - a.z, b.y - a.y, strip, vertical);
				Face(new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, a.y, a.z), a, Vector3.down, b.x - a.x, b.z - a.z, strip, false);
				Face(new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), b, new Vector3(a.x, b.y, b.z), Vector3.up, b.x - a.x, b.z - a.z, strip, false);
			}

			private void Face(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, float across, float up, Strip strip, bool vertical)
			{
				// Rebuilt from native code (token 0x06000436, RVA 0x27AB384)
				int start = v.Count;
				v.Add(p0);
				v.Add(p1);
				v.Add(p2);
				v.Add(p3);
				for (int i = 0; i < 4; i++)
				{
					n.Add(normal);
				}
				float u = Mathf.Min(Atlas - 1f, (vertical ? up : across) * PxPerMetre) / Atlas;
				float bottom = strip.top + Mathf.Clamp((vertical ? across : up) * PxPerMetre, 1f, strip.height);
				float v0 = 1f - (bottom - 0.5f) / Atlas;
				float v1 = 1f - (strip.top + 0.5f) / Atlas;
				if (!vertical)
				{
					uv.Add(new Vector2(0f, v0));
					uv.Add(new Vector2(u, v0));
					uv.Add(new Vector2(u, v1));
					uv.Add(new Vector2(0f, v1));
				}
				else
				{
					uv.Add(new Vector2(0f, v1));
					uv.Add(new Vector2(0f, v0));
					uv.Add(new Vector2(u, v0));
					uv.Add(new Vector2(u, v1));
				}
				tris.Add(start);
				tris.Add(start + 2);
				tris.Add(start + 1);
				tris.Add(start);
				tris.Add(start + 3);
				tris.Add(start + 2);
			}

			public Mesh ToMesh()
			{
				// Rebuilt from native code (token 0x06000437, RVA 0x27AACA4)
				Mesh mesh = new Mesh();
				mesh.name = BoardName;
				mesh.SetVertices(v);
				mesh.SetNormals(n);
				mesh.SetUVs(0, uv);
				mesh.SetTriangles(tris, 0);
				mesh.RecalculateBounds();
				return mesh;
			}
		}

		private const string BoardName = "Sign Board";

		private const float Atlas = 256f;

		private const float PxPerMetre = 100f;

		private const float PlankHeight = 0.16f;

		private const float PlankLength = 2.56f;

		private const float Depth = 0.03f;

		private const float TrimWidth = 0.06f;

		private static readonly Color Letters = new Color(0.96f, 0.94f, 0.88f, 1f);

		[SerializeField]
		[HideInInspector]
		private string fitted;

		[SerializeField]
		[HideInInspector]
		private string measured;

		private static readonly Strip Metal = new Strip(208f, 16f);

		public static bool Dress(TextMesh text)
		{
			// Rebuilt from native code (token 0x06000426, RVA 0x27A8DBC)
			if (!text || string.IsNullOrEmpty(text.text))
			{
				return false;
			}
			Font font = PixelFonts.Sign;
			bool changed = false;
			if (text.font != font)
			{
				PixelFonts.Use(text, font, text.characterSize * (text.fontSize > 0 ? text.fontSize : 64) * 0.1f * 0.75f, 1.35f);
				text.color = Letters;
				changed = true;
			}
			Transform board = text.transform.Find(BoardName);
			LifeguardSign sign = board ? board.GetComponent<LifeguardSign>() : null;
			string key = string.Format("{0}|{1}|{2}|{3}|{4}", text.text, text.characterSize, text.fontSize, text.lineSpacing, text.anchor);
			MeshFilter filter = board ? board.GetComponent<MeshFilter>() : null;
			if (sign && sign.fitted == key && filter && filter.sharedMesh)
			{
				return changed | sign.Solidify(filter.sharedMesh);
			}
			if (!board)
			{
				board = new GameObject(BoardName, typeof(MeshFilter), typeof(MeshRenderer), typeof(LifeguardSign)).transform;
				board.SetParent(text.transform, false);
				sign = board.GetComponent<LifeguardSign>();
				filter = board.GetComponent<MeshFilter>();
			}
			board.localPosition = Vector3.zero;
			board.localRotation = Quaternion.identity;
			board.localScale = Vector3.one;
			board.GetComponent<MeshRenderer>().sharedMaterial = LifeguardProps.MaterialFor("LifeguardSign", null);
			sign.fitted = key;
			bool measuredOk = Measure(text, out Rect box);
			sign.Fit(text, measuredOk ? box : Estimate(text));
			sign.measured = measuredOk ? key : "";
			return true;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000427, RVA 0x27AA070)
			if (measured == fitted)
			{
				return;
			}
			TextMesh text = transform.parent ? transform.parent.GetComponent<TextMesh>() : null;
			if (text && Measure(text, out Rect box))
			{
				measured = fitted;
				Fit(text, box);
			}
		}

		private void Fit(TextMesh text, Rect box)
		{
			// Rebuilt from native code (token 0x06000428, RVA 0x27A9DAC)
			MeshFilter filter = GetComponent<MeshFilter>();
			MeshCollider meshCollider = GetComponent<MeshCollider>();
			if (meshCollider)
			{
				meshCollider.enabled = false;
			}
			if (filter.sharedMesh && !IsAsset(filter.sharedMesh))
			{
				Object.DestroyImmediate(filter.sharedMesh);
			}
			float em = text.characterSize * text.fontSize * 0.1f;
			Builder b = Layout(text.text.GetHashCode() & int.MaxValue, em, box, out Rect outer, out float back);
			Mount(text.transform, b, outer.xMin, outer.xMax, outer.yMin, outer.yMax, back);
			filter.sharedMesh = b.ToMesh();
			Solidify(filter.sharedMesh);
		}

		private bool Solidify(Mesh mesh)
		{
			// Rebuilt from native code (token 0x06000429, RVA 0x27A96FC)
			if (gameObject.layer == 2)
			{
				gameObject.layer = 0;
			}
			MeshCollider meshCollider = GetComponent<MeshCollider>();
			if (meshCollider && meshCollider.enabled && meshCollider.sharedMesh == mesh)
			{
				return false;
			}
			if (!meshCollider)
			{
				meshCollider = gameObject.AddComponent<MeshCollider>();
			}
			meshCollider.sharedMesh = mesh;
			meshCollider.enabled = true;
			return true;
		}

		private static bool Measure(TextMesh text, out Rect box)
		{
			// Rebuilt from native code (token 0x0600042A, RVA 0x27A98A0)
			box = default;
			MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
			if (!meshRenderer)
			{
				return false;
			}
			Bounds bounds = meshRenderer.bounds;
			if (bounds.size.sqrMagnitude < 1E-08f)
			{
				return false;
			}
			Vector3 center = bounds.center;
			Vector3 extents = bounds.extents;
			Vector2 min = Vector3.positiveInfinity;
			Vector2 max = Vector3.negativeInfinity;
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = new Vector3(center.x + ((i & 1) == 0 ? -extents.x : extents.x), center.y + ((i & 2) == 0 ? -extents.y : extents.y), center.z + ((i & 4) == 0 ? -extents.z : extents.z));
				Vector2 local = text.transform.InverseTransformPoint(corner);
				min = Vector2.Min(min, local);
				max = Vector2.Max(max, local);
			}
			box = new Rect(min, max - min);
			return box.width > 0.001f && box.height > 0.001f;
		}

		private static Rect Estimate(TextMesh text)
		{
			// Rebuilt from native code (token 0x0600042B, RVA 0x27A9B08)
			string[] lines = text.text.Replace("\r", "").Split('\n');
			int longest = 0;
			foreach (string line in lines)
			{
				longest = Mathf.Max(longest, line.Length);
			}
			float em = text.characterSize * text.fontSize * 0.1f;
			float aspect = (text.font && text.font.name.Contains("PressStart")) ? 1f : 0.6f;
			float width = em * longest * aspect;
			float height = em + text.lineSpacing * (em * (lines.Length - 1));
			float x = 0f;
			switch (text.anchor)
			{
			case TextAnchor.UpperLeft:
			case TextAnchor.MiddleLeft:
			case TextAnchor.LowerLeft:
				x = width * 0.5f;
				break;
			case TextAnchor.UpperRight:
			case TextAnchor.MiddleRight:
			case TextAnchor.LowerRight:
				x = width * -0.5f;
				break;
			}
			float y = 0f;
			switch (text.anchor)
			{
			case TextAnchor.UpperLeft:
			case TextAnchor.UpperCenter:
			case TextAnchor.UpperRight:
				y = height * -0.5f;
				break;
			case TextAnchor.LowerLeft:
			case TextAnchor.LowerCenter:
			case TextAnchor.LowerRight:
				y = height * 0.5f;
				break;
			}
			return new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
		}

		private static bool IsAsset(Object o)
		{
			// Rebuilt from native code (token 0x0600042C, RVA 0x27AA1E8)
			return false;
		}

		private static Strip Plank(int k)
		{
			// Rebuilt from native code (token 0x0600042D, RVA 0x27AAD7C)
			return new Strip(k % 10 * 16f, 16f);
		}

		private static Strip Trim(int k)
		{
			// Rebuilt from native code (token 0x0600042E, RVA 0x27AADB4)
			return new Strip(160f + k % 6 * 8f, 8f);
		}

		private static Strip Post(int k)
		{
			// Rebuilt from native code (token 0x0600042F, RVA 0x27AADF4)
			return new Strip(224f + k % 2 * 16f, 16f);
		}

		internal static Builder Layout(int seed, float em, Rect letters, out Rect outer, out float back)
		{
			// Rebuilt from native code (token 0x06000430, RVA 0x27AA1F0)
			Vector2 center = letters.center;
			Vector2 half = letters.size * 0.5f;
			float pad = em * 0.5f + 0.05f;
			float x0 = center.x - half.x - pad;
			float x1 = center.x + half.x + pad;
			float y0 = center.y - half.y - pad;
			float y1 = center.y + half.y + pad;
			back = 0.008f + Depth;
			Builder b = new Builder();
			float width = x1 - x0;
			int planks = Mathf.CeilToInt(width / PlankLength - 0.001f);
			int k = seed;
			for (float top = y1; top > y0 + 0.001f; top -= PlankHeight)
			{
				float bottom = Mathf.Max(y0, top - PlankHeight);
				for (int i = 0; i < planks; i++)
				{
					b.Box(new Vector3(x0 + width / planks * i, bottom, 0.008f), new Vector3(x0 + width / planks * (i + 1), top, back), Plank(k + i), false);
				}
				k += 3;
			}
			b.Box(new Vector3(x0 - TrimWidth, y1, -0.006f), new Vector3(x1 + TrimWidth, y1 + TrimWidth, back), Trim(seed), false);
			b.Box(new Vector3(x0 - TrimWidth, y0 - TrimWidth, -0.006f), new Vector3(x1 + TrimWidth, y0, back), Trim(seed + 1), false);
			b.Box(new Vector3(x0 - TrimWidth, y0, -0.006f), new Vector3(x0, y1, back), Trim(seed + 2), true);
			b.Box(new Vector3(x1, y0, -0.006f), new Vector3(x1 + TrimWidth, y1, back), Trim(seed + 3), true);
			Vector2[] bolts = new Vector2[4]
			{
				new Vector2(x0 - 0.03f, y0 - 0.03f),
				new Vector2(x1 + 0.03f, y0 - 0.03f),
				new Vector2(x0 - 0.03f, y1 + 0.03f),
				new Vector2(x1 + 0.03f, y1 + 0.03f)
			};
			foreach (Vector2 bolt in bolts)
			{
				b.Box(new Vector3(bolt.x - 0.014f, bolt.y - 0.014f, -0.014f), new Vector3(bolt.x + 0.014f, bolt.y + 0.014f, -0.006f), Metal, false);
			}
			outer = Rect.MinMaxRect(x0 - TrimWidth, y0 - TrimWidth, x1 + TrimWidth, y1 + TrimWidth);
			return b;
		}

		private static void Mount(Transform sign, Builder b, float x0, float x1, float y0, float y1, float back)
		{
			// Rebuilt from native code (token 0x06000431, RVA 0x27AA790)
			float span = x1 - x0;
			float[] posts = span > 0.7f ? new float[2]
			{
				x0 + span * 0.22f,
				x1 - span * 0.22f
			} : new float[1] { (x0 + x1) * 0.5f };
			Vector3 forward = sign.forward;
			Vector3 reach = sign.TransformVector(Vector3.forward);
			float midY = (y0 + y1) * 0.5f;
			float scale = Mathf.Max(0.0001f, reach.magnitude);
			Vector3 centre = sign.TransformPoint(new Vector3((x0 + x1) * 0.5f, midY, back));
			if (Physics.Raycast(centre, forward, out RaycastHit wall, scale, ~4, QueryTriggerInteraction.Ignore))
			{
				float gap = wall.distance / scale;
				if (gap > 0.004f)
				{
					foreach (float x in posts)
					{
						b.Box(new Vector3(x - 0.04f, midY - 0.05f, back), new Vector3(x + 0.04f, midY + 0.05f, gap + back + 0.02f), Post(0), false);
					}
				}
				return;
			}
			int k = 0;
			foreach (float x in posts)
			{
				if (Physics.Raycast(sign.TransformPoint(new Vector3(x, y0, back + 0.045f)), Vector3.down, out RaycastHit ground, 8f, ~4, QueryTriggerInteraction.Ignore))
				{
					float foot = sign.InverseTransformPoint(ground.point).y;
					b.Box(new Vector3(x - 0.045f, foot, back), new Vector3(x + 0.045f, y1 - 0.04f, back + 0.09f), Post(k++), true);
				}
			}
		}
	}
}
