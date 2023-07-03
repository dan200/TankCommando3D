using System;
using System.Collections.Generic;
using Dan200.Core.Assets;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Platform;
using Dan200.Core.Render;

namespace Dan200.Core.GUI
{
    internal static class DebugGUI
    {
        public static float DEBUG_GUI_EXPECTED_SCREEN_HEIGHT = 1080.0f;
        public static float DEBUG_GUI_SCALE = (float)Game.Game.Game.TARGET_SCREEN_HEIGHT / DEBUG_GUI_EXPECTED_SCREEN_HEIGHT;
    }

    internal struct DebugGUITheme
    {
        public static DebugGUITheme Default
        {
            get
            {
                var theme = new DebugGUITheme();
                theme.Font = Font.Get("fonts/Arial64.fnt");
                theme.FontSize = (int)(16.0f * DebugGUI.DEBUG_GUI_SCALE);
                theme.BackgroundColour = new Colour(228, 229, 233);
                theme.HighlightColour = new Colour(177, 205, 237);
                theme.HoverColour = new Colour(119, 173, 238);
                theme.TextColour = Colour.Black;
                theme.BoxColour = Colour.White;
                theme.Margin = 6.0f * DebugGUI.DEBUG_GUI_SCALE;
                theme.Spacing = 4.0f * DebugGUI.DEBUG_GUI_SCALE;
                theme.Indent = 6.0f * DebugGUI.DEBUG_GUI_SCALE;
                return theme;
            }
        }

        public Font Font;
        public int FontSize;
        public Colour BackgroundColour;
        public Colour HighlightColour;
        public Colour HoverColour;
        public Colour TextColour;
        public Colour BoxColour;
        public float Margin;
        public float Spacing;
        public float Indent;
    }

    internal class DebugGUIState
    {
        public string SelectedID;
        public string UncommitedText;
        public int TextSelectionStart;
        public int TextSelectionLength;
        public bool IsFirstClick;
        public readonly HashSet<string> OpenSectionIDs;

        public DebugGUIState()
        {
            SelectedID = null;
            UncommitedText = null;
            TextSelectionStart = 0;
            TextSelectionLength = 0;
            IsFirstClick = false;
            OpenSectionIDs = new HashSet<string>();
        }
    }

    internal struct DebugGUIBuilder
    {
        private GUIBuilder m_builder;
        public DebugGUITheme Theme;
        private DebugGUIState m_state;

        private Screen m_screen;
        private Quad m_area;
        private Vector2 m_position;
        private string m_path;
        private float m_indent;

        private float[] m_columnWidths;
        private int m_numColumns;
        private int m_column;
        private float m_rowHeight;

        public DebugGUIBuilder(GUIBuilder builder, Screen screen, Quad area, in DebugGUITheme theme, DebugGUIState state)
        {
            m_builder = builder;
            m_screen = screen;
            m_area = area;
            Theme = theme;
            m_state = state;

            m_position = area.TopLeft;
            m_path = "";
            m_indent = 0.0f;

            m_numColumns = 1;
            m_columnWidths = new float[4];
            m_columnWidths[0] = area.Width;
            m_column = 0;
            m_rowHeight = 0.0f;
        }

        public void Columns(int count)
        {
            App.Assert(m_column == 0);
            App.Assert(count >= 1);

            m_numColumns = count;
            if (m_columnWidths.Length < m_numColumns)
            {
                Array.Resize(ref m_columnWidths, m_numColumns);
            }
            for (int i = 0; i < m_numColumns; ++i)
            {
                m_columnWidths[i] = 0.0f;
            }
            RescaleColumns();
        }

        public void Columns(float width0)
        {
            m_numColumns = 1;
            m_columnWidths[0] = width0;
            RescaleColumns();
        }

        public void Columns(float width0, float width1)
        {
            m_numColumns = 2;
            m_columnWidths[0] = width0;
            m_columnWidths[1] = width1;
            RescaleColumns();
        }

        public void Columns(float width0, float width1, float width2)
        {
            m_numColumns = 3;
            m_columnWidths[0] = width0;
            m_columnWidths[1] = width1;
            m_columnWidths[2] = width2;
            RescaleColumns();
        }

