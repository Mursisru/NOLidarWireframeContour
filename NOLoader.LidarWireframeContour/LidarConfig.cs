using System;
using NOLoader.ModConfig;
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
        internal static float UniformSmoothSec = 0.2f;
        internal static bool ForceKeepDepthTextureActive;
        internal static float DebugForceBlend;
        internal static int DebugShaderMode;
        internal static bool DebugLogVerbose = true;
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
        internal static int TerrainLayerMask;
        internal static float EdgeThreshold = 0.20f;
        internal static float EdgeStrength = 1.6f;
        internal static float EdgeThinPow = 4.2f;
        internal static float EdgeTexelScale = 0.50f;
        internal static float NoiseStrength = 0.15f;
        internal static float DistanceFadeMeters = 175f;
        internal static float ConeFalloffCos = 0.05f;
        internal static float HudBrightness = 0.62f;

        internal static float ConeCosHalfAngle =>
            Mathf.Cos(ConeHalfAngleDeg * Mathf.Deg2Rad);

        internal static void Load(string modRoot) => ApplyFromIni(modRoot, ensureDefault: true);

        internal static void Reload(string modRoot) => ApplyFromIni(modRoot, ensureDefault: false);

        private static void ApplyFromIni(string modRoot, bool ensureDefault)
        {
            const string defaults = @"[Lidar]
Enabled=true
ProbeIntervalSec=0.2
ProbeIntervalNearSec=0.05
TtiActivateSec=7.0
FadeOutSec=0.3
FadeInSec=0.3
FadeInUrgentSec=0.12
UniformSmoothSec=0.2
ForceKeepDepthTextureActive=true
DebugForceBlend=0
DebugShaderMode=0
DebugLogVerbose=true
OutputCameraName=
CastMaxDistanceM=1500
CastRadiusNearM=2
CastRadiusFarM=50
MinSpeedMps=30
SafeAglMeters=500
DepthClipMarginM=5
NearClipM=80
ImpactBandHalfM=200
ConeHalfAngleDeg=15
LidarColorHex=#00CC66
TerrainLayerMask=2112
EdgeThreshold=0.20
EdgeStrength=1.6
EdgeThinPow=4.2
EdgeTexelScale=0.50
NoiseStrength=0.15
DistanceFadeMeters=175
ConeFalloffCos=0.05
HudBrightness=0.62
";

            if (ensureDefault)
                ModIniConfig.EnsureDefault(modRoot, defaults);

            var cfg = ModIniConfig.Load(modRoot);

            Enabled = cfg.GetBool("Lidar", "Enabled", true);
            ProbeIntervalSec = Mathf.Max(0.05f, cfg.GetFloat("Lidar", "ProbeIntervalSec", 0.2f));
            ProbeIntervalNearSec = Mathf.Clamp(cfg.GetFloat("Lidar", "ProbeIntervalNearSec", 0.05f), 0.02f, ProbeIntervalSec);
            TtiActivateSec = Mathf.Max(0.5f, cfg.GetFloat("Lidar", "TtiActivateSec", 7f));
            FadeOutSec = Mathf.Max(0.05f, cfg.GetFloat("Lidar", "FadeOutSec", 0.3f));
            FadeInSec = Mathf.Max(0.05f, cfg.GetFloat("Lidar", "FadeInSec", 0.3f));
            FadeInUrgentSec = Mathf.Clamp(cfg.GetFloat("Lidar", "FadeInUrgentSec", 0.12f), 0.03f, FadeInSec);
            UniformSmoothSec = Mathf.Clamp(cfg.GetFloat("Lidar", "UniformSmoothSec", 0.2f), 0.05f, 1f);
            ForceKeepDepthTextureActive = cfg.GetBool("Lidar", "ForceKeepDepthTextureActive", false);
            DebugForceBlend = Mathf.Clamp01(cfg.GetFloat("Lidar", "DebugForceBlend", 0f));
            DebugShaderMode = Mathf.Clamp(cfg.GetInt("Lidar", "DebugShaderMode", 0), 0, 6);
            DebugLogVerbose = cfg.GetBool("Lidar", "DebugLogVerbose", true);
            OutputCameraName = cfg.GetString("Lidar", "OutputCameraName", string.Empty) ?? string.Empty;
            CastMaxDistanceM = Mathf.Max(100f, cfg.GetFloat("Lidar", "CastMaxDistanceM", 1500f));
            CastRadiusNearM = Mathf.Max(0.5f, cfg.GetFloat("Lidar", "CastRadiusNearM", 2f));
            CastRadiusFarM = Mathf.Max(CastRadiusNearM, cfg.GetFloat("Lidar", "CastRadiusFarM", 50f));
            MinSpeedMps = Mathf.Max(1f, cfg.GetFloat("Lidar", "MinSpeedMps", 30f));
            SafeAglMeters = Mathf.Max(0f, cfg.GetFloat("Lidar", "SafeAglMeters", 500f));
            DepthClipMarginM = Mathf.Max(0f, cfg.GetFloat("Lidar", "DepthClipMarginM", 5f));
            NearClipM = Mathf.Max(10f, cfg.GetFloat("Lidar", "NearClipM", 80f));
            ImpactBandHalfM = Mathf.Max(20f, cfg.GetFloat("Lidar", "ImpactBandHalfM", 120f));
            ConeHalfAngleDeg = Mathf.Clamp(cfg.GetFloat("Lidar", "ConeHalfAngleDeg", 15f), 0.5f, 25f);
            EdgeThreshold = Mathf.Max(0.01f, cfg.GetFloat("Lidar", "EdgeThreshold", 0.20f));
            EdgeStrength = Mathf.Max(0f, cfg.GetFloat("Lidar", "EdgeStrength", 1.6f));
            EdgeThinPow = Mathf.Clamp(cfg.GetFloat("Lidar", "EdgeThinPow", 4.2f), 1f, 8f);
            EdgeTexelScale = Mathf.Clamp(cfg.GetFloat("Lidar", "EdgeTexelScale", 0.50f), 0.25f, 1.5f);
            NoiseStrength = Mathf.Clamp01(cfg.GetFloat("Lidar", "NoiseStrength", 0.15f));
            DistanceFadeMeters = Mathf.Max(50f, cfg.GetFloat("Lidar", "DistanceFadeMeters", 175f));
            ConeFalloffCos = Mathf.Clamp(cfg.GetFloat("Lidar", "ConeFalloffCos", 0.05f), 0.005f, 0.2f);
            HudBrightness = Mathf.Clamp(cfg.GetFloat("Lidar", "HudBrightness", 0.62f), 0.1f, 1f);

            string colorHex = cfg.GetString("Lidar", "LidarColorHex", "#00CC66");
            if (!ColorUtility.TryParseHtmlString(colorHex, out Color parsed))
                parsed = new Color(0f, 1f, 0.4f, 1f);
            LidarColor = parsed;

            int mask = cfg.GetInt("Lidar", "TerrainLayerMask", 0);
            TerrainLayerMask = mask != 0 ? mask : BuildDefaultLayerMask();
        }

        private static int BuildDefaultLayerMask() => 2112;
    }
}
