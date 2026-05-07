using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CustomKeyEvents;
using CustomKeyEvents.Configuration;
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

		public enum EventRouteTarget
		{
			NoChange,
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
			LeftGrip = KeyCode.JoystickButton4,
			RightGrip = KeyCode.JoystickButton5,
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

		[Tooltip("Enable chord press. Trigger button becomes active only while the chord button is also held.")]
		public bool EnableChordPress = false;

		[Tooltip("Additional button required together with trigger button.")]
		public IndexButton IndexChordButton = IndexButton.None;

		[Tooltip("Additional button required together with trigger button.")]
		public ViveButton ViveChordButton = ViveButton.None;

		[Tooltip("Additional button required together with trigger button.")]
		public OculusButton OculusChordButton = OculusButton.None;

		[Tooltip("Additional button required together with trigger button.")]
		public WMRButton WMRChordButton = WMRButton.None;

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

		[Tooltip("Route click events to another event type.")]
		public EventRouteTarget ClickEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route double click events to another event type.")]
		public EventRouteTarget DoubleClickEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route long click events to another event type.")]
		public EventRouteTarget LongClickEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route press events to another event type.")]
		public EventRouteTarget PressEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route hold events to another event type.")]
		public EventRouteTarget HoldEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route release events to another event type.")]
		public EventRouteTarget ReleaseEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Route release-after-long-click events to another event type.")]
		public EventRouteTarget ReleaseAfterLongClickEventsChange = EventRouteTarget.NoChange;

		[Tooltip("Max interval in seconds to treat two short presses as a double click.")]
		public float DoubleClickInterval = defaultDoubleClickInterval;

		[Tooltip("Hold duration in seconds to trigger long click.")]
		public float LongClickInterval = defaultLongClickInterval;

		protected const float defaultDoubleClickInterval = 0.5f;
		protected const float defaultLongClickInterval = 0.6f;
		protected const float minClickInterval = 0.05f;
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
		private KeyCode previousChordButton = KeyCode.None;
		private bool previousChordEnabled = false;
		private readonly HashSet<KeyCode> legacyFallbackLoggedButtons = new HashSet<KeyCode>();
		private readonly HashSet<string> debugOnceLogKeys = new HashSet<string>();
		private string lastInputReadRoute = "none";
		private string lastInputReadDetail = "";
		private bool lastInputReadPressed = false;
		private float nextDiagnosticsLogTime = 0f;
		private float nextReacquireDiagnosticsLogTime = 0f;
		private string lastReacquireDiagnosticsMessage = string.Empty;
		private bool initialDefaultsCaptured = false;
		private string initialKeyConfigurationSignature = string.Empty;
		private IndexButton initialIndexTriggerButton = IndexButton.None;
		private ViveButton initialViveTriggerButton = ViveButton.None;
		private OculusButton initialOculusTriggerButton = OculusButton.None;
		private WMRButton initialWMRTriggerButton = WMRButton.None;
		private bool initialEnableChordPress = false;
		private IndexButton initialIndexChordButton = IndexButton.None;
		private ViveButton initialViveChordButton = ViveButton.None;
		private OculusButton initialOculusChordButton = OculusButton.None;
		private WMRButton initialWMRChordButton = WMRButton.None;
		private EventRouteTarget initialClickEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialDoubleClickEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialLongClickEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialPressEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialHoldEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialReleaseEventsChange = EventRouteTarget.NoChange;
		private EventRouteTarget initialReleaseAfterLongClickEventsChange = EventRouteTarget.NoChange;
		private float initialDoubleClickInterval = defaultDoubleClickInterval;
		private float initialLongClickInterval = defaultLongClickInterval;

		private void Awake()
		{
			CaptureInitialDefaults();
		}

		private void OnEnable()
		{
			CaptureInitialDefaults();
			CustomKeyEventSettingsStore.Register(this);
		}

		private void OnDisable()
		{
			CustomKeyEventSettingsStore.Unregister(this);
		}

		void Start()
		{
			this.leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			this.rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			CaptureInitialDefaults();
			CustomKeyEventSettingsStore.Register(this);
		}

		void Update()
		{
			KeyCode triggerButton = ResolveTriggerButton();
			if (triggerButton == KeyCode.None)
			{
				return;
			}
			float doubleClickInterval = GetEffectiveDoubleClickInterval();
			float longClickInterval = GetEffectiveLongClickInterval();

			KeyCode chordButton = ResolveChordButton();
			bool chordEnabled = EnableChordPress && chordButton != KeyCode.None;

			if (previousTriggerButton != triggerButton || previousChordButton != chordButton || previousChordEnabled != chordEnabled)
			{
				triggerPressed = false;
				checkLongClick = false;
				longClicked = false;
				previousTriggerButton = triggerButton;
				previousChordButton = chordButton;
				previousChordEnabled = chordEnabled;
			}

			bool primaryPressed = GetButtonPressState(triggerButton);
			bool chordPressed = !chordEnabled || GetButtonPressState(chordButton);
			bool pressedNow = primaryPressed && chordPressed;
			DebugOnlyLogPoll(triggerButton, chordButton, pressedNow, primaryPressed, chordPressed, chordEnabled);

			if (pressedNow && !triggerPressed)
			{
				DebugOnlyLogStateChange(triggerButton, true, pressedNow);
				checkDoubleClick = (Time.time - pressTime <= doubleClickInterval);
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
					DebugOnlyLog(triggerButton + " is longClicked");
					checkLongClick = false;
					OnLongClick();
					longClicked = true;
				}
			}

			if (!pressedNow && triggerPressed)
			{
				DebugOnlyLogStateChange(triggerButton, false, pressedNow);
				releaseTime = Time.time;
				OnRelease();
				if (longClicked)
				{
					OnReleaseAfterLongClick();
					longClicked = false;
				}
				if (releaseTime - pressTime <= doubleClickInterval)
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

			if (checkClick && Time.time - releaseTime > doubleClickInterval)
			{
				checkClick = false;
				OnClick();
			}
		}

		public string GetHierarchyPath()
		{
			var pathParts = new Stack<string>();
			for (var current = transform; current != null; current = current.parent)
			{
				pathParts.Push(current.name);
			}

			return string.Join("/", pathParts.ToArray());
		}

		public int GetComponentOrdinal()
		{
			var siblings = GetComponents<CustomKeyEvent>();
			for (var index = 0; index < siblings.Length; index++)
			{
				if (ReferenceEquals(siblings[index], this))
				{
					return index + 1;
				}
			}

			return 1;
		}

		public string GetDisplayLabel()
		{
			var rootName = GetHierarchyPath().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
			return string.IsNullOrWhiteSpace(rootName)
				? $"#{GetComponentOrdinal()}"
				: $"#{GetComponentOrdinal()} {rootName}";
		}

		public string GetDefaultSettingsSummary()
		{
			return $"Trigger(Index={initialIndexTriggerButton}, Vive={initialViveTriggerButton}, Oculus={initialOculusTriggerButton}, WMR={initialWMRTriggerButton}); Chord({(initialEnableChordPress ? "On" : "Off")} Index={initialIndexChordButton}, Vive={initialViveChordButton}, Oculus={initialOculusChordButton}, WMR={initialWMRChordButton}); Timing(Double={initialDoubleClickInterval:F2}, Long={initialLongClickInterval:F2})";
		}

		public string GetKeyConfigurationSignature()
		{
			return BuildKeyConfigurationSignature(
				IndexTriggerButton,
				ViveTriggerButton,
				OculusTriggerButton,
				WMRTriggerButton,
				EnableChordPress,
				IndexChordButton,
				ViveChordButton,
				OculusChordButton,
				WMRChordButton);
		}

		internal string GetStableProfileKey()
		{
			CaptureInitialDefaults();
			return $"{GetHierarchyPath()}|#{GetComponentOrdinal()}|{initialKeyConfigurationSignature}";
		}

		internal string GetInitialKeyConfigurationSignature()
		{
			CaptureInitialDefaults();
			return initialKeyConfigurationSignature;
		}

		public bool HasPersistentEvent(ButtonEventType eventType)
		{
			var unityEvent = GetUnityEvent(eventType);
			return unityEvent != null && unityEvent.GetPersistentEventCount() > 0;
		}

		internal CustomKeyEventProfile CreateProfileSnapshot()
		{
			CaptureInitialDefaults();

			return new CustomKeyEventProfile
			{
				HierarchyPath = GetHierarchyPath(),
				ComponentOrdinal = GetComponentOrdinal(),
				InitialKeyConfigurationSignature = initialKeyConfigurationSignature,
				CurrentKeyConfigurationSignature = GetKeyConfigurationSignature(),
				IndexTriggerButton = IndexTriggerButton,
				ViveTriggerButton = ViveTriggerButton,
				OculusTriggerButton = OculusTriggerButton,
				WMRTriggerButton = WMRTriggerButton,
				EnableChordPress = EnableChordPress,
				IndexChordButton = IndexChordButton,
				ViveChordButton = ViveChordButton,
				OculusChordButton = OculusChordButton,
				WMRChordButton = WMRChordButton,
				ClickEventsChange = ClickEventsChange,
				DoubleClickEventsChange = DoubleClickEventsChange,
				LongClickEventsChange = LongClickEventsChange,
				PressEventsChange = PressEventsChange,
				HoldEventsChange = HoldEventsChange,
				ReleaseEventsChange = ReleaseEventsChange,
				ReleaseAfterLongClickEventsChange = ReleaseAfterLongClickEventsChange,
				DoubleClickInterval = GetEffectiveDoubleClickInterval(),
				LongClickInterval = GetEffectiveLongClickInterval()
			};
		}

		internal void ApplyProfile(CustomKeyEventProfile profile)
		{
			if (profile == null)
			{
				return;
			}

			IndexTriggerButton = profile.IndexTriggerButton;
			ViveTriggerButton = profile.ViveTriggerButton;
			OculusTriggerButton = profile.OculusTriggerButton;
			WMRTriggerButton = profile.WMRTriggerButton;
			EnableChordPress = profile.EnableChordPress;
			IndexChordButton = profile.IndexChordButton;
			ViveChordButton = profile.ViveChordButton;
			OculusChordButton = profile.OculusChordButton;
			WMRChordButton = profile.WMRChordButton;
			ClickEventsChange = profile.ClickEventsChange;
			DoubleClickEventsChange = profile.DoubleClickEventsChange;
			LongClickEventsChange = profile.LongClickEventsChange;
			PressEventsChange = profile.PressEventsChange;
			HoldEventsChange = profile.HoldEventsChange;
			ReleaseEventsChange = profile.ReleaseEventsChange;
			ReleaseAfterLongClickEventsChange = profile.ReleaseAfterLongClickEventsChange;
			DoubleClickInterval = SanitizeInterval(profile.DoubleClickInterval, defaultDoubleClickInterval);
			LongClickInterval = SanitizeInterval(profile.LongClickInterval, defaultLongClickInterval);
			ResetRuntimeState();
		}

		internal void ResetToInitialDefaults()
		{
			CaptureInitialDefaults();

			IndexTriggerButton = initialIndexTriggerButton;
			ViveTriggerButton = initialViveTriggerButton;
			OculusTriggerButton = initialOculusTriggerButton;
			WMRTriggerButton = initialWMRTriggerButton;
			EnableChordPress = initialEnableChordPress;
			IndexChordButton = initialIndexChordButton;
			ViveChordButton = initialViveChordButton;
			OculusChordButton = initialOculusChordButton;
			WMRChordButton = initialWMRChordButton;
			ClickEventsChange = initialClickEventsChange;
			DoubleClickEventsChange = initialDoubleClickEventsChange;
			LongClickEventsChange = initialLongClickEventsChange;
			PressEventsChange = initialPressEventsChange;
			HoldEventsChange = initialHoldEventsChange;
			ReleaseEventsChange = initialReleaseEventsChange;
			ReleaseAfterLongClickEventsChange = initialReleaseAfterLongClickEventsChange;
			DoubleClickInterval = initialDoubleClickInterval;
			LongClickInterval = initialLongClickInterval;
			ResetRuntimeState();
		}

		private void CaptureInitialDefaults()
		{
			if (initialDefaultsCaptured)
			{
				return;
			}

			initialIndexTriggerButton = IndexTriggerButton;
			initialViveTriggerButton = ViveTriggerButton;
			initialOculusTriggerButton = OculusTriggerButton;
			initialWMRTriggerButton = WMRTriggerButton;
			initialEnableChordPress = EnableChordPress;
			initialIndexChordButton = IndexChordButton;
			initialViveChordButton = ViveChordButton;
			initialOculusChordButton = OculusChordButton;
			initialWMRChordButton = WMRChordButton;
			initialClickEventsChange = ClickEventsChange;
			initialDoubleClickEventsChange = DoubleClickEventsChange;
			initialLongClickEventsChange = LongClickEventsChange;
			initialPressEventsChange = PressEventsChange;
			initialHoldEventsChange = HoldEventsChange;
			initialReleaseEventsChange = ReleaseEventsChange;
			initialReleaseAfterLongClickEventsChange = ReleaseAfterLongClickEventsChange;
			initialDoubleClickInterval = SanitizeInterval(DoubleClickInterval, defaultDoubleClickInterval);
			initialLongClickInterval = SanitizeInterval(LongClickInterval, defaultLongClickInterval);
			initialKeyConfigurationSignature = BuildKeyConfigurationSignature(
				initialIndexTriggerButton,
				initialViveTriggerButton,
				initialOculusTriggerButton,
				initialWMRTriggerButton,
				initialEnableChordPress,
				initialIndexChordButton,
				initialViveChordButton,
				initialOculusChordButton,
				initialWMRChordButton);
			initialDefaultsCaptured = true;
		}

		private static string BuildKeyConfigurationSignature(
			IndexButton indexTriggerButton,
			ViveButton viveTriggerButton,
			OculusButton oculusTriggerButton,
			WMRButton wmrTriggerButton,
			bool enableChordPress,
			IndexButton indexChordButton,
			ViveButton viveChordButton,
			OculusButton oculusChordButton,
			WMRButton wmrChordButton)
		{
			return $"IndexTriggerButton={indexTriggerButton};ViveTriggerButton={viveTriggerButton};OculusTriggerButton={oculusTriggerButton};WMRTriggerButton={wmrTriggerButton};EnableChordPress={enableChordPress};IndexChordButton={indexChordButton};ViveChordButton={viveChordButton};OculusChordButton={oculusChordButton};WMRChordButton={wmrChordButton}";
		}

		private float GetEffectiveDoubleClickInterval()
		{
			return SanitizeInterval(DoubleClickInterval, defaultDoubleClickInterval);
		}

		private float GetEffectiveLongClickInterval()
		{
			return SanitizeInterval(LongClickInterval, defaultLongClickInterval);
		}

		private static float SanitizeInterval(float value, float fallback)
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value < minClickInterval)
			{
				return fallback;
			}

			return value;
		}

		private void ResetRuntimeState()
		{
			pressTime = 0f;
			releaseTime = 0f;
			checkClick = false;
			checkDoubleClick = false;
			checkLongClick = false;
			longClicked = false;
			triggerPressed = false;
			previousTriggerButton = KeyCode.None;
			previousChordButton = KeyCode.None;
			previousChordEnabled = false;
			legacyFallbackLoggedButtons.Clear();
			debugOnceLogKeys.Clear();
			lastInputReadRoute = "none";
			lastInputReadDetail = "";
			lastInputReadPressed = false;
			nextDiagnosticsLogTime = 0f;
		}

		private KeyCode ResolveChordButton()
		{
			switch (CustomKeyEventsController.Model)
			{
				case CustomKeyEventsController.DeviceModel.Index:
					return (KeyCode)IndexChordButton;
				case CustomKeyEventsController.DeviceModel.Vive:
					return (KeyCode)ViveChordButton;
				case CustomKeyEventsController.DeviceModel.Oculus:
					return (KeyCode)OculusChordButton;
				case CustomKeyEventsController.DeviceModel.WMR:
					return (KeyCode)WMRChordButton;
			}

			// Keep old avatars working even when model detection fails.
			if (ViveChordButton != ViveButton.None)
			{
				return (KeyCode)ViveChordButton;
			}
			if (IndexChordButton != IndexButton.None)
			{
				return (KeyCode)IndexChordButton;
			}
			if (OculusChordButton != OculusButton.None)
			{
				return (KeyCode)OculusChordButton;
			}
			if (WMRChordButton != WMRButton.None)
			{
				return (KeyCode)WMRChordButton;
			}

			return KeyCode.None;
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
				DebugOnlyLog($"Cannot map {triggerButton} to OpenXR usage. Fallback to Input.GetKey().");
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
				LogOnce($"axis_controller_missing_{node}_{usage.name}", $"Controller unavailable while reading axis {usage.name} on {node}");
				return false;
			}
			ClearLogOnce($"axis_controller_missing_{node}_{usage.name}");

			if (!controller.TryGetFeatureValue(usage, out float axisValue))
			{
				LogOnce($"axis_missing_{node}_{usage.name}", $"Axis feature {usage.name} not available on {node}");
				return false;
			}
			ClearLogOnce($"axis_missing_{node}_{usage.name}");

			float threshold = triggerPressed ? analogReleaseThreshold : analogPressThreshold;
			pressed = axisValue >= threshold;
			return true;
		}

		private bool TryReadBoolFeature(XRNode node, out bool pressed, params InputFeatureUsage<bool>[] usages)
		{
			pressed = false;
			if (!TryGetController(node, out InputDevice controller))
			{
				LogOnce($"bool_controller_missing_{node}", $"Controller unavailable while reading bool feature on {node}");
				return false;
			}
			ClearLogOnce($"bool_controller_missing_{node}");

			for (int i = 0; i < usages.Length; i++)
			{
				if (controller.TryGetFeatureValue(usages[i], out bool value))
				{
					pressed = value;
					ClearLogOnce($"bool_missing_{node}_{BuildUsageList(usages)}");
					return true;
				}
			}
			LogOnce($"bool_missing_{node}_{BuildUsageList(usages)}", $"No bool feature available on {node}. tried={BuildUsageList(usages)}");
			return false;
		}

		private bool TryGetController(XRNode node, out InputDevice controller)
		{
			controller = (node == XRNode.LeftHand) ? leftController : rightController;
			if (!controller.isValid)
			{
				LogOnce($"controller_invalid_initial_{node}", $"Cached controller invalid for {node}. Attempting reacquire.");
				controller = InputDevices.GetDeviceAtXRNode(node);
				if (node == XRNode.LeftHand)
				{
					leftController = controller;
				}
				else
				{
					rightController = controller;
				}
				LogReacquireDiagnostics($"Reacquire {node}: valid={controller.isValid} name='{controller.name}' characteristics={controller.characteristics}");
			}
			if (controller.isValid)
			{
				ClearLogOnce($"controller_invalid_initial_{node}");
			}
			else
			{
				LogOnce($"controller_still_invalid_{node}", $"Controller still invalid for {node} after reacquire.");
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

		private void Log(string message)
		{
			CustomKeyEvents.Logger.log.Warn(message);
		}

		private void LogReacquireDiagnostics(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			if (string.Equals(lastReacquireDiagnosticsMessage, message, StringComparison.Ordinal)
				&& Time.time < nextReacquireDiagnosticsLogTime)
			{
				return;
			}

			lastReacquireDiagnosticsMessage = message;
			nextReacquireDiagnosticsLogTime = Time.time + diagnosticsPollInterval;
			Log(message);
		}

		private void LogOnce(string key, string message)
		{
			if (debugOnceLogKeys.Add(key))
			{
				CustomKeyEvents.Logger.log.Warn(message);
			}
		}

		private void ClearLogOnce(string key)
		{
			debugOnceLogKeys.Remove(key);
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
		private void DebugOnlyLogPoll(KeyCode triggerButton, KeyCode chordButton, bool pressedNow, bool primaryPressed, bool chordPressed, bool chordEnabled)
		{
			if (Time.time < nextDiagnosticsLogTime)
			{
				return;
			}
			nextDiagnosticsLogTime = Time.time + diagnosticsPollInterval;
			CustomKeyEvents.Logger.log.Debug(
				$"[diag] poll button={triggerButton} chord={chordButton} chordEnabled={chordEnabled} pressedNow={pressedNow} primaryPressed={primaryPressed} chordPressed={chordPressed} model={CustomKeyEventsController.Model} triggerPressed={triggerPressed} route={lastInputReadRoute} routePressed={lastInputReadPressed} detail={lastInputReadDetail}");
		}

		[Conditional("DEBUG")]
		private void DebugOnlyLogStateChange(KeyCode triggerButton, bool toPressed, bool pressedNow)
		{
			CustomKeyEvents.Logger.log.Debug(
				$"[diag] transition button={triggerButton} toPressed={toPressed} pressedNow={pressedNow} prevTriggerPressed={triggerPressed} pressTime={pressTime:F3} releaseTime={releaseTime:F3} checkDouble={checkDoubleClick} checkLong={checkLongClick} longClicked={longClicked} route={lastInputReadRoute} detail={lastInputReadDetail}");
		}

		private UnityEvent GetUnityEvent(ButtonEventType eventType)
		{
			switch (eventType)
			{
				case ButtonEventType.Click:
					return clickEvents;
				case ButtonEventType.DoubleClick:
					return doubleClickEvents;
				case ButtonEventType.LongClick:
					return longClickEvents;
				case ButtonEventType.Press:
					return pressEvents;
				case ButtonEventType.Hold:
					return holdEvents;
				case ButtonEventType.Release:
					return releaseEvents;
				case ButtonEventType.ReleaseAfterLongClick:
					return releaseAfterLongClickEvents;
				default:
					return null;
			}
		}

		private EventRouteTarget GetRouteTarget(ButtonEventType sourceEventType)
		{
			switch (sourceEventType)
			{
				case ButtonEventType.Click:
					return ClickEventsChange;
				case ButtonEventType.DoubleClick:
					return DoubleClickEventsChange;
				case ButtonEventType.LongClick:
					return LongClickEventsChange;
				case ButtonEventType.Press:
					return PressEventsChange;
				case ButtonEventType.Hold:
					return HoldEventsChange;
				case ButtonEventType.Release:
					return ReleaseEventsChange;
				case ButtonEventType.ReleaseAfterLongClick:
					return ReleaseAfterLongClickEventsChange;
				default:
					return EventRouteTarget.NoChange;
			}
		}

		private static ButtonEventType ConvertRouteTargetToEventType(EventRouteTarget routeTarget, ButtonEventType fallback)
		{
			switch (routeTarget)
			{
				case EventRouteTarget.Click:
					return ButtonEventType.Click;
				case EventRouteTarget.DoubleClick:
					return ButtonEventType.DoubleClick;
				case EventRouteTarget.LongClick:
					return ButtonEventType.LongClick;
				case EventRouteTarget.Press:
					return ButtonEventType.Press;
				case EventRouteTarget.Hold:
					return ButtonEventType.Hold;
				case EventRouteTarget.Release:
					return ButtonEventType.Release;
				case EventRouteTarget.ReleaseAfterLongClick:
					return ButtonEventType.ReleaseAfterLongClick;
				default:
					return fallback;
			}
		}

		private void InvokeMappedEvent(ButtonEventType sourceEventType)
		{
			var routeTarget = GetRouteTarget(sourceEventType);
			var destinationEventType = ConvertRouteTargetToEventType(routeTarget, sourceEventType);
			DebugOnlyLog($"InvokeMappedEvent source={sourceEventType} destination={destinationEventType}");
			var unityEvent = GetUnityEvent(destinationEventType);
			unityEvent?.Invoke();
		}

		void OnClick()
		{
			DebugOnlyLog("OnClick");
			InvokeMappedEvent(ButtonEventType.Click);
		}

		void OnDoubleClick()
		{
			DebugOnlyLog("OnDoubleClick");
			InvokeMappedEvent(ButtonEventType.DoubleClick);
		}

		void OnLongClick()
		{
			DebugOnlyLog("OnLongClick");
			InvokeMappedEvent(ButtonEventType.LongClick);
		}

		void OnPress()
		{
			DebugOnlyLog("OnPress");
			InvokeMappedEvent(ButtonEventType.Press);
		}

		void OnHold()
		{
			DebugOnlyLog("OnHold");
			InvokeMappedEvent(ButtonEventType.Hold);
		}

		void OnRelease()
		{
			DebugOnlyLog("OnRelease");
			InvokeMappedEvent(ButtonEventType.Release);
		}

		void OnReleaseAfterLongClick()
		{
			DebugOnlyLog("OnReleaseAfterLongClick");
			InvokeMappedEvent(ButtonEventType.ReleaseAfterLongClick);
		}
	}
}
