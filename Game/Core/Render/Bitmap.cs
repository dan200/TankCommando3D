using Dan200.Core.Main;
using Dan200.Core.Util;

using System;
using System.IO;
using System.Runtime.InteropServices;

#if SDL
using SDL2;
#endif

#if ANDROID
using Android.Graphics;
#endif

#if IOS
using Foundation;
using CoreGraphics;
#endif

namespace Dan200.Core.Render
{
    internal class Bitmap
    {
        public unsafe struct Bits : IDisposable
        {
            private readonly Bitmap m_owner;
            public readonly int Width;
            public readonly int Height;
            public readonly int Stride;
            public readonly int BytesPerPixel;
            public readonly ColourSpace ColourSpace;
            public readonly byte* Data;

            public Bits(Bitmap owner, int width, int height, int stride, int bytesPerPixel, ColourSpace colourSpace, byte* data)
            {
                m_owner = owner;
                Width = width;
                Height = height;
                Stride = stride;
                BytesPerPixel = bytesPerPixel;
                ColourSpace = colourSpace;
                Data = data;
            }

            public void Dispose()
            {
                m_owner.Unlock();
            }

            public Colour GetPixel(int x, int y)
            {
                App.Assert(x >= 0 && x < Width);
                App.Assert(y >= 0 && y < Height);
                var r = *(Data + x * BytesPerPixel + y * Stride + 0);
                var g = *(Data + x * BytesPerPixel + y * Stride + 1);
                var b = *(Data + x * BytesPerPixel + y * Stride + 2);
                var a = (BytesPerPixel >= 4) ?
                    *(Data + x * BytesPerPixel + y * Stride + 3) :
                    (byte)255;
                return new Colour(r, g, b, a);
            }

            public ColourF Sample(float xFrac, float yFrac)
            {
                float x = xFrac * (float)Width;
                float y = yFrac * (float)Height;
                int px = (int)System.Math.Floor(x);
                int py = (int)System.Math.Floor(y);
                float fx = x - (float)System.Math.Floor(x);
                float fy = y - (float)System.Math.Floor(y);
                var c00 = GetPixel(px, py).ToColourF();
                var c01 = GetPixel(px, py + 1).ToColourF();
                var c10 = GetPixel(px + 1, py).ToColourF();
                var c11 = GetPixel(px + 1, py + 1).ToColourF();
                if(ColourSpace == ColourSpace.SRGB)
                {
                    c00 = c00.ToLinear();
                    c01 = c01.ToLinear();
                    c10 = c10.ToLinear();
                    c11 = c11.ToLinear();
                }
                var i0 = (1.0f - fx) * c00 + fx * c10;
                var i1 = (1.0f - fx) * c01 + fx * c11;
                var result = (1.0f - fy) * i0 + fy * i1;
                if(ColourSpace == ColourSpace.SRGB)
                {
                    result = result.ToSRGB();
                }
                return result;
            }

            public void SetPixel(int x, int y, Colour colour)
            {
                App.Assert(x >= 0 && x < Width);
                App.Assert(y >= 0 && y < Height);
                *(Data + x * BytesPerPixel + y * Stride + 0) = colour.R;
                *(Data + x * BytesPerPixel + y * Stride + 1) = colour.G;
                *(Data + x * BytesPerPixel + y * Stride + 2) = colour.B;
                if (BytesPerPixel >= 4)
                {
                    *(Data + x * BytesPerPixel + y * Stride + 3) = colour.A;
                }
            }
        }

        private int m_width;
        private int m_height;
        private int m_bytesPerPixel;
        private int m_stride;
        private ColourSpace m_colourSpace;
        private byte[] m_data;
        private GCHandle m_lockHandle;
        private bool m_locked;

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public ColourSpace ColourSpace
        {
            get
            {
                return m_colourSpace;
            }
            set
            {
                m_colourSpace = value;
            }
        }

        public Bitmap(int width, int height, int bytesPerPixel, ColourSpace colourSpace)
        {
            App.Assert(width > 0);
            App.Assert(height > 0);
            App.Assert(bytesPerPixel == 3 || bytesPerPixel == 4);
            m_width = width;
            m_height = height;
            m_bytesPerPixel = bytesPerPixel;
            m_stride = width * bytesPerPixel;
            m_colourSpace = colourSpace;
            m_data = new byte[m_height * m_stride];
			m_locked = false;
        }

