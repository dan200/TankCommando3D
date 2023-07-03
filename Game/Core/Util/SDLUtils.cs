#if SDL
using Dan200.Core.Main;
using Dan200.Core.Render;
using SDL2;
using System;

namespace Dan200.Core.Util
{
    internal class SDLException : Exception
    {
        public SDLException(string method) : base(method + " failed.")
        {
        }

        public SDLException(string method, string message) : base(method + " failed: " + message + ".")
        {
        }
    }

    internal static class SDLUtils
    {
        private static SDLException MakeSDLException(string functionName)
        {
            var error = SDL.SDL_GetError();
            if (error != null && error.Length > 0)
            {
                SDL.SDL_ClearError();
                return new SDLException(functionName, error);
            }
            else
            {
                return new SDLException(functionName);
            }
        }

        public static void CheckResult(string functionName, int result)
        {
            if (result < 0)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckResult(string functionName, IntPtr result)
        {
            if (result == IntPtr.Zero)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void CheckResult(string functionName, string result)
        {
            if (result == null)
            {
                throw MakeSDLException(functionName);
            }
        }

        public static void GetRGBASurfaceMasks(int bytesPerPixel, out uint o_rmask, out uint o_gmask, out uint o_bmask, out uint o_amask)
        {
            App.Assert(bytesPerPixel == 3 || bytesPerPixel == 4);
            if (BitConverter.IsLittleEndian)
            {
                o_rmask = 0x000000ff;
                o_gmask = 0x0000ff00;
                o_bmask = 0x00ff0000;
                o_amask = (bytesPerPixel == 3) ? 0 : 0xff000000;
            }
            else
            {
                int shift = (bytesPerPixel == 3) ? 8 : 0;
                o_rmask = (uint)(0xff000000 >> shift);
                o_gmask = (uint)(0x00ff0000 >> shift);
                o_bmask = (uint)(0x0000ff00 >> shift);
                o_amask = (uint)(0x000000ff >> shift);
            }
        }

        public static unsafe IntPtr CreateSurfaceFromBits(Bitmap.Bits bits)
        {
            uint rmask, gmask, bmask, amask;
            GetRGBASurfaceMasks(bits.BytesPerPixel, out rmask, out gmask, out bmask, out amask);
            var surface = SDL.SDL_CreateRGBSurfaceFrom(new IntPtr(bits.Data), bits.Width, bits.Height, bits.BytesPerPixel * 8, bits.Stride, rmask, gmask, bmask, amask);
            CheckResult("SDL_CreateRGBSurfaceFrom", surface);
            return surface;
        }
    }
}
#endif
