using System;
using Photon.Voice;
using Photon.Voice.Unity;
using UnityEngine;

namespace Shallows
{
	public class MicLoudness : MonoBehaviour
	{
		private class Meter : IProcessor<float>, IDisposable, IProcessor<short>
		{
			public volatile float rms;

			public volatile int tick = int.MinValue / 2;

			public float[] Process(float[] buf)
			{
				// Rebuilt from native code (token 0x060005BB, RVA 0x27E3E1C)
				double sum = 0.0;
				for (int i = 0; i < buf.Length; i++)
				{
					sum += buf[i] * buf[i];
				}
				Store(sum, buf.Length);
				return buf;
			}

			public short[] Process(short[] buf)
			{
				// Rebuilt from native code (token 0x060005BC, RVA 0x27E3F20)
				double sum = 0.0;
				for (int i = 0; i < buf.Length; i++)
				{
					double v = buf[i] / 32768.0;
					sum += v * v;
				}
				Store(sum, buf.Length);
				return buf;
			}

			private void Store(double sum, int count)
			{
				// Rebuilt from native code (token 0x060005BD, RVA 0x27E3E7C)
				rms = count > 0 ? (float)Math.Sqrt(sum / count) : 0f;
				tick = Environment.TickCount;
			}

			public void Dispose()
			{
				// Rebuilt from native code (token 0x060005BE, RVA 0x27E3F80)
			}
		}

		private readonly Meter meter = new Meter();

		// Rebuilt from native code (token 0x060005B8, RVA 0x27E3B40)
		public float Loudness => Environment.TickCount - meter.tick < 150 ? meter.rms : 0f;

		private void PhotonVoiceCreated(PhotonVoiceCreatedParams p)
		{
			// Rebuilt from native code (token 0x060005B9, RVA 0x27E3BA4)
			if (p.Voice is LocalVoiceAudioFloat floatVoice)
			{
				floatVoice.AddPreProcessor(meter);
			}
			else if (p.Voice is LocalVoiceAudioShort shortVoice)
			{
				shortVoice.AddPreProcessor(meter);
			}
		}
	}
}
