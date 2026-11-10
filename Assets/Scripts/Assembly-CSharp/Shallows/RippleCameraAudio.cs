using System;
using System.Diagnostics;
using UnityEngine;

namespace Shallows
{
	public class RippleCameraAudio : MonoBehaviour
	{
		private readonly object gate = new object();

		private readonly float[] samples = new float[192000];

		private readonly float[] microphone = new float[96000];

		private readonly float[] micChunk = new float[4096];

		private SharedMicrophone.Reader micReader;

		private int micRead;

		private int micWrite;

		private int micCount;

		private int inputRate;

		private double resamplePhase;

		private float previousMic;

		private int read;

		private int write;

		private int count;

		private long totalFrames;

		private long origin;

		private long firstOffset;

		private int sampleRate;

		private PeakLimiter limiter;

		private volatile float micVolume = 2f;

		public bool Capturing { get; private set; }

		public bool HasPendingAudio
		{
			get
			{
				// Rebuilt from native code (token 0x06000062, RVA 0x270E844)
				lock (gate)
				{
					return count > 0;
				}
			}
		}

		public static RippleCameraAudio Attach()
		{
			// Rebuilt from native code (token 0x0600005F, RVA 0x270E338)
			AudioListener active = null;
			foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
			{
				if (listener.isActiveAndEnabled && listener.TryGetComponent(out RippleCameraAudio existing))
				{
					return existing;
				}
				if (listener.isActiveAndEnabled && !active)
				{
					active = listener;
				}
			}
			if (!active)
			{
				return null;
			}
			if (active.GetComponent<AudioSource>())
			{
				GameObject tap = new GameObject("Listener (camera sound tap)");
				tap.layer = active.gameObject.layer;
				tap.transform.SetParent(active.transform, false);
				AudioVelocityUpdateMode mode = active.velocityUpdateMode;
				active.enabled = false;
				active = tap.AddComponent<AudioListener>();
				active.velocityUpdateMode = mode;
			}
			return active.gameObject.AddComponent<RippleCameraAudio>();
		}

		public void Begin(long originTicks, int rate)
		{
			// Rebuilt from native code (token 0x06000060, RVA 0x270E610)
			lock (gate)
			{
				micRead = 0;
				micWrite = 0;
				micCount = 0;
				inputRate = 0;
				resamplePhase = 0.0;
				read = 0;
				write = 0;
				count = 0;
				totalFrames = 0L;
				origin = originTicks;
				firstOffset = -1L;
				sampleRate = rate;
				limiter = default;
				Capturing = true;
			}
			micReader?.Dispose();
			micReader = new SharedMicrophone.Reader();
		}

		public void End()
		{
			// Rebuilt from native code (token 0x06000061, RVA 0x270E764)
			micReader?.Dispose();
			micReader = null;
			lock (gate)
			{
				Capturing = false;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000063, RVA 0x270E90C)
			ShallowsTuning tuning = ShallowsTuning.I;
			micVolume = tuning ? tuning.recordingMicVolume : 2f;
			ReadMicrophone();
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x06000064, RVA 0x270EBDC)
			End();
		}

		private void ReadMicrophone()
		{
			// Rebuilt from native code (token 0x06000065, RVA 0x270E9C8)
			if (micReader == null)
			{
				return;
			}
			int channels = micReader.Channels;
			int rate = micReader.SampleRate;
			if (channels < 1 || rate < 1)
			{
				return;
			}
			int frames;
			while ((frames = micReader.ReadSome(micChunk)) > 0)
			{
				lock (gate)
				{
					if (!Capturing)
					{
						return;
					}
					for (int i = 0; i < frames * channels; i += channels)
					{
						float sum = 0f;
						for (int c = 0; c < channels; c++)
						{
							sum += micChunk[i + c];
						}
						WriteMicrophone(sum / channels, rate);
					}
				}
			}
		}

		private void WriteMicrophone(float value, int rate)
		{
			// Rebuilt from native code (token 0x06000066, RVA 0x270EBE0)
			if (inputRate != rate)
			{
				inputRate = rate;
				resamplePhase = 0.0;
				previousMic = value;
			}
			resamplePhase += (double)sampleRate / rate;
			while (resamplePhase >= 1.0)
			{
				resamplePhase -= 1.0;
				if (micCount >= Math.Min(microphone.Length, sampleRate / 4))
				{
					micRead = (micRead + 1) % microphone.Length;
					micCount--;
				}
				float sample = Mathf.LerpUnclamped(previousMic, value, (float)(1.0 - resamplePhase / ((double)sampleRate / rate)));
				if (!float.IsFinite(sample))
				{
					sample = 0f;
				}
				microphone[micWrite] = sample;
				micWrite = (micWrite + 1) % microphone.Length;
				micCount++;
			}
			previousMic = value;
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			// Rebuilt from native code (token 0x06000067, RVA 0x270ED98)
			lock (gate)
			{
				if (channels < 1 || !Capturing)
				{
					return;
				}
				if (firstOffset < 0)
				{
					long now = Stopwatch.GetTimestamp();
					firstOffset = Math.Max(0L, (now - origin) * 1000000 / Stopwatch.Frequency - data.Length / channels * 1000000L / sampleRate);
				}
				for (int i = 0; i + channels <= data.Length; i += channels)
				{
					if (count + 2 > samples.Length)
					{
						count -= 2;
						read = (read + 2) % samples.Length;
						totalFrames++;
					}
					samples[write] = data[i];
					write = (write + 1) % samples.Length;
					samples[write] = data[i + (channels > 1 ? 1 : 0)];
					write = (write + 1) % samples.Length;
					count += 2;
				}
			}
		}

		public bool Drain(byte[] pcm, int rate, out long timestamp)
		{
			// Rebuilt from native code (token 0x06000068, RVA 0x270F058)
			lock (gate)
			{
				long offset = Math.Max(0L, firstOffset);
				timestamp = totalFrames * 1000000 / rate + offset;
				if (Capturing ? count < sampleRate * 240 / 1000 + 2048 : count == 0)
				{
					return false;
				}
				int available = Math.Min(count, 2048);
				for (int i = 0; i < 2048; i += 2)
				{
					float mic = 0f;
					float left = 0f;
					float right = 0f;
					if (i < available)
					{
						if (micCount > 0)
						{
							micCount--;
							mic = microphone[micRead];
							micRead = (micRead + 1) % microphone.Length;
						}
						left = samples[read] + mic * micVolume;
						read = (read + 1) % samples.Length;
					}
					if (i + 1 < available)
					{
						right = samples[read] + mic * micVolume;
						read = (read + 1) % samples.Length;
					}
					float gain = limiter.Gain(Math.Max(Math.Abs(left), Math.Abs(right)), sampleRate);
					for (int c = 0; c < 2; c++)
					{
						short value = (short)(Math.Clamp(gain * (c == 0 ? left : right), -1f, 1f) * 32767f);
						int at = (i + c) * 2;
						pcm[at] = (byte)value;
						pcm[at + 1] = (byte)(value >> 8);
					}
				}
				count -= available;
				totalFrames += 1024;
				return true;
			}
		}
	}
}
