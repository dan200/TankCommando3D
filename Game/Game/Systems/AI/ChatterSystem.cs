using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Util;

namespace Dan200.Game.Systems.AI
{
    // High priority chatter can leapfrog low priority chatter in the queue
    // Immediate chatter will only be played if it can be played immediately
    struct Chatter
    {
        public string Speaker;
        public string Dialogue;
        public int Priority;
        public bool Immediate;
    }

    internal struct ChatterSystemData
    {
    }

    internal class ChatterSystem : System<ChatterSystemData>
    {
        private int m_previousChatterPriority;
        private List<Chatter> m_chatterQueue;

        protected override void OnInit(in ChatterSystemData properties)
        {
            m_previousChatterPriority = -1;
            m_chatterQueue = new List<Chatter>();
        }

        protected override void OnShutdown()
        {
        }

        public void CullQueuedChatter(string speaker)
        {
            for (int i = 0; i < m_chatterQueue.Count; ++i)
            {
                var chatter = m_chatterQueue[i];
                if(chatter.Speaker == speaker)
                {
                    m_chatterQueue.RemoveAt(i);
                    i--;
                }
            }
        }

        public void QueueChatter(in Chatter chatter)
        {
            var insertIndex = m_chatterQueue.Count;
            for(int i=0; i<m_chatterQueue.Count; ++i)
            {
                var oldChatter = m_chatterQueue[i];
                if(chatter.Priority > oldChatter.Priority)
                {
                    insertIndex = i;
                    break;
                }
            }
            if(insertIndex == 0 || !chatter.Immediate)
            {               
                if(insertIndex == 0 && m_chatterQueue.Count > 0)
                {
                    // Inserting at the front of a non-empty list, check we're not usurping a would-be immediate chatter
                    var nextChatter = m_chatterQueue[0];
                    if(nextChatter.Immediate)
                    {
                        m_chatterQueue[0] = chatter;
                    }
                    else
                    {
                        m_chatterQueue.Insert(0, chatter);
                    }
                }
                else
                {
                    // Insert somewhere else in the queue
                    m_chatterQueue.Insert(0, chatter);
                }
            }
        }

        public bool GetNextChatter(out Chatter o_chatter, bool previousChatterIsFinished)
        {
            if(m_chatterQueue.Count > 0)
            {
                var nextChatter = m_chatterQueue[0];
                if (previousChatterIsFinished || nextChatter.Priority > m_previousChatterPriority)
                {
                    // A new chatter is ready to be played
                    m_chatterQueue.RemoveAt(0);
                    o_chatter = nextChatter;
                    m_previousChatterPriority = nextChatter.Priority;
                    return true;
                }
                if(nextChatter.Immediate)
                {
                    // An immediate chatter just missed it's oportunity to get played, remove it
                    m_chatterQueue.RemoveAt(0);
                }
            }
            o_chatter = default(Chatter);
            return false;
        }
    }
}
