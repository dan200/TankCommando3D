using Dan200.Core.Components.Core;
using Dan200.Core.Components.Physics;
using Dan200.Core.Interfaces;
using Dan200.Core.Interfaces.Core;
using Dan200.Core.Level;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Components.Misc;
using Dan200.Game.Components.Weapons;
using Dan200.Game.Interfaces;
using Dan200.Game.Systems.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.AI
{
    internal struct TankBehaviourComponentData
    {
        public float ForgetTargetTime;
        public string TurretPath;
        public string PatrolRoutePath;
    }

    [RequireSystem(typeof(NoiseSystem))]
    [RequireSystem(typeof(ChatterSystem))]
    [RequireComponentOnAncestor(typeof(NavGraphComponent))]
    [RequireComponent(typeof(SightComponent))]
    [RequireComponent(typeof(NavigatorComponent))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(CharacterNameComponent))]
    [AfterComponent(typeof(PatrolRouteComponent))]
    [AfterComponent(typeof(NameComponent))]
    [AfterComponent(typeof(TankGunnerComponent))]
    internal class TankBehaviourComponent : Component<TankBehaviourComponentData>, IUpdate, IDebugDraw
    {
        private NameSystem m_nameSystem;
        private NoiseSystem m_noise;
        private ChatterSystem m_chatterSystem;
        private NavGraphComponent m_navigation;
        private SightComponent m_sight;
        private TransformComponent m_transform;
        private NavigatorComponent m_navigator;
        private PatrolRouteComponent m_patrolRoute;
        private TankGunnerComponent m_gunner;
        private HealthComponent m_health;
        private CharacterNameComponent m_characterName;
        private TankBehaviourComponentData m_properties;

        private enum TargetAcquisitionMethod
        {
            Heard = 0,
            Seen,
            AttackedBy,
        }
        private Entity m_target;
        private TargetAcquisitionMethod m_targetAcquistionMethod;
        private bool m_targetVisible;
        private Vector3 m_lastKnownTargetPosition;
        private float m_targetTimeout;
        private bool m_muteChatter;

        protected override void OnInit(in TankBehaviourComponentData properties)
        {
            m_nameSystem = Level.GetSystem<NameSystem>();
            m_noise = Level.GetSystem<NoiseSystem>();
            m_chatterSystem = Level.GetSystem<ChatterSystem>();
            m_navigation = Entity.GetComponentOnAncestor<NavGraphComponent>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_sight = Entity.GetComponent<SightComponent>();
            m_navigator = Entity.GetComponent<NavigatorComponent>();
            m_gunner = m_nameSystem.Lookup(properties.TurretPath, Entity).GetComponent<TankGunnerComponent>();
            m_health = Entity.GetComponent<HealthComponent>();
            m_characterName = Entity.GetComponent<CharacterNameComponent>();
            m_patrolRoute = m_nameSystem.Lookup(properties.PatrolRoutePath, Entity).GetComponent<PatrolRouteComponent>();
            m_properties = properties;

            m_health.OnDamaged += OnDamaged;
            m_health.OnDeath += OnDeath;
            m_target = null;
        }

        protected override void OnShutdown()
        {
            m_health.OnDamaged -= OnDamaged;
            m_health.OnDeath -= OnDeath;
        }

        private void OnDamaged(HealthComponent sender, DamageEventArgs args)
        {
            var origin = args.Damage.Origin;
            if (origin != null)
            {
                if (m_target == null || m_targetAcquistionMethod < TargetAcquisitionMethod.AttackedBy)
                {
                    Bark(BarkType.UnderAttack);
                    m_targetAcquistionMethod = TargetAcquisitionMethod.AttackedBy;
                }
                m_target = origin;
                m_targetTimeout = m_properties.ForgetTargetTime;
                m_targetVisible = m_sight.VisibleTargets.Contains(m_target);
                UpdateLastKnownTargetPosition();
                PlotRouteToLastKnownTargetPosition();
            }
        }

        private void OnDeath(HealthComponent sender, DamageEventArgs args)
        {
            Bark(BarkType.Die);
            Explode(args.Damage);
        }

        private void UpdateTarget(float dt)
        {
            // Forget our target immediately if the entity was deleted
            if (m_target != null && m_target.Dead)
            {
                m_target = null;
            }

            // Acquire targets by sight            
            if (m_sight.VisibleTargets.Contains(m_target))
            {
                // Refresh the current target
                if(m_targetAcquistionMethod < TargetAcquisitionMethod.Seen)
                {
                    m_targetAcquistionMethod = TargetAcquisitionMethod.Seen;
                }
                m_targetTimeout = m_properties.ForgetTargetTime;
                m_targetVisible = true;
                UpdateLastKnownTargetPosition();
            }
            else
            {
                // Look for new targets
                m_targetVisible = false;
                foreach (var target in m_sight.VisibleTargets)
                {                    
                    if (m_target == null || m_targetAcquistionMethod < TargetAcquisitionMethod.Seen)
                    {
                        m_targetAcquistionMethod = TargetAcquisitionMethod.Seen;
                    }
                    m_target = target;
                    m_targetTimeout = m_properties.ForgetTargetTime;
                    m_targetVisible = true;
                    UpdateLastKnownTargetPosition();
                    break;
                }
            }

            // Acquire targets by sound
            var position = m_transform.Position;
            if (!m_targetVisible)
            {
                foreach (var noise in m_noise.RecentNoises)
                {
                    App.Assert(noise.Origin != null);
                    var distanceSq = (noise.Position - position).LengthSquared;
                    if (distanceSq <= Mathf.Square(noise.Radius))
                    {
                        if (m_target == null || m_targetAcquistionMethod < TargetAcquisitionMethod.Heard)
                        {
                            m_targetAcquistionMethod = TargetAcquisitionMethod.Heard;
                        }
                        m_target = noise.Origin;
                        m_targetTimeout = m_properties.ForgetTargetTime;
                        m_targetVisible = false;
                        m_lastKnownTargetPosition = noise.Position;
                        PlotRouteToLastKnownTargetPosition();
                        break;
                    }
                }
            }

            // Forget about targets we can't see after a while
            if (!m_targetVisible)
            {
                m_targetTimeout -= dt;
                if (m_targetTimeout < 0.0f)
                {
                    m_target = null;
                }
            }
        }

        private enum BarkType // in priority order: later entries will interupt earlier ones
        {
            LostSightOfEnemy,
            ForgottenEnemy,
            HeardSomething,
            GainedSightOfEnemy,
            EnemySpotted,
            UnderAttack,
            EnemyKilled,
            Die
        }

        private void Bark(BarkType bark, bool immediate=true)
        {
            if (!m_muteChatter)
            {
                var language = Level.GetSystem<GUISystem>().Screen.Language;
                var chatter = new Chatter();
                chatter.Speaker = m_characterName.Name;
                chatter.Dialogue = language.Translate(
                    language.GetRandomVariant("ai.chatter." + bark.ToString().ToLowerUnderscored())
                );
                chatter.Priority = (int)bark;
                chatter.Immediate = immediate;

                if (bark == BarkType.Die || bark == BarkType.EnemyKilled)
                {
                    m_chatterSystem.CullQueuedChatter(m_characterName.Name);
                    m_muteChatter = true;
                }
                m_chatterSystem.QueueChatter(chatter);
            }
        }

        private void PlotPatrolRoute()
        {
            m_navigator.ClearRoute();
            foreach (var waypoint in m_patrolRoute.Waypoints)
            {
                m_navigator.AppendRoute(waypoint.Node);
            }
        }

        private void UpdateLastKnownTargetPosition()
        {
            App.Assert(m_target != null);
            var targetTransform = m_target.GetComponent<TransformComponent>();
            m_lastKnownTargetPosition = targetTransform.Position;
        }

        private void PlotRouteToLastKnownTargetPosition()
        {
            var target = m_navigation.FindWaypointNear(m_lastKnownTargetPosition);
            if (m_navigator.DestinationWaypoint != target)
            {
                m_navigator.ClearRoute();
                m_navigator.AppendRoute(target);
            }
        }

        private void Explode(in Damage lastDamage)
        {
            // Become physics
            var gun = m_gunner.Entity;
            var gunHierarchy = gun.GetComponent<HierarchyComponent>();
            if (gunHierarchy != null)
            {
                gunHierarchy.Parent = Level.Entities.Lookup(1); // TODO
            }
            var gunPhysics = gun.GetComponent<PhysicsComponent>();
            if(gunPhysics != null)
            {
                gunPhysics.Object.Kinematic = false;
            }
            var physics = Entity.GetComponent<PhysicsComponent>();
            if(physics != null)
            {
                physics.Object.Kinematic = false;
            }

            // Create an explosion
            var prefab = EntityPrefab.Get("entities/explosion.entity");
            var properties = new LuaTable();
            properties["Position"] = (m_transform.Position + Vector3.YAxis).ToLuaValue();
            properties["Rotation"] = new Vector3(
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f),
                GlobalRandom.Float(0.0f, 360.0f)
            ).ToLuaValue();
            properties["Radius"] = 6.0f;
            properties["Lifespan"] = 0.1f;
            properties["Damage"] = 100.0f;
            var explosion = prefab.Instantiate(Level, properties, 1); // TODO

            // Propagate damage origin
            foreach (var propagator in explosion.GetComponentsWithInterface<IDamagePropagator>())
            {
                propagator.DamageOrigin = lastDamage.Origin;
            }

            // Despawn soom
            gun.GetComponent<DespawnerComponent>().Despawn();
            Entity.GetComponent<DespawnerComponent>().Despawn();
        }

        public void Update(float dt)
        {
            // Do nothing when dead
            if(m_health.IsDead)
            {
                return;
            }

            // Kill the enemy
            if (m_target != null && m_target.GetComponent<HealthComponent>() != null && m_target.GetComponent<HealthComponent>().IsDead)
            {
                Bark(BarkType.EnemyKilled);
            }

            // Pick a new target
            var previousTarget = m_target;
            var oldTarget = m_target;
            var oldTargetAcquisitionMethod = m_targetAcquistionMethod;
            var oldLastKnownTargetPosition = m_lastKnownTargetPosition;
            var oldTargetVisible = m_targetVisible;
            UpdateTarget(dt);

            // Do barks
            if (m_target != oldTarget)
            {
                if (m_target == null)
                {
                    Bark(BarkType.ForgottenEnemy, false);
                }
                else if (m_targetAcquistionMethod == TargetAcquisitionMethod.Seen)
                {
                    Bark(BarkType.EnemySpotted);
                }
                else
                {
                    Bark(BarkType.HeardSomething);
                }
            }
            else if (m_target != null && m_targetVisible != oldTargetVisible)
            {
                if (m_targetVisible)
                {
                    Bark(BarkType.GainedSightOfEnemy);
                }
                else
                {
                    Bark(BarkType.LostSightOfEnemy, false);
                }
            }

            // Do Navigation
            if(m_target != null)
            {
                if(m_lastKnownTargetPosition != oldLastKnownTargetPosition)
                {
                    PlotRouteToLastKnownTargetPosition();
                }
            }
            else
            {
                if(previousTarget != null)
                {
                    PlotPatrolRoute();
                }
                if(m_navigator.GetNextWaypointOnRoute() == null)
                {
                    PlotPatrolRoute();
                }
            }

            // Update gunner
            if(m_target != null)
            {
                var targetHealth = m_target.GetComponent<HealthComponent>();
                var targetDead = targetHealth != null && targetHealth.IsDead;
                if (m_targetVisible && !targetDead)
                {
                    m_gunner.ShootAt(m_target, Vector3.YAxis);
                }
                else if(m_navigator.GetNextWaypointOnRoute() != null)
                {
                    m_gunner.AimAt(m_lastKnownTargetPosition + Vector3.YAxis);
                }
                else
                {
                    m_gunner.LookAroundRandomly();
                }
            }
            else
            {
                m_gunner.AimForward();
            }
        }

        public void DebugDraw()
        {
            var position = m_transform.Position;
            App.DebugDraw.DrawCross(position, 1.0f, Colour.White);

            if (m_target != null)
            {
                var targetPosition = m_target.GetComponent<TransformComponent>().Position;
                App.DebugDraw.DrawLine(position, m_lastKnownTargetPosition, Colour.Yellow);
                App.DebugDraw.DrawLine(position, targetPosition, Colour.Green);
            }
        }
    }
}
