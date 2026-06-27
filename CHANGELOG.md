# Changelog

All notable changes to this project are documented in this file.

## [0.2.1V] — 2026-06-26

### Fixed

- TTI boot strobe: triggers on `_wantsActive` rising edge only; strobe drives contour visibility (not drowned by fade-in)
- Boot uses elapsed seconds for accelerating blink; brightness ramps to `HudBrightness`
- Near-combat probe rate raised to 30 Hz (`ProbeIntervalNearSec` ≈ 0.033)

## [0.2.0V] — 2026-06-26

### Changed

- Versioning migrated to reglement format `MAJOR.MINOR.PATCH` + type suffix (`0.2.0V`)
- Numeric-only `0.2.0` in `mod.json` / assembly; full `0.2.0V` in logs and `DisplayVersion`
- Replaces legacy `Build DEV1Px` strings

## [0.1.0] — 2026-06-26 (legacy DEV builds)

### Added (DEV1P40V)

- HUD boot sequence: 0.5s accelerating strobe + brightness ramp to `HudBrightness` on each appear
- Config: `AppearBootSec`, `AppearBootFreqStart`, `AppearBootFreqEnd`, `AppearBootDim`

### Changed (DEV1P40V)

- Authorship and assembly metadata aligned with [Mursisru](https://github.com/Mursisru) GitHub org
- `mod.json` id → `com.mursisru.lidarwireframecontour`

### Added (DEV1P39VM)

- Per-frame TTI extrapolation between probes — activates without waiting for next 5 Hz tick
- Adaptive probe rate: `ProbeIntervalNearSec` (0.05s) when TTI within 10s of threshold or combat active
- Smoothed probe uniforms (`UniformSmoothSec`) — cone/distance no longer snap every probe
- Urgent fade-in (`FadeInUrgentSec` 0.12s) when TTI already below ~4.5s at activation

### Added (DEV1P38V)

- Razor-thin Laplacian contours: hard NMS, flat-kill for water/runways, tighter upper-lap cap
- Tactical HUD dimming: `HudBrightness` (default 0.62), softer CRT scanlines (0.09 amplitude)
- Symmetric lifecycle fade: `FadeInSec` (default 0.3s) separate from `FadeOutSec`
- Velocity-only combat cone (no cam+vel blend on dive)

### Fixed (DEV1P38V)

- **Horizontal screen bands:** removed `ImpactDepthMask` from mode 0; static `EffectiveNearClipM` (no probe jitter)
- Thinner edge defaults: `EdgeTexelScale=0.50`, `EdgeThreshold=0.20`, `EdgeStrength=1.6`, `EdgeThinPow=4.2`
- Default lidar tint `#00CC66`, `ConeFalloffCos=0.05`

### Added (DEV1P37VA)

- CRT HUD polish: animated scanlines + time-based micro-noise (`ApplyHudIntensity`, mode 0 only)
- Smooth cone vignette via `smoothstep` + `ConeFalloffCos` (replaces hard linear cutoff)
- Radial distance fade: last `DistanceFadeMeters` before `_MaxLidarDistance`
- Thinner Laplacian lines: `EdgeTexelScale` uniform; defaults `EdgeThreshold=0.18`, `EdgeStrength=1.8`, `EdgeThinPow=3.4`

### Fixed (DEV1P36VM)

- Mode 0 anti-fill: Laplacian + upper-cap + slope wash + soft NMS (replaces Sobel far-boost)
- Combat masks restored: `ImpactDepthMask` band + `CombatConeMask` (floor 0.55) + noise
- Mode 3 debug stays full-terrain edge without cone/band
- Defaults: `EdgeThreshold=0.24`, `EdgeStrength=2.8`, `EdgeThinPow=3.0`

### Fixed (DEV1P35VP)

- `shader_source_hash.txt` written at bundle build; runtime reads hash without loose `.shader` in game folder
- Deploy removes stale `NOLidarWireframeContour_Data/Shaders` from game mod folder

### Fixed (DEV1P34VP)

- Deploy gate: verify DLL + `lidar_shaders` bundle; re-copy mod payload after PatchTool; stop deploying loose `.shader` sources
- Shader sync: `build-shader-bundle.ps1` copies `NOLidarWireframeContour_Data/Shaders` → UnityBundleBuilder before build
- Composite: FullScreenPass backbuffer path (`GetCameraColorBackBuffer` + `DrawFullScreen`) instead of swap-blip ping-pong
- Debug mode 5/6 run before blend early-out; mode 0 uses contour path without cone/noise
- Combat gate: keep GPU during impact hold (`SetCombatActive`); TTI activates at exactly 7.0s
- `mod_loaded` logs `shaderHash` for bundle/source verification

### Fixed (DEV1P33VP)

- Full edge rewrite: depth Sobel + lap gate (replaces Laplacian bandpass that zeroed output)
- Dynamic near-clip + impact depth band in shader
- Debug mode 6 shows raw edge (no terrain mask)
- Combat cone floor 0.55

### Fixed (DEV1P32V)

- Roll back P31 over-filtering (NMS/facet/diag killed all edges → black screen)
- Laplacian + upper-lap cap + slope-fill only (P30 visibility, less wash than P30)

### Fixed (DEV1P31V)

- Narrow Laplacian bandpass (kills wide soft fill patches)
- Facet-break + Laplacian NMS (local max only) + diagonal spike test
- Final `smoothstep(0.4, 0.58)` hardens lines vs gradient wash

### Fixed (DEV1P30V)

- **Slope fill at distance:** Sobel replaced with **Laplacian** (zero on ramps, peaks on mesh breaks) + slope-fill rejection
- Far-range 2x Laplacian pass for LOD triangle edges at 300–1500m
- Defaults: `EdgeThreshold=0.22`, `EdgeStrength=2.8`, `EdgeThinPow=2.6`

### Fixed (DEV1P29V)

- **Contour-only (no ground fill):** Sobel in meters + bandpass + local-peak thinning; removed wide-scale gradient that painted entire terrain slabs
- New `EdgeThinPow` (default 2.5); `EdgeThreshold` now meters (default 0.5, scales with depth)

### Fixed (DEV1P28VP)

- **Root cause:** edge pass + composite used **ARGB8 packed depth** — 8-bit quantization kills Sobel at 300–1500m (logs showed healthy pipeline but zero visible edges)
- Edge pass and `_DepthTex` now use **R32 float** capture
- Terrain mask simplified: `NearClipM .. MaxLidarDistance` only
- `build-shader-bundle.ps1` fails on shader compile errors

### Fixed (DEV1P27VP)

- **Critical:** composite shader failed to compile (nested function in CG) — bundle shipped broken shader → zero output including mode 5; extracted `EdgeMetricAtScale` to top level

### Fixed (DEV1P26VM)

- **Far-range contours:** multi-scale Sobel (1x / wide / 2x wide kernel scaled by depth) + absolute meter gradient — terrain edges visible at 700m+ TTI, not only near impact
- **Depth window:** replaced narrow band around `impactDist` with envelope `nearClip .. impactDist+50%` (e.g. 80–1200m at 800m impact)
- Defaults: `ImpactBandHalfM=200`, `EdgeThreshold=0.0003`, `EdgeStrength=4.0`

### Fixed (DEV1P25VM)

- **Terrain-only contours:** `NearClipM` rejects own-aircraft depth (<80m); `ImpactBandHalfM` band around probe `impactDist` — no more fuselage wireframe
- **Far terrain edges:** Sobel on relative linear-depth gradient (works at 300–1500m, not only near ground)
- Modes 3/6 apply same terrain mask; mode 0 uses cone + noise on masked edges
- Fixed `centerConeDot` log (full `invProj * float4` unprojection)

### Fixed (DEV1P24VP)

- **Vulkan MSAA4x depth copy:** `ConfigureMsaaKeywords` used destination `desc.msaaSamples=1` instead of camera source MSAA — CopyDepth shader read non-MSAA path on MSAA4x attachment → flat depth → black edge RT (mode 6 black world, mode 3/0 invisible)
- `depth_captured` log now includes `sourceMsaa`

### Fixed (DEV1P23VP)

- **Root cause (mode 0/3 invisible):** deployed `lidar_shaders` bundle was **stale** (12132 B) — missing `LidarDepthEdge.shader`; logs showed `edge:false` but misleading `hasEdge:true` on empty RT
- `deploy-mod.ps1` now runs `build-shader-bundle.ps1` before copy; prints bundle size in deploy output
- `TryGetCapturedEdge` requires edge pass actually ran (`_lastEdgePassOk`)
- `mod_loaded` logs: `bundleBytes`, `packShader`, `edgeShader`, `version`
- Shader fallback edge uses raw depth Sobel (not LinearEyeDepth); `DebugShaderMode=6` shows edge mask

### Fixed (DEV1P22VM)

- **Invisible lidar (combat):** hard `ImpactClip` at ~55m rejected almost all screen pixels when diving from altitude — replaced with soft `ImpactDepthFade` up to `CastMaxDistanceM`
- **Edge=0 on Vulkan:** precomputed `_LidarDepthEdge` RT in capture pass (`LidarDepthEdge.shader`); composite reads `_EdgeTex`
- Removed sky hard-discard (was zeroing output when packed depth read as 0)
- Logs: `hasEdge`, `impactDist`, `centerConeDot` in combat summary

### Fixed (DEV1P21VP)

- **Root cause (black exterior on blend=1):** `DrawFullScreen` did not set `_BlitScaleBias` — vertex UV collapsed; scene sample black → empty outside
- **Composite:** URP canonical `ScriptableRenderPass.Blit` + `SwapColorBuffer` (MSAA resolve via Blitter, correct scale/bias)
- **Shader vert:** removed `_BlitScaleBias` dependency (matches depth-pack procedural fullscreen)
- **Shader combat:** `UNITY_REVERSED_Z` sky clip; opaque alpha `1.0` on output

### Fixed (DEV1P20VP)

- **Shader audit:** debug modes 1–5 checked before `_DebugBypass` — mode 5 solid green no longer blocked when `DebugForceBlend=1`
- **Composite camera:** output pass back on **Main Camera** (world color buffer); `cockpitCamRender` overlay stays on top — fixes empty exterior + cockpit-visible symptom from `postProcessingRenderer` blit
- **URP command buffer:** composite uses `renderingData.commandBuffer` + `cmd.Clear()` (FullScreenPassRendererFeature pattern)
- **Execute dedupe:** one composite draw per camera per frame
- **Logging:** `config_reloaded` (modRoot, iniPath, debugForce, shaderMode); `composite_detail` (source/backBuffer RT names, sameRef, dims)

### Fixed (DEV1P19VP)

- **Vulkan multi-camera stack:** composite moved to `postProcessingRenderer` (final cockpit output); depth capture stays on Main Camera
- **Enqueue dedupe:** HashSet per frame — was 236 enqueues/frame (broken `renderedFrameCount` dedupe)
- **Composite path:** URP FullScreenPass canonical — `Blitter.BlitCameraTexture` + `GetCameraColorBackBuffer` + `DrawFullScreen` (not swap-buffer Blit)
- **Depth pack:** R32 CopyDepth → ARGB32 `_LidarDepthPacked` for Vulkan CG sampling
- **Config hot-reload:** `mod_config.ini` re-read every 1s (no restart needed for debug modes)
- **DebugShaderMode=5:** solid green visibility test; modes 1–4 use alpha=1

### Fixed (DEV1P18VP)

- **Vulkan root cause:** `_CameraDepthTexture` R32 global samples as flat zero in CG bundle shader — Sobel edge always 0 (C/cone worked, B/D/E silent)
- Wire `LidarDepthCapturePass` (URP `CopyDepth` blit, `AfterRenderingTransparents`) → composite reads owned `_LidarDepthCapture` RT, not global
- `DebugShaderMode=4` raw depth grayscale for depth pipeline validation
- Log `depthSource` captured vs global; `depth_captured` event with graphics API

### Fixed (DEV1P17VM)

- **Root cause (combat invisible):** `_MaxLidarDistance` was fed probe hit distance (~55 m) — shader clipped almost all scene pixels; split into `_MaxLidarDistance` (CastMaxDistanceM) and `_ImpactDistance` (probe hit)
- `DebugShaderMode` 0–3: CombatNoCone, VisualizeCone, VisualizeEdge for binary isolation without fullscreen debug
- `combat_frame_summary` NDJSON (0.5s rate-limit): blend, tti, hitDist, coneDir, centerConeDot, depthTex, cam
- `DebugForceBlend` now uses same camera gates as combat (Main Camera + cockpit only)
- Probe hold 1.5s when TTI < 1s; `can_run_false` / `low_speed` no longer kill effect during hold
- Cone axis: camera-forward when diving (lookDown > 0.26); default cone widened to 15°
- Extended logging: draw_ok, enqueue_rejected, probe_state (holdTimer, missStreak, blend); log cleared on mod load

### In-game test matrix (manual)

| # | Config | Expected |
|---|--------|----------|
| A | `DebugShaderMode=0`, `DebugForceBlend=0` | combat baseline |
| B | `DebugShaderMode=1` | green depth edges when probe active |
| C | `DebugShaderMode=2` | red/green cone visualization |
| D | `DebugShaderMode=3` | edge-only overlay |
| E | `DebugForceBlend=1` | fullscreen control |

### Fixed (DEV1P10VP)

- Composite writes to `GetCameraColorBackBuffer` (was `cameraColorTargetHandle` — wrong ping-pong buffer, invisible on screen)
- `cmd.DrawMesh(RenderingUtils.fullscreenMesh)` instead of `DrawProcedural` / mismatched vertex paths
- Depth copy via URP `Hidden/Universal Render Pipeline/CopyDepth` material (plain blit produced zero edges)
- Base green tint when active (`blend * tti * 0.18`) so effect is visible even before Sobel picks edges
- `ForceKeepDepthTextureActive=true` default; cone widened to 6°; edge threshold 0.005
- `DebugForceBlend` skips cockpit gate for isolation testing

### Fixed (DEV1P9VP)

- **Root cause:** URP `CoreUtils.DrawFullScreen` / `Blit` use procedural fullscreen triangle (`SV_VertexID`); shader used mesh `appdata` vertex — GPU drew invalid geometry (invisible output despite `draw_ok` logs)
- Shader vertex rewritten to `SV_VertexID` fullscreen triangle (CG, bundle-compatible)
- Split depth capture (`LidarDepthCapturePass` at `BeforeRenderingPostProcessing`) from composite (`AfterRenderingPostProcessing`)
- Dedupe `EnqueuePass` once per camera per frame
- `_MaxLidarDistance` now uses `CastMaxDistanceM` (1500 m) instead of probe hit distance

### Fixed

- GPU pass aligned with URP `FullScreenPassRendererFeature` (`CoreUtils.DrawFullScreen`, explicit `_DepthTex` copy via game `CopyDepth` shader)
- Shader bundle kept in memory; optional `LidarWireframeContour.mat` load path; Windows Gfx API list pinned at bundle build
- DEV1P6V regression: `isSupported` gate blocked all rendering (`gpu=false` in logs)

### Fixed (DEV1P6V)

- Shader asset bundle: URP HLSL `#include Packages/...` reported `isSupported=false` at runtime — rewritten to bundle-safe `CGPROGRAM`

- **GPU rendering:** URP `ScriptableRenderPass` (`LidarWireframeRenderPass`) enqueued from `beginCameraRendering`; composites Sobel depth wireframe into cockpit camera framebuffer
- Shader rewritten to URP HLSL (`RenderPipeline=UniversalPipeline`, `SampleSceneDepth`, `_BlitTexture` composite)
- `_InvViewProjMatrix` synced in render pass `Execute` from `renderingData.cameraData.camera`
- `LidarWireframeRenderPass.Cleanup()` releases `_LidarFullscreenPassColorCopy` RTHandle on shutdown, blend idle, and controller destroy
- Optional `DebugForceBlend` config key for shader isolation testing

### Added

- Initial release: **Lidar Wireframe Contour** (ACT Phase 2 standalone NOLoader mod)
- `ACT_LidarCollisionController` — 5 Hz dual SphereCast probe, TTI gate, CPU `_EffectBlend` fade
- Cast #2 origin offset (+50 m) to avoid initial overlap with aircraft collider
- `LidarPostProcess` — URP `endCameraRendering` blit with manual `_InvViewProjMatrix`
- `ForceKeepDepthTextureActive` config for pinned depth + shader early-out
- HLSL shader `Hidden/ACT/LidarWireframeContour` — Sobel depth edges, hard clip, cone mask, CRT noise
- `mod_config.ini` tuning surface
- Unity bundle builder for `lidar_shaders`

### Notes

- Works on **all aircraft** (no rank gate)
- No hard dependency on Aviation Career Tracker
