using Dan200.Core.Assets;
using System;
using System.Linq;
using System.Collections.Generic;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;

namespace Dan200.Core.Render
{
	internal abstract class EffectInstance : IDisposable
    {
		public abstract Effect Effect { get; }
		public abstract event StructEventHandler<EffectInstance> OnUniformLocationsChanged;

		public abstract void Dispose();

		public abstract int GetUniformLocation(string name);
		public abstract int GetUniformBlockLocation(string name);
		public abstract int GetAttributeLocation(string name);

		public abstract void SetUniform(int location, int value);
		public abstract void SetUniform(int location, float value);
		public abstract void SetUniform(int location, Vector2 value);
		public abstract void SetUniform(int location, Vector3 value);
		public abstract void SetUniform(int location, Vector4 value);
		public abstract void SetUniform(int location, ColourF value, ColourSpace space = ColourSpace.SRGB);
		public abstract void SetUniform(int location, ref Matrix3 value);
		public abstract void SetUniform(int location, ref Matrix4 value);

		public void SetUniform(int location, int[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, float[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, Vector2[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, Vector3[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, Vector4[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, ColourF[] value, ColourSpace space = ColourSpace.SRGB)
		{
			SetUniform(location, value, 0, value.Length, space);
		}

		public void SetUniform(int location, Matrix3[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public void SetUniform(int location, Matrix4[] value)
		{
			SetUniform(location, value, 0, value.Length);
		}

		public abstract void SetUniform(int location, int[] value, int start, int count);
		public abstract void SetUniform(int location, float[] value, int start, int count);
		public abstract void SetUniform(int location, Vector2[] value, int start, int count);
		public abstract void SetUniform(int location, Vector3[] value, int start, int count);
		public abstract void SetUniform(int location, Vector4[] value, int start, int count);
		public abstract void SetUniform(int location, ColourF[] value, int start, int count, ColourSpace space = ColourSpace.SRGB);
		public abstract void SetUniform(int location, Matrix3[] value, int start, int count);
		public abstract void SetUniform(int location, Matrix4[] value, int start, int count);

		public abstract void SetUniform(int location, ITexture texture);
		public abstract void SetUniformBlock(int location, IUniformBlock block);
	}
}
