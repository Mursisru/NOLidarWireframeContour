using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarShaderAssets
    {
        private const string BundleFileName = "lidar_shaders";
        private const string ShaderAssetName = "LidarWireframeContour.shader";
        private const string EdgeShaderAssetName = "LidarDepthEdge.shader";
        private const string MaterialAssetName = "LidarWireframeContour.mat";
        private const string ShaderName = "Hidden/ACT/LidarWireframeContour";
        private const string EdgeShaderName = "Hidden/ACT/LidarDepthEdge";

        private static Shader? _compositeShader;
        private static Shader? _edgeShader;
        private static AssetBundle? _shaderBundle;
        private static bool _loadAttempted;
        private static string? _modRoot;

        internal static Shader? CompositeShader
        {
            get
            {
                if (!_loadAttempted)
                    TryLoad();
                return _compositeShader;
            }
        }

        internal static Shader? EdgeShader
        {
            get
            {
                if (!_loadAttempted)
                    TryLoad();
                return _edgeShader;
            }
        }

        internal static bool IsReady => CompositeShader != null;

        internal static string? LoadedBundlePath { get; private set; }

        internal static long LoadedBundleBytes { get; private set; }

        internal static bool HasEdgeShader => EdgeShader != null;

        internal static bool CompositeSupported => CompositeShader != null && CompositeShader.isSupported;

        internal static string CompositeShaderSourceHash { get; private set; } = string.Empty;

        internal static void Initialize(string modRoot)
        {
            _modRoot = modRoot;
            CompositeShaderSourceHash = ComputeCompositeSourceHash(modRoot);
            TryLoad();
        }

        internal static string ComputeCompositeSourceHash(string modRoot)
        {
            try
            {
                string hashFile = Path.Combine(modRoot, "NOLidarWireframeContour_Data", "shader_source_hash.txt");
                if (File.Exists(hashFile))
                {
                    string stored = File.ReadAllText(hashFile).Trim();
                    if (stored.Length >= 8)
                        return stored.Substring(0, 8);
                }

                string path = Path.Combine(modRoot, "NOLidarWireframeContour_Data", "Shaders", "LidarWireframeContour.shader");
                if (!File.Exists(path))
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NOLidarWireframeContour_Data", "Shaders", "LidarWireframeContour.shader");
                if (!File.Exists(path))
                    return "missing";

                byte[] bytes = File.ReadAllBytes(path);
                byte[] hash = SHA256.Create().ComputeHash(bytes);
                var sb = new StringBuilder(8);
                for (int i = 0; i < 4; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
            catch
            {
                return "error";
            }
        }

        private static void TryLoad()
        {
            _loadAttempted = true;

            if (TryLoadFromBundle(out Shader? bundled, out Shader? edge, out string? bundlePath) && bundled != null)
            {
                _compositeShader = bundled;
                _edgeShader = edge ?? Shader.Find(EdgeShaderName);
                LoadedBundlePath = bundlePath;
                if (!string.IsNullOrEmpty(bundlePath) && File.Exists(bundlePath))
                    LoadedBundleBytes = new FileInfo(bundlePath).Length;
                Debug.Log(
                    "[LidarWireframe] Shader loaded from bundle (" + bundlePath +
                    ", bytes=" + LoadedBundleBytes +
                    ", composite=" + (bundled != null) +
                    ", edge=" + (_edgeShader != null) +
                    ", supported=" + bundled!.isSupported + ").");
                return;
            }

            _compositeShader = Shader.Find(ShaderName);
            _edgeShader = Shader.Find(EdgeShaderName);
            if (_compositeShader != null)
                Debug.Log("[LidarWireframe] Shader found via Shader.Find (supported=" + _compositeShader.isSupported + ").");
        }

        private static bool TryLoadFromBundle(out Shader? shader, out Shader? edgeShader, out string? loadedPath)
        {
            shader = null;
            edgeShader = null;
            loadedPath = null;

            try
            {
                string[] roots =
                {
                    _modRoot ?? string.Empty,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NOLidarWireframeContour_Data"),
                };

                foreach (string root in roots)
                {
                    if (string.IsNullOrEmpty(root))
                        continue;

                    string[] candidates =
                    {
                        Path.Combine(root, "NOLidarWireframeContour_Data", BundleFileName),
                        Path.Combine(root, BundleFileName),
                        Path.Combine(root, "..", "NOLidarWireframeContour_Data", BundleFileName),
                    };

                    foreach (string path in candidates)
                    {
                        string full = Path.GetFullPath(path);
                        if (!File.Exists(full))
                            continue;

                        AssetBundle? bundle = AssetBundle.LoadFromFile(full);
                        if (bundle == null)
                            continue;

                        _shaderBundle = bundle;

                        Material? material = bundle.LoadAsset<Material>(MaterialAssetName);
                        if (material != null && material.shader != null)
                            shader = material.shader;
                        else
                        {
                            shader = bundle.LoadAsset<Shader>(ShaderAssetName);
                            if (shader == null)
                                shader = bundle.LoadAsset<Shader>("LidarWireframeContour");
                        }

                        edgeShader = bundle.LoadAsset<Shader>(EdgeShaderAssetName);
                        if (edgeShader == null)
                            edgeShader = bundle.LoadAsset<Shader>("LidarDepthEdge");

                        if (shader != null)
                        {
                            loadedPath = full;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LidarWireframe] Asset bundle load failed: " + ex.Message);
            }

            return false;
        }
    }
}
