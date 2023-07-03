using Dan200.Core.Input;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;
using System;

namespace Dan200.Core.Window
{
    internal enum DisplayType
    {
        Monitor,
        Tablet,
        Phone,
        TV,
        Unknown
    }

	internal interface IWindow : IDisposable
    {
        string Title { get; set; }
        int Width { get; }
        int Height { get; }
        Vector2I Size { get; }
        bool Closed { get; }
        bool Fullscreen { get; set; }
        bool Maximised { get; }
        bool VSync { get; set; }
        bool Focus { get; }
        DisplayType DisplayType { get; }

        DeviceCollection InputDevices { get; }
		IRenderer Renderer { get; }

        event StructEventHandler<IWindow> OnClosed;
        event StructEventHandler<IWindow> OnResized;

        void SetIcon(Bitmap bitmap);
    }
}

