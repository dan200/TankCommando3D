using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Main;
using Dan200.Core.Window;
using Dan200.Core.Window.SDL2;

#if GLES
using OpenTK.Graphics.ES20;
using BufferUsageHint = OpenTK.Graphics.ES20.BufferUsage;
using VertexAttribIPointerType = OpenTK.Graphics.ES20.VertexAttribPointerType;
#else
using OpenTK.Graphics.OpenGL;
using GLExt = OpenTK.Graphics.OpenGL.GL.Ext;
#endif

namespace Dan200.Core.Render.OpenGL
{
    internal class OpenGLRenderer : IRenderer, IDisposable
    {
        public static OpenGLRenderer Instance
        {
            get;
            private set;
        }

        private readonly SDL2Window m_window;
        private EffectInstance m_effect;
        private RenderTexture m_target;
        private BlendMode m_blendMode;
        private Rect m_viewport;
        private bool m_colourWrite;
        private bool m_depthWrite;
        private bool m_depthTest;
        private bool m_stencilTest;
        private StencilParameters m_stencilParams;
        private bool m_cullBackfaces;
        private RenderStats m_renderStats;

        public EffectInstance CurrentEffect
        {
            get
            {
                return m_effect;
            }
            set
            {
                if(m_effect != value)
                {
                    if (m_effect != null)
                    {
                        ((OpenGLEffectInstance)m_effect).Active = false;
                    }
                    m_effect = value;
                    if (m_effect != null)
                    {
                        BindEffect(m_effect);
                        ((OpenGLEffectInstance)m_effect).Active = true;
                    }
                }
            }
        }

        public RenderTexture Target
        {
            get
            {
                return m_target;
            }
            set
            {
                if(m_target != value)
                {
                    m_target = value;
                    if(m_target != null)
                    {
                        // Bind the framebuffer
                        GLExt.BindFramebuffer(FramebufferTarget.Framebuffer, m_target.FrameBufferID);
                        ResetViewport();
                        GLUtils.CheckError();
                    }
                    else
                    {
                        // Bind nothing
#if IOS
                        App.IOSWindow.ViewController.GLKView.BindDrawable();
#else
                        GLExt.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
#endif
                        ResetViewport();
                        GLUtils.CheckError();
                    }
                }
            }
        }

        public BlendMode BlendMode
        {
            get
            {
                return m_blendMode;
            }
            set
            {
                if (m_blendMode != value)
                {
                    // Set the blend mode
                    m_blendMode = value;
                    switch (m_blendMode)
                    {
                        case BlendMode.Overwrite:
                        default:
                            {
                                GL.Disable(EnableCap.Blend);
                                break;
                            }
                        case BlendMode.Alpha:
                            {
                                GL.Enable(EnableCap.Blend);
                                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                                break;
                            }
                        case BlendMode.Additive:
                            {
                                GL.Enable(EnableCap.Blend);
                                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                                break;
                            }
                    }
                    GLUtils.CheckError();
                }
            }
        }

        public Rect Viewport
        {
            get
            {
                return m_viewport;
            }
            set
            {
                if (m_viewport != value)
                {
                    m_viewport = value;
                    GL.Viewport(m_viewport.X, m_viewport.Y, m_viewport.Width, m_viewport.Height);
                    GL.Scissor(m_viewport.X, m_viewport.Y, m_viewport.Width, m_viewport.Height);
                }
            }
        }

        public bool ColourWrite
        {
            get
            {
                return m_colourWrite;
            }
            set
            {
                if (m_colourWrite != value)
                {
                    m_colourWrite = value;
                    GL.ColorMask(m_colourWrite, m_colourWrite, m_colourWrite, m_colourWrite);
                }
            }
        }

        public bool DepthWrite
        {
            get
            {
                return m_depthWrite;
            }
            set
            {
                if (m_depthWrite != value)
                {
                    m_depthWrite = value;
                    GL.DepthMask(m_depthWrite);
                }
            }
        }

        public bool DepthTest
        {
            get
            {
                return m_depthTest;
            }
            set
            {
                if (m_depthTest != value)
                {
                    m_depthTest = value;
                    if(m_depthTest)
                    {
                        GL.Enable(EnableCap.DepthTest);
                    }
                    else
                    {
                        GL.Disable(EnableCap.DepthTest);
                    }
                }
            }
        }

