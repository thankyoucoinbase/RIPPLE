using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Shallows
{
	[DefaultExecutionOrder(-100)]
	public class PhysicsClock : MonoBehaviour
	{
		public static event Action<float> Step;

		public static bool LockToDisplay = true;

		private const float LongestStep = 1f / 45f;

		private const int MostFramesBehind = 4;

		private static PhysicsClock instance;

		private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

		private float frameTime;

		private float nextRateCheck;

		private double behind;

		private int measuredFrames;

		private int doubleSteps;

		private int longerSteps;

		private int rawCount;

		private float rawMin = float.MaxValue;

		private float rawMax;

		private float nextReport;

		private double rawSum;

		private double rawSquares;

		private double wobbleSum;

		public static float DeltaTime { get; private set; } = 0.013888889f;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060002D7, RVA 0x278CF5C)
			Step = null;
			instance = null;
			DeltaTime = 0.013888889f;
			LockToDisplay = true;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Begin()
		{
			// Rebuilt from native code (token 0x060002D8, RVA 0x278D024)
			if (!instance)
			{
				GameObject go = new GameObject("Physics Clock");
				go.hideFlags = HideFlags.HideInHierarchy;
				DontDestroyOnLoad(go);
				instance = go.AddComponent<PhysicsClock>();
			}
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060002D9, RVA 0x278D17C)
			Physics.simulationMode = SimulationMode.Script;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060002DA, RVA 0x278D1D0)
			if (instance == this)
			{
				Physics.simulationMode = SimulationMode.FixedUpdate;
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060002DB, RVA 0x278D2A0)
			float raw = Time.deltaTime;
			if (raw <= 0f)
			{
				return;
			}
			float dt = DisplayFrames(raw);
			DeltaTime = dt;
			if (Debug.isDebugBuild)
			{
				Measure(raw, dt);
			}
			int steps = Mathf.Max(Mathf.CeilToInt(dt / LongestStep), 1);
			float step = dt / steps;
			for (int i = 0; i < steps; i++)
			{
				Step?.Invoke(step);
				Physics.Simulate(step);
			}
		}

		private float DisplayFrames(float raw)
		{
			// Rebuilt from native code (token 0x060002DC, RVA 0x278D470)
			if (Time.unscaledTime >= nextRateCheck)
			{
				nextRateCheck = Time.unscaledTime + 1f;
				frameTime = HeadsetFrameTime();
			}
			if (!LockToDisplay || frameTime <= 0f || Time.timeScale != 1f)
			{
				behind = 0.0;
				return raw;
			}
			behind += raw;
			int frames = Mathf.RoundToInt((float)(behind / frameTime));
			if (frames > MostFramesBehind)
			{
				behind = 0.0;
				return raw;
			}
			float dt = frameTime * Mathf.Max(frames, 1);
			behind -= dt;
			if (behind < -frameTime)
			{
				behind = -frameTime;
			}
			return dt;
		}

		private static float HeadsetFrameTime()
		{
			// Rebuilt from native code (token 0x060002DD, RVA 0x278DC00)
			SubsystemManager.GetSubsystems(displays);
			foreach (XRDisplaySubsystem display in displays)
			{
				if (display.running && display.TryGetDisplayRefreshRate(out float rate) && rate >= 30f && rate <= 500f)
				{
					return 1f / rate;
				}
			}
			return 0f;
		}

		private void Measure(float raw, float dt)
		{
			// Rebuilt from native code (token 0x060002DE, RVA 0x278D674)
			rawCount++;
			measuredFrames++;
			rawSum += raw;
			rawSquares += (double)raw * raw;
			rawMin = Mathf.Min(rawMin, raw);
			rawMax = Mathf.Max(rawMax, raw);
			wobbleSum += Mathf.Abs(raw - dt);
			if (frameTime > 0f)
			{
				int frames = Mathf.RoundToInt(dt / frameTime);
				if (frames == 2)
				{
					doubleSteps++;
				}
				else if (frames >= 3)
				{
					longerSteps++;
				}
			}
			if (Time.unscaledTime < nextReport)
			{
				return;
			}
			nextReport = Time.unscaledTime + 5f;
			double mean = rawSum / rawCount;
			double sd = Math.Sqrt(Math.Max(0.0, rawSquares / rawCount - mean * mean));
			Debug.Log(string.Format("[ShallowsClock] frames {0} display {1:F2} ms locked {2} | ", measuredFrames, frameTime * 1000f, LockToDisplay)
				+ string.Format("Time.deltaTime mean {0:F2} sd {1:F2} min {2:F2} max {3:F2} ms, ", mean * 1000.0, sd * 1000.0, rawMin * 1000f, rawMax * 1000f)
				+ string.Format("off from locked by {0:F2} ms on average | 2-frame steps {1}, longer {2}", wobbleSum / rawCount * 1000.0, doubleSteps, longerSteps));
			measuredFrames = 0;
			doubleSteps = 0;
			longerSteps = 0;
			rawCount = 0;
			rawSum = 0.0;
			rawSquares = 0.0;
			wobbleSum = 0.0;
			rawMin = float.MaxValue;
			rawMax = 0f;
		}
	}
}
