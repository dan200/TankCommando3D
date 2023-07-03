using Dan200.Core.Assets;
using Dan200.Core.Math;
using System.IO;
using System.Text;
using System;
using Dan200.Core.Lua;
using Dan200.Core.Util;

namespace Dan200.Core.Render
{
    internal class ParticleStyle : IBasicAsset
    {
        public static ParticleStyle Get(string path)
        {
            return Assets.Assets.Get<ParticleStyle>(path);
        }

        private string m_path;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string Texture
        {
            get;
            private set;
        }

        public float Lifetime
        {
            get;
            private set;
        }

        public float EmitterRate
        {
            get;
            private set;
        }

        public Vector3 Position
        {
            get;
            private set;
        }

        public Vector3 PositionRange
        {
            get;
            private set;
        }

        public Vector3 Velocity
        {
            get;
            private set;
        }

        public Vector3 VelocityRange
        {
            get;
            private set;
        }

        public Vector3 Gravity
        {
            get;
            private set;
        }

        public float Radius
        {
            get;
            private set;
        }

        public float FinalRadius
        {
            get;
            private set;
        }

        public ColourF Colour
        {
            get;
            private set;
        }

        public ColourF FinalColour
        {
            get;
            private set;
        }

        public static object LoadData(Stream stream, string path)
        {
			var decoder = new LONDecoder(stream);
            decoder.AddMacro("Vector2", LONMacros.Vector2);
            decoder.AddMacro("Vector3", LONMacros.Vector3);
            decoder.AddMacro("Colour", LONMacros.Colour);
			return decoder.DecodeValue().GetTable();
        }

        public ParticleStyle(string path, object data)
        {
            m_path = path;
            Reload(data);
        }

        public void Dispose()
        {
        }

        public void Reload(object data)
        {
            // TODO
            /*
            var table = (LuaTable)data;

			Lifetime = table.GetOptionalFloat("Lifetime", 5.0f);
            EmitterRate = table.GetOptionalFloat("EmitterRate", 1.0f);

			Position = table.GetOptionalVector3("Position", Math.Vector3.Zero);
			PositionRange = table.GetOptionalVector3("PositionRange", Math.Vector3.Zero);
            Velocity = table.GetOptionalVector3("Velocity", Math.Vector3.Zero);
            VelocityRange = table.GetOptionalVector3("VelocityRange", Math.Vector3.Zero);
            Gravity = table.GetOptionalVector3("Gravity", new Vector3(0.0f, -9.8f, 0.0f));
            Radius = table.GetOptionalFloat("Radius", 0.125f);
            FinalRadius = table.GetOptionalFloat("FinalRadius", Radius);

			var colour = table.GetOptionalColour("Colour", Render.Colour.White).ToColourF();
			var alpha = table.GetOptionalFloat("Alpha", 1.0f);
            colour.A = alpha;
            Colour = colour;

			var finalColour = table.GetOptionalColour("FinalColour", Render.Colour.White).ToColourF();
            var finalAlpha = table.GetOptionalFloat("FinalAlpha", alpha);
            finalColour.A = finalAlpha;
            FinalColour = finalColour;

			Texture = table.GetOptionalString("Texture", "white.png");
			*/
        }
    }
}
