# Installation

GPU lidar wireframe mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/).  
Pick **one** loader — **NOLoader** or **BepInEx**. Never install both builds at once (double URP hook).

---

## Prerequisites

| Loader | Required |
|--------|----------|
| **Game** | [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) (Steam) |
| **NOLoader** | [NOLoader](https://github.com/Mursisru/NOLoader) installed + PatchTool applied once |
| **BepInEx** | [BepInEx 5](https://docs.bepinex.dev/) for Nuclear Option + run game once to generate folders |
| **BepInEx config** | [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) in `BepInEx\plugins\` |

---

## From GitHub release (recommended)

1. Open [Releases](https://github.com/Mursisru/NOLidarWireframeContour/releases).
2. Download the zip for your loader:
   - `NOLidarWireframeContour-NOLoader-vX.Y.Z.zip`
   - `NOLidarWireframeContour-BepInEx-vX.Y.Z.zip`
3. Extract into your Nuclear Option install folder (paths below).
4. Follow loader-specific steps in the next sections.

---

## NOLoader path

### Target layout

```
Nuclear Option\
  NOLoader\
    mods\
      LidarWireframeContour\
        NOLoader.LidarWireframeContour.dll
        NOLidarWireframeContour.Core.dll
        NOLoader.ModConfig.dll
        mod.json
        mod_config.ini
        NOLidarWireframeContour_Data\
          lidar_shaders          ← required (~20 KB AssetBundle)
          shader_source_hash.txt
```

### Steps

1. Copy the `LidarWireframeContour` folder to `Nuclear Option\NOLoader\mods\`.
2. Confirm `NOLidarWireframeContour_Data\lidar_shaders` exists and is **≥ 15 KB**.
3. Run **NOLoader PatchTool** once so `FlightHud` Harmony patches apply.
4. Launch the game and enter a **mission** (mod `loadStage` is `Mission`).
5. Edit `mod_config.ini` in the mod folder — changes hot-reload about every **1 s**.

### Config file

`mod_config.ini` — flat `[Lidar]` section. See [CONFIGURATION.md](CONFIGURATION.md) for all keys.

---

## BepInEx path

### Target layout

```
Nuclear Option\
  BepInEx\
    plugins\
      BepInEx.LidarWireframeContour.dll
      NOLidarWireframeContour.Core.dll
      NOLidarWireframeContour_Data\
        lidar_shaders
        shader_source_hash.txt
```

### Steps

1. Install BepInEx 5 and Configuration Manager (if not already).
2. Copy **both DLLs** and the **`NOLidarWireframeContour_Data`** folder into `BepInEx\plugins\`.
3. Launch the game and enter a **mission**.
4. Press **F1** to open Configuration Manager — sections: General, Probe, Activation, Fade, Visual, Debug.

### Generated config

BepInEx writes:

`BepInEx\config\com.mursisru.lidarwireframecontour.bepinex.cfg`

Changes apply immediately via `SettingChanged` → `LidarConfig.ApplySnapshot`.

---

## Verify install

After loading a mission, BepInEx / game log should include something like:

```
[LidarWireframe] 0.3.6V loaded (stage=Mission, gpu=True)
```

| Check | Healthy |
|-------|---------|
| `gpu=True` | URP passes registered |
| `bundleBytes` | **> 15000** |
| Wireframe at TTI ≤ 7 s | Night, gear up, speed > 30 m/s, AGL < 500 m |

If `gpu=False` or no wireframe, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md).

---

## Uninstall

| Loader | Remove |
|--------|--------|
| NOLoader | Delete `NOLoader\mods\LidarWireframeContour\` |
| BepInEx | Delete `BepInEx\plugins\BepInEx.LidarWireframeContour.dll`, `NOLidarWireframeContour.Core.dll`, `NOLidarWireframeContour_Data\`, and optional `BepInEx\config\com.mursisru.lidarwireframecontour.bepinex.cfg` |

---

## Related

- [README](../README.md) — overview and features
- [ARCHITECTURE.md](ARCHITECTURE.md) — runtime design
- [CONFIGURATION.md](CONFIGURATION.md) — all settings
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — common issues
