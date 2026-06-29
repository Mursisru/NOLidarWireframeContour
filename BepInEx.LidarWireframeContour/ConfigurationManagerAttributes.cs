#nullable disable
using System;
using BepInEx.Configuration;

namespace LidarWireframeContour.BepInEx
{
    internal sealed class ConfigurationManagerAttributes
    {
        public bool? ShowRangeAsPercent = null;
        public Action<ConfigEntryBase> CustomDrawer = null;
        public CustomHotkeyDrawerFunc CustomHotkeyDrawer = null;
        public bool? Browsable = null;
        public string Category = null;
        public object DefaultValue = null;
        public bool? HideDefaultButton = null;
        public bool? HideSettingName = null;
        public string Description = null;
        public string DispName = null;
        public int? Order = null;
        public bool? ReadOnly = null;
        public bool? IsAdvanced = null;
        public Func<object, string> ObjToStr = null;
        public Func<string, object> StrToObj = null;

        public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
    }
}
