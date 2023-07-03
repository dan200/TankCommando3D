using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Script;
using Dan200.Core.Main;
using Dan200.Core.Assets;
using Dan200.Core.Render;
using Dan200.Core.Math;

namespace Dan200.Core.Systems
{
    internal struct LightingSystemData
    {
    }

    internal class LightingSystem : System<LightingSystemData>
    {
        private AmbientLight m_ambientLight;
		private List<DirectionalLight> m_directionalLights;
        private List<PointLight> m_pointLights;

        public AmbientLight AmbientLight
        {
            get
            {
                return m_ambientLight;
            }
        }

		public List<DirectionalLight> DirectionalLights
		{
			get
			{
				return m_directionalLights;
			}
		}

        public List<PointLight> PointLights
        {
            get
            {
                return m_pointLights;
            }
        }

        protected override void OnInit(in LightingSystemData properties)
        {
            // Setup some suitable default lights
			m_ambientLight = new AmbientLight(ColourF.Black);
			m_directionalLights = new List<DirectionalLight>();
            m_pointLights = new List<PointLight>();
        }

        protected override void OnShutdown()
        {
        }
    }
}