        public void Columns(float width0, float width1, float width2, float width3)
        {
            m_numColumns = 4;
            m_columnWidths[0] = width0;
            m_columnWidths[1] = width1;
            m_columnWidths[2] = width2;
            m_columnWidths[3] = width3;
            RescaleColumns();
        }

        public void Columns(params float[] widths)
        {
            App.Assert(m_column == 0);
            App.Assert(widths.Length > 0);

            m_numColumns = widths.Length;
            if (m_columnWidths.Length < m_numColumns)
            {
                Array.Resize(ref m_columnWidths, m_numColumns);
            }
            for (int i = 0; i < m_numColumns; ++i)
            {
                m_columnWidths[i] = widths[i];
            }
            RescaleColumns();
        }

        private void NextColumn()
        {
            m_position.X += Mathf.Abs(m_columnWidths[m_column]) + Theme.Spacing;
            m_column++;
            if (m_column >= m_numColumns)
            {
                EndRow();
            }
        }

        public void EndRow()
        {
            if(m_column > 0)
            {
                m_position.X = m_area.X + m_indent;
                m_position.Y += m_rowHeight + Theme.Spacing;
                m_column = 0;
                m_rowHeight = 0.0f;
            }
        }

        public bool BeginSection(string name)
        {
            var id = AssetPath.Combine(m_path, name);
            bool open = m_state.OpenSectionIDs.Contains(id);
            if (Button(name))
            {
                if (open)
                {
                    m_state.OpenSectionIDs.Remove(id);
                    open = false;
                }
                else
                {
                    m_state.OpenSectionIDs.Add(id);
                    open = true;
                }
            }
            if (open)
            {
                m_path = id;
                return true;
            }
            return false;
        }

        public void EndSection()
        {
            m_path = AssetPath.GetDirectoryName(m_path);
        }

        public void Indent()
        {
            App.Assert(m_column == 0);
            m_indent += Theme.Indent;
            m_position.X += Theme.Indent;
            RescaleColumns();
        }

        public void Outdent()
        {
            App.Assert(m_column == 0);
            m_indent -= Theme.Indent;
            m_position.X -= Theme.Indent;
            RescaleColumns();
        }

        private void RescaleColumns()
        {
            var availableWidth = m_area.Width - m_indent - (m_numColumns - 1) * Theme.Spacing;
            var usedWidth = 0.0f;
            var numDynamicColumns = 0;
            for (int i = 0; i < m_numColumns; ++i)
            {
                if (m_columnWidths[i] > 0.0f)
                {
                    usedWidth += m_columnWidths[i];
                }
                else
                {
                    numDynamicColumns++;
                }
            }

            if (numDynamicColumns == 0 || usedWidth > availableWidth)
            {
                var squish = availableWidth / usedWidth;
                for (int i = 0; i < m_numColumns; ++i)
                {
                    if (m_columnWidths[i] > 0.0f)
                    {
                        m_columnWidths[i] *= squish;
                    }
                    else
                    {
                        m_columnWidths[i] = 0.0f;
                    }
                }
            }
            else if (usedWidth < availableWidth)
            {
                var dynamicWidth = (availableWidth - usedWidth) / (float)numDynamicColumns;
                for (int i = 0; i < m_numColumns; ++i)
                {
                    if (m_columnWidths[i] <= 0.0f)
                    {
                        m_columnWidths[i] = -dynamicWidth;
                    }
                }
            }
        }

        public void Label(string format, params object[] args)
        {
            var text = string.Format(format, args);
            var size = Theme.Font.Measure(text, Theme.FontSize, false);
            m_builder.AddText(text, m_position, Theme.Font, Theme.FontSize, Theme.TextColour);

            m_rowHeight = Mathf.Max(m_rowHeight, size.Y);
            NextColumn();
        }

        public bool Button(string text)
        {
            var result = false;

            var columnWidth = Mathf.Abs(m_columnWidths[m_column]);
            var size = new Vector2(
                columnWidth,
                Theme.Font.GetHeight(Theme.FontSize) + 2.0f * Theme.Spacing
            );

            bool hover = false;
            var mouse = m_screen.InputDevices.Mouse;
            if (mouse != null &&
                m_area.Contains(m_screen.MousePosition) &&
                new Quad(m_position, size).Contains(m_screen.MousePosition))
            {
                hover = true;
                if (mouse.GetInput(MouseButton.Left).Pressed)
                {
                    result = true;
                }
            }

            DrawBox(m_position, size, hover ? Theme.HoverColour : Theme.HighlightColour);
            DrawText(m_position, size, text, TextAlignment.Center, Theme.TextColour);

            m_rowHeight = Mathf.Max(m_rowHeight, size.Y);
            NextColumn();
            return result;
        }

