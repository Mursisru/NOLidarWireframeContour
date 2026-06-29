using System;
using BepInEx.Configuration;
using NOLoader.LidarWireframeContour;
using UnityEngine;

namespace LidarWireframeContour.BepInEx
{
    internal static class LidarWireframeBepInConfig
    {
        private const string General = "General";
        private const string Probe = "Probe";
        private const string Activation = "Activation";
        private const string Fade = "Fade";
        private const string Visual = "Visual";
        private const string Debug = "Debug";

        internal static bool IsBound { get; private set; }

        internal static ConfigEntry<bool> Enabled { get; private set; } = null!;
        internal static ConfigEntry<float> ProbeIntervalSec { get; private set; } = null!;
        internal static ConfigEntry<float> ProbeIntervalNearSec { get; private set; } = null!;
        internal static ConfigEntry<float> TtiActivateSec { get; private set; } = null!;
        internal static ConfigEntry<float> FadeOutSec { get; private set; } = null!;
        internal static ConfigEntry<float> FadeInSec { get; private set; } = null!;
        internal static ConfigEntry<float> FadeInUrgentSec { get; private set; } = null!;
        internal static ConfigEntry<float> UniformSmoothSec { get; private set; } = null!;
        internal static ConfigEntry<float> HoldAfterEscapeSec { get; private set; } = null!;
        internal static ConfigEntry<bool> ForceKeepDepthTextureActive { get; private set; } = null!;
        internal static ConfigEntry<float> DebugForceBlend { get; private set; } = null!;
        internal static ConfigEntry<int> DebugShaderMode { get; private set; } = null!;
        internal static ConfigEntry<bool> DebugLogVerbose { get; private set; } = null!;
        internal static ConfigEntry<string> OutputCameraName { get; private set; } = null!;
        internal static ConfigEntry<float> CastMaxDistanceM { get; private set; } = null!;
        internal static ConfigEntry<float> CastRadiusNearM { get; private set; } = null!;
        internal static ConfigEntry<float> CastRadiusFarM { get; private set; } = null!;
        internal static ConfigEntry<float> MinSpeedMps { get; private set; } = null!;
        internal static ConfigEntry<float> SafeAglMeters { get; private set; } = null!;
        internal static ConfigEntry<float> DepthClipMarginM { get; private set; } = null!;
        internal static ConfigEntry<float> NearClipM { get; private set; } = null!;
        internal static ConfigEntry<float> ImpactBandHalfM { get; private set; } = null!;
        internal static ConfigEntry<float> ConeHalfAngleDeg { get; private set; } = null!;
        internal static ConfigEntry<string> LidarColorHex { get; private set; } = null!;
        internal static ConfigEntry<int> TerrainLayerMask { get; private set; } = null!;
        internal static ConfigEntry<float> EdgeThreshold { get; private set; } = null!;
        internal static ConfigEntry<float> EdgeStrength { get; private set; } = null!;
        internal static ConfigEntry<float> EdgeThinPow { get; private set; } = null!;
        internal static ConfigEntry<float> EdgeTexelScale { get; private set; } = null!;
        internal static ConfigEntry<float> NoiseStrength { get; private set; } = null!;
        internal static ConfigEntry<float> DistanceFadeMeters { get; private set; } = null!;
        internal static ConfigEntry<float> ConeFalloffCos { get; private set; } = null!;
        internal static ConfigEntry<float> HudBrightness { get; private set; } = null!;
        internal static ConfigEntry<float> AppearBootSec { get; private set; } = null!;
        internal static ConfigEntry<float> AppearBootFreqStart { get; private set; } = null!;
        internal static ConfigEntry<float> AppearBootFreqEnd { get; private set; } = null!;
        internal static ConfigEntry<float> AppearBootDim { get; private set; } = null!;
        internal static ConfigEntry<bool> BlockWhenGearDeployed { get; private set; } = null!;
        internal static ConfigEntry<bool> BlockDuringDaytime { get; private set; } = null!;
        internal static ConfigEntry<float> DaytimeStartHour { get; private set; } = null!;
        internal static ConfigEntry<float> DaytimeEndHour { get; private set; } = null!;
        internal static ConfigEntry<bool> ForceHotkeyEnabled { get; private set; } = null!;
        internal static ConfigEntry<string> ForceHotkeyBinding { get; private set; } = null!;

