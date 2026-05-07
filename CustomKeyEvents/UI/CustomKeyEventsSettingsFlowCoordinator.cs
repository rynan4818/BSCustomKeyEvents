using BeatSaberMarkupLanguage;
using HMUI;

namespace CustomKeyEvents.UI
{
	internal class CustomKeyEventsSettingsFlowCoordinator : FlowCoordinator
	{
		private CustomKeyEventsSettingsListViewController listViewController;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			if (firstActivation)
			{
				SetTitle("Custom Key Events");
				showBackButton = true;
				listViewController = BeatSaberUI.CreateViewController<CustomKeyEventsSettingsListViewController>();
				ProvideInitialViewControllers(listViewController);
			}
		}

		protected override void BackButtonWasPressed(ViewController topViewController)
		{
			BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
			base.BackButtonWasPressed(topViewController);
		}
	}
}