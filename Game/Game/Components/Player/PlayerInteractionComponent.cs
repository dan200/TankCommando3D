using Dan200.Core.Geometry;
using Dan200.Core.Level;
using Dan200.Core.Math;
using Dan200.Core.Physics;
using Dan200.Core.Main;
using Dan200.Core.Interfaces;
using Dan200.Core.Lua;
using Dan200.Core.Systems;
using Dan200.Core.Components;
using Dan200.Game.Interfaces;
using System.Linq;
using Dan200.Core.Util;
using Dan200.Core.Components.Core;
using Dan200.Game.Components.Misc;
using Dan200.Game.Components.Weapons;
using Dan200.Core.Serialisation;

namespace Dan200.Game.Components.Player
{
    internal struct PlayerInteractionComponentData
    {
        [Range(Min = 0.0)]
        public float InteractDistance;
    }

    [RequireSystem(typeof(PhysicsSystem))]
    [RequireComponent(typeof(PlayerInputComponent))]
	[RequireComponent(typeof(PlayerMovementComponent))]
    [RequireComponent(typeof(HealthComponent))]
    internal class PlayerInteractionComponent : Component<PlayerInteractionComponentData>, IUpdate
	{
        private PhysicsSystem m_physics;
        private PlayerInputComponent m_input;
		private PlayerMovementComponent m_movement;
        private HealthComponent m_health;
        private PlayerInteractionComponentData m_properties;

        private Entity m_usedEntity;
        private Entity m_interactableUnderCursor;

        public Entity InteractableUnderCursor
        {
            get
            {
                return m_interactableUnderCursor;
            }
        }

        protected override void OnInit(in PlayerInteractionComponentData properties)
		{
            m_physics = Level.GetSystem<PhysicsSystem>();
            m_input = Entity.GetComponent<PlayerInputComponent>();
			m_movement = Entity.GetComponent<PlayerMovementComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_properties = properties;

            m_usedEntity = null;
            m_interactableUnderCursor = null;
        }
        
        protected override void OnShutdown()
		{
			if (m_usedEntity != null)
			{
				TryInteraction(Entity, Interaction.EndUse);
				m_usedEntity = null;
				m_movement.MovementLocked = false;
			}
		}

		public void Update(float dt)
		{
            if (m_usedEntity == null)
			{
                // Find a thing to interact with
                m_interactableUnderCursor = null;
                RaycastResult result;
                if (m_physics.World.Raycast(
                    new Ray(m_movement.EyePos, m_movement.EyeLook, m_properties.InteractDistance),
                    CollisionGroup.Prop | CollisionGroup.Environment | CollisionGroup.NPC,
                    out result
                ))
                {
                    var entity = result.Shape.UserData as Entity;
                    if (entity != null && (CanInteract(entity, Interaction.UseOnce) || CanInteract(entity, Interaction.StartUse)))
                    {
                        m_interactableUnderCursor = entity;
                    }
                }

                // Peform the interaction
                if (m_input.Interact && !m_health.IsDead && m_interactableUnderCursor != null)
                {
					if (TryInteraction(m_interactableUnderCursor, Interaction.UseOnce))
					{
						// One shot interaction: nothing else to do
					}
					else if (TryInteraction(m_interactableUnderCursor, Interaction.StartUse))
					{
                        // Start/finish interaction: wait for us to disable it later
						m_usedEntity = m_interactableUnderCursor;
						m_movement.MovementLocked = true;
					}							
				}
			}
			else
			{
                // Finish interacting
                m_interactableUnderCursor = null;
                if (m_input.Interact || m_health.IsDead)
				{
					if (m_usedEntity != null && TryInteraction(m_usedEntity, Interaction.EndUse))
					{
						m_usedEntity = null;
						m_movement.MovementLocked = false;
					}
				}
			}
		}

        private bool CanInteract(Entity e, Interaction action)
        {
            while (e != null)
            {
                foreach (var interactable in e.GetComponentsWithInterface<IInteractable>())
                {
                    if (interactable.CanInteract(Entity, action))
                    {
                        return true;
                    }
                }
                e = HierarchyComponent.GetParent(e);
            }
            return false;
        }

        private bool TryInteraction(Entity e, Interaction action)
		{
			bool result = false;
			while (!result && e != null)
			{
				foreach (var interactable in e.GetComponentsWithInterface<IInteractable>())
				{
					if (interactable.Interact(Entity, action))
					{
						result = true;
					}
				}
				e = HierarchyComponent.GetParent(e);
			}
			return result;
		}        
	}
}
