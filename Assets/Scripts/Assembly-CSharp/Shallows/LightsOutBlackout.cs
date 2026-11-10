using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(60)]
	public class LightsOutBlackout : MonoBehaviour
	{
		[Tooltip("How often to look for things that moved into or out of a dark zone (s).")]
		public float rescanEvery = 1f;

		[Tooltip("Things this far outside a dark zone are watched too, so they vanish the moment they walk in (m).")]
		public float margin = 6f;

		[Tooltip("Inside, things show once the room is this dark, so they don't flash lit as you step in.")]
		[Range(0f, 1f)]
		public float showAtDarkness = 0.9f;

		[Tooltip("Anything this close to your head is yours (hands, held items, belt) and is never hidden (m).")]
		public float ownReach = 1.3f;

		private static readonly int DarkId = Shader.PropertyToID("_ShallowsDark");

		private readonly List<Renderer> watched = new List<Renderer>();

		private readonly HashSet<Renderer> hidden = new HashSet<Renderer>();

		private readonly List<Renderer> unhide = new List<Renderer>();

		private readonly List<AtmosphereZone> dark = new List<AtmosphereZone>();

		private float nextScan;

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000439, RVA 0x27ABC44)
			ShowAll();
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x0600043A, RVA 0x27ABDF0)
			PlayerRig rig = PlayerRig.I;
			Atmosphere atmosphere = Atmosphere.I;
			dark.Clear();
			if (atmosphere && atmosphere.zones != null)
			{
				foreach (AtmosphereZone zone in atmosphere.zones)
				{
					if (zone != null && zone.darkness > 0.9f)
					{
						dark.Add(zone);
					}
				}
			}
			if (!rig || !rig.head || dark.Count == 0)
			{
				ShowAll();
				return;
			}
			if (Time.unscaledTime >= nextScan)
			{
				nextScan = Time.unscaledTime + rescanEvery;
				Scan(rig);
			}
			Vector3 head = rig.head.position;
			AtmosphereZone here = Atmosphere.At(head);
			Vector4 darkness = Shader.GetGlobalVector(DarkId);
			float show = showAtDarkness;
			float reach = ownReach * ownReach;
			foreach (Renderer r in watched)
			{
				if (!r)
				{
					continue;
				}
				Vector3 center = r.bounds.center;
				bool inDark = false;
				if ((center - head).sqrMagnitude > reach)
				{
					foreach (AtmosphereZone zone in dark)
					{
						if (zone.bounds.Contains(center) && !(zone == here && darkness.x >= show))
						{
							inDark = true;
							break;
						}
					}
				}
				if (inDark)
				{
					if (hidden.Add(r))
					{
						r.forceRenderingOff = true;
					}
				}
				else if (hidden.Remove(r))
				{
					r.forceRenderingOff = false;
				}
			}
			unhide.Clear();
			foreach (Renderer r in hidden)
			{
				if (!r || !watched.Contains(r))
				{
					unhide.Add(r);
				}
			}
			foreach (Renderer r in unhide)
			{
				hidden.Remove(r);
				if (r)
				{
					r.forceRenderingOff = false;
				}
			}
		}

		private void Scan(PlayerRig rig)
		{
			// Rebuilt from native code (token 0x0600043B, RVA 0x27AC828)
			watched.Clear();
			foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
			{
				if (Near(r.bounds) && !Ours(r, rig) && !DarkByItself(r) && !ShowsInTheDark(r))
				{
					watched.Add(r);
				}
			}
		}

		private bool Near(Bounds b)
		{
			// Rebuilt from native code (token 0x0600043C, RVA 0x27ACA44)
			foreach (AtmosphereZone zone in dark)
			{
				Bounds bounds = zone.bounds;
				bounds.Expand(margin * 2f);
				if (bounds.Contains(b.center))
				{
					return true;
				}
			}
			return false;
		}

		private static bool Ours(Renderer r, PlayerRig rig)
		{
			// Rebuilt from native code (token 0x0600043D, RVA 0x27ACBD8)
			if (r.gameObject.layer == 30 || r.transform.IsChildOf(rig.transform))
			{
				return true;
			}
			ApeBody apeBody = r.GetComponentInParent<ApeBody>();
			if (!apeBody)
			{
				return false;
			}
			return apeBody == ApeBody.Local;
		}

		private static bool DarkByItself(Renderer r)
		{
			// Rebuilt from native code (token 0x0600043E, RVA 0x27ACD68)
			foreach (Material material in r.sharedMaterials)
			{
				if (!material || !material.shader || !material.shader.name.StartsWith("Shallows/"))
				{
					return false;
				}
			}
			return true;
		}

		private static bool ShowsInTheDark(Renderer r)
		{
			// Rebuilt from native code (token 0x0600043F, RVA 0x27ACEAC)
			NetAvatar netAvatar = r.GetComponentInParent<NetAvatar>();
			if (!netAvatar)
			{
				return false;
			}
			if (netAvatar.beam && r.gameObject == netAvatar.beam.gameObject)
			{
				return true;
			}
			if (netAvatar.nameTag)
			{
				return r.gameObject == netAvatar.nameTag.gameObject;
			}
			return false;
		}

		private void ShowAll()
		{
			// Rebuilt from native code (token 0x06000440, RVA 0x27ABC48)
			foreach (Renderer r in hidden)
			{
				if (r)
				{
					r.forceRenderingOff = false;
				}
			}
			hidden.Clear();
		}
	}
}
