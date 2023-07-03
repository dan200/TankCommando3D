using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Math;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dan200.Core.GUI;
using Dan200.Core.Util;
using System.Linq;

namespace Dan200.Core.Render
{
    internal class Font : IBasicAsset
    {
        public static Font Get(string path)
        {
            return Assets.Assets.Get<Font>(path);
        }

        public static int AdvanceGlyph(string s, bool parseImages)
        {
            return AdvanceGlyph(s, 0, parseImages);
        }

        public static int AdvanceGlyph(string s, int start, bool parseImages)
        {
            return AdvanceGlyph(s, start, s.Length - start, parseImages);
        }

        public static int AdvanceGlyph(string s, int start, int length, bool parseImages)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (length == 0)
            {
                return 0;
            }

            var c = s[start];
            if (parseImages && c == '[')
            {
                int newPos = start + 1;
                while (newPos < start + length)
                {
                    if (s[newPos] == ']')
                    {
                        return (newPos + 1) - start; // Eat the image
                    }
                    else if (s[newPos] == ' ' || s[newPos] == '\n')
                    {
                        break; // Probably not a path, ignore the image.
                    }
                    ++newPos;
                }
                return 1; // Eat the bracket
            }
            else
            {
                if (!char.IsSurrogate(c))
                {
                    return 1; // Eat the char
                }
                else if (start + 1 < start + length && char.IsSurrogatePair(c, s[start + 1]))
                {
                    return 2; // Eat the pair
                }
                else
                {
                    return 1; // Eat the (invalid) char
                }
            }
        }

        public static int AdvanceWhitespace(string s)
        {
            return AdvanceWhitespace(s, 0);
        }

        public static int AdvanceWhitespace(string s, int start)
        {
            return AdvanceWhitespace(s, start, s.Length - start);
        }

        public static int AdvanceWhitespace(string s, int start, int length)
        {
			App.Assert(start >= 0 && length >= 0 && start + length <= s.Length);
            int pos = start;
            while (pos < start + length)
            {
                if (s[pos] == '\n')
                {
                    ++pos;
                    break;
                }
                else if (s[pos] == ' ' || s[pos] == '\t' || s[pos] == '\r')
                {
                    ++pos;
                }
                else
                {
                    break;
                }
            }
            return pos - start;
        }

        public static int AdvanceSentence(string s)
        {
            return AdvanceSentence(s, 0);
        }

        public static int AdvanceSentence(string s, int start)
        {
            return AdvanceSentence(s, start, s.Length - start);
        }

