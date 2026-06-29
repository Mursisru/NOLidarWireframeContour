using NOLoader.ModConfig;
using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarIniConfigLoader
    {
        internal static void Register()
        {
            LidarConfig.LoadFromModRoot = modRoot => ApplyFromIni(modRoot, ensureDefault: true);
            LidarConfig.ReloadFromModRoot = modRoot => ApplyFromIni(modRoot, ensureDefault: false);
        }

        private static void ApplyFromIni(string modRoot, bool ensureDefault)
        {
            string defaults = LidarIniDefaults.IniSection + "\n" + LidarIniDefaults.DefaultIniBody;

            if (ensureDefault)
                ModIniConfig.EnsureDefault(modRoot, defaults);

            var cfg = ModIniConfig.Load(modRoot);
            var snapshot = LidarSettingsSnapshot.CreateDefaults();

            snapshot.Enabled = cfg.GetBool("Lidar", "Enabled", snapshot.Enabled);
            snapshot.ProbeIntervalSec = cfg.GetFloat("Lidar", "ProbeIntervalSec", snapshot.ProbeIntervalSec);
            snapshot.ProbeIntervalNearSec = cfg.GetFloat("Lidar", "ProbeIntervalNearSec", snapshot.ProbeIntervalNearSec);
            snapshot.TtiActivateSec = cfg.GetFloat("Lidar", "TtiActivateSec", snapshot.TtiActivateSec);
            snapshot.FadeOutSec = cfg.GetFloat("Lidar", "FadeOutSec", snapshot.FadeOutSec);
            snapshot.FadeInSec = cfg.GetFloat("Lidar", "FadeInSec", snapshot.FadeInSec);
            snapshot.FadeInUrgentSec = cfg.GetFloat("Lidar", "FadeInUrgentSec", snapshot.FadeInUrgentSec);
            snapshot.UniformSmoothSec = cfg.GetFloat("Lidar", "UniformSmoothSec", snapshot.UniformSmoothSec);
            snapshot.HoldAfterEscapeSec = cfg.GetFloat("Lidar", "HoldAfterEscapeSec", snapshot.HoldAfterEscapeSec);
            snapshot.ForceKeepDepthTextureActive = cfg.GetBool("Lidar", "ForceKeepDepthTextureActive", snapshot.ForceKeepDepthTextureActive);
            snapshot.DebugForceBlend = cfg.GetFloat("Lidar", "DebugForceBlend", snapshot.DebugForceBlend);
            snapshot.DebugShaderMode = cfg.GetInt("Lidar", "DebugShaderMode", snapshot.DebugShaderMode);
            snapshot.DebugLogVerbose = cfg.GetBool("Lidar", "DebugLogVerbose", snapshot.DebugLogVerbose);
            snapshot.OutputCameraName = cfg.GetString("Lidar", "OutputCameraName", snapshot.OutputCameraName) ?? string.Empty;
            snapshot.CastMaxDistanceM = cfg.GetFloat("Lidar", "CastMaxDistanceM", snapshot.CastMaxDistanceM);
            snapshot.CastRadiusNearM = cfg.GetFloat("Lidar", "CastRadiusNearM", snapshot.CastRadiusNearM);
            snapshot.CastRadiusFarM = cfg.GetFloat("Lidar", "CastRadiusFarM", snapshot.CastRadiusFarM);
            snapshot.MinSpeedMps = cfg.GetFloat("Lidar", "MinSpeedMps", snapshot.MinSpeedMps);
            snapshot.SafeAglMeters = cfg.GetFloat("Lidar", "SafeAglMeters", snapshot.SafeAglMeters);
            snapshot.DepthClipMarginM = cfg.GetFloat("Lidar", "DepthClipMarginM", snapshot.DepthClipMarginM);
            snapshot.NearClipM = cfg.GetFloat("Lidar", "NearClipM", snapshot.NearClipM);
            snapshot.ImpactBandHalfM = cfg.GetFloat("Lidar", "ImpactBandHalfM", snapshot.ImpactBandHalfM);
            snapshot.ConeHalfAngleDeg = cfg.GetFloat("Lidar", "ConeHalfAngleDeg", snapshot.ConeHalfAngleDeg);
            snapshot.EdgeThreshold = cfg.GetFloat("Lidar", "EdgeThreshold", snapshot.EdgeThreshold);
            snapshot.EdgeStrength = cfg.GetFloat("Lidar", "EdgeStrength", snapshot.EdgeStrength);
            snapshot.EdgeThinPow = cfg.GetFloat("Lidar", "EdgeThinPow", snapshot.EdgeThinPow);
            snapshot.EdgeTexelScale = cfg.GetFloat("Lidar", "EdgeTexelScale", snapshot.EdgeTexelScale);
            snapshot.NoiseStrength = cfg.GetFloat("Lidar", "NoiseStrength", snapshot.NoiseStrength);
            snapshot.DistanceFadeMeters = cfg.GetFloat("Lidar", "DistanceFadeMeters", snapshot.DistanceFadeMeters);
            snapshot.ConeFalloffCos = cfg.GetFloat("Lidar", "ConeFalloffCos", snapshot.ConeFalloffCos);
            snapshot.HudBrightness = cfg.GetFloat("Lidar", "HudBrightness", snapshot.HudBrightness);
            snapshot.AppearBootSec = cfg.GetFloat("Lidar", "AppearBootSec", snapshot.AppearBootSec);
            snapshot.AppearBootFreqStart = cfg.GetFloat("Lidar", "AppearBootFreqStart", snapshot.AppearBootFreqStart);
            snapshot.AppearBootFreqEnd = cfg.GetFloat("Lidar", "AppearBootFreqEnd", snapshot.AppearBootFreqEnd);
            snapshot.AppearBootDim = cfg.GetFloat("Lidar", "AppearBootDim", snapshot.AppearBootDim);
            snapshot.BlockWhenGearDeployed = cfg.GetBool("Lidar", "BlockWhenGearDeployed", snapshot.BlockWhenGearDeployed);
            snapshot.BlockDuringDaytime = cfg.GetBool("Lidar", "BlockDuringDaytime", snapshot.BlockDuringDaytime);
            snapshot.DaytimeStartHour = cfg.GetFloat("Lidar", "DaytimeStartHour", snapshot.DaytimeStartHour);
            snapshot.DaytimeEndHour = cfg.GetFloat("Lidar", "DaytimeEndHour", snapshot.DaytimeEndHour);
            snapshot.ForceHotkeyEnabled = cfg.GetBool("Lidar", "ForceHotkeyEnabled", snapshot.ForceHotkeyEnabled);
            snapshot.ForceHotkeyBinding = cfg.GetString("Lidar", "ForceHotkeyBinding", snapshot.ForceHotkeyBinding) ?? "Y";

            string colorHex = cfg.GetString("Lidar", "LidarColorHex", "#00CC66");
            if (!ColorUtility.TryParseHtmlString(colorHex, out Color parsed))
                parsed = snapshot.LidarColor;
            snapshot.LidarColor = parsed;

            snapshot.TerrainLayerMask = cfg.GetInt("Lidar", "TerrainLayerMask", snapshot.TerrainLayerMask);

            LidarConfig.ApplySnapshot(snapshot);
        }
    }
}
