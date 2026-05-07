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

        private static bool IsSceneObject(CustomKeyEvent component)
        {
            return component != null
                && component.gameObject != null
                && component.gameObject.scene.IsValid();
        }
    }
}