        public static int AdvanceSentence(string s, int start, int length)
        {
            if (start < 0 || length < 0 || start + length > s.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            int pos = start;
            char lastChar = '\0';
            while (pos < start + length)
            {
                var thisChar = s[pos];
                if (thisChar == '\n')
                {
                    break;
                }
                else if (thisChar == ' ')
                {
                    if (lastChar == '.' ||
                        lastChar == '!' ||
                        lastChar == '?' ||
                        lastChar == ':' ||
                        lastChar == (char)161 || // Inverted exclamation mark 
                        lastChar == (char)191 // Inverted question mark
                       )
                    {
                        break;
                    }
                }
                ++pos;
                lastChar = thisChar;
            }
            return pos - start;
        }

        private string m_path;
        private FNTFile m_file;
        private string[] m_texturePaths;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string TypeFace
        {
            get
            {
                return m_file.TypeFace;
            }
        }

        public int PageCount
        {
            get
            {
                return m_file.Pages.Length;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            var fntFile = new FNTFile();
            var reader = new StreamReader(stream, Encoding.UTF8);
            fntFile.Parse(reader);
            return fntFile;
        }

        public Font(string path, object data)
        {
            m_path = path;
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
            var file = (FNTFile)data;
            m_file = file;
            m_texturePaths = new string[m_file.Pages.Length];
            var dir = AssetPath.GetDirectoryName(m_path);
            for (uint i = 0; i < m_texturePaths.Length; ++i)
            {
                m_texturePaths[i] = AssetPath.Combine(dir, m_file.Pages[i].File);
            }
        }

        private void Unload()
        {
            m_file = null;
            m_texturePaths = null;
        }

        public Texture GetPageTexture(int page)
        {
            return Texture.Get(m_texturePaths[page], true);
        }

        public struct PositionedGlyph
        {
            public int TextStart;
            public int TextLength;

			public Vector2 Position;
			public Vector2 Size;

            public int PageTexture;
            public Texture ImageTexture;
			public Quad Region;
        }

        public IEnumerable<PositionedGlyph> EnumerateGlyphs(string s, int start, int length, int size, bool parseImages)
        {
            App.Assert(start >= 0 && length >= 0 && start + length <= s.Length);
            if (length > 0)
            {
                // Measure text
                float xPos = 0.0f;
                float yPos = 0.0f;
                float scale = (float)size / (float)m_file.Size;
                int previousDisplayChar = -1;
                int pos = start;
				int line = 0;
                while (pos < start + length)
                {
                    int glyphLength = AdvanceGlyph(s, pos, (start + length) - pos, parseImages);
                    if (glyphLength >= 2 && s[pos] == '[' && s[pos + glyphLength - 1] == ']')
                    {
                        // Emit image
                        var imagePath = s.Substring(pos + 1, glyphLength - 2);
                        var texture = Texture.Get(imagePath, true);
                        var imageHeight = (float)m_file.LineHeight * scale;
                        var imageWidth = imageHeight * ((float)texture.Width / (float)texture.Height);
                        var imageAdvance = imageWidth;

                        float expand = -0.05f;
                        float startX = (float)(xPos) - (float)imageWidth * expand;
                        float startY = (float)(yPos) - (float)imageHeight * expand;
                        float endX = (float)(xPos + imageWidth) + (float)imageWidth * expand;
                        float endY = (float)(yPos + imageHeight) + (float)imageHeight * expand;

                        var glyph = new PositionedGlyph();
                        glyph.TextStart = pos;
                        glyph.TextLength = glyphLength;
                        glyph.PageTexture = -1;
                        glyph.ImageTexture = texture;
						glyph.Position = new Vector2(startX, startY);
						glyph.Size = new Vector2(endX - startX, endY - startY);
						glyph.Region = Quad.UnitSquare;
                        yield return glyph;

                        xPos += imageAdvance;
                        previousDisplayChar = -1;
                    }
                    else
                    {
                        // Get character
						int codepoint;
                        if (glyphLength == 1)
                        {
                            codepoint = s[pos];
                        }
                        else if (glyphLength == 2)
                        {
                            codepoint = char.ConvertToUtf32(s[pos], s[pos + 1]);
                        }
                        else
                        {
							codepoint = UnicodeUtils.REPLACEMENT_CHARACTER;
                        }

						// Emit character
						switch (codepoint)
						{
							case '\r':
								{
									// Carriage return (will be followed by newline, ignore)
									break;
								}
							case '\n':
								{
									// Newline
									xPos = 0.0f;
									yPos += (float)m_file.LineHeight * scale;
									++line;
									previousDisplayChar = -1;
									break;
								}
							case '\t':
								{
									// Tab
									FNTFile.Char charData;
									if (m_file.Chars.TryGetValue((int)' ', out charData))
									{
										float spaceWidth = (float)charData.XAdvance * scale;
										float tabWidth = 4.0f * spaceWidth;
										if (tabWidth > 0.0)
										{
											xPos = (Mathf.Floor(xPos / tabWidth) + 1.0f) * tabWidth;
										}
										previousDisplayChar = (int)' ';
									}
									break;
								}
							default:
								{
									// Printable character
									int displayChar = codepoint;
									FNTFile.Char charData;
									if (!m_file.Chars.TryGetValue(displayChar, out charData))
									{
										displayChar = '?';
										if (!m_file.Chars.TryGetValue(displayChar, out charData))
										{
                                            App.Assert(m_file.Chars.Count > 0, "Font " + m_path + " has no characters!");
                                            displayChar = m_file.Chars.Keys.First();
                                            App.LogWarning("Font file " + m_path + " does not have a '?' character to use as a fallback. Using '" + char.ConvertFromUtf32(displayChar) + " instead.");
                                        }
                                    }
									if (m_file.Kernings != null && previousDisplayChar >= 0)
									{
										var pair = new FNTFile.KerningPair(previousDisplayChar, displayChar);
										FNTFile.Kerning kerning;
										if (m_file.Kernings.TryGetValue(pair, out kerning))
										{
											xPos += (float)kerning.Amount * scale;
										}
									}
									{
										if (xPos == 0.0f)
										{
											xPos = -(float)charData.XOffset * scale;
										}
										float startX = xPos + (float)charData.XOffset * scale;
										float startY = yPos + (float)charData.YOffset * scale;
										float endX = xPos + (float)(charData.XOffset + charData.Width) * scale;
										float endY = yPos + (float)(charData.YOffset + charData.Height) * scale;
										float texStartX = (float)(charData.X) / (float)m_file.ScaleW;
										float texStartY = (float)(charData.Y) / (float)m_file.ScaleH;
										float texWidth = (float)(charData.Width) / (float)m_file.ScaleW;
										float texHeight = (float)(charData.Height) / (float)m_file.ScaleH;

										var glyph = new PositionedGlyph();
										glyph.TextStart = pos;
										glyph.TextLength = glyphLength;
										glyph.PageTexture = charData.Page;
										glyph.ImageTexture = null;
										glyph.Position = new Vector2(startX, startY);
										glyph.Size = new Vector2(endX - startX, endY - startY);
										glyph.Region = new Quad(texStartX, texStartY, texWidth, texHeight);
										yield return glyph;

										xPos += (float)charData.XAdvance * scale;
										previousDisplayChar = displayChar;
									}
									break;
								}
						}
                    }
                    pos += glyphLength;
                }
            }
        }

        public float GetHeight(int size)
        {
            return (float)m_file.LineHeight * ((float)size / (float)m_file.Size);
        }

        public Vector2 Measure(string s, int fontSize, bool parseImages, float maxWidth = float.MaxValue)
        {
			return Measure(s, 0, s.Length, fontSize, parseImages, maxWidth);
        }

		public Vector2 Measure(string s, int start, int length, int fontSize, bool parseImages, float maxWidth = float.MaxValue)
        {
			App.Assert(start >= 0 && length >= 0 && start + length <= s.Length);
			float lineHeight = GetHeight(fontSize);
            float width = 0.0f;
			int pos = start;
			int line = 0;
			while (pos < (start + length))
			{
				int lineLength = WordWrap(s, pos, (start + length) - pos, fontSize, parseImages, maxWidth);
				foreach (var glyph in EnumerateGlyphs(s, pos, lineLength, fontSize, parseImages))
				{
					width = Mathf.Max(width, glyph.Position.X + glyph.Size.X);
				}
				pos += lineLength;
				pos += AdvanceWhitespace(s, pos, (start + length) - pos);
				line++;
			}
			float height = System.Math.Max(line, 1) * lineHeight;
            return new Vector2(width, height);
        }

        public int WordWrap(string s, int size, bool parseImages, float maxWidth)
        {
            return WordWrap(s, 0, s.Length, size, parseImages, maxWidth);
        }

        public int WordWrap(string s, int start, int length, int size, bool parseImages, float maxWidth)
        {
			App.Assert(start >= 0 && length >= 0 && start + length <= s.Length);
			var newlineIndex = s.IndexOf('\n', start, length);
			if (newlineIndex >= 0)
			{
				length = newlineIndex - start;
			}
			if (maxWidth < float.MaxValue)
			{
				var currentLineLen = 0;
				var currentWordLen = 0;
				foreach (var glyph in EnumerateGlyphs(s, start, length, size, parseImages))
				{
					if (glyph.TextLength == 1)
					{
						var c = s[glyph.TextStart];
						if (c == ' ')
						{
							currentLineLen += currentWordLen;
							currentWordLen = 0;
						}
					}
					if (glyph.Position.X + glyph.Size.X > maxWidth)
					{
						if (currentLineLen > 0)
						{
							return currentLineLen;
						}
						else if (currentWordLen > 0)
						{
							return currentWordLen;
						}
						else
						{
							return glyph.TextLength;
						}
					}
					currentWordLen += glyph.TextLength;
				}
			}
            return length;
        }

		public void Render(GUIBuilder builder, string s, int start, int length, Vector2 position, int fontSize, Colour colour, bool parseImages)
        {
            // Build the geometries
            foreach (var glyph in EnumerateGlyphs(s, start, length, fontSize, parseImages))
            {
				ITexture texture;
                if (glyph.ImageTexture != null)
                {
					texture = glyph.ImageTexture;
                }
                else
                {
					texture = GetPageTexture(glyph.PageTexture);
                }
				builder.AddQuad(
					position + glyph.Position,
					position + glyph.Position + glyph.Size,
					texture,
					glyph.Region,
					(glyph.ImageTexture != null) ? Colour.White : colour
				);
            }
        }
    }
}
