using BeatSaberMarkupLanguage;
using HMUI;
using Zenject;

namespace CustomKeyEvents.UI
{
	internal class CustomKeyEventsSettingsFlowCoordinator : FlowCoordinator
	{
		private CustomKeyEventsSettingsListViewController listViewController;

		[Inject]
		public void Construct(CustomKeyEventsSettingsListViewController listViewController)
		{
			this.listViewController = listViewController;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			if (firstActivation)
			{
				SetTitle("Custom Key Events");
				showBackButton = true;
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
