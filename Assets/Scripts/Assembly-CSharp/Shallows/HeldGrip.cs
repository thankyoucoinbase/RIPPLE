using UnityEngine;

namespace Shallows
{
	public class HeldGrip
	{
		public readonly Vector3 knuckles;

		public readonly Vector3 thumb;

		public readonly float lift;

		public readonly Vector3 middle;

		public readonly float radius;

		public readonly float length;

		private const float Squash = 0.004f;

		public static readonly HeldGrip Pebble = new HeldGrip(Vector3.forward, Vector3.right, 0.02f, Vector3.zero, 0.03f);

		public static readonly HeldGrip Duck = new HeldGrip(Vector3.forward, Vector3.up, 0.047f, new Vector3(0f, 0.022f, 0.006f), 0.047f);

		public static readonly HeldGrip Fuse = new HeldGrip(Vector3.forward, Vector3.up, 0.025f, Vector3.zero, 0.025f, 0.08f);

		public static readonly HeldGrip GlowStick = new HeldGrip(Vector3.forward, Vector3.up, 0.02f, Vector3.zero, 0.02f, 0.07f);

		public static readonly HeldGrip Sponge = new HeldGrip(Vector3.forward, Vector3.right, 0.027f, Vector3.zero, 0.033f, 0.02f);

		public static readonly HeldGrip Can = new HeldGrip(Vector3.forward, Vector3.up, 0.033f, Vector3.zero, 0.033f, 0.035f);

		private HeldGrip(Vector3 knuckles, Vector3 thumb, float lift, Vector3 middle, float radius, float length = 0f)
		{
			// Rebuilt from native code (token 0x0600080C, RVA 0x28220B4)
			this.knuckles = knuckles;
			this.thumb = thumb;
			this.lift = lift;
			this.middle = middle;
			this.radius = radius;
			this.length = length;
		}

		public static HeldGrip For(ItemKind kind)
		{
			// Rebuilt from native code (token 0x0600080D, RVA 0x282213C)
			if (kind == ItemKind.GlowStick)
			{
				return GlowStick;
			}
			if (kind == ItemKind.Duck)
			{
				return Duck;
			}
			if (kind == ItemKind.Sponge)
			{
				return Sponge;
			}
			if (LoungeCan.Is(kind))
			{
				return Can;
			}
			return Pebble;
		}

		public static HeldGrip For(Throwable item)
		{
			// Rebuilt from native code (token 0x0600080E, RVA 0x2822254)
			if (!item)
			{
				return null;
			}
			if (item.GetComponent<Shallows.Fuse>())
			{
				return Fuse;
			}
			return For((ItemKind)item.netKind);
		}

		public void Place(Vector3 palmPoint, Vector3 knucklesDir, Vector3 thumbDir, Vector3 palmDir, out Vector3 position, out Quaternion rotation, out Vector3 center)
		{
			// Rebuilt from native code (token 0x0600080F, RVA 0x28179D0)
			rotation = Quaternion.LookRotation(knucklesDir, thumbDir) * Quaternion.Inverse(Quaternion.LookRotation(knuckles, thumb));
			center = palmPoint + palmDir * (lift - Squash);
			position = center - rotation * middle;
		}

		public float Distance(Vector3 point, Vector3 center, Vector3 thumbDir)
		{
			// Rebuilt from native code (token 0x06000810, RVA 0x2818678)
			float along = Mathf.Clamp(Vector3.Dot(point - center, thumbDir), -length, length);
			return Vector3.Distance(point, center + thumbDir * along) - radius;
		}

		public static void Fallback(Transform hand, Vector3 offset, out Vector3 position, out Quaternion rotation)
		{
			// Rebuilt from native code (token 0x06000811, RVA 0x2822368)
			position = hand.position + hand.rotation * offset;
			rotation = hand.rotation;
		}
	}
}
