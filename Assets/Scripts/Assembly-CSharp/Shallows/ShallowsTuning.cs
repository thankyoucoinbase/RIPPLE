using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(-100)]
	public class ShallowsTuning : MonoBehaviour
	{
		[Header("Monster")]
		[Tooltip("Speed while it hasn't heard anything (m/s).")]
		public float roamSpeed = 3.2f;

		[Tooltip("Speed while running to a splash (m/s).")]
		public float chargeSpeed = 27f;

		[Tooltip("How long it searches a splash spot before wandering again (s).")]
		public float searchTime = 1.5f;

		[Tooltip("How far its arms reach (m). It grabs anyone wet this close while it charges at a splash or searches.")]
		public float grabReach = 2.2f;

		[Header("Monster hearing voices")]
		[Tooltip("How loud a voice has to be for a monster to hear it at all (0 to 1, the same scale as the mouth opening).")]
		public float voiceHearLevel = 0.2f;

		[Tooltip("How far a monster hears someone talking quietly, and someone shouting (m).")]
		public float voiceHearRangeMin = 8f;

		[Tooltip("How far a monster hears someone talking quietly, and someone shouting (m).")]
		public float voiceHearRange = 24f;

		[Tooltip("Chance (0 to 1) that a monster actually hears a sentence someone says within earshot. Rolled once per sentence (a pause of 1.5 s starts a new one), so talking is usually safe, and now and then it isn't.")]
		public float voiceHearChance = 0.1f;

		[Tooltip("How fast it creeps toward a voice it heard (m/s). Splashes still make it charge.")]
		public float stalkSpeed = 1.3f;

		[Header("Splash thresholds (m/s)")]
		[Tooltip("A hand hitting the water faster than this splashes. Slower is a silent dip.")]
		public float handSplashSpeed = 1.4f;

		[Tooltip("A hand moving underwater faster than this splashes.")]
		public float handChurnSpeed = 1.6f;

		[Tooltip("Your body dropping into the water faster than this splashes.")]
		public float bodySplashSpeed = 0.8f;

		[Tooltip("Moving through the water faster than this splashes. Slower is silent wading: only a really slow creep is.")]
		public float wadeQuietSpeed = 0.25f;

		[Tooltip("Wading this fast is as loud as wading gets (m/s).")]
		public float wadeLoudSpeed = 1.6f;

		[Tooltip("A thrown object hitting the water faster than this splashes.")]
		public float objectSplashSpeed = 1f;

		[Tooltip("Impact speed that counts as the loudest possible splash.")]
		public float fullIntensitySpeed = 6f;

		[Header("Hearing (m)")]
		[Tooltip("How far the smallest splash is heard.")]
		public float hearRadiusMin = 6f;

		[Tooltip("How far the biggest splash is heard.")]
		public float hearRadiusMax = 45f;

		[Tooltip("Splashes at least this loud (0 to 1) are heard by every monster in the room, however far (walls still stop them).")]
		public float heardEverywhere = 0.75f;

		[Header("Player")]
		[Tooltip("Eyes to the bottom of your body (m). Shorter puts the ground closer to your hands.")]
		public float bodyHeight = 0.55f;

		[Tooltip("How much the water slows you down.")]
		public float waterDrag = 3f;

		public float jumpMultiplier = 1.1f;

		public float maxJumpSpeed = 6.5f;

		[Tooltip("The fastest you can launch while your body is in the water (m/s): waist-deep water holds a jump back.")]
		public float waterJumpSpeed = 4f;

		[Header("Gorilla model")]
		[Tooltip("How far the body leans forward (degrees). It stays leaned whichever way you look.")]
		public float bodyLean = 20f;

		[Tooltip("The gorilla's size, as a multiple of the size that puts its feet at the bottom of your body.")]
		public float bodySize = 1.15f;

		[Tooltip("Tips the hands' knuckles toward the palm (positive) or away (degrees).")]
		public float handPitch;

		[Tooltip("Voice loudness (dB below full scale) where the mouth starts to open.")]
		public float mouthQuietDb = -45f;

		[Tooltip("Voice loudness (dB below full scale) where the mouth is fully open. In between, it opens as loud as the voice is.")]
		public float mouthLoudDb = -12f;

		[Header("Sound")]
		[Tooltip("How much the rooms echo. 1 is the normal tiled-hall echo, 0 is none.")]
		public float echo = 1f;

		[Tooltip("How loud your hands are when they hit something solid.")]
		public float tapVolume = 0.8f;

		[Tooltip("The pool hall's hum and drips.")]
		public float ambienceVolume = 0.35f;

		[Tooltip("Water swishing round you as you wade (only you hear it).")]
		public float wadingVolume = 0.6f;

		[Tooltip("The monsters' breathing.")]
		public float monsterBreathVolume = 0.8f;

		[Tooltip("How much louder other players' voices are than they arrive (dB, a limiter stops clipping). 0 is as sent.")]
		public float voiceBoostDb = 10f;

		[Tooltip("How loud your own mic is in camera videos, on top of the game sound (a limiter stops clipping).")]
		public float recordingMicVolume = 2f;

		[Header("Debug")]
		[Tooltip("Shows hearing rings on splashes and the monster's state above its head.")]
		public bool showDebug;

		[Tooltip("Shows your own gorilla's head, for checking the model. Normally it's hidden so it's never in your view.")]
		public bool showOwnHead;

		public static ShallowsTuning I { get; private set; }

		private void Awake()
		{
			// Rebuilt from native code (token 0x0600038F, RVA 0x279839C)
			I = this;
		}

		public float IntensityFromSpeed(float speed, float threshold)
		{
			// Rebuilt from native code (token 0x06000390, RVA 0x27983F4)
			return Mathf.Clamp01((speed - threshold) / Mathf.Max(0.01f, fullIntensitySpeed - threshold));
		}

		public float HearingRadius(float intensity)
		{
			// Rebuilt from native code (token 0x06000391, RVA 0x2798430)
			if (intensity >= heardEverywhere) return 1000f;
			return Mathf.Lerp(hearRadiusMin, hearRadiusMax, intensity);
		}
	}
}