        internal static void Bind(ConfigFile config)
        {
            var defaults = LidarSettingsSnapshot.CreateDefaults();

            Enabled = BindBool(config, General, "Enabled", defaults.Enabled, "Master switch.", 0);
            ProbeIntervalSec = BindFloat(config, Probe, "ProbeIntervalSec", defaults.ProbeIntervalSec, "Probe interval cruise (seconds).", 0, 0.05f, 2f);
            ProbeIntervalNearSec = BindFloat(config, Probe, "ProbeIntervalNearSec", defaults.ProbeIntervalNearSec, "Probe interval near TTI (seconds).", 1, 0.02f, 1f);
            TtiActivateSec = BindFloat(config, Probe, "TtiActivateSec", defaults.TtiActivateSec, "Activate below this TTI (seconds).", 2, 0.5f, 30f);
            MinSpeedMps = BindFloat(config, Probe, "MinSpeedMps", defaults.MinSpeedMps, "Minimum speed for lidar probe.", 3, 1f, 200f);
            SafeAglMeters = BindFloat(config, Probe, "SafeAglMeters", defaults.SafeAglMeters, "Disable above this AGL.", 4, 0f, 5000f);
            CastMaxDistanceM = BindFloat(config, Probe, "CastMaxDistanceM", defaults.CastMaxDistanceM, "Max cast range (m).", 5, 100f, 5000f);
            CastRadiusNearM = BindFloat(config, Probe, "CastRadiusNearM", defaults.CastRadiusNearM, "Near SphereCast radius (m).", 6, 0.5f, 20f);
            CastRadiusFarM = BindFloat(config, Probe, "CastRadiusFarM", defaults.CastRadiusFarM, "Far SphereCast radius (m).", 7, 1f, 100f);

            BlockWhenGearDeployed = BindBool(config, Activation, "BlockWhenGearDeployed", defaults.BlockWhenGearDeployed, "Block auto-activation when gear deployed.", 0);
            BlockDuringDaytime = BindBool(config, Activation, "BlockDuringDaytime", defaults.BlockDuringDaytime, "Block auto-activation during daytime.", 1);
            DaytimeStartHour = BindFloat(config, Activation, "DaytimeStartHour", defaults.DaytimeStartHour, "Day start hour (0-24).", 2, 0f, 24f);
            DaytimeEndHour = BindFloat(config, Activation, "DaytimeEndHour", defaults.DaytimeEndHour, "Day end hour (0-24).", 3, 0f, 24f);
            ForceHotkeyEnabled = BindBool(config, Activation, "ForceHotkeyEnabled", defaults.ForceHotkeyEnabled, "Enable forced night hotkey.", 4);
            ForceHotkeyBinding = BindString(config, Activation, "ForceHotkeyBinding", defaults.ForceHotkeyBinding, "Physical key binding (e.g. Y).", 5);

            FadeOutSec = BindFloat(config, Fade, "FadeOutSec", defaults.FadeOutSec, "Shader fade-out duration.", 0, 0.05f, 3f);
            FadeInSec = BindFloat(config, Fade, "FadeInSec", defaults.FadeInSec, "Shader fade-in duration.", 1, 0.05f, 3f);
            FadeInUrgentSec = BindFloat(config, Fade, "FadeInUrgentSec", defaults.FadeInUrgentSec, "Faster fade when TTI already low.", 2, 0.03f, 3f);
            UniformSmoothSec = BindFloat(config, Fade, "UniformSmoothSec", defaults.UniformSmoothSec, "Cone/distance smoothing time constant.", 3, 0.05f, 1f);
            HoldAfterEscapeSec = BindFloat(config, Fade, "HoldAfterEscapeSec", defaults.HoldAfterEscapeSec, "Hold after leaving collision threat.", 4, 0f, 5f);

            LidarColorHex = BindString(config, Visual, "LidarColorHex", "#00CC66", "Wireframe tint (#RRGGBB).", 0);
            HudBrightness = BindFloat(config, Visual, "HudBrightness", defaults.HudBrightness, "HUD intensity multiplier.", 1, 0.1f, 1f);
            ConeHalfAngleDeg = BindFloat(config, Visual, "ConeHalfAngleDeg", defaults.ConeHalfAngleDeg, "Velocity cone half-angle (deg).", 2, 0.5f, 25f);
            ConeFalloffCos = BindFloat(config, Visual, "ConeFalloffCos", defaults.ConeFalloffCos, "Cone edge softness.", 3, 0.005f, 0.2f);
            EdgeThreshold = BindFloat(config, Visual, "EdgeThreshold", defaults.EdgeThreshold, "Laplacian edge threshold.", 4, 0.01f, 1f);
            EdgeStrength = BindFloat(config, Visual, "EdgeStrength", defaults.EdgeStrength, "Edge intensity.", 5, 0f, 5f);
            EdgeThinPow = BindFloat(config, Visual, "EdgeThinPow", defaults.EdgeThinPow, "Line thinning exponent.", 6, 1f, 8f);
            EdgeTexelScale = BindFloat(config, Visual, "EdgeTexelScale", defaults.EdgeTexelScale, "Depth sample stride.", 7, 0.25f, 1.5f);
            NoiseStrength = BindFloat(config, Visual, "NoiseStrength", defaults.NoiseStrength, "Legacy CRT noise strength.", 8, 0f, 1f);
            DistanceFadeMeters = BindFloat(config, Visual, "DistanceFadeMeters", defaults.DistanceFadeMeters, "Soft range fade.", 9, 50f, 500f);
            DepthClipMarginM = BindFloat(config, Visual, "DepthClipMarginM", defaults.DepthClipMarginM, "Depth clip margin (m).", 10, 0f, 50f);
            NearClipM = BindFloat(config, Visual, "NearClipM", defaults.NearClipM, "Near clip (m).", 11, 10f, 500f);
            ImpactBandHalfM = BindFloat(config, Visual, "ImpactBandHalfM", defaults.ImpactBandHalfM, "Impact band half-width (m).", 12, 20f, 500f);
            TerrainLayerMask = BindInt(config, Visual, "TerrainLayerMask", defaults.TerrainLayerMask, "Physics layer mask for terrain.", 13);
            AppearBootSec = BindFloat(config, Visual, "AppearBootSec", defaults.AppearBootSec, "Boot strobe duration.", 14, 0.1f, 2f);
            AppearBootFreqStart = BindFloat(config, Visual, "AppearBootFreqStart", defaults.AppearBootFreqStart, "Boot strobe freq start.", 15, 1f, 40f);
            AppearBootFreqEnd = BindFloat(config, Visual, "AppearBootFreqEnd", defaults.AppearBootFreqEnd, "Boot strobe freq end.", 16, 2f, 80f);
            AppearBootDim = BindFloat(config, Visual, "AppearBootDim", defaults.AppearBootDim, "Boot dim level.", 17, 0f, 1f);

            ForceKeepDepthTextureActive = BindBoolAdv(config, Debug, "ForceKeepDepthTextureActive", defaults.ForceKeepDepthTextureActive, "Pin URP depth texture.", 0);
            DebugForceBlend = BindFloatAdv(config, Debug, "DebugForceBlend", defaults.DebugForceBlend, "Force combat GPU path (0-1).", 1, 0f, 1f);
            DebugShaderMode = BindIntAdv(config, Debug, "DebugShaderMode", defaults.DebugShaderMode, "Shader debug mode 0-6.", 2, 0, 6);
            DebugLogVerbose = BindBoolAdv(config, Debug, "DebugLogVerbose", defaults.DebugLogVerbose, "Verbose debug log file.", 3);
            OutputCameraName = BindStringAdv(config, Debug, "OutputCameraName", string.Empty, "Composite camera override (empty = main).", 4);

            IsBound = true;
            SyncToRuntime();
        }

