using System.Collections.Generic;
using System.Linq;
using AvatarScriptPack;
using UnityEngine;

namespace CustomKeyEvents.Configuration
{
    internal static class CustomKeyEventSettingsStore
    {
        private static readonly Dictionary<int, CustomKeyEvent> registeredComponents = new Dictionary<int, CustomKeyEvent>();
        private static readonly object syncRoot = new object();

        public static void Register(CustomKeyEvent component)
        {
            if (component == null)
            {
                return;
            }

            lock (syncRoot)
            {
                registeredComponents[component.GetInstanceID()] = component;
            }

            EnsureProfileExists(component);
            ApplyToComponent(component);
        }

        public static void Unregister(CustomKeyEvent component)
        {
            if (component == null)
            {
                return;
            }

            lock (syncRoot)
            {
                registeredComponents.Remove(component.GetInstanceID());
            }
        }

        public static void RebindLoadedSceneComponents()
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<CustomKeyEvent>())
            {
                if (!IsSceneObject(component))
                {
                    continue;
                }

                Register(component);
            }
        }

        public static void ReapplyRegisteredComponents()
        {
            CustomKeyEvent[] snapshot;
            lock (syncRoot)
            {
                snapshot = registeredComponents.Values.Where(component => component != null).ToArray();
            }

            foreach (var component in snapshot)
            {
                ApplyToComponent(component);
            }
        }

        public static void SaveComponent(CustomKeyEvent component)
        {
            if (component == null || PluginConfig.Instance == null)
            {
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null)
            {
                profiles = new Dictionary<string, CustomKeyEventProfile>();
                PluginConfig.Instance.CustomKeyEventProfiles = profiles;
            }

            var stableKey = component.GetStableProfileKey();
            var profile = component.CreateProfileSnapshot();
            profiles[stableKey] = profile;
            PluginConfig.Instance.Changed();
        }

        public static bool TryGetProfile(string stableKey, out CustomKeyEventProfile profile)
        {
            profile = null;
            if (string.IsNullOrWhiteSpace(stableKey) || PluginConfig.Instance == null)
            {
                return false;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null)
            {
                return false;
            }

            return profiles.TryGetValue(stableKey, out profile);
        }

        public static void SaveProfile(string stableKey, CustomKeyEventProfile profile)
        {
            if (string.IsNullOrWhiteSpace(stableKey) || profile == null || PluginConfig.Instance == null)
            {
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null)
            {
                profiles = new Dictionary<string, CustomKeyEventProfile>();
                PluginConfig.Instance.CustomKeyEventProfiles = profiles;
            }

            profiles[stableKey] = profile;
            PluginConfig.Instance.Changed();
        }

        private static void ApplyToComponent(CustomKeyEvent component)
        {
            if (component == null || PluginConfig.Instance == null)
            {
                return;
            }

            if (TryResolveProfile(component, out var profile))
            {
                component.ApplyProfile(profile);
            }
        }

        private static bool TryResolveProfile(CustomKeyEvent component, out CustomKeyEventProfile profile)
        {
            profile = null;
            var config = PluginConfig.Instance;
            var profiles = config?.CustomKeyEventProfiles;
            if (profiles == null || profiles.Count == 0)
            {
                return false;
            }

            var stableKey = component.GetStableProfileKey();
            if (profiles.TryGetValue(stableKey, out profile))
            {
                return true;
            }

            return false;
        }

        private static void EnsureProfileExists(CustomKeyEvent component)
        {
            if (component == null || PluginConfig.Instance == null)
            {
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null)
            {
                profiles = new Dictionary<string, CustomKeyEventProfile>();
                PluginConfig.Instance.CustomKeyEventProfiles = profiles;
            }

            var stableKey = component.GetStableProfileKey();
            if (profiles.TryGetValue(stableKey, out var existingProfile))
            {
                if (UpdateProfileMetadata(existingProfile, component))
                {
                    PluginConfig.Instance.Changed();
                }
                return;
            }

            profiles[stableKey] = component.CreateProfileSnapshot();
            PluginConfig.Instance.Changed();
        }

        private static bool UpdateProfileMetadata(CustomKeyEventProfile profile, CustomKeyEvent component)
        {
            if (profile == null || component == null)
            {
                return false;
            }

            bool changed = false;
            string hierarchyPath = component.GetHierarchyPath();
            int componentOrdinal = component.GetComponentOrdinal();
            string initialSignature = component.GetInitialKeyConfigurationSignature();

            if (!string.Equals(profile.HierarchyPath, hierarchyPath))
            {
                profile.HierarchyPath = hierarchyPath;
                changed = true;
            }

            if (profile.ComponentOrdinal != componentOrdinal)
            {
                profile.ComponentOrdinal = componentOrdinal;
                changed = true;
            }

            if (!string.Equals(profile.InitialKeyConfigurationSignature, initialSignature))
            {
                profile.InitialKeyConfigurationSignature = initialSignature;
                changed = true;
            }

            bool hasClickEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.Click);
            bool hasDoubleClickEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.DoubleClick);
            bool hasLongClickEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.LongClick);
            bool hasPressEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.Press);
            bool hasHoldEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.Hold);
            bool hasReleaseEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.Release);
            bool hasReleaseAfterLongClickEvents = component.HasPersistentEvent(CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick);

            if (profile.HasClickEvents != hasClickEvents)
            {
                profile.HasClickEvents = hasClickEvents;
                changed = true;
            }

            if (profile.HasDoubleClickEvents != hasDoubleClickEvents)
            {
                profile.HasDoubleClickEvents = hasDoubleClickEvents;
                changed = true;
            }

            if (profile.HasLongClickEvents != hasLongClickEvents)
            {
                profile.HasLongClickEvents = hasLongClickEvents;
                changed = true;
            }

            if (profile.HasPressEvents != hasPressEvents)
            {
                profile.HasPressEvents = hasPressEvents;
                changed = true;
            }

            if (profile.HasHoldEvents != hasHoldEvents)
            {
                profile.HasHoldEvents = hasHoldEvents;
                changed = true;
            }

            if (profile.HasReleaseEvents != hasReleaseEvents)
            {
                profile.HasReleaseEvents = hasReleaseEvents;
                changed = true;
            }

            if (profile.HasReleaseAfterLongClickEvents != hasReleaseAfterLongClickEvents)
            {
                profile.HasReleaseAfterLongClickEvents = hasReleaseAfterLongClickEvents;
                changed = true;
            }

            return changed;
        }

        private static bool IsSceneObject(CustomKeyEvent component)
        {
            return component != null
                && component.gameObject != null
                && component.gameObject.scene.IsValid();
        }
    }
}
