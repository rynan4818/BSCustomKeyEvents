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
		private CustomKeyEventOption selectedComponentOption;

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

		[UIValue("selectedComponentSummary")]
		public string SelectedComponentSummary => selectedComponentOption?.DefaultSummary
			?? "No CustomKeyEvent components are currently loaded.";

		[UIValue("hasSelectedComponent")]
		public bool HasSelectedComponent => GetSelectedComponent() != null;

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
			get => GetSelectedComponent()?.IndexTriggerButton ?? CustomKeyEvent.IndexButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.IndexTriggerButton);
				if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(component.IndexTriggerButton, selected))
				{
					return false;
				}

				component.IndexTriggerButton = selected;
				return true;
			});
		}

		[UIValue("viveTriggerButton")]
		public object ViveTriggerButton
		{
			get => GetSelectedComponent()?.ViveTriggerButton ?? CustomKeyEvent.ViveButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.ViveTriggerButton);
				if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(component.ViveTriggerButton, selected))
				{
					return false;
				}

				component.ViveTriggerButton = selected;
				return true;
			});
		}

		[UIValue("oculusTriggerButton")]
		public object OculusTriggerButton
		{
			get => GetSelectedComponent()?.OculusTriggerButton ?? CustomKeyEvent.OculusButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.OculusTriggerButton);
				if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(component.OculusTriggerButton, selected))
				{
					return false;
				}

				component.OculusTriggerButton = selected;
				return true;
			});
		}

		[UIValue("wmrTriggerButton")]
		public object WmrTriggerButton
		{
			get => GetSelectedComponent()?.WMRTriggerButton ?? CustomKeyEvent.WMRButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.WMRTriggerButton);
				if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(component.WMRTriggerButton, selected))
				{
					return false;
				}

				component.WMRTriggerButton = selected;
				return true;
			});
		}

		[UIValue("enableChordPress")]
		public bool EnableChordPress
		{
			get => GetSelectedComponent()?.EnableChordPress ?? false;
			set => ApplySelectedComponentChange(component =>
			{
				if (component.EnableChordPress == value)
				{
					return false;
				}

				component.EnableChordPress = value;
				return true;
			});
		}

		[UIValue("indexChordButton")]
		public object IndexChordButton
		{
			get => GetSelectedComponent()?.IndexChordButton ?? CustomKeyEvent.IndexButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.IndexChordButton);
				if (EqualityComparer<CustomKeyEvent.IndexButton>.Default.Equals(component.IndexChordButton, selected))
				{
					return false;
				}

				component.IndexChordButton = selected;
				return true;
			});
		}

		[UIValue("viveChordButton")]
		public object ViveChordButton
		{
			get => GetSelectedComponent()?.ViveChordButton ?? CustomKeyEvent.ViveButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.ViveChordButton);
				if (EqualityComparer<CustomKeyEvent.ViveButton>.Default.Equals(component.ViveChordButton, selected))
				{
					return false;
				}

				component.ViveChordButton = selected;
				return true;
			});
		}

		[UIValue("oculusChordButton")]
		public object OculusChordButton
		{
			get => GetSelectedComponent()?.OculusChordButton ?? CustomKeyEvent.OculusButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.OculusChordButton);
				if (EqualityComparer<CustomKeyEvent.OculusButton>.Default.Equals(component.OculusChordButton, selected))
				{
					return false;
				}

				component.OculusChordButton = selected;
				return true;
			});
		}

		[UIValue("wmrChordButton")]
		public object WmrChordButton
		{
			get => GetSelectedComponent()?.WMRChordButton ?? CustomKeyEvent.WMRButton.None;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEnumValue(value, component.WMRChordButton);
				if (EqualityComparer<CustomKeyEvent.WMRButton>.Default.Equals(component.WMRChordButton, selected))
				{
					return false;
				}

				component.WMRChordButton = selected;
				return true;
			});
		}

		[UIValue("clickEventsChange")]
		public object ClickEventsChange
		{
			get => FindEventRouteOption(clickEventsChangeOptions, GetSelectedComponent()?.ClickEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.ClickEventsChange, clickEventsChangeOptions);
				if (component.ClickEventsChange == selected)
				{
					return false;
				}

				component.ClickEventsChange = selected;
				return true;
			});
		}

		[UIValue("doubleClickEventsChange")]
		public object DoubleClickEventsChange
		{
			get => FindEventRouteOption(doubleClickEventsChangeOptions, GetSelectedComponent()?.DoubleClickEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.DoubleClickEventsChange, doubleClickEventsChangeOptions);
				if (component.DoubleClickEventsChange == selected)
				{
					return false;
				}

				component.DoubleClickEventsChange = selected;
				return true;
			});
		}

		[UIValue("longClickEventsChange")]
		public object LongClickEventsChange
		{
			get => FindEventRouteOption(longClickEventsChangeOptions, GetSelectedComponent()?.LongClickEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.LongClickEventsChange, longClickEventsChangeOptions);
				if (component.LongClickEventsChange == selected)
				{
					return false;
				}

				component.LongClickEventsChange = selected;
				return true;
			});
		}

		[UIValue("pressEventsChange")]
		public object PressEventsChange
		{
			get => FindEventRouteOption(pressEventsChangeOptions, GetSelectedComponent()?.PressEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.PressEventsChange, pressEventsChangeOptions);
				if (component.PressEventsChange == selected)
				{
					return false;
				}

				component.PressEventsChange = selected;
				return true;
			});
		}

		[UIValue("holdEventsChange")]
		public object HoldEventsChange
		{
			get => FindEventRouteOption(holdEventsChangeOptions, GetSelectedComponent()?.HoldEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.HoldEventsChange, holdEventsChangeOptions);
				if (component.HoldEventsChange == selected)
				{
					return false;
				}

				component.HoldEventsChange = selected;
				return true;
			});
		}

		[UIValue("releaseEventsChange")]
		public object ReleaseEventsChange
		{
			get => FindEventRouteOption(releaseEventsChangeOptions, GetSelectedComponent()?.ReleaseEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.ReleaseEventsChange, releaseEventsChangeOptions);
				if (component.ReleaseEventsChange == selected)
				{
					return false;
				}

				component.ReleaseEventsChange = selected;
				return true;
			});
		}

		[UIValue("releaseAfterLongClickEventsChange")]
		public object ReleaseAfterLongClickEventsChange
		{
			get => FindEventRouteOption(releaseAfterLongClickEventsChangeOptions, GetSelectedComponent()?.ReleaseAfterLongClickEventsChange ?? CustomKeyEvent.EventRouteTarget.NoChange);
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveEventRouteValue(value, component.ReleaseAfterLongClickEventsChange, releaseAfterLongClickEventsChangeOptions);
				if (component.ReleaseAfterLongClickEventsChange == selected)
				{
					return false;
				}

				component.ReleaseAfterLongClickEventsChange = selected;
				return true;
			});
		}

		[UIValue("doubleClickInterval")]
		public float DoubleClickInterval
		{
			get => GetSelectedComponent()?.DoubleClickInterval ?? 0.5f;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveFloatValue(value, component.DoubleClickInterval);
				if (Math.Abs(component.DoubleClickInterval - selected) < 0.0001f)
				{
					return false;
				}

				component.DoubleClickInterval = selected;
				return true;
			});
		}

		[UIValue("longClickInterval")]
		public float LongClickInterval
		{
			get => GetSelectedComponent()?.LongClickInterval ?? 0.6f;
			set => ApplySelectedComponentChange(component =>
			{
				var selected = ResolveFloatValue(value, component.LongClickInterval);
				if (Math.Abs(component.LongClickInterval - selected) < 0.0001f)
				{
					return false;
				}

				component.LongClickInterval = selected;
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
			NotifySelectedComponentProperties();
		}

		[UIAction("reset-selected-component")]
		public void ResetSelectedComponent()
		{
			var component = GetSelectedComponent();
			if (component == null)
			{
				return;
			}

			component.ResetToInitialDefaults();
			CustomKeyEventSettingsStore.SaveComponent(component);
			NotifySelectedComponentProperties();
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

		private bool HasPersistentEvent(CustomKeyEvent.ButtonEventType eventType)
		{
			var component = GetSelectedComponent();
			return component != null && component.HasPersistentEvent(eventType);
		}

		private void ApplySelectedComponentChange(Func<CustomKeyEvent, bool> update)
		{
			var component = GetSelectedComponent();
			if (component == null || update == null || !update(component))
			{
				return;
			}

			CustomKeyEventSettingsStore.SaveComponent(component);
			NotifySelectedComponentProperties();
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
			NotifyPropertyChanged(nameof(SelectedComponent));
			NotifyPropertyChanged(nameof(HasSelectedComponent));
			NotifyPropertyChanged(nameof(SelectedComponentPath));
			NotifyPropertyChanged(nameof(SelectedComponentOrdinal));
			NotifyPropertyChanged(nameof(SelectedComponentSummary));
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

			dropdown.values = options;
			dropdown.UpdateChoices();
			dropdown.ReceiveValue();
			ConfigureDropdownSelectedLabel(dropdown);
		}

		private static void ConfigureDropdownSelectedLabel(DropDownListSetting dropdown)
		{
			TextMeshProUGUI label = dropdown?.dropdown?.GetField<TextMeshProUGUI, SimpleTextDropdown>("_text");
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
