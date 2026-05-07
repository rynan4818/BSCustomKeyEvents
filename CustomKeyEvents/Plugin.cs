using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomKeyEvents.UI;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace CustomKeyEvents
{

	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin
	{
		internal static Plugin instance { get; private set; }
		internal static string Name => "CustomKeyEvents";

		[Init]
		public void Init(IPALogger logger, Config conf)
		{
			instance = this;
			Logger.log = logger;
			Logger.log.Debug("Logger initialized.");
			Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
			Logger.log.Debug("Config loaded");
		}

		[OnStart]
		public void OnApplicationStart()
		{
			Logger.log.Debug("OnApplicationStart");
			new GameObject("CustomKeyEventsController").AddComponent<CustomKeyEventsController>();
			CustomKeyEventsMenuButtonController.Initialize();

		}

		[OnExit]
		public void OnApplicationQuit()
		{
			Logger.log.Debug("OnApplicationQuit");
			CustomKeyEventsMenuButtonController.Dispose();

		}
	}
}
