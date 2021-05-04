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

		protected bool checkIndex, checkVive, checkOculus, checkWMR;
		protected const float interval = 0.5f;
		protected const float longClickInterval = 0.6f;
		protected float pressTime;
		protected float releaseTime;
		protected bool invalidDetected = false;
		protected float invalidDetectedTime;
		protected bool checkClick = false;
		protected bool checkDoubleClick = false;
		protected bool checkLongClick = false;
		protected bool longClicked = false;

		private InputDevice leftController;
		private InputDevice rightController;

		protected bool triggerPressed = false;
		protected bool useTriggerValue = true;

		// Use this for initialization
		void Start()
		{
			string model = XRDevice.model != null ? XRDevice.model.ToLower() : "";
			checkIndex = false;
			checkVive = false;
			checkOculus = false;
			checkWMR = false;
			if (model.Contains("index"))
			{
				checkIndex = true;
			}
			else if (model.Contains("vive"))
			{
				checkVive = true;
			}
			else if (model.Contains("oculus"))
			{
				checkOculus = true;
			}
			else
			{
				checkWMR = true;
			}
			CustomKeyEvents.Logger.log.Debug("model: " + model);
			//Debug.Log("model: " + model);
			this.leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			this.rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		}

		// Update is called once per frame
		void Update()
		{
			//foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
			//{
			//	if (Input.GetKeyDown(kcode))
			//		Console.WriteLine("KeyCode down: " + kcode);
			//	if (Input.GetKey(kcode))
			//		Console.WriteLine("KeyCode hold: " + kcode);
			//	if (Input.GetKeyUp(kcode))
			//		Console.WriteLine("KeyCode up: " + kcode);
			//}
			KeyCode triggerButton = (checkIndex)
										   ? (KeyCode)IndexTriggerButton
										   : (checkVive)
										   ? (KeyCode)ViveTriggerButton
										   : (checkOculus)
										   ? (KeyCode)OculusTriggerButton
										   : (checkWMR)
										   ? (KeyCode)WMRTriggerButton
										   : KeyCode.None;
			if (triggerButton == KeyCode.None)
				return;

			if (useTriggerValue && (triggerButton == (KeyCode)ViveButton.LeftTrigger || triggerButton == (KeyCode)ViveButton.RightTrigger))
			{
				if (processControllerTrigger(triggerButton))
				{
					return;
				}
			}
			if (true)
			{
				if (Input.GetKeyDown(triggerButton))
				{
					//Debug.Log(triggerButton + " is pressed");
					CustomKeyEvents.Logger.log.Debug(triggerButton + " is pressed");
					checkDoubleClick = (Time.time - pressTime <= interval);
					pressTime = Time.time;
					OnPress();
					checkLongClick = true;
					checkClick = false;
				}
				if (Input.GetKey(triggerButton))
				{
					//Debug.Log(triggerButton + " is hold");
					//CustomKeyEvents.Logger.log.Debug(buttonAction.name + " is hold");
					OnHold();
					if (checkLongClick && Time.time - pressTime >= longClickInterval)
					{
						CustomKeyEvents.Logger.log.Debug(triggerButton + " is longClicked");
						checkLongClick = false;
						OnLongClick();
						longClicked = true;
					}
				}
				if (Input.GetKeyUp(triggerButton))
				{
					//Debug.Log(triggerButton + " is up");
					CustomKeyEvents.Logger.log.Debug(triggerButton + " is up");
					releaseTime = Time.time;
					OnRelease();
					if (longClicked)
					{
						OnReleaseAfterLongClick();
						longClicked = false;
					}
					//Debug.Log("GetKeyUp : releaseTime - pressTime = " + (releaseTime - pressTime));
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
				if (checkClick && Time.time - releaseTime > interval)
				{
					checkClick = false;
					OnClick();
				}
			}
			

		}

		private bool processControllerTrigger(KeyCode triggerButton)
		{
			// Get triggerValue
			float triggerValue = 0f;
			if (triggerButton == (KeyCode)ViveButton.RightTrigger)
			{
				if (invalidDetected)
				{
					if (Time.time - invalidDetectedTime <= 1.0f)
					{
						// 何もしない
						return true;
					}
					// 取得しなおす
					this.rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
				}
				if (!this.rightController.isValid)
				{
					if (!invalidDetected)
					{
						invalidDetected = true;
						invalidDetectedTime = Time.time;
						CustomKeyEvents.Logger.log.Warn($"Cannot get right controller.");
						// 何もしない
						return true;
					}
					// 何もしない
					return true;
				}
				else
				{
					if (invalidDetected)
					{
						CustomKeyEvents.Logger.log.Info($"== Right controller found. ==");
					}
					invalidDetected = false;
				}
				if (!this.rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue))
				{
					CustomKeyEvents.Logger.log.Warn($"Cannot get right trigger value ({triggerButton}). Use GetKeyDown().");
					useTriggerValue = false;
					return false;
				}
				
				triggerValue = rightTriggerValue;
			}
			else
			{
				if (invalidDetected)
				{
					if (Time.time - invalidDetectedTime <= 1.0f)
					{
						// 何もしない
						return true;
					}
					// 取得しなおす
					this.leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
				}
				if (!this.leftController.isValid)
				{
					if (!invalidDetected)
					{
						invalidDetected = true;
						invalidDetectedTime = Time.time;
						CustomKeyEvents.Logger.log.Warn($"Cannot get left controller.");
						// 何もしない
						return true;
					}
					// 何もしない
					return true;
				}
				else
				{
					if (invalidDetected)
					{
						CustomKeyEvents.Logger.log.Info($"== Left controller found. ==");
					}
					invalidDetected = false;
				}
				if (!this.leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue))
				{
					CustomKeyEvents.Logger.log.Warn($"Cannot get left trigger value ({triggerButton}). Use GetKeyDown().");
					useTriggerValue = false;
					return false;
				}

				triggerValue = leftTriggerValue;
			}
			if (triggerValue > 0.5f)
			{
				if (!triggerPressed)
				{
					CustomKeyEvents.Logger.log.Debug(triggerButton + " is pressed");
					triggerPressed = true;
					checkDoubleClick = (Time.time - pressTime <= interval);
					pressTime = Time.time;
					OnPress();
					checkLongClick = true;
					checkClick = false;
				}
				OnHold();
				if (checkLongClick && Time.time - pressTime >= longClickInterval)
				{
					CustomKeyEvents.Logger.log.Debug(triggerButton + " is longClicked");
					checkLongClick = false;
					OnLongClick();
					longClicked = true;
				}
				return true;
			}
			if (triggerPressed && triggerValue < 0.1f)
			{
				CustomKeyEvents.Logger.log.Debug(triggerButton + " is up");
				//GetKeyUp
				triggerPressed = false;
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
			if (checkClick && Time.time - releaseTime > interval)
			{
				checkClick = false;
				OnClick();
			}
			return true;
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

