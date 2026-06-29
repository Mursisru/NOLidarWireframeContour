using System;
using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarConfig
    {
        internal static bool Enabled = true;
        internal static float ProbeIntervalSec = 0.2f;
        internal static float ProbeIntervalNearSec = 0.05f;
        internal static float TtiActivateSec = 7f;
        internal static float FadeOutSec = 0.3f;
        internal static float FadeInSec = 0.3f;
        internal static float FadeInUrgentSec = 0.12f;
        internal static float UniformSmoothSec = 0.32f;
        internal static float HoldAfterEscapeSec = 1f;
        internal static bool ForceKeepDepthTextureActive;
        internal static float DebugForceBlend;
        internal static int DebugShaderMode;
        internal static bool DebugLogVerbose;
        internal static string OutputCameraName = string.Empty;
        internal static float CastMaxDistanceM = 1500f;
        internal static float CastRadiusNearM = 2f;
        internal static float CastRadiusFarM = 50f;
        internal static float MinSpeedMps = 30f;
        internal static float SafeAglMeters = 500f;
        internal static float DepthClipMarginM = 5f;
        internal static float NearClipM = 80f;
        internal static float ImpactBandHalfM = 200f;
        internal static float ConeHalfAngleDeg = 15f;
        internal static Color LidarColor = new Color(0f, 1f, 0.4f, 1f);
        internal static int TerrainLayerMask = 2112;
        internal static float EdgeThreshold = 0.20f;
        internal static float EdgeStrength = 1.6f;
        internal static float EdgeThinPow = 4.2f;
        internal static float EdgeTexelScale = 0.50f;
        internal static float NoiseStrength = 0.15f;
        internal static float DistanceFadeMeters = 175f;
        internal static float ConeFalloffCos = 0.05f;
        internal static float HudBrightness = 0.62f;
        internal static float AppearBootSec = 0.5f;
        internal static float AppearBootFreqStart = 6f;
        internal static float AppearBootFreqEnd = 40f;
        internal static float AppearBootDim = 0f;
        internal static bool BlockWhenGearDeployed = true;
        internal static bool BlockDuringDaytime = true;
        internal static float DaytimeStartHour = 6f;
        internal static float DaytimeEndHour = 18f;
        internal static bool ForceHotkeyEnabled = true;
        internal static string ForceHotkeyBinding = "Y";

        internal static Action<string>? LoadFromModRoot { get; set; }
        internal static Action<string>? ReloadFromModRoot { get; set; }

        internal static float ConeCosHalfAngle =>
            Mathf.Cos(ConeHalfAngleDeg * Mathf.Deg2Rad);

        internal static void Load(string modRoot) => LoadFromModRoot?.Invoke(modRoot);

        internal static void Reload(string modRoot) => ReloadFromModRoot?.Invoke(modRoot);

        internal static void ApplySnapshot(LidarSettingsSnapshot raw)
        {
            Enabled = raw.Enabled;
            ProbeIntervalSec = Mathf.Max(0.05f, raw.ProbeIntervalSec);
            ProbeIntervalNearSec = Mathf.Clamp(raw.ProbeIntervalNearSec, 0.02f, ProbeIntervalSec);
            TtiActivateSec = Mathf.Max(0.5f, raw.TtiActivateSec);
            FadeOutSec = Mathf.Max(0.05f, raw.FadeOutSec);
            FadeInSec = Mathf.Max(0.05f, raw.FadeInSec);
            FadeInUrgentSec = Mathf.Clamp(raw.FadeInUrgentSec, 0.03f, FadeInSec);
            UniformSmoothSec = Mathf.Clamp(raw.UniformSmoothSec, 0.05f, 1f);
            HoldAfterEscapeSec = Mathf.Clamp(raw.HoldAfterEscapeSec, 0f, 5f);
            ForceKeepDepthTextureActive = raw.ForceKeepDepthTextureActive;
            DebugForceBlend = Mathf.Clamp01(raw.DebugForceBlend);
            DebugShaderMode = Mathf.Clamp(raw.DebugShaderMode, 0, 6);
            DebugLogVerbose = raw.DebugLogVerbose;
            OutputCameraName = raw.OutputCameraName ?? string.Empty;
            CastMaxDistanceM = Mathf.Max(100f, raw.CastMaxDistanceM);
            CastRadiusNearM = Mathf.Max(0.5f, raw.CastRadiusNearM);
            CastRadiusFarM = Mathf.Max(CastRadiusNearM, raw.CastRadiusFarM);
            MinSpeedMps = Mathf.Max(1f, raw.MinSpeedMps);
            SafeAglMeters = Mathf.Max(0f, raw.SafeAglMeters);
            DepthClipMarginM = Mathf.Max(0f, raw.DepthClipMarginM);
            NearClipM = Mathf.Max(10f, raw.NearClipM);
            ImpactBandHalfM = Mathf.Max(20f, raw.ImpactBandHalfM);
            ConeHalfAngleDeg = Mathf.Clamp(raw.ConeHalfAngleDeg, 0.5f, 25f);
            LidarColor = raw.LidarColor;
            TerrainLayerMask = raw.TerrainLayerMask != 0 ? raw.TerrainLayerMask : 2112;
            EdgeThreshold = Mathf.Max(0.01f, raw.EdgeThreshold);
            EdgeStrength = Mathf.Max(0f, raw.EdgeStrength);
            EdgeThinPow = Mathf.Clamp(raw.EdgeThinPow, 1f, 8f);
            EdgeTexelScale = Mathf.Clamp(raw.EdgeTexelScale, 0.25f, 1.5f);
            NoiseStrength = Mathf.Clamp01(raw.NoiseStrength);
            DistanceFadeMeters = Mathf.Max(50f, raw.DistanceFadeMeters);
            ConeFalloffCos = Mathf.Clamp(raw.ConeFalloffCos, 0.005f, 0.2f);
            HudBrightness = Mathf.Clamp(raw.HudBrightness, 0.1f, 1f);
            AppearBootSec = Mathf.Clamp(raw.AppearBootSec, 0.1f, 2f);
            AppearBootFreqStart = Mathf.Clamp(raw.AppearBootFreqStart, 1f, 40f);
            AppearBootFreqEnd = Mathf.Clamp(raw.AppearBootFreqEnd, AppearBootFreqStart + 1f, 80f);
            AppearBootDim = Mathf.Clamp01(raw.AppearBootDim);
            BlockWhenGearDeployed = raw.BlockWhenGearDeployed;
            BlockDuringDaytime = raw.BlockDuringDaytime;
            DaytimeStartHour = Mathf.Clamp(raw.DaytimeStartHour, 0f, 24f);
            DaytimeEndHour = Mathf.Clamp(raw.DaytimeEndHour, DaytimeStartHour, 24f);
            ForceHotkeyEnabled = raw.ForceHotkeyEnabled;
            ForceHotkeyBinding = string.IsNullOrEmpty(raw.ForceHotkeyBinding) ? "Y" : raw.ForceHotkeyBinding;
            LidarForceHotkey.ApplyBinding(ForceHotkeyBinding);
        }
    }
}
