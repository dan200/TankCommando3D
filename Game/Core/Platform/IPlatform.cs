using System;
using Dan200.Core.Network;
using Dan200.Core.Util;
using Dan200.Core.Window;

namespace Dan200.Core.Platform
{
	internal enum WebBrowserType
	{
		Overlay,
		External
	}

	internal interface IPlatform
	{		
        bool Headless { get; }
		PlatformType Type { get; }
		INetwork Network { get; }
		string SystemLanguage { get; }

		IWindow CreateWindow(string title, int width, int height, bool fullscreen, bool maximised, bool vsync);
		void ShowMessageBox(string title, string message, bool isError);

		bool OpenFileBrowser(string path);
		bool OpenTextEditor(string path);
		bool OpenWebBrowser(string url, WebBrowserType preferredType);
	}

	internal static class PlatformExtensions
	{
        public static bool IsDesktop(this IPlatform platform)
        {
            return !platform.IsMobile();
        }

        public static bool IsMobile(this IPlatform platform)
        {
            switch(platform.Type)
            {
                case PlatformType.Android:
                case PlatformType.IOS:
                    return true;
                default:
                    return false;
            }
        }

        public static void OpenTwitter(this IPlatform platform, string handle)
		{
			platform.OpenWebBrowser(
				string.Format("http://www.twitter.com/{0}", handle.URLEncode()),
				WebBrowserType.External
			);
		}

		public static void OpenComposeTweet(this IPlatform platform, string tweet)
		{
			platform.OpenWebBrowser(
				string.Format("http://www.twitter.com/intent/tweet?text={0}", tweet.URLEncode()),
				WebBrowserType.External
			);
		}
	}
}
