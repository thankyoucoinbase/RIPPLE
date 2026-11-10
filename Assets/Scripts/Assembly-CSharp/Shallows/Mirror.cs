using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace Shallows
{
	public class Mirror : MonoBehaviour
	{
		public const int FirstPersonLayer = 30;

		public const int ReflectionLayer = 31;

		private const string ShaderPath = "ShallowsMirror";

		[Tooltip("The glass's width and height (m). Its front faces this object's +Z.")]
		public Vector2 size = new Vector2(3f, 2f);

		[Tooltip("Most pixels across the reflection, for each eye: what you get right up against the glass.")]
		public int resolution = 2048;

		[Tooltip("Fewest pixels across the reflection, for each eye: what you get from far away.")]
		public int minResolution = 256;

		[Tooltip("Reflection pixels per pixel of your view. 1 matches the headset's sharpness; lower is cheaper.")]
		public float quality = 1f;

		[Tooltip("The mirror stops rendering when you're further away than this (m).")]
		public float renderDistance = 12f;

		[Tooltip("Seconds to fade to black when you leave the spawn deck, and back in when you return.")]
		public float fadeTime = 0.6f;

		// Rebuilt from native code (token 0x06000492, RVA 0x27B70BC)
		private readonly Camera[] eyes = new Camera[2];

		private readonly RenderTexture[] images = new RenderTexture[2];

		private Material material;

		private Camera viewer;

		private Vector3 eyeOffset = new Vector3(0.032f, 0f, 0f);

		private float nextEyeCheck;

		private float fade = 1f;

		private float shownWidth = -1f;

		// Rebuilt from native code (token 0x06000493, RVA 0x27B7198)
		private static readonly int[] ImageIds = { Shader.PropertyToID("_LeftTex"), Shader.PropertyToID("_RightTex") };

		private static readonly int FadeId = Shader.PropertyToID("_Fade");

		private static readonly int ImageScaleId = Shader.PropertyToID("_ImageScale");

		public static Mirror Create(Vector3 center, Quaternion facing, Vector2 size, Transform parent = null)
		{
			// Rebuilt from native code (token 0x06000486, RVA 0x27B463C)
			Shader shader = Resources.Load<Shader>(ShaderPath);
			if (!shader)
			{
				return null;
			}
			GameObject go = new GameObject("Mirror");
			go.transform.SetParent(parent, false);
			go.transform.SetPositionAndRotation(center, facing);
			Mirror mirror = go.AddComponent<Mirror>();
			mirror.size = size;
			GameObject glass = new GameObject("Glass");
			glass.transform.SetParent(go.transform, false);
			glass.AddComponent<MeshFilter>().sharedMesh = GlassMesh(size);
			MeshRenderer renderer = glass.AddComponent<MeshRenderer>();
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			mirror.material = new Material(shader) { name = "Mirror" };
			if (!Application.isPlaying)
			{
				Sheen(mirror.material);
			}
			renderer.sharedMaterial = mirror.material;
			Material wood = null;
			Bar("Frame Top", new Vector3(0f, size.y * 0.5f + 0.04f, 0f), new Vector3(size.x + 0.16f, 0.08f, 0.05f));
			Bar("Frame Bottom", new Vector3(0f, -(size.y * 0.5f + 0.04f), 0f), new Vector3(size.x + 0.16f, 0.08f, 0.05f));
			Bar("Frame Left", new Vector3(-(size.x * 0.5f + 0.04f), 0f, 0f), new Vector3(0.08f, size.y, 0.05f));
			Bar("Frame Right", new Vector3(size.x * 0.5f + 0.04f, 0f, 0f), new Vector3(0.08f, size.y, 0.05f));
			return mirror;

			void Bar(string name, Vector3 position, Vector3 scale)
			{
				// Rebuilt from native code (token 0x06000494, RVA 0x27B5040)
				GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
				bar.name = name;
				DestroyImmediate(bar.GetComponent<Collider>());
				bar.transform.SetParent(go.transform, false);
				bar.transform.localPosition = position;
				bar.transform.localScale = scale;
				Renderer barRenderer = bar.GetComponent<Renderer>();
				if (!wood)
				{
					wood = new Material(barRenderer.sharedMaterial)
					{
						name = "Mirror Frame",
						color = new Color(0.16f, 0.12f, 0.09f, 1f)
					};
					if (wood.HasProperty("_Smoothness"))
					{
						wood.SetFloat("_Smoothness", 0.2f);
					}
				}
				barRenderer.sharedMaterial = wood;
			}
		}

		private static void Sheen(Material material)
		{
			// Rebuilt from native code (token 0x06000487, RVA 0x27B4DF0)
			Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
			{
				name = "Mirror Sheen",
				wrapMode = TextureWrapMode.Clamp
			};
			Color[] pixels = new Color[32 * 32];
			for (int y = 0; y < 32; y++)
			{
				float shade = Mathf.Lerp(0.42f, 0.62f, y / 31f);
				for (int x = 0; x < 32; x++)
				{
					float diagonal = (x + y) / 64f;
					float streaks = Mathf.Clamp01(1f - Mathf.Abs(diagonal - 0.62f) * 9f) * 0.25f + Mathf.Clamp01(1f - Mathf.Abs(diagonal - 0.42f) * 30f) * 0.12f;
					float v = shade + streaks;
					pixels[y * 32 + x] = new Color(v * 0.9f, v * 0.97f, v, 1f);
				}
			}
			texture.SetPixels(pixels);
			texture.Apply();
			material.SetTexture(ImageIds[0], texture);
			material.SetTexture(ImageIds[1], texture);
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000488, RVA 0x27B52E0)
			if (Application.isMobilePlatform)
			{
				resolution = Mathf.Min(resolution, 1024);
				minResolution = Mathf.Min(minResolution, 192);
			}
			if (material)
			{
				return;
			}
			Transform glass = transform.Find("Glass");
			MeshRenderer renderer = glass ? glass.GetComponent<MeshRenderer>() : null;
			Shader shader = Resources.Load<Shader>(ShaderPath);
			if (!renderer || !shader)
			{
				return;
			}
			material = new Material(shader) { name = "Mirror" };
			renderer.sharedMaterial = material;
		}

		private static Mesh GlassMesh(Vector2 size)
		{
			// Rebuilt from native code (token 0x06000489, RVA 0x27B4B24)
			Mesh mesh = new Mesh { name = "Mirror Glass" };
			Vector2 half = size * 0.5f;
			mesh.vertices = new[]
			{
				new Vector3(-half.x, -half.y, 0f),
				new Vector3(half.x, -half.y, 0f),
				new Vector3(half.x, half.y, 0f),
				new Vector3(-half.x, half.y, 0f)
			};
			mesh.uv = new[]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};
			mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
			mesh.triangles = new[] { 1, 2, 3, 1, 3, 0 };
			mesh.RecalculateBounds();
			return mesh;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x0600048A, RVA 0x27B551C)
			RenderPipelineManager.beginContextRendering += PlaceEyes;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x0600048B, RVA 0x27B559C)
			RenderPipelineManager.beginContextRendering -= PlaceEyes;
			foreach (Camera eye in eyes)
			{
				if (eye)
				{
					eye.enabled = false;
				}
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x0600048C, RVA 0x27B56AC)
			foreach (RenderTexture image in images)
			{
				if (image)
				{
					image.Release();
				}
			}
			if (material && Application.isPlaying)
			{
				Destroy(material);
			}
		}

		private Camera MakeEye(int index)
		{
			// Rebuilt from native code (token 0x0600048D, RVA 0x27B57F4)
			int width = Mathf.Max(resolution, 64);
			int height = Mathf.Max(Mathf.RoundToInt(size.y * resolution / size.x), 64);
			images[index] = new RenderTexture(width, height, 24, RenderTextureFormat.Default, RenderTextureReadWrite.Default)
			{
				name = index == 0 ? "Mirror Left Eye" : "Mirror Right Eye",
				antiAliasing = 1
			};
			material.SetTexture(ImageIds[index], images[index]);
			GameObject go = new GameObject(index == 0 ? "Mirror Eye Left" : "Mirror Eye Right");
			go.transform.SetParent(transform, false);
			Camera eye = go.AddComponent<Camera>();
			eye.targetTexture = images[index];
			eye.allowMSAA = false;
			eye.allowHDR = false;
			eye.depth = viewer.depth - 10f;
			eye.clearFlags = viewer.clearFlags;
			eye.backgroundColor = viewer.backgroundColor;
			UniversalAdditionalCameraData data = eye.GetUniversalAdditionalCameraData();
			data.renderPostProcessing = false;
			data.antialiasing = AntialiasingMode.None;
			data.renderShadows = !Application.isMobilePlatform;
			data.requiresColorTexture = false;
			data.requiresDepthTexture = false;
			eye.enabled = false;
			return eye;
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x0600048E, RVA 0x27B5C70)
			if (!material)
			{
				return;
			}
			if (!viewer || !viewer.isActiveAndEnabled)
			{
				viewer = PlayerRig.I ? PlayerRig.I.head.GetComponentInChildren<Camera>() : null;
				if (!viewer)
				{
					viewer = Camera.main;
				}
				if (!viewer)
				{
					return;
				}
			}
			float target = GameManager.I ? (GameManager.I.InSafeZone ? 1f : 0f) : 1f;
			fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
			material.SetFloat(FadeId, fade * fade * (3f - 2f * fade));
			Vector3 viewerLocal = transform.InverseTransformPoint(viewer.transform.position);
			Vector3 toMirror = transform.position - viewer.transform.position;
			float distance = toMirror.magnitude;
			bool show = fade > 0f && viewerLocal.z > 0.02f && distance < renderDistance && (distance < size.magnitude || Vector3.Angle(toMirror, viewer.transform.forward) < 100f);
			bool stereo = viewer.stereoEnabled;
			for (int i = 0; i < 2; i++)
			{
				bool wanted = show && (stereo || i == 0);
				if (wanted && !eyes[i])
				{
					eyes[i] = MakeEye(i);
				}
				if (eyes[i])
				{
					eyes[i].enabled = wanted;
				}
			}
			if (!stereo && images[0])
			{
				material.SetTexture(ImageIds[1], images[0]);
			}
			else
			{
				if (images[1])
				{
					material.SetTexture(ImageIds[1], images[1]);
				}
				if (stereo && Time.unscaledTime >= nextEyeCheck)
				{
					ReadEyeOffset();
				}
			}
			if (show)
			{
				FitSharpness(viewerLocal, stereo);
			}
		}

		private void FitSharpness(Vector3 viewerLocal, bool stereo)
		{
			// Rebuilt from native code (token 0x0600048F, RVA 0x27B6744)
			if (!images[0])
			{
				return;
			}
			float pixels = stereo && XRSettings.eyeTextureWidth > 0 ? XRSettings.eyeTextureWidth : viewer.pixelWidth;
			float aspect = stereo && XRSettings.eyeTextureHeight > 0 ? (float)XRSettings.eyeTextureWidth / XRSettings.eyeTextureHeight : viewer.aspect;
			float fov = Mathf.Max(0.1f, Camera.VerticalToHorizontalFieldOfView(viewer.fieldOfView, aspect) * Mathf.Deg2Rad);
			Vector2 half = size * 0.5f;
			Vector3 nearest = new Vector3(Mathf.Clamp(viewerLocal.x, -half.x, half.x), Mathf.Clamp(viewerLocal.y, -half.y, half.y), 0f);
			float distance = Mathf.Max(0.05f, Vector3.Distance(viewerLocal, nearest));
			int max = images[0].width;
			float across = pixels / fov * size.x / distance;
			float target = Mathf.Clamp(quality * across, Mathf.Min(minResolution, max), max);
			shownWidth = shownWidth < 0f ? target : Mathf.Lerp(shownWidth, target, 1f - MathF.Exp(-6f * Time.unscaledDeltaTime));
			int width = Mathf.Clamp(Mathf.RoundToInt(shownWidth), 16, max);
			int height = Mathf.Clamp(Mathf.RoundToInt(size.y * width / size.x), 16, images[0].height);
			Rect rect = new Rect(0f, 0f, width, height);
			foreach (Camera eye in eyes)
			{
				if (eye && eye.pixelRect != rect)
				{
					eye.pixelRect = rect;
				}
			}
			material.SetVector(ImageScaleId, new Vector4(width / (float)max, height / (float)images[0].height, 0.5f / max, 0.5f / images[0].height));
		}

		private void ReadEyeOffset()
		{
			// Rebuilt from native code (token 0x06000490, RVA 0x27B6570)
			nextEyeCheck = Time.unscaledTime + 1f;
			InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
			if (!head.isValid)
			{
				head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
			}
			if (head.isValid && head.TryGetFeatureValue(CommonUsages.rightEyePosition, out Vector3 rightEye) && head.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 centreEye) && head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation))
			{
				Vector3 offset = Quaternion.Inverse(rotation) * (rightEye - centreEye);
				if (offset.magnitude > 0.02f && offset.magnitude < 0.05f)
				{
					eyeOffset = offset;
				}
			}
		}

		private void PlaceEyes(ScriptableRenderContext context, List<Camera> cameras)
		{
			// Rebuilt from native code (token 0x06000491, RVA 0x27B6DC8)
			if (!viewer)
			{
				return;
			}
			int mask = (viewer.cullingMask & ~(1 << FirstPersonLayer)) | (1 << ReflectionLayer);
			for (int i = 0; i < 2; i++)
			{
				Camera eye = eyes[i];
				if (!eye || !eye.enabled)
				{
					continue;
				}
				Vector3 eyePosition = viewer.stereoEnabled ? viewer.transform.TransformPoint(i == 0 ? -eyeOffset : eyeOffset) : viewer.transform.position;
				Vector3 local = transform.InverseTransformPoint(eyePosition);
				float near = Mathf.Max(0.01f, local.z);
				eye.transform.SetLocalPositionAndRotation(new Vector3(local.x, local.y, -near), Quaternion.identity);
				eye.cullingMask = mask;
				float far = near + renderDistance * 4f;
				eye.nearClipPlane = near;
				eye.farClipPlane = far;
				eye.projectionMatrix = Matrix4x4.Frustum(-size.x * 0.5f - local.x, size.x * 0.5f - local.x, -size.y * 0.5f - local.y, size.y * 0.5f - local.y, near, far);
			}
		}
	}
}
