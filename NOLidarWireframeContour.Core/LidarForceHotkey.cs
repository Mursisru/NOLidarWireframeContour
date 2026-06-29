using System;
using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarForceHotkey
    {
        private static KeyCode[] s_modifiers = Array.Empty<KeyCode>();
        private static KeyCode s_mainKey = KeyCode.Y;
        private static string s_parsedBinding = string.Empty;
        private static int s_lastConsumedFrame = -1;

        internal static void ApplyBinding(string binding)
        {
            binding = binding?.Trim() ?? string.Empty;
            if (binding == s_parsedBinding)
                return;

            s_parsedBinding = binding;
            s_modifiers = Array.Empty<KeyCode>();
            s_mainKey = KeyCode.Y;

            if (string.IsNullOrEmpty(binding))
                return;

            string[] parts = binding.Split('+');
            if (parts.Length == 0)
                return;

            var mods = new KeyCode[parts.Length - 1];
            int modCount = 0;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (TryParseKey(parts[i].Trim(), out KeyCode mod))
                    mods[modCount++] = mod;
            }

            if (modCount > 0)
            {
                Array.Resize(ref mods, modCount);
                s_modifiers = mods;
            }

            string mainToken = parts[parts.Length - 1].Trim();
            if (TryParsePhysicalKey(mainToken, out KeyCode main))
                s_mainKey = main;
        }

        internal static bool TryConsumePress()
        {
            if (!LidarConfig.ForceHotkeyEnabled)
                return false;

            int frame = Time.frameCount;
            if (frame == s_lastConsumedFrame)
                return false;

            for (int i = 0; i < s_modifiers.Length; i++)
            {
                if (!Input.GetKey(s_modifiers[i]))
                    return false;
            }

            if (!Input.GetKeyDown(s_mainKey))
                return false;

            s_lastConsumedFrame = frame;
            return true;
        }

        private static bool TryParseKey(string token, out KeyCode key)
        {
            key = KeyCode.None;
            if (string.IsNullOrEmpty(token))
                return false;

            return Enum.TryParse(token, true, out key) && key != KeyCode.None;
        }

        private static bool TryParsePhysicalKey(string token, out KeyCode key)
        {
            key = KeyCode.None;
            if (string.IsNullOrEmpty(token))
                return false;

            if (TryParseKey(token, out key))
                return true;

            if (token.Length != 1)
                return false;

            char c = char.ToUpperInvariant(token[0]);
            if (c < 'A' || c > 'Z')
                return false;

            key = (KeyCode)((int)KeyCode.A + (c - 'A'));
            return true;
        }
    }
}
