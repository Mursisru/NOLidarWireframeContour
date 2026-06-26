# Shader bundle (required at runtime)

Runtime loads **`lidar_shaders`** only (AssetBundle). Loose `.shader` files in this folder are **not** used by the game.

Build with Unity 2022.3 LTS:

```powershell
..\scripts\build-shader-bundle.ps1
```

The script syncs `Shaders/*.shader` into `UnityBundleBuilder/Assets/LidarWireframe/` before compiling.

Output: `lidar_shaders` in this folder (~15–20 KB).
