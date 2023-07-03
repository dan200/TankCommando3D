
using Dan200.Core.Assets;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Render
{
    internal class FNTFile
    {
        public string TypeFace;
        public int Size;

        public int LineHeight;
        public int Base;
        public int ScaleW;
        public int ScaleH;

        internal struct Page
        {
            public string File;
        }

        public Page[] Pages
        {
            get;
            private set;
        }

        internal struct Char
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int XOffset;
            public int YOffset;
            public int XAdvance;
            public int Page;
        }

        public Dictionary<int, Char> Chars
        {
            get;
            private set;
        }

        internal struct KerningPair : IEquatable<KerningPair>
        {
            public readonly int First;
            public readonly int Second;

            public KerningPair(int first, int second)
            {
                First = first;
                Second = second;
            }

            public override bool Equals(object other)
            {
                if (other is KerningPair)
                {
                    return Equals((KerningPair)other);
                }
                return false;
            }

            public bool Equals(KerningPair other)
            {
                return other.First == First && other.Second == Second;
            }

            public override int GetHashCode()
            {
                return First << 16 + Second;
            }
        }

        internal struct Kerning
        {
            public int Amount;
        }

        public Dictionary<KerningPair, Kerning> Kernings
        {
            get;
            private set;
        }

        public FNTFile()
        {
            Pages = null;
            Chars = null;
            Kernings = null;
        }

        public void Parse(TextReader reader)
        {
            // Load the data
            string line;
            string type = null;
			var stringOptions = new Dictionary<string, string>();
			var intOptions = new Dictionary<string, int>();
            while ((line = reader.ReadLine()) != null)
            {
                // Parse each line
                string[] parts = line.Split(' ');
                if (parts.Length < 1)
                {
                    continue;
                }

                // Extract type and options
                type = parts[0];
                stringOptions.Clear();
				intOptions.Clear();
                for (int i = 1; i < parts.Length; ++i)
                {
                    string part = parts[i];
                    int equalsIndex = part.IndexOf('=');
                    if (equalsIndex >= 0)
                    {
                        string key = part.Substring(0, equalsIndex);
                        string value = part.Substring(equalsIndex + 1);
                        int intValue;
                        if (value.StartsWith("\"", StringComparison.Ordinal))
                        {
                            if (value.EndsWith("\"", StringComparison.Ordinal) && value.Length >= 2)
                            {
                                value = value.Substring(1, value.Length - 2);
                            }
                            else
                            {
                                value = value.Substring(1) + " ";
                                i++;
                                while (!parts[i].EndsWith("\"", StringComparison.Ordinal))
                                {
                                    value += parts[i] + " ";
                                    i++;
                                }
                                value += parts[i].Substring(0, parts[i].Length - 1);
                            }
                            stringOptions[key] = value;
                        }
                        else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                        {
							intOptions[key] = intValue;
                        }
                    }
                }

                // Interpret
                switch (type)
                {
                    case "info":
                        {
                            TypeFace = stringOptions["face"];
                            Size = intOptions["size"];
                            break;
                        }
                    case "common":
                        {
                            LineHeight = intOptions["lineHeight"];
                            Base = intOptions["base"];
                            ScaleW = intOptions["scaleW"];
                            ScaleH = intOptions["scaleH"];

                            int pages = intOptions["pages"];
                            Pages = new Page[pages];
                            break;
                        }
                    case "page":
                        {
                            int id = intOptions["id"];
                            var page = new Page();
                            page.File = stringOptions["file"];
                            Pages[id] = page;
                            break;
                        }
                    case "chars":
                        {
                            var count = intOptions["count"];
                            Chars = new Dictionary<int, Char>(count);
                            break;
                        }
                    case "char":
                        {
                            var id = intOptions["id"];
                            var c = new Char();
                            c.X = intOptions["x"];
                            c.Y = intOptions["y"];
                            c.Width = intOptions["width"];
                            c.Height = intOptions["height"];
                            c.XOffset = intOptions["xoffset"];
                            c.YOffset = intOptions["yoffset"];
                            c.XAdvance = intOptions["xadvance"];
                            c.Page = intOptions["page"];
                            Chars[id] = c;
                            break;
                        }
                    case "kernings":
                        {
                            var count = intOptions["count"];
                            Kernings = new Dictionary<KerningPair, Kerning>(count, StructComparer<KerningPair>.Instance);
                            break;
                        }
                    case "kerning":
                        {
                            var first = intOptions["first"];
                            var second = intOptions["second"];
                            var pair = new KerningPair(first, second);
                            var kerning = new Kerning();
                            kerning.Amount = intOptions["amount"];
                            Kernings[pair] = kerning;
                            break;
                        }
                }
            }
        }
    }
}
