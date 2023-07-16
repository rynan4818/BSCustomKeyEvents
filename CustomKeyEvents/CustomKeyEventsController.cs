using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace CustomKeyEvents
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class CustomKeyEventsController : MonoBehaviour
    {
        public static CustomKeyEventsController instance { get; private set; }

        public enum DeviceModel
        {
            None = 0,
            Index = 1,
            Vive = 2,
            Oculus = 3,
            WMR = 4,
        }

        private static volatile int deviceModel = ((int)DeviceModel.None);

        public static DeviceModel Model
        {
            get
            {
                return ((DeviceModel)Enum.ToObject(typeof(DeviceModel), deviceModel));
            }
        }

        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (instance != null)
            {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            instance = this;
            Logger.log?.Debug($"{name}: Awake()");
        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after any other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var device in devices)
            {
                DeviceConnected(device);
            }
        }

        /// <summary>
        /// Called every frame if the script is enabled.
        /// </summary>
        private void Update()
        {

        }

        /// <summary>
        /// Called every frame after every other enabled script's Update().
        /// </summary>
        private void LateUpdate()
        {

        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {
            Logger.log?.Debug($"{name}: OnEnable()");
            InputDevices.deviceConnected += DeviceConnected;
        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            Logger.log?.Debug($"{name}: OnDisable()");
            InputDevices.deviceConnected -= DeviceConnected;
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Logger.log?.Debug($"{name}: OnDestroy()");
            instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

        }
        #endregion

        void DeviceConnected(InputDevice device)
        {
            Logger.log?.Debug("device connected: " + device.name + "(" + device.characteristics.ToString() + "), valid: " + device.isValid);
            if (!device.isValid)
            {
                return;
            }
            if (!device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
            {
                return;
            }
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) || device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                string model = device.name.ToLower();
                if (model.Contains("index"))
                {
                    deviceModel = (int)DeviceModel.Index;
                }
                else if (model.Contains("vive"))
                {
                    deviceModel = (int)DeviceModel.Vive;
                }
                else if (model.Contains("oculus"))
                {
                    deviceModel = (int)DeviceModel.Oculus;
                }
                else
                {
                    deviceModel = (int)DeviceModel.WMR;
                }
            }
        }
    }
}
