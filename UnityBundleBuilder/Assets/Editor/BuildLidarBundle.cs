#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Build / Lidar Wireframe Shaders
/// Batch: Unity -batchmode -projectPath UnityBundleBuilder -executeMethod BuildLidarBundle.BuildBatch -quit
/// </summary>
public static class BuildLidarBundle
{
    private const string BundleName = "lidar_shaders";
    private const string ShaderAssetPath = "Assets/LidarWireframe/LidarWireframeContour.shader";
    private const string PackShaderAssetPath = "Assets/LidarWireframe/LidarDepthPack.shader";
    private const string EdgeShaderAssetPath = "Assets/LidarWireframe/LidarDepthEdge.shader";
    private const string MaterialAssetPath = "Assets/LidarWireframe/LidarWireframeContour.mat";

    [MenuItem("Build/Lidar Wireframe Shaders")]
    public static void BuildFromMenu()
    {
        if (!BuildInternal())
            EditorUtility.DisplayDialog("Lidar Wireframe", "Build failed — see Console.", "OK");
        else
            EditorUtility.DisplayDialog("Lidar Wireframe", "Bundle built and copied.\nSee Console for paths.", "OK");
    }

    public static void BuildBatch()
    {
        bool ok = BuildInternal();
        EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool BuildInternal()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderAssetPath);
        if (shader == null)
        {
            Debug.LogError("[LidarWireframe] Shader not found at " + ShaderAssetPath);
            return false;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (material == null)
        {
            Debug.LogError("[LidarWireframe] Material not found at " + MaterialAssetPath);
            return false;
        }

        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.StandaloneWindows64,
            new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
                UnityEngine.Rendering.GraphicsDeviceType.Direct3D12,
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
            });

        AssetImporter shaderImporter = AssetImporter.GetAtPath(ShaderAssetPath);
        if (shaderImporter == null)
        {
            Debug.LogError("[LidarWireframe] No importer for " + ShaderAssetPath);
            return false;
        }

        shaderImporter.assetBundleName = BundleName;
        shaderImporter.SaveAndReimport();

        AssetImporter materialImporter = AssetImporter.GetAtPath(MaterialAssetPath);
        if (materialImporter == null)
        {
            Debug.LogError("[LidarWireframe] No importer for " + MaterialAssetPath);
            return false;
        }

        materialImporter.assetBundleName = BundleName;
        materialImporter.SaveAndReimport();

        AssetImporter packImporter = AssetImporter.GetAtPath(PackShaderAssetPath);
        if (packImporter != null)
        {
            packImporter.assetBundleName = BundleName;
            packImporter.SaveAndReimport();
        }

        AssetImporter edgeImporter = AssetImporter.GetAtPath(EdgeShaderAssetPath);
        if (edgeImporter != null)
        {
            edgeImporter.assetBundleName = BundleName;
            edgeImporter.SaveAndReimport();
        }

        AssetDatabase.Refresh();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outDir = Path.Combine(projectRoot, "BuiltBundles");
        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64);

        string bundlePath = Path.Combine(outDir, BundleName);
        if (!File.Exists(bundlePath))
        {
            Debug.LogError("[LidarWireframe] Bundle file missing: " + bundlePath);
            return false;
        }

        string repoRoot = Directory.GetParent(projectRoot).FullName;
        string[] copyTargets =
        {
            Path.Combine(repoRoot, "NOLidarWireframeContour_Data", BundleName),
            Path.Combine(repoRoot, "NOLoader.LidarWireframeContour", "bin", "Release", "NOLidarWireframeContour_Data", BundleName),
            Path.Combine(repoRoot, "NOLoader.LidarWireframeContour", "bin", "Debug", "NOLidarWireframeContour_Data", BundleName),
        };

        foreach (string target in copyTargets)
        {
            string dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(bundlePath, target, true);
            Debug.Log("[LidarWireframe] Copied bundle -> " + target);
        }

        Debug.Log("[LidarWireframe] OK: " + bundlePath + " (" + new FileInfo(bundlePath).Length + " bytes)");
        Debug.Log("[LidarWireframe] Shader supported=" + shader.isSupported);
        Debug.Log("[LidarWireframe] Material shader supported=" + material.shader.isSupported);
        return true;
    }
}
#endif
