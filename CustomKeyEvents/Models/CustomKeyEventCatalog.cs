using System;
using System.Collections.Generic;
using System.Linq;
using AvatarScriptPack;
using CustomKeyEvents.Configuration;
using UnityEngine;

namespace CustomKeyEvents.Models
{
	internal static class CustomKeyEventCatalog
	{
		private const int MaxDropdownObjectNameLength = 30;
		private static readonly CustomKeyEventOption NoComponentOption = new CustomKeyEventOption(
			string.Empty,
			"(No CustomKeyEvent components found)",
			string.Empty,
			0,
			string.Empty,
			"No discovered CustomKeyEvent targets were found.",
			null,
			null,
			null);

		public static List<CustomKeyEventOption> Discover()
		{
			var optionsByKey = new Dictionary<string, CustomKeyEventOption>(StringComparer.Ordinal);
			foreach (var component in Resources.FindObjectsOfTypeAll<CustomKeyEvent>().Where(IsSceneObject))
			{
				var option = CreateOption(component);
				if (option == null || string.IsNullOrWhiteSpace(option.IdentityKey))
				{
					continue;
				}

				optionsByKey[option.IdentityKey] = option;
			}

			var runtimeProfiles = CustomKeyEventSettingsStore.GetRuntimeKnownProfilesSnapshot();
			foreach (var pair in runtimeProfiles)
			{
				var stableKey = pair.Key;
				var profile = pair.Value;
				if (string.IsNullOrWhiteSpace(stableKey) || profile == null)
				{
					continue;
				}

				if (optionsByKey.TryGetValue(stableKey, out var loadedOption))
				{
					loadedOption.SetStoredProfile(profile);
					continue;
				}

				optionsByKey[stableKey] = CreateOptionFromProfile(stableKey, profile);
			}

			var profiles = PluginConfig.Instance?.CustomKeyEventProfiles;
			if (profiles != null)
			{
				foreach (var pair in profiles)
				{
					var stableKey = pair.Key;
					var profile = pair.Value;
					if (string.IsNullOrWhiteSpace(stableKey) || profile == null)
					{
						continue;
					}

					if (optionsByKey.TryGetValue(stableKey, out var loadedOption))
					{
						loadedOption.SetStoredProfile(profile);
						continue;
					}

					optionsByKey[stableKey] = CreateOptionFromProfile(stableKey, profile);
				}
			}

			var options = optionsByKey.Values
				.OrderBy(option => option.HierarchyPath, StringComparer.OrdinalIgnoreCase)
				.ThenBy(option => option.ComponentOrdinal)
				.ThenBy(option => option.KeyConfigurationSignature, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (options.Count == 0)
			{
				options.Add(NoComponentOption);
			}

			return options;
		}

		public static CustomKeyEventOption NoComponent => NoComponentOption;

		private static bool IsSceneObject(CustomKeyEvent component)
		{
			return component != null
				&& component.gameObject != null
				&& component.gameObject.scene.IsValid();
		}

		private static CustomKeyEventOption CreateOption(CustomKeyEvent component)
		{
			var hierarchyPath = component.GetHierarchyPath();
			var componentOrdinal = component.GetComponentOrdinal();
			var keyConfigurationSignature = component.GetInitialKeyConfigurationSignature();
			var objectName = component.gameObject != null ? component.gameObject.name : string.Empty;
			var stableKey = CustomKeyEventSettingsStore.TryGetRegisteredStableKey(component, out var registeredStableKey)
				? registeredStableKey
				: component.GetStableProfileKey();
			float activeDurationSeconds = component.GetActiveDurationSeconds();
			if (CustomKeyEventSettingsStore.TryGetRuntimeActiveDuration(component, out var runtimeActiveSeconds))
			{
				activeDurationSeconds = runtimeActiveSeconds;
			}

			return new CustomKeyEventOption(
				stableKey,
				BuildDisplayLabel(componentOrdinal, objectName, activeDurationSeconds),
				hierarchyPath,
				componentOrdinal,
				keyConfigurationSignature,
				component.GetDefaultSettingsSummary(),
				component,
				null,
				activeDurationSeconds);
		}

		private static CustomKeyEventOption CreateOptionFromProfile(string stableKey, CustomKeyEventProfile profile)
		{
			ExtractStableKeyMetadata(stableKey, out var stableKeyHierarchyPath, out var stableKeyComponentOrdinal);
			var hierarchyPath = string.IsNullOrWhiteSpace(stableKeyHierarchyPath)
				? profile.HierarchyPath ?? string.Empty
				: stableKeyHierarchyPath;
			var componentOrdinal = stableKeyComponentOrdinal > 0
				? stableKeyComponentOrdinal
				: profile.ComponentOrdinal;
			var keyConfigurationSignature = ResolveKeyConfigurationSignature(stableKey, profile);
			var objectName = ExtractLeafName(hierarchyPath);
			float? activeDurationSeconds = null;
			if (CustomKeyEventSettingsStore.TryGetRuntimeActiveDuration(stableKey, out var cachedActiveSeconds))
			{
				activeDurationSeconds = cachedActiveSeconds;
			}

			return new CustomKeyEventOption(
				stableKey,
				BuildDisplayLabel(componentOrdinal, objectName, activeDurationSeconds),
				hierarchyPath,
				componentOrdinal,
				keyConfigurationSignature,
				BuildStoredProfileSummary(profile),
				null,
				profile,
				activeDurationSeconds);
		}

		private static string BuildDisplayLabel(int componentOrdinal, string objectName, float? activeDurationSeconds)
		{
			var normalizedName = string.IsNullOrWhiteSpace(objectName)
				? "(Unnamed)"
				: objectName.Trim();
			var labelCore = $"#{componentOrdinal}{TruncateForDropdown(normalizedName, MaxDropdownObjectNameLength)}";
			return activeDurationSeconds.HasValue
				? $"{labelCore}@{activeDurationSeconds.Value:F1}s"
				: $"{labelCore}@unloaded";
		}

		private static string TruncateForDropdown(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || maxLength <= 0)
			{
				return string.Empty;
			}

			if (value.Length <= maxLength)
			{
				return value;
			}

			if (maxLength <= 3)
			{
				return value.Substring(0, maxLength);
			}

			return $"{value.Substring(0, maxLength - 3)}...";
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

		private static string ResolveKeyConfigurationSignature(string stableKey, CustomKeyEventProfile profile)
		{
			if (!string.IsNullOrWhiteSpace(profile.InitialKeyConfigurationSignature))
			{
				return profile.InitialKeyConfigurationSignature;
			}

			if (string.IsNullOrWhiteSpace(stableKey))
			{
				return string.Empty;
			}

			var separatorIndex = stableKey.LastIndexOf('|');
			return separatorIndex >= 0 && separatorIndex + 1 < stableKey.Length
				? stableKey.Substring(separatorIndex + 1)
				: string.Empty;
		}

		private static void ExtractStableKeyMetadata(string stableKey, out string hierarchyPath, out int componentOrdinal)
		{
			hierarchyPath = string.Empty;
			componentOrdinal = 0;
			if (string.IsNullOrWhiteSpace(stableKey))
			{
				return;
			}

			int firstSeparator = stableKey.LastIndexOf("|#", StringComparison.Ordinal);
			int secondSeparator = stableKey.LastIndexOf('|');
			if (firstSeparator < 0 || secondSeparator <= firstSeparator + 2)
			{
				return;
			}

			hierarchyPath = stableKey.Substring(0, firstSeparator);
			string ordinalText = stableKey.Substring(firstSeparator + 2, secondSeparator - (firstSeparator + 2));
			if (!int.TryParse(ordinalText, out componentOrdinal))
			{
				componentOrdinal = 0;
			}
		}

		private static string BuildStoredProfileSummary(CustomKeyEventProfile profile)
		{
			return $"Stored Profile (Unloaded); Trigger(Index={profile.IndexTriggerButton}, Vive={profile.ViveTriggerButton}, Oculus={profile.OculusTriggerButton}, WMR={profile.WMRTriggerButton}); Chord({(profile.EnableChordPress ? "On" : "Off")} Index={profile.IndexChordButton}, Vive={profile.ViveChordButton}, Oculus={profile.OculusChordButton}, WMR={profile.WMRChordButton}); Timing(Double={profile.DoubleClickInterval:F2}, Long={profile.LongClickInterval:F2})";
		}
	}

