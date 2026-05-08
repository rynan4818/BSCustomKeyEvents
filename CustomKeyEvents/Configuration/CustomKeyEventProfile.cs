using AvatarScriptPack;

namespace CustomKeyEvents.Configuration
{
    internal class CustomKeyEventProfile
    {
        public virtual string HierarchyPath { get; set; } = string.Empty;
        public virtual int ComponentOrdinal { get; set; }
        public virtual string InitialKeyConfigurationSignature { get; set; } = string.Empty;
        public virtual string CurrentKeyConfigurationSignature { get; set; } = string.Empty;
        public virtual bool BaselineInitialized { get; set; }
        public virtual CustomKeyEvent.IndexButton BaselineIndexTriggerButton { get; set; } = CustomKeyEvent.IndexButton.None;
        public virtual CustomKeyEvent.ViveButton BaselineViveTriggerButton { get; set; } = CustomKeyEvent.ViveButton.None;
        public virtual CustomKeyEvent.OculusButton BaselineOculusTriggerButton { get; set; } = CustomKeyEvent.OculusButton.None;
        public virtual CustomKeyEvent.WMRButton BaselineWMRTriggerButton { get; set; } = CustomKeyEvent.WMRButton.None;
        public virtual bool BaselineEnableChordPress { get; set; }
        public virtual CustomKeyEvent.IndexButton BaselineIndexChordButton { get; set; } = CustomKeyEvent.IndexButton.None;
        public virtual CustomKeyEvent.ViveButton BaselineViveChordButton { get; set; } = CustomKeyEvent.ViveButton.None;
        public virtual CustomKeyEvent.OculusButton BaselineOculusChordButton { get; set; } = CustomKeyEvent.OculusButton.None;
        public virtual CustomKeyEvent.WMRButton BaselineWMRChordButton { get; set; } = CustomKeyEvent.WMRButton.None;
        public virtual CustomKeyEvent.EventRouteTarget BaselineClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselineDoubleClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselineLongClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselinePressEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselineHoldEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselineReleaseEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget BaselineReleaseAfterLongClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual float BaselineDoubleClickInterval { get; set; } = 0.5f;
        public virtual float BaselineLongClickInterval { get; set; } = 0.6f;
        public virtual CustomKeyEvent.IndexButton IndexTriggerButton { get; set; } = CustomKeyEvent.IndexButton.None;
        public virtual CustomKeyEvent.ViveButton ViveTriggerButton { get; set; } = CustomKeyEvent.ViveButton.None;
        public virtual CustomKeyEvent.OculusButton OculusTriggerButton { get; set; } = CustomKeyEvent.OculusButton.None;
        public virtual CustomKeyEvent.WMRButton WMRTriggerButton { get; set; } = CustomKeyEvent.WMRButton.None;
        public virtual bool EnableChordPress { get; set; }
        public virtual CustomKeyEvent.IndexButton IndexChordButton { get; set; } = CustomKeyEvent.IndexButton.None;
        public virtual CustomKeyEvent.ViveButton ViveChordButton { get; set; } = CustomKeyEvent.ViveButton.None;
        public virtual CustomKeyEvent.OculusButton OculusChordButton { get; set; } = CustomKeyEvent.OculusButton.None;
        public virtual CustomKeyEvent.WMRButton WMRChordButton { get; set; } = CustomKeyEvent.WMRButton.None;
        public virtual CustomKeyEvent.EventRouteTarget ClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget DoubleClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget LongClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget PressEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget HoldEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget ReleaseEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual CustomKeyEvent.EventRouteTarget ReleaseAfterLongClickEventsChange { get; set; } = CustomKeyEvent.EventRouteTarget.NoChange;
        public virtual bool HasClickEvents { get; set; }
        public virtual bool HasDoubleClickEvents { get; set; }
        public virtual bool HasLongClickEvents { get; set; }
        public virtual bool HasPressEvents { get; set; }
        public virtual bool HasHoldEvents { get; set; }
        public virtual bool HasReleaseEvents { get; set; }
        public virtual bool HasReleaseAfterLongClickEvents { get; set; }
        public virtual float DoubleClickInterval { get; set; } = 0.5f;
        public virtual float LongClickInterval { get; set; } = 0.6f;
    }
}