        public bool Checkbox(string label, ref bool io_bool)
        {
            var changed = false;
            var boxText = io_bool ? "X" : "";

            var columnWidth = Mathf.Abs(m_columnWidths[m_column]);
            var labelSize = new Vector2(
                (columnWidth - Theme.Spacing) * 0.5f,
                Theme.Font.GetHeight(Theme.FontSize) + 2.0f * Theme.Spacing
            );
            var boxSize = new Vector2(
                labelSize.Y,
                labelSize.Y
            );

            var boxPos = m_position + new Vector2(labelSize.X + Theme.Spacing, 0.0f);
            DrawText(m_position, labelSize, label, TextAlignment.Left, Theme.TextColour);
            DrawBox(boxPos, boxSize, Theme.BoxColour);
            DrawText(boxPos, boxSize, boxText, TextAlignment.Center, Theme.TextColour);

            var mouse = m_screen.InputDevices.Mouse;
            if (mouse != null &&
                m_area.Contains(m_screen.MousePosition) &&
                new Quad(boxPos, boxSize).Contains(m_screen.MousePosition) &&
                mouse.GetInput(MouseButton.Left).Pressed)
            {
                io_bool = !io_bool;
                changed = true;
            }

            m_rowHeight = Mathf.Max(m_rowHeight, labelSize.Y);
            NextColumn();
            return changed;
        }

        private float GetCursorPosition(string text, int pos)
        {
            App.Assert(pos >= 0 && pos <= text.Length);

            float previousEnd = 0.0f;
            foreach (var glyph in Theme.Font.EnumerateGlyphs(text, 0, text.Length, Theme.FontSize, false))
            {
                float start = glyph.Position.X;
                float end = glyph.Position.X + glyph.Size.X;
                if (pos >= glyph.TextStart && pos < glyph.TextStart + glyph.TextLength)
                {
                    return start;
                }
                previousEnd = end;
            }
            return previousEnd;
        }

        private int GetNearestCursorPosition(string text, float xPos)
        {
            float closestDistance = float.MaxValue;
            int closestIndex = 0;

            float previousEnd = 0.0f;
            foreach(var glyph in Theme.Font.EnumerateGlyphs(text, 0, text.Length, Theme.FontSize, false))
            {
                float start = glyph.Position.X;
                float end = glyph.Position.X + glyph.Size.X;
                float distance = Mathf.Abs(xPos - start);
                if( distance <= closestDistance )
                {
                    closestIndex = glyph.TextStart;
                    closestDistance = distance;
                }
                else
                {
                    // We've passed the cursor and started getting further away again
                    return closestIndex;
                }
                previousEnd = end;
            }

            float endDistance = Mathf.Abs(xPos - previousEnd);
            if(endDistance <= closestDistance)
            {
                closestIndex = text.Length;
            }
            return closestIndex;
        }

