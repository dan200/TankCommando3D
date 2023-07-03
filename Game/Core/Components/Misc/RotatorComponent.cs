using Dan200.Core.Components.Core;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan200.Core.Components.Misc
{
    internal struct RotatorComponentData
    {
        public Vector3 AngularVelocity;
    }

    [RequireComponent(typeof(TransformComponent))]
    internal class RotatorComponent : Component<RotatorComponentData>, IUpdate
    {
        private TransformComponent m_transform;
        private Vector3 m_angularVelocity;

        protected override void OnInit(in RotatorComponentData properties)
        {
            m_transform = Entity.GetComponent<TransformComponent>();
            m_angularVelocity = properties.AngularVelocity * Mathf.DEGREES_TO_RADIANS;
        }

        protected override void OnShutdown()
        {
        }

        public void Update(float dt)
        {
            var rotationMatrix = Matrix4.CreateTranslationScaleRotation(Vector3.Zero, Vector3.One, m_angularVelocity * dt).Rotation;
            m_transform.LocalAngularVelocity = m_angularVelocity;
            m_transform.LocalRotation = rotationMatrix * m_transform.LocalRotation;
        }
    }
}
