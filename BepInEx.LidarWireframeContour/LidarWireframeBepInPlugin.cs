using System.IO;
using BepInEx;
using NOLoader.LidarWireframeContour;

namespace LidarWireframeContour.BepInEx
{
    [BepInPlugin(PluginGuid, PluginName, AppVersion.Semver)]
    public sealed class LidarWireframeBepInPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.mursisru.lidarwireframecontour.bepinex";
        public const string PluginName = "Lidar Wireframe Contour";

        private void Awake()
        {
            LidarWireframeBepInConfig.Bind(Config);

            string? pluginDir = Path.GetDirectoryName(Info.Location);
            if (string.IsNullOrEmpty(pluginDir))
            {
                Logger.LogError("Could not resolve plugin directory.");
                return;
            }

            LidarWireframeHost.Ensure(pluginDir, Logger);
            Logger.LogInfo($"[LidarWireframe] {AppVersion.DisplayVersion} BepInEx bootstrap.");
        }
    }
}
