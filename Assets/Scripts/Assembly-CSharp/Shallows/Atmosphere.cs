using System;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(-35)]
	public class Atmosphere : MonoBehaviour
	{
		public Light sun;

		public AtmosphereZone[] zones;

		[Tooltip("How quickly it changes over when you walk into a zone.")]
		public float blendSpeed = 2.5f;

		private static readonly int DarkId = Shader.PropertyToID("_ShallowsDark");

		private AtmosphereZone normal;

		private Color fog;

		private Color ambient;

		private float fogStart;

		private float fogEnd;

		private float sunIntensity;

		private float darkness;

		private float reflections;

		private float farClip;

		private float baseFar;

		private Camera view;

		public static Atmosphere I { get; private set; }

		private void Awake()
		{
			// Rebuilt from native code (token 0x060003F3, RVA 0x27A1D88)
			I = this;
			if (!GetComponent<LightsOutBlackout>())
			{
				gameObject.AddComponent<LightsOutBlackout>();
			}
			normal = new AtmosphereZone
			{
				name = "Pool",
				fog = RenderSettings.fogColor,
				fogStart = RenderSettings.fogStartDistance,
				fogEnd = RenderSettings.fogEndDistance,
				ambient = RenderSettings.ambientLight,
				sun = sun ? sun.intensity : 1f
			};
			reflections = RenderSettings.reflectionIntensity;
			Snap(normal);
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060003F4, RVA 0x27A201C)
			if (I == this)
			{
				Shader.SetGlobalVector(DarkId, Vector4.zero);
				Snap(normal);
			}
		}

		public static AtmosphereZone At(Vector3 p)
		{
			// Rebuilt from native code (token 0x060003F5, RVA 0x27A2158)
			if (!I)
			{
				return null;
			}
			if (I.zones != null)
			{
				foreach (AtmosphereZone zone in I.zones)
				{
					if (zone.bounds.Contains(p))
					{
						return zone;
					}
				}
			}
			return null;
		}

		public static bool HeadlampAt(Vector3 p)
		{
			// Rebuilt from native code (token 0x060003F6, RVA 0x27A234C)
			AtmosphereZone zone = At(p);
			return zone != null && zone.headlamp;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060003F7, RVA 0x27A23D4)
			PlayerRig rig = PlayerRig.I;
			if (!rig)
			{
				return;
			}
			AtmosphereZone zone = At(rig.head.position) ?? normal;
			float t = 1f - MathF.Exp(-blendSpeed * Time.deltaTime);
			fog = Color.Lerp(fog, zone.fog, t);
			ambient = Color.Lerp(ambient, zone.ambient, t);
			fogStart = Mathf.Lerp(fogStart, zone.fogStart, t);
			fogEnd = Mathf.Lerp(fogEnd, zone.fogEnd, t);
			sunIntensity = Mathf.Lerp(sunIntensity, zone.sun, t);
			darkness = Mathf.Lerp(darkness, zone.darkness, t);
			float far = zone.farClip > 0f ? zone.farClip : baseFar;
			farClip = farClip > 0f ? Mathf.Lerp(farClip, far, t) : far;
			Apply();
		}

		private void Snap(AtmosphereZone zone)
		{
			// Rebuilt from native code (token 0x060003F8, RVA 0x27A1FD4)
			fog = zone.fog;
			ambient = zone.ambient;
			fogStart = zone.fogStart;
			fogEnd = zone.fogEnd;
			sunIntensity = zone.sun;
			darkness = zone.darkness;
			farClip = zone.farClip > 0f ? zone.farClip : baseFar;
			Apply();
		}

		private void Apply()
		{
			// Rebuilt from native code (token 0x060003F9, RVA 0x27A25B0)
			RenderSettings.fogColor = fog;
			RenderSettings.fogStartDistance = fogStart;
			RenderSettings.fogEndDistance = fogEnd;
			RenderSettings.ambientLight = ambient;
			RenderSettings.reflectionIntensity = reflections * (1f - darkness);
			if (sun)
			{
				sun.intensity = sunIntensity;
			}
			Shader.SetGlobalVector(DarkId, new Vector4(darkness, 0f, 0f, 0f));
			if (!view && PlayerRig.I)
			{
				view = PlayerRig.I.head.GetComponent<Camera>();
				if (view)
				{
					baseFar = view.farClipPlane;
				}
			}
			if (!view)
			{
				return;
			}
			view.backgroundColor = fog;
			if (farClip > 0f)
			{
				view.farClipPlane = farClip;
			}
		}
	}
}
