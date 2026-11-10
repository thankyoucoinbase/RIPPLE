using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Shallows
{
	public class DesktopTestControls : MonoBehaviour
	{
		public PlayerRig rig;

		public float wadeSpeed = 0.4f;

		public float walkSpeed = 1.5f;

		public float runSpeed = 4f;

		public float lookSensitivity = 0.15f;

		private float splashPower = 0.5f;

		private float pitch;

		private bool active;

		private const int Ground = -5;

		private static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

		private void Start()
		{
			// Rebuilt from native code (token 0x06000099, RVA 0x2711B9C)
			active = !HeadsetRunning();
			if (!active)
			{
				enabled = false;
				return;
			}
			rig.DesktopMode = true;
			rig.body.isKinematic = true;
			rig.locomotion.enabled = false;
			Debug.Log("Shallows: no headset yet, starting in desktop test mode. It switches to VR as soon as a headset starts.");
		}

		private static bool HeadsetRunning()
		{
			// Rebuilt from native code (token 0x0600009A, RVA 0x2711CA0)
			if (XRSettings.isDeviceActive)
			{
				return true;
			}
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

		private void ExitDesktopMode()
		{
			// Rebuilt from native code (token 0x0600009B, RVA 0x2711E70)
			active = false;
			enabled = false;
			rig.DesktopMode = false;
			rig.head.localRotation = Quaternion.identity;
			if (!rig.Frozen)
			{
				rig.body.isKinematic = false;
			}
			rig.locomotion.enabled = true;
			rig.locomotion.ResetPositions();
			Debug.Log("Shallows: headset found, switched to VR with arm-swing movement.");
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x0600009C, RVA 0x2711FA4)
			if (HeadsetRunning())
			{
				ExitDesktopMode();
				return;
			}
			Mouse mouse = Mouse.current;
			if (mouse == null)
			{
				return;
			}
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
			{
				return;
			}
			if (!rig.Frozen)
			{
				if (mouse.rightButton.isPressed)
				{
					Vector2 delta = mouse.delta.ReadValue();
					rig.transform.Rotate(0f, delta.x * lookSensitivity, 0f, Space.World);
					pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -80f, 80f);
				}
				rig.head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
				Vector3 input = Vector3.zero;
				if (keyboard.wKey.isPressed)
				{
					input.z += 1f;
				}
				if (keyboard.sKey.isPressed)
				{
					input.z -= 1f;
				}
				if (keyboard.dKey.isPressed)
				{
					input.x += 1f;
				}
				if (keyboard.aKey.isPressed)
				{
					input.x -= 1f;
				}
				float speed = keyboard.leftShiftKey.isPressed ? runSpeed : (keyboard.leftCtrlKey.isPressed ? wadeSpeed : walkSpeed);
				Vector3 move = (rig.transform.forward * input.z + rig.transform.right * input.x).normalized;
				Vector3 step = move * speed * Time.deltaTime;
				Vector3 position = rig.transform.position;
				Vector3 head = rig.head.position;
				if (!Physics.SphereCast(rig.head.position + Vector3.down * 0.5f, 0.2f, step.normalized, out RaycastHit _, step.magnitude + 0.05f, Ground, QueryTriggerInteraction.Ignore))
				{
					rig.transform.position = position + step;
				}
				if (Physics.Raycast(head + step + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f, Ground, QueryTriggerInteraction.Ignore))
				{
					float rise = hit.point.y - rig.BodyBottom.y;
					Transform body = rig.transform;
					body.position += Vector3.up * (rise > 0.7f ? 0f : rise * Mathf.Clamp01(Time.deltaTime * 12f));
				}
			}
			rig.leftHand.position = rig.leftController.position;
			rig.rightHand.position = rig.rightController.position;
			Vector2 scroll = mouse.scroll.ReadValue();
			if (Mathf.Abs(scroll.y) > 0.01f)
			{
				splashPower = Mathf.Clamp01(splashPower + (scroll.y < 0f ? -0.1f : 0.1f));
			}
			if (mouse.leftButton.wasPressedThisFrame)
			{
				Click(mouse.position.ReadValue());
			}
		}

		private void Click(Vector2 screen)
		{
			// Rebuilt from native code (token 0x0600009D, RVA 0x2712928)
			Camera camera = rig.head.GetComponent<Camera>();
			if (!camera)
			{
				return;
			}
			Ray ray = camera.ScreenPointToRay(screen);
			if (Physics.Raycast(ray, out RaycastHit hit, 30f, 4, QueryTriggerInteraction.Collide))
			{
				PokeButton button = hit.collider.GetComponent<PokeButton>();
				if (button)
				{
					button.Press(true);
					return;
				}
			}
			WaterVolume water = WaterVolume.First;
			if (!water || !SplashSystem.I || Mathf.Abs(ray.direction.y) < 0.001f)
			{
				return;
			}
			float distance = (water.SurfaceY - ray.origin.y) / ray.direction.y;
			if (distance <= 0f || distance > 60f || Physics.Raycast(ray, distance, Ground, QueryTriggerInteraction.Ignore))
			{
				return;
			}
			Vector3 point = ray.GetPoint(distance);
			if (WaterVolume.At(point))
			{
				SplashSystem.I.Loud(point, splashPower, SplashKind.Object);
			}
		}

		private void OnGUI()
		{
			// Rebuilt from native code (token 0x0600009E, RVA 0x2712EF8)
			if (!active)
			{
				return;
			}
			string splash = string.Format("Left click the water: splash at {0:0}% power (scroll to change)   Left click a tuning button: press it", splashPower * 100f);
			GUI.Label(new Rect(12f, 10f, 900f, 80f), "DESKTOP TEST MODE (no headset found)\nHold right mouse: look   WASD: move   Shift: run   Ctrl: wade slowly\n" + splash);
		}
	}
}
