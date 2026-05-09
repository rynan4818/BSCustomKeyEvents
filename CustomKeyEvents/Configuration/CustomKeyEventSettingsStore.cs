using System;
using System.Collections.Generic;
using System.Linq;
using AvatarScriptPack;
using UnityEngine;

namespace CustomKeyEvents.Configuration
{
    internal static class CustomKeyEventSettingsStore
    {
        internal sealed class RuntimeEventEntry
        {
            public RuntimeEventEntry(CustomKeyEvent.ButtonEventType sourceEventType, CustomKeyEvent.ButtonEventType destinationEventType, float realtimeSeconds, int frameCount)
            {
                SourceEventType = sourceEventType;
                DestinationEventType = destinationEventType;
                RealtimeSeconds = realtimeSeconds;
                FrameCount = frameCount;
            }

            public CustomKeyEvent.ButtonEventType SourceEventType { get; }
            public CustomKeyEvent.ButtonEventType DestinationEventType { get; }
            public float RealtimeSeconds { get; }
            public int FrameCount { get; }
        }

        internal sealed class RuntimeEventSnapshot
        {
            public RuntimeEventSnapshot(string stableKey, RuntimeEventEntry lastEvent, List<RuntimeEventEntry> recentEvents)
            {
                StableKey = stableKey ?? string.Empty;
                LastEvent = lastEvent;
                RecentEvents = recentEvents ?? new List<RuntimeEventEntry>();
            }

            public string StableKey { get; }
            public RuntimeEventEntry LastEvent { get; }
            public List<RuntimeEventEntry> RecentEvents { get; }
        }

        private const int maxRuntimeEventHistory = 12;
        private static readonly Dictionary<int, CustomKeyEvent> registeredComponents = new Dictionary<int, CustomKeyEvent>();
        private static readonly Dictionary<int, string> registeredStableKeyByInstanceId = new Dictionary<int, string>();
        private static readonly Dictionary<int, float> registeredActiveBaselineByInstanceId = new Dictionary<int, float>();
        private static readonly Dictionary<string, float> runtimeActiveDurationByStableKey = new Dictionary<string, float>();
        private static readonly Dictionary<string, CustomKeyEventProfile> runtimeKnownProfilesByStableKey = new Dictionary<string, CustomKeyEventProfile>();
        private static readonly Dictionary<string, List<RuntimeEventEntry>> runtimeEventHistoryByStableKey = new Dictionary<string, List<RuntimeEventEntry>>();
        private static readonly Dictionary<string, bool> holdRecordedInCurrentPressByStableKey = new Dictionary<string, bool>();
        private static readonly object syncRoot = new object();
        private static bool isRuntimeEventMonitorEnabled;

        public static event Action<string> RuntimeEventObserved;

        public static void Register(CustomKeyEvent component)
        {
            if (component == null)
            {
                return;
            }

            int instanceId = component.GetInstanceID();
            string stableKey = component.GetStableProfileKey();
            lock (syncRoot)
            {
                bool alreadyRegistered = registeredStableKeyByInstanceId.ContainsKey(instanceId);
                registeredComponents[instanceId] = component;
                registeredStableKeyByInstanceId[instanceId] = stableKey;
                if (!alreadyRegistered)
                {
                    registeredActiveBaselineByInstanceId[instanceId] = component.GetActiveDurationSeconds();
                }
            }

            UpdateRuntimeKnownProfile(component, stableKey);
            UpdatePersistedProfileMetadataIfExists(component, stableKey);
            ApplyToComponent(component, stableKey);
        }

