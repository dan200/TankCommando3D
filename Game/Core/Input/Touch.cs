using Dan200.Core.Math;


namespace Dan200.Core.Input
{
    internal class Touch
    {
        private bool m_held;
        private bool m_wasHeld;
        private bool m_pendingHeld;
        private bool m_pendingCancel;

        private Vector2 m_startPos;
        private Vector2 m_latestPos;
        private Vector2 m_pendingPos;
        private Vector2 m_velocity;
        private Vector2 m_delta;
        private float m_duration;
        private bool m_claimed;

        public bool Held
        {
            get { return m_held; }
        }

        public bool Pressed
        {
            get { return m_held && !m_wasHeld; }
        }

        public bool Released
        {
            get { return !m_held && m_wasHeld; }
        }

        public Vector2 StartPosition
        {
            get { return m_startPos; }
        }

        public Vector2 LatestPosition
        {
            get { return m_latestPos; }
        }

        public Vector2 DeltaPosition
        {
            get { return m_delta; }
        }

        public Vector2 Velocity
        {
            get { return m_velocity; }
        }

        public float Duration
        {
            get
            {
                return m_duration;
            }
        }

        public bool Claimed
        {
            get
            {
                return m_claimed;
            }
        }

        public Touch(Vector2 startPos)
        {
            m_wasHeld = false;
            m_held = false;
            m_pendingHeld = false;

            m_startPos = startPos;
            m_latestPos = startPos;
            m_pendingPos = startPos;
            m_velocity = Vector2.Zero;

            m_duration = 0.0f;
            m_claimed = false;
        }

        public void Claim()
        {
            m_claimed = true;
        }

        public void Press()
        {
            m_pendingHeld = true;
        }

        public void Move(Vector2 pos)
        {
            m_pendingPos = pos;
        }

        public void Release()
        {
            m_pendingHeld = false;
        }

        public void Cancel()
        {
            m_pendingHeld = false;
            m_pendingCancel = true;
        }

        public void Update(float dt)
        {
            m_wasHeld = m_held;
            m_held = m_pendingHeld;
            if (m_pendingCancel)
            {
                m_wasHeld = m_held;
                m_pendingCancel = false;
            }
            if (m_held)
            {
                m_duration += dt;
            }
            m_delta = m_pendingPos - m_latestPos;
            if (dt > 0.0f)
            {
                m_velocity = 0.5f * m_velocity + 0.5f * (m_delta / dt);
            }
            m_latestPos = m_pendingPos;
        }
    }
}

