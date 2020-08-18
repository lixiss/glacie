using System;

namespace Glacie.Data.Compression.Utilities
{
    internal static class LibDeflatePlatformSupport
    {
        private static readonly bool s_isSupported;

        static LibDeflatePlatformSupport()
        {
            s_isSupported = Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        public static bool IsSupported => s_isSupported;
    }
}
