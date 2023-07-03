using System;
using System.Runtime.InteropServices;

namespace Dan200.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Matrix4 : IEquatable<Matrix4>
    {
        public static readonly Matrix4 Identity = new Matrix4(
            new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
        );

        public static Matrix4 CreateTranslation(float x, float y, float z)
        {
            return new Matrix4(
                new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(x, y, z, 1.0f)
            );
        }

        public static Matrix4 CreateTranslation(Vector3 trans)
        {
            return new Matrix4(
                new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(trans.X, trans.Y, trans.Z, 1.0f)
            );
        }

        public static Matrix4 CreateRotationX(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix4(
                new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, cosA, sinA, 0.0f),
                new Vector4(0.0f, -sinA, cosA, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        public static Matrix4 CreateRotationY(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix4(
                new Vector4(cosA, 0.0f, -sinA, 0.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
                new Vector4(sinA, 0.0f, cosA, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        public static Matrix4 CreateRotationZ(float angle)
        {
            var cosA = Mathf.Cos(angle);
            var sinA = Mathf.Sin(angle);
            return new Matrix4(
                new Vector4(cosA, sinA, 0.0f, 0.0f),
                new Vector4(-sinA, cosA, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        public static Matrix4 CreateScale(Vector3 scale)
        {
            return new Matrix4(
                new Vector4(scale.X, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, scale.Y, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, scale.Z, 0.0f),
                new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
            );
        }

        public static Matrix4 CreateTranslationScaleRotation(Vector3 trans, Vector3 scale, Vector3 rot)
        {
            // Create a simple translation/scale matrix
            var result = new Matrix4(
                new Vector4(scale.X, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, scale.Y, 0.0f, 0.0f),
                new Vector4(0.0f, 0.0f, scale.Z, 0.0f),
                new Vector4(trans.X, trans.Y, trans.Z, 1.0f)
            );

            // Apply yaw, pitch and roll
            if (rot.X != 0.0f)
            {
                result = CreateRotationX(rot.X) * result;
            }
            if (rot.Y != 0.0f)
            {
                result = CreateRotationY(rot.Y) * result;
            }
            if (rot.Z != 0.0f)
            {
                result = CreateRotationZ(rot.Z) * result;
            }

            return result;
        }

        // Creates a transform for an object positioned at "pos" and looking at "target"
        // (This is the opposite of what something like gluLookAt() does!)
        public static Matrix4 CreateLookAt(Vector3 pos, Vector3 target, UnitVector3 up)
        {
            var f = (target - pos).Normalise();
            var r = up.Cross(f).Normalise();
            var u = f.Cross(r);
            return new Matrix4(
                new Vector4(r.X, r.Y, r.Z, 0.0f),
                new Vector4(u.X, u.Y, u.Z, 0.0f),
                new Vector4(f.X, f.Y, f.Z, 0.0f),
                new Vector4(pos.X, pos.Y, pos.Z, 1.0f)
            );
        }

		public static Matrix4 CreatePerspective(float fovY, float aspect, float zNear, float zFar)
        {
            var top = zNear * Mathf.Tan(0.5f * fovY);
            var bottom = -top;
            var left = bottom * aspect;
            var right = top * aspect;
            return CreateFrustum(left, right, bottom, top, zNear, zFar);
        }

        public static Matrix4 CreateFrustum(float left, float right, float bottom, float top, float near, float far)
        {
            float x = (2.0f * near) / (right - left);
            float y = (2.0f * near) / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
			float c, d;
			if (far >= float.PositiveInfinity)
			{
				c = -1.0f; ;
				d = 2.0f * near;
			}
			else
			{
				c = -(far + near) / (far - near);
				d = (2.0f * far * near) / (far - near);
			}
            return new Matrix4(
                new Vector4(x, 0.0f, 0.0f, 0.0f),
                new Vector4(0.0f, y, 0.0f, 0.0f),
                new Vector4(a, b, c, 1.0f),
                new Vector4(0.0f, 0.0f, d, 0.0f)
            );
        }

        public static Matrix4 Lerp(Matrix4 a, Matrix4 b, float f)
        {
            var fwd = Vector3.Lerp(a.Forward, b.Forward, f).Normalise();
            var right = Vector3.Lerp(a.Right, b.Right, f).Normalise();
            var up = Vector3.Lerp(a.Up, b.Up, f).Normalise();
            var pos = Vector3.Lerp(a.Position, b.Position, f);

            var result = Matrix4.Identity;
            result.Right = right;
            result.Up = up;
            result.Forward = fwd;
            result.Position = pos;
            return result;
        }

        public Vector4 Row0;
        public Vector4 Row1;
        public Vector4 Row2;
        public Vector4 Row3;

        public Vector4 Column0
        {
            get
            {
                return new Vector4(Row0.X, Row1.X, Row2.X, Row3.X);
            }
        }

        public Vector4 Column1
        {
            get
            {
                return new Vector4(Row0.Y, Row1.Y, Row2.Y, Row3.Y);
            }
        }

        public Vector4 Column2
        {
            get
            {
                return new Vector4(Row0.Z, Row1.Z, Row2.Z, Row3.Z);
            }
        }

        public Vector4 Column3
        {
            get
            {
                return new Vector4(Row0.W, Row1.W, Row2.W, Row3.W);
            }
        }

        public Vector3 Position
        {
            get
            {
                return Row3.XYZ;
            }
            set
            {
                Row3.XYZ = value;
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Right
        {
            get
            {
                return new UnitVector3(Row0.XYZ);
            }
            set
            {
                Row0.XYZ = value;
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Up
        {
            get
            {
                return new UnitVector3(Row1.XYZ);
            }
            set
            {
                Row1.XYZ = value;
            }
        }

        // Only correct for affine matrices
        public UnitVector3 Forward
        {
            get
            {
                return new UnitVector3(Row2.XYZ);
            }
            set
            {
                Row2.XYZ = value;
            }
        }

        public Matrix3 Rotation
        {
            get
            {
                return new Matrix3(
                    Row0.XYZ,
                    Row1.XYZ,
                    Row2.XYZ
                );
            }
            set
            {
                Row0.XYZ = value.Row0;
                Row1.XYZ = value.Row1;
                Row2.XYZ = value.Row2;
            }
        }

        public Matrix4(Vector4 row0, Vector4 row1, Vector4 row2, Vector4 row3)
        {
            Row0 = row0;
            Row1 = row1;
            Row2 = row2;
            Row3 = row3;
        }

        public Matrix4 Transpose()
        {
            return new Matrix4(
                Column0,
                Column1,
                Column2,
                Column3
            );
        }

        public Vector4 Transform(Vector4 vec)
        {
            return new Vector4(
                vec.Dot(Column0),
                vec.Dot(Column1),
                vec.Dot(Column2),
                vec.Dot(Column3)
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
        public Vector3 ToWorldPos(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Column0.XYZ) + Row3.X,
                vec.Dot(Column1.XYZ) + Row3.Y,
                vec.Dot(Column2.XYZ) + Row3.Z
            );
        }

        // Only works correctly for affine matrices
        public Vector3 ToWorldDir(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Column0.XYZ),
                vec.Dot(Column1.XYZ),
                vec.Dot(Column2.XYZ)
            );
        }

        // Only works correctly for affine matrices
        public UnitVector3 ToWorldDir(UnitVector3 vec)
        {
            return UnitVector3.ConstructUnsafe(
                vec.Dot(Column0.XYZ),
                vec.Dot(Column1.XYZ),
                vec.Dot(Column2.XYZ)
            );
        }

        // Only works correctly for affine matrices
        public Vector3 ToLocalPos(Vector3 vec)
        {
            vec = vec - Row3.XYZ;
            return new Vector3(
                vec.Dot(Row0.XYZ),
                vec.Dot(Row1.XYZ),
                vec.Dot(Row2.XYZ)
            );
        }

        // Only works correctly for affine matrices
        public Vector3 ToLocalDir(Vector3 vec)
        {
            return new Vector3(
                vec.Dot(Row0.XYZ),
                vec.Dot(Row1.XYZ),
                vec.Dot(Row2.XYZ)
            );
        }

        // Only works correctly for affine matrices
        public UnitVector3 ToLocalDir(UnitVector3 vec)
        {
            return UnitVector3.ConstructUnsafe(
                vec.Dot(Row0.XYZ),
                vec.Dot(Row1.XYZ),
                vec.Dot(Row2.XYZ)
            );
        }

		// Only works correctly for affine matrices
		public Matrix4 ToWorld(Matrix4 mat)
		{
			return mat * this;
		}

		// Only works correctly for affine matrices
		public Matrix4 ToLocal(Matrix4 mat)
		{
			return mat * this.InvertAffine();
		}

        // Only works correctly for affine matrices
        public Matrix4 InvertAffine()
        {
            return new Matrix4(
                new Vector4(Row0.X, Row1.X, Row2.X, 0.0f),
                new Vector4(Row0.Y, Row1.Y, Row2.Y, 0.0f),
                new Vector4(Row0.Z, Row1.Z, Row2.Z, 0.0f),
                new Vector4(-Row3.Dot(Row0), -Row3.Dot(Row1), -Row3.Dot(Row2), 1.0f)
            );
        }

        public override bool Equals(object o)
        {
            if (o is Matrix4)
            {
                return Equals((Matrix4)o);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Row0.GetHashCode() ^ Row1.GetHashCode() ^ Row2.GetHashCode() ^ Row3.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}, {3}]", Row0, Row1, Row2, Row3);
        }

        public bool Equals(Matrix4 o)
        {
            return
                Row0.Equals(o.Row0) &&
                Row1.Equals(o.Row1) &&
                Row2.Equals(o.Row2) &&
                Row3.Equals(o.Row3);
        }

        public static bool operator ==(Matrix4 a, Matrix4 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Matrix4 a, Matrix4 b)
        {
            return !a.Equals(b);
        }

		public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            float
                a11 = a.Row0.X, a12 = a.Row0.Y, a13 = a.Row0.Z, a14 = a.Row0.W,
                a21 = a.Row1.X, a22 = a.Row1.Y, a23 = a.Row1.Z, a24 = a.Row1.W,
                a31 = a.Row2.X, a32 = a.Row2.Y, a33 = a.Row2.Z, a34 = a.Row2.W,
                a41 = a.Row3.X, a42 = a.Row3.Y, a43 = a.Row3.Z, a44 = a.Row3.W,
                b11 = b.Row0.X, b12 = b.Row0.Y, b13 = b.Row0.Z, b14 = b.Row0.W,
                b21 = b.Row1.X, b22 = b.Row1.Y, b23 = b.Row1.Z, b24 = b.Row1.W,
                b31 = b.Row2.X, b32 = b.Row2.Y, b33 = b.Row2.Z, b34 = b.Row2.W,
                b41 = b.Row3.X, b42 = b.Row3.Y, b43 = b.Row3.Z, b44 = b.Row3.W;

            Matrix4 result;
            result.Row0.X = (((a11 * b11) + (a12 * b21)) + (a13 * b31)) + (a14 * b41);
            result.Row0.Y = (((a11 * b12) + (a12 * b22)) + (a13 * b32)) + (a14 * b42);
            result.Row0.Z = (((a11 * b13) + (a12 * b23)) + (a13 * b33)) + (a14 * b43);
            result.Row0.W = (((a11 * b14) + (a12 * b24)) + (a13 * b34)) + (a14 * b44);
            result.Row1.X = (((a21 * b11) + (a22 * b21)) + (a23 * b31)) + (a24 * b41);
            result.Row1.Y = (((a21 * b12) + (a22 * b22)) + (a23 * b32)) + (a24 * b42);
            result.Row1.Z = (((a21 * b13) + (a22 * b23)) + (a23 * b33)) + (a24 * b43);
            result.Row1.W = (((a21 * b14) + (a22 * b24)) + (a23 * b34)) + (a24 * b44);
            result.Row2.X = (((a31 * b11) + (a32 * b21)) + (a33 * b31)) + (a34 * b41);
            result.Row2.Y = (((a31 * b12) + (a32 * b22)) + (a33 * b32)) + (a34 * b42);
            result.Row2.Z = (((a31 * b13) + (a32 * b23)) + (a33 * b33)) + (a34 * b43);
            result.Row2.W = (((a31 * b14) + (a32 * b24)) + (a33 * b34)) + (a34 * b44);
            result.Row3.X = (((a41 * b11) + (a42 * b21)) + (a43 * b31)) + (a44 * b41);
            result.Row3.Y = (((a41 * b12) + (a42 * b22)) + (a43 * b32)) + (a44 * b42);
            result.Row3.Z = (((a41 * b13) + (a42 * b23)) + (a43 * b33)) + (a44 * b43);
            result.Row3.W = (((a41 * b14) + (a42 * b24)) + (a43 * b34)) + (a44 * b44);
            return result;
        }
    }
}
