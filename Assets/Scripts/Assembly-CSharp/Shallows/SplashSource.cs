using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(100)]
	public class SplashSource : MonoBehaviour
	{
		public SplashKind kind;

		[Tooltip("Hands only: which controller buzzes.")]
		public bool leftHand;

		[Tooltip("Objects only: how loud this thing is when it lands (pebble 1).")]
		public float loudness = 1f;

		private const int History = 4;

		private readonly Vector3[] positions = new Vector3[History];

		private readonly float[] times = new float[History];

		private int newest;

		private bool wasUnder;

		private float cooldownUntil;

		private float nextBuzz;

		private float nextWake;

		private float suppressUntil;

		private Vector3 Point
		{
			get
			{
				// Rebuilt from native code (token 0x06000B16, RVA 0x276B8DC)
				if (kind != SplashKind.Body || !PlayerRig.I)
				{
					return transform.position;
				}
				return PlayerRig.I.BodyBottom;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000B17, RVA 0x276B9DC)
			ResetTracking(0f);
		}

		public void ResetTracking(float suppressSeconds)
		{
			// Rebuilt from native code (token 0x06000B18, RVA 0x276B9E4)
			Vector3 point = Point;
			for (int i = 0; i < History; i++)
			{
				positions[i] = point;
				times[i] = Time.time - (History - 1 - i) * 0.014f;
			}
			newest = History - 1;
			wasUnder = WaterVolume.IsUnderwater(point, out _);
			suppressUntil = Time.time + suppressSeconds;
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000B19, RVA 0x276BB20)
			if (Time.deltaTime <= 0f)
			{
				return;
			}
			Vector3 point = Point;
			newest = (newest + 1) % History;
			positions[newest] = point;
			times[newest] = Time.time;
			bool under = WaterVolume.IsUnderwater(point, out WaterVolume water);
			ShallowsTuning t = ShallowsTuning.I;
			bool muted = Time.time < suppressUntil || ((kind == SplashKind.Hand || kind == SplashKind.Body) && PlayerRig.I && PlayerRig.I.Frozen);
			if (!muted && t && SplashSystem.I && water != null)
			{
				Vector3 v = Velocity();
				Vector3 surface = new Vector3(point.x, water.SurfaceY, point.z);
				if (under)
				{
					if (!wasUnder)
					{
						Enter(surface, v, t);
					}
					else
					{
						MoveUnderwater(surface, v, t);
					}
				}
				else if (wasUnder)
				{
					Exit(surface);
				}
			}
			wasUnder = under;
		}

		private Vector3 Velocity()
		{
			// Rebuilt from native code (token 0x06000B1A, RVA 0x276BE8C)
			int oldest = (newest + 1) % History;
			float dt = times[newest] - times[oldest];
			if (dt > 0.0001f)
			{
				return (positions[newest] - positions[oldest]) / dt;
			}
			return Vector3.zero;
		}

		private void Enter(Vector3 surface, Vector3 v, ShallowsTuning t)
		{
			// Rebuilt from native code (token 0x06000B1B, RVA 0x276BF88)
			float down = Mathf.Max(0f, -v.y);
			float across = Mathf.Sqrt(v.x * v.x + v.z * v.z);
			float speed;
			float threshold;
			if (kind == SplashKind.Body)
			{
				speed = down + across * 0.2f;
				threshold = t.bodySplashSpeed;
			}
			else if (kind == SplashKind.Hand)
			{
				speed = down + across * 0.35f;
				threshold = t.handSplashSpeed;
			}
			else
			{
				speed = v.magnitude * loudness;
				threshold = t.objectSplashSpeed;
			}
			if (speed >= threshold)
			{
				float intensity = t.IntensityFromSpeed(speed, threshold);
				Vector3 heading = kind == SplashKind.Body ? new Vector3(v.x, 0f, v.z) : Vector3.zero;
				SplashSystem.I.Loud(surface, intensity, kind, false, heading);
				if (kind == SplashKind.Body)
				{
					XRHands.HapticBoth(Mathf.Lerp(0.3f, 0.9f, intensity), Mathf.Lerp(0.08f, 0.25f, intensity));
				}
				else if (kind == SplashKind.Hand)
				{
					XRHands.Haptic(leftHand, Mathf.Lerp(0.4f, 1f, intensity), Mathf.Lerp(0.06f, 0.22f, intensity));
				}
				cooldownUntil = Time.time + 0.25f;
				return;
			}
			SplashSystem.I.Quiet(surface, speed / Mathf.Max(0.01f, threshold));
			if (kind == SplashKind.Body)
			{
				XRHands.HapticBoth(0.08f, 0.05f);
			}
			else if (kind == SplashKind.Hand)
			{
				XRHands.Haptic(leftHand, 0.12f, 0.03f);
			}
		}

		private void Exit(Vector3 surface)
		{
			// Rebuilt from native code (token 0x06000B1C, RVA 0x276C444)
			if (kind == SplashKind.Object)
			{
				return;
			}
			SplashSystem.I.Quiet(surface, 0.3f);
			if (kind == SplashKind.Hand)
			{
				XRHands.Haptic(leftHand, 0.08f, 0.03f);
			}
		}

		private void MoveUnderwater(Vector3 surface, Vector3 v, ShallowsTuning t)
		{
			// Rebuilt from native code (token 0x06000B1D, RVA 0x276C57C)
			if (kind == SplashKind.Body)
			{
				float speed = Mathf.Sqrt(v.x * v.x + v.z * v.z);
				float quiet = Mathf.Max(0.01f, t.wadeQuietSpeed);
				if (speed > quiet && Time.time >= cooldownUntil)
				{
					float loud = Mathf.Max(quiet + 0.1f, t.wadeLoudSpeed);
					float intensity = Mathf.Lerp(0.12f, 0.7f, Mathf.InverseLerp(quiet, loud, speed));
					SplashSystem.I.Loud(surface, intensity, kind);
					XRHands.HapticBoth(Mathf.Lerp(0.25f, 0.6f, intensity), 0.08f);
					cooldownUntil = Time.time + Mathf.Lerp(0.45f, 0.2f, intensity);
					return;
				}
				float buzz = Mathf.InverseLerp(quiet * 0.7f, quiet, speed);
				if (buzz > 0f && Time.time >= nextBuzz)
				{
					nextBuzz = Time.time + 0.05f;
					XRHands.HapticBoth(0.05f + 0.15f * buzz * buzz, 0.07f, false);
				}
				if (speed > 0.15f && Time.time >= nextWake)
				{
					nextWake = Time.time + 0.4f;
					SplashSystem.I.Ripple(surface, Mathf.Lerp(0.1f, 0.3f, speed / quiet));
				}
			}
			else if (kind == SplashKind.Hand)
			{
				float speed = v.magnitude;
				float churn = Mathf.Clamp01(speed / Mathf.Max(0.01f, t.handChurnSpeed));
				if (Time.time >= nextBuzz)
				{
					nextBuzz = Time.time + 0.05f;
					if (speed > 0.15f)
					{
						XRHands.Haptic(leftHand, 0.03f + 0.22f * churn * churn, 0.07f, false);
					}
				}
				if (speed > t.handChurnSpeed && Time.time >= cooldownUntil)
				{
					float intensity = t.IntensityFromSpeed(speed, t.handChurnSpeed) * 0.7f;
					SplashSystem.I.Loud(surface, intensity, kind);
					XRHands.Haptic(leftHand, Mathf.Lerp(0.35f, 0.8f, intensity), 0.08f);
					cooldownUntil = Time.time + Mathf.Lerp(0.4f, 0.18f, intensity);
					return;
				}
				if (speed > 0.3f && Time.time >= nextWake)
				{
					nextWake = Time.time + 0.35f;
					SplashSystem.I.Ripple(surface, Mathf.Lerp(0.08f, 0.22f, churn));
				}
			}
		}
	}
}