        public Bitmap(int width, int height, int bytesPerPixel, int stride, ColourSpace colourSpace, byte[] data)
        {
            App.Assert(width > 0);
            App.Assert(height > 0);
            App.Assert(bytesPerPixel == 3 || bytesPerPixel == 4);
            App.Assert(stride >= width * bytesPerPixel);
            App.Assert(data.Length >= height * stride);
            m_width = width;
            m_height = height;
            m_bytesPerPixel = bytesPerPixel;
            m_stride = stride;
            m_data = data;
            m_colourSpace = colourSpace;
            m_locked = false;
        }

        public Bitmap(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                LoadFromStream(stream);
            }
        }

        public Bitmap(Stream stream)
        {
            LoadFromStream(stream);
        }

        public unsafe Bits Lock()
        {
            App.Assert(!m_locked);
            m_lockHandle = GCHandle.Alloc(m_data, GCHandleType.Pinned);
            m_locked = true;
            return new Bits(this, m_width, m_height, m_stride, m_bytesPerPixel, m_colourSpace, (byte*)m_lockHandle.AddrOfPinnedObject());
        }

        private void Unlock()
        {
            App.Assert(m_locked);
            m_lockHandle.Free();
            m_locked = false;
        }

        public unsafe void FlipY()
        {
            App.Assert(!m_locked);
            using (var bits = Lock())
            {
                var buffer = new byte[bits.Width * bits.BytesPerPixel];
                var srcBuffer = new byte[bits.Width * bits.BytesPerPixel];
                for (int y = 0; y < bits.Height / 2; ++y)
                {
                    int srcY = (bits.Height - 1) - y;
                    Marshal.Copy(
                        new IntPtr(bits.Data + y * bits.Stride),
                        buffer, 0, buffer.Length
                    );
                    Marshal.Copy(
                        new IntPtr(bits.Data + srcY * bits.Stride),
                        srcBuffer, 0, srcBuffer.Length
                    );
                    Marshal.Copy(
                        buffer, 0,
                        new IntPtr(bits.Data + srcY * bits.Stride),
                        buffer.Length
                    );
                    Marshal.Copy(
                        srcBuffer, 0,
                        new IntPtr(bits.Data + y * bits.Stride),
                        srcBuffer.Length
                    );
                }
            }
        }

		public Bitmap Copy()
		{
			var dataCopy = new byte[m_data.Length];
			Array.Copy(m_data, dataCopy, m_data.Length);
			return new Bitmap(m_width, m_height, m_bytesPerPixel, m_stride, m_colourSpace, dataCopy);
		}

        public Bitmap ToColourSpace(ColourSpace space)
        {
            if(space == m_colourSpace)
            {
                // No conversion necessary
                return this;
            }
            else if(space == ColourSpace.Linear)
            {
                // SRGB -> Linear
                App.Assert(m_colourSpace == ColourSpace.SRGB && space == ColourSpace.Linear);
                var copy = new Bitmap(m_width, m_height, m_bytesPerPixel, ColourSpace.Linear);
                using (var srcBits = Lock())
                {
                    using (var dstBits = copy.Lock())
                    {
                        for(int y=0; y<srcBits.Height; ++y)
                        {
                            for(int x=0; x<dstBits.Width; ++x)
                            {
                                var src = srcBits.GetPixel(x, y);
                                dstBits.SetPixel(x, y, src.ToLinear());
                            }
                        }
                    }
                }
                return copy;
            }
            else
            {
                // Linear -> SRGB
                App.Assert(m_colourSpace == ColourSpace.Linear && space == ColourSpace.SRGB);
                var copy = new Bitmap(m_width, m_height, m_bytesPerPixel, ColourSpace.SRGB);
                using (var srcBits = Lock())
                {
                    using (var dstBits = copy.Lock())
                    {
                        for (int y = 0; y < srcBits.Height; ++y)
                        {
                            for (int x = 0; x < dstBits.Width; ++x)
                            {
                                var src = srcBits.GetPixel(x, y);
                                dstBits.SetPixel(x, y, src.ToSRGB());
                            }
                        }
                    }
                }
                return copy;
            }
        }

