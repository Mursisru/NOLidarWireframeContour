# Configuration

All gameplay settings map to `LidarSettingsSnapshot` → `LidarConfig.ApplySnapshot`.  
**43 keys** — same semantics in NOLoader `mod_config.ini` and BepInEx Configuration Manager.

---

## NOLoader (`mod_config.ini`)

Single section `[Lidar]`. File is re-read about every **1 second** while the mod is loaded.

Path: `NOLoader\mods\LidarWireframeContour\mod_config.ini`

---

## BepInEx (Configuration Manager)

Press **F1** in-game. Config file:

`BepInEx\config\com.mursisru.lidarwireframecontour.bepinex.cfg`

| CM section | Keys |
|------------|------|
| **General** | `Enabled` |
| **Probe** | `ProbeIntervalSec`, `ProbeIntervalNearSec`, `TtiActivateSec`, `MinSpeedMps`, `SafeAglMeters`, `CastMaxDistanceM`, `CastRadiusNearM`, `CastRadiusFarM` |
| **Activation** | `BlockWhenGearDeployed`, `BlockDuringDaytime`, `DaytimeStartHour`, `DaytimeEndHour`, `ForceHotkeyEnabled`, `ForceHotkeyBinding` |
| **Fade** | `FadeOutSec`, `FadeInSec`, `FadeInUrgentSec`, `UniformSmoothSec`, `HoldAfterEscapeSec` |
| **Visual** | `LidarColorHex`, `HudBrightness`, `ConeHalfAngleDeg`, `ConeFalloffCos`, `EdgeThreshold`, `EdgeStrength`, `EdgeThinPow`, `EdgeTexelScale`, `NoiseStrength`, `DistanceFadeMeters`, `DepthClipMarginM`, `NearClipM`, `ImpactBandHalfM`, `TerrainLayerMask`, `AppearBootSec`, `AppearBootFreqStart`, `AppearBootFreqEnd`, `AppearBootDim` |
| **Debug** | `ForceKeepDepthTextureActive`, `DebugForceBlend`, `DebugShaderMode`, `DebugLogVerbose`, `OutputCameraName` |

Advanced Debug keys are hidden until CM “Show advanced” is enabled.

---

## Full key reference

### General

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master switch |

### Probe

| Key | Default | Range (BepInEx) | Description |
|-----|---------|-----------------|-------------|
| `ProbeIntervalSec` | `0.2` | 0.05–2 | Cruise probe interval (5 Hz) |
| `ProbeIntervalNearSec` | `0.05` | 0.02–1 | Near-TTI probe interval (20 Hz) |
| `TtiActivateSec` | `7.0` | 0.5–30 | Activate below this time-to-impact (s) |
| `MinSpeedMps` | `30` | 1–200 | Minimum aircraft speed for probing |
| `SafeAglMeters` | `500` | 0–5000 | Disable above this AGL |
| `CastMaxDistanceM` | `1500` | 100–5000 | Max SphereCast range |
| `CastRadiusNearM` | `2` | 0.5–20 | Near cast radius |
| `CastRadiusFarM` | `50` | 1–100 | Far cast radius |

### Activation

| Key | Default | Description |
|-----|---------|-------------|
| `BlockWhenGearDeployed` | `true` | Block auto when landing gear down |
| `BlockDuringDaytime` | `true` | Block auto during day hours |
| `DaytimeStartHour` | `6` | Day start (0–24, in-game) |
| `DaytimeEndHour` | `18` | Day end (0–24) |
| `ForceHotkeyEnabled` | `true` | Enable force-night toggle |
| `ForceHotkeyBinding` | `Y` | Physical key name (`KeyCode` name, layout-independent) |

### Fade

| Key | Default | Description |
|-----|---------|-------------|
| `FadeOutSec` | `0.3` | Shader fade-out after hold |
| `FadeInSec` | `0.3` | Shader fade-in duration |
| `FadeInUrgentSec` | `0.12` | Faster fade when TTI already low |
| `UniformSmoothSec` | `0.32` | Cone / distance smoothing time constant |
| `HoldAfterEscapeSec` | `1.0` | Keep effect after leaving threat |

### Visual

| Key | Default | Description |
|-----|---------|-------------|
| `LidarColorHex` | `#00CC66` | Wireframe tint (`#RRGGBB`) |
| `HudBrightness` | `0.62` | HUD intensity multiplier |
| `ConeHalfAngleDeg` | `15` | Velocity cone half-angle (degrees) |
| `ConeFalloffCos` | `0.05` | Cone edge softness (cosine space) |
| `EdgeThreshold` | `0.20` | Laplacian edge threshold |
| `EdgeStrength` | `1.6` | Edge intensity |
| `EdgeThinPow` | `4.2` | Line thinning exponent |
| `EdgeTexelScale` | `0.50` | Depth sample stride |
| `NoiseStrength` | `0.15` | Legacy CRT noise (minimal in current shader) |
| `DistanceFadeMeters` | `175` | Soft fade before max range |
| `DepthClipMarginM` | `5` | Depth clip margin |
| `NearClipM` | `80` | Reject geometry closer than this |
| `ImpactBandHalfM` | `200` | Impact band half-width (debug / masks) |
| `TerrainLayerMask` | `2112` | Physics layers for terrain casts |
| `AppearBootSec` | `0.5` | Boot strobe duration on appear |
| `AppearBootFreqStart` | `6` | Boot strobe start frequency |
| `AppearBootFreqEnd` | `40` | Boot strobe end frequency |
| `AppearBootDim` | `0` | Boot dim level |

### Debug

| Key | Default | Description |
|-----|---------|-------------|
| `ForceKeepDepthTextureActive` | `false` | Pin URP depth texture (debug; may cost perf) |
| `DebugForceBlend` | `0` | Force combat GPU path (0–1) |
| `DebugShaderMode` | `0` | Shader debug ladder 0–6 |
| `DebugLogVerbose` | `false` | Verbose debug log file |
| `OutputCameraName` | *(empty)* | Override composite camera (default: main) |

---

## Debug bisect ladder

| Step | Settings | Expected |
|------|----------|----------|
| 1 | defaults | Log: `0.3.6V loaded`, `gpu=True`, `bundleBytes>15000` |
| 2 | `DebugForceBlend=1`, `DebugShaderMode=5` | Full green screen |
| 3 | `DebugShaderMode=6` | White terrain lines (edge RT) |
| 4 | `DebugShaderMode=4` | Grayscale depth |
| 5 | `DebugForceBlend=0`, `DebugShaderMode=3` | Green contour overlay |
| 6 | `DebugShaderMode=0` | Combat lidar at TTI ≤ 7 s |

---

## Ini ↔ CM naming

Keys are **identical** between `mod_config.ini` and BepInEx cfg (PascalCase).  
BepInEx binds via `LidarWireframeBepInConfig.Bind` → `SyncToRuntime` on every change.
