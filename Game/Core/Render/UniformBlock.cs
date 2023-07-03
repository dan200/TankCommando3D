using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dan200.Core.Math;
using Dan200.Core.Render.OpenGL;
using Dan200.Core.Util;
using OpenTK.Graphics.OpenGL;

namespace Dan200.Core.Render
{
	internal interface IUniformData
	{
	}

	internal interface IUniformBlock
	{
		int GLUniformBuffer { get; }
	}

    [Flags]
    internal enum UniformBlockFlags
    {
        Default = 0,
        Dynamic = 1,
    }

	internal class UniformBlock<TUniformData> : IUniformBlock, IDisposable
        where TUniformData : struct, IUniformData
	{
		private UniformBlockFlags m_flags;
		private int m_uniformBuffer;
		public TUniformData Data;

		public int GLUniformBuffer
		{
			get
			{
				return m_uniformBuffer;
			}
		}

		public UniformBlock(UniformBlockFlags flags = UniformBlockFlags.Default)
		{
			m_flags = flags;
			GL.GenBuffers(1, out m_uniformBuffer);
			Data = default(TUniformData);
		}

		public void Dispose()
		{
			GL.DeleteBuffers(1, ref m_uniformBuffer);
		}

		public void Upload()
		{
			var layout = UniformBlockLayout.Get<TUniformData>();
			unsafe
			{
                byte* src = stackalloc byte[layout.SrcSize];
                Marshal.StructureToPtr<TUniformData>(Data, new IntPtr(src), false);
				if (layout.IsIdentity)
				{
                    Upload(src, layout.DstSize);
				}
				else
				{
					byte* dst = stackalloc byte[layout.DstSize];
					foreach (var entry in layout.Entries)
					{
						byte* srcPos = src + entry.SrcOffset;
						byte* srcEnd = srcPos + entry.Size;
						byte* dstPos = dst + entry.DstOffset;
						while (srcPos < srcEnd)
						{
							*(dstPos++) = *(srcPos++);
						}
					}
					Upload(dst, layout.DstSize);
				}
			}
		}

		private unsafe void Upload(byte* data, int size)
		{
			var bufferUsageHint = ((m_flags & UniformBlockFlags.Dynamic) != 0) ?
				BufferUsageHint.DynamicDraw :
				BufferUsageHint.StaticDraw;
			
			GL.BindBuffer(BufferTarget.UniformBuffer, m_uniformBuffer);
			GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(size), new IntPtr(data), bufferUsageHint);
			GLUtils.CheckError();
		}
	}
}