        public bool Textbox(string label, ref string io_text, Func<string, bool> filter = null)
        {
            var changed = false;
            var id = AssetPath.Combine(m_path, label);

            var columnWidth = Mathf.Abs(m_columnWidths[m_column]);
            var textHeight = Theme.Font.GetHeight(Theme.FontSize);
            var size = new Vector2(
                (columnWidth - Theme.Spacing) * 0.5f,
                textHeight + 2.0f * Theme.Spacing
            );
            var textPos = m_position + new Vector2(size.X + Theme.Spacing, 0.0f);

            string text;
            if (m_state.SelectedID == id)
            {
                text = m_state.UncommitedText;
            }
            else
            {
                text = (io_text != null) ? io_text : "";
            }
           
            var mouse = m_screen.InputDevices.Mouse;
            if (mouse != null)
            {
                var input = mouse.GetInput(MouseButton.Left);
                if (input.Pressed)
                {
                    if (m_area.Contains(m_screen.MousePosition) &&
                        new Quad(textPos, size).Contains(m_screen.MousePosition))
                    {
                        if (m_state.SelectedID != id)
                        {
                            m_state.SelectedID = id;
                            m_state.UncommitedText = text;
                            m_state.IsFirstClick = true;
                        }

                        float x = m_screen.MousePosition.X - (textPos.X + Theme.Spacing);
                        m_state.TextSelectionStart = GetNearestCursorPosition(text, x);
                        m_state.TextSelectionLength = 0;
                    }
                    else if (m_state.SelectedID == id)
                    {
                        m_state.SelectedID = null;
                    }
                }
                else if(input.Held)
                {
                    if(m_state.SelectedID == id)
                    {
                        float x = m_screen.MousePosition.X - (textPos.X + Theme.Spacing);
                        m_state.TextSelectionLength = GetNearestCursorPosition(text, x) - m_state.TextSelectionStart;
                    }
                }
                else if(input.Released)
                {
                    if (m_state.SelectedID == id)
                    {
                        if (m_state.TextSelectionLength == 0 && m_state.IsFirstClick)
                        {
                            m_state.TextSelectionStart = 0;
                            m_state.TextSelectionLength = text.Length;
                        }
                        m_state.IsFirstClick = false;
                    }
                }
            }

            DrawText(m_position, size, label, TextAlignment.Left, Theme.TextColour);
            DrawBox(textPos, size, Theme.BoxColour);
            if (m_state.SelectedID == id)
            {
                if (m_state.TextSelectionLength == 0)
                {
                    float xPos = GetCursorPosition(text, m_state.TextSelectionStart);
                    var width = Theme.Font.Measure("|", Theme.FontSize, false).X;
                    DrawText(
                        textPos + new Vector2(Theme.Spacing + xPos - 50.0f, Theme.Spacing), // TODO UNHARDCODE
                        new Vector2(100.0f, textHeight),
                        "|",
                        TextAlignment.Center,
                        Theme.TextColour
                    );
                }
                else
                {
                    float xStart = GetCursorPosition(text, m_state.TextSelectionStart);
                    float xEnd = GetCursorPosition(text, m_state.TextSelectionStart + m_state.TextSelectionLength);
                    m_builder.AddQuad(
                        textPos + new Vector2(Theme.Spacing + Mathf.Min(xStart, xEnd), Theme.Spacing),
                        textPos + new Vector2(Theme.Spacing + Mathf.Max(xStart, xEnd), Theme.Spacing + textHeight),
                        Texture.White,
                        Quad.UnitSquare,
                        Theme.HighlightColour
                    );
                }
            }
            DrawText(
                textPos + new Vector2(Theme.Spacing, 0.0f),
                size - new Vector2(2.0f * Theme.Spacing, 0.0f),
                text,
                TextAlignment.Left,
                Theme.TextColour
            );

            if (m_state.SelectedID == id)
            {
                var keyboard = m_screen.InputDevices.Keyboard;
                if (keyboard != null)
                {
                    if (keyboard.GetInput(Key.Left).Pressed)
                    {
                        if (keyboard.GetInput(Key.LeftShift).Held || keyboard.GetInput(Key.RightShift).Held)
                        {
                            // Grow the selection
                            if(m_state.TextSelectionStart + m_state.TextSelectionLength > 0)
                            {
                                m_state.TextSelectionLength--;
                            }
                        }
                        else
                        {
                            // Move the cursor
                            if (m_state.TextSelectionLength > 0)
                            {
                                m_state.TextSelectionLength = 0;
                            }
                            else if(m_state.TextSelectionLength < 0)
                            {
                                m_state.TextSelectionStart += m_state.TextSelectionLength;
                                m_state.TextSelectionLength = 0;
                            }
                            else if (m_state.TextSelectionStart > 0)
                            {
                                m_state.TextSelectionStart--;
                            }
                        }
                    }

                    if (keyboard.GetInput(Key.Right).Pressed)
                    {
                        if (keyboard.GetInput(Key.LeftShift).Held || keyboard.GetInput(Key.RightShift).Held)
                        {
                            // Grow the selection
                            if (m_state.TextSelectionStart + m_state.TextSelectionLength < m_state.UncommitedText.Length)
                            {
                                m_state.TextSelectionLength++;
                            }
                        }
                        else
                        {
                            // Move the cursor
                            if (m_state.TextSelectionLength > 0)
                            {
                                m_state.TextSelectionStart += m_state.TextSelectionLength;
                                m_state.TextSelectionLength = 0;
                            }
                            else if(m_state.TextSelectionLength < 0)
                            {
                                m_state.TextSelectionLength = 0;
                            }
                            else if (m_state.TextSelectionStart < m_state.UncommitedText.Length)
                            {
                                m_state.TextSelectionStart++;
                            }
                        }
                    }

                    foreach (var c in keyboard.Text)
                    {
                        int beforeEnd, afterStart;
                        if (m_state.TextSelectionLength >= 0)
                        {
                            beforeEnd = m_state.TextSelectionStart;
                            afterStart = m_state.TextSelectionStart + m_state.TextSelectionLength;
                        }
                        else
                        {
                            beforeEnd = m_state.TextSelectionStart + m_state.TextSelectionLength;
                            afterStart = m_state.TextSelectionStart;
                        }
                        if (c == '\n')
                        {
                            m_state.SelectedID = null;
                            break;
                        }
                        else if (c == '\b')
                        {
                            if (m_state.TextSelectionLength != 0)
                            {
                                m_state.UncommitedText = m_state.UncommitedText.Substring(0, beforeEnd) + m_state.UncommitedText.Substring(afterStart);
                                m_state.TextSelectionStart = beforeEnd;
                                m_state.TextSelectionLength = 0;
                                changed = true;
                            }
                            else if (beforeEnd > 0)
                            {
                                m_state.UncommitedText = m_state.UncommitedText.Substring(0, beforeEnd - 1) + m_state.UncommitedText.Substring(afterStart);
                                m_state.TextSelectionStart = beforeEnd - 1;
                                m_state.TextSelectionLength = 0;
                                changed = true;
                            }
                        }
                        else
                        {
                            m_state.UncommitedText = m_state.UncommitedText.Substring(0, beforeEnd) + c + m_state.UncommitedText.Substring(afterStart);
                            m_state.TextSelectionStart = beforeEnd + 1;
                            m_state.TextSelectionLength = 0;
                            changed = true;
                        }
                    }

                    if (keyboard.GetInput(Key.C).Pressed)
                    {
                        if ((App.Platform.Type == PlatformType.MacOS) ?
                            (keyboard.GetInput(Key.LeftGUI).Held || keyboard.GetInput(Key.RightGUI).Held) :
                            (keyboard.GetInput(Key.LeftCtrl).Held || keyboard.GetInput(Key.RightCtrl).Held))
                        {
                            if(m_state.TextSelectionLength != 0)
                            {
                                var selectedText = (m_state.TextSelectionLength >= 0) ?
                                    m_state.UncommitedText.Substring(m_state.TextSelectionStart, m_state.TextSelectionLength) :
                                    m_state.UncommitedText.Substring(m_state.TextSelectionStart + m_state.TextSelectionLength, -m_state.TextSelectionLength);
                                keyboard.SetClipboardText(selectedText);
                            }
                        }
                    }

                    if (changed)
                    {
                        if (filter == null || filter.Invoke(m_state.UncommitedText))
                        {
                            io_text = m_state.UncommitedText;
                        }
                        else
                        {
                            changed = false;
                        }
                    }
                }
            }

            m_rowHeight = Mathf.Max(m_rowHeight, size.Y);
            NextColumn();
            return changed;
        }