	internal sealed class CustomKeyEventOption
	{
		public CustomKeyEventOption(string identityKey, string displayLabel, string hierarchyPath, int componentOrdinal, string keyConfigurationSignature, string defaultSummary, CustomKeyEvent component, CustomKeyEventProfile storedProfile, float? activeDurationSeconds)
		{
			DisplayLabel = displayLabel ?? string.Empty;
			HierarchyPath = hierarchyPath ?? string.Empty;
			ComponentOrdinal = componentOrdinal;
			KeyConfigurationSignature = keyConfigurationSignature ?? string.Empty;
			DefaultSummary = defaultSummary ?? string.Empty;
			Component = component;
			IdentityKey = string.IsNullOrWhiteSpace(identityKey)
				? $"{HierarchyPath}|#{ComponentOrdinal}|{KeyConfigurationSignature}"
				: identityKey;
			StoredProfile = storedProfile;
			ActiveDurationSeconds = activeDurationSeconds;
		}

		public CustomKeyEvent Component { get; }
		public string IdentityKey { get; }
		public string DisplayLabel { get; }
		public string HierarchyPath { get; }
		public int ComponentOrdinal { get; }
		public string KeyConfigurationSignature { get; }
		public string DefaultSummary { get; }
		public CustomKeyEventProfile StoredProfile { get; private set; }
		public float? ActiveDurationSeconds { get; private set; }

		public void SetStoredProfile(CustomKeyEventProfile storedProfile)
		{
			StoredProfile = storedProfile;
		}

		public override string ToString()
		{
			return DisplayLabel;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj is CustomKeyEventOption option)
			{
				return string.Equals(IdentityKey, option.IdentityKey, StringComparison.Ordinal);
			}

			if (obj is string value)
			{
				return string.Equals(IdentityKey, value, StringComparison.Ordinal)
					|| string.Equals(DisplayLabel, value, StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return StringComparer.Ordinal.GetHashCode(IdentityKey ?? string.Empty);
		}
	}
}