        public static void Unregister(CustomKeyEvent component)
        {
            if (component == null)
            {
                return;
            }

            string stableKey;
            int instanceId = component.GetInstanceID();
            float baselineActiveSeconds = 0f;
            bool hasBaseline = false;
            lock (syncRoot)
            {
                if (!registeredStableKeyByInstanceId.TryGetValue(instanceId, out stableKey)
                    || string.IsNullOrWhiteSpace(stableKey))
                {
                    return;
                }

                registeredComponents.Remove(instanceId);
                if (registeredActiveBaselineByInstanceId.TryGetValue(instanceId, out var registeredBaseline))
                {
                    baselineActiveSeconds = registeredBaseline;
                    hasBaseline = true;
                }
                registeredStableKeyByInstanceId.Remove(instanceId);
                registeredActiveBaselineByInstanceId.Remove(instanceId);
            }

            float currentActiveSeconds = component.GetActiveDurationSeconds();
            float deltaActiveSeconds = hasBaseline
                ? Mathf.Max(0f, currentActiveSeconds - baselineActiveSeconds)
                : Mathf.Max(0f, currentActiveSeconds);
            AddRuntimeActiveDuration(stableKey, deltaActiveSeconds);
            UpdateRuntimeKnownProfile(component, stableKey);
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
            KeyValuePair<int, CustomKeyEvent>[] snapshot;
            lock (syncRoot)
            {
                snapshot = registeredComponents
                    .Where(pair => pair.Value != null)
                    .ToArray();
            }

            foreach (var pair in snapshot)
            {
                string stableKey;
                lock (syncRoot)
                {
                    if (!registeredStableKeyByInstanceId.TryGetValue(pair.Key, out stableKey) || string.IsNullOrWhiteSpace(stableKey))
                    {
                        stableKey = pair.Value.GetStableProfileKey();
                    }
                }

                ApplyToComponent(pair.Value, stableKey);
            }
        }

