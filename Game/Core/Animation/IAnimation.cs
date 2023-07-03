using Dan200.Core.Math;
using Dan200.Core.Render;

namespace Dan200.Core.Animation
{
    internal interface IAnimation
    {
        void Animate(string partName, float time, out bool o_visible, out Matrix4 o_transform, out Matrix3 o_uvTransform, out ColourF o_colour);
    }
}

