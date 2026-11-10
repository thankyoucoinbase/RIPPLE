using UnityEngine;
using UnityEngine.XR;

namespace Shallows
{
	public static class HandPresence
	{
		private static int frame = -1;

		private static bool leftAway;

		private static bool rightAway;

		private static readonly bool[] seen = new bool[2];

		private static readonly Vector3[] lastLocal = new Vector3[2];

		private static readonly float[] lostSince = new float[2] { -1f, -1f };

		private const float LostGrace = 4f;

		public static int Changes { get; private set; }

		public static bool Away(bool left)
		{
			// Rebuilt from native code (token 0x060007FF, RVA 0x281ECD4)
			if (frame != Time.frameCount)
			{
				Refresh();
			}
			return left ? leftAway : rightAway;
		}

		private static void Refresh()
		{
			// Rebuilt from native code (token 0x06000800, RVA 0x2820C6C)
			frame = Time.frameCount;
			bool left;
			bool right;
			if (!XRSettings.isDeviceActive || (PlayerRig.I && PlayerRig.I.DesktopMode))
			{
				left = false;
				right = false;
			}
			else if (Application.isEditor || Application.isFocused)
			{
				left = !Tracked(XRNode.LeftHand, 0);
				right = !Tracked(XRNode.RightHand, 1);
			}
			else
			{
				left = true;
				right = true;
			}
			if (leftAway != left || rightAway != right)
			{
				Changes++;
			}
			leftAway = left;
			rightAway = right;
		}

		private static bool Tracked(XRNode node, int hand)
		{
			// Rebuilt from native code (token 0x06000801, RVA 0x2820F20)
			InputDevice device = InputDevices.GetDeviceAtXRNode(node);
			if (!device.isValid)
			{
				return !seen[hand];
			}
			seen[hand] = true;
			if (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || tracked || Moving(hand))
			{
				lostSince[hand] = -1f;
				return true;
			}
			float now = Time.unscaledTime;
			if (lostSince[hand] < 0f)
			{
				lostSince[hand] = now;
			}
			return now - lostSince[hand] < LostGrace;
		}

		private static bool Moving(int hand)
		{
			// Rebuilt from native code (token 0x06000802, RVA 0x2821188)
			PlayerRig rig = PlayerRig.I;
			Transform controller = rig ? (hand == 0 ? rig.leftController : rig.rightController) : null;
			if (!controller)
			{
				return false;
			}
			Vector3 position = controller.localPosition;
			Vector3 last = lastLocal[hand];
			lastLocal[hand] = position;
			return (position - last).sqrMagnitude > 1e-08f;
		}
	}
}
