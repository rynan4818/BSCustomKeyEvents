using System;
using System.Collections.Generic;
using System.Linq;
using AvatarScriptPack;
using UnityEngine;

namespace CustomKeyEvents.Models
{
	internal static class CustomKeyEventCatalog
	{
		private const int MaxDropdownObjectNameLength = 30;
		private static readonly CustomKeyEventOption NoComponentOption = new CustomKeyEventOption(
			"(No CustomKeyEvent components found)",
			string.Empty,
			0,
			string.Empty,
			"No loaded CustomKeyEvent components were found.",
			null);

		public static List<CustomKeyEventOption> Discover()
		{
			var options = Resources.FindObjectsOfTypeAll<CustomKeyEvent>()
				.Where(IsSceneObject)
				.Select(CreateOption)
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

			return new CustomKeyEventOption(
				BuildDisplayLabel(componentOrdinal, objectName),
				hierarchyPath,
				componentOrdinal,
				keyConfigurationSignature,
				component.GetDefaultSettingsSummary(),
				component);
		}

		private static string BuildDisplayLabel(int componentOrdinal, string objectName)
		{
			var normalizedName = string.IsNullOrWhiteSpace(objectName)
				? "(Unnamed)"
				: objectName.Trim();
			return $"#{componentOrdinal}{TruncateForDropdown(normalizedName, MaxDropdownObjectNameLength)}";
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
	}

	internal sealed class CustomKeyEventOption
	{
		public CustomKeyEventOption(string displayLabel, string hierarchyPath, int componentOrdinal, string keyConfigurationSignature, string defaultSummary, CustomKeyEvent component)
		{
			DisplayLabel = displayLabel ?? string.Empty;
			HierarchyPath = hierarchyPath ?? string.Empty;
			ComponentOrdinal = componentOrdinal;
			KeyConfigurationSignature = keyConfigurationSignature ?? string.Empty;
			DefaultSummary = defaultSummary ?? string.Empty;
			Component = component;
			IdentityKey = $"{HierarchyPath}|#{ComponentOrdinal}|{KeyConfigurationSignature}";
		}

		public CustomKeyEvent Component { get; }
		public string IdentityKey { get; }
		public string DisplayLabel { get; }
		public string HierarchyPath { get; }
		public int ComponentOrdinal { get; }
		public string KeyConfigurationSignature { get; }
		public string DefaultSummary { get; }

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
