using System.Collections.Generic;
using System.Diagnostics;
using CustomKeyEvents;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace AvatarScriptPack
{
	public class CustomKeyEvent : MonoBehaviour
	{
		public enum ButtonEventType
		{
			Click,
			DoubleClick,
			LongClick,
			Press,
			Hold,
			Release,
			ReleaseAfterLongClick
		}

		public enum IndexButton
		{
			LeftInnerFace = KeyCode.JoystickButton2,
			RightInnerFace = KeyCode.JoystickButton0,
			LeftOuterFace = KeyCode.JoystickButton3,
			RightOuterFace = KeyCode.JoystickButton1,
			LeftTrackpadPress = KeyCode.JoystickButton8,
			RightTrackpadPress = KeyCode.JoystickButton9,
			LeftTrackpadTouch = KeyCode.JoystickButton16,
			RightTrackpadTouch = KeyCode.JoystickButton17,
			LeftTrigger = KeyCode.JoystickButton14,
			RightTrigger = KeyCode.JoystickButton15,
			Space = KeyCode.Space,
			None = KeyCode.None
		}

		public enum ViveButton
		{
			RightMenu = KeyCode.JoystickButton0,
			LeftMenu = KeyCode.JoystickButton2,
			LeftTrackpadPress = KeyCode.JoystickButton8,
			RightTrackpadPress = KeyCode.JoystickButton9,
			LeftTrigger = KeyCode.JoystickButton14,
			RightTrigger = KeyCode.JoystickButton15,
			LeftTrackpadTouch = KeyCode.JoystickButton16,
			RightTrackpadTouch = KeyCode.JoystickButton17,
			Space = KeyCode.Space,
			None = KeyCode.None
		}

		public enum OculusButton
		{
			A = KeyCode.JoystickButton0,
			B = KeyCode.JoystickButton1,
			X = KeyCode.JoystickButton2,
			Y = KeyCode.JoystickButton3,
			Start = KeyCode.JoystickButton7,
			LeftThumbstickPress = KeyCode.JoystickButton8,
			RightThumbstickPress = KeyCode.JoystickButton9,
			LeftTrigger = KeyCode.JoystickButton14,
			RightTrigger = KeyCode.JoystickButton15,
			LeftThumbstickTouch = KeyCode.JoystickButton16,
			RightThumbstickTouch = KeyCode.JoystickButton17,
			LeftThumbRestTouch = KeyCode.JoystickButton18,
			RightThumbRestTouch = KeyCode.JoystickButton19,
			Space = KeyCode.Space,
			None = KeyCode.None
		}

		public enum WMRButton
		{
			LeftMenu = KeyCode.JoystickButton6,
			RightMenu = KeyCode.JoystickButton7,
			LeftThumbstickPress = KeyCode.JoystickButton8,
			RightThumbstickPress = KeyCode.JoystickButton9,
			LeftTrigger = KeyCode.JoystickButton14,
			RightTrigger = KeyCode.JoystickButton15,
			LeftTouchpadPress = KeyCode.JoystickButton16,
			RightTouchpadPress = KeyCode.JoystickButton17,
			LeftTouchpadTouch = KeyCode.JoystickButton18,
			RightTouchpadTouch = KeyCode.JoystickButton19,
			Space = KeyCode.Space,
			None = KeyCode.None
		}

		[Tooltip("Button to trigger the events.")]
		public IndexButton IndexTriggerButton = IndexButton.None;

		[Tooltip("Button to trigger the events.")]
		public ViveButton ViveTriggerButton = ViveButton.None;

		[Tooltip("Button to trigger the events.")]
		public OculusButton OculusTriggerButton = OculusButton.None;

		[Tooltip("Button to trigger the events.")]
		public WMRButton WMRTriggerButton = WMRButton.None;

		[Space(20)]

		[Tooltip("Called when the click event is triggered.")]
		public UnityEvent clickEvents = new UnityEvent();

		[Tooltip("Called when the double click event is triggered.")]
		public UnityEvent doubleClickEvents = new UnityEvent();

		[Tooltip("Called when the long click event is triggered.")]
		public UnityEvent longClickEvents = new UnityEvent();

		[Tooltip("Called when the press event is triggered.")]
		public UnityEvent pressEvents = new UnityEvent();

		[Tooltip("Called when the hold event is triggered.")]
		public UnityEvent holdEvents = new UnityEvent();

		[Tooltip("Called when the release event is triggered.")]
		public UnityEvent releaseEvents = new UnityEvent();

		[Tooltip("Called when released after long click.")]
		public UnityEvent releaseAfterLongClickEvents = new UnityEvent();

		protected const float interval = 0.5f;
		protected const float longClickInterval = 0.6f;
		protected const float analogPressThreshold = 0.55f;
		protected const float analogReleaseThreshold = 0.25f;
		protected const float diagnosticsPollInterval = 2.0f;

		protected float pressTime;
		protected float releaseTime;
		protected bool checkClick = false;
		protected bool checkDoubleClick = false;
		protected bool checkLongClick = false;
		protected bool longClicked = false;
		protected bool triggerPressed = false;

		private InputDevice leftController;
		private InputDevice rightController;
		private KeyCode previousTriggerButton = KeyCode.None;
		private readonly HashSet<KeyCode> legacyFallbackLoggedButtons = new HashSet<KeyCode>();
		private readonly HashSet<string> debugOnceLogKeys = new HashSet<string>();
		private string lastInputReadRoute = "none";
		private string lastInputReadDetail = "";
		private bool lastInputReadPressed = false;
		private float nextDiagnosticsLogTime = 0f;

		void Start()
		{
			this.leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			this.rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		}

		void Update()
		{
			KeyCode triggerButton = ResolveTriggerButton();
			if (triggerButton == KeyCode.None)
			{
				return;
			}

			if (previousTriggerButton != triggerButton)
			{
				triggerPressed = false;
				checkLongClick = false;
				longClicked = false;
				previousTriggerButton = triggerButton;
			}

			bool pressedNow = GetButtonPressState(triggerButton);
			DebugOnlyLogPoll(triggerButton, pressedNow);

			if (pressedNow && !triggerPressed)
			{
				DebugOnlyLogStateChange(triggerButton, true, pressedNow);
				CustomKeyEvents.Logger.log.Debug(triggerButton + " is pressed");
				checkDoubleClick = (Time.time - pressTime <= interval);
				pressTime = Time.time;
				OnPress();
				checkLongClick = true;
				checkClick = false;
			}

			if (pressedNow)
			{
				OnHold();
				if (checkLongClick && Time.time - pressTime >= longClickInterval)
				{
					CustomKeyEvents.Logger.log.Debug(triggerButton + " is longClicked");
					checkLongClick = false;
					OnLongClick();
					longClicked = true;
				}
			}

			if (!pressedNow && triggerPressed)
			{
				DebugOnlyLogStateChange(triggerButton, false, pressedNow);
				CustomKeyEvents.Logger.log.Debug(triggerButton + " is up");
				releaseTime = Time.time;
				OnRelease();
				if (longClicked)
				{
					OnReleaseAfterLongClick();
					longClicked = false;
				}
				if (releaseTime - pressTime <= interval)
				{
					if (checkDoubleClick)
					{
						OnDoubleClick();
					}
					else
					{
						checkClick = true;
					}
				}
			}

			triggerPressed = pressedNow;

			if (checkClick && Time.time - releaseTime > interval)
			{
				checkClick = false;
				OnClick();
			}
		}

		private KeyCode ResolveTriggerButton()
		{
			switch (CustomKeyEventsController.Model)
			{
				case CustomKeyEventsController.DeviceModel.Index:
					return (KeyCode)IndexTriggerButton;
				case CustomKeyEventsController.DeviceModel.Vive:
					return (KeyCode)ViveTriggerButton;
				case CustomKeyEventsController.DeviceModel.Oculus:
					return (KeyCode)OculusTriggerButton;
				case CustomKeyEventsController.DeviceModel.WMR:
					return (KeyCode)WMRTriggerButton;
			}

			// Keep old avatars working even when model detection fails.
			if (ViveTriggerButton != ViveButton.None)
			{
				return (KeyCode)ViveTriggerButton;
			}
			if (IndexTriggerButton != IndexButton.None)
			{
				return (KeyCode)IndexTriggerButton;
			}
			if (OculusTriggerButton != OculusButton.None)
			{
				return (KeyCode)OculusTriggerButton;
			}
			if (WMRTriggerButton != WMRButton.None)
			{
				return (KeyCode)WMRTriggerButton;
			}

			return KeyCode.None;
		}

		private bool GetButtonPressState(KeyCode triggerButton)
		{
			if (TryGetOpenXRButtonPress(triggerButton, out bool pressed))
			{
				DebugOnlySetInputTrace("openxr", $"button={triggerButton}", pressed);
				return pressed;
			}

			if (legacyFallbackLoggedButtons.Add(triggerButton))
			{
				CustomKeyEvents.Logger.log.Warn($"Cannot map {triggerButton} to OpenXR usage. Fallback to Input.GetKey().");
			}
			bool legacyPressed = Input.GetKey(triggerButton);
			DebugOnlySetInputTrace("legacy_getkey", $"button={triggerButton}", legacyPressed);
			return legacyPressed;
		}

		private bool TryGetOpenXRButtonPress(KeyCode triggerButton, out bool pressed)
		{
			pressed = false;
			if (!TryGetNodeFromKeyCode(triggerButton, out XRNode node))
			{
				DebugOnlySetInputTrace("node_map_failed", $"button={triggerButton}", false);
				return false;
			}

			switch (triggerButton)
			{
				case KeyCode.JoystickButton14:
				case KeyCode.JoystickButton15:
					return TryReadTriggerPress(node, out pressed);
				case KeyCode.JoystickButton4:
				case KeyCode.JoystickButton5:
					return TryReadGripPress(node, out pressed);
				case KeyCode.JoystickButton8:
				case KeyCode.JoystickButton9:
					return TryReadBoolFeature(node, out pressed, CommonUsages.primary2DAxisClick, CommonUsages.secondary2DAxisClick);
				case KeyCode.JoystickButton16:
				case KeyCode.JoystickButton17:
					return TryReadBoolFeature(node, out pressed, CommonUsages.primary2DAxisTouch, CommonUsages.secondary2DAxisTouch, CommonUsages.primaryTouch, CommonUsages.secondaryTouch);
				case KeyCode.JoystickButton18:
				case KeyCode.JoystickButton19:
					return TryReadBoolFeature(node, out pressed, CommonUsages.primaryTouch, CommonUsages.secondaryTouch, CommonUsages.secondary2DAxisTouch, CommonUsages.primary2DAxisTouch);
				case KeyCode.JoystickButton0:
				case KeyCode.JoystickButton2:
					return TryReadBoolFeature(node, out pressed, CommonUsages.primaryButton, CommonUsages.menuButton);
				case KeyCode.JoystickButton1:
				case KeyCode.JoystickButton3:
					return TryReadBoolFeature(node, out pressed, CommonUsages.secondaryButton);
				case KeyCode.JoystickButton6:
				case KeyCode.JoystickButton7:
					return TryReadBoolFeature(node, out pressed, CommonUsages.menuButton, CommonUsages.primaryButton);
				default:
					return TryReadBoolFeature(node, out pressed, CommonUsages.primaryButton, CommonUsages.secondaryButton, CommonUsages.menuButton);
			}
		}

		private bool TryGetNodeFromKeyCode(KeyCode triggerButton, out XRNode node)
		{
			switch (triggerButton)
			{
				case KeyCode.JoystickButton2:
				case KeyCode.JoystickButton3:
				case KeyCode.JoystickButton4:
				case KeyCode.JoystickButton6:
				case KeyCode.JoystickButton8:
				case KeyCode.JoystickButton14:
				case KeyCode.JoystickButton16:
				case KeyCode.JoystickButton18:
					node = XRNode.LeftHand;
					return true;
				case KeyCode.JoystickButton0:
				case KeyCode.JoystickButton1:
				case KeyCode.JoystickButton5:
				case KeyCode.JoystickButton7:
				case KeyCode.JoystickButton9:
				case KeyCode.JoystickButton15:
				case KeyCode.JoystickButton17:
				case KeyCode.JoystickButton19:
					node = XRNode.RightHand;
					return true;
				default:
					node = XRNode.LeftHand;
					return false;
			}
		}

		private bool TryReadTriggerPress(XRNode node, out bool pressed)
		{
			if (TryReadAxisPress(node, CommonUsages.trigger, out pressed))
			{
				DebugOnlySetInputTrace("openxr_axis_trigger", $"node={node}", pressed);
				return true;
			}
			if (TryReadBoolFeature(node, out pressed, CommonUsages.triggerButton))
			{
				DebugOnlySetInputTrace("openxr_bool_triggerButton", $"node={node}", pressed);
				return true;
			}
			DebugOnlySetInputTrace("openxr_trigger_read_failed", $"node={node}", false);
			return false;
		}

		private bool TryReadGripPress(XRNode node, out bool pressed)
		{
			if (TryReadBoolFeature(node, out pressed, CommonUsages.gripButton))
			{
				DebugOnlySetInputTrace("openxr_bool_gripButton", $"node={node}", pressed);
				return true;
			}
			if (TryReadAxisPress(node, CommonUsages.grip, out pressed))
			{
				DebugOnlySetInputTrace("openxr_axis_grip", $"node={node}", pressed);
				return true;
			}
			DebugOnlySetInputTrace("openxr_grip_read_failed", $"node={node}", false);
			return false;
		}

		private bool TryReadAxisPress(XRNode node, InputFeatureUsage<float> usage, out bool pressed)
		{
			pressed = false;
			if (!TryGetController(node, out InputDevice controller))
			{
				DebugOnlyLogOnce($"axis_controller_missing_{node}_{usage.name}", $"[diag] Controller unavailable while reading axis {usage.name} on {node}");
				return false;
			}
			DebugOnlyClearLogOnce($"axis_controller_missing_{node}_{usage.name}");

			if (!controller.TryGetFeatureValue(usage, out float axisValue))
			{
				DebugOnlyLogOnce($"axis_missing_{node}_{usage.name}", $"[diag] Axis feature {usage.name} not available on {node}");
				return false;
			}
			DebugOnlyClearLogOnce($"axis_missing_{node}_{usage.name}");

			float threshold = triggerPressed ? analogReleaseThreshold : analogPressThreshold;
			pressed = axisValue >= threshold;
			return true;
		}

		private bool TryReadBoolFeature(XRNode node, out bool pressed, params InputFeatureUsage<bool>[] usages)
		{
			pressed = false;
			if (!TryGetController(node, out InputDevice controller))
			{
				DebugOnlyLogOnce($"bool_controller_missing_{node}", $"[diag] Controller unavailable while reading bool feature on {node}");
				return false;
			}
			DebugOnlyClearLogOnce($"bool_controller_missing_{node}");

			for (int i = 0; i < usages.Length; i++)
			{
				if (controller.TryGetFeatureValue(usages[i], out bool value))
				{
					pressed = value;
					DebugOnlyClearLogOnce($"bool_missing_{node}_{BuildUsageList(usages)}");
					return true;
				}
			}
			DebugOnlyLogOnce($"bool_missing_{node}_{BuildUsageList(usages)}", $"[diag] No bool feature available on {node}. tried={BuildUsageList(usages)}");
			return false;
		}

		private bool TryGetController(XRNode node, out InputDevice controller)
		{
			controller = (node == XRNode.LeftHand) ? leftController : rightController;
			if (!controller.isValid)
			{
				DebugOnlyLogOnce($"controller_invalid_initial_{node}", $"[diag] Cached controller invalid for {node}. Attempting reacquire.");
				controller = InputDevices.GetDeviceAtXRNode(node);
				if (node == XRNode.LeftHand)
				{
					leftController = controller;
				}
				else
				{
					rightController = controller;
				}
				DebugOnlyLog($"[diag] Reacquire {node}: valid={controller.isValid} name='{controller.name}' characteristics={controller.characteristics}");
			}
			if (controller.isValid)
			{
				DebugOnlyClearLogOnce($"controller_invalid_initial_{node}");
			}
			else
			{
				DebugOnlyLogOnce($"controller_still_invalid_{node}", $"[diag] Controller still invalid for {node} after reacquire.");
			}
			return controller.isValid;
		}

		private static string BuildUsageList(InputFeatureUsage<bool>[] usages)
		{
			if (usages == null || usages.Length == 0)
			{
				return "none";
			}
			List<string> names = new List<string>(usages.Length);
			for (int i = 0; i < usages.Length; i++)
			{
				names.Add(usages[i].name);
			}
			return string.Join("|", names);
		}

		[Conditional("DEBUG")]
		private void DebugOnlyLog(string message)
		{
			CustomKeyEvents.Logger.log.Debug(message);
		}

		[Conditional("DEBUG")]
		private void DebugOnlyLogOnce(string key, string message)
		{
			if (debugOnceLogKeys.Add(key))
			{
				CustomKeyEvents.Logger.log.Debug(message);
			}
		}

		[Conditional("DEBUG")]
		private void DebugOnlyClearLogOnce(string key)
		{
			debugOnceLogKeys.Remove(key);
		}

		[Conditional("DEBUG")]
		private void DebugOnlySetInputTrace(string route, string detail, bool pressed)
		{
			lastInputReadRoute = route;
			lastInputReadDetail = detail;
			lastInputReadPressed = pressed;
		}

		[Conditional("DEBUG")]
		private void DebugOnlyLogPoll(KeyCode triggerButton, bool pressedNow)
		{
			if (Time.time < nextDiagnosticsLogTime)
			{
				return;
			}
			nextDiagnosticsLogTime = Time.time + diagnosticsPollInterval;
			CustomKeyEvents.Logger.log.Debug(
				$"[diag] poll button={triggerButton} model={CustomKeyEventsController.Model} pressedNow={pressedNow} triggerPressed={triggerPressed} route={lastInputReadRoute} routePressed={lastInputReadPressed} detail={lastInputReadDetail}");
		}

		[Conditional("DEBUG")]
		private void DebugOnlyLogStateChange(KeyCode triggerButton, bool toPressed, bool pressedNow)
		{
			CustomKeyEvents.Logger.log.Debug(
				$"[diag] transition button={triggerButton} toPressed={toPressed} pressedNow={pressedNow} prevTriggerPressed={triggerPressed} pressTime={pressTime:F3} releaseTime={releaseTime:F3} checkDouble={checkDoubleClick} checkLong={checkLongClick} longClicked={longClicked} route={lastInputReadRoute} detail={lastInputReadDetail}");
		}

		void OnClick()
		{
			//Debug.Log("OnClick");
			CustomKeyEvents.Logger.log.Debug("OnClick");
			clickEvents.Invoke();
		}

		void OnDoubleClick()
		{
			//Debug.Log("OnDoubleClick");
			CustomKeyEvents.Logger.log.Debug("OnDoubleClick");
			doubleClickEvents.Invoke();
		}

		void OnLongClick()
		{
			//Debug.Log("OnLongClick");
			CustomKeyEvents.Logger.log.Debug("OnLongClick");
			longClickEvents.Invoke();
		}

		void OnPress()
		{
			//Debug.Log("OnPress");
			CustomKeyEvents.Logger.log.Debug("OnPress");
			pressEvents.Invoke();
		}

		void OnHold()
		{
			//Debug.Log("OnHold");
			//CustomKeyEvents.Logger.log.Debug("OnHold");
			holdEvents.Invoke();
		}

		void OnRelease()
		{
			//Debug.Log("OnRelease");
			CustomKeyEvents.Logger.log.Debug("OnRelease");
			releaseEvents.Invoke();
		}

		void OnReleaseAfterLongClick()
		{
			//Debug.Log("OnRelease");
			CustomKeyEvents.Logger.log.Debug("OnReleaseAfterLongClick");
			releaseAfterLongClickEvents.Invoke();
		}
	}
}
