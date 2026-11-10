using UnityEngine;

namespace Shallows
{
	[RequireComponent(typeof(GorillaLocomotion))]
	public class HandTaps : MonoBehaviour
	{
		public AudioSource left;

		public AudioSource right;

		[Tooltip("Seconds before the same hand can tap again.")]
		public float cooldown = 0.08f;

		[Tooltip("Hand speed (m/s) that gives the loudest tap and the strongest buzz.")]
		public float hardHit = 3f;

		[Tooltip("Seconds a hand stays wet after it comes out of the water.")]
		public float wetFor = 5f;

		private const float WetFlag = 2f;

		private GorillaLocomotion locomotion;

		private readonly float[] ready = new float[2];

		private readonly float[] wetUntil = new float[2];

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000804, RVA 0x2821444)
			locomotion = GetComponent<GorillaLocomotion>();
			foreach (AudioSource source in new AudioSource[] { left, right })
			{
				if (source)
				{
					source.minDistance = Mathf.Max(source.minDistance, 1.5f);
				}
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000805, RVA 0x28215AC)
			PlayerRig rig = PlayerRig.I;
			if (!rig)
			{
				return;
			}
			if (rig.leftHand && WaterVolume.IsUnderwater(rig.leftHand.position, out _))
			{
				wetUntil[0] = Time.time + wetFor;
			}
			if (rig.rightHand && WaterVolume.IsUnderwater(rig.rightHand.position, out _))
			{
				wetUntil[1] = Time.time + wetFor;
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x06000806, RVA 0x28217B0)
			locomotion.HandTouched += Touched;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000807, RVA 0x282183C)
			locomotion.HandTouched -= Touched;
		}

		private void Touched(bool isLeft, Vector3 at, float speed)
		{
			// Rebuilt from native code (token 0x06000808, RVA 0x28218C8)
			int hand = isLeft ? 0 : 1;
			if (Time.time < ready[hand])
			{
				return;
			}
			ready[hand] = Time.time + cooldown;
			if (WaterVolume.IsUnderwater(at, out _))
			{
				return;
			}
			float t = Mathf.Clamp01(speed / hardHit);
			bool wet = Time.time < wetUntil[hand];
			float volume = ShallowsTuning.I ? ShallowsTuning.I.tapVolume : 0.8f;
			AudioSource source = isLeft ? left : right;
			if (source && volume > 0f)
			{
				source.pitch = Mathf.Lerp(1.08f, 0.9f, t) * Random.Range(0.93f, 1.07f);
				source.PlayOneShot(wet ? Sfx.HandTapWet() : Sfx.HandTap(), Loudness(t) * volume);
			}
			XRHands.Haptic(isLeft, Mathf.Lerp(0.1f, 0.75f, t), Mathf.Lerp(0.025f, 0.07f, t), false);
			NetBridge.ReportTap(at, wet ? t + WetFlag : t);
		}

		private static float Loudness(float hit)
		{
			// Rebuilt from native code (token 0x06000809, RVA 0x2821C88)
			return Mathf.Lerp(0.35f, 1f, hit);
		}

		public static void PlayRemote(Vector3 at, float hit)
		{
			// Rebuilt from native code (token 0x0600080A, RVA 0x2821CB8)
			Camera main = Camera.main;
			if (!SplashSystem.I)
			{
				return;
			}
			if (main && (main.transform.position - at).sqrMagnitude > 400f)
			{
				return;
			}
			float volume = ShallowsTuning.I ? ShallowsTuning.I.tapVolume : 0.8f;
			if (volume <= 0f)
			{
				return;
			}
			bool wet = hit >= WetFlag;
			if (wet)
			{
				hit -= WetFlag;
			}
			float t = Mathf.Clamp01(hit);
			SplashSystem.I.PlaySound(wet ? Sfx.HandTapWet() : Sfx.HandTap(), at, Loudness(t) * volume, Mathf.Lerp(1.08f, 0.9f, t) * Random.Range(0.93f, 1.07f));
		}
	}
}
