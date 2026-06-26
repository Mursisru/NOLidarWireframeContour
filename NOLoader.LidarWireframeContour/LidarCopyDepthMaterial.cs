using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarCopyDepthMaterial
    {
        private static Material? _material;

        internal static Material? Instance
        {
            get
            {
                if (_material != null)
                    return _material;

                Shader? shader = Shader.Find("Hidden/Universal Render Pipeline/CopyDepth");
                if (shader == null)
                    return null;

                _material = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                return _material;
            }
        }
    }
}
