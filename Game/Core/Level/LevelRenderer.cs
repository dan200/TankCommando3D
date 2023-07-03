using System;
using Dan200.Core.Interfaces;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System.Collections.Generic;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Core.Main;
using System.Reflection;
using Dan200.Core.Level;

namespace Dan200.Core.Level
{
	internal class EffectHelperPool : IDisposable
	{
        private IRenderer m_renderer;
		private ShaderDefines m_baseDefines;
		private Dictionary<Type, EffectHelper> m_effects;

		public EffectHelperPool(IRenderer renderer, ShaderDefines baseDefines)
		{
            m_renderer = renderer;
			m_baseDefines = baseDefines;
			m_effects = new Dictionary<Type, EffectHelper>();
		}

		public void Dispose()
		{
			foreach (var effect in m_effects)
			{
				effect.Value.Dispose();
			}
			m_effects = null;
		}

        public TEffectHelper Get<TEffectHelper>() where TEffectHelper : EffectHelper
		{
			var type = typeof(TEffectHelper);
			EffectHelper result;
			if (!m_effects.TryGetValue(type, out result))
			{
				try
				{
                    result = (TEffectHelper)Activator.CreateInstance(type, m_renderer, m_baseDefines);
				}
				catch (TargetInvocationException e)
				{
					App.Rethrow(e.InnerException);
				}
				m_effects[type] = result;
			}
			return (TEffectHelper)result;
		}
	}

	internal struct LightingParams
	{
		public readonly LightingSystem Lights;
		public readonly int FirstPointLight;
		public readonly int NumPointLights;
		public readonly int FirstDirectionalLight;
		public readonly int NumDirectionalLights;
		public readonly bool IncludeAmbientLight;

		public LightingParams(LightingSystem lights, int firstPointLight, int numPointLights, int firstDirectionalLight, int numDirectionalLights, bool includeAmbientLight)
		{
			Lights = lights;
			FirstPointLight = firstPointLight;
			NumPointLights = numPointLights;
			FirstDirectionalLight = firstDirectionalLight;
			NumDirectionalLights = numDirectionalLights;
			IncludeAmbientLight = includeAmbientLight;
		}
	}

	internal interface IDrawable<in TEffectHelper>
		where TEffectHelper : EffectHelper
	{
		void Draw(IRenderer renderer, TEffectHelper effect);
	}

	internal class DrawQueue
	{
		private struct PerEffectDrawQueue
		{
			public List<object> Drawables;
			public Func<EffectHelperPool, EffectHelper> GetEffect;
			public Action<List<object>, IRenderer, EffectHelper> Draw;
		}

		private Dictionary<Type, PerEffectDrawQueue> m_opaqueDrawQueues;
		private Dictionary<Type, PerEffectDrawQueue> m_cutoutDrawQueues;
		private Dictionary<Type, PerEffectDrawQueue> m_translucentDrawQueues;
		private Dictionary<Type, PerEffectDrawQueue> m_shadowDrawQueues;
		private bool m_empty;

		public bool IsEmpty
		{
			get
			{
				return m_empty;
			}
		}

		public DrawQueue()
		{
			m_opaqueDrawQueues = new Dictionary<Type, PerEffectDrawQueue>();
			m_cutoutDrawQueues = new Dictionary<Type, PerEffectDrawQueue>();
			m_translucentDrawQueues = new Dictionary<Type, PerEffectDrawQueue>();
			m_shadowDrawQueues = new Dictionary<Type, PerEffectDrawQueue>();
			m_empty = true;
		}

		private void ClearDrawQueues(Dictionary<Type, PerEffectDrawQueue> queues)
		{
			foreach (var pair in queues)
			{
				var queue = pair.Value;
				queue.Drawables.Clear();
			}
		}

		public void Clear()
		{
			ClearDrawQueues(m_opaqueDrawQueues);
			ClearDrawQueues(m_cutoutDrawQueues);
			ClearDrawQueues(m_translucentDrawQueues);
			ClearDrawQueues(m_shadowDrawQueues);
			m_empty = true;
		}

		private static EffectHelper PerEffectQueue_GetEffect<TEffectHelper>(EffectHelperPool pool)
			where TEffectHelper : EffectHelper
		{
			return pool.Get<TEffectHelper>();
		}

		private static void PerEffectQueue_Draw<TEffectHelper>(List<object> drawables, IRenderer renderer, EffectHelper effect)
			where TEffectHelper : EffectHelper
		{
			var typedEffect = (TEffectHelper)effect;
			foreach (var drawable in drawables)
			{
				var typedDrawable = (IDrawable<TEffectHelper>)drawable;
				typedDrawable.Draw(renderer, typedEffect);
			}
		}

