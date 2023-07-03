using Dan200.Core.Main;

namespace Dan200.Core.Level
{
    internal class Clock
    {
        private float m_time;
        private float m_realTime;
        private float m_rate;

        public float Time
        {
            get
            {
                return m_time;
            }
        }

        public float RealTime
        {
            get
            {
                return m_realTime;
            }
        }

        public float Rate
        {
            get
            {
                return m_rate;
            }
            set
            {
                App.Assert(value >= 0.0f);
                m_rate = value;
            }
        }

        public Clock()
        {
            m_time = 0.0f;
            m_realTime = 0.0f;
            m_rate = 1.0f;
        }

        public void Update(float dt)
        {
            m_time += dt * m_rate;
            m_realTime += dt;
        }
    }
}
