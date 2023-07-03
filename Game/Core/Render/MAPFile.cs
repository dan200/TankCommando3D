using Dan200.Core.Math;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Dan200.Core.Render
{
    internal class MAPFile
    {
        public struct Face
        {
            public Vector3 Pos0;
            public Vector3 Pos1;
            public Vector3 Pos2;
            public string TextureName;
            public UnitVector3 UAxis;
            public UnitVector3 VAxis;
            public Vector2 UVOffset;
            public float UVRotation;
            public Vector2 UVScale;
        }

        public struct Brush
        {
            public List<Face> Faces;
        }

        public struct Entity
        {
            public Dictionary<string, string> Properties;
            public List<Brush> Brushes;
        }

        public List<Entity> Entities;

        public MAPFile()
        {
            Entities = new List<Entity>();
        }

        public void Parse(Stream input)
        {
            var parser = new SimpleTextParser(input);
            while (true)
            {
                ByteString tokenText;
                var token = ReadToken(parser, out tokenText);
                switch (token)
                {
                    case Token.OpenCurly:
                        Entities.Add(ParseEntity(parser));
                        break;
                    case Token.EOF:
                        break;
                    default:
                        throw InvalidToken(parser, tokenText);
                }
                if(token == Token.EOF)
                {
                    break;
                }
            }
        }

        private Entity ParseEntity(SimpleTextParser parser)
        {
            var entity = new Entity();
            entity.Properties = new Dictionary<string, string>();
            entity.Brushes = new List<Brush>();

            // Read properties
            ByteString tokenText;
            var token = ReadToken(parser, out tokenText);
            while(token == Token.QuotedString)
            {
                ByteString valueTokenText;
                ExpectToken(parser, Token.QuotedString, out valueTokenText);

                var key = tokenText.Substring(1, tokenText.Length - 2).ToString();
                var value = valueTokenText.Substring(1, valueTokenText.Length - 2).ToString();
                entity.Properties[key] = value;

                token = ReadToken(parser, out tokenText);
            }

            // Read brushes
            while(token == Token.OpenCurly)
            {
                entity.Brushes.Add(ParseBrush(parser));
                token = ReadToken(parser, out tokenText);
            }

            // Finish
            if(token != Token.CloseCurly)
            {
                throw InvalidToken(parser, tokenText);
            }

            return entity;
        }

        private Brush ParseBrush(SimpleTextParser parser)
        {
            var brush = new Brush();
            brush.Faces = new List<Face>();

            // Read faces
            ByteString tokenText;
            var token = ReadToken(parser, out tokenText);
            while(token == Token.OpenParen)
            {
                Face face;
                face.Pos0 = ReadVector3(parser);

                ExpectToken(parser, Token.OpenParen, out tokenText);
                face.Pos1 = ReadVector3(parser);

                ExpectToken(parser, Token.OpenParen, out tokenText);
                face.Pos2 = ReadVector3(parser);

                ExpectToken(parser, Token.String, out tokenText);
                face.TextureName = tokenText.ToString();

                ExpectToken(parser, Token.OpenSquare, out tokenText);
                var uAxisOffset = ReadVector4(parser);

                ExpectToken(parser, Token.OpenSquare, out tokenText);
                var vAxisOffset = ReadVector4(parser);

                face.UAxis = uAxisOffset.XYZ.Normalise();
                face.VAxis = vAxisOffset.XYZ.Normalise();
                face.UVOffset.X = uAxisOffset.W;
                face.UVOffset.Y = vAxisOffset.W;

                ExpectToken(parser, Token.Number, out tokenText);
                face.UVRotation = float.Parse(tokenText.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

                ExpectToken(parser, Token.Number, out tokenText);
                face.UVScale.X = float.Parse(tokenText.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

                ExpectToken(parser, Token.Number, out tokenText);
                face.UVScale.Y = float.Parse(tokenText.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

                brush.Faces.Add(face);

                token = ReadToken(parser, out tokenText);
            }

            // Finish
            if (token != Token.CloseCurly)
            {
                throw InvalidToken(parser, tokenText);
            }

            return brush;
        }

        private Vector3 ReadVector3(SimpleTextParser parser)
        {
            ByteString text;
            float x, y, z;

            ExpectToken(parser, Token.Number, out text);
            x = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.Number, out text);
            y = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.Number, out text);
            z = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.CloseParen, out text);

            return new Vector3(x, y, z);
        }

        private Vector4 ReadVector4(SimpleTextParser parser)
        {
            ByteString text;
            float x, y, z, w;

            ExpectToken(parser, Token.Number, out text);
            x = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.Number, out text);
            y = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.Number, out text);
            z = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.Number, out text);
            w = float.Parse(text.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);

            ExpectToken(parser, Token.CloseSquare, out text);

            return new Vector4(x, y, z, w);
        }

        private enum Token
        {
            OpenParen,
            CloseParen,
            OpenCurly,
            CloseCurly,
            OpenSquare,
            CloseSquare,
            QuotedString,
            String,
            Number,
            EOF
        }

        private void ExpectToken(SimpleTextParser parser, Token token, out ByteString o_text)
        {
            if (ReadToken(parser, out o_text) != token)
            {
                throw InvalidToken(parser, o_text);
            }
        }

        private Token ReadToken(SimpleTextParser parser, out ByteString o_string)
        {
            // Skip BOM and whitespace
            parser.SkipBOM();
            parser.SkipWhitespace();

            // Parse a token
            // Parse
            if (parser.ReadNumber(out o_string))
            {
                return Token.Number;
            }
            else if (parser.ReadQuotedString(out o_string))
            {
                return Token.QuotedString;
            }
            else if (parser.ReadIdentifier(out o_string))
            {
                return Token.String;
            }
            else
            {
                var b = parser.ReadByte();
                if (b == -1)
                {
                    // EOF
                    o_string = ByteString.Intern("EOF");
                    return Token.EOF;
                }
                else if (b == '(')
                {
                    // (
                    o_string = ByteString.Intern("(");
                    return Token.OpenParen;
                }
                else if (b == ')')
                {
                    // )
                    o_string = ByteString.Intern(")");
                    return Token.CloseParen;
                }
                else if (b == '{')
                {
                    // {
                    o_string = ByteString.Intern("{");
                    return Token.OpenCurly;
                }
                else if (b == '}')
                {
                    // }
                    o_string = ByteString.Intern("}");
                    return Token.CloseCurly;
                }
                else if (b == '[')
                {
                    // [
                    o_string = ByteString.Intern("[");
                    return Token.OpenSquare;
                }
                else if (b == ']')
                {
                    // ]
                    o_string = ByteString.Intern("]");
                    return Token.CloseSquare;
                }
                else
                {
                    throw parser.InvalidByte(b);
                }
            }
        }

        private IOException InvalidToken(SimpleTextParser parser, ByteString tokenText)
        {
            return new IOException("Unexpected token on line " + parser.Line + ": " + tokenText.ToString());
        }
    }
}
