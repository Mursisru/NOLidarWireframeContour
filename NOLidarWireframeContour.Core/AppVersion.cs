namespace NOLoader.LidarWireframeContour
{
    public static class AppVersion
    {
        /// <summary>Numeric semver only — mod.json, assembly, loaders.</summary>
        public const string Semver = "0.3.6";

        /// <summary>Type suffix: V=visual/client-only (no MP mechanic impact).</summary>
        public const string Suffix = "V";

        /// <summary>Full display string for logs, CHANGELOG, UI.</summary>
        public const string DisplayVersion = Semver + Suffix;
    }
}