        internal static void SyncToRuntime()
        {
            if (!IsBound)
                return;

            var snapshot = LidarSettingsSnapshot.CreateDefaults();
            snapshot.Enabled = Enabled.Value;
            snapshot.ProbeIntervalSec = ProbeIntervalSec.Value;
            snapshot.ProbeIntervalNearSec = ProbeIntervalNearSec.Value;
            snapshot.TtiActivateSec = TtiActivateSec.Value;
            snapshot.FadeOutSec = FadeOutSec.Value;
            snapshot.FadeInSec = FadeInSec.Value;
            snapshot.FadeInUrgentSec = FadeInUrgentSec.Value;
            snapshot.UniformSmoothSec = UniformSmoothSec.Value;
            snapshot.HoldAfterEscapeSec = HoldAfterEscapeSec.Value;
            snapshot.ForceKeepDepthTextureActive = ForceKeepDepthTextureActive.Value;
            snapshot.DebugForceBlend = DebugForceBlend.Value;
            snapshot.DebugShaderMode = DebugShaderMode.Value;
            snapshot.DebugLogVerbose = DebugLogVerbose.Value;
            snapshot.OutputCameraName = OutputCameraName.Value ?? string.Empty;
            snapshot.CastMaxDistanceM = CastMaxDistanceM.Value;
            snapshot.CastRadiusNearM = CastRadiusNearM.Value;
            snapshot.CastRadiusFarM = CastRadiusFarM.Value;
            snapshot.MinSpeedMps = MinSpeedMps.Value;
            snapshot.SafeAglMeters = SafeAglMeters.Value;
            snapshot.DepthClipMarginM = DepthClipMarginM.Value;
            snapshot.NearClipM = NearClipM.Value;
            snapshot.ImpactBandHalfM = ImpactBandHalfM.Value;
            snapshot.ConeHalfAngleDeg = ConeHalfAngleDeg.Value;
            snapshot.EdgeThreshold = EdgeThreshold.Value;
            snapshot.EdgeStrength = EdgeStrength.Value;
            snapshot.EdgeThinPow = EdgeThinPow.Value;
            snapshot.EdgeTexelScale = EdgeTexelScale.Value;
            snapshot.NoiseStrength = NoiseStrength.Value;
            snapshot.DistanceFadeMeters = DistanceFadeMeters.Value;
            snapshot.ConeFalloffCos = ConeFalloffCos.Value;
            snapshot.HudBrightness = HudBrightness.Value;
            snapshot.AppearBootSec = AppearBootSec.Value;
            snapshot.AppearBootFreqStart = AppearBootFreqStart.Value;
            snapshot.AppearBootFreqEnd = AppearBootFreqEnd.Value;
            snapshot.AppearBootDim = AppearBootDim.Value;
            snapshot.BlockWhenGearDeployed = BlockWhenGearDeployed.Value;
            snapshot.BlockDuringDaytime = BlockDuringDaytime.Value;
            snapshot.DaytimeStartHour = DaytimeStartHour.Value;
            snapshot.DaytimeEndHour = DaytimeEndHour.Value;
            snapshot.ForceHotkeyEnabled = ForceHotkeyEnabled.Value;
            snapshot.ForceHotkeyBinding = ForceHotkeyBinding.Value ?? "Y";
            snapshot.TerrainLayerMask = TerrainLayerMask.Value;

            string colorHex = LidarColorHex.Value ?? "#00CC66";
            if (!ColorUtility.TryParseHtmlString(colorHex, out Color parsed))
                parsed = snapshot.LidarColor;
            snapshot.LidarColor = parsed;

            LidarConfig.ApplySnapshot(snapshot);
        }

