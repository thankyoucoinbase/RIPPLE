using UnityEngine;

namespace Shallows
{
	public class LoungeVisibility : MonoBehaviour
	{
		public Renderer[] renderers;

		public float fromY;

		private bool shown = true;

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000479, RVA 0x27B2C28)
			bool visible = true;
			if (Application.isPlaying)
			{
				Camera main = Camera.main;
				if (main)
				{
					visible = main.transform.position.y > fromY;
				}
			}
			if (shown == visible)
			{
				return;
			}
			shown = visible;
			foreach (Renderer renderer in renderers)
			{
				if (renderer)
				{
					renderer.enabled = visible;
				}
			}
		}
	}
}
