using Dan200.Core.GUI;
using Dan200.Core.Math;
using Dan200.Core.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.GUI
{
    internal class ChatterDisplay : Element
    {
        private const float SCAN_RATE = 40.0f;
        private const float TEXT_TIMEOUT = 5.0f;

        private string m_currentText;
        private int m_charactersScanned;
        private float m_characterScanTimer;
        private float m_timeRemaining;

        public bool IsTextShowing
        {
            get
            {
                return m_currentText != null;
            }
        }
        
        public ChatterDisplay()
        {
            Height = LowResUI.TextFont.GetHeight(LowResUI.TextFontSize);
        }

        protected override void OnInit()
        {
            m_currentText = null;
        }

        public void ShowText(string text)
        {
            m_currentText = text;
            m_charactersScanned = 0;
            m_characterScanTimer = 0.0f;
            m_timeRemaining = TEXT_TIMEOUT;
            RequestRebuild();
        }

        protected override void OnUpdate(float dt)
        {
            if(m_currentText != null)
            {
                if (m_charactersScanned < m_currentText.Length)
                {
                    m_characterScanTimer -= dt;
                    while (m_characterScanTimer < 0.0f && m_charactersScanned < m_currentText.Length)
                    {
                        m_characterScanTimer += 1.0f / SCAN_RATE;
                        m_charactersScanned++;
                        RequestRebuild();
                    }
                }
                else
                {
                    m_timeRemaining -= dt;
                    if(m_timeRemaining < 0.0f)
                    {
                        m_currentText = null;
                        RequestRebuild();
                    }
                }
            }
        }

        protected override void OnRebuild(GUIBuilder builder)
        {
            if (m_currentText != null)
            {
                var position = Position;
                var text = m_currentText.Substring(0, m_charactersScanned);
                builder.AddText(text, position, LowResUI.TextFont, LowResUI.TextFontSize, Colour.White, TextAlignment.Left, false, Width);
            }
        }
    }
}