        private static ConfigEntry<bool> BindBool(ConfigFile config, string section, string key, bool defaultValue, string desc, int order)
        {
            var entry = config.Bind(section, key, defaultValue, Desc(desc, order));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<bool> BindBoolAdv(ConfigFile config, string section, string key, bool defaultValue, string desc, int order)
        {
            var entry = config.Bind(section, key, defaultValue, DescAdv(desc, order));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<float> BindFloat(ConfigFile config, string section, string key, float defaultValue, string desc, int order, float min, float max)
        {
            var entry = config.Bind(section, key, defaultValue, new ConfigDescription(desc, new AcceptableValueRange<float>(min, max), Cm(order)));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<float> BindFloatAdv(ConfigFile config, string section, string key, float defaultValue, string desc, int order, float min, float max)
        {
            var entry = config.Bind(section, key, defaultValue, new ConfigDescription(desc, new AcceptableValueRange<float>(min, max), CmAdv(order)));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<int> BindInt(ConfigFile config, string section, string key, int defaultValue, string desc, int order)
        {
            var entry = config.Bind(section, key, defaultValue, Desc(desc, order));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<int> BindIntAdv(ConfigFile config, string section, string key, int defaultValue, string desc, int order, int min, int max)
        {
            var entry = config.Bind(section, key, defaultValue, new ConfigDescription(desc, new AcceptableValueRange<int>(min, max), CmAdv(order)));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<string> BindString(ConfigFile config, string section, string key, string defaultValue, string desc, int order)
        {
            var entry = config.Bind(section, key, defaultValue, Desc(desc, order));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigEntry<string> BindStringAdv(ConfigFile config, string section, string key, string defaultValue, string desc, int order)
        {
            var entry = config.Bind(section, key, defaultValue, DescAdv(desc, order));
            entry.SettingChanged += OnSettingChanged;
            return entry;
        }

        private static ConfigDescription Desc(string desc, int order) =>
            new ConfigDescription(desc, null, Cm(order));

        private static ConfigDescription DescAdv(string desc, int order) =>
            new ConfigDescription(desc, null, CmAdv(order));

        private static ConfigurationManagerAttributes Cm(int order) =>
            new ConfigurationManagerAttributes { Order = order };

        private static ConfigurationManagerAttributes CmAdv(int order) =>
            new ConfigurationManagerAttributes { Order = order, IsAdvanced = true };

        private static void OnSettingChanged(object sender, EventArgs e) => SyncToRuntime();
    }
}
