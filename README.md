# Lidar Wireframe Contour

[![Version](https://img.shields.io/badge/version-0.2.1V-blue.svg)](CHANGELOG.md)
[![Game](https://img.shields.io/badge/game-Nuclear%20Option-darkgreen.svg)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![Loader](https://img.shields.io/badge/loader-NOLoader-orange.svg)](https://github.com/Mursisru/NOLoader)
[![.NET](https://img.shields.io/badge/.NET-4.8-purple.svg)](https://dotnet.microsoft.com/)

Standalone **NOLoader** mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/): active lidar terrain wireframe with a forward-looking collision cone (ACT Phase 2 visual system).

## Features

- **5 Hz CPU probe** — dual-stage `Physics.SphereCast` along **velocity** (wind drift aware), not nose direction
- **TTI activation** — wireframe appears when time-to-impact &lt; 7 s
- **GPU post-process** — Laplacian edge, velocity cone, tactical green HUD, CRT scanlines, 0.5s boot strobe on appear
- **Zero `Update()`** on probe path — `INOModTickNormal` + 200 ms accumulator
- **CPU fade** — `_EffectBlend` driven on CPU (not shader time)
- **URP mitigations** — manual `_InvViewProjMatrix`, optional pinned depth texture

## Requirements

- Nuclear Option (Steam)
- [NOLoader](https://github.com/Mursisru/NOLoader) installed and deployed
- .NET Framework 4.8 SDK (build only)

## Install (players)

1. Copy folder `LidarWireframeContour` into  
   `Nuclear Option\NOLoader\mods\`
2. Run NOLoader PatchTool once (or use full NOLoader deploy) so `FlightHud` patches apply.
3. Ensure `NOLidarWireframeContour_Data\` contains `lidar_shaders` bundle (see Build shaders below).

## Build

```powershell
dotnet build NOLoader.LidarWireframeContour\NOLoader.LidarWireframeContour.csproj -c Release
.\scripts\deploy-mod.ps1
```

Close the game before deploy.

### Shader asset bundle

```powershell
.\scripts\build-shader-bundle.ps1
```

Requires Unity 2022.3 LTS. Output: `NOLidarWireframeContour_Data\lidar_shaders`.

### Versioning

| Context | Format | Example |
|---------|--------|---------|
| `mod.json`, assembly | numeric semver only | `0.2.0` |
| Logs, `DisplayVersion`, CHANGELOG | semver + type suffix | `0.2.0V` |

Suffix letters: **V** visual, **M** mechanic, **P** program, **A** audio, **Q** QoL, **O** other. `A+V→Q`; `Q+M` forbidden.

## Configuration

Edit `mod_config.ini` in the mod folder:

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master switch |
| `ProbeIntervalSec` | `0.2` | SphereCast interval (cruise) |
| `ProbeIntervalNearSec` | `0.033` | Faster probe at 30 Hz when TTI ≤ 10s or combat |
| `TtiActivateSec` | `7.0` | Activate below this TTI |
| `FadeOutSec` | `0.3` | CPU fade-out duration |
| `FadeInSec` | `0.3` | CPU fade-in duration |
| `FadeInUrgentSec` | `0.12` | Faster fade-in when TTI already low |
| `UniformSmoothSec` | `0.2` | Smooth cone/distance probe jumps |
| `ForceKeepDepthTextureActive` | `false` | Pin depth RT (avoids URP toggle stutter) |
| `CastMaxDistanceM` | `1500` | Max cast range |
| `CastRadiusNearM` / `CastRadiusFarM` | `2` / `50` | Dual cast radii |
| `MinSpeedMps` | `30` | Min speed for lidar |
| `SafeAglMeters` | `500` | Sleep above this AGL |
| `LidarColorHex` | `#00CC66` | Wireframe tint |
| `EdgeThreshold` | `0.20` | Laplacian edge threshold (meters, depth-scaled) |
| `EdgeStrength` | `1.6` | Edge intensity multiplier |
| `EdgeThinPow` | `4.2` | Line thinning exponent |
| `EdgeTexelScale` | `0.50` | Depth sample stride (lower = thinner lines) |
| `NoiseStrength` | `0.15` | CRT flicker strength (mode 0) |
| `DistanceFadeMeters` | `175` | Soft fade before max lidar range |
| `ConeFalloffCos` | `0.05` | Smooth cone edge width (cosine space) |
| `HudBrightness` | `0.62` | Tactical HUD dim multiplier (mode 0) |
| `AppearBootSec` | `0.5` | Boot strobe duration on HUD appear |
| `AppearBootFreqStart` | `4` | Initial blink frequency (Hz, normalized) |
| `AppearBootFreqEnd` | `32` | Final blink frequency at end of boot |
| `AppearBootDim` | `0.06` | Minimum brightness during boot off-phase |
| `DebugForceBlend` | `0` | Force `blend=1` (isolation test) |
| `DebugShaderMode` | `0` | See debug ladder below |
| `OutputCameraName` | *(empty)* | Composite camera override (default: Main Camera) |

### Debug bisect ladder

Hot-reload `mod_config.ini` (~1 s). Close/reopen not required.

| Step | Settings | Expected |
|------|----------|----------|
| 1 | default | Log `mod_loaded`: `gpu:true`, `bundleBytes>15000`, `shaderHash` present |
| 2 | `DebugForceBlend=1`, `DebugShaderMode=5` | **Full green screen** |
| 3 | `DebugShaderMode=6` | White terrain lines on black |
| 4 | `DebugShaderMode=4` | Grayscale depth |
| 5 | `DebugForceBlend=0`, `DebugShaderMode=3` | Green contour overlay in combat |
| 6 | `DebugShaderMode=0` | Combat lidar at TTI ≤ 7 s |

## Architecture

```
INOModTickNormal
  ├─ FadeTick(dt)          → _EffectBlend
  └─ ProbeTick (0.2 s)     → SphereCast + TTI
LidarPostProcess
  └─ beginCameraRendering  → depth capture + backbuffer composite
Shader bundle lidar_shaders
  └─ Laplacian contours + velocity cone + CRT HUD (mode 0)
```

## Related projects

- [NOAviationCareerTracker](https://github.com/at747/NOAviationCareerTracker) — ACT meta-mod (naming convention only; no hard dependency)
- [TerrainSilhouetteHud](https://github.com/at747/TerrainSilhouetteHud_Engine) — alternative terrain HUD (do not run both for same warning role)

## License

[MIT](LICENSE) — Copyright (c) 2026 [Mursisru](https://github.com/Mursisru)

## Author

**[Mursisru](https://github.com/Mursisru)** — Nuclear Option modding
