namespace Dan200.Core.Platform
{
    internal enum PlatformID
    {
        Unknown,
        Windows,
        MacOS,
        Linux,
        Android,
		IOS,
    }

    internal static class PlatformIDExtensions
    {
        public static bool IsDesktop(this PlatformID platform)
        {
            return !platform.IsMobile();
        }

        public static bool IsMobile(this PlatformID platform)
        {
            return platform == PlatformID.Android || platform == PlatformID.IOS;
        }
    }
}
