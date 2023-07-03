using Dan200.Core.Util;
using System;
using System.Threading;

namespace Dan200.Core.Async
{
    internal class TaskThread : IDisposable
    {
        private readonly TaskQueue m_tasks;
        private readonly Thread m_thread;
        private readonly AutoResetEvent m_event;
        private volatile bool m_stop;

        public TaskQueue Tasks
        {
            get
            {
                return m_tasks;
            }
        }

        public TaskThread(string name, TaskQueue tasks)
        {
            m_tasks = tasks;
            m_thread = new Thread(Run);
            m_thread.Name = name;
            m_event = new AutoResetEvent(false);
            m_tasks.OnTaskAdded += OnTaskAdded;
            m_stop = false;
            m_thread.Start();
        }

        public void Dispose()
        {
            m_stop = true;
            m_event.Set();
            m_thread.Join();
            m_tasks.OnTaskAdded -= OnTaskAdded;
        }

        private void OnTaskAdded(TaskQueue sender, StructEventArgs e)
        {
            m_event.Set();
        }

        private void Run()
        {
            while (!m_stop)
            {
                if (!m_tasks.DoTask())
                {
                    m_event.WaitOne();
                }
            }
        }
    }
}
