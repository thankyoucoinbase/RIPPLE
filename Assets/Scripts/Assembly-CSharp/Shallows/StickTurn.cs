using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(-35)]
	public class StickTurn : MonoBehaviour
	{
		private const float SmoothDeadZone = 0.2f;

		private const float SnapPush = 0.7f;

		private const float SnapRelease = 0.35f;

		private PlayerRig rig;

		private HandGrabber[] hands;

		private bool snapArmed = true;

		private void Start()
		{
			// Rebuilt from native code (token 0x06000842, RVA 0x282881C)
			rig = GetComponent<PlayerRig>();
			PlayerProfile.Load();
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000843, RVA 0x28288A8)
			if (!rig || !rig.head || rig.Frozen || rig.DesktopMode || Time.deltaTime <= 0f)
			{
				return;
			}
			Vector2 stick = HandPresence.Away(false) ? Vector2.zero : XRHands.Stick(false);
			switch (PlayerProfile.Turn)
			{
			case PlayerProfile.TurnMode.Snap:
				if (snapArmed && Mathf.Abs(stick.x) > SnapPush)
				{
					snapArmed = false;
					float angle = PlayerProfile.SnapTurnAngle;
					Turn(stick.x < 0f ? -angle : angle, true);
				}
				else if (Mathf.Abs(stick.x) < SnapRelease)
				{
					snapArmed = true;
				}
				break;
			case PlayerProfile.TurnMode.Smooth:
			{
				snapArmed = true;
				float amount = Mathf.Clamp01((Mathf.Abs(stick.x) - SmoothDeadZone) / (1f - SmoothDeadZone));
				if (amount > 0f)
				{
					Turn((stick.x < 0f ? -amount : amount) * PlayerProfile.SmoothTurnSpeed * Time.deltaTime, false);
				}
				break;
			}
			default:
				snapArmed = true;
				break;
			}
		}

		private void Turn(float degrees, bool snap)
		{
			// Rebuilt from native code (token 0x06000844, RVA 0x2828BC4)
			Vector3 pivot = rig.head.position;
			Quaternion rotation = Quaternion.AngleAxis(degrees, Vector3.up);
			rig.TurnBy(degrees);
			if (rig.locomotion)
			{
				rig.locomotion.Turned(pivot, rotation);
			}
			if (hands == null)
			{
				hands = FindObjectsByType<HandGrabber>(FindObjectsSortMode.None);
			}
			foreach (HandGrabber hand in hands)
			{
				if (hand)
				{
					hand.Turned(pivot, rotation);
				}
			}
			if (ApeBody.Local)
			{
				ApeBody.Local.Turned(degrees);
			}
			if (!snap)
			{
				return;
			}
			if (rig.leftHandSplash)
			{
				rig.leftHandSplash.ResetTracking(0.1f);
			}
			if (rig.rightHandSplash)
			{
				rig.rightHandSplash.ResetTracking(0.1f);
			}
			if (rig.bodySplash)
			{
				rig.bodySplash.ResetTracking(0.1f);
			}
		}
	}
}
