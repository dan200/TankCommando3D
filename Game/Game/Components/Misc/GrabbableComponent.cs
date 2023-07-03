using System;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Serialisation;
using Dan200.Core.Util;
using Dan200.Game.Interfaces;

namespace Dan200.Game.Components.Misc
{
    internal enum GrabAlignment
    {
        Free,
        Upright,
        FacePlayer,
        FacePlayerUpright,
    }

    internal struct GrabbaleComponentData
    {
        [Range(Min = 0.0f)]
        public float Distance;
        public GrabAlignment Alignment;
    }

    internal class GrabbableComponent : Component<GrabbaleComponentData>, IInteractable
    {
        private float m_distance;
        private GrabAlignment m_alignment;

        public float Distance
        {
            get
            {
                return m_distance;
            }
        }

        public GrabAlignment Alignment
        {
            get
            {
                return m_alignment;
            }
        }

        protected override void OnInit(in GrabbaleComponentData properties)
        {
            m_distance = properties.Distance;
            m_alignment = properties.Alignment;
        }

        protected override void OnShutdown()
        {
        }

        public bool CanInteract(Entity player, Interaction action)
        {
            switch (action)
            {
                case Interaction.Grab:
                case Interaction.Drop:
                    return true;
                default:
                    return false;
            }
        }

        public bool Interact(Entity player, Interaction action)
        {
            return CanInteract(player, action);
        }
    }
}
