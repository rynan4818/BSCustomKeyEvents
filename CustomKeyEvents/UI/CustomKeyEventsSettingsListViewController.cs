using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AvatarScriptPack;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomKeyEvents.Configuration;
using CustomKeyEvents.Models;

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
			NotifyPropertyChanged(nameof(IndexTriggerButton));
			NotifyPropertyChanged(nameof(ViveTriggerButton));
			NotifyPropertyChanged(nameof(OculusTriggerButton));
			NotifyPropertyChanged(nameof(WmrTriggerButton));
			NotifyPropertyChanged(nameof(EnableChordPress));
			NotifyPropertyChanged(nameof(IndexChordButton));
			NotifyPropertyChanged(nameof(ViveChordButton));
			NotifyPropertyChanged(nameof(OculusChordButton));
			NotifyPropertyChanged(nameof(WmrChordButton));
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

		private static List<object> BuildEnumOptions(Type enumType)
		{
			return enumType
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.OrderBy(field => field.MetadataToken)
				.Select(field => field.GetValue(null))
				.ToList();
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
		}
	}
}
