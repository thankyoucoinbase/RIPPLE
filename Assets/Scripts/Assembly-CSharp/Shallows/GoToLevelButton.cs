using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(PokeButton))]
	public class GoToLevelButton : MonoBehaviour
	{
		public int level;

		private void Start()
		{
			// Rebuilt from native code (token 0x060000A1, RVA 0x27130B0)
			GetComponent<PokeButton>().onPress = () =>
			{
				// Rebuilt from native code (token 0x060000A3, RVA 0x271316C)
				if (GameManager.I)
				{
					GameManager.I.GoToLevel(level);
				}
			};
		}
	}
}
