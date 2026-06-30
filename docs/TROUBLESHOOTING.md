# Troubleshooting

Symptoms, causes, and fixes for Lidar Wireframe Contour.

---

## No wireframe in combat

### Check activation gates

Auto lidar requires **all** of:

- Night **or** force-night mode (**Y** toggled on)
- Landing gear **up** (unless force-night)
- Speed **> `MinSpeedMps`** (default 30 m/s)
- AGL **< `SafeAglMeters`** (default 500 m)
- TTI **≤ `TtiActivateSec`** (default 7 s) toward terrain

Dive toward ground in a night mission to test.

### Check loader choice

| Mistake | Fix |
|---------|-----|
| NOLoader **and** BepInEx DLLs installed | Remove one loader’s files completely |
| Only plugin DLL, no **Core** | Copy `NOLidarWireframeContour.Core.dll` |
| Missing `lidar_shaders` bundle | Reinstall from release zip or run `build-shader-bundle.ps1` |

### Check mission stage

Mod loads on **Mission** only. Main menu / editor will not run the probe chain.

### BepInEx: plugin loads but lidar dead (0.3.4 and earlier)

Symptom: log shows `mod_loaded` / `urp_hooked` but **no** probe or `gpu=True` combat logs.

**Cause:** tick lived on `BaseUnityPlugin.gameObject` which never received `Update`.

**Fix:** use **0.3.5V+** with `LidarWireframeHost` on `DontDestroyOnLoad` object.

---

## `gpu=False` in load log

| Cause | Fix |
|-------|-----|
| URP not ready / wrong graphics API | Enter mission; check BepInEx log for exceptions |
| Shader bundle missing or stale | Verify `lidar_shaders` ≥ 15 KB; rebuild bundle |
| `Enabled=false` | Set `Enabled=true` in config |

---

## `bundleBytes` too small (< 15000)

Stale or incomplete AssetBundle — often missing `LidarDepthEdge.shader` in bundle.

1. Run `.\scripts\build-shader-bundle.ps1` from repo root (Unity 2022.3 LTS).
2. Redeploy / recopy `NOLidarWireframeContour_Data\lidar_shaders`.

---

## Black screen / invisible overlay

| Step | Action |
|------|--------|
| 1 | `DebugShaderMode=5`, `DebugForceBlend=1` → expect solid green |
| 2 | If black: composite camera / URP path issue — check `OutputCameraName` |
| 3 | `DebugShaderMode=6` → if black, edge pass failed (depth / bundle) |
| 4 | `DebugShaderMode=4` → validate depth capture |

See debug ladder in [CONFIGURATION.md](CONFIGURATION.md).

---

## Vulkan-specific history

Older builds sampled global `_CameraDepthTexture` as flat zero in bundled CG shaders.  
Current builds use **owned R32 capture** in `LidarDepthCapturePass`. Ensure release **≥ 0.2.4V**.

---

## NOLoader PatchTool

If `FlightHud` patches did not apply:

1. Close the game.
2. Run NOLoader PatchTool.
3. Confirm `mod.json` patch hashes match game version.

BepInEx path uses Harmony at mission load — no PatchTool.

---

## Hotkey not working

- Default: **Y** (`KeyCode.Y`, physical key).
- `ForceHotkeyEnabled` must be `true`.
- Hotkey toggles **force-night** only; TTI gate still applies for visible effect.

---

## Performance

| Setting | Trade-off |
|---------|-----------|
| `ForceKeepDepthTextureActive=true` | Easier debugging; may add URP depth cost |
| `ProbeIntervalNearSec` | Lower = more CPU physics casts |
| Half-res edge | Not used — full-res edge @ 60 Hz by design (0.2.5V+) |

---

## Log locations

| Loader | Log |
|--------|-----|
| BepInEx | `BepInEx\LogOutput.log` |
| NOLoader | NOLoader ring / game log per NOLoader docs |
| Verbose mod | Enable `DebugLogVerbose=true` |

Successful load line example:

```
[LidarWireframe] 0.3.6V loaded (stage=Mission, gpu=True)
```

---

## Still stuck?

1. Confirm single-loader install.
2. Run debug bisect ladder (modes 5 → 6 → 4 → 0).
3. Open a [GitHub issue](https://github.com/Mursisru/NOLidarWireframeContour/issues) with loader type, mod version, and `LogOutput.log` excerpt.
