using Dan200.Core.Math;
using Ode = ODE.ODE;

namespace Dan200.Core.Physics
{
    internal static unsafe class ODEHelpers
    {
        public static Vector3 ToVector3(float* vec)
        {
            return new Vector3(vec[0], vec[1], vec[2]);
        }

        public static Vector3 ToVector3(ref Ode.dVector3 vec)
        {
            fixed (float* v = vec.v)
            {
                return new Vector3(v[0], v[1], v[2]);
            }
        }

        public static void ToDVector3(ref Vector3 vec, ref Ode.dVector3 result)
        {
            fixed (float* v = result.v)
            {
                v[0] = vec.X;
                v[1] = vec.Y;
                v[2] = vec.Z;
                v[3] = 0.0f;
            }
        }

        public static void ToDVector3(ref UnitVector3 vec, ref Ode.dVector3 result)
        {
            fixed (float* v = result.v)
            {
                v[0] = vec.X;
                v[1] = vec.Y;
                v[2] = vec.Z;
                v[3] = 0.0f;
            }
        }

        public static void ToMatrix3(float* mat, ref Matrix3 result)
        {
            result.Row0 = new Vector3(mat[0], mat[4], mat[8]);
            result.Row1 = new Vector3(mat[1], mat[5], mat[9]);
            result.Row2 = new Vector3(mat[2], mat[6], mat[10]);
        }

        public static void ToMatrix3(ref Ode.dMatrix3 mat, ref Matrix3 result)
        {
            fixed (float* v = mat.v)
            {
                result.Row0 = new Vector3(v[0], v[4], v[8]);
                result.Row1 = new Vector3(v[1], v[5], v[9]);
                result.Row2 = new Vector3(v[2], v[6], v[10]);
            }
        }

        public static void ToDMatrix3(ref Matrix3 mat, ref Ode.dMatrix3 result)
        {
            fixed (float* v = result.v)
            {
                v[0] = mat.Row0.X;
                v[1] = mat.Row1.X;
                v[2] = mat.Row2.X;
                v[3] = 0.0f;
                v[4] = mat.Row0.Y;
                v[5] = mat.Row1.Y;
                v[6] = mat.Row2.Y;
                v[7] = 0.0f;
                v[8] = mat.Row0.Z;
                v[9] = mat.Row1.Z;
                v[10] = mat.Row2.Z;
                v[11] = 0.0f;
            }
        }

        public static void ToMatrix4(float* pos, float* rot, ref Matrix4 result)
        {
            result.Row0 = new Vector4(rot[0], rot[4], rot[8], 0.0f);
            result.Row1 = new Vector4(rot[1], rot[5], rot[9], 0.0f);
            result.Row2 = new Vector4(rot[2], rot[6], rot[10], 0.0f);
            result.Row3 = new Vector4(pos[0], pos[1], pos[2], 1.0f);
        }

        public static void ToDVector3AndDMatrix3(ref Matrix4 mat, ref Ode.dVector3 pos, ref Ode.dMatrix3 rot)
        {
            fixed (float* v = pos.v)
            {
                v[0] = mat.Row3.X;
                v[1] = mat.Row3.Y;
                v[2] = mat.Row3.Z;
                v[3] = 0.0f;
            }
            fixed (float* v = rot.v)
            {
                v[0] = mat.Row0.X;
                v[1] = mat.Row1.X;
                v[2] = mat.Row2.X;
                v[3] = 0.0f;
                v[4] = mat.Row0.Y;
                v[5] = mat.Row1.Y;
                v[6] = mat.Row2.Y;
                v[7] = 0.0f;
                v[8] = mat.Row0.Z;
                v[9] = mat.Row1.Z;
                v[10] = mat.Row2.Z;
                v[11] = 0.0f;
            }
        }

    }
}