        public bool StencilTest
        {
            get
            {
                return m_stencilTest;
            }
            set
            {
                if (m_stencilTest != value)
                {
                    m_stencilTest = value;
                    if (m_stencilTest)
                    {
                        GL.Enable(EnableCap.StencilTest);
                    }
                    else
                    {
                        GL.Disable(EnableCap.StencilTest);
                    }
                }
            }
        }

        public StencilParameters StencilParameters
        {
            get
            {
                return m_stencilParams;
            }
            set
            {
                m_stencilParams = value;
                GL.StencilFunc(Convert(m_stencilParams.Test), m_stencilParams.RefValue, 0xff);
                GL.StencilOpSeparate(StencilFace.Back, Convert(m_stencilParams.StencilTestFailOp), Convert(m_stencilParams.DepthTestFailOp), Convert(m_stencilParams.PassOp));
                GL.StencilOpSeparate(StencilFace.Front, Convert(m_stencilParams.BackfaceStencilTestFailOp), Convert(m_stencilParams.BackfaceDepthTestFailOp), Convert(m_stencilParams.BackfacePassOp));
            }
        }

        public bool CullBackfaces
        {
            get
            {
                return m_cullBackfaces;
            }
            set
            {
                if (m_cullBackfaces != value)
                {
                    m_cullBackfaces = value;
                    if (m_cullBackfaces)
                    {
                        GL.Enable(EnableCap.CullFace);
                    }
                    else
                    {
                        GL.Disable(EnableCap.CullFace);
                    }
                }
            }
        }

        public RenderStats RenderStats
        {
            get
            {
                return m_renderStats;
            }
        }

        public OpenGLRenderer(SDL2Window window)
        {
            App.Assert(Instance == null);
            Instance = this;

            m_window = window;
            m_effect = null;
            m_target = null;
            m_viewport = new Rect(0, 0, window.Width, window.Height);
            m_colourWrite = true;
            m_depthWrite = true;
            m_depthTest = true;
            m_stencilTest = false;
            m_stencilParams = new StencilParameters();
            m_cullBackfaces = true;
            m_renderStats = new RenderStats();

            m_window.MakeCurrent();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.ColorMask(true, true, true, true);
            GLUtils.CheckError();

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Gequal);
            GL.DepthMask(true);
            GLUtils.CheckError();

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GLUtils.CheckError();

            GL.Disable(EnableCap.StencilTest);
            GL.StencilFunc(StencilFunction.Always, 0, 0xff);
            GL.StencilOpSeparate(StencilFace.Back, OpenTK.Graphics.OpenGL.StencilOp.Keep, OpenTK.Graphics.OpenGL.StencilOp.Keep, OpenTK.Graphics.OpenGL.StencilOp.Keep);
            GL.StencilOpSeparate(StencilFace.Front, OpenTK.Graphics.OpenGL.StencilOp.Keep, OpenTK.Graphics.OpenGL.StencilOp.Keep, OpenTK.Graphics.OpenGL.StencilOp.Keep);
            GL.ClearStencil(0);
            GL.StencilMask(0xff);
            GLUtils.CheckError();

            GL.Enable(EnableCap.ScissorTest);
            GLUtils.CheckError();

            GL.Disable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.Zero);
            GLUtils.CheckError();

            GL.LineWidth(1.0f);
            GLUtils.CheckError();

