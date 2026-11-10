using UnityEngine;

namespace Shallows
{
	public static class Fingertips
	{
		public const float Radius = 0.008f;

		private static ApeBody body;

		private static Transform leftMid;

		private static Transform leftEnd;

		private static Transform rightMid;

		private static Transform rightEnd;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060007D6, RVA 0x281B7BC)
			body = null;
			rightEnd = null;
			rightMid = null;
			leftEnd = null;
			leftMid = null;
		}

		public static bool Get(bool left, out Vector3 tip)
		{
			// Rebuilt from native code (token 0x060007D7, RVA 0x281B860)
			ApeBody local = ApeBody.Local;
			if (local != body)
			{
				body = local;
				leftMid = local ? LifeguardProps.Find(local.transform, "IndexIntermediate.L") : null;
				leftEnd = local ? LifeguardProps.Find(local.transform, "IndexDistal.L") : null;
				rightMid = local ? LifeguardProps.Find(local.transform, "IndexIntermediate.R") : null;
				rightEnd = local ? LifeguardProps.Find(local.transform, "IndexDistal.R") : null;
			}
			Transform mid = left ? leftMid : rightMid;
			Transform end = left ? leftEnd : rightEnd;
			if (mid && end && body.isActiveAndEnabled)
			{
				tip = end.position + (end.position - mid.position) * 0.9f;
				return true;
			}
			PlayerRig rig = PlayerRig.I;
			Transform hand = rig ? (left ? rig.leftHand : rig.rightHand) : null;
			if (!hand)
			{
				tip = Vector3.zero;
				return false;
			}
			Transform aim = left ? rig.leftController : rig.rightController;
			Vector3 from = hand.position;
			tip = from + (aim ? aim : hand).forward * 0.06f;
			return true;
		}
	}
}