        public bool Int(string label, ref int io_number, int min = int.MinValue, int max = int.MaxValue)
        {
            App.Assert(max >= min);
            string numberAsString = io_number.ToString();
            if (Textbox(label, ref numberAsString, delegate (string s) {
                if (s.Length > 0)
                {
                    int n;
                    return int.TryParse(s, out n) && n >= min && n <= max;
                }
                else
                {
                    return true;
                }
            }))
            {
                if (numberAsString.Length > 0)
                {
                    io_number = int.Parse(numberAsString);
                }
                else
                {
                    io_number = System.Math.Min(System.Math.Max(0, min), max);
                }
                return true;
            }
            return false;
        }

        public bool Float(string label, ref float io_number, float min = float.MinValue, float max = float.MaxValue)
        {
            App.Assert(max >= min);
            string numberAsString = io_number.ToString();
            if (Textbox(label, ref numberAsString, delegate (string s) {
                if (s.Length > 0)
                {
                    float n;
                    return float.TryParse(s, out n) && n >= min && n <= max;
                }
                else
                {
                    return true;
                }
            }))
            {
                if (numberAsString.Length > 0)
                {
                    io_number = float.Parse(numberAsString);
                }
                else
                {
                    io_number = Mathf.Clamp(0.0f, min, max);
                }
                return true;
            }
            return false;
        }