        public Bitmap Resize(int width, int height, bool maintainAspect, bool smooth)
        {
            App.Assert(!m_locked);
            Rect srcRect;
            if (maintainAspect)
            {
                // Crop the image to keep the same aspect ratio
                var aspect = (double)m_width / (double)m_height;
                var resizedAspect = (double)width / (double)height;
                if (aspect > resizedAspect)
                {
                    // Crop horizontally
                    int desiredWidth = (width * m_height) / height;
                    srcRect.X = (m_width - desiredWidth) / 2;
                    srcRect.Y = 0;
                    srcRect.Width = desiredWidth;
                    srcRect.Height = m_height;
                }
                else if (aspect < resizedAspect)
                {
                    // Crop vertically
                    int desiredHeight = (height * m_width) / width;
                    srcRect.X = 0;
                    srcRect.Y = (m_height - desiredHeight) / 2;
                    srcRect.Width = m_width;
                    srcRect.Height = desiredHeight;
                }
                else
                {
                    // Don't crop
                    srcRect.X = 0;
                    srcRect.Y = 0;
                    srcRect.Width = m_width;
                    srcRect.Height = m_height;
                }
            }
            else
            {
                // Don't crop
                srcRect.X = 0;
                srcRect.Y = 0;
                srcRect.Width = m_width;
                srcRect.Height = m_height;
            }

            if (smooth)
            {
                // Smooth resize
				var result = new Bitmap(width, height, m_bytesPerPixel, m_colourSpace);
                using (var srcBits = Lock())
                {
                    using (var dstBits = result.Lock())
                    {
                        float fDstWidth = (float)dstBits.Width;
                        float fDstHeight = (float)dstBits.Height;
                        float fSrcAreaStartX = (float)srcRect.X / (float)srcBits.Width;
                        float fSrcAreaStartY = (float)srcRect.Y / (float)srcBits.Height;
                        float fSrcAreaWidth = (float)srcRect.Width / (float)srcBits.Width;
                        float fSrcAreaHeight = (float)srcRect.Height / (float)srcBits.Height;
                        float kernelSize = 0.25f;
                        for (int y = 0; y < dstBits.Height; ++y)
                        {
                            for (int x = 0; x < dstBits.Width; ++x)
                            {
                                var c00 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x - kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y - kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c01 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x + kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y - kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c10 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x - kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y + kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                var c11 = srcBits.Sample(
                                    fSrcAreaStartX + (((float)x + kernelSize) / fDstWidth) * fSrcAreaWidth,
                                    fSrcAreaStartY + (((float)y + kernelSize) / fDstHeight) * fSrcAreaHeight
                                );
                                if (m_colourSpace == ColourSpace.SRGB)
                                {
                                    c00 = c00.ToLinear();
                                    c01 = c01.ToLinear();
                                    c10 = c10.ToLinear();
                                    c11 = c11.ToLinear();
                                }
                                ColourF cResult = ((c00 + c01 + c10 + c11) * 0.25f);
                                if(m_colourSpace == ColourSpace.SRGB)
                                {
                                    cResult = cResult.ToSRGB();
                                }
                                dstBits.SetPixel(x, y, cResult.ToColour());
                            }
                        }
                    }
                }
                return result;
            }
            else
            {
                // Nearest neighbour resize
				var result = new Bitmap(width, height, m_bytesPerPixel, m_colourSpace);
                using (var srcBits = Lock())
                {
                    using (var dstBits = result.Lock())
                    {
                        int dstWidth = dstBits.Width;
                        int dstHeight = dstBits.Height;
                        int srcAreaStartX = srcRect.X;
                        int srcAreaStartY = srcRect.Y;
                        int srcAreaWidth = srcRect.Width;
                        int srcAreaHeight = srcRect.Height;
                        for (int y = 0; y < dstHeight; ++y)
                        {
                            for (int x = 0; x < dstWidth; ++x)
                            {
                                var c = srcBits.GetPixel(
                                    srcAreaStartX + (x / dstWidth) * srcAreaWidth,
                                    srcAreaStartY + (y / dstHeight) * srcAreaHeight
                                );
                                dstBits.SetPixel(x, y, c);
                            }
                        }
                    }
                }
                return result;
            }
        }

        public void Blit(Bitmap src, int xPos, int yPos)
        {
            // Blended blit
            App.Assert(!m_locked);
            using (var dstBits = Lock())
            {
                using (var srcBits = src.Lock())
                {
                    var xStart = System.Math.Max(xPos, 0);
                    var xEnd = System.Math.Min(xPos + srcBits.Width, dstBits.Width);
                    var yStart = System.Math.Max(yPos, 0);
                    var yEnd = System.Math.Min(yPos + srcBits.Height, dstBits.Height);
                    for (int dstY = yStart; dstY < yEnd; ++dstY)
                    {
                        var srcY = dstY - yPos;
                        for (int dstX = xStart; dstX < xEnd; ++dstX)
                        {
                            var srcX = dstX - xPos;
                            var srcColor = srcBits.GetPixel(srcX, srcY);
                            var dstColor = dstBits.GetPixel(dstX, dstY);
                            var srcA = srcColor.A;
                            var oneMinusSrcA = 255 - srcColor.A;
                            var blendedR = (byte)((srcColor.R * srcA + dstColor.R * oneMinusSrcA) >> 8);
                            var blendedG = (byte)((srcColor.G * srcA + dstColor.G * oneMinusSrcA) >> 8);
                            var blendedB = (byte)((srcColor.B * srcA + dstColor.B * oneMinusSrcA) >> 8);
                            var blendedA = (byte)((srcColor.A * 255 + dstColor.A * oneMinusSrcA) >> 8);
                            dstBits.SetPixel(dstX, dstY, new Colour(blendedR, blendedG, blendedB, blendedA));
                        }
                    }
                }
            }
        }

