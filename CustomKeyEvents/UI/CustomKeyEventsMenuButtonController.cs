using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace CustomKeyEvents.UI
{
	internal class CustomKeyEventsMenuButtonController : IInitializable, IDisposable
	{
		private readonly CustomKeyEventsSettingsFlowCoordinator flowCoordinator;
		private MenuButton menuButton;

		[Inject]
		public CustomKeyEventsMenuButtonController(CustomKeyEventsSettingsFlowCoordinator flowCoordinator)
		{
			this.flowCoordinator = flowCoordinator;
		}

		public void Initialize()
		{
			menuButton = new MenuButton("Custom Key Events", "Inspect loaded CustomKeyEvent components.", ShowFlowCoordinator);
			MenuButtons.instance?.RegisterButton(menuButton);
		}

		public void Dispose()
		{
			if (menuButton != null)
			{
				MenuButtons.instance?.UnregisterButton(menuButton);
				menuButton = null;
			}
		}

		private void ShowFlowCoordinator()
		{
			BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(flowCoordinator);
		}
	}
}