        public static void SaveComponent(CustomKeyEvent component)
        {
            if (component == null || PluginConfig.Instance == null)
            {
                return;
            }

            var stableKey = ResolveStableKey(component);
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            UpdateRuntimeKnownProfile(component, stableKey);

            if (component.IsUsingInitialDefaults())
            {
                RemoveProfile(stableKey);
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null)
            {
                profiles = new Dictionary<string, CustomKeyEventProfile>();
                PluginConfig.Instance.CustomKeyEventProfiles = profiles;
            }

            profiles[stableKey] = component.CreateProfileSnapshot();
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

            lock (syncRoot)
            {
                runtimeKnownProfilesByStableKey[stableKey] = profile;
            }

            if (IsProfileUsingBaseline(profile))
            {
                RemoveProfile(stableKey);
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

        public static void RemoveProfile(string stableKey)
        {
            if (string.IsNullOrWhiteSpace(stableKey) || PluginConfig.Instance == null)
            {
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles != null && profiles.Remove(stableKey))
            {
                PluginConfig.Instance.Changed();
            }

            RestoreRuntimeProfileToBaseline(stableKey);
        }

        public static bool IsProfileUsingBaseline(CustomKeyEventProfile profile)
        {
            if (profile == null || !profile.BaselineInitialized)
            {
                return false;
            }

            return profile.IndexTriggerButton == profile.BaselineIndexTriggerButton
                && profile.ViveTriggerButton == profile.BaselineViveTriggerButton
                && profile.OculusTriggerButton == profile.BaselineOculusTriggerButton
                && profile.WMRTriggerButton == profile.BaselineWMRTriggerButton
                && profile.EnableChordPress == profile.BaselineEnableChordPress
                && profile.IndexChordButton == profile.BaselineIndexChordButton
                && profile.ViveChordButton == profile.BaselineViveChordButton
                && profile.OculusChordButton == profile.BaselineOculusChordButton
                && profile.WMRChordButton == profile.BaselineWMRChordButton
                && profile.ClickEventsChange == profile.BaselineClickEventsChange
                && profile.DoubleClickEventsChange == profile.BaselineDoubleClickEventsChange
                && profile.LongClickEventsChange == profile.BaselineLongClickEventsChange
                && profile.PressEventsChange == profile.BaselinePressEventsChange
                && profile.HoldEventsChange == profile.BaselineHoldEventsChange
                && profile.ReleaseEventsChange == profile.BaselineReleaseEventsChange
                && profile.ReleaseAfterLongClickEventsChange == profile.BaselineReleaseAfterLongClickEventsChange
                && Mathf.Abs(profile.DoubleClickInterval - profile.BaselineDoubleClickInterval) < 0.0001f
                && Mathf.Abs(profile.LongClickInterval - profile.BaselineLongClickInterval) < 0.0001f;
        }

        public static bool TryGetRuntimeActiveDuration(string stableKey, out float activeSeconds)
        {
            activeSeconds = 0f;
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return false;
            }

            lock (syncRoot)
            {
                return runtimeActiveDurationByStableKey.TryGetValue(stableKey, out activeSeconds);
            }
        }

        public static bool TryGetRuntimeActiveDuration(CustomKeyEvent component, out float activeSeconds)
        {
            activeSeconds = 0f;
            if (component == null)
            {
                return false;
            }

            int instanceId = component.GetInstanceID();
            float committedActiveSeconds = 0f;
            float liveDeltaSeconds = 0f;
            bool hasValue = false;

            lock (syncRoot)
            {
                string stableKey;
                if (!registeredStableKeyByInstanceId.TryGetValue(instanceId, out stableKey)
                    || string.IsNullOrWhiteSpace(stableKey))
                {
                    return false;
                }

                if (runtimeActiveDurationByStableKey.TryGetValue(stableKey, out var committed))
                {
                    committedActiveSeconds = committed;
                    hasValue = true;
                }

                if (registeredActiveBaselineByInstanceId.TryGetValue(instanceId, out var baseline))
                {
                    liveDeltaSeconds = Mathf.Max(0f, component.GetActiveDurationSeconds() - baseline);
                    if (liveDeltaSeconds > 0f)
                    {
                        hasValue = true;
                    }
                }
            }

            activeSeconds = committedActiveSeconds + liveDeltaSeconds;
            return hasValue;
        }

        public static bool TryGetRegisteredStableKey(CustomKeyEvent component, out string stableKey)
        {
            stableKey = null;
            if (component == null)
            {
                return false;
            }

            int instanceId = component.GetInstanceID();
            lock (syncRoot)
            {
                if (registeredStableKeyByInstanceId.TryGetValue(instanceId, out var registeredStableKey)
                    && !string.IsNullOrWhiteSpace(registeredStableKey))
                {
                    stableKey = registeredStableKey;
                    return true;
                }
            }

            return false;
        }

        public static Dictionary<string, CustomKeyEventProfile> GetRuntimeKnownProfilesSnapshot()
        {
            lock (syncRoot)
            {
                return new Dictionary<string, CustomKeyEventProfile>(runtimeKnownProfilesByStableKey);
            }
        }

        public static void ReportRuntimeEvent(CustomKeyEvent component, CustomKeyEvent.ButtonEventType sourceEventType, CustomKeyEvent.ButtonEventType destinationEventType, float realtimeSeconds, int frameCount)
        {
            if (component == null)
            {
                return;
            }

            var stableKey = ResolveStableKey(component);
            ReportRuntimeEvent(stableKey, sourceEventType, destinationEventType, realtimeSeconds, frameCount);
        }

        public static bool TryGetRuntimeEventSnapshot(string stableKey, out RuntimeEventSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return false;
            }

            lock (syncRoot)
            {
                runtimeEventHistoryByStableKey.TryGetValue(stableKey, out var history);
                if (history == null || history.Count == 0)
                {
                    return false;
                }

                var copiedHistory = new List<RuntimeEventEntry>(history);
                var lastEvent = copiedHistory[copiedHistory.Count - 1];
                snapshot = new RuntimeEventSnapshot(stableKey, lastEvent, copiedHistory);
                return true;
            }
        }

