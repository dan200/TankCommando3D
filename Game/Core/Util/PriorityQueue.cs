using Dan200.Core.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Util
{
    internal class PriorityQueue<T>
    {
        private struct Entry
        {
            public readonly T Data;
            public readonly float Priority;

            public Entry(in T data, float priority)
            {
                Data = data;
                Priority = priority;
            }
        }

        private List<Entry> m_contents;
        private EqualityComparer<T> m_comparer;

        public int Count
        {
            get
            {
                return m_contents.Count;
            }
        }

        public PriorityQueue()
        {
            m_contents = new List<Entry>();
            m_comparer = EqualityComparer<T>.Default;
        }

        private static int SortDescendingByPriority(Entry a, Entry b)
        {
            return -a.Priority.CompareTo(b.Priority);
        }

        public void Enqueue(in T item, float priority)
        {
            m_contents.InsertSorted(new Entry(item, priority), SortDescendingByPriority);
        }

        public T Dequeue()
        {
            App.Assert(m_contents.Count > 0);
            int last = m_contents.Count - 1;
            var result = m_contents[last].Data;
            m_contents.RemoveAt(last);
            return result;
        }

        public void UpdatePriority(in T item, float priority)
        {
            // Find the existing position
            int existingIndex = -1;
            float existingPriority = 0.0f;
            for (int i = 0; i < m_contents.Count; ++i)
            {
                if (m_comparer.Equals(item, m_contents[i].Data))
                {
                    existingIndex = i;
                    existingPriority = m_contents[i].Priority;
                    break;
                }
            }

            // Replace it
            var newEntry = new Entry(item, priority);
            if (existingIndex < 0)
            {
                m_contents.InsertSorted(newEntry, SortDescendingByPriority);
            }
            else
            {
                int insertIndex;
                if (priority >= existingPriority)
                {
                    insertIndex = m_contents.BinarySearch(0, existingIndex, newEntry, SortDescendingByPriority);
                }
                else
                {
                    insertIndex = m_contents.BinarySearch(existingIndex, m_contents.Count - existingIndex, newEntry, SortDescendingByPriority);
                }
                if (insertIndex < 0)
                {
                    insertIndex = ~insertIndex;
                }

                if (insertIndex == existingIndex)
                {
                    m_contents[existingIndex] = newEntry;
                }
                else if(insertIndex > existingIndex)
                {
                    m_contents.RemoveAt(existingIndex);
                    m_contents.Insert(insertIndex - 1, new Entry(item, priority));
                }
                else
                {
                    m_contents.RemoveAt(existingIndex);
                    m_contents.Insert(insertIndex, new Entry(item, priority));
                }
            }
            App.Assert(m_contents.IsSorted(SortDescendingByPriority));
        }

        public void Clear()
        {
            m_contents.Clear();
        }
    }
}
