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
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Render;

namespace Dan200.Core.Systems
{
    internal struct GUISystemData
    {
    }

    internal class GUISystem : System<GUISystemData>
    {
        public Screen Screen
        {
            get;
            private set;
        }

        public Camera MainCamera
        {
            get;
            private set;
        }

        public GUISystem(Screen screen, Camera mainCamera)
        {
            Screen = screen;
            MainCamera = mainCamera;
        }

        protected override void OnInit(in GUISystemData properties)
        {
        }

        protected override void OnShutdown()
        {
        }
    }
}
