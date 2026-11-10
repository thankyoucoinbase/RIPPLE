using System;
using Photon.Voice;

namespace Shallows
{
	internal class SharedMicrophoneSource : IAudioReader<float>, IDataReader<float>, IDisposable, IAudioDesc
	{
		private readonly SharedMicrophone.Reader reader = new SharedMicrophone.Reader();

		public int SamplingRate
		{
			get
			{
				// Rebuilt from native code (token 0x060006EC, RVA 0x28066AC)
				return SharedMicrophone.SampleRate;
			}
		}

		public int Channels
		{
			get
			{
				// Rebuilt from native code (token 0x060006ED, RVA 0x28066C0)
				return SharedMicrophone.Channels;
			}
		}

		public string Error
		{
			get
			{
				// Rebuilt from native code (token 0x060006EE, RVA 0x28066D4)
				return SharedMicrophone.HasDevice ? null : "No microphone.";
			}
		}

		public bool Read(float[] buffer)
		{
			// Rebuilt from native code (token 0x060006EF, RVA 0x2806720)
			return reader.Read(buffer);
		}

		public void Dispose()
		{
			// Rebuilt from native code (token 0x060006F0, RVA 0x2806734)
			reader.Dispose();
		}
	}
}