		private Dictionary<Type, PerEffectDrawQueue> GetDrawQueuesForRenderPass(RenderPass pass)
		{
			switch (pass)
			{
				case RenderPass.Opaque:
				default:
					return m_opaqueDrawQueues;
				case RenderPass.Cutout:
					return m_cutoutDrawQueues;
				case RenderPass.Translucent:
					return m_translucentDrawQueues;
			}
		}

		private void AddToDrawQueues<TEffectHelper>(Dictionary<Type, PerEffectDrawQueue> queues, IDrawable<TEffectHelper> drawable)
			where TEffectHelper : EffectHelper
		{
			var type = typeof(TEffectHelper);
			PerEffectDrawQueue queue;
			if (!queues.TryGetValue(type, out queue))
			{
				queue = new PerEffectDrawQueue();
				queue.Drawables = new List<object>();
				queue.GetEffect = PerEffectQueue_GetEffect<TEffectHelper>;
				queue.Draw = PerEffectQueue_Draw<TEffectHelper>;
				queues.Add(type, queue);
			}
			queue.Drawables.Add(drawable);
			m_empty = false;
		}

		public void Add<TEffectHelper>(IDrawable<TEffectHelper> drawable, RenderPass pass)
			where TEffectHelper : WorldEffectHelper
		{
			AddToDrawQueues(GetDrawQueuesForRenderPass(pass), drawable);
		}

		public void AddShadow<TEffectHelper>(IDrawable<TEffectHelper> drawable)
			where TEffectHelper : ShadowEffectHelper
		{
			AddToDrawQueues(m_shadowDrawQueues, drawable);
		}

		public void Draw(IRenderer renderer, UniformBlock<CameraUniformData> cameraData, LightingParams lighting, EffectHelperPool pool, RenderPass pass)
		{
			var queues = GetDrawQueuesForRenderPass(pass);
			foreach (var pair in queues)
			{
				var queue = pair.Value;
				if (queue.Drawables.Count == 0)
				{
					continue;
				}

				// Bind the effect
				var effect = (WorldEffectHelper)queue.GetEffect(pool);
				renderer.CurrentEffect = effect.Instance;

				// Setup global uniforms
				effect.CameraBlock = cameraData;
				if (effect is LitEffectHelper)
				{
					var litEffect = (LitEffectHelper)effect;
					litEffect.AmbientLightColour = lighting.IncludeAmbientLight ? lighting.Lights.AmbientLight.Colour : ColourF.Black;
					for (int i = 0; i < lighting.NumPointLights; ++i)
					{
						litEffect.PointLights[i] = lighting.Lights.PointLights[lighting.FirstPointLight + i];
					}
					for (int i = 0; i < lighting.NumDirectionalLights; ++i)
					{
						litEffect.DirectionalLights[i] = lighting.Lights.DirectionalLights[lighting.FirstDirectionalLight + i];
					}
				}

				// Draw the drawables
				queue.Draw(queue.Drawables, renderer, effect);
			}
		}

		public void DrawShadows(IRenderer renderer, UniformBlock<CameraUniformData> cameraData, PointLight light, EffectHelperPool pool)
		{
			var queues = m_shadowDrawQueues;
			foreach (var pair in queues)
			{
				var queue = pair.Value;
				if (queue.Drawables.Count == 0)
				{
					continue;
				}

				// Bind the effect
				var effect = (ShadowEffectHelper)queue.GetEffect(pool);
				renderer.CurrentEffect = effect.Instance;

				// Setup global uniforms
				effect.CameraBlock = cameraData;
				effect.LightPosition = light.Position;

				// Draw the drawables
				queue.Draw(queue.Drawables, renderer, effect);
			}
		}

		public void DrawShadows(IRenderer renderer, UniformBlock<CameraUniformData> cameraData, DirectionalLight light, EffectHelperPool pool)
		{
			var queues = m_shadowDrawQueues;
			foreach (var pair in queues)
			{
				var queue = pair.Value;
				if (queue.Drawables.Count == 0)
				{
					continue;
				}

				// Bind the effect
				var effect = (ShadowEffectHelper)queue.GetEffect(pool);
				renderer.CurrentEffect = effect.Instance;

				// Setup global uniforms
				effect.CameraBlock = cameraData;
				effect.LightDirection = light.Direction;

				// Draw the drawables
				queue.Draw(queue.Drawables, renderer, effect);
			}
		}
	}

	internal class LevelRenderer : IDisposable
	{
        private Level m_level;
        private LightingSystem m_lighting;

		private Dictionary<Pair<int, int>, EffectHelperPool> m_effectsPools;
		private EffectHelperPool m_directionalShadowEffectPool;
		private EffectHelperPool m_pointShadowEffectPool;
		private UniformBlock<CameraUniformData> m_cameraUniforms;
		private DrawQueue m_drawQueue;

