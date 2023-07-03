using System;
using System.Threading;
using System.Threading.Tasks;
using Dan200.Core.Main;
using Dan200.Core.Async;

namespace Dan200.Game.Game
{
    internal class GameThreadPool : IDisposable
    {
        private TaskQueue m_ioTasks;
        private TaskThread[] m_ioThreads;

        private TaskQueue m_workerTasks;
        private TaskThread[] m_workerThreads;

        public TaskQueue IOTasks
        {
            get
            {
                return m_ioTasks;
            }
        }

        public TaskQueue WorkerTasks
        {
            get
            {
                return m_workerTasks;
            }
        }

        public GameThreadPool(int numIOThreads, int numWorkerThreads)
        {
            App.Assert(numIOThreads > 0);
            m_ioTasks = new TaskQueue();
            m_ioThreads = new TaskThread[numIOThreads];
            for (int i = 0; i < m_ioThreads.Length; ++i)
            {
                m_ioThreads[i] = new TaskThread("IO" + i, m_ioTasks);
            }

            App.Assert(numWorkerThreads > 0);
            m_workerTasks = new TaskQueue();
            m_workerThreads = new TaskThread[numWorkerThreads];
            for (int i = 0; i < m_workerThreads.Length; ++i)
            {
                m_workerThreads[i] = new TaskThread("Worker" + i, m_workerTasks);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_ioThreads.Length; ++i)
            {
                m_ioThreads[i].Dispose();
            }
            m_ioThreads = null;
            m_ioTasks = null;

            for (int i = 0; i < m_workerThreads.Length; ++i)
            {
                m_workerThreads[i].Dispose();
            }
            m_workerThreads = null;
            m_workerTasks = null;
        }
    }
}
