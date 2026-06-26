# Lidar Wireframe Contour

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](CHANGELOG.md)
[![Game](https://img.shields.io/badge/game-Nuclear%20Option-darkgreen.svg)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![Loader](https://img.shields.io/badge/loader-NOLoader-orange.svg)](https://github.com/Mursisru/NOLoader_Engine)
[![.NET](https://img.shields.io/badge/.NET-4.8-purple.svg)](https://dotnet.microsoft.com/)

Standalone **NOLoader** mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/): active lidar terrain wireframe with a forward-looking collision cone (ACT Phase 2 visual system).

## Features

- **5 Hz CPU probe** — dual-stage `Physics.SphereCast` along **velocity** (wind drift aware), not nose direction
- **TTI activation** — wireframe appears when time-to-impact &lt; 7 s
- **GPU post-process** — Laplacian edge detection on depth buffer, impact band, forward cone mask
- **Zero `Update()`** on probe path — `INOModTickNormal` + 200 ms accumulator
- **CPU fade** — `_EffectBlend` driven on CPU (not shader time)
- **URP mitigations** — manual `_InvViewProjMatrix`, optional pinned depth texture

## Requirements

- Nuclear Option (Steam)
- [NOLoader](https://github.com/at747/NOLoader_Engine) installed and deployed
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

## Configuration

Edit `mod_config.ini` in the mod folder:

| Key | Default | Description |
|-----|---------|-------------|
| `Enabled` | `true` | Master switch |
| `ProbeIntervalSec` | `0.2` | SphereCast interval |
| `TtiActivateSec` | `7.0` | Activate below this TTI |
| `FadeOutSec` | `0.3` | CPU fade duration |
| `ForceKeepDepthTextureActive` | `false` | Pin depth RT (avoids URP toggle stutter) |
| `CastMaxDistanceM` | `1500` | Max cast range |
| `CastRadiusNearM` / `CastRadiusFarM` | `2` / `50` | Dual cast radii |
| `MinSpeedMps` | `30` | Min speed for lidar |
| `SafeAglMeters` | `500` | Sleep above this AGL |
| `LidarColorHex` | `#00FF66` | Wireframe tint |
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
  └─ Laplacian contours + terrain / impact / cone masks (mode 0 combat)
```

## Related projects

- [NOAviationCareerTracker](https://github.com/at747/NOAviationCareerTracker) — ACT meta-mod (naming convention only; no hard dependency)
- [TerrainSilhouetteHud](https://github.com/at747/TerrainSilhouetteHud_Engine) — alternative terrain HUD (do not run both for same warning role)

## License

[MIT](LICENSE) — Copyright (c) 2026 at747

## Author

**at747** — Nuclear Option modding
