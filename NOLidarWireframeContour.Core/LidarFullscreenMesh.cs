using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarFullscreenMesh
    {
        private static Mesh? _mesh;

        internal static Mesh Get()
        {
            if (_mesh != null)
                return _mesh;

            float topV = 1f;
            float bottomV = 0f;

            _mesh = new Mesh
            {
                name = "ACT.LidarFullscreenMesh",
                hideFlags = HideFlags.HideAndDontSave,
            };

            _mesh.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(-1f, 1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
            };
            _mesh.uv = new[]
            {
                new Vector2(0f, bottomV),
                new Vector2(0f, topV),
                new Vector2(1f, bottomV),
                new Vector2(1f, topV),
            };
            _mesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
            _mesh.UploadMeshData(true);
            return _mesh;
        }
    }
}
