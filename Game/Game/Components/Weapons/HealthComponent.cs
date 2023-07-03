using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Serialisation;
using Dan200.Core.Systems;
using Dan200.Core.Util;
using Dan200.Game.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Game.Components.Weapons
{
    internal struct HealthComponentData
    {
        [Optional(Default = 0.0f)]
        [Range(Min = 0.0)]
        public float MaxHealth;

        [Optional(Default = false)]
        public bool Invulnerable;

        [Optional(Default = false)]
        public bool IsProxy;

        [Optional]
        public string ProxyRedirectPath;
    }

    internal struct DamageEventArgs
    {
        public readonly Damage Damage;

        public DamageEventArgs(in Damage damage)
        {
            Damage = damage;
        }
    }

    internal class HealthComponent : Component<HealthComponentData>
    {
        private HealthComponent m_redirect;
        private float m_health;
        private bool m_invulnerable;

        public float Health
        {
            get
            {
                if (m_redirect != null)
                {
                    return m_redirect.Health;
                }
                else
                {
                    return m_health;
                }
            }
        }

        public bool Invulnerable
        {
            get
            {
                if (m_redirect != null)
                {
                    return m_redirect.Invulnerable;
                }
                else
                {
                    return m_invulnerable;
                }
            }
            set
            {
                if (m_redirect != null)
                {
                    m_redirect.Invulnerable = value;
                }
                else
                {
                    m_invulnerable = value;
                }
            }
        }

        public bool IsDead
        {
            get
            {
                return Health <= 0.0f;
            }
        }

        public HealthComponent Redirect
        {
            get
            {
                return m_redirect;
            }
        }

        public event StructEventHandler<HealthComponent, DamageEventArgs> OnDamaged;
        public event StructEventHandler<HealthComponent, DamageEventArgs> OnDeath;

        protected override void OnInit(in HealthComponentData properties)
        {
            if (properties.IsProxy)
            {
                App.Assert(properties.ProxyRedirectPath != null);
                m_redirect = Level.GetSystem<NameSystem>().Lookup(properties.ProxyRedirectPath, Entity).GetComponent<HealthComponent>();
                App.Assert(m_redirect != this);
                m_health = 0.0f;
                m_invulnerable = false;
            }
            else
            {
                App.Assert(properties.MaxHealth > 0.0f);
                m_health = properties.MaxHealth;
                m_invulnerable = properties.Invulnerable;
            }
        }

        protected override void OnShutdown()
        {
        }

        public void ApplyDamage(in Damage damage)
        {
            if (m_redirect != null)
            {
                m_redirect.ApplyDamage(damage);
                return;
            }

            App.Assert(damage.Ammount > 0.0f);
            if (m_health > 0.0f)
            {
                if (!m_invulnerable)
                {
                    m_health = Mathf.Max(m_health - damage.Ammount, 0.0f);
                }
                if(damage.Origin != null)
                {
                    foreach(var origin in damage.Origin.GetComponentsWithInterface<IDamageOrigin>())
                    {
                        origin.NotifyDamageDealt(this, damage);
                    }
                }
                FireOnDamaged(damage);
                if(m_health <= 0.0f)
                {
                    FireOnDeath(damage);
                }
            }
        }

        private void FireOnDamaged(in Damage damage)
        {
            if(OnDamaged != null)
            {
                OnDamaged.Invoke(this, new DamageEventArgs(damage));
            }
        }

        private void FireOnDeath(in Damage damage)
        {
            if (OnDeath != null)
            {
                OnDeath.Invoke(this, new DamageEventArgs(damage));
            }
        }
    }
}