        public bool FloatSlider(string label, ref float io_number, float min, float max, float rounding=0.0f, string format="N2")
        {
            App.Assert(max >= min);
            bool changed = false;

            var columnWidth = Mathf.Abs(m_columnWidths[m_column]);
            var size = new Vector2(
                (columnWidth - Theme.Spacing) * 0.5f,
                Theme.Font.GetHeight(Theme.FontSize) + 2.0f * Theme.Spacing
            );
            var sliderPosition = new Vector2(
                m_position.X + size.X + Theme.Spacing,
                m_position.Y
            );

            var mouse = m_screen.InputDevices.Mouse;
            if (mouse != null)
            {
                var id = AssetPath.Combine(m_path, label);
                if (m_area.Contains(m_screen.MousePosition) &&
                    new Quad(sliderPosition, size).Contains(m_screen.MousePosition) &&
                    mouse.GetInput(MouseButton.Left).Pressed)
                {
                    m_state.SelectedID = id;
                }
                if(m_state.SelectedID == id)
                {
                    if(mouse.GetInput(MouseButton.Left).Held)
                    {
                        var mouseFraction = Mathf.Saturate((m_screen.MousePosition.X - sliderPosition.X) / size.X);
                        var newNumber = min + (max - min) * mouseFraction;
                        if(rounding != 0.0f)
                        {
                            newNumber = Mathf.Round(newNumber, rounding);
                        }
                        if(newNumber != io_number)
                        {
                            io_number = newNumber;
                            changed = true;
                        }
                    }
                    else
                    {
                        m_state.SelectedID = null;
                    }
                }
            }

            DrawText(m_position, size, label, TextAlignment.Left, Theme.TextColour);
            DrawBox(sliderPosition, size, Theme.BoxColour);
            var oldClip = m_builder.ClipRegion;
            try
            {
                var fraction = (max > min) ? Mathf.Saturate((io_number - min) / (max - min)) : 0.0f;
                m_builder.ClipRegion = new Quad(sliderPosition, size.X * fraction, size.Y);
                DrawBox(sliderPosition, size, Theme.HighlightColour);
            }
            finally
            {
                m_builder.ClipRegion = oldClip;
            }
            DrawText(sliderPosition, size, io_number.ToString(format), TextAlignment.Center, Theme.TextColour);

            m_rowHeight = Mathf.Max(m_rowHeight, size.Y);
            NextColumn();
            return changed;
        }

        public bool IntSlider(string label, ref int io_number, int min, int max)
        {
            float floatNumber = (float)io_number;
            if(FloatSlider(label, ref floatNumber, (float)min, (float)max, 1.0f, "N0"))
            {
                io_number = (int)floatNumber;
                return true;
            }
            return false;
        }

