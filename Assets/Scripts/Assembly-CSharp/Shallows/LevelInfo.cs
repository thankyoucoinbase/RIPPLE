using System;
using UnityEngine;

namespace Shallows
{
	[Serializable]
	public class LevelInfo
	{
		public string name;

		[Tooltip("Where you respawn after dying in this level.")]
		public Transform spawn;

		[Tooltip("The dry start deck. Standing here makes this level your checkpoint.")]
		public Bounds startDeck;
	}
}
