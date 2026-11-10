using System;
using UnityEngine;
using UnityEngine.Android;

namespace Shallows
{
	public static class SharedMicrophone
	{
		public sealed class Reader : IDisposable
		{
			private AudioClip source;

			private int lastPosition;

			private long micPosition;

			private long readPosition;

			private bool open;

			public bool Ready
			{
				get
				{
					// Rebuilt from native code (token 0x060006E4, RVA 0x28062AC)
					return open && clip;
				}
			}

			public int SampleRate
			{
				get
				{
					// Rebuilt from native code (token 0x060006E5, RVA 0x280633C)
					return SharedMicrophone.SampleRate;
				}
			}

			public int Channels
			{
				get
				{
					// Rebuilt from native code (token 0x060006E6, RVA 0x2806340)
					return SharedMicrophone.Channels;
				}
			}

			private void Sync()
			{
				// Rebuilt from native code (token 0x060006E7, RVA 0x28061F0)
				source = clip;
				lastPosition = source ? Microphone.GetPosition(null) : 0;
				micPosition = readPosition = lastPosition;
			}

			public int Available()
			{
				// Rebuilt from native code (token 0x060006E8, RVA 0x2806344)
				if (!open)
				{
					return 0;
				}
				Keep();
				if (source != clip)
				{
					Sync();
				}
				if (!source || !Microphone.IsRecording(null))
				{
					return 0;
				}
				int samples = source.samples;
				int position = Microphone.GetPosition(null);
				int delta = ((position - lastPosition) % samples + samples) % samples;
				lastPosition = position;
				micPosition += delta;
				if (micPosition - readPosition > samples / 2)
				{
					readPosition = micPosition - samples / 8;
				}
				return (int)(micPosition - readPosition);
			}

			public bool Read(float[] buffer)
			{
				// Rebuilt from native code (token 0x060006E9, RVA 0x28064AC)
				int channels = SharedMicrophone.Channels;
				if (channels < 1)
				{
					return false;
				}
				int frames = buffer.Length / channels;
				if (frames == 0 || Available() < frames)
				{
					return false;
				}
				source.GetData(buffer, (int)(readPosition % source.samples));
				readPosition += frames;
				return true;
			}

			public int ReadSome(float[] buffer)
			{
				// Rebuilt from native code (token 0x060006EA, RVA 0x280654C)
				int channels = SharedMicrophone.Channels;
				if (channels < 1)
				{
					return 0;
				}
				int frames = Math.Min(Available(), buffer.Length / channels);
				if (frames < 1)
				{
					return 0;
				}
				source.GetData(buffer.AsSpan(0, frames * channels), (int)(readPosition % source.samples));
				readPosition += frames;
				return frames;
			}

			public void Dispose()
			{
				// Rebuilt from native code (token 0x060006EB, RVA 0x2806684)
				if (open)
				{
					source = null;
					open = false;
					Release();
				}
			}

			public Reader()
			{
				// Rebuilt from native code (token 0x060006E3, RVA 0x28061C8)
				open = true;
				Acquire();
				Sync();
			}
		}

		private const int LoopSeconds = 2;

		private const int PreferredRate = 48000;

		private static AudioClip clip;

		private static int users;

		private static float retryAt;

		private static int rate;

		public static int SampleRate
		{
			get
			{
				// Rebuilt from native code (token 0x060006DD, RVA 0x2805D68)
				if (clip)
				{
					return clip.frequency;
				}
				if (rate == 0 && Microphone.devices.Length != 0)
				{
					Microphone.GetDeviceCaps(null, out int minFreq, out int maxFreq);
					rate = (minFreq == 0 && maxFreq == 0) ? PreferredRate : Mathf.Clamp(PreferredRate, minFreq, maxFreq);
				}
				return rate;
			}
		}

		public static int Channels
		{
			get
			{
				// Rebuilt from native code (token 0x060006DE, RVA 0x2805E88)
				if (!clip)
				{
					return 1;
				}
				return clip.channels;
			}
		}

		public static bool HasDevice
		{
			get
			{
				// Rebuilt from native code (token 0x060006DF, RVA 0x2805F2C)
				return Microphone.devices.Length != 0;
			}
		}

		private static void Acquire()
		{
			// Rebuilt from native code (token 0x060006E0, RVA 0x2805F54)
			users++;
			retryAt = 0f;
			Keep();
		}

		private static void Release()
		{
			// Rebuilt from native code (token 0x060006E1, RVA 0x2806104)
			if (--users > 0)
			{
				return;
			}
			users = 0;
			if (clip)
			{
				Microphone.End(null);
				clip = null;
			}
		}

		private static void Keep()
		{
			// Rebuilt from native code (token 0x060006E2, RVA 0x2805FA4)
			if (users < 1)
			{
				return;
			}
			if (clip && Microphone.IsRecording(null))
			{
				return;
			}
			if (Time.realtimeSinceStartup < retryAt)
			{
				return;
			}
			clip = null;
			retryAt = Time.realtimeSinceStartup + 1f;
			if (Microphone.devices.Length != 0 && Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
			{
				clip = Microphone.Start(null, true, LoopSeconds, SampleRate);
			}
		}
	}
}
