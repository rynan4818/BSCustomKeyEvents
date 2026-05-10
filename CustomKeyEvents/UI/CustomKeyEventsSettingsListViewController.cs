using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AvatarScriptPack;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomKeyEvents.Configuration;
using CustomKeyEvents.Models;
using HMUI;
using IPA.Utilities;
using TMPro;
using UnityEngine;

namespace CustomKeyEvents.UI
{
	[HotReload]
	internal class CustomKeyEventsSettingsListViewController : BSMLAutomaticViewController
	{
		private readonly List<object> componentOptions = new List<object>();
		private readonly List<object> indexButtonOptions = BuildEnumOptions(typeof(CustomKeyEvent.IndexButton));
		private readonly List<object> viveButtonOptions = BuildEnumOptions(typeof(CustomKeyEvent.ViveButton));
		private readonly List<object> oculusButtonOptions = BuildEnumOptions(typeof(CustomKeyEvent.OculusButton));
		private readonly List<object> wmrButtonOptions = BuildEnumOptions(typeof(CustomKeyEvent.WMRButton));
		private readonly List<object> clickEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.Click);
		private readonly List<object> doubleClickEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.DoubleClick);
		private readonly List<object> longClickEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.LongClick);
		private readonly List<object> pressEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.Press);
		private readonly List<object> holdEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.Hold);
		private readonly List<object> releaseEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.Release);
		private readonly List<object> releaseAfterLongClickEventsChangeOptions = BuildEventRouteOptions(CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick);
		private const int LiveMonitorRecentEventDisplayCount = 5;
		private CustomKeyEventOption selectedComponentOption;
		private bool isRuntimeEventObserverSubscribed;
		private string liveMonitorTargetStableKey = string.Empty;
		private string liveMonitorLastEventText = "(n/a)";
		private string liveMonitorRecentEventsText = "(n/a)";
		private float liveMonitorSessionStartRealtime;

		[UIComponent("component-dropdown")]
		public DropDownListSetting componentDropdown;

		[UIComponent("index-trigger-button-dropdown")]
		public DropDownListSetting indexTriggerButtonDropdown;

		[UIComponent("vive-trigger-button-dropdown")]
		public DropDownListSetting viveTriggerButtonDropdown;

		[UIComponent("oculus-trigger-button-dropdown")]
		public DropDownListSetting oculusTriggerButtonDropdown;

		[UIComponent("wmr-trigger-button-dropdown")]
		public DropDownListSetting wmrTriggerButtonDropdown;

		[UIComponent("enable-chord-press-toggle")]
		public ToggleSetting enableChordPressToggle;

		[UIComponent("include-hierarchy-path-toggle")]
		public ToggleSetting includeHierarchyPathToggle;

		[UIComponent("index-chord-button-dropdown")]
		public DropDownListSetting indexChordButtonDropdown;

		[UIComponent("vive-chord-button-dropdown")]
		public DropDownListSetting viveChordButtonDropdown;

		[UIComponent("oculus-chord-button-dropdown")]
		public DropDownListSetting oculusChordButtonDropdown;

		[UIComponent("wmr-chord-button-dropdown")]
		public DropDownListSetting wmrChordButtonDropdown;

		[UIComponent("click-events-change-dropdown")]
		public DropDownListSetting clickEventsChangeDropdown;

		[UIComponent("double-click-events-change-dropdown")]
		public DropDownListSetting doubleClickEventsChangeDropdown;

		[UIComponent("long-click-events-change-dropdown")]
		public DropDownListSetting longClickEventsChangeDropdown;

		[UIComponent("press-events-change-dropdown")]
		public DropDownListSetting pressEventsChangeDropdown;

		[UIComponent("hold-events-change-dropdown")]
		public DropDownListSetting holdEventsChangeDropdown;

		[UIComponent("release-events-change-dropdown")]
		public DropDownListSetting releaseEventsChangeDropdown;

		[UIComponent("release-after-long-click-events-change-dropdown")]
		public DropDownListSetting releaseAfterLongClickEventsChangeDropdown;

		[UIComponent("double-click-interval-setting")]
		public IncrementSetting doubleClickIntervalSetting;

		[UIComponent("long-click-interval-setting")]
		public IncrementSetting longClickIntervalSetting;

		public CustomKeyEventsSettingsListViewController()
		{
			componentOptions.Add(CustomKeyEventCatalog.NoComponent);
			selectedComponentOption = CustomKeyEventCatalog.NoComponent;
		}

		[UIAction("#post-parse")]
		public void PostParse()
		{
			InitializeEnumDropdowns();
			RefreshComponents();
			ConfigureAllDropdownSelectedLabels();
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			liveMonitorSessionStartRealtime = Time.unscaledTime;
			CustomKeyEventSettingsStore.SetRuntimeEventMonitorEnabled(true);
			SubscribeRuntimeEventObserver();
			RefreshLiveEventMonitorForSelectedTarget();
			NotifyLiveEventMonitorProperties();
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			UnsubscribeRuntimeEventObserver();
			CustomKeyEventSettingsStore.SetRuntimeEventMonitorEnabled(false);
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
		}

		[UIValue("componentOptions")]
		public List<object> ComponentOptions
		{
			get
			{
				EnsureComponentOptionsInitialized();
				return componentOptions;
			}
		}

		[UIValue("selectedComponent")]
		public object SelectedComponent
		{
			get => selectedComponentOption ?? CustomKeyEventCatalog.NoComponent;
			set
			{
				var selected = ResolveComponentOption(value);
				if (selected == null || ReferenceEquals(selectedComponentOption, selected))
				{
					return;
				}

				selectedComponentOption = selected;
				NotifySelectedComponentProperties();
			}
		}

		[UIValue("selectedComponentPath")]
		public string SelectedComponentPath => string.IsNullOrWhiteSpace(selectedComponentOption?.HierarchyPath)
			? "(no component selected)"
			: selectedComponentOption.HierarchyPath;

		[UIValue("selectedComponentOrdinal")]
		public string SelectedComponentOrdinal => selectedComponentOption == null || selectedComponentOption.ComponentOrdinal <= 0
			? "(n/a)"
			: $"#{selectedComponentOption.ComponentOrdinal}";

		[UIValue("selectedComponentActiveDuration")]
		public string SelectedComponentActiveDuration
		{
			get
			{
				if (ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent) || selectedComponentOption == null)
				{
					return "(n/a)";
				}

				if (selectedComponentOption.ActiveDurationSeconds.HasValue)
				{
					return $"{selectedComponentOption.ActiveDurationSeconds.Value:F1}s";
				}

				return "(unloaded)";
			}
		}

		[UIValue("selectedTargetDisplay")]
		public string SelectedTargetDisplay
		{
			get
			{
				if (ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent) || selectedComponentOption == null)
				{
					return "(no component selected)";
				}

				var ordinalText = selectedComponentOption.ComponentOrdinal > 0
					? $"#{selectedComponentOption.ComponentOrdinal}"
					: "#?";
				var hierarchyPath = string.IsNullOrWhiteSpace(selectedComponentOption.HierarchyPath)
					? "(no path)"
					: selectedComponentOption.HierarchyPath;
				var activeDurationText = selectedComponentOption.ActiveDurationSeconds.HasValue
					? $"{selectedComponentOption.ActiveDurationSeconds.Value:F1}sec"
					: "unloaded";
				return $"{ordinalText} {hierarchyPath}@{activeDurationText}";
			}
		}

		[UIValue("selectedComponentSummary")]
		public string SelectedComponentSummary => GetSelectedComponent()?.GetDefaultSettingsSummary()
			?? BuildStoredProfileSummary(GetSelectedProfile())
			?? selectedComponentOption?.DefaultSummary
			?? "No CustomKeyEvent targets are currently discovered.";

		[UIValue("selectedComponentLiveLastEvent")]
		public string SelectedComponentLiveLastEvent => HasSelectedTarget
			? liveMonitorLastEventText
			: "(n/a)";

		[UIValue("selectedComponentLiveRecentEvents")]
		public string SelectedComponentLiveRecentEvents => HasSelectedTarget
			? liveMonitorRecentEventsText
			: "(n/a)";

		[UIValue("hasSelectedComponent")]
		public bool HasSelectedComponent => GetSelectedComponent() != null;

		[UIValue("hasSelectedTarget")]
		public bool HasSelectedTarget => HasSelectedComponent || GetSelectedProfile() != null;

		[UIValue("includeHierarchyPathInIdentity")]
		public bool IncludeHierarchyPathInIdentity
		{
			get => PluginConfig.Instance?.IncludeHierarchyPathInIdentity ?? false;
			set
			{
				if (PluginConfig.Instance == null || PluginConfig.Instance.IncludeHierarchyPathInIdentity == value)
				{
					return;
				}

				PluginConfig.Instance.IncludeHierarchyPathInIdentity = value;
				PluginConfig.Instance.Changed();
				CustomKeyEventSettingsStore.RefreshRegisteredStableKeys();
				CustomKeyEventSettingsStore.RebindLoadedSceneComponents();
				RefreshComponents();
				NotifyPropertyChanged(nameof(IncludeHierarchyPathInIdentity));
				includeHierarchyPathToggle?.ReceiveValue();
			}
		}

		[UIValue("resetActionButtonText")]
		public string ResetActionButtonText
		{
			get
			{
				if (!HasSelectedTarget)
				{
					return "Reset to Defaults";
				}

				if (HasSelectedComponent || (selectedComponentOption?.ActiveDurationSeconds.HasValue ?? false))
				{
					return "Reset to Defaults";
				}

				return "Delete Stored Settings";
			}
		}

		[UIValue("hasClickEvents")]
		public bool HasClickEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.Click);

		[UIValue("hasDoubleClickEvents")]
		public bool HasDoubleClickEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.DoubleClick);

		[UIValue("hasLongClickEvents")]
		public bool HasLongClickEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.LongClick);

		[UIValue("hasPressEvents")]
		public bool HasPressEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.Press);

		[UIValue("hasHoldEvents")]
		public bool HasHoldEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.Hold);

		[UIValue("hasReleaseEvents")]
		public bool HasReleaseEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.Release);

		[UIValue("hasReleaseAfterLongClickEvents")]
		public bool HasReleaseAfterLongClickEvents => HasPersistentEvent(CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick);

		[UIValue("hasAnyRoutableEvents")]
		public bool HasAnyRoutableEvents => HasClickEvents || HasDoubleClickEvents || HasLongClickEvents || HasPressEvents || HasHoldEvents || HasReleaseEvents || HasReleaseAfterLongClickEvents;

		[UIValue("indexTriggerButtonOptions")]
		public List<object> IndexTriggerButtonOptions => indexButtonOptions;

		[UIValue("viveTriggerButtonOptions")]
		public List<object> ViveTriggerButtonOptions => viveButtonOptions;

		[UIValue("oculusTriggerButtonOptions")]
		public List<object> OculusTriggerButtonOptions => oculusButtonOptions;

		[UIValue("wmrTriggerButtonOptions")]
		public List<object> WmrTriggerButtonOptions => wmrButtonOptions;

		[UIValue("indexChordButtonOptions")]
		public List<object> IndexChordButtonOptions => indexButtonOptions;

		[UIValue("viveChordButtonOptions")]
		public List<object> ViveChordButtonOptions => viveButtonOptions;

		[UIValue("oculusChordButtonOptions")]
		public List<object> OculusChordButtonOptions => oculusButtonOptions;

		[UIValue("wmrChordButtonOptions")]
		public List<object> WmrChordButtonOptions => wmrButtonOptions;

		[UIValue("clickEventsChangeOptions")]
		public List<object> ClickEventsChangeOptions => clickEventsChangeOptions;

		[UIValue("doubleClickEventsChangeOptions")]
		public List<object> DoubleClickEventsChangeOptions => doubleClickEventsChangeOptions;

		[UIValue("longClickEventsChangeOptions")]
		public List<object> LongClickEventsChangeOptions => longClickEventsChangeOptions;

		[UIValue("pressEventsChangeOptions")]
		public List<object> PressEventsChangeOptions => pressEventsChangeOptions;

		[UIValue("holdEventsChangeOptions")]
		public List<object> HoldEventsChangeOptions => holdEventsChangeOptions;

		[UIValue("releaseEventsChangeOptions")]
		public List<object> ReleaseEventsChangeOptions => releaseEventsChangeOptions;

		[UIValue("releaseAfterLongClickEventsChangeOptions")]
		public List<object> ReleaseAfterLongClickEventsChangeOptions => releaseAfterLongClickEventsChangeOptions;

		[UIValue("indexTriggerButton")]
		public object IndexTriggerButton
		{
			get => GetSelectedValue(component => component.IndexTriggerButton, profile => profile.IndexTriggerButton, CustomKeyEvent.IndexButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.IndexTriggerButton);
					if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(component.IndexTriggerButton, selected))
					{
						return false;
					}

					component.IndexTriggerButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.IndexTriggerButton);
					if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(profile.IndexTriggerButton, selected))
					{
						return false;
					}

					profile.IndexTriggerButton = selected;
					return true;
				});
		}

		[UIValue("viveTriggerButton")]
		public object ViveTriggerButton
		{
			get => GetSelectedValue(component => component.ViveTriggerButton, profile => profile.ViveTriggerButton, CustomKeyEvent.ViveButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.ViveTriggerButton);
					if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(component.ViveTriggerButton, selected))
					{
						return false;
					}

					component.ViveTriggerButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.ViveTriggerButton);
					if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(profile.ViveTriggerButton, selected))
					{
						return false;
					}

					profile.ViveTriggerButton = selected;
					return true;
				});
		}

		[UIValue("oculusTriggerButton")]
		public object OculusTriggerButton
		{
			get => GetSelectedValue(component => component.OculusTriggerButton, profile => profile.OculusTriggerButton, CustomKeyEvent.OculusButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.OculusTriggerButton);
					if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(component.OculusTriggerButton, selected))
					{
						return false;
					}

					component.OculusTriggerButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.OculusTriggerButton);
					if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(profile.OculusTriggerButton, selected))
					{
						return false;
					}

					profile.OculusTriggerButton = selected;
					return true;
				});
		}

		[UIValue("wmrTriggerButton")]
		public object WmrTriggerButton
		{
			get => GetSelectedValue(component => component.WMRTriggerButton, profile => profile.WMRTriggerButton, CustomKeyEvent.WMRButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.WMRTriggerButton);
					if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(component.WMRTriggerButton, selected))
					{
						return false;
					}

					component.WMRTriggerButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.WMRTriggerButton);
					if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(profile.WMRTriggerButton, selected))
					{
						return false;
					}

					profile.WMRTriggerButton = selected;
					return true;
				});
		}

		[UIValue("enableChordPress")]
		public bool EnableChordPress
		{
			get => GetSelectedValue(component => component.EnableChordPress, profile => profile.EnableChordPress, false);
			set => ApplySelectedComponentChange(
				component =>
				{
					if (component.EnableChordPress == value)
					{
						return false;
					}

					component.EnableChordPress = value;
					return true;
				},
				profile =>
				{
					if (profile.EnableChordPress == value)
					{
						return false;
					}

					profile.EnableChordPress = value;
					return true;
				});
		}

		[UIValue("indexChordButton")]
		public object IndexChordButton
		{
			get => GetSelectedValue(component => component.IndexChordButton, profile => profile.IndexChordButton, CustomKeyEvent.IndexButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.IndexChordButton);
					if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(component.IndexChordButton, selected))
					{
						return false;
					}

					component.IndexChordButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.IndexChordButton);
					if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(profile.IndexChordButton, selected))
					{
						return false;
					}

					profile.IndexChordButton = selected;
					return true;
				});
		}

		[UIValue("viveChordButton")]
		public object ViveChordButton
		{
			get => GetSelectedValue(component => component.ViveChordButton, profile => profile.ViveChordButton, CustomKeyEvent.ViveButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.ViveChordButton);
					if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(component.ViveChordButton, selected))
					{
						return false;
					}

					component.ViveChordButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.ViveChordButton);
					if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(profile.ViveChordButton, selected))
					{
						return false;
					}

					profile.ViveChordButton = selected;
					return true;
				});
		}

		[UIValue("oculusChordButton")]
		public object OculusChordButton
		{
			get => GetSelectedValue(component => component.OculusChordButton, profile => profile.OculusChordButton, CustomKeyEvent.OculusButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.OculusChordButton);
					if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(component.OculusChordButton, selected))
					{
						return false;
					}

					component.OculusChordButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.OculusChordButton);
					if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(profile.OculusChordButton, selected))
					{
						return false;
					}

					profile.OculusChordButton = selected;
					return true;
				});
		}

		[UIValue("wmrChordButton")]
		public object WmrChordButton
		{
			get => GetSelectedValue(component => component.WMRChordButton, profile => profile.WMRChordButton, CustomKeyEvent.WMRButton.None);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEnumValue(value, component.WMRChordButton);
					if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(component.WMRChordButton, selected))
					{
						return false;
					}

					component.WMRChordButton = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEnumValue(value, profile.WMRChordButton);
					if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(profile.WMRChordButton, selected))
					{
						return false;
					}

					profile.WMRChordButton = selected;
					return true;
				});
		}

		[UIValue("clickEventsChange")]
		public object ClickEventsChange
		{
			get => FindEventRouteOption(clickEventsChangeOptions, GetSelectedValue(component => component.ClickEventsChange, profile => profile.ClickEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.ClickEventsChange, clickEventsChangeOptions);
					if (component.ClickEventsChange == selected)
					{
						return false;
					}

					component.ClickEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.ClickEventsChange, clickEventsChangeOptions);
					if (profile.ClickEventsChange == selected)
					{
						return false;
					}

					profile.ClickEventsChange = selected;
					return true;
				});
		}

		[UIValue("doubleClickEventsChange")]
		public object DoubleClickEventsChange
		{
			get => FindEventRouteOption(doubleClickEventsChangeOptions, GetSelectedValue(component => component.DoubleClickEventsChange, profile => profile.DoubleClickEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.DoubleClickEventsChange, doubleClickEventsChangeOptions);
					if (component.DoubleClickEventsChange == selected)
					{
						return false;
					}

					component.DoubleClickEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.DoubleClickEventsChange, doubleClickEventsChangeOptions);
					if (profile.DoubleClickEventsChange == selected)
					{
						return false;
					}

					profile.DoubleClickEventsChange = selected;
					return true;
				});
		}

		[UIValue("longClickEventsChange")]
		public object LongClickEventsChange
		{
			get => FindEventRouteOption(longClickEventsChangeOptions, GetSelectedValue(component => component.LongClickEventsChange, profile => profile.LongClickEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.LongClickEventsChange, longClickEventsChangeOptions);
					if (component.LongClickEventsChange == selected)
					{
						return false;
					}

					component.LongClickEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.LongClickEventsChange, longClickEventsChangeOptions);
					if (profile.LongClickEventsChange == selected)
					{
						return false;
					}

					profile.LongClickEventsChange = selected;
					return true;
				});
		}

		[UIValue("pressEventsChange")]
		public object PressEventsChange
		{
			get => FindEventRouteOption(pressEventsChangeOptions, GetSelectedValue(component => component.PressEventsChange, profile => profile.PressEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.PressEventsChange, pressEventsChangeOptions);
					if (component.PressEventsChange == selected)
					{
						return false;
					}

					component.PressEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.PressEventsChange, pressEventsChangeOptions);
					if (profile.PressEventsChange == selected)
					{
						return false;
					}

					profile.PressEventsChange = selected;
					return true;
				});
		}

		[UIValue("holdEventsChange")]
		public object HoldEventsChange
		{
			get => FindEventRouteOption(holdEventsChangeOptions, GetSelectedValue(component => component.HoldEventsChange, profile => profile.HoldEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.HoldEventsChange, holdEventsChangeOptions);
					if (component.HoldEventsChange == selected)
					{
						return false;
					}

					component.HoldEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.HoldEventsChange, holdEventsChangeOptions);
					if (profile.HoldEventsChange == selected)
					{
						return false;
					}

					profile.HoldEventsChange = selected;
					return true;
				});
		}

		[UIValue("releaseEventsChange")]
		public object ReleaseEventsChange
		{
			get => FindEventRouteOption(releaseEventsChangeOptions, GetSelectedValue(component => component.ReleaseEventsChange, profile => profile.ReleaseEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.ReleaseEventsChange, releaseEventsChangeOptions);
					if (component.ReleaseEventsChange == selected)
					{
						return false;
					}

					component.ReleaseEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.ReleaseEventsChange, releaseEventsChangeOptions);
					if (profile.ReleaseEventsChange == selected)
					{
						return false;
					}

					profile.ReleaseEventsChange = selected;
					return true;
				});
		}

		[UIValue("releaseAfterLongClickEventsChange")]
		public object ReleaseAfterLongClickEventsChange
		{
			get => FindEventRouteOption(releaseAfterLongClickEventsChangeOptions, GetSelectedValue(component => component.ReleaseAfterLongClickEventsChange, profile => profile.ReleaseAfterLongClickEventsChange, CustomKeyEvent.EventRouteTarget.NoChange));
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveEventRouteValue(value, component.ReleaseAfterLongClickEventsChange, releaseAfterLongClickEventsChangeOptions);
					if (component.ReleaseAfterLongClickEventsChange == selected)
					{
						return false;
					}

					component.ReleaseAfterLongClickEventsChange = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveEventRouteValue(value, profile.ReleaseAfterLongClickEventsChange, releaseAfterLongClickEventsChangeOptions);
					if (profile.ReleaseAfterLongClickEventsChange == selected)
					{
						return false;
					}

					profile.ReleaseAfterLongClickEventsChange = selected;
					return true;
				});
		}

		[UIValue("doubleClickInterval")]
		public float DoubleClickInterval
		{
			get => GetSelectedValue(component => component.DoubleClickInterval, profile => profile.DoubleClickInterval, 0.5f);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveFloatValue(value, component.DoubleClickInterval);
					if (Math.Abs(component.DoubleClickInterval - selected) < 0.0001f)
					{
						return false;
					}

					component.DoubleClickInterval = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveFloatValue(value, profile.DoubleClickInterval);
					if (Math.Abs(profile.DoubleClickInterval - selected) < 0.0001f)
					{
						return false;
					}

					profile.DoubleClickInterval = selected;
					return true;
				});
		}

		[UIValue("longClickInterval")]
		public float LongClickInterval
		{
			get => GetSelectedValue(component => component.LongClickInterval, profile => profile.LongClickInterval, 0.6f);
			set => ApplySelectedComponentChange(
				component =>
				{
					var selected = ResolveFloatValue(value, component.LongClickInterval);
					if (Math.Abs(component.LongClickInterval - selected) < 0.0001f)
					{
						return false;
					}

					component.LongClickInterval = selected;
					return true;
				},
				profile =>
				{
					var selected = ResolveFloatValue(value, profile.LongClickInterval);
					if (Math.Abs(profile.LongClickInterval - selected) < 0.0001f)
					{
						return false;
					}

					profile.LongClickInterval = selected;
					return true;
				});
		}

		[UIAction("refresh-components")]
		public void RefreshComponents()
		{
			var previousKey = selectedComponentOption?.IdentityKey;
			var discoveredOptions = CustomKeyEventCatalog.Discover();

			componentOptions.Clear();
			componentOptions.AddRange(discoveredOptions.Cast<object>());

			selectedComponentOption = FindOptionByKey(previousKey)
				?? discoveredOptions.FirstOrDefault()
				?? CustomKeyEventCatalog.NoComponent;

			RefreshDropdown(componentDropdown, componentOptions);
			NotifyPropertyChanged(nameof(ComponentOptions));
			NotifyPropertyChanged(nameof(IncludeHierarchyPathInIdentity));
			includeHierarchyPathToggle?.ReceiveValue();
			NotifySelectedComponentProperties();
		}

		[UIAction("reset-selected-component")]
		public void ResetSelectedComponent()
		{
			var component = GetSelectedComponent();
			if (component != null)
			{
				component.ResetToInitialDefaults();
				CustomKeyEventSettingsStore.SaveComponent(component);
				SyncSelectedStoredProfile(ResolveStableKeyForComponent(component));
				NotifySelectedComponentProperties();
				return;
			}

			if (selectedComponentOption == null
				|| ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent)
				|| string.IsNullOrWhiteSpace(selectedComponentOption.IdentityKey))
			{
				return;
			}

			CustomKeyEventSettingsStore.RemoveProfile(selectedComponentOption.IdentityKey);
			selectedComponentOption.SetStoredProfile(null);
			RefreshComponents();
		}

		private void InitializeEnumDropdowns()
		{
			RefreshDropdown(indexTriggerButtonDropdown, indexButtonOptions);
			RefreshDropdown(viveTriggerButtonDropdown, viveButtonOptions);
			RefreshDropdown(oculusTriggerButtonDropdown, oculusButtonOptions);
			RefreshDropdown(wmrTriggerButtonDropdown, wmrButtonOptions);
			RefreshDropdown(indexChordButtonDropdown, indexButtonOptions);
			RefreshDropdown(viveChordButtonDropdown, viveButtonOptions);
			RefreshDropdown(oculusChordButtonDropdown, oculusButtonOptions);
			RefreshDropdown(wmrChordButtonDropdown, wmrButtonOptions);
			RefreshDropdown(clickEventsChangeDropdown, clickEventsChangeOptions);
			RefreshDropdown(doubleClickEventsChangeDropdown, doubleClickEventsChangeOptions);
			RefreshDropdown(longClickEventsChangeDropdown, longClickEventsChangeOptions);
			RefreshDropdown(pressEventsChangeDropdown, pressEventsChangeOptions);
			RefreshDropdown(holdEventsChangeDropdown, holdEventsChangeOptions);
			RefreshDropdown(releaseEventsChangeDropdown, releaseEventsChangeOptions);
			RefreshDropdown(releaseAfterLongClickEventsChangeDropdown, releaseAfterLongClickEventsChangeOptions);
		}

		private void ConfigureAllDropdownSelectedLabels()
		{
			ConfigureDropdownSelectedLabel(componentDropdown);
			ConfigureDropdownSelectedLabel(indexTriggerButtonDropdown);
			ConfigureDropdownSelectedLabel(viveTriggerButtonDropdown);
			ConfigureDropdownSelectedLabel(oculusTriggerButtonDropdown);
			ConfigureDropdownSelectedLabel(wmrTriggerButtonDropdown);
			ConfigureDropdownSelectedLabel(indexChordButtonDropdown);
			ConfigureDropdownSelectedLabel(viveChordButtonDropdown);
			ConfigureDropdownSelectedLabel(oculusChordButtonDropdown);
			ConfigureDropdownSelectedLabel(wmrChordButtonDropdown);
			ConfigureDropdownSelectedLabel(clickEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(doubleClickEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(longClickEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(pressEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(holdEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(releaseEventsChangeDropdown);
			ConfigureDropdownSelectedLabel(releaseAfterLongClickEventsChangeDropdown);
		}

		private void EnsureComponentOptionsInitialized()
		{
			if (componentOptions.Count > 0)
			{
				return;
			}

			RefreshComponents();
		}

		private CustomKeyEvent GetSelectedComponent()
		{
			return selectedComponentOption?.Component;
		}

		private CustomKeyEventProfile GetSelectedProfile()
		{
			if (selectedComponentOption == null || ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent))
			{
				return null;
			}

			if (selectedComponentOption.StoredProfile != null)
			{
				return selectedComponentOption.StoredProfile;
			}

			if (CustomKeyEventSettingsStore.TryGetProfile(selectedComponentOption.IdentityKey, out var profile))
			{
				selectedComponentOption.SetStoredProfile(profile);
				return profile;
			}

			return null;
		}

		private bool HasPersistentEvent(CustomKeyEvent.ButtonEventType eventType)
		{
			var component = GetSelectedComponent();
			if (component != null)
			{
				return component.HasPersistentEvent(eventType);
			}

			var profile = GetSelectedProfile();
			if (profile == null)
			{
				return false;
			}

			switch (eventType)
			{
				case CustomKeyEvent.ButtonEventType.Click:
					return profile.HasClickEvents;
				case CustomKeyEvent.ButtonEventType.DoubleClick:
					return profile.HasDoubleClickEvents;
				case CustomKeyEvent.ButtonEventType.LongClick:
					return profile.HasLongClickEvents;
				case CustomKeyEvent.ButtonEventType.Press:
					return profile.HasPressEvents;
				case CustomKeyEvent.ButtonEventType.Hold:
					return profile.HasHoldEvents;
				case CustomKeyEvent.ButtonEventType.Release:
					return profile.HasReleaseEvents;
				case CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick:
					return profile.HasReleaseAfterLongClickEvents;
				default:
					return false;
			}
		}

		private void ApplySelectedComponentChange(Func<CustomKeyEvent, bool> update)
		{
			var component = GetSelectedComponent();
			if (component == null || update == null || !update(component))
			{
				return;
			}

			CustomKeyEventSettingsStore.SaveComponent(component);
			SyncSelectedStoredProfile(ResolveStableKeyForComponent(component));
			NotifySelectedComponentProperties();
		}

		private void ApplySelectedComponentChange(Func<CustomKeyEvent, bool> updateComponent, Func<CustomKeyEventProfile, bool> updateProfile)
		{
			var component = GetSelectedComponent();
			if (component != null)
			{
				if (updateComponent == null || !updateComponent(component))
				{
					return;
				}

				CustomKeyEventSettingsStore.SaveComponent(component);
				SyncSelectedStoredProfile(ResolveStableKeyForComponent(component));

				NotifySelectedComponentProperties();
				return;
			}

			if (selectedComponentOption == null
				|| ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent)
				|| string.IsNullOrWhiteSpace(selectedComponentOption.IdentityKey)
				|| updateProfile == null)
			{
				return;
			}

			var profile = GetSelectedProfile() ?? CreateProfileFromSelection(selectedComponentOption);
			if (!updateProfile(profile))
			{
				return;
			}

			CustomKeyEventSettingsStore.SaveProfile(selectedComponentOption.IdentityKey, profile);
			if (CustomKeyEventSettingsStore.TryGetProfile(selectedComponentOption.IdentityKey, out var savedProfile))
			{
				selectedComponentOption.SetStoredProfile(savedProfile);
				NotifySelectedComponentProperties();
				return;
			}

			selectedComponentOption.SetStoredProfile(null);
			RefreshComponents();
		}

		private string ResolveStableKeyForComponent(CustomKeyEvent component)
		{
			if (component == null)
			{
				return string.Empty;
			}

			if (CustomKeyEventSettingsStore.TryGetRegisteredStableKey(component, out var registeredStableKey))
			{
				return registeredStableKey;
			}

			return component.GetStableProfileKey();
		}

		private void SyncSelectedStoredProfile(string stableKey)
		{
			if (selectedComponentOption == null || string.IsNullOrWhiteSpace(stableKey))
			{
				return;
			}

			if (CustomKeyEventSettingsStore.TryGetProfile(stableKey, out var profile))
			{
				selectedComponentOption.SetStoredProfile(profile);
				return;
			}

			selectedComponentOption.SetStoredProfile(null);
		}

		private static CustomKeyEventProfile CreateProfileFromSelection(CustomKeyEventOption option)
		{
			return new CustomKeyEventProfile
			{
				HierarchyPath = option?.HierarchyPath ?? string.Empty,
				ObjectName = ExtractLeafName(option?.HierarchyPath),
				ComponentOrdinal = option?.ComponentOrdinal ?? 0,
				InitialKeyConfigurationSignature = option?.KeyConfigurationSignature ?? string.Empty,
				CurrentKeyConfigurationSignature = option?.KeyConfigurationSignature ?? string.Empty
			};
		}

		private static string ExtractLeafName(string hierarchyPath)
		{
			if (string.IsNullOrWhiteSpace(hierarchyPath))
			{
				return string.Empty;
			}

			var parts = hierarchyPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			return parts.Length == 0 ? string.Empty : parts[parts.Length - 1];
		}

		private string BuildStoredProfileSummary(CustomKeyEventProfile profile)
		{
			if (profile == null)
			{
				return null;
			}

			return $"Stored Profile (Unloaded); Trigger(Index={profile.IndexTriggerButton}, Vive={profile.ViveTriggerButton}, Oculus={profile.OculusTriggerButton}, WMR={profile.WMRTriggerButton}); Chord({(profile.EnableChordPress ? "On" : "Off")} Index={profile.IndexChordButton}, Vive={profile.ViveChordButton}, Oculus={profile.OculusChordButton}, WMR={profile.WMRChordButton}); Timing(Double={profile.DoubleClickInterval:F2}, Long={profile.LongClickInterval:F2})";
		}

		private CustomKeyEventOption ResolveComponentOption(object value)
		{
			if (value is CustomKeyEventOption option)
			{
				return option;
			}

			var raw = value as string;
			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			var trimmed = raw.Trim();
			return componentOptions
				.OfType<CustomKeyEventOption>()
				.FirstOrDefault(candidate => string.Equals(candidate.IdentityKey, trimmed, StringComparison.Ordinal)
					|| string.Equals(candidate.DisplayLabel, trimmed, StringComparison.OrdinalIgnoreCase));
		}

		private CustomKeyEventOption FindOptionByKey(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return null;
			}

			return componentOptions
				.OfType<CustomKeyEventOption>()
				.FirstOrDefault(candidate => string.Equals(candidate.IdentityKey, key, StringComparison.Ordinal));
		}

		private void NotifySelectedComponentProperties()
		{
			RefreshLiveEventMonitorForSelectedTarget();
			NotifyPropertyChanged(nameof(SelectedComponent));
			NotifyPropertyChanged(nameof(HasSelectedComponent));
			NotifyPropertyChanged(nameof(HasSelectedTarget));
			NotifyPropertyChanged(nameof(ResetActionButtonText));
			NotifyPropertyChanged(nameof(SelectedTargetDisplay));
			NotifyPropertyChanged(nameof(SelectedComponentPath));
			NotifyPropertyChanged(nameof(SelectedComponentOrdinal));
			NotifyPropertyChanged(nameof(SelectedComponentActiveDuration));
			NotifyPropertyChanged(nameof(SelectedComponentSummary));
			NotifyPropertyChanged(nameof(SelectedComponentLiveLastEvent));
			NotifyPropertyChanged(nameof(SelectedComponentLiveRecentEvents));
			NotifyPropertyChanged(nameof(HasClickEvents));
			NotifyPropertyChanged(nameof(HasDoubleClickEvents));
			NotifyPropertyChanged(nameof(HasLongClickEvents));
			NotifyPropertyChanged(nameof(HasPressEvents));
			NotifyPropertyChanged(nameof(HasHoldEvents));
			NotifyPropertyChanged(nameof(HasReleaseEvents));
			NotifyPropertyChanged(nameof(HasReleaseAfterLongClickEvents));
			NotifyPropertyChanged(nameof(HasAnyRoutableEvents));
			NotifyPropertyChanged(nameof(IndexTriggerButton));
			NotifyPropertyChanged(nameof(ViveTriggerButton));
			NotifyPropertyChanged(nameof(OculusTriggerButton));
			NotifyPropertyChanged(nameof(WmrTriggerButton));
			NotifyPropertyChanged(nameof(EnableChordPress));
			NotifyPropertyChanged(nameof(IndexChordButton));
			NotifyPropertyChanged(nameof(ViveChordButton));
			NotifyPropertyChanged(nameof(OculusChordButton));
			NotifyPropertyChanged(nameof(WmrChordButton));
			NotifyPropertyChanged(nameof(ClickEventsChange));
			NotifyPropertyChanged(nameof(DoubleClickEventsChange));
			NotifyPropertyChanged(nameof(LongClickEventsChange));
			NotifyPropertyChanged(nameof(PressEventsChange));
			NotifyPropertyChanged(nameof(HoldEventsChange));
			NotifyPropertyChanged(nameof(ReleaseEventsChange));
			NotifyPropertyChanged(nameof(ReleaseAfterLongClickEventsChange));
			NotifyPropertyChanged(nameof(DoubleClickInterval));
			NotifyPropertyChanged(nameof(LongClickInterval));
			componentDropdown?.ReceiveValue();
			indexTriggerButtonDropdown?.ReceiveValue();
			viveTriggerButtonDropdown?.ReceiveValue();
			oculusTriggerButtonDropdown?.ReceiveValue();
			wmrTriggerButtonDropdown?.ReceiveValue();
			enableChordPressToggle?.ReceiveValue();
			indexChordButtonDropdown?.ReceiveValue();
			viveChordButtonDropdown?.ReceiveValue();
			oculusChordButtonDropdown?.ReceiveValue();
			wmrChordButtonDropdown?.ReceiveValue();
			clickEventsChangeDropdown?.ReceiveValue();
			doubleClickEventsChangeDropdown?.ReceiveValue();
			longClickEventsChangeDropdown?.ReceiveValue();
			pressEventsChangeDropdown?.ReceiveValue();
			holdEventsChangeDropdown?.ReceiveValue();
			releaseEventsChangeDropdown?.ReceiveValue();
			releaseAfterLongClickEventsChangeDropdown?.ReceiveValue();
			doubleClickIntervalSetting?.ReceiveValue();
			longClickIntervalSetting?.ReceiveValue();
		}

		private void SubscribeRuntimeEventObserver()
		{
			if (isRuntimeEventObserverSubscribed)
			{
				return;
			}

			CustomKeyEventSettingsStore.RuntimeEventObserved += OnRuntimeEventObserved;
			isRuntimeEventObserverSubscribed = true;
		}

		private void UnsubscribeRuntimeEventObserver()
		{
			if (!isRuntimeEventObserverSubscribed)
			{
				return;
			}

			CustomKeyEventSettingsStore.RuntimeEventObserved -= OnRuntimeEventObserved;
			isRuntimeEventObserverSubscribed = false;
		}

		private void OnRuntimeEventObserved(string stableKey)
		{
			if (!isActivated)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(stableKey)
				|| !string.Equals(stableKey, liveMonitorTargetStableKey, StringComparison.Ordinal))
			{
				return;
			}

			RefreshLiveEventMonitorForSelectedTarget();
			NotifyLiveEventMonitorProperties();
		}

		private void RefreshLiveEventMonitorForSelectedTarget()
		{
			liveMonitorTargetStableKey = selectedComponentOption?.IdentityKey ?? string.Empty;
			liveMonitorLastEventText = "(waiting for trigger)";
			liveMonitorRecentEventsText = "(no events yet)";

			if (selectedComponentOption == null
				|| ReferenceEquals(selectedComponentOption, CustomKeyEventCatalog.NoComponent)
				|| string.IsNullOrWhiteSpace(liveMonitorTargetStableKey))
			{
				liveMonitorLastEventText = "(n/a)";
				liveMonitorRecentEventsText = "(n/a)";
				return;
			}

			if (!CustomKeyEventSettingsStore.TryGetRuntimeEventSnapshot(liveMonitorTargetStableKey, out var snapshot)
				|| snapshot == null)
			{
				return;
			}

			liveMonitorLastEventText = BuildRuntimeEventDisplay(snapshot.LastEvent);
			var recentEvents = snapshot.RecentEvents ?? new List<CustomKeyEventSettingsStore.RuntimeEventEntry>();
			liveMonitorRecentEventsText = string.Join(" | ", recentEvents
				.AsEnumerable()
				.Reverse()
				.Take(LiveMonitorRecentEventDisplayCount)
				.Select(entry => BuildRuntimeEventDisplay(entry))
				.Where(value => !string.IsNullOrWhiteSpace(value))
				.ToArray());
			if (string.IsNullOrWhiteSpace(liveMonitorRecentEventsText))
			{
				liveMonitorRecentEventsText = "(no events yet)";
			}
		}

		private void NotifyLiveEventMonitorProperties()
		{
			NotifyPropertyChanged(nameof(SelectedComponentLiveLastEvent));
			NotifyPropertyChanged(nameof(SelectedComponentLiveRecentEvents));
		}

		private string BuildRuntimeEventDisplay(CustomKeyEventSettingsStore.RuntimeEventEntry runtimeEvent)
		{
			if (runtimeEvent == null)
			{
				return string.Empty;
			}

			float elapsedSeconds = Math.Max(0f, runtimeEvent.RealtimeSeconds - liveMonitorSessionStartRealtime);
			var sourceLabel = BuildEventTypeDisplayName(runtimeEvent.SourceEventType);
			var destinationLabel = BuildEventTypeDisplayName(runtimeEvent.DestinationEventType);
			if (runtimeEvent.SourceEventType == runtimeEvent.DestinationEventType)
			{
				return $"{sourceLabel} @{elapsedSeconds:F2}s";
			}

			return $"{sourceLabel}->{destinationLabel} @{elapsedSeconds:F2}s";
		}

		private static TEnum ResolveEnumValue<TEnum>(object value, TEnum fallback) where TEnum : struct
		{
			if (value is TEnum typedValue)
			{
				return typedValue;
			}

			var raw = value as string;
			if (!string.IsNullOrWhiteSpace(raw))
			{
				try
				{
					return (TEnum)Enum.Parse(typeof(TEnum), raw.Trim(), true);
				}
				catch
				{
				}
			}

			return fallback;
		}

		private T GetSelectedValue<T>(Func<CustomKeyEvent, T> componentSelector, Func<CustomKeyEventProfile, T> profileSelector, T fallback)
		{
			var component = GetSelectedComponent();
			if (component != null && componentSelector != null)
			{
				return componentSelector(component);
			}

			var profile = GetSelectedProfile();
			if (profile != null && profileSelector != null)
			{
				return profileSelector(profile);
			}

			return fallback;
		}

		private static float ResolveFloatValue(object value, float fallback)
		{
			if (value == null)
			{
				return fallback;
			}

			if (value is float floatValue)
			{
				return floatValue;
			}

			if (value is double doubleValue)
			{
				return (float)doubleValue;
			}

			if (value is int intValue)
			{
				return intValue;
			}

			var raw = value as string;
			if (!string.IsNullOrWhiteSpace(raw)
				&& float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
			{
				return parsedValue;
			}

			return fallback;
		}

		private static List<object> BuildEnumOptions(Type enumType)
		{
			return enumType
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.OrderBy(field => field.MetadataToken)
				.Select(field => field.GetValue(null))
				.ToList();
		}

		private static List<object> BuildEventRouteOptions(CustomKeyEvent.ButtonEventType sourceEventType)
		{
			var options = new List<object>
			{
				new EventRouteOption(CustomKeyEvent.EventRouteTarget.NoChange, "No Change")
			};

			foreach (CustomKeyEvent.ButtonEventType candidateEventType in Enum.GetValues(typeof(CustomKeyEvent.ButtonEventType)))
			{
				if (candidateEventType == sourceEventType)
				{
					continue;
				}

				options.Add(new EventRouteOption(ConvertEventTypeToRouteTarget(candidateEventType), BuildEventTypeDisplayName(candidateEventType)));
			}

			return options;
		}

		private static string BuildEventTypeDisplayName(CustomKeyEvent.ButtonEventType eventType)
		{
			switch (eventType)
			{
				case CustomKeyEvent.ButtonEventType.DoubleClick:
					return "Double Click";
				case CustomKeyEvent.ButtonEventType.LongClick:
					return "Long Click";
				case CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick:
					return "Release After Long Click";
				default:
					return eventType.ToString();
			}
		}

		private static CustomKeyEvent.EventRouteTarget ConvertEventTypeToRouteTarget(CustomKeyEvent.ButtonEventType eventType)
		{
			switch (eventType)
			{
				case CustomKeyEvent.ButtonEventType.Click:
					return CustomKeyEvent.EventRouteTarget.Click;
				case CustomKeyEvent.ButtonEventType.DoubleClick:
					return CustomKeyEvent.EventRouteTarget.DoubleClick;
				case CustomKeyEvent.ButtonEventType.LongClick:
					return CustomKeyEvent.EventRouteTarget.LongClick;
				case CustomKeyEvent.ButtonEventType.Press:
					return CustomKeyEvent.EventRouteTarget.Press;
				case CustomKeyEvent.ButtonEventType.Hold:
					return CustomKeyEvent.EventRouteTarget.Hold;
				case CustomKeyEvent.ButtonEventType.Release:
					return CustomKeyEvent.EventRouteTarget.Release;
				case CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick:
					return CustomKeyEvent.EventRouteTarget.ReleaseAfterLongClick;
				default:
					return CustomKeyEvent.EventRouteTarget.NoChange;
			}
		}

		private static CustomKeyEvent.EventRouteTarget ResolveEventRouteValue(object value, CustomKeyEvent.EventRouteTarget fallback, List<object> options)
		{
			if (value is EventRouteOption option)
			{
				return option.Target;
			}

			if (value is CustomKeyEvent.EventRouteTarget enumValue)
			{
				return enumValue;
			}

			var raw = value as string;
			if (!string.IsNullOrWhiteSpace(raw))
			{
				var trimmed = raw.Trim();
				foreach (var candidate in options.OfType<EventRouteOption>())
				{
					if (string.Equals(candidate.Label, trimmed, StringComparison.OrdinalIgnoreCase)
						|| string.Equals(candidate.Target.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
					{
						return candidate.Target;
					}
				}
			}

			return fallback;
		}

		private static EventRouteOption FindEventRouteOption(List<object> options, CustomKeyEvent.EventRouteTarget target)
		{
			return options
				.OfType<EventRouteOption>()
				.FirstOrDefault(option => option.Target == target)
				?? options.OfType<EventRouteOption>().First();
		}

		private static void RefreshDropdown(DropDownListSetting dropdown, List<object> options)
		{
			if (dropdown == null)
			{
				return;
			}

			dropdown.Values = options;
			dropdown.UpdateChoices();
			dropdown.ReceiveValue();
			ConfigureDropdownSelectedLabel(dropdown);
		}

		private static void ConfigureDropdownSelectedLabel(DropDownListSetting dropdown)
		{
			TextMeshProUGUI label = dropdown?.Dropdown?.GetField<TextMeshProUGUI, SimpleTextDropdown>("_text");
			if (label == null)
			{
				return;
			}

			label.enableWordWrapping = false;
			label.overflowMode = TextOverflowModes.Ellipsis;
		}

		private sealed class EventRouteOption
		{
			public EventRouteOption(CustomKeyEvent.EventRouteTarget target, string label)
			{
				Target = target;
				Label = string.IsNullOrWhiteSpace(label) ? target.ToString() : label;
			}

			public CustomKeyEvent.EventRouteTarget Target { get; }
			public string Label { get; }

			public override string ToString()
			{
				return Label;
			}
		}
	}
}
