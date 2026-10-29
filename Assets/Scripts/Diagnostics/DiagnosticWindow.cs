#if ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using UnityEngine;

namespace MoriEchoes.Diagnostics
{
    internal static class DiagnosticWindow
    {
        public static IEnumerator Prepare()
        {
            string[] args = Environment.GetCommandLineArgs();
            int width = Requested(args, "-screen-width", 1280);
            int height = Requested(args, "-screen-height", 720);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(0.35f);
        }

        private static int Requested(string[] args, string flag, int fallback)
        {
            int index = Array.IndexOf(args, flag);
            return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int value)
                ? Mathf.Clamp(value, 480, 3840) : fallback;
        }
    }
}
#endif
