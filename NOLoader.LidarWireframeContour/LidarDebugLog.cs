using System;
using System.IO;
using System.Text;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarDebugLog
    {
        private const string LogPath = @"C:\Users\at747\source\repos\NOLidarWireframeContour\debug-215717.log";
        private static readonly StringBuilder s_sb = new StringBuilder(512);

        internal static void ClearOnModLoad()
        {
            // #region agent log
            try
            {
                File.WriteAllText(LogPath, string.Empty);
            }
            catch
            {
            }
            // #endregion
        }

        internal static void Write(string hypothesisId, string location, string message, Action<StringBuilder> buildData)
        {
            if (!LidarConfig.DebugLogVerbose && message != "mod_loaded")
                return;

            // #region agent log
            try
            {
                s_sb.Clear();
                s_sb.Append('{');
                AppendStr(s_sb, "sessionId", "215717");
                s_sb.Append(',');
                AppendStr(s_sb, "hypothesisId", hypothesisId);
                s_sb.Append(',');
                AppendStr(s_sb, "location", location);
                s_sb.Append(',');
                AppendStr(s_sb, "message", message);
                s_sb.Append(',');
                s_sb.Append("\"data\":");
                s_sb.Append('{');
                buildData(s_sb);
                s_sb.Append('}');
                s_sb.Append(',');
                s_sb.Append("\"timestamp\":");
                s_sb.Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                s_sb.Append('}');
                s_sb.Append('\n');
                File.AppendAllText(LogPath, s_sb.ToString());
            }
            catch
            {
            }
            // #endregion
        }

        internal static void AppendFloat(StringBuilder sb, string key, float value)
        {
            sb.Append('\"');
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString("F3"));
        }

        private static void AppendStr(StringBuilder sb, string key, string value)
        {
            sb.Append('\"');
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(Escape(value));
            sb.Append('\"');
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
