namespace NOLoader.LidarWireframeContour
{
    internal static class LidarIniDefaults
    {
        internal const string IniSection = "[Lidar]";

        internal const string DefaultIniBody = @"Enabled=true
ProbeIntervalSec=0.2
ProbeIntervalNearSec=0.05
TtiActivateSec=7.0
FadeOutSec=0.3
FadeInSec=0.3
FadeInUrgentSec=0.12
UniformSmoothSec=0.32
HoldAfterEscapeSec=1.0
ForceKeepDepthTextureActive=false
DebugForceBlend=0
DebugShaderMode=0
DebugLogVerbose=false
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
AppearBootSec=0.5
AppearBootFreqStart=6
AppearBootFreqEnd=40
AppearBootDim=0
BlockWhenGearDeployed=true
BlockDuringDaytime=true
DaytimeStartHour=6
DaytimeEndHour=18
ForceHotkeyEnabled=true
ForceHotkeyBinding=Y
";
    }
}