        public bool DropDown<T>(string label, ref T io_value, T[] options)
        {
            var columnWidth = Mathf.Abs(m_columnWidths[m_column]);
            var fontHeight = Theme.Font.GetHeight(Theme.FontSize);
            var size = new Vector2(
                (columnWidth - Theme.Spacing) * 0.5f,
                fontHeight + 2.0f * Theme.Spacing
            );
            var boxPosition = new Vector2(
                m_position.X + size.X + Theme.Spacing,
                m_position.Y
            );
            var arrowSize = new Vector2(size.Y, size.Y);
            var arrowPosition = new Vector2(boxPosition.X + size.X - arrowSize.X, boxPosition.Y);

            DrawText(m_position, size, label, TextAlignment.Left, Theme.TextColour);

            var id = AssetPath.Combine(m_path, label);
            var mouse = m_screen.InputDevices.Mouse;
            if (mouse != null &&
                m_area.Contains(m_screen.MousePosition) &&
                new Quad(boxPosition, size).Contains(m_screen.MousePosition) &&
                mouse.GetInput(MouseButton.Left).Pressed)
            {
                if(m_state.SelectedID == id)
                {
                    m_state.SelectedID = null;
                }
                else
                {
                    m_state.SelectedID = id;
                }
            }

            bool open = (m_state.SelectedID == id);
            DrawBox(boxPosition, size, Theme.BoxColour);
            DrawText(boxPosition + new Vector2(Theme.Spacing, 0.0f), size - new Vector2(arrowSize.X + 2.0f * Theme.Spacing, 0.0f), io_value.ToString(), TextAlignment.Left, Theme.TextColour);
            DrawBox(arrowPosition, arrowSize, Theme.HighlightColour);
            DrawText(arrowPosition, arrowSize, open ? "-" : "+", TextAlignment.Center, Theme.TextColour);

            bool changed = false;
            if (open)
            {
                var dropDownPosition = boxPosition + new Vector2(0.0f, size.Y);
                var dropDownSize = new Vector2(
                    size.X,
                    (fontHeight + Theme.Spacing) * options.Length + Theme.Spacing
                );
                DrawBox(dropDownPosition, dropDownSize, Theme.BoxColour);

                for (int i = 0; i < options.Length; ++i)
                {
                    var entryPosition = dropDownPosition + new Vector2(Theme.Spacing, i * (Theme.Spacing + fontHeight));
                    var entrySize = size - new Vector2(2.0f * Theme.Spacing, 0.0f);
                    var hover = false;
                    if (mouse != null &&
                        m_area.Contains(m_screen.MousePosition) &&
                        new Quad(entryPosition, entrySize).Contains(m_screen.MousePosition))
                    {
                        hover = true;
                        if(mouse.GetInput(MouseButton.Left).Pressed)
                        {
                            io_value = options[i];
                            m_state.SelectedID = null;
                            changed = true;
                        }
                        mouse = null;
                    }

                    DrawText(
                        entryPosition,
                        entrySize,
                        options[i].ToString(),
                        TextAlignment.Left,
                        hover ? Theme.HighlightColour : Theme.TextColour
                    );
                }
            }

            m_rowHeight = Mathf.Max(m_rowHeight, size.Y);
            NextColumn();
            return changed;
        }

        private void DrawBox(Vector2 start, Vector2 size, Colour colour)
        {
            var texture = Texture.Get("debuggui/box.png", false);
            float xEdgeWidth = (float)texture.Width * 0.25f * DebugGUI.DEBUG_GUI_SCALE;
            float yEdgeWidth = (float)texture.Height * 0.25f * DebugGUI.DEBUG_GUI_SCALE;
            m_builder.AddNineSlice(
                start,
                start + size,
                xEdgeWidth,
                yEdgeWidth,
                xEdgeWidth,
                yEdgeWidth,
                texture,
                colour
            );
        }

        private void DrawText(Vector2 start, Vector2 size, string text, TextAlignment alignment, Colour colour)
        {
            var centre = start + 0.5f * size;
            centre.Y -= 0.5f * Theme.Font.GetHeight(Theme.FontSize);
            if (alignment == TextAlignment.Left)
            {
                centre.X = start.X;
            }
            else if (alignment == TextAlignment.Right)
            {
                centre.X = start.X + size.X;
            }
            m_builder.AddText(text, centre, Theme.Font, Theme.FontSize, colour, alignment);
        }
    }

    internal abstract class DebugWindow : Element
    {
        private string m_title;
        private DebugGUITheme m_theme;
        private DebugGUIState m_state;

        private ISpatialPress m_drag;
        private Vector2 m_lastDragPosition;
        private bool m_dragIsResize;

        public string Title
        {
            get
            {
                return m_title;
            }
            set
            {
                m_title = value;
                RequestRebuild();
            }
        }

        protected DebugWindow()
        {
            Size = new Vector2(256.0f, 256.0f) * DebugGUI.DEBUG_GUI_SCALE;
            m_title = "";
            m_theme = DebugGUITheme.Default;
            m_state = new DebugGUIState();
        }

        protected override void OnInit()
        {
        }

        private void BringToFront()
        {
            Element topElement = this;
            foreach (var element in Parent.Elements)
            {
                if(element.ZOrder >= topElement.ZOrder)
                {
                    topElement = element;
                }
            }
            if(topElement != this)
            {
                ZOrder = topElement.ZOrder + 1;
            }
        }