        public LevelRenderer(IRenderer renderer, Level level)
		{
			m_level = level;
            m_lighting = m_level.GetSystem<LightingSystem>();

			m_effectsPools = new Dictionary<Pair<int, int>, EffectHelperPool>();

			var directionalShadowDefines = new ShaderDefines();
            var pointShadowDefines = new ShaderDefines();
            pointShadowDefines.Define("SHADOW_LIGHT_IS_POSITIONAL");
            m_directionalShadowEffectPool = new EffectHelperPool(renderer, directionalShadowDefines);
			m_pointShadowEffectPool = new EffectHelperPool(renderer, pointShadowDefines);

			m_cameraUniforms = new UniformBlock<CameraUniformData>(UniformBlockFlags.Dynamic);
			m_drawQueue = new DrawQueue();
		}

		public void Dispose()
		{
			foreach (var pool in m_effectsPools.Values)
			{
				pool.Dispose();
			}
			m_effectsPools.Clear();
			m_directionalShadowEffectPool.Dispose();
			m_pointShadowEffectPool.Dispose();
			m_cameraUniforms.Dispose();
		}

		private void PrepareToDraw(View view)
		{
            // Let each entity prepare for drawing (usually animation is done here)
            foreach (var component in m_level.GetComponentsWithInterface<IPrepareToDraw>())
            {
                if (component.Entity.Visible)
                {
                    component.PrepareToDraw(view);
                }
            }
		}

		private void Draw(IRenderer renderer, LightingParams lighting, RenderPass pass)
		{
			// Create an effect pool for this light combination
			EffectHelperPool pool;
			if (!m_effectsPools.TryGetValue(Pair.Create(lighting.NumPointLights, lighting.NumDirectionalLights), out pool))
			{
				var baseDefines = new ShaderDefines();
				baseDefines.Define("USE_DIFFUSE_TEXTURE");
				baseDefines.Define("USE_EMISSIVE_TEXTURE");
                baseDefines.Define("USE_SPECULAR_TEXTURE");
                baseDefines.Define("USE_NORMAL_TEXTURE");
                if (lighting.NumPointLights > 0)
				{
					baseDefines.Define("NUM_POINT_LIGHTS", lighting.NumPointLights);
				}
				if (lighting.NumDirectionalLights > 0)
				{
					baseDefines.Define("NUM_DIRECTIONAL_LIGHTS", lighting.NumDirectionalLights);
				}
				pool = new EffectHelperPool(renderer, baseDefines);
				m_effectsPools.Add(Pair.Create(lighting.NumPointLights, lighting.NumDirectionalLights), pool);
			}

			// Draw the queue using this effect pool
			m_drawQueue.Draw(renderer, m_cameraUniforms, lighting, pool, pass);
		}

		private void DrawShadows(IRenderer renderer, PointLight light)
		{
			// Draw the queue
			var pool = m_pointShadowEffectPool;
			m_drawQueue.DrawShadows(renderer, m_cameraUniforms, light, pool);
		}

		private void DrawShadows(IRenderer renderer, DirectionalLight light)
		{
			// Draw the queue
			var pool = m_directionalShadowEffectPool;
			m_drawQueue.DrawShadows(renderer, m_cameraUniforms, light, pool);
		}

