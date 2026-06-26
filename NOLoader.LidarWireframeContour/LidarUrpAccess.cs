using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarUrpAccess
    {
        private static readonly MethodInfo? GetBackBufferMethod = typeof(ScriptableRenderer).GetMethod(
            "GetCameraColorBackBuffer",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static RTHandle ResolveColorBackBuffer(ScriptableRenderer renderer, CommandBuffer cmd, RTHandle fallback)
        {
            if (GetBackBufferMethod == null)
                return fallback;

            object? result = GetBackBufferMethod.Invoke(renderer, new object[] { cmd });
            return result as RTHandle ?? fallback;
        }
    }
}
