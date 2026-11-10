using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(1200)]
	public class RippleCameraPresenter : MonoBehaviour
	{
		public RippleCamera owner;

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000077, RVA 0x27105AC)
			if (!owner || !owner.isActiveAndEnabled) return;
			owner.Present(Time.unscaledDeltaTime);
		}
	}
}
