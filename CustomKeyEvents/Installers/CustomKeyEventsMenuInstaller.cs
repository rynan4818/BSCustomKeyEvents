using CustomKeyEvents.UI;
using Zenject;

namespace CustomKeyEvents.Installers
{
	internal class CustomKeyEventsMenuInstaller : Installer
	{
		public override void InstallBindings()
		{
			Container.BindInterfacesAndSelfTo<CustomKeyEventsSettingsListViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<CustomKeyEventsSettingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
			Container.BindInterfacesAndSelfTo<CustomKeyEventsMenuButtonController>().AsSingle().NonLazy();
		}
	}
}
