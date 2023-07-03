using System;
using Dan200.Core.Main;
using Dan200.Core.Render.OpenGL;

#if GLES
using OpenTK.Graphics.ES20;
using GLExt = OpenTK.Graphics.ES20.GL;
using FramebufferAttachment = OpenTK.Graphics.ES20.FramebufferSlot;
using RenderbufferStorage = OpenTK.Graphics.ES20.RenderbufferInternalFormat;
#else
using OpenTK.Graphics.OpenGL;
using GLExt = OpenTK.Graphics.OpenGL.GL.Ext;
#endif

namespace Dan200.Core.Render
{
	internal class RenderTexture : ITexture, IDisposable, IOpenGLTexture
    {
        public static RenderTexture Current
        {
            get;
            private set;
        }

        private int m_width;
        private int m_height;

        private int m_texture;
        private int m_depthStencilBuffer;
        private int m_frameBuffer;

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

        public int TextureID
        {
            get
            {
                return m_texture;
            }
        }

        public int FrameBufferID
        {
            get
            {
                return m_frameBuffer;
            }
        }

		public RenderTexture(int width, int height, ColourSpace colourSpace, bool filter)
        {
            m_width = width;
            m_height = height;

            // Generate texture (for colour data)
			m_texture = GLUtils.CreateBlankTexture(width, height, filter, false, colourSpace);

            // Generate depth buffer (for depth data)
            GLExt.GenRenderbuffers(1, out m_depthStencilBuffer);
            try
            {
                GLExt.BindRenderbuffer(RenderbufferTarget.Renderbuffer, m_depthStencilBuffer);
#if GLES
    	        GLExt.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferStorage)All.Depth24Stencil8Oes, m_width, m_height);
#else
                GLExt.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, m_width, m_height);
#endif
            }
            finally
            {
                GLExt.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            }
            GLUtils.CheckError();

            // Generate Framebuffer (links the buffers together)
            GLExt.GenFramebuffers(1, out m_frameBuffer);
            try
            {
                GLExt.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBuffer);
                GLExt.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, m_texture, 0);
                GLUtils.CheckError();

                //GLExt.FramebufferRenderbuffer( FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, m_depthStencilBuffer );
                GLExt.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, m_depthStencilBuffer);
                GLExt.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, m_depthStencilBuffer);
                GLUtils.CheckError();

                // Check everything worked
                var status = GLExt.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (status != FramebufferErrorCode.FramebufferComplete)
                {
                    throw new OpenGLException("Error creating framebuffer: " + status);
                }
            }
            finally
            {
                // Return to the current binding
                if (Current != null)
                {
                    GLExt.BindFramebuffer(FramebufferTarget.Framebuffer, Current.m_frameBuffer);
                }
                else
                {
#if IOS
                    App.IOSWindow.ViewController.GLKView.BindDrawable();
#else
                    GLExt.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
#endif
                }
            }
            GLUtils.CheckError();
        }

        public void Dispose()
        {
            GLExt.DeleteFramebuffers(1, ref m_frameBuffer);
            m_frameBuffer = -1;

            GLExt.DeleteRenderbuffers(1, ref m_depthStencilBuffer);
            m_depthStencilBuffer = -1;

            GL.DeleteTextures(1, ref m_texture);
            m_texture = -1;

            GLUtils.CheckError();
        }

        public void Resize(int width, int height)
        {
            if (m_width != width || m_height != height)
            {
                m_width = width;
                m_height = height;

                // Rescale texture
                try
                {
                    GL.BindTexture(TextureTarget.Texture2D, m_texture);
                    GLUtils.ClearTexture(m_width, m_height, ColourSpace.Linear);
                }
                finally
                {
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
                GLUtils.CheckError();

                // Rescale depth buffer
                try
                {
                    GLExt.BindRenderbuffer(RenderbufferTarget.Renderbuffer, m_depthStencilBuffer);
#if GLES
					GLExt.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferStorage)All.Depth24Stencil8Oes, m_width, m_height);
#else
                    GLExt.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, m_width, m_height);
#endif
                }
                finally
                {
                    GLExt.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                }
                GLUtils.CheckError();
            }
        }
    }
}