        public static void SetRuntimeEventMonitorEnabled(bool enabled)
        {
            lock (syncRoot)
            {
                isRuntimeEventMonitorEnabled = enabled;
                if (enabled)
                {
                    return;
                }

                runtimeEventHistoryByStableKey.Clear();
                holdRecordedInCurrentPressByStableKey.Clear();
            }
        }

        private static void ApplyToComponent(CustomKeyEvent component, string stableKey)
        {
            if (component == null || PluginConfig.Instance == null)
            {
                return;
            }

            if (TryResolveProfile(stableKey, out var profile))
            {
                component.ApplyProfile(profile);
            }

            UpdateRuntimeKnownProfile(component, stableKey);
        }

        private static bool TryResolveProfile(string stableKey, out CustomKeyEventProfile profile)
        {
            profile = null;
            var config = PluginConfig.Instance;
            var profiles = config?.CustomKeyEventProfiles;
            if (profiles == null || profiles.Count == 0)
            {
                return false;
            }

            return profiles.TryGetValue(stableKey, out profile);
        }

        private static void AddRuntimeActiveDuration(string stableKey, float activeSeconds)
        {
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            lock (syncRoot)
            {
                if (activeSeconds <= 0f)
                {
                    return;
                }

                if (runtimeActiveDurationByStableKey.TryGetValue(stableKey, out var existing))
                {
                    runtimeActiveDurationByStableKey[stableKey] = existing + activeSeconds;
                }
                else
                {
                    runtimeActiveDurationByStableKey[stableKey] = activeSeconds;
                }
            }
        }

        private static void ReportRuntimeEvent(string stableKey, CustomKeyEvent.ButtonEventType sourceEventType, CustomKeyEvent.ButtonEventType destinationEventType, float realtimeSeconds, int frameCount)
        {
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            lock (syncRoot)
            {
                if (!isRuntimeEventMonitorEnabled)
                {
                    return;
                }
            }

            if (!ShouldRecordRuntimeEvent(stableKey, sourceEventType))
            {
                return;
            }

            var runtimeEvent = new RuntimeEventEntry(sourceEventType, destinationEventType, realtimeSeconds, frameCount);
            lock (syncRoot)
            {
                if (!runtimeEventHistoryByStableKey.TryGetValue(stableKey, out var history) || history == null)
                {
                    history = new List<RuntimeEventEntry>(maxRuntimeEventHistory);
                    runtimeEventHistoryByStableKey[stableKey] = history;
                }

                history.Add(runtimeEvent);
                if (history.Count > maxRuntimeEventHistory)
                {
                    history.RemoveAt(0);
                }
            }

            var handler = RuntimeEventObserved;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(stableKey);
            }
            catch (Exception ex)
            {
                CustomKeyEvents.Logger.log.Warn($"Failed to notify runtime event observers: {ex.Message}");
            }
        }

        private static bool ShouldRecordRuntimeEvent(string stableKey, CustomKeyEvent.ButtonEventType sourceEventType)
        {
            lock (syncRoot)
            {
                switch (sourceEventType)
                {
                    case CustomKeyEvent.ButtonEventType.Press:
                        holdRecordedInCurrentPressByStableKey[stableKey] = false;
                        return true;
                    case CustomKeyEvent.ButtonEventType.Hold:
                        if (holdRecordedInCurrentPressByStableKey.TryGetValue(stableKey, out var holdAlreadyRecorded) && holdAlreadyRecorded)
                        {
                            return false;
                        }

                        holdRecordedInCurrentPressByStableKey[stableKey] = true;
                        return true;
                    case CustomKeyEvent.ButtonEventType.Release:
                    case CustomKeyEvent.ButtonEventType.ReleaseAfterLongClick:
                        holdRecordedInCurrentPressByStableKey[stableKey] = false;
                        return true;
                    default:
                        return true;
                }
            }
        }

