using UnityEngine;

namespace Shallows
{
	public class LoungeMusic : MonoBehaviour
	{
		[Tooltip("How loud it is right by the boombox (0-1).")]
		[Range(0f, 1f)]
		public float volume = 0.7f;

		public float fromY;

		public float floorY;

		public Vector2 ladder;

		public float ladderReach = 2.5f;

		private const float FadeIn = 1f;

		private const float Muffled = 1400f;

		private AudioSource music;

		private AudioLowPassFilter muffle;

		private void Start()
		{
			// Rebuilt from native code (token 0x06000476, RVA 0x27B27C4)
			AudioClip clip = Resources.Load<AudioClip>("Music/RippleVictory");
			if (!clip)
			{
				enabled = false;
				return;
			}
			music = gameObject.AddComponent<AudioSource>();
			music.clip = clip;
			music.loop = true;
			music.playOnAwake = false;
			music.volume = 0f;
			music.priority = 0;
			music.dopplerLevel = 0f;
			music.spatialBlend = 1f;
			music.rolloffMode = AudioRolloffMode.Logarithmic;
			music.minDistance = 1.5f;
			music.maxDistance = 40f;
			muffle = gameObject.AddComponent<AudioLowPassFilter>();
			muffle.cutoffFrequency = Muffled;
			music.Play();
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x06000477, RVA 0x27B29F4)
			if (!music) return;
			Camera main = Camera.main;
			float audible = 0f;
			float open = 0f;
			if (main)
			{
				Vector3 position = main.transform.position;
				if (position.y > floorY - 0.3f || Vector2.Distance(new Vector2(position.x, position.z), ladder) < ladderReach)
				{
					audible = Mathf.Clamp01((position.y - fromY) / FadeIn);
				}
				open = Mathf.Clamp01((position.y - (floorY - 0.4f)) / 0.6f);
			}
			music.volume = audible * volume;
			muffle.cutoffFrequency = Mathf.Lerp(Muffled, 22000f, open * open);
		}
	}
}