        public void Save(string path)
        {
            App.Assert(!m_locked);
            SaveToFile(path);
        }

#if SDL
        private static bool SurfaceNeedsConversion(SDL.SDL_PixelFormat fmt)
        {
            if (fmt.BitsPerPixel == 24 || fmt.BitsPerPixel == 32)
            {
                uint rmask, gmask, bmask, amask;
                SDLUtils.GetRGBASurfaceMasks(fmt.BitsPerPixel / 8, out rmask, out gmask, out bmask, out amask);
                return fmt.Rmask != rmask || fmt.Gmask != gmask || fmt.Bmask != bmask || fmt.Amask != amask;
            }
            else
            {
                return true;
            }
        }

        private unsafe void LoadFromStream(Stream stream)
        {
            // Load the surface
            IntPtr surface;
            var data = stream.ReadToEnd();
            fixed (byte* pData = data)
            {
                var rwops = SDL.SDL_RWFromMem(data, data.Length);
                SDLUtils.CheckResult("SDL_RWFromMem", rwops);

                surface = SDL_image.IMG_Load_RW(rwops, 0);
                SDLUtils.CheckResult("IMG_Load_RW", surface);
            }

            // Extract info from the surface
            var surfaceDetails = (SDL.SDL_Surface)Marshal.PtrToStructure(
                surface,
                typeof(SDL.SDL_Surface)
            );
            m_width = surfaceDetails.w;
            m_height = surfaceDetails.h;
            m_colourSpace = ColourSpace.SRGB;

            // Extract and check the format
            var pixelFormat = (SDL.SDL_PixelFormat)Marshal.PtrToStructure(
                surfaceDetails.format,
                typeof(SDL.SDL_PixelFormat)
            );
            if (SurfaceNeedsConversion(pixelFormat))
            {
                // Unknown format, convert to RGBA
                IntPtr newSurface = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                SDLUtils.CheckResult("SDL_ConvertSurfaceFormat", newSurface);
                SDL.SDL_FreeSurface(surface);
                surface = newSurface;

                // Get format again as stride may have changed
                surfaceDetails = (SDL.SDL_Surface)Marshal.PtrToStructure(
                    surface,
                    typeof(SDL.SDL_Surface)
                );
                m_stride = surfaceDetails.pitch;
                m_bytesPerPixel = 4;
            }
            else
            {
                // Recognised format
                m_stride = surfaceDetails.pitch;
                m_bytesPerPixel = pixelFormat.BitsPerPixel / 8;
            }

            try
            {
                // Lock the surface
                bool needsLock = SDL.SDL_MUSTLOCK(surface);
                if (needsLock)
                {
                    SDLUtils.CheckResult("SDL_LockSurface", SDL.SDL_LockSurface(surface));
                }
                try
                {
                    // Copy data from the surface
                    m_data = new byte[m_height * m_stride];
                    Marshal.Copy(surfaceDetails.pixels, m_data, 0, m_data.Length);
                }
                finally
                {
                    // Unlock the surface
                    if (needsLock)
                    {
                        SDL.SDL_UnlockSurface(surface);
                    }
                }
            }
            finally
            {
                SDL.SDL_FreeSurface(surface);
            }
        }

        private void SaveToFile(string path)
        {
            var srgbCopy = ToColourSpace(ColourSpace.SRGB);
            using (var bits = srgbCopy.Lock())
            {
                var surface = SDLUtils.CreateSurfaceFromBits(bits);
                try
                {
                    SDL_image.IMG_SavePNG(surface, path);
                }
                finally
                {
                    SDL.SDL_FreeSurface(surface);
                }
            }
        }
#elif ANDROID
        private void LoadFromStream(Stream stream)
        {
            throw new NotImplementedException();
        }

        private void SaveToFile(string path)
        {
            throw new NotImplementedException("Bitmap saving is not supported on Android");
        }
#elif IOS
        private void LoadFromStream(Stream stream)
        {
            throw new NotImplementedException();
        }

        private void SaveToFile(string path)
        {
            throw new NotImplementedException("Bitmap saving is not supported on iOS");
        }
#else
#error "NYI"
#endif
    }
}
