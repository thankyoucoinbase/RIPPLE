using System;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Shallows
{
	public class RippleCameraRecorder : MonoBehaviour
	{
		public enum Status
		{
			Ready = 0,
			Starting = 1,
			Recording = 2,
			Saving = 3,
			Saved = 4,
			Error = 5,
			Unavailable = 6
		}

		public const int Width = 1920;

		public const int Height = 1080;

		public const int FramesPerSecond = 60;

		private const string JavaClass = "com.savastudios.ripple.recording.GalleryRecorder";

		public Status State { get; private set; }

		public string Detail { get; private set; } = "";

		public bool Busy
		{
			get
			{
				// Rebuilt from native code (token 0x0600007D, RVA 0x2710670)
				return sessionAllocated || State == Status.Starting || State == Status.Recording || State == Status.Saving;
			}
		}

		public bool CaptureActive
		{
			get
			{
				// Rebuilt from native code (token 0x0600007E, RVA 0x2704DF4)
				return State == Status.Recording;
			}
		}

		public RippleCamera Owner { get; private set; }

		public int DroppedFrames { get; private set; }

		public float Seconds
		{
			get
			{
				// Rebuilt from native code (token 0x06000083, RVA 0x27106B4)
				return (float)(Time.realtimeSinceStartupAsDouble - startTime);
			}
		}

		public string ButtonText
		{
			get
			{
				// Rebuilt from native code (token 0x06000084, RVA 0x270D514)
				switch (State)
				{
				case Status.Starting:
					return "STARTING";
				case Status.Recording:
					return "STOP";
				case Status.Saving:
					return "SAVING...";
				default:
					return "RECORD";
				}
			}
		}

		public string StatusText
		{
			get
			{
				// Rebuilt from native code (token 0x06000085, RVA 0x270D59C)
				switch (State)
				{
				case Status.Ready:
					return "QUEST VIDEO";
				case Status.Starting:
					return "STARTING...";
				case Status.Recording:
					return string.Format("REC {0:00}:{1:00}", Mathf.FloorToInt(Seconds) / 60, Mathf.FloorToInt(Seconds) % 60);
				case Status.Saving:
					return "SAVING VIDEO";
				case Status.Saved:
					return "SAVED TO GALLERY";
				case Status.Unavailable:
					return "OBS ON PC";
				default:
					return "RECORDING FAILED";
				}
			}
		}

		private Material pack;

		private RenderTexture packed;

		private const int Slots = 6;

		private readonly NativeArray<byte>[] buffers = new NativeArray<byte>[Slots];

		private readonly bool[] reading = new bool[Slots];

		private RippleCameraAudio audioTap;

		private readonly byte[] pcm = new byte[4096];

		private int sampleRate;

		private int pendingReads;

		private int consecutiveErrors;

		private double startTime;

		private double startingAt;

		private bool stopSent;

		private bool sessionAllocated;

		private AndroidJavaObject encoder;

		public static RippleCameraRecorder Create(RippleCamera owner)
		{
			// Rebuilt from native code (token 0x06000086, RVA 0x270C830)
			RippleCameraRecorder recorder = new GameObject("Ripple camera recorder").AddComponent<RippleCameraRecorder>();
			DontDestroyOnLoad(recorder.gameObject);
			recorder.Owner = owner;
			return recorder;
		}

		public bool Toggle()
		{
			// Rebuilt from native code (token 0x06000087, RVA 0x270C928)
			if (State == Status.Recording)
			{
				Stop();
				return true;
			}
			if (Busy)
			{
				return false;
			}
			if (Application.platform != RuntimePlatform.Android)
			{
				Detail = "Videos save on Quest. On PC, record the game window with OBS (F8 swaps the window back).";
				State = Status.Unavailable;
				return false;
			}
			if (!SystemInfo.supportsAsyncGPUReadback)
			{
				Fail("GPU readback is unavailable on this device.");
				return false;
			}
			Detail = "";
			State = Status.Starting;
			try
			{
				startingAt = Time.realtimeSinceStartupAsDouble;
				stopSent = false;
				sessionAllocated = true;
				consecutiveErrors = 0;
				DroppedFrames = 0;
				pack = new Material(Resources.Load<Shader>("RippleCamera/PackYuv"));
				pack.SetFloat("_Width", Width);
				pack.SetFloat("_Height", Height);
				packed = new RenderTexture(Width / 4, Height * 3 / 2, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
				{
					name = "Ripple camera YUV",
					filterMode = FilterMode.Point
				};
				packed.Create();
				using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
				{
					using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
					{
						encoder = new AndroidJavaObject(JavaClass, activity);
					}
				}
				for (int i = 0; i < buffers.Length; i++)
				{
					buffers[i] = new NativeArray<byte>(Width * Height * 3 / 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					IntPtr buffer = AndroidJNI.NewDirectByteBuffer(buffers[i]);
					using (var javaBuffer = new AndroidJavaObject(buffer))
					{
						encoder.Call("setBuffer", i, javaBuffer);
					}
					AndroidJNI.DeleteLocalRef(buffer);
				}
				sampleRate = AudioSettings.outputSampleRate;
				encoder.Call("start", Width, Height, sampleRate, FramesPerSecond);
			}
			catch (Exception exception)
			{
				Fail(exception.Message);
				Cleanup();
				return false;
			}
			return true;
		}

		public void Stop()
		{
			// Rebuilt from native code (token 0x06000088, RVA 0x2708D14)
			if (!Busy || State == Status.Saving)
			{
				return;
			}
			State = Status.Saving;
			if (audioTap)
			{
				audioTap.End();
			}
			DrainAudio(96);
			SendStopIfReady();
		}

		private void DrainAudio(int maximum)
		{
			// Rebuilt from native code (token 0x06000089, RVA 0x2710978)
			if (encoder == null || !audioTap)
			{
				return;
			}
			for (int i = 0; i < maximum; i++)
			{
				if (!audioTap.Drain(pcm, sampleRate, out long timestamp))
				{
					break;
				}
				encoder.Call<bool>("sound", pcm, timestamp);
			}
		}

		private void SendStopIfReady()
		{
			// Rebuilt from native code (token 0x0600008A, RVA 0x2710B40)
			if (encoder == null || (State != Status.Error && State != Status.Saving) || pendingReads != 0 || stopSent)
			{
				return;
			}
			if (State != Status.Error && audioTap && audioTap.HasPendingAudio)
			{
				return;
			}
			encoder.Call("stop");
			stopSent = true;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600008B, RVA 0x2710C8C)
			if (!Owner && Busy)
			{
				Stop();
			}
			if (encoder != null)
			{
				try
				{
					int state = encoder.Call<int>("getState");
					if (state == 1 && State == Status.Starting)
					{
						State = Status.Recording;
						startTime = Time.realtimeSinceStartupAsDouble;
						audioTap = RippleCameraAudio.Attach();
						if (audioTap)
						{
							audioTap.Begin(Stopwatch.GetTimestamp(), sampleRate);
						}
						else
						{
							Fail("No AudioListener to record the sound from.");
						}
					}
					if (State == Status.Starting && Time.realtimeSinceStartupAsDouble - startingAt > 10.0)
					{
						Fail("The recorder didn't start.");
					}
					if (State == Status.Recording || State == Status.Saving)
					{
						DrainAudio(8);
					}
					SendStopIfReady();
					if (state >= 3 && pendingReads == 0)
					{
						Detail = encoder.Call<string>("getResult");
						State = state == 3 ? Status.Saved : Status.Error;
						if (state != 3)
						{
							Debug.LogError("Shallows camera recording: " + Detail);
						}
						Cleanup();
					}
				}
				catch (Exception exception)
				{
					Fail(exception.Message);
				}
			}
			if (Owner)
			{
				Owner.UpdateRecordingLabels();
			}
			else if (!Busy)
			{
				Destroy(gameObject);
			}
		}

		public void Capture(RenderTexture source)
		{
			// Rebuilt from native code (token 0x0600008C, RVA 0x270DBB4)
			int index;
			long time;
			if (State != Status.Recording || encoder == null || !source)
			{
				return;
			}
			double now = Time.realtimeSinceStartupAsDouble;
			try
			{
				int free = encoder.Call<int>("freeMask");
				for (int i = 0; i < Slots; i++)
				{
					if (reading[i] || ((free >> i) & 1) == 0)
					{
						continue;
					}
					Graphics.Blit(source, packed, pack);
					time = (long)((now - startTime) * 1000000.0);
					reading[i] = true;
					index = i;
					pendingReads++;
					AsyncGPUReadback.RequestIntoNativeArray(ref buffers[i], packed, 0, TextureFormat.RGBA32, request =>
					{
						// Rebuilt from native code (token 0x06000094, RVA 0x271144C)
						reading[index] = false;
						pendingReads--;
						SendStopIfReady();
						if (request.hasError)
						{
							if (++consecutiveErrors >= 3)
							{
								Fail("The GPU couldn't read the camera's frames.");
							}
							return;
						}
						consecutiveErrors = 0;
						if (State != Status.Recording || encoder == null)
						{
							return;
						}
						try
						{
							if (!encoder.Call<bool>("submit", index, time))
							{
								DroppedFrames++;
							}
						}
						catch (Exception exception)
						{
							Fail(exception.Message);
						}
					});
					return;
				}
				DroppedFrames++;
			}
			catch (Exception exception)
			{
				Fail(exception.Message);
			}
		}

		private void Fail(string message)
		{
			// Rebuilt from native code (token 0x0600008D, RVA 0x27106D8)
			Detail = message;
			State = Status.Error;
			if (audioTap)
			{
				audioTap.End();
			}
			Debug.LogError("Shallows camera recording: " + message);
		}

		private void Cleanup()
		{
			// Rebuilt from native code (token 0x0600008E, RVA 0x27107CC)
			sessionAllocated = false;
			if (audioTap)
			{
				audioTap.End();
			}
			encoder?.Dispose();
			encoder = null;
			for (int i = 0; i < Slots; i++)
			{
				if (buffers[i].IsCreated)
				{
					buffers[i].Dispose();
				}
			}
			if (packed)
			{
				packed.Release();
				Destroy(packed);
			}
			if (pack)
			{
				Destroy(pack);
			}
		}

		private void OnApplicationPause(bool paused)
		{
			// Rebuilt from native code (token 0x0600008F, RVA 0x27111AC)
			if (paused)
			{
				Stop();
			}
		}

		private void OnApplicationQuit()
		{
			// Rebuilt from native code (token 0x06000090, RVA 0x27111B8)
			Stop();
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000091, RVA 0x27111BC)
			if (pendingReads > 0)
			{
				AsyncGPUReadback.WaitAllRequests();
			}
			if (encoder != null)
			{
				encoder.Call("awaitStop");
				if (encoder.Call<int>("getState") < 3)
				{
					return;
				}
			}
			Cleanup();
		}
	}
}
