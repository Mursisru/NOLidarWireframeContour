using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal struct LidarSettingsSnapshot
    {
        internal bool Enabled;
        internal float ProbeIntervalSec;
        internal float ProbeIntervalNearSec;
        internal float TtiActivateSec;
        internal float FadeOutSec;
        internal float FadeInSec;
        internal float FadeInUrgentSec;
        internal float UniformSmoothSec;
        internal float HoldAfterEscapeSec;
        internal bool ForceKeepDepthTextureActive;
        internal float DebugForceBlend;
        internal int DebugShaderMode;
        internal bool DebugLogVerbose;
        internal string OutputCameraName;
        internal float CastMaxDistanceM;
        internal float CastRadiusNearM;
        internal float CastRadiusFarM;
        internal float MinSpeedMps;
        internal float SafeAglMeters;
        internal float DepthClipMarginM;
        internal float NearClipM;
        internal float ImpactBandHalfM;
        internal float ConeHalfAngleDeg;
        internal Color LidarColor;
        internal int TerrainLayerMask;
        internal float EdgeThreshold;
        internal float EdgeStrength;
        internal float EdgeThinPow;
        internal float EdgeTexelScale;
        internal float NoiseStrength;
        internal float DistanceFadeMeters;
        internal float ConeFalloffCos;
        internal float HudBrightness;
        internal float AppearBootSec;
        internal float AppearBootFreqStart;
        internal float AppearBootFreqEnd;
        internal float AppearBootDim;
        internal bool BlockWhenGearDeployed;
        internal bool BlockDuringDaytime;
        internal float DaytimeStartHour;
        internal float DaytimeEndHour;
        internal bool ForceHotkeyEnabled;
        internal string ForceHotkeyBinding;

        internal static LidarSettingsSnapshot CreateDefaults()
        {
            return new LidarSettingsSnapshot
            {
                Enabled = true,
                ProbeIntervalSec = 0.2f,
                ProbeIntervalNearSec = 0.05f,
                TtiActivateSec = 7f,
                FadeOutSec = 0.3f,
                FadeInSec = 0.3f,
                FadeInUrgentSec = 0.12f,
                UniformSmoothSec = 0.32f,
                HoldAfterEscapeSec = 1f,
                ForceKeepDepthTextureActive = false,
                DebugForceBlend = 0f,
                DebugShaderMode = 0,
                DebugLogVerbose = false,
                OutputCameraName = string.Empty,
                CastMaxDistanceM = 1500f,
                CastRadiusNearM = 2f,
                CastRadiusFarM = 50f,
                MinSpeedMps = 30f,
                SafeAglMeters = 500f,
                DepthClipMarginM = 5f,
                NearClipM = 80f,
                ImpactBandHalfM = 200f,
                ConeHalfAngleDeg = 15f,
                LidarColor = new Color(0f, 1f, 0.4f, 1f),
                TerrainLayerMask = 2112,
                EdgeThreshold = 0.20f,
                EdgeStrength = 1.6f,
                EdgeThinPow = 4.2f,
                EdgeTexelScale = 0.50f,
                NoiseStrength = 0.15f,
                DistanceFadeMeters = 175f,
                ConeFalloffCos = 0.05f,
                HudBrightness = 0.62f,
                AppearBootSec = 0.5f,
                AppearBootFreqStart = 6f,
                AppearBootFreqEnd = 40f,
                AppearBootDim = 0f,
                BlockWhenGearDeployed = true,
                BlockDuringDaytime = true,
                DaytimeStartHour = 6f,
                DaytimeEndHour = 18f,
                ForceHotkeyEnabled = true,
                ForceHotkeyBinding = "Y",
            };
        }
    }
}
