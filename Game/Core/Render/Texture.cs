using System;
using System.IO;
using Dan200.Core.Assets;
using Dan200.Core.Render.OpenGL;

#if GLES
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render
{
	internal abstract class Texture : ITexture, IBasicAsset
    {
        public static Texture Get(string path, bool filter)
        {
            var texture = Assets.Assets.Get<OpenGLTexture>(path);
            if (texture != null)
            {
                texture.Filter = false;//filter;
                texture.Wrap = true;
            }
            return texture;
        }

        public static Texture GetLocalised(string path, Language language, bool filter)
        {
            // Try the current language and it's fallbacks
            bool triedEnglish = false;
            while (language != null)
            {
                var specificPath = AssetPath.Combine(
                    AssetPath.GetDirectoryName(path),
                    AssetPath.GetFileNameWithoutExtension(path) + "_" + language.Code + ".png"
                );
                if (Assets.Assets.Exists<Texture>(specificPath))
                {
                    return Texture.Get(specificPath, filter);
                }
                if (language.Code == "en")
                {
                    triedEnglish = true;
                }
                language = language.Fallback;
            }
            if (!triedEnglish)
            {
                // Try english
                var englishPath = AssetPath.Combine(
                    AssetPath.GetDirectoryName(path),
                    AssetPath.GetFileNameWithoutExtension(path) + "_en.png"
                );
                if (Assets.Assets.Exists<Texture>(englishPath))
                {
                    return Texture.Get(englishPath, filter);
                }
            }

            // Try unlocalised
            return Texture.Get(path, filter);
        }

        public static Texture Black
        {
            get
            {
                return Texture.Get("black.png", false);
            }
        }

        public static Texture White
        {
            get
            {
                return Texture.Get("white.png", false);
            }
        }

        public static Texture Flat
        {
            get
            {
                return Texture.Get("flat.norm.png", false);
            }
        }

		public abstract string Path { get; }
		public abstract int Width { get; }
		public abstract int Height { get; }
		public abstract bool Filter { get; set; }
		public abstract bool Wrap { get; set; }

		public abstract void Reload(object data);
		public abstract void Dispose();
    }
}

