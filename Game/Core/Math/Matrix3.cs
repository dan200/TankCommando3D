using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Matrix3 : IEquatable<Matrix3>
    {
        public static readonly Matrix3 Identity = new Matrix3(
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f)
        );

        public static Matrix3 CreateRotationX(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix3(
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, cosA, sinA),
                new Vector3(0.0f, -sinA, cosA)
            );
        }

        public static Matrix3 CreateRotationY(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix3(
                new Vector3(cosA, 0.0f, -sinA),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(sinA, 0.0f, cosA)
            );
        }

        public static Matrix3 CreateRotationZ(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix3(
                new Vector3(cosA, sinA, 0.0f),
                new Vector3(-sinA, cosA, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f)
            );
        }

        public static Matrix3 CreateScale(Vector3 scale)
        {
            return new Matrix3(
                new Vector3(scale.X, 0.0f, 0.0f),
                new Vector3(0.0f, scale.Y, 0.0f),
                new Vector3(0.0f, 0.0f, scale.Z)
            );
        }

        public static Matrix3 CreateUVTranslationScaleRotation(Vector2 trans, Vector2 scale, float rot)
        {
            // Create a simple translation/scale matrix
            var result = new Matrix3(
                new Vector3(scale.X, 0.0f, 0.0f),
                new Vector3(0.0f, scale.Y, 0.0f),
                new Vector3(trans.X, trans.Y, 1.0f)
            );

            // Apply rotation
            if (rot != 0.0f)
            {
                result = CreateRotationZ(rot) * result;
            }

            return result;
        }

        public static Matrix3 CreateLook(UnitVector3 dir, UnitVector3 up)
        {
            var f = dir;
            var r = up.Cross(f).Normalise();
            var u = f.Cross(r);
            return new Matrix3(
                new Vector3(r.X, r.Y, r.Z),
                new Vector3(u.X, u.Y, u.Z),
                new Vector3(f.X, f.Y, f.Z)
            );
        }

        public Vector3 Row0;
        public Vector3 Row1;
        public Vector3 Row2;

        public Vector3 Column0
        {
            get
            {
                return new Vector3(Row0.X, Row1.X, Row2.X);
            }
        }

        public Vector3 Column1
        {
            get
            {
                return new Vector3(Row0.Y, Row1.Y, Row2.Y);
            }
        }

        public Vector3 Column2
        {
            get
            {
                return new Vector3(Row0.Z, Row1.Z, Row2.Z);
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Right
        {
            get
            {
                return new UnitVector3(Row0);
            }
            set
            {
                Row0 = value;
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Up
        {
            get
            {
                return new UnitVector3(Row1);
            }
            set
            {
                Row1 = value;
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Forward
        {
            get
            {
                return new UnitVector3(Row2);
            }
            set
            {
                Row2 = value;
            }
        }

        public Matrix3(Vector3 row0, Vector3 row1, Vector3 row2)
        {
            Row0 = row0;
            Row1 = row1;
            Row2 = row2;
        }

        public Matrix3 Transpose()
        {
            return new Matrix3(
                Column0,
                Column1,
                Column2
            );
        }

        public Vector3 Transform(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Column0),
                vec.Dot(Column1),
                vec.Dot(Column2)
            );
        }

        public Vector3 GetRotationAngles()
        {
            return new Vector3(
                Mathf.ATan2(-Row2.Y, Row2.Z),
                Mathf.ATan2(Row2.X, new Vector2(Row2.Y, Row2.Z).Length),
                Mathf.ATan2(-Row1.X, Row0.X)
            );
        }
        
        // Only works correctly for affine matrices
        public Vector3 ToWorldDir(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Column0),
                vec.Dot(Column1),
                vec.Dot(Column2)
            );
        }

        // Only works correctly for affine matrices
        public UnitVector3 ToWorldDir(UnitVector3 vec)
        {
            return UnitVector3.ConstructUnsafe(
                vec.Dot(Column0),
                vec.Dot(Column1),
                vec.Dot(Column2)
            );
        }

        // Only works correctly for affine matrices
        public Vector3 ToLocalDir(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Row0),
                vec.Dot(Row1),
                vec.Dot(Row2)
            );
        }

        // Only works correctly for affine matrices
        public UnitVector3 ToLocalDir(UnitVector3 vec)
        {
            return UnitVector3.ConstructUnsafe(
                vec.Dot(Row0),
                vec.Dot(Row1),
                vec.Dot(Row2)
            );
        }

        // Only works correctly for affine matrices
        public Matrix3 InvertAffine()
        {
            return new Matrix3(
                new Vector3(Row0.X, Row1.X, Row2.X),
                new Vector3(Row0.Y, Row1.Y, Row2.Y),
                new Vector3(Row0.Z, Row1.Z, Row2.Z)
            );
        }

        public override bool Equals(object o)
        {
            if (o is Matrix3)
            {
                return Equals((Matrix3)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Row0.GetHashCode() ^ Row1.GetHashCode() ^ Row2.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", Row0, Row1, Row2);
        }

        public bool Equals(Matrix3 o)
        {
            return
                Row0.Equals(o.Row0) &&
                Row1.Equals(o.Row1) &&
                Row2.Equals(o.Row2);
        }

        public static bool operator ==(Matrix3 a, Matrix3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Matrix3 a, Matrix3 b)
        {
            return !a.Equals(b);
        }

        public static Matrix3 operator *(Matrix3 a, Matrix3 b)
        {
            float
                a11 = a.Row0.X, a12 = a.Row0.Y, a13 = a.Row0.Z,
                a21 = a.Row1.X, a22 = a.Row1.Y, a23 = a.Row1.Z,
                a31 = a.Row2.X, a32 = a.Row2.Y, a33 = a.Row2.Z,
                b11 = b.Row0.X, b12 = b.Row0.Y, b13 = b.Row0.Z,
                b21 = b.Row1.X, b22 = b.Row1.Y, b23 = b.Row1.Z,
                b31 = b.Row2.X, b32 = b.Row2.Y, b33 = b.Row2.Z;

            Matrix3 result;
            result.Row0.X = (((a11 * b11) + (a12 * b21)) + (a13 * b31));
            result.Row0.Y = (((a11 * b12) + (a12 * b22)) + (a13 * b32));
            result.Row0.Z = (((a11 * b13) + (a12 * b23)) + (a13 * b33));
            result.Row1.X = (((a21 * b11) + (a22 * b21)) + (a23 * b31));
            result.Row1.Y = (((a21 * b12) + (a22 * b22)) + (a23 * b32));
            result.Row1.Z = (((a21 * b13) + (a22 * b23)) + (a23 * b33));
            result.Row2.X = (((a31 * b11) + (a32 * b21)) + (a33 * b31));
            result.Row2.Y = (((a31 * b12) + (a32 * b22)) + (a33 * b32));
            result.Row2.Z = (((a31 * b13) + (a32 * b23)) + (a33 * b33));
            return result;
        }
    }
}
