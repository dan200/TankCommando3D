using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Dan200.Core.Async
{
    internal class TaskQueue
    {
        private readonly Queue<Task> m_queue;
        private volatile int m_unfinishedTasks;
        private StructEventHandler<TaskQueue> m_onTaskAdded;

        public event StructEventHandler<TaskQueue> OnTaskAdded
        {
            add
            {
                lock (m_queue)
                {
                    m_onTaskAdded += value;
                }
            }
            remove
            {
                lock (m_queue)
                {
                    m_onTaskAdded -= value;
                }
            }
        }

        public TaskQueue()
        {
            m_queue = new Queue<Task>();
            m_unfinishedTasks = 0;
        }

        public void AddTask(Task task)
        {
            lock (m_queue)
            {
                Interlocked.Increment(ref m_unfinishedTasks);
                m_queue.Enqueue(task);
                FireOnTaskAdded();
            }
        }

        public bool DoTask()
        {
            Task task;
            lock (m_queue)
            {
                if (m_queue.Count == 0)
                {
                    return false;
                }
                task = m_queue.Dequeue();
            }
            task.Invoke();
            Interlocked.Decrement(ref m_unfinishedTasks);
            return true;
        }

        public void WaitUntilEmpty()
        {
            while (m_unfinishedTasks > 0)
            {
                if(!DoTask())
                {
                    // TODO: Find a way to yield instead of busy waiting
                }
            }
        }

        private void FireOnTaskAdded()
        {
            var onTaskAdded = m_onTaskAdded;
            if (onTaskAdded != null)
            {
                onTaskAdded.Invoke(this, StructEventArgs.Empty);
            }
        }
    }
}
