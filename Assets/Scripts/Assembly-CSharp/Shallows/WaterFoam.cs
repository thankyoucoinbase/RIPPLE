using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(100)]
	public class WaterFoam : MonoBehaviour
	{
		private struct Candidate
		{
			public float priority;

			public Vector4 shape;

			public Vector4 look;

			public Vector4 amount;
		}

		public const int MaxShapes = 64;

		[Tooltip("The foam step's material (shader Hidden/Shallows/Foam Field).")]
		public Material simMaterial;

		[Tooltip("Size of the square of water round the player with live foam (m). Fades out over its last 3 m.")]
		public float meters = 40f;

		[Tooltip("Texels across. 512 over 40 m is about 8 cm each.")]
		public int resolution = 512;

		[Tooltip("Seconds for foam left behind (wakes) to fade to about a third.")]
		public float fadeTime = 1.4f;

		[Tooltip("How quickly left-behind foam spreads out (0 to 1, per 60th of a second).")]
		public float spread = 0.12f;

		private static readonly int ShiftId = Shader.PropertyToID("_FoamShift");

		private static readonly int OriginId = Shader.PropertyToID("_FoamOrigin");

		private static readonly int FadeId = Shader.PropertyToID("_FoamFade");

		private static readonly int SpreadId = Shader.PropertyToID("_FoamSpread");

		private static readonly int CountId = Shader.PropertyToID("_FoamCount");

		private static readonly int ShapesId = Shader.PropertyToID("_FoamShapes");

		private static readonly int LooksId = Shader.PropertyToID("_FoamLooks");

		private static readonly int AmountsId = Shader.PropertyToID("_FoamAmounts");

		private static readonly int FieldId = Shader.PropertyToID("_ShallowsFoamField");

		private static readonly int RectId = Shader.PropertyToID("_ShallowsFoamRect");

		private readonly Vector4[] shapes = new Vector4[MaxShapes];

		private readonly Vector4[] looks = new Vector4[MaxShapes];

		private readonly Vector4[] amounts = new Vector4[MaxShapes];

		private int count;

		private Rect window;

		private Vector2 centreXZ;

		private readonly List<Candidate> candidates = new List<Candidate>(128);

		private static readonly Comparison<Candidate> ByPriority = (a, b) => a.priority.CompareTo(b.priority);

		private RenderTexture read;

		private RenderTexture write;

		private Vector2 origin;

		private bool hasOrigin;

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000B30, RVA 0x276E290)
			Shader.SetGlobalVector(RectId, Vector4.zero);
			if (read)
			{
				read.Release();
			}
			if (write)
			{
				write.Release();
			}
			write = null;
			read = null;
			hasOrigin = false;
		}

		private RenderTexture MakeTexture()
		{
			// Rebuilt from native code (token 0x06000B31, RVA 0x276E3E4)
			RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf) ? RenderTextureFormat.RHalf : RenderTextureFormat.R8;
			RenderTexture texture = new RenderTexture(resolution, resolution, 0, format, RenderTextureReadWrite.Linear);
			texture.name = "Water Foam";
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.useMipMap = false;
			texture.Create();
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = texture;
			GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
			RenderTexture.active = active;
			return texture;
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000B32, RVA 0x276E520)
			if (!simMaterial || WaterVolume.First == null)
			{
				return;
			}
			if (!read || !read.IsCreated())
			{
				read = MakeTexture();
				write = MakeTexture();
				hasOrigin = false;
			}
			PlayerRig rig = PlayerRig.I;
			Vector3 centre = (rig && rig.head) ? rig.head.position : (Camera.main ? Camera.main.transform.position : Vector3.zero);
			float half = meters * 0.5f;
			float texel = meters / resolution;
			Vector2 corner = new Vector2(centre.x - half, centre.z - half) / texel;
			Vector2 snapped = new Vector2(Mathf.Floor(corner.x), Mathf.Floor(corner.y)) * texel;
			Vector2 shift = hasOrigin ? (snapped - origin) / meters : new Vector2(2f, 2f);
			origin = snapped;
			hasOrigin = true;
			centreXZ = new Vector2(centre.x, centre.z);
			window = new Rect(snapped.x - 1f, snapped.y - 1f, meters + 2f, meters + 2f);
			float dt = Mathf.Max(Time.deltaTime, 0.0001f);
			Gather(dt, rig);
			simMaterial.SetVector(ShiftId, new Vector4(shift.x, shift.y, 1f / resolution, read.format == RenderTextureFormat.R8 ? 0.002f : 0.0005f));
			simMaterial.SetVector(OriginId, new Vector4(snapped.x, snapped.y, meters, 0f));
			simMaterial.SetFloat(FadeId, MathF.Exp(-dt / Mathf.Max(0.05f, fadeTime)));
			simMaterial.SetFloat(SpreadId, Mathf.Clamp01(dt * spread * 60f));
			simMaterial.SetFloat(CountId, count);
			simMaterial.SetVectorArray(ShapesId, shapes);
			simMaterial.SetVectorArray(LooksId, looks);
			simMaterial.SetVectorArray(AmountsId, amounts);
			Graphics.Blit(read, write, simMaterial);
			RenderTexture swap = read;
			read = write;
			write = swap;
			Shader.SetGlobalTexture(FieldId, read);
			Shader.SetGlobalVector(RectId, new Vector4(snapped.x, snapped.y, 1f / meters, 1f));
		}

		private void Gather(float dt, PlayerRig rig)
		{
			// Rebuilt from native code (token 0x06000B33, RVA 0x276EB78)
			candidates.Clear();
			List<FoamCaster> casters = FoamCaster.All;
			for (int i = 0; i < casters.Count; i++)
			{
				if (casters[i].Foam(dt, out Vector4 shape, out Vector4 look, out Vector4 amount))
				{
					Rect bounds = new Rect(shape.x - shape.z - look.w, shape.y - shape.w - look.w, 2f * (shape.z + look.w), 2f * (shape.w + look.w));
					if (window.Overlaps(bounds))
					{
						Add(shape, look, amount);
					}
				}
			}
			if (rig && !rig.Frozen)
			{
				Upright(rig.BodyBottom, 0.2f, 1f, 0.6f, 0.22f, true);
				if (rig.leftHand)
				{
					Ball(rig.leftHand.position, 0.07f, true);
				}
				if (rig.rightHand)
				{
					Ball(rig.rightHand.position, 0.07f, true);
				}
			}
			List<Vector3> remoteFeet = NetBridge.RemoteFeet;
			for (int i = 0; i < remoteFeet.Count; i++)
			{
				Upright(remoteFeet[i], 0.2f, 1f, 0.6f, 0.22f);
			}
			List<Vector3> remoteHands = NetBridge.RemoteHands;
			for (int i = 0; i < remoteHands.Count; i++)
			{
				Ball(remoteHands[i], 0.07f);
			}
			List<MonsterAI> monsters = MonsterAI.All;
			for (int i = 0; i < monsters.Count; i++)
			{
				Transform[] feet = monsters[i].feet;
				if (feet == null)
				{
					continue;
				}
				foreach (Transform foot in feet)
				{
					if (foot)
					{
						Upright(foot.position + Vector3.down * 0.05f, 0.09f, 1f, 0.7f, 0.2f);
					}
				}
			}
			candidates.Sort(ByPriority);
			count = Mathf.Min(candidates.Count, MaxShapes);
			for (int i = 0; i < count; i++)
			{
				shapes[i] = candidates[i].shape;
				looks[i] = candidates[i].look;
				amounts[i] = candidates[i].amount;
			}
		}

		private void Upright(Vector3 bottom, float radius, float height, float edge, float band, bool mine = false)
		{
			// Rebuilt from native code (token 0x06000B34, RVA 0x276F4C0)
			WaterVolume water = WaterVolume.At(bottom);
			if (!water)
			{
				return;
			}
			float surfaceY = water.SurfaceY;
			if (bottom.y > surfaceY || bottom.y + height < surfaceY)
			{
				return;
			}
			Round(bottom, radius, edge, band, mine);
		}

		private void Ball(Vector3 centre, float radius, bool mine = false)
		{
			// Rebuilt from native code (token 0x06000B35, RVA 0x276F608)
			WaterVolume water = WaterVolume.At(centre);
			if (!water)
			{
				return;
			}
			float surfaceY = water.SurfaceY;
			if (Mathf.Abs(surfaceY - centre.y) >= radius)
			{
				return;
			}
			Round(centre, Mathf.Sqrt(radius * radius - (surfaceY - centre.y) * (surfaceY - centre.y)) + 0.02f, 0.75f, 0.15f, mine);
		}

		private void Round(Vector3 at, float radius, float edge, float band, bool mine)
		{
			// Rebuilt from native code (token 0x06000B36, RVA 0x276F744)
			if (window.Contains(new Vector2(at.x, at.z)))
			{
				Add(new Vector4(at.x, at.z, radius, radius), new Vector4(1f, 0f, radius, band), new Vector4(edge, 0f, 0f, 0f), mine);
			}
		}

		private void Add(Vector4 shape, Vector4 look, Vector4 amount, bool mine = false)
		{
			// Rebuilt from native code (token 0x06000B37, RVA 0x276F300)
			float reach = Mathf.Max(shape.z, shape.w);
			float priority = mine ? -1f : Mathf.Max(0f, Vector2.Distance(new Vector2(shape.x, shape.y), centreXZ) - reach);
			candidates.Add(new Candidate
			{
				priority = priority,
				shape = shape,
				look = look,
				amount = amount
			});
		}
	}
}
