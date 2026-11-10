using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace Shallows
{
	public class PerfProbe : MonoBehaviour
	{
		private class Stats
		{
			// Rebuilt from native code (token 0x0600027F, RVA 0x27883CC)
			private readonly List<float> frames = new List<float>();

			private readonly FrameTiming[] timing = new FrameTiming[1];

			private double main;

			private double render;

			private double gpu;

			private int timed;

			private double draws;

			private double batches;

			private double setPass;

			private double triangles;

			private double garbage;

			private ProbeCounter drawCounter;

			private ProbeCounter batchCounter;

			private ProbeCounter setPassCounter;

			private ProbeCounter triangleCounter;

			private ProbeCounter garbageCounter;

			private readonly List<(string name, Recorder recorder)> markers = new List<(string name, Recorder recorder)>();

			private readonly Dictionary<string, (double cpu, double gpu, double calls)> totals = new Dictionary<string, (double cpu, double gpu, double calls)>();

			private int deaths;

			private string players = "";

			public float AverageMs
			{
				get
				{
					// Rebuilt from native code (token 0x0600027A, RVA 0x27860D0)
					if (frames.Count < 1)
					{
						return 0f;
					}
					return frames.Average();
				}
			}

			public void Begin()
			{
				// Rebuilt from native code (token 0x0600027B, RVA 0x2786134)
				drawCounter = new ProbeCounter(ProfilerCategory.Render, "Draw Calls Count");
				batchCounter = new ProbeCounter(ProfilerCategory.Render, "Batches Count");
				setPassCounter = new ProbeCounter(ProfilerCategory.Render, "SetPass Calls Count");
				triangleCounter = new ProbeCounter(ProfilerCategory.Render, "Triangles Count");
				garbageCounter = new ProbeCounter(ProfilerCategory.Memory, "GC Allocated In Frame");
				List<ProfilerRecorderHandle> handles = new List<ProfilerRecorderHandle>();
				ProfilerRecorderHandle.GetAvailable(handles);
				HashSet<string> names = new HashSet<string>();
				foreach (ProfilerRecorderHandle handle in handles)
				{
					ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
					if (description.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds && !string.IsNullOrEmpty(description.Name))
					{
						names.Add(description.Name);
					}
				}
				foreach (string name in names)
				{
					Recorder recorder = Recorder.Get(name);
					if (recorder == null || !recorder.isValid)
					{
						continue;
					}
					recorder.enabled = true;
					markers.Add((name, recorder));
				}
			}

			public void Sample()
			{
				// Rebuilt from native code (token 0x0600027C, RVA 0x2786830)
				frames.Add(Time.unscaledDeltaTime * 1000f);
				FrameTimingManager.CaptureFrameTimings();
				if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].cpuFrameTime > 0.0)
				{
					main += timing[0].cpuMainThreadFrameTime;
					render += timing[0].cpuRenderThreadFrameTime;
					gpu += timing[0].gpuFrameTime;
					timed++;
				}
				draws += drawCounter.Value;
				batches += batchCounter.Value;
				setPass += setPassCounter.Value;
				triangles += triangleCounter.Value;
				garbage += garbageCounter.Value;
				foreach (var (name, recorder) in markers)
				{
					long cpuNs = recorder.elapsedNanoseconds;
					long gpuNs = recorder.gpuElapsedNanoseconds;
					if (cpuNs == 0 && gpuNs == 0)
					{
						continue;
					}
					totals.TryGetValue(name, out var total);
					totals[name] = (total.cpu + cpuNs, total.gpu + gpuNs, total.calls + recorder.sampleBlockCount);
				}
				if (GameManager.I && !GameManager.I.Playing)
				{
					deaths++;
				}
				if (NetworkManager.I)
				{
					players = string.Format("{0}{1}", NetworkManager.I.PlayerCount, NetworkManager.I.Connected ? "" : " offline");
				}
			}

			public string Report(string name, float hz)
			{
				// Rebuilt from native code (token 0x0600027D, RVA 0x2786F68)
				int n = Mathf.Max(frames.Count, 1);
				List<float> sorted = frames.OrderBy(f => f).ToList();
				float p95 = sorted.Count > 0 ? sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.95f))] : 0f;
				float budget = 1000f / hz;
				int slow = frames.Count(f => f > budget * 1.4f);
				StringBuilder text = new StringBuilder();
				text.Append(string.Format("=== {0} | {1} frames, {2:0.0} fps of {3:0} | avg {4:0.0} ms p95 {5:0.0} | ", name, frames.Count, frames.Count / Measure, hz, AverageMs, p95) + string.Format("slow frames {0:0}% | ", slow * 100f / n));
				if (timed > 0)
				{
					text.Append(string.Format("cpu main {0:0.0} render {1:0.0} gpu {2:0.0} ms | ", main / timed, render / timed, gpu / timed));
				}
				text.Append(string.Format("draws {0:0} batches {1:0} setpass {2:0} tris {3:0}k | gc {4:0.0} KB/frame | players {5}", draws / n, batches / n, setPass / n, triangles / n / 1000.0, garbage / n / 1024.0, players));
				if (deaths > 0)
				{
					text.Append(string.Format(" | NOT PLAYING for {0} frames (caught?)", deaths));
				}
				foreach (KeyValuePair<string, (double cpu, double gpu, double calls)> total in totals.OrderByDescending(t => t.Value.cpu).Take(30))
				{
					double cpuMs = total.Value.cpu / n / 1000000.0;
					double gpuMs = total.Value.gpu / n / 1000000.0;
					if (cpuMs < 0.08 && gpuMs < 0.08)
					{
						break;
					}
					text.Append(string.Format("\n    {0,6:0.00} ms{1}  x{2:0.#}  {3}", cpuMs, gpuMs > 0.01 ? string.Format(" (gpu {0:0.00})", gpuMs) : "", total.Value.calls / n, total.Key));
				}
				foreach (KeyValuePair<string, (double cpu, double gpu, double calls)> total in totals.Where(t => t.Value.gpu / n / 1000000.0 > 0.1).OrderByDescending(t => t.Value.gpu).Take(12))
				{
					text.Append(string.Format("\n    gpu {0,6:0.00} ms  {1}", total.Value.gpu / n / 1000000.0, total.Key));
				}
				return text.ToString();
			}

			public void End()
			{
				// Rebuilt from native code (token 0x0600027E, RVA 0x2788214)
				foreach (var (_, recorder) in markers)
				{
					recorder.enabled = false;
				}
				markers.Clear();
				drawCounter.Dispose();
				batchCounter.Dispose();
				setPassCounter.Dispose();
				triangleCounter.Dispose();
				garbageCounter.Dispose();
			}
		}

		private struct ProbeCounter
		{
			private ProfilerRecorder recorder;

			// Rebuilt from native code (token 0x06000289, RVA 0x2786F38)
			public long Value => recorder.Valid ? recorder.LastValue : 0L;

			public ProbeCounter(ProfilerCategory category, string name)
			{
				// Rebuilt from native code (token 0x06000288, RVA 0x2786804)
				recorder = ProfilerRecorder.StartNew(category, name);
			}

			public void Dispose()
			{
				// Rebuilt from native code (token 0x0600028A, RVA 0x27883C4)
				recorder.Dispose();
			}
		}

		private const float Settle = 4f;

		private const float Measure = 8f;

		// Rebuilt from native code (token 0x06000279, RVA 0x2785F4C)
		private static readonly bool FullRun = false;

		private static readonly float[] FoveationLevels = { 0f, 0.33f, 0.66f, 1f };

		private static readonly int ProbeFlatId = Shader.PropertyToID("_ShallowsProbeFlat");

		private string results;

		private static readonly List<Renderer> switchedOff = new List<Renderer>();

		private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x06000268, RVA 0x27850DC)
			if (!Debug.isDebugBuild || !File.Exists(Path.Combine(Application.persistentDataPath, "perfprobe")))
			{
				return;
			}
			GameObject go = new GameObject("Perf probe");
			DontDestroyOnLoad(go);
			go.AddComponent<PerfProbe>();
		}

		private void Start()
		{
			// Rebuilt from native code (token 0x06000269, RVA 0x278525C)
			results = Path.Combine(Application.persistentDataPath, "perf-results.txt");
			File.WriteAllText(results, "");
			StartCoroutine(Run());
		}

		private void Log(string line)
		{
			// Rebuilt from native code (token 0x0600026A, RVA 0x27853C4)
			Debug.Log("[ShallowsPerf] " + line);
			File.AppendAllText(results, line + "\n");
		}

		private IEnumerator Run()
		{
			// Rebuilt from native code (token 0x0600026B, RVA 0x2785358; state machine 0x060002C9, RVA 0x278B31C)
			float start = Time.realtimeSinceStartup;
			while (!GameManager.I || !GameManager.I.Playing || Time.realtimeSinceStartup - start < 20f)
			{
				yield return null;
			}
			Log(string.Format("device {0} | gpu {1} {2} | refresh {3:0} Hz | ", SystemInfo.deviceModel, SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType, RefreshRate())
				+ string.Format("eye scale {0:0.00} | eye texture {1}x{2} | ", XRSettings.eyeTextureResolutionScale, XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight)
				+ string.Format("msaa {0} | levels {1} | ", Pipeline()?.msaaSampleCount, GameManager.I.levels?.Length)
				+ string.Format("foveation {0:0.00} (display says {1:0.00})", QuestFoveation.Level, QuestFoveation.Apply()));
			yield return Scenario("spawn", -1, null);
			yield return Scenario("spawn | plain tile shader", -1, Flat(true), Flat(false));
			yield return Foveation("spawn", -1);
			yield return Scenario("spawn | no mirror", -1, Mirrors(false), Mirrors(true));
			if (FullRun)
			{
				yield return Scenario("spawn | camera preview 30fps", -1, () => RippleCamera.ProbeRender = 1, () => RippleCamera.ProbeRender = 0);
			}
			yield return Scenario("spawn | camera 1080p60 picture", -1, () => RippleCamera.ProbeRender = 2, () => RippleCamera.ProbeRender = 0);
			yield return Recording("spawn | camera RECORDING 1080p60", -1);
			if (FullRun)
			{
				yield return Scenario("spawn | camera 1080p60 picture, no mirror", -1, () =>
				{
					RippleCamera.ProbeRender = 2;
					Mirrors(false)();
				}, () =>
				{
					RippleCamera.ProbeRender = 0;
					Mirrors(true)();
				});
				yield return Ablations("spawn", -1);
			}
			(int level, float ms) worst = (-1, 0f);
			int count = GameManager.I.levels != null ? GameManager.I.levels.Length : 0;
			for (int i = 0; i < count; i++)
			{
				Stats stats = new Stats();
				yield return Scenario(string.Format("L{0} {1}", i + 1, GameManager.I.levels[i].name), i, null, null, stats);
				if (stats.AverageMs > worst.ms)
				{
					worst = (i, stats.AverageMs);
				}
				if (FullRun)
				{
					yield return Scenario(string.Format("L{0} | camera 1080p60 picture", i + 1), i, () => RippleCamera.ProbeRender = 2, () => RippleCamera.ProbeRender = 0);
				}
			}
			if (worst.level >= 0)
			{
				string place = string.Format("L{0} (slowest)", worst.level + 1);
				yield return Scenario(place + " | plain tile shader", worst.level, Flat(true), Flat(false));
				yield return Foveation(place, worst.level);
				yield return Recording(place + " | camera RECORDING 1080p60", worst.level);
				if (FullRun)
				{
					yield return Ablations(place, worst.level);
				}
			}
			GameManager.I.GoToLevel(-1);
			Log("DONE");
			try
			{
				File.Delete(Path.Combine(Application.persistentDataPath, "perfprobe"));
			}
			catch
			{
			}
		}

		private IEnumerator Ablations(string place, int level)
		{
			// Rebuilt from native code (token 0x0600026C, RVA 0x27854B4; state machine 0x060002B7, RVA 0x2789C20)
			yield return Scenario(place + " | no fog backdrop", level, Objects("Fog Backdrop", false), Objects("Fog Backdrop", true));
			yield return Scenario(place + " | no water", level, Renderers(s => s.Contains("Water"), false), Renderers(s => s.Contains("Water"), true));
			yield return Scenario(place + " | no props", level, Renderers(s => s.Contains("ShallowsProp"), false), Renderers(s => s.Contains("ShallowsProp"), true));
			yield return Scenario(place + " | no tiles", level, Renderers(s => s.Contains("Tile"), false), Renderers(s => s.Contains("Tile"), true));
			yield return Scenario(place + " | no skinned meshes (players, monsters)", level, Skinned(false), Skinned(true));
			int msaa = Pipeline() ? Pipeline().msaaSampleCount : 1;
			yield return Scenario(place + " | msaa off", level, () =>
			{
				if (Pipeline())
				{
					Pipeline().msaaSampleCount = 1;
				}
			}, () =>
			{
				if (Pipeline())
				{
					Pipeline().msaaSampleCount = msaa;
				}
			});
			bool depth = Pipeline() && Pipeline().supportsCameraDepthTexture;
			bool opaque = Pipeline() && Pipeline().supportsCameraOpaqueTexture;
			yield return Scenario(place + " | no depth/opaque texture", level, () =>
			{
				if (Pipeline())
				{
					Pipeline().supportsCameraDepthTexture = false;
					Pipeline().supportsCameraOpaqueTexture = false;
				}
			}, () =>
			{
				if (Pipeline())
				{
					Pipeline().supportsCameraDepthTexture = depth;
					Pipeline().supportsCameraOpaqueTexture = opaque;
				}
			});
			UniversalAdditionalCameraData head = HeadData();
			bool post = head && head.renderPostProcessing;
			yield return Scenario(place + " | no post processing", level, () =>
			{
				if (head)
				{
					head.renderPostProcessing = false;
				}
			}, () =>
			{
				if (head)
				{
					head.renderPostProcessing = post;
				}
			});
			float scale = XRSettings.eyeTextureResolutionScale;
			yield return Scenario(place + " | eye resolution 70%", level, () => XRSettings.eyeTextureResolutionScale = 0.7f, () => XRSettings.eyeTextureResolutionScale = scale);
		}

		private IEnumerator Foveation(string place, int level)
		{
			// Rebuilt from native code (token 0x0600026D, RVA 0x278556C; state machine 0x060002BD, RVA 0x278AB3C)
			float normal = QuestFoveation.Level;
			foreach (float f in FoveationLevels)
			{
				yield return Scenario(string.Format("{0} | foveation {1:0.00}", place, f), level, () =>
				{
					QuestFoveation.Level = f;
					Log(string.Format("  display foveation {0:0.00}", QuestFoveation.Apply()));
				}, () =>
				{
					QuestFoveation.Level = normal;
					QuestFoveation.Apply();
				});
			}
		}

		private IEnumerator Recording(string name, int level)
		{
			// Rebuilt from native code (token 0x0600026E, RVA 0x2785624; state machine 0x060002C3, RVA 0x278AE94)
			RippleCamera camera = RippleCamera.Local;
			if (!camera)
			{
				Log(name + ": no camera");
				yield break;
			}
			yield return Scenario(name, level, () => camera.ToggleRecording(), () =>
			{
				if (camera && camera.Recorder && camera.Recorder.CaptureActive)
				{
					camera.ToggleRecording();
				}
			});
			float until = Time.realtimeSinceStartup + 20f;
			while (camera && camera.Recorder && camera.Recorder.Busy && Time.realtimeSinceStartup < until)
			{
				yield return null;
			}
			if (camera && camera.Recorder)
			{
				Log(string.Format("  recorder: {0}, dropped frames {1}, {2}", camera.Recorder.StatusText, camera.Recorder.DroppedFrames, camera.Recorder.Detail));
			}
		}

		private IEnumerator Scenario(string name, int level, Action begin, Action end = null, Stats stats = null)
		{
			// Rebuilt from native code (token 0x0600026F, RVA 0x27856DC; state machine 0x060002CF, RVA 0x278C78C)
			GameManager.I.GoToLevel(level);
			yield return new WaitForSecondsRealtime(1f);
			if (begin != null)
			{
				try
				{
					begin();
				}
				catch (Exception e)
				{
					Log(name + ": couldn't set up: " + e.Message);
				}
			}
			yield return new WaitForSecondsRealtime(Settle);
			if (stats == null)
			{
				stats = new Stats();
			}
			Debug.Log("[ShallowsPerf] --- measuring " + name);
			try
			{
				stats.Begin();
			}
			catch (Exception e)
			{
				Log(name + ": couldn't start measuring: " + e.Message);
			}
			float until = Time.realtimeSinceStartup + Measure;
			while (Time.realtimeSinceStartup < until)
			{
				yield return null;
				stats.Sample();
			}
			Debug.Log("[ShallowsPerf] --- done " + name);
			if (end != null)
			{
				try
				{
					end();
				}
				catch (Exception e)
				{
					Log(name + ": couldn't undo: " + e.Message);
				}
			}
			Log(stats.Report(name, RefreshRate()));
			stats.End();
		}

		private static Action Flat(bool on)
		{
			// Rebuilt from native code (token 0x06000270, RVA 0x27857E0)
			return () => Shader.SetGlobalFloat(ProbeFlatId, on ? 1f : 0f);
		}

		private static Action Mirrors(bool on)
		{
			// Rebuilt from native code (token 0x06000271, RVA 0x2785894)
			return () =>
			{
				foreach (Mirror mirror in FindObjectsByType<Mirror>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					mirror.gameObject.SetActive(on);
				}
			};
		}

		private static Action Objects(string objectName, bool on)
		{
			// Rebuilt from native code (token 0x06000272, RVA 0x2785948)
			return () =>
			{
				foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					if (t.name == objectName)
					{
						t.gameObject.SetActive(on);
					}
				}
			};
		}

		private static Action Renderers(Func<string, bool> shader, bool on)
		{
			// Rebuilt from native code (token 0x06000273, RVA 0x2785A18)
			return () =>
			{
				if (on)
				{
					foreach (Renderer renderer in switchedOff)
					{
						if (renderer)
						{
							renderer.enabled = true;
						}
					}
					switchedOff.Clear();
					return;
				}
				foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
				{
					Material material = renderer.sharedMaterial;
					if (renderer.enabled && material && material.shader && shader(material.shader.name))
					{
						renderer.enabled = false;
						switchedOff.Add(renderer);
					}
				}
			};
		}

		private static Action Skinned(bool on)
		{
			// Rebuilt from native code (token 0x06000274, RVA 0x2785AE8)
			return () =>
			{
				if (on)
				{
					foreach (Renderer renderer in switchedOff)
					{
						if (renderer)
						{
							renderer.enabled = true;
						}
					}
					switchedOff.Clear();
					return;
				}
				foreach (SkinnedMeshRenderer renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
				{
					if (renderer.enabled)
					{
						renderer.enabled = false;
						switchedOff.Add(renderer);
					}
				}
			};
		}

		private static UniversalRenderPipelineAsset Pipeline()
		{
			// Rebuilt from native code (token 0x06000275, RVA 0x2785B9C)
			return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
		}

		private static UniversalAdditionalCameraData HeadData()
		{
			// Rebuilt from native code (token 0x06000276, RVA 0x2785C3C)
			PlayerRig rig = PlayerRig.I;
			if (!rig || !rig.head)
			{
				return null;
			}
			return rig.head.TryGetComponent(out UniversalAdditionalCameraData data) ? data : null;
		}

		private static float RefreshRate()
		{
			// Rebuilt from native code (token 0x06000277, RVA 0x2785D40)
			SubsystemManager.GetSubsystems(displays);
			foreach (XRDisplaySubsystem display in displays)
			{
				if (display.running && display.TryGetDisplayRefreshRate(out float rate) && rate > 1f)
				{
					return rate;
				}
			}
			return 72f;
		}
	}
}
