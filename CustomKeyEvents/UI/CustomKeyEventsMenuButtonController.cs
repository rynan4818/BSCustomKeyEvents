using System.Collections;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using UnityEngine;

namespace CustomKeyEvents.UI
{
	internal class CustomKeyEventsMenuButtonController : MonoBehaviour
	{
		private static CustomKeyEventsMenuButtonController instance;
		private MenuButton menuButton;
		private CustomKeyEventsSettingsFlowCoordinator flowCoordinator;

		public static void Initialize()
		{
			if (instance != null)
			{
				return;
			}

			var gameObject = new GameObject("CustomKeyEventsMenuButtonController");
			GameObject.DontDestroyOnLoad(gameObject);
			instance = gameObject.AddComponent<CustomKeyEventsMenuButtonController>();
		}

		public static void Dispose()
		{
			if (instance == null)
			{
				return;
			}

			instance.Shutdown();
			GameObject.Destroy(instance.gameObject);
			instance = null;
		}

		private void Start()
		{
			StartCoroutine(RegisterWhenReady());
		}

		private IEnumerator RegisterWhenReady()
		{
			while (MenuButtons.instance == null)
			{
				yield return null;
			}

			menuButton = new MenuButton("Custom Key Events", "Inspect loaded CustomKeyEvent components.", ShowFlowCoordinator);
			MenuButtons.instance.RegisterButton(menuButton);
		}

		private void OnDestroy()
		{
			Shutdown();
		}

		private void Shutdown()
		{
			if (menuButton != null && MenuButtons.instance != null)
			{
				MenuButtons.instance.UnregisterButton(menuButton);
				menuButton = null;
			}

			flowCoordinator = null;
		}

		private void ShowFlowCoordinator()
		{
			if (BeatSaberUI.MainFlowCoordinator == null)
			{
				return;
			}

			if (flowCoordinator == null)
			{
				flowCoordinator = BeatSaberUI.CreateFlowCoordinator<CustomKeyEventsSettingsFlowCoordinator>();
			}

			BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(flowCoordinator);
		}
	}
}