        protected override void OnUpdate(float dt)
        {
            var titleHeight = m_theme.Font.GetHeight(m_theme.FontSize) + 2.0f * m_theme.Spacing;
            var titleSize = new Vector2(Width, titleHeight);
            var corner = Texture.Get("debuggui/corner.png", false);
            var cornerSize = new Vector2(corner.Width, corner.Height) * DebugGUI.DEBUG_GUI_SCALE;

            if (m_drag == null)
            {
                // Start drag
                IMouse mouse;
                if (Screen.CheckMousePressed(new Quad(Position, titleSize), MouseButton.Left, out mouse))
                {
                    m_drag = new MousePress(Screen, mouse, MouseButton.Left);
                    m_lastDragPosition = m_drag.CurrentPosition;
                    m_dragIsResize = false;
                    BringToFront();
                }
                else if (Screen.CheckMousePressed(new Quad(Position + Size - cornerSize, cornerSize), MouseButton.Left, out mouse))
                {
                    m_drag = new MousePress(Screen, mouse, MouseButton.Left);
                    m_lastDragPosition = m_drag.CurrentPosition;
                    m_dragIsResize = true;
                    BringToFront();
                }
                else if(Screen.CheckMousePressed(Area, MouseButton.Left, out mouse))
                {
                    BringToFront();
                }
            }
            else
            {
                // Continue drag
                var pos = m_drag.CurrentPosition;
                var delta = pos - m_lastDragPosition;
                if (m_dragIsResize)
                {
                    var newSize = Size + delta;
                    newSize.X = Mathf.Max(newSize.X, cornerSize.X + m_theme.Margin);
                    newSize.Y = Mathf.Max(newSize.Y, titleHeight + cornerSize.Y);
                    var newDelta = newSize - Size;
                    Size += newDelta;
                    m_lastDragPosition += newDelta;
                }
                else
                {
                    LocalPosition += delta;
                    m_lastDragPosition += delta;
                }
                if (!m_drag.Held)
                {
                    m_drag = null;
                }
            }

            RequestRebuild();
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            var oldClip = builder.ClipRegion;
            try
            {
                builder.ClipRegion = Area;

                // Background
                var position = Position;
                var box = Texture.Get("debuggui/box.png", false);
                builder.AddNineSlice(
                    position,
                    position + Size,
                    box.Width * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Height * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Width * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Height * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box,
                    m_theme.BackgroundColour
                );

                // Corner
                var corner = Texture.Get("debuggui/corner.png", false);
                var cornerSize = new Vector2(corner.Width, corner.Height) * DebugGUI.DEBUG_GUI_SCALE;
                corner.Wrap = false;
                builder.AddQuad(
                    position + Size - cornerSize,
                    position + Size,
                    corner,
                    Quad.UnitSquare,
                    m_theme.HighlightColour
                );

                // Title
                var textHeight = m_theme.Font.GetHeight(m_theme.FontSize);
                var titleSize = new Vector2(
                    Width,
                    textHeight + 2.0f * m_theme.Spacing
                );
                builder.AddNineSlice(
                    position,
                    position + titleSize,
                    box.Width * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Height * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Width * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box.Height * 0.25f * DebugGUI.DEBUG_GUI_SCALE,
                    box,
                    m_theme.HighlightColour
                );
                builder.AddText(
                    m_title,
                    position + new Vector2(0.5f * titleSize.X, 0.5f * titleSize.Y - 0.5f * textHeight),
                    m_theme.Font,
                    m_theme.FontSize,
                    m_theme.TextColour,
                    TextAlignment.Center
                );

                var area = new Quad(
                    position + new Vector2(m_theme.Margin, titleSize.Y + m_theme.Spacing),
                    Size - new Vector2(2.0f * m_theme.Margin, m_theme.Margin + titleSize.Y + m_theme.Spacing)
                );
                var debugBuilder = new DebugGUIBuilder(builder, Screen, area, m_theme, m_state);
                OnGUI(ref debugBuilder);
            }
            finally
            {
                builder.ClipRegion = oldClip;
            }
        }

        protected abstract void OnGUI(ref DebugGUIBuilder builder);
    }
}
