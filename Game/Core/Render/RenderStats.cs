namespace Dan200.Core.Render
{
    internal class RenderStats
    {
        private int m_trianglesLastFrame;
        private int m_drawCallsLastFrame;

        private int m_trianglesThisFrame;
        private int m_drawCallsThisFrame;

        public int Triangles
        {
            get
            {
                return m_trianglesLastFrame;
            }
        }

        public int DrawCalls
        {
            get
            {
                return m_drawCallsLastFrame;
            }
        }

		public RenderStats()
		{
			m_trianglesLastFrame = 0;
			m_drawCallsLastFrame = 0;
			m_trianglesThisFrame = 0;
			m_drawCallsThisFrame = 0;
		}

        public void EndFrame()
        {
            m_trianglesLastFrame = m_trianglesThisFrame;
            m_trianglesThisFrame = 0;
            m_drawCallsLastFrame = m_drawCallsThisFrame;
            m_drawCallsThisFrame = 0;
        }

        public void AddDrawCall(int triangles)
        {
            m_drawCallsThisFrame++;
            m_trianglesThisFrame += triangles;
        }
    }
}
