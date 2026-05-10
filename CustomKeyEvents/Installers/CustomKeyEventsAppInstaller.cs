using Zenject;

namespace CustomKeyEvents.Installers
{
	internal class CustomKeyEventsAppInstaller : Installer
	{
		public override void InstallBindings()
		{
			Container.BindInterfacesAndSelfTo<CustomKeyEventsController>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
		}
	}
}
