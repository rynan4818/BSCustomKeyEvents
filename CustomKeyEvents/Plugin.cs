using CustomKeyEvents.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPALogger = IPA.Logging.Logger;
using SiraUtil.Zenject;

namespace CustomKeyEvents
{
	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin
	{
		internal static Plugin instance { get; private set; }
		internal static string Name => "CustomKeyEvents";

		[Init]
		public void Init(IPALogger logger, Config conf, Zenjector zenjector)
		{
			instance = this;
			Logger.log = logger;
			Logger.log.Debug("Logger initialized.");
			Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
			Logger.log.Debug("Config loaded");

			zenjector.Install<CustomKeyEventsAppInstaller>(Location.App);
			zenjector.Install<CustomKeyEventsMenuInstaller>(Location.Menu);
		}

		[OnStart]
		public void OnApplicationStart()
		{
			Logger.log.Debug("OnApplicationStart");
		}

		[OnExit]
		public void OnApplicationQuit()
		{
			Logger.log.Debug("OnApplicationQuit");
		}
	}
}
