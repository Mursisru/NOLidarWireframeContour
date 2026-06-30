# Shader bundle (required at runtime)

Runtime loads **`lidar_shaders`** only (Unity AssetBundle). Loose files under `Shaders/` are **source** — not read by the game.

## Prerequisites

- Unity **2022.3 LTS** (project uses `UnityBundleBuilder/`)
- Windows (build script locates Unity Hub editor)

## Build

From repository root:

```powershell
.\scripts\build-shader-bundle.ps1
```

The script:

1. Syncs `NOLidarWireframeContour_Data/Shaders/*.shader` → `UnityBundleBuilder/Assets/LidarWireframe/`
2. Invokes Unity batch build (`BuildLidarBundle.cs`)
3. Writes `NOLidarWireframeContour_Data/lidar_shaders` (~20 KB)
4. Updates `shader_source_hash.txt` for `mod_loaded` diagnostics

## Shaders in bundle

| Asset | Role |
|-------|------|
| `LidarWireframeContour.shader` | Composite post-process (cone, fade, CRT) |
| `LidarDepthEdge.shader` | Laplacian edge pre-pass |
| `LidarWireframeContour.mat` | Material reference for bundle load |

> **Note:** `LidarDepthPack.shader` was removed in perf v2 (0.2.3V+) — depth uses R32 capture only.

## Verify

After deploy, game log should include:

```
[LidarWireframe] 0.3.6V loaded (stage=Mission, gpu=True)
```

With `DebugLogVerbose=true`, `mod_loaded` reports `bundleBytes`, `edgeShader`, `compositeSupported`, `shaderHash`.
