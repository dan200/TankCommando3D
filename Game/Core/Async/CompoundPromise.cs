using System.Collections.Generic;
using System.Text;
using Dan200.Core.Main;

namespace Dan200.Core.Async
{
    internal class CompoundPromise : PromiseBase, IProgress
    {
        private readonly IReadOnlyCollection<PromiseBase> m_promises;

        public override bool IsReady
        {
            get
            {
                foreach (PromiseBase promise in m_promises)
                {
                    if (!promise.IsReady)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public int CurrentProgress
        {
            get
            {
                int count = 0;
                foreach (PromiseBase promise in m_promises)
                {
                    if(promise.IsReady)
                    {
                        ++count;
                    }
                }
                return count;
            }
        }

        public int TotalProgress
        {
            get
            {
                return m_promises.Count;
            }
        }     

        public CompoundPromise(IReadOnlyCollection<PromiseBase> promises)
        {
            m_promises = promises;
        }
    }
}
