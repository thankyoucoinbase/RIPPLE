using System;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(50)]
	public class Headlamp : MonoBehaviour
	{
		public Light spot;

		public Color color = new Color(1f, 0.95f, 0.85f, 1f);

		[Tooltip("How bright it makes the tiles and water.")]
		public float brightness = 1.6f;

		public float range = 7.5f;

		[Tooltip("Half-angles of the bright middle and the soft edge (degrees).")]
		public float innerAngle = 18f;

		[Tooltip("Half-angles of the bright middle and the soft edge (degrees).")]
		public float outerAngle = 34f;

		private static readonly int PosId = Shader.PropertyToID("_ShallowsLampPos");

		private static readonly int DirId = Shader.PropertyToID("_ShallowsLampDir");

		private static readonly int ColorId = Shader.PropertyToID("_ShallowsLampColor");

		private float level;

		public bool On { get; private set; }

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000419, RVA 0x27A7CCC)
			Shader.SetGlobalVector(ColorId, Vector4.zero);
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x0600041A, RVA 0x27A7D64)
			bool on = Atmosphere.HeadlampAt(transform.position);
			On = on;
			level = Mathf.MoveTowards(level, on ? 1f : 0f, Time.deltaTime * 3f);
			if (spot)
			{
				spot.enabled = level > 0.01f;
				spot.intensity = level * 2.5f;
				spot.range = range;
				spot.spotAngle = outerAngle * 2f;
				spot.innerSpotAngle = innerAngle * 2f;
				spot.color = color;
			}
			if (level <= 0f)
			{
				Shader.SetGlobalVector(ColorId, Vector4.zero);
				return;
			}
			Vector3 position = transform.position;
			Vector3 forward = transform.forward;
			Shader.SetGlobalVector(PosId, new Vector4(position.x, position.y, position.z, range));
			Shader.SetGlobalVector(DirId, new Vector4(forward.x, forward.y, forward.z, MathF.Cos(outerAngle * Mathf.Deg2Rad)));
			Shader.SetGlobalVector(ColorId, new Vector4(color.r * brightness * level, color.g * brightness * level, color.b * brightness * level, MathF.Cos(innerAngle * Mathf.Deg2Rad)));
		}
	}
}
