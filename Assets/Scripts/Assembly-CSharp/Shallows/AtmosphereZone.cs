using System;
using UnityEngine;

namespace Shallows
{
	[Serializable]
	public class AtmosphereZone
	{
		public string name;

		public Bounds bounds;

		public Color fog = new Color(0f, 0f, 0f, 1f);

		public float fogStart = 0.5f;

		public float fogEnd = 7f;

		public Color ambient = new Color(0f, 0f, 0f, 1f);

		[Tooltip("Sun brightness here.")]
		public float sun = 0.05f;

		[Tooltip("0 = normal pool lighting, 1 = pitch black (only the headlamp lights the tiles and water).")]
		[Range(0f, 1f)]
		public float darkness;

		[Tooltip("Everyone here wears a headlamp.")]
		public bool headlamp;

		[Tooltip("How much it echoes here, against the normal tiled hall (0 = none, like outdoors).")]
		public float echo = 1f;

		[Tooltip("How far you can see here (m); 0 keeps the camera's own far clip.")]
		public float farClip;
	}
}
