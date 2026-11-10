using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace Shallows
{
	public static class XRHands
	{
		private static InputAction leftGrip;

		private static InputAction rightGrip;

		private static InputAction leftTrigger;

		private static InputAction rightTrigger;

		private static InputAction leftThumb;

		private static InputAction rightThumb;

		private static InputAction leftHaptic;

		private static InputAction rightHaptic;

		private static InputAction talk;

		private static InputAction leftStick;

		private static InputAction rightStick;

		private static MethodInfo openXRHaptic;

		private static bool initialized;

		// Rebuilt from native code (token 0x06000882, RVA 0x282C65C)
		private static readonly float[] strongUntil = new float[2];

		private static void Init()
		{
			// Rebuilt from native code (token 0x06000877, RVA 0x282B360)
			if (initialized)
			{
				return;
			}
			initialized = true;
			leftGrip = MakeAction("Left Grip", "<XRController>{LeftHand}/{Grip}", InputActionType.Value);
			rightGrip = MakeAction("Right Grip", "<XRController>{RightHand}/{Grip}", InputActionType.Value);
			leftTrigger = MakeAction("Left Trigger", "<XRController>{LeftHand}/{Trigger}", InputActionType.Value);
			rightTrigger = MakeAction("Right Trigger", "<XRController>{RightHand}/{Trigger}", InputActionType.Value);
			leftStick = MakeAction("Left Stick", "<XRController>{LeftHand}/{Primary2DAxis}", InputActionType.Value);
			rightStick = MakeAction("Right Stick", "<XRController>{RightHand}/{Primary2DAxis}", InputActionType.Value);
			leftThumb = ThumbAction("{LeftHand}");
			rightThumb = ThumbAction("{RightHand}");
			talk = new InputAction("Talk", InputActionType.PassThrough);
			foreach (string hand in new string[] { "{LeftHand}", "{RightHand}" })
			{
				talk.AddBinding("<XRController>" + hand + "/{PrimaryButton}");
				talk.AddBinding("<XRController>" + hand + "/{SecondaryButton}");
			}
			talk.AddBinding("<Keyboard>/v");
			talk.Enable();
			leftHaptic = MakeAction("Left Haptic", "<XRController>{LeftHand}/{Haptic}", InputActionType.PassThrough);
			rightHaptic = MakeAction("Right Haptic", "<XRController>{RightHand}/{Haptic}", InputActionType.PassThrough);
			Type openXRInput = Type.GetType("UnityEngine.XR.OpenXR.Input.OpenXRInput, Unity.XR.OpenXR");
			if (openXRInput == null)
			{
				return;
			}
			foreach (MethodInfo method in openXRInput.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				if (method.Name != "SendHapticImpulse")
				{
					continue;
				}
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length == 4
					&& parameters[0].ParameterType == typeof(InputAction)
					&& parameters[1].ParameterType == typeof(float)
					&& parameters[2].ParameterType == typeof(float)
					&& parameters[3].ParameterType == typeof(UnityEngine.InputSystem.InputDevice))
				{
					openXRHaptic = method;
					break;
				}
			}
		}

		private static InputAction MakeAction(string name, string binding, InputActionType type)
		{
			// Rebuilt from native code (token 0x06000878, RVA 0x282BC04)
			InputAction action = new InputAction(name, type, binding);
			action.Enable();
			return action;
		}

		private static InputAction ThumbAction(string hand)
		{
			// Rebuilt from native code (token 0x06000879, RVA 0x282BC98)
			InputAction action = new InputAction("Thumb " + hand, InputActionType.PassThrough);
			foreach (string control in new string[] { "PrimaryTouch", "SecondaryTouch", "Primary2DAxisTouch", "PrimaryButton", "SecondaryButton", "Primary2DAxisClick" })
			{
				action.AddBinding("<XRController>" + hand + "/{" + control + "}");
			}
			action.Enable();
			return action;
		}

		public static float Trigger(bool left)
		{
			// Rebuilt from native code (token 0x0600087A, RVA 0x282C020)
			Init();
			return (left ? leftTrigger : rightTrigger).ReadValue<float>();
		}

		public static Vector2 Stick(bool left)
		{
			// Rebuilt from native code (token 0x0600087B, RVA 0x2828B14)
			Init();
			return (left ? leftStick : rightStick).ReadValue<Vector2>();
		}

		public static bool ThumbDown(bool left)
		{
			// Rebuilt from native code (token 0x0600087C, RVA 0x282C0D0)
			Init();
			foreach (InputControl control in (left ? leftThumb : rightThumb).controls)
			{
				if (control is ButtonControl button && button.isPressed)
				{
					return true;
				}
			}
			return false;
		}

		public static bool TalkHeld()
		{
			// Rebuilt from native code (token 0x0600087D, RVA 0x282C2D4)
			Init();
			foreach (InputControl control in talk.controls)
			{
				if (control is ButtonControl button && button.isPressed)
				{
					return true;
				}
			}
			return false;
		}

		public static float Grip(bool left)
		{
			// Rebuilt from native code (token 0x0600087E, RVA 0x282C4A4)
			Init();
			return (left ? leftGrip : rightGrip).ReadValue<float>();
		}

		public static void Haptic(bool left, float amplitude, float duration, bool strong = true)
		{
			// Rebuilt from native code (token 0x0600087F, RVA 0x2827D50)
			Init();
			int hand = left ? 0 : 1;
			if (strong)
			{
				strongUntil[hand] = Time.time + duration;
			}
			else if (Time.time < strongUntil[hand])
			{
				return;
			}
			amplitude = Mathf.Clamp01(amplitude);
			if (duration <= 0f || amplitude <= 0f)
			{
				return;
			}
			if (openXRHaptic != null)
			{
				UnityEngine.InputSystem.InputDevice device = HandDevice(left);
				if (device != null)
				{
					openXRHaptic.Invoke(null, new object[] { left ? leftHaptic : rightHaptic, amplitude, duration, device });
					return;
				}
			}
			UnityEngine.XR.InputDevice node = InputDevices.GetDeviceAtXRNode(left ? XRNode.LeftHand : XRNode.RightHand);
			if (node.isValid)
			{
				node.SendHapticImpulse(0u, amplitude, duration);
			}
		}

		private static UnityEngine.InputSystem.InputDevice HandDevice(bool left)
		{
			// Rebuilt from native code (token 0x06000880, RVA 0x282C550)
			XRController controller = left ? XRController.leftHand : XRController.rightHand;
			if (controller != null)
			{
				return controller;
			}
			ReadOnlyArray<InputControl> controls = (left ? leftHaptic : rightHaptic).controls;
			return controls.Count > 0 ? controls[0].device : null;
		}

		public static void HapticBoth(float amplitude, float duration, bool strong = true)
		{
			// Rebuilt from native code (token 0x06000881, RVA 0x28283D8)
			Haptic(true, amplitude, duration, strong);
			Haptic(false, amplitude, duration, strong);
		}
	}
}
