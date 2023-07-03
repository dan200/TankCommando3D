using Dan200.Core.Main;
using System;
using System.Threading;

namespace Dan200.Core.Async
{
    internal abstract class PromiseBase
    {
        public abstract bool IsReady { get; }
    }

    internal class Promise : PromiseBase
    {
		private bool m_ready;
        private Exception m_error;

        public override bool IsReady
        {
            get
            {
                return m_ready;
            }
        }

        public Promise()
        {
            m_ready = false;
            m_error = null;
        }

        public void Succeed()
        {
            App.Assert(!IsReady);
            m_ready = true;
        }

        public void Fail(Exception error)
        {
            App.Assert(!IsReady);
            m_error = error;
            m_ready = true;
        }

		public void Complete()
        {
            App.Assert(IsReady);
            if (m_error != null)
            {
                throw App.Rethrow(m_error);
            }
        }
    }

    internal class Promise<T> : PromiseBase
    {
		private bool m_ready;
        private T m_result;
        private Exception m_error;

        public override bool IsReady
        {
            get
            {
                return m_ready;
            }
        }

        public Promise()
        {
            m_ready = false;
            m_result = default(T);
            m_error = null;
        }

        public void Succeed(T result)
        {
            App.Assert(!IsReady);
            m_result = result;
            m_ready = true;
        }

        public void Fail(Exception error)
        {
            App.Assert(!IsReady);
            m_error = error;
            m_ready = true;
        }

        public T Complete()
        {
            App.Assert(IsReady);
            if (m_error != null)
            {
                throw App.Rethrow(m_error);
            }
            else
            {
                return m_result;
            }
        }
    }
}