        private static void UpdateRuntimeKnownProfile(CustomKeyEvent component, string stableKey)
        {
            if (component == null || string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            var snapshot = component.CreateProfileSnapshot();
            lock (syncRoot)
            {
                runtimeKnownProfilesByStableKey[stableKey] = snapshot;
            }
        }

        private static void RestoreRuntimeProfileToBaseline(string stableKey)
        {
            if (string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            lock (syncRoot)
            {
                if (!runtimeKnownProfilesByStableKey.TryGetValue(stableKey, out var runtimeProfile)
                    || runtimeProfile == null
                    || !runtimeProfile.BaselineInitialized)
                {
                    return;
                }

                runtimeProfile.IndexTriggerButton = runtimeProfile.BaselineIndexTriggerButton;
                runtimeProfile.ViveTriggerButton = runtimeProfile.BaselineViveTriggerButton;
                runtimeProfile.OculusTriggerButton = runtimeProfile.BaselineOculusTriggerButton;
                runtimeProfile.WMRTriggerButton = runtimeProfile.BaselineWMRTriggerButton;
                runtimeProfile.EnableChordPress = runtimeProfile.BaselineEnableChordPress;
                runtimeProfile.IndexChordButton = runtimeProfile.BaselineIndexChordButton;
                runtimeProfile.ViveChordButton = runtimeProfile.BaselineViveChordButton;
                runtimeProfile.OculusChordButton = runtimeProfile.BaselineOculusChordButton;
                runtimeProfile.WMRChordButton = runtimeProfile.BaselineWMRChordButton;
                runtimeProfile.ClickEventsChange = runtimeProfile.BaselineClickEventsChange;
                runtimeProfile.DoubleClickEventsChange = runtimeProfile.BaselineDoubleClickEventsChange;
                runtimeProfile.LongClickEventsChange = runtimeProfile.BaselineLongClickEventsChange;
                runtimeProfile.PressEventsChange = runtimeProfile.BaselinePressEventsChange;
                runtimeProfile.HoldEventsChange = runtimeProfile.BaselineHoldEventsChange;
                runtimeProfile.ReleaseEventsChange = runtimeProfile.BaselineReleaseEventsChange;
                runtimeProfile.ReleaseAfterLongClickEventsChange = runtimeProfile.BaselineReleaseAfterLongClickEventsChange;
                runtimeProfile.DoubleClickInterval = runtimeProfile.BaselineDoubleClickInterval;
                runtimeProfile.LongClickInterval = runtimeProfile.BaselineLongClickInterval;
                runtimeProfile.CurrentKeyConfigurationSignature = runtimeProfile.InitialKeyConfigurationSignature;
            }
        }

        private static void UpdatePersistedProfileMetadataIfExists(CustomKeyEvent component, string stableKey)
        {
            if (component == null || PluginConfig.Instance == null || string.IsNullOrWhiteSpace(stableKey))
            {
                return;
            }

            var profiles = PluginConfig.Instance.CustomKeyEventProfiles;
            if (profiles == null || !profiles.TryGetValue(stableKey, out var profile))
            {
                return;
            }

            bool changed = false;
            if (!profile.BaselineInitialized)
            {
                component.WriteBaselineToProfile(profile);
                changed = true;
            }

            if (UpdateProfileMetadata(profile, component))
            {
                changed = true;
            }

            if (IsProfileUsingBaseline(profile))
            {
                profiles.Remove(stableKey);
                changed = true;
            }

            if (changed)
            {
                PluginConfig.Instance.Changed();
            }
        }

        private static string ResolveStableKey(CustomKeyEvent component)
        {
            if (component == null)
            {
                return string.Empty;
            }

            if (TryGetRegisteredStableKey(component, out var registeredStableKey))
            {
                return registeredStableKey;
            }

            return component.GetStableProfileKey();
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
            string currentSignature = component.GetKeyConfigurationSignature();

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

            if (!string.Equals(profile.CurrentKeyConfigurationSignature, currentSignature))
            {
                profile.CurrentKeyConfigurationSignature = currentSignature;
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