            GL.Enable(EnableCap.FramebufferSrgb);
            GLUtils.CheckError();
        }

        public void Dispose()
        {
            App.Assert(Instance == this);
            Instance = null;
        }

        public void MakeCurrent()
        {
            m_window.MakeCurrent();

            GL.Viewport(m_viewport.X, m_viewport.Y, m_viewport.Width, m_viewport.Height);
            GL.Scissor(m_viewport.X, m_viewport.Y, m_viewport.Width, m_viewport.Height);

            switch (m_blendMode)
            {
                case BlendMode.Overwrite:
                default:
                    {
                        GL.Disable(EnableCap.Blend);
                        break;
                    }
                case BlendMode.Alpha:
                    {
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                        break;
                    }
                case BlendMode.Additive:
                    {
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                        break;
                    }
            }

            GL.ColorMask(m_colourWrite, m_colourWrite, m_colourWrite, m_colourWrite);
            GL.DepthMask(m_depthWrite);

            if (m_depthTest)
            {
                GL.Enable(EnableCap.DepthTest);
            }
            else
            {
                GL.Disable(EnableCap.DepthTest);
            }

            if (m_stencilTest)
            {
                GL.Enable(EnableCap.StencilTest);
            }
            else
            {
                GL.Disable(EnableCap.StencilTest);
            }

            GL.StencilFunc(Convert(m_stencilParams.Test), m_stencilParams.RefValue, 0xff);
            GL.StencilOpSeparate(StencilFace.Back, Convert(m_stencilParams.StencilTestFailOp), Convert(m_stencilParams.DepthTestFailOp), Convert(m_stencilParams.PassOp));
            GL.StencilOpSeparate(StencilFace.Front, Convert(m_stencilParams.BackfaceStencilTestFailOp), Convert(m_stencilParams.BackfaceDepthTestFailOp), Convert(m_stencilParams.BackfacePassOp));

            if (m_cullBackfaces)
            {
                GL.Enable(EnableCap.CullFace);
            }
            else
            {
                GL.Disable(EnableCap.CullFace);
            }

            if(m_effect != null)
            {
                BindEffect(m_effect);
            }

            GLUtils.CheckError();
        }

        public void Present()
        {
            GL.Flush();
            GLUtils.CheckError();
            m_window.SwapBuffers();
            m_renderStats.EndFrame();
        }

        public void ResetViewport()
        {
            if(m_target != null)
            {
                Viewport = new Rect(0, 0, m_target.Width, m_target.Height);
            }
            else
            {
                Viewport = new Rect(0, 0, m_window.Width, m_window.Height);
            }
        }

        public void Clear(ColourF colour, byte stencil=0)
        {
            GL.ClearDepth(0.0);
            GL.ClearColor(colour.R, colour.G, colour.B, colour.A);
            GL.ClearStencil(stencil);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GLUtils.CheckError();
        }

        public void ClearColourOnly(ColourF colour)
        {
            GL.ClearColor(colour.R, colour.G, colour.B, colour.A);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GLUtils.CheckError();
        }

        public void ClearDepthOnly()
        {
            GL.ClearDepth(0.0);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GLUtils.CheckError();
        }

        public void ClearStencilOnly(byte stencil=0)
        {
            GL.ClearStencil(stencil);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GLUtils.CheckError();
        }

        public EffectInstance Instantiate(Effect effect, ShaderDefines defines)
        {
            return new OpenGLEffectInstance(effect, defines);
        }

        public IRenderGeometry<TVertex> Upload<TVertex>(Geometry<TVertex> geometry, RenderGeometryFlags flags) where TVertex : struct, IVertex
        {
            return new OpenGLRenderGeometry<TVertex>(geometry, flags);
        }

        private void BindEffect(EffectInstance effect)
        {
            // Select the program
            var glEffect = (OpenGLEffectInstance)effect;
            GL.UseProgram(glEffect.ProgramID);

            // Bind all the texture units
            for (int unit = 0; unit < glEffect.m_textures.Count; ++unit)
            {
                var texture = glEffect.m_textures[unit];
                GL.ActiveTexture(TextureUnit.Texture0 + unit);
                GL.BindTexture(TextureTarget.Texture2D, ((IOpenGLTexture)texture).TextureID);
                GLUtils.CheckError();
            }

            // Bind all the uniform blocks
            for (int unit = 0; unit < glEffect.m_uniformBlocks.Count; ++unit)
            {
                var block = glEffect.m_uniformBlocks[unit];
                GL.BindBuffer(BufferTarget.UniformBuffer, block.GLUniformBuffer);
                GL.BindBufferBase(BufferTarget.UniformBuffer, unit, block.GLUniformBuffer);
                GLUtils.CheckError();
            }
        }

        public void Draw<TVertex>(IRenderGeometry<TVertex> geometry) where TVertex : struct, IVertex
        {
            DrawRange(geometry, 0, geometry.IndexCount);
        }

        public unsafe void DrawRange<TVertex>(IRenderGeometry<TVertex> geometry, int startIndex, int indexCount) where TVertex : struct, IVertex
        {
            var glGeometry = (OpenGLRenderGeometry<TVertex>)geometry;

            App.Assert(startIndex >= 0 && indexCount >= 0 && startIndex + indexCount <= geometry.IndexCount);
            App.Assert((indexCount % geometry.PrimitiveType.GetVertexCount()) == 0);
            if (m_effect == null || indexCount == 0)
            {
                return;
            }

            // Generate the VAO
            int vertexArrayObject;
            GL.GenVertexArrays(1, out vertexArrayObject);

            // Bind the VAOO
            GL.BindVertexArray(vertexArrayObject);

            // Bind the buffers
            GL.BindBuffer(BufferTarget.ArrayBuffer, glGeometry.GLVertexBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, glGeometry.GLIndexBuffer);
            GLUtils.CheckError();

            // Setup the attributes
            var layout = VertexLayout.Get<TVertex>();
            var stride = layout.Stride;
            var entries = layout.Entries;
            var effect = m_effect;
            for (int i = 0; i < entries.Length; ++i)
            {
                int location = effect.GetAttributeLocation(entries[i].Name);
                if (location >= 0)
                {
                    GL.VertexAttribPointer(
                        location,
                        entries[i].ElementCount,
                        entries[i].GLElementType,
                        entries[i].Normalised,
                        stride,
                        entries[i].Offset
                    );
                    GLUtils.CheckError();
                    GL.EnableVertexAttribArray(location);
                    GLUtils.CheckError();
                }
            }
            GLUtils.CheckError();

            // Draw the buffers
            var beginMode = (geometry.PrimitiveType == Primitive.Triangles) ? BeginMode.Triangles : BeginMode.Lines;
            GL.DrawElements(beginMode, indexCount, DrawElementsType.UnsignedShort, (IntPtr)(startIndex * sizeof(ushort)));
            m_renderStats.AddDrawCall(indexCount / geometry.PrimitiveType.GetVertexCount());
            GLUtils.CheckError();

            // Delete the VAO
            GL.DeleteVertexArray(vertexArrayObject);
        }

        public unsafe Bitmap Capture()
        {
            // Complete all outstanding rendering
            GL.Finish();

            // Capture image
            var bitmap = new Bitmap(m_viewport.Width, m_viewport.Height, 3, ColourSpace.SRGB);
            using (var bits = bitmap.Lock())
            {
                try
                {
#if !GLES
                    GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
                    GL.PixelStore(PixelStoreParameter.PackRowLength, bits.Stride / bits.BytesPerPixel);
#endif
                    GL.ReadPixels(
                        m_viewport.X, m_viewport.Y,
                        bits.Width, bits.Height,
                        (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                        PixelType.UnsignedByte,
                        new IntPtr(bits.Data)
                    );
                }
                finally
                {
                    GLUtils.CheckError();
                }
            }

            // Flip and return image
            bitmap.FlipY();
            return bitmap;
        }

        private static StencilFunction Convert(StencilTest test)
        {
            switch (test)
            {
                case Render.StencilTest.Always:
                default:
                    return StencilFunction.Always;
                case Render.StencilTest.EqualTo:
                    return StencilFunction.Equal;
                case Render.StencilTest.LessThan:
                    return StencilFunction.Less;
                case Render.StencilTest.LessThanOrEqualTo:
                    return StencilFunction.Lequal;
                case Render.StencilTest.GreaterThan:
                    return StencilFunction.Greater;
                case Render.StencilTest.GreaterThanOrEqualTo:
                    return StencilFunction.Gequal;
            }
        }

        private static OpenTK.Graphics.OpenGL.StencilOp Convert(StencilOp op)
        {
            switch (op)
            {
                case StencilOp.Keep:
                default:
                    return OpenTK.Graphics.OpenGL.StencilOp.Keep;
                case StencilOp.Replace:
                    return OpenTK.Graphics.OpenGL.StencilOp.Replace;
                case StencilOp.Increment:
                    return OpenTK.Graphics.OpenGL.StencilOp.IncrWrap;
                case StencilOp.Decrement:
                    return OpenTK.Graphics.OpenGL.StencilOp.DecrWrap;
            }
        }
    }
}
