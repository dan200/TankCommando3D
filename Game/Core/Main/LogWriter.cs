using System.IO;
using System.Text;

namespace Dan200.Core.Main
{
    internal class LogWriter : TextWriter
    {
        private LogLevel m_level;
        private StringBuilder m_pendingLine;

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public LogWriter(LogLevel level)
        {
            m_level = level;
            m_pendingLine = new StringBuilder();
        }

        public override void Write(string value)
        {
            int pos = 0;
            var newLineIdx = value.IndexOf('\n', pos);
            while(newLineIdx >= 0)
            {
                var length = newLineIdx - pos;
                if(newLineIdx - 1 >= pos && value[newLineIdx - 1] == '\r')
                {
                    --length;
                }
                m_pendingLine.Append(value.Substring(pos, length));
                Emit();
                pos = newLineIdx + 1;
                newLineIdx = value.IndexOf('\n', pos);
            }
            m_pendingLine.Append(value.Substring(pos));
        }

        public override void Write(char value)
        {
            if (value == '\n')
            {
                Emit();
            }
            else if (value != '\r')
            {
                m_pendingLine.Append(value);
            }
        }

        private void Emit()
        {
			App.Log(m_level, m_pendingLine.ToString());
            m_pendingLine.Clear();
        }
    }
}

