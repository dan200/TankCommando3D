using Dan200.Core.Main;

namespace Dan200.Core.Async
{
    public class SimplePromise : Promise
    {
        private volatile Status m_status;
        private string m_error;

        public Status Status
        {
            get
            {
                lock (this)
                {
                    return m_status;
                }
            }
        }

        public string Error
        {
            get
            {
                lock (this)
                {
                    return m_error;
                }
            }
        }

        public SimplePromise()
        {
            m_status = Status.Waiting;
            m_error = null;
        }

        public void Succeed()
        {
            lock (this)
            {
                m_status = Status.Complete;
                m_error = null;
            }
        }

        public void Fail(string error)
        {
            lock (this)
            {
                m_status = Status.Error;
                m_error = error;
            }
        }
    }

    public class SimplePromise<T> : Promise<T>
    {
        private volatile Status m_status;
        private T m_result;
        private string m_error;

        public Status Status
        {
            get
            {
                return m_status;
            }
        }

        public T Result
        {
            get
            {
                App.Assert(m_status == Status.Complete);
                return m_result;
            }
        }

        public string Error
        {
            get
            {
                App.Assert(m_status == Status.Error);
                return m_error;
            }
        }

        public SimplePromise()
        {
            m_status = Status.Waiting;
            m_result = default(T);
            m_error = null;
        }

        public void Succeed(T result)
        {
            App.Assert(m_status == Status.Waiting);
            m_result = result;
            m_status = Status.Complete;
        }

        public void Fail(string error)
        {
            App.Assert(m_status == Status.Waiting);
            m_error = error;
            m_status = Status.Error;
        }
    }
}