		public void Draw(IRenderer renderer, View view, bool drawShadows = false)
		{
			// Prepare entities
			PrepareToDraw(view);

			// Collect the list of things to draw
			m_drawQueue.Clear();
            foreach (var component in m_level.GetComponentsWithInterface<IDraw>())
            {
				if (component.Entity.Visible)
                {
					component.AddToDrawQueue(m_drawQueue);
                }
            }

			// Early out on an empty world
			if (m_drawQueue.IsEmpty)
			{
				return;
			}

			// Count the lights
			int numShadowCastingDirectionalLights = 0;
			int numShadowCastingPointLights = 0;
			if (drawShadows)
			{
				for (int i = 0; i<m_lighting.DirectionalLights.Count; ++i)
				{
					if (m_lighting.DirectionalLights[i].CastShadows)
					{
						App.Assert(numShadowCastingDirectionalLights == i, "Shadow casting lights must be sorted to the front of the list");
						numShadowCastingDirectionalLights++;
					}
				}
				for (int i = 0; i < m_lighting.PointLights.Count; ++i)
				{
					if (m_lighting.PointLights[i].CastShadows)
					{
						App.Assert(numShadowCastingPointLights == i, "Shadow casting lights must be sorted to the front of the list");
						numShadowCastingPointLights++;
					}
				}
			}
			int totalLights = numShadowCastingPointLights + numShadowCastingDirectionalLights;

			// Set camera uniforms
			m_cameraUniforms.Data.ViewMatrix = view.Camera.ViewMatrix;
			m_cameraUniforms.Data.ProjectionMatrix = view.Camera.ProjectionMatrix;
			m_cameraUniforms.Data.CameraPosition = view.Camera.Position;
			m_cameraUniforms.Upload();

			// Draw the opaque geometry lit by all non-shadow-casting lights
			Draw(renderer, new LightingParams(m_lighting, numShadowCastingPointLights, m_lighting.PointLights.Count - numShadowCastingPointLights, numShadowCastingDirectionalLights, m_lighting.DirectionalLights.Count - numShadowCastingDirectionalLights, true), RenderPass.Opaque);
			Draw(renderer, new LightingParams(m_lighting, numShadowCastingPointLights, m_lighting.PointLights.Count - numShadowCastingPointLights, numShadowCastingDirectionalLights, m_lighting.DirectionalLights.Count - numShadowCastingDirectionalLights, true), RenderPass.Cutout);

            var shadowStencilParams = new StencilParameters();
            shadowStencilParams.Test = StencilTest.Always;
            shadowStencilParams.PassOp = StencilOp.Increment;
            shadowStencilParams.BackfacePassOp = StencilOp.Decrement;

            // Draw each shadow-casting directional light in order
            if (numShadowCastingDirectionalLights > 0)
			{
				for (int i = 0; i < numShadowCastingDirectionalLights; ++i)
				{
					// Disable colour writes
					renderer.ColourWrite = false;
					renderer.DepthWrite = false;

					// Enable stencil writes
					renderer.StencilTest = true;
					renderer.StencilParameters = shadowStencilParams;

					// Clear the stencil buffer
					if (i > 0)
					{
						renderer.ClearStencilOnly(0);
					}

					// Draw the shadow volumes
					renderer.CullBackfaces = false;
					try
					{
						DrawShadows(renderer, m_lighting.DirectionalLights[i]);
					}
					finally
					{
						renderer.CullBackfaces = true;
					}

					// Enable colour writes
					renderer.ColourWrite = true;
					renderer.DepthWrite = true;

					// Filter out unlit areas
					var renderStencilParams = new StencilParameters();
					renderStencilParams.Test = StencilTest.EqualTo;
					renderStencilParams.RefValue = 0;
					renderer.StencilParameters = renderStencilParams;

					// Draw the opaque geometry lit by the light (additive)
					renderer.BlendMode = BlendMode.Additive;
					try
					{
						Draw(renderer, new LightingParams(m_lighting, 0, 0, i, 1, false), RenderPass.Opaque);
						Draw(renderer, new LightingParams(m_lighting, 0, 0, i, 1, false), RenderPass.Cutout);
					}
					finally
					{
						renderer.BlendMode = BlendMode.Overwrite;
					}

					// Disable stencil test
					renderer.StencilTest = false;
				}
			}

			// Draw each shadow-casting point light in order
			if (numShadowCastingPointLights > 0)
			{
				for (int i = 0; i < numShadowCastingPointLights; ++i)
				{
					// Disable colour writes
					renderer.ColourWrite = false;
					renderer.DepthWrite = false;

					// Enable stencil writes
					renderer.StencilTest = true;
					renderer.StencilParameters = shadowStencilParams;

					// Clear the stencil buffer
					if (i + numShadowCastingDirectionalLights > 0)
					{
						renderer.ClearStencilOnly(0);
					}

					// Draw the shadow volumes
					renderer.CullBackfaces = false;
					try
					{
						DrawShadows(renderer, m_lighting.PointLights[i]);
					}
					finally
					{
						renderer.CullBackfaces = true;
					}

					// Enable colour writes
					renderer.ColourWrite = true;
					renderer.DepthWrite = true;

					// Filter out unlit areas
					var renderStencilParams = new StencilParameters();
					renderStencilParams.Test = StencilTest.EqualTo;
					renderStencilParams.RefValue = 0;
					renderer.StencilParameters = renderStencilParams;

					// Draw the opaque geometry lit by the light (additive)
					renderer.BlendMode = BlendMode.Additive;
					try
					{
						Draw(renderer, new LightingParams(m_lighting, i, 1, 0, 0, false), RenderPass.Opaque);
						Draw(renderer, new LightingParams(m_lighting, i, 1, 0, 0, false), RenderPass.Cutout);
					}
					finally
					{
						renderer.BlendMode = BlendMode.Overwrite;
					}

					// Disable stencil test
					renderer.StencilTest = false;
				}
			}

			// Draw the translucent geometry lit by all lights (alpha)
			renderer.BlendMode = BlendMode.Alpha;
			try
			{
				Draw(renderer, new LightingParams(m_lighting, 0, m_lighting.PointLights.Count, 0, m_lighting.DirectionalLights.Count, true), RenderPass.Translucent);
			}
			finally
			{
				renderer.BlendMode = BlendMode.Overwrite;
			}
		}
	}
}
