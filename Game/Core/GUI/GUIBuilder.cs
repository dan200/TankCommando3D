using System;
using System.Collections.Generic;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.GUI
{
	internal class GUIBuilder : IDisposable
	{
        private struct DrawCall
        {
            public ITexture Texture;
            public int IndexCount;
        }
        private List<DrawCall> m_drawCalls;

        private Geometry<ScreenVertex> m_geometry;
        private IRenderGeometry<ScreenVertex> m_renderGeometry;
		public Quad ClipRegion;

		public GUIBuilder()
		{
			m_geometry = new Geometry<ScreenVertex>(Primitive.Triangles);
            m_renderGeometry = null;
            m_drawCalls = new List<DrawCall>();
			ClipRegion = Quad.Zero;
		}

		public void Dispose()
		{
            if(m_renderGeometry != null)
            {
                m_renderGeometry.Dispose();
                m_renderGeometry = null;
            }
		}

		public void Clear()
		{
            m_geometry.Clear();
            m_drawCalls.Clear();
		}

		public void AddQuad(Vector2 topLeft, Vector2 bottomRight, ITexture texture)
		{
			AddQuad(topLeft, bottomRight, texture, Quad.UnitSquare, Colour.White);
		}

		public void AddQuad(Vector2 topLeft, Vector2 bottomRight, ITexture texture, Quad region)
		{
			AddQuad(topLeft, bottomRight, texture, region, Colour.White);
		}

		public void AddQuad(Vector2 topLeft, Vector2 bottomRight, ITexture texture, Quad region, Colour colour)
		{
			// Clip X
			float width = bottomRight.X - topLeft.X;
			float leftClip = (ClipRegion.X - topLeft.X) / width;
			float rightClip = (ClipRegion.X + ClipRegion.Width - topLeft.X) / width;
			if (leftClip >= 1.0f || rightClip <= 0.0f)
			{
				return;
			}
			else if (leftClip > 0.0f && rightClip < 1.0f)
			{
				topLeft.X = topLeft.X + leftClip * width;
				bottomRight.X = bottomRight.X - (1.0f - rightClip) * width;
				region.X = region.X + leftClip * region.Width;
				region.Width *= (1.0f - (rightClip - leftClip));
			}
			else if (leftClip > 0.0f)
			{
				topLeft.X = topLeft.X + leftClip * width;
				region.X = region.X + leftClip * region.Width;
				region.Width *= (1.0f - leftClip);
			}
			else if (rightClip < 1.0f)
			{
				bottomRight.X = bottomRight.X - (1.0f - rightClip) * width;
				region.Width *= rightClip;
			}

			// Clip Y
			float height = bottomRight.Y - topLeft.Y;
			float topClip = (ClipRegion.Y - topLeft.Y) / height;
			float bottomClip = (ClipRegion.Y + ClipRegion.Height - topLeft.Y) / height;
			if (topClip >= 1.0f || bottomClip <= 0.0f)
			{
				return;
			}
			else if (topClip > 0.0f && bottomClip < 1.0f)
			{
				topLeft.Y = topLeft.Y + topClip * height;
				bottomRight.Y = bottomRight.Y - (1.0f - bottomClip) * height;
				region.Y = region.Y + topClip * region.Height;
				region.Height *= (1.0f - (bottomClip - topClip));
			}
			else if (topClip > 0.0f)
			{
				topLeft.Y = topLeft.Y + topClip * height;
				region.Y = region.Y + topClip * region.Height;
				region.Height *= (1.0f - topClip);
			}
			else if (bottomClip < 1.0f)
			{
				bottomRight.Y = bottomRight.Y - (1.0f - bottomClip) * height;
				region.Height *= bottomClip;
			}

            // Add the quad
            var geometry = m_geometry;
			var firstVertex = geometry.VertexPos;
			geometry.AddVertex(topLeft, region.TopLeft, colour);
			geometry.AddVertex(new Vector2(bottomRight.X, topLeft.Y), region.TopRight, colour);
			geometry.AddVertex(new Vector2(topLeft.X, bottomRight.Y), region.BottomLeft, colour);
			geometry.AddVertex(bottomRight, region.BottomRight, colour);

			geometry.AddIndex(firstVertex + 2);
			geometry.AddIndex(firstVertex + 1);
			geometry.AddIndex(firstVertex);
			geometry.AddIndex(firstVertex + 1);
			geometry.AddIndex(firstVertex + 2);
			geometry.AddIndex(firstVertex + 3);

            // Add to the draw call list
            if(m_drawCalls.Count == 0 || m_drawCalls.Last().Texture != texture )
            {
                // New call
                var call = new DrawCall();
                call.Texture = texture;
                call.IndexCount = 6;
                m_drawCalls.Add(call);
            }
            else
            {
                // Extend previous call
                var call = m_drawCalls.Last();
                call.IndexCount += 6;
                m_drawCalls[m_drawCalls.Count - 1] = call;
            }
        }

        public void AddNineSlice(Vector2 topLeft, Vector2 bottomRight, float leftMargin, float topMargin, float rightMargin, float bottomMargin, ITexture texture)
		{
			AddNineSlice(topLeft, bottomRight, leftMargin, topMargin, rightMargin, bottomMargin, texture, Colour.White);
		}

		public void AddNineSlice(Vector2 topLeft, Vector2 bottomRight, float leftMargin, float topMargin, float rightMargin, float bottomMargin, ITexture texture, Colour colour)
		{
			// Clip
			if (bottomRight.X <= ClipRegion.X ||
			   topLeft.X >= ClipRegion.X + ClipRegion.Width ||
			   bottomRight.Y <= ClipRegion.Y ||
			   topLeft.Y >= ClipRegion.Y + ClipRegion.Height)
			{
				return;
			}

			var topRight = new Vector2(bottomRight.X, topLeft.Y);
			var bottomLeft = new Vector2(topLeft.X, bottomRight.Y);

			// Top
			AddQuad(topLeft, topLeft + new Vector2(leftMargin, topMargin), texture, new Quad(0.0f, 0.0f, 0.25f, 0.25f), colour);
			AddQuad(topLeft + new Vector2(leftMargin, 0.0f), topRight + new Vector2(-rightMargin, topMargin), texture, new Quad(0.25f, 0.0f, 0.5f, 0.25f), colour);
            AddQuad(topRight + new Vector2(-rightMargin, 0.0f), topRight + new Vector2(0.0f, topMargin), texture, new Quad(0.75f, 0.0f, 0.25f, 0.25f), colour);

            // Middle
            AddQuad(topLeft + new Vector2(0.0f, topMargin), bottomLeft + new Vector2(leftMargin, -bottomMargin), texture, new Quad(0.0f, 0.25f, 0.25f, 0.5f), colour);
            AddQuad(topLeft + new Vector2(leftMargin, topMargin), bottomRight + new Vector2(-rightMargin, -bottomMargin), texture, new Quad(0.25f, 0.25f, 0.5f, 0.5f), colour);
            AddQuad(topRight + new Vector2(-rightMargin, topMargin), bottomRight + new Vector2(0.0f, -bottomMargin), texture, new Quad(0.75f, 0.25f, 0.25f, 0.5f), colour);

            // Bottom
            AddQuad(bottomLeft + new Vector2(0.0f, -bottomMargin), bottomLeft + new Vector2(leftMargin, 0.0f), texture, new Quad(0.0f, 0.75f, 0.25f, 0.25f), colour);
            AddQuad(bottomLeft + new Vector2(leftMargin, -bottomMargin), bottomRight + new Vector2(-rightMargin, 0.0f), texture, new Quad(0.25f, 0.75f, 0.5f, 0.25f), colour);
            AddQuad(bottomRight + new Vector2(-rightMargin, -bottomMargin), bottomRight, texture, new Quad(0.75f, 0.75f, 0.25f, 0.25f), colour);
		}

		public void AddThreeSlice(Vector2 topLeft, Vector2 bottomRight, float leftMargin, float rightMargin, ITexture texture)
		{
			AddThreeSlice(topLeft, bottomRight, leftMargin, rightMargin, texture, Colour.White);
		}

		public void AddThreeSlice(Vector2 topLeft, Vector2 bottomRight, float leftMargin, float rightMargin, ITexture texture, Colour colour)
		{
			// Clip
			if (bottomRight.X <= ClipRegion.X ||
			   topLeft.X >= ClipRegion.X + ClipRegion.Width ||
			   bottomRight.Y <= ClipRegion.Y ||
			   topLeft.Y >= ClipRegion.Y + ClipRegion.Height)
			{
				return;
			}

			var height = bottomRight.Y - topLeft.Y;

			// Middle
			AddQuad(topLeft, topLeft + new Vector2(leftMargin, height), texture, new Quad(0.0f, 0.0f, 0.25f, 1.0f), colour);
			AddQuad(topLeft + new Vector2(leftMargin, 0.0f), bottomRight, texture, new Quad(0.25f, 0.0f, 0.5f, 1.0f), colour);
			AddQuad(bottomRight - new Vector2(rightMargin, height), bottomRight, texture, new Quad(0.75f, 0.0f, 0.25f, 1.0f), colour);
		}

		public void AddText(string text, Vector2 position, Font font, int fontSize, Colour colour, TextAlignment alignment = TextAlignment.Left, bool parseImages = false, float maxWidth = float.MaxValue)
		{
			// Render
			int pos = 0;
			int line = 0;
			while (pos < text.Length)
			{
				int lineLength = font.WordWrap(text, pos, text.Length - pos, fontSize, parseImages, maxWidth);
				float yPos = position.Y + (float)line * font.GetHeight(fontSize);
				float xPos;
				switch (alignment)
				{
				case TextAlignment.Left:
				default:
					xPos = position.X;
					break;
				case TextAlignment.Right:
					xPos = position.X - font.Measure(text, pos, lineLength, fontSize, parseImages).X;
					break;
				case TextAlignment.Center:
					xPos = position.X - 0.5f * font.Measure(text, pos, lineLength, fontSize, parseImages).X;
					break;
				}
				font.Render(this, text, pos, lineLength, new Vector2(xPos, yPos), fontSize, colour, parseImages);
				pos += lineLength;
				pos += Font.AdvanceWhitespace(text, pos);
				line++;
			}
		}

        public void Upload(IRenderer renderer)
		{
            if(m_renderGeometry == null)
            {
                m_renderGeometry = renderer.Upload(m_geometry, RenderGeometryFlags.Dynamic);
            }
            else
            {
                m_renderGeometry.Update(m_geometry);
            }
		}

		public void Draw(IRenderer renderer, ScreenEffectHelper effect)
		{
            if (m_renderGeometry != null)
            {
                int index = 0;
                foreach (var call in m_drawCalls)
                {
                    effect.Texture = call.Texture;
                    renderer.DrawRange(m_renderGeometry, index, call.IndexCount);
                    index += call.IndexCount;
                }
            }
		}
	}
}
