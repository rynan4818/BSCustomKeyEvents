using System.Runtime.CompilerServices;
using System.Collections.Generic;
using AvatarScriptPack;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace CustomKeyEvents.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        [UseConverter(typeof(DictionaryConverter<CustomKeyEventProfile>))]
        public virtual Dictionary<string, CustomKeyEventProfile> CustomKeyEventProfiles { get; set; } = new Dictionary<string, CustomKeyEventProfile>();

        public virtual bool IncludeHierarchyPathInIdentity { get; set; } = false;

        public virtual void OnReload()
        {
            CustomKeyEventSettingsStore.ReapplyRegisteredComponents();
        }

        public virtual void Changed()
        {
        }

        public virtual void CopyFrom(PluginConfig other)
        {
            if (other == null)
            {
                return;
            }

            CustomKeyEventProfiles = other.CustomKeyEventProfiles != null
                ? new Dictionary<string, CustomKeyEventProfile>(other.CustomKeyEventProfiles)
                : new Dictionary<string, CustomKeyEventProfile>();
            IncludeHierarchyPathInIdentity = other.IncludeHierarchyPathInIdentity;
        }
    }
}
