using Dan200.Core.Assets;
using Dan200.Core.Lua;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.IO;
using Dan200.Core.Util;
using Dan200.Core.Serialisation;

namespace Dan200.Game.Level
{
    internal class Sky : IBasicAsset
    {
        public static Sky Get(string path)
        {
            return Assets.Get<Sky>(path);
        }

        public string Path
        {
            get;
            set;
        }

        public ColourF BackgroundColour
        {
            get;
            set;
        }

        public string BackgroundImage
        {
            get;
            set;
        }

        public ColourF AmbientColour
        {
            get;
            set;
        }

        private struct SkyData
        {
            public Colour BackgroundColour;
            public Colour AmbientColour;

            [Optional]
            public string BackgroundImage;
        }

        public static object LoadData(Stream stream, string path)
        {
            var decoder = new LONDecoder(stream);
            decoder.AddMacro("Colour", LONMacros.Colour);
            decoder.AddMacro("Vector3", LONMacros.Vector3);
            var table = decoder.DecodeValue().GetTable();
            return LONSerialiser.Parse<SkyData>(table);
        }

        public Sky(string path, object data)
        {
            Path = path;
            Load(data);
        }

        public void Reload(object data)
        {
            Unload();
            Load(data);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(object data)
        {
            var skyData = (SkyData)data;
            BackgroundColour = skyData.BackgroundColour.ToColourF();
            BackgroundImage = skyData.BackgroundImage;
            AmbientColour = skyData.AmbientColour.ToColourF();
        }

        private void Unload()
        {
        }
    }
}
