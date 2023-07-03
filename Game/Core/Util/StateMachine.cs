using System;
using Dan200.Core.Main;

namespace Dan200.Core.Util
{
	internal interface IState<TState> where TState : class, IState<TState>
	{
		void Enter(TState previous);
		void Leave(TState next);
	}

	internal class StateMachine<TState> where TState : class, IState<TState>
	{
		private TState m_current;
		private TState m_pending;

		public TState CurrentState
		{
			get
			{
				return m_current;
			}
		}

		public StateMachine(TState initialState)
		{
			m_current = initialState;
			m_pending = null;
			initialState.Enter(null);
		}

		public void QueueState(TState state)
		{
			m_pending = state;
		}

        public void EnterQueuedState()
        {
            if (m_pending != null)
            {
                m_current.Leave(m_pending);
                m_pending.Enter(m_current);
                m_current = m_pending;
                m_pending = null;
            }
        }

		public void Shutdown()
		{
			m_current.Leave(null);
			m_current = null;
			m_pending = null;
		}
	}
}
