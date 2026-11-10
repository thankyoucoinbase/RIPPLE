using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Shallows
{
	[DefaultExecutionOrder(1250)]
	public class RippleCameraDesktop : MonoBehaviour
	{
		public bool OutputEnabled { get; private set; } = true;

		private RippleCamera owner;

		private Material material;

		private bool backgroundSaved;

		private bool previousBackground;

		private bool showing;

		private float nextDisplayCheck;

		private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

		private readonly Dictionary<XRDisplaySubsystem, int> originalMirrors = new Dictionary<XRDisplaySubsystem, int>();

		public bool Active
		{
			get
			{
				// Rebuilt from native code (token 0x0600006C, RVA 0x2704CD8)
				if (!OutputEnabled)
				{
					return false;
				}
				if (!owner || !owner.isActiveAndEnabled)
				{
					return false;
				}
				return HeadsetRunning();
			}
		}

		public void Bind(RippleCamera camera)
		{
			// Rebuilt from native code (token 0x0600006D, RVA 0x270F778)
			owner = camera;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600006E, RVA 0x270F780)
			nextDisplayCheck = 0f;
			RenderPipelineManager.endContextRendering += PresentDesktop;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600006F, RVA 0x270F804)
			RenderPipelineManager.endContextRendering -= PresentDesktop;
			StopOutput();
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x06000070, RVA 0x270FB1C)
			if (material)
			{
				Destroy(material);
			}
		}

		private bool HeadsetRunning()
		{
			// Rebuilt from native code (token 0x06000071, RVA 0x270F5F8)
			SubsystemManager.GetSubsystems(displays);
			foreach (XRDisplaySubsystem display in displays)
			{
				if (display.running)
				{
					return true;
				}
			}
			return false;
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x06000072, RVA 0x270FBA8)
			if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
			{
				nextDisplayCheck = 0f;
				OutputEnabled = !OutputEnabled;
			}
			if (!Active)
			{
				if (showing)
				{
					StopOutput();
				}
				return;
			}
			if (Time.unscaledTime >= nextDisplayCheck)
			{
				StartOutput();
				nextDisplayCheck = Time.unscaledTime + 1f;
			}
		}

		private void StartOutput()
		{
			// Rebuilt from native code (token 0x06000073, RVA 0x270FC7C)
			showing = true;
			if (!backgroundSaved)
			{
				previousBackground = Application.runInBackground;
				backgroundSaved = true;
				Application.runInBackground = true;
			}
			SubsystemManager.GetSubsystems(displays);
			foreach (XRDisplaySubsystem display in displays)
			{
				if (!display.running)
				{
					continue;
				}
				if (!originalMirrors.ContainsKey(display))
				{
					originalMirrors.Add(display, display.GetPreferredMirrorBlitMode());
				}
				display.SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.None);
			}
			if (material)
			{
				return;
			}
			Shader shader = Resources.Load<Shader>("RippleCamera/DesktopOutput");
			if (shader)
			{
				material = new Material(shader) { name = "Ripple camera on the desktop" };
			}
		}

		private void StopOutput()
		{
			// Rebuilt from native code (token 0x06000074, RVA 0x270F88C)
			showing = false;
			SubsystemManager.GetSubsystems(displays);
			foreach (KeyValuePair<XRDisplaySubsystem, int> pair in originalMirrors)
			{
				XRDisplaySubsystem display = pair.Key;
				if (displays.Contains(display) && display.running && display.GetPreferredMirrorBlitMode() == XRMirrorViewBlitMode.None)
				{
					display.SetPreferredMirrorBlitMode(pair.Value);
				}
			}
			originalMirrors.Clear();
			if (backgroundSaved)
			{
				Application.runInBackground = previousBackground;
				backgroundSaved = false;
			}
		}

		private void PresentDesktop(ScriptableRenderContext context, List<Camera> cameras)
		{
			// Rebuilt from native code (token 0x06000075, RVA 0x2710008)
			if (!showing || !material || !owner || !owner.Preview)
			{
				return;
			}
			bool toScreen = false;
			foreach (Camera camera in cameras)
			{
				if (camera.cameraType == CameraType.Game && !camera.targetTexture)
				{
					toScreen = true;
					break;
				}
			}
			if (!toScreen)
			{
				return;
			}
			int width = Mathf.Max(Screen.width, 1);
			int height = Mathf.Max(Screen.height, 1);
			material.SetTexture("_BaseMap", owner.Preview);
			material.SetFloat("_OutputAspect", (float)width / height);
			CommandBuffer cmd = CommandBufferPool.Get("Ripple camera on the desktop");
			try
			{
				cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				cmd.SetViewport(new Rect(0f, 0f, width, height));
				cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
				context.ExecuteCommandBuffer(cmd);
				context.Submit();
			}
			finally
			{
				CommandBufferPool.Release(cmd);
			}
		}
	}
}
