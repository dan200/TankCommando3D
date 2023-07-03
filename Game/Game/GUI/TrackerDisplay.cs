using Dan200.Core.GUI;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class TrackerDisplay : Element
    {
        public readonly Camera Camera;

        public struct Blip
        {
            public Vector3 Position;
            public bool Confirmed;
        }
        public readonly List<Blip> Blips;
        private Texture m_texture;

        public TrackerDisplay(Camera camera)
        {
            Camera = camera;
            Blips = new List<Blip>();
            m_texture = Texture.Get("gui/tracker.png", true);
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
            RequestRebuild();
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            float screenAspect = Screen.AspectRatio;
            float verticalFOV = Camera.FOV;
            float cameraHeightAtOne = Mathf.Tan(verticalFOV * 0.5f);
            var halfSize = Screen.Size * 0.5f;

            var blipSize = new Vector2(0.5f * m_texture.Width, m_texture.Height);
            foreach (var blip in Blips)
            {
                // Get position of blip
                var posCS = Camera.Transform.ToLocalPos(blip.Position);

                // Transform to screen space
                var posProjected = Camera.ProjectionMatrix.Transform(new Vector4(posCS, 1.0f));
                if(posProjected.W <= 0.0f)
                {
                    continue;
                }
                var posSS = posProjected.XYZ / posProjected.W;
                var pos = halfSize + halfSize * posSS.XY * new Vector2(1.0f, -1.0f);

                // Draw the blip
                var region = blip.Confirmed ?
                    new Quad(0.5f, 0.0f, 0.5f, 1.0f) :
                    new Quad(0.0f, 0.0f, 0.5f, 1.0f);
                builder.AddQuad(pos - 0.5f * blipSize, pos + 0.5f * blipSize, m_texture, region);

                // Draw the text
                var distance = posCS.Length;
                builder.AddText(
                    string.Format("{0:N0}M", distance),
                    pos + new Vector2(0.5f * blipSize.X + 6.0f, -0.5f * LowResUI.TextFont.GetHeight(LowResUI.TextFontSize)),
                    LowResUI.TextFont,
                    LowResUI.TextFontSize,
                    Colour.White
                );
            }
        }
    }
}
