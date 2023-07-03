using System;
using Dan200.Core.Util;

namespace Dan200.Core.Render
{
	internal abstract class EffectHelper : IDisposable
	{
		public readonly EffectInstance Instance;

        public EffectHelper(IRenderer renderer, Effect effect, ShaderDefines defines)
		{
            Instance = renderer.Instantiate(effect, defines);
			LookupUniforms();
			Instance.OnUniformLocationsChanged += OnUniformLocationsChanged;
		}

		public void Dispose()
		{
			Instance.OnUniformLocationsChanged -= OnUniformLocationsChanged;
			Instance.Dispose();
		}

		private void OnUniformLocationsChanged(EffectInstance instance, StructEventArgs args)
		{
			LookupUniforms();
		}

		protected abstract void LookupUniforms();
	}
}
