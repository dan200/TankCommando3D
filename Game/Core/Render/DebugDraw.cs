using Dan200.Core.Math;
using System;
using System.Collections.Generic;
using Dan200.Core.Geometry;

namespace Dan200.Core.Render
{
    internal class DebugDraw : IDisposable
    {
        private FlatEffectHelper m_flatEffect;
        private UniformBlock<CameraUniformData> m_cameraUniforms;
        private Geometry<FlatVertex> m_geometry;
        private IRenderGeometry<FlatVertex> m_renderGeometry;
        private Stack<Matrix4> m_transformStack;
        private Matrix4 m_currentTransform;
        private bool m_inFrame;

        public DebugDraw(IRenderer renderer)
        {
            m_flatEffect = new FlatEffectHelper(renderer);
            m_geometry = new Geometry<FlatVertex>(Primitive.Lines);
            m_renderGeometry = renderer.Upload(m_geometry, RenderGeometryFlags.Dynamic);
            m_cameraUniforms = new UniformBlock<CameraUniformData>(UniformBlockFlags.Dynamic);
            m_transformStack = new Stack<Matrix4>();
            m_currentTransform = Matrix4.Identity;
        }

        public void Dispose()
        {
            m_renderGeometry.Dispose();
            m_flatEffect.Dispose();
            m_cameraUniforms.Dispose();
        }

        public void BeginFrame()
        {
            m_transformStack.Clear();
            m_currentTransform = Matrix4.Identity;
            m_geometry.Clear();
            m_inFrame = true;
        }

        public void EndFrame()
        {
            m_renderGeometry.Update(m_geometry);
            m_inFrame = false;
        }

        public void PushMatrix()
        {
            m_transformStack.Push(m_currentTransform);
        }

        public void MultMatrix(Matrix4 transform)
        {
            m_currentTransform = m_currentTransform.ToWorld(transform);
        }

        public void PopMatrix()
        {
            if (m_transformStack.Count > 0)
            {
                m_currentTransform = m_transformStack.Pop();
            }
        }

        public void DrawLine(Vector3 start, Vector3 end, Colour colour)
        {
            if (m_inFrame && (m_geometry.VertexPos + 1) <= ushort.MaxValue)
            {
                int firstVertex = m_geometry.VertexPos;
                m_geometry.AddVertex(m_currentTransform.ToWorldPos(start), colour);
                m_geometry.AddVertex(m_currentTransform.ToWorldPos(end), colour);
                m_geometry.AddIndex(firstVertex);
                m_geometry.AddIndex(firstVertex + 1);
            }
        }

        public void DrawCross(Matrix4 transform, float radius, Colour colour)
        {
            PushMatrix();
            try
            {
                MultMatrix(transform);
                DrawCross(Vector3.Zero, radius, colour);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawCross(Vector3 pos, float radius, Colour colour)
        {
            DrawLine(new Vector3(pos.X, pos.Y - radius, pos.Z), new Vector3(pos.X, pos.Y + radius, pos.Z), colour);
            DrawLine(new Vector3(pos.X - radius, pos.Y, pos.Z), new Vector3(pos.X + radius, pos.Y, pos.Z), colour);
            DrawLine(new Vector3(pos.X, pos.Y, pos.Z - radius), new Vector3(pos.X, pos.Y, pos.Z + radius), colour);
        }

        public void DrawAxisMarker(Matrix4 transform, float radius)
        {
            PushMatrix();
            try
            {
                MultMatrix(transform);
                DrawLine(Vector3.Zero, Vector3.XAxis * radius, Colour.Red);
                DrawLine(Vector3.Zero, Vector3.YAxis * radius, Colour.Green);
                DrawLine(Vector3.Zero, Vector3.ZAxis * radius, Colour.Blue);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawBox(Matrix4 transform, Vector3 size, Colour colour)
        {
            DrawBox(transform, size.X, size.Y, size.Z, colour);
        }

        public void DrawBox(Matrix4 transform, float x, float y, float z, Colour colour)
        {
            float sx = -0.5f * x;
            float ex = 0.5f * x;
            float sy = -0.5f * y;
            float ey = 0.5f * y;
            float sz = -0.5f * z;
            float ez = 0.5f * z;

            PushMatrix();
            try
            {
                MultMatrix(transform);

                DrawLine(new Vector3(sx, sy, sz), new Vector3(ex, sy, sz), colour);
                DrawLine(new Vector3(sx, ey, sz), new Vector3(ex, ey, sz), colour);
                DrawLine(new Vector3(sx, sy, sz), new Vector3(sx, ey, sz), colour);
                DrawLine(new Vector3(ex, sy, sz), new Vector3(ex, ey, sz), colour);

                DrawLine(new Vector3(sx, sy, sz), new Vector3(sx, sy, ez), colour);
                DrawLine(new Vector3(ex, sy, sz), new Vector3(ex, sy, ez), colour);
                DrawLine(new Vector3(sx, ey, sz), new Vector3(sx, ey, ez), colour);
                DrawLine(new Vector3(ex, ey, sz), new Vector3(ex, ey, ez), colour);

                DrawLine(new Vector3(sx, sy, ez), new Vector3(ex, sy, ez), colour);
                DrawLine(new Vector3(sx, ey, ez), new Vector3(ex, ey, ez), colour);
                DrawLine(new Vector3(sx, sy, ez), new Vector3(sx, ey, ez), colour);
                DrawLine(new Vector3(ex, sy, ez), new Vector3(ex, ey, ez), colour);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawSphere(Vector3 center, float radius, Colour colour)
        {
            DrawSphere(Matrix4.CreateRotationX(0.5f * Mathf.PI) * Matrix4.CreateTranslation(center), radius, colour);
        }

        private void DrawHalfSphereImpl(float radius, float centerZ, float zDir, Colour colour)
        {
            var pitchSteps = 8;
            var yawSteps = 16;
            var pitchStep = (1.0f / (float)pitchSteps) * 0.5f * Mathf.PI;
            var yawStep = (1.0f / (float)yawSteps) * 2.0f * Mathf.PI;

            var pitch = 0.0f;
            var cosPitch = Mathf.Cos(pitch);
            var sinPitch = Mathf.Sin(pitch);
            for (int p = 0; p < pitchSteps; ++p)
            {
                var nextPitch = pitch + pitchStep;
                var cosNextPitch = Mathf.Cos(nextPitch);
                var sinNextPitch = Mathf.Sin(nextPitch);

                var yaw = 0.0f;
                var cosYaw = Mathf.Cos(yaw);
                var sinYaw = Mathf.Sin(yaw);
                for (int y = 0; y < yawSteps; ++y)
                {
                    var nextYaw = yaw + yawStep;
                    var cosNextYaw = Mathf.Cos(nextYaw);
                    var sinNextYaw = Mathf.Sin(nextYaw);

                    var v0 = new Vector3(radius * cosPitch * cosYaw, radius * cosPitch * sinYaw, centerZ + radius * sinPitch * zDir);
                    var v1 = new Vector3(radius * cosPitch * cosNextYaw, radius * cosPitch * sinNextYaw, centerZ + radius * sinPitch * zDir);
                    var v2 = new Vector3(radius * cosNextPitch * cosYaw, radius * cosNextPitch * sinYaw, centerZ + radius * sinNextPitch * zDir);

                    DrawLine(v0, v1, colour);
                    DrawLine(v0, v2, colour);

                    yaw = nextYaw;
                    cosYaw = cosNextYaw;
                    sinYaw = sinNextYaw;
                }

                pitch = nextPitch;
                cosPitch = cosNextPitch;
                sinPitch = sinNextPitch;
            }
        }

        private void DrawCylinderImpl(float radius, float startZ, float endZ, Colour colour)
        {
            var yawSteps = 16;
            var yawStep = (1.0f / (float)yawSteps) * 2.0f * Mathf.PI;

            var yaw = 0.0f;
            var cosYaw = Mathf.Cos(yaw);
            var sinYaw = Mathf.Sin(yaw);
            for (int y = 0; y < yawSteps; ++y)
            {
                var nextYaw = yaw + yawStep;
                var cosNextYaw = Mathf.Cos(nextYaw);
                var sinNextYaw = Mathf.Sin(nextYaw);

                var v0 = new Vector3(radius * cosYaw, radius * sinYaw, startZ);
                var v1 = new Vector3(radius * cosNextYaw, radius * sinNextYaw, startZ);
                var v2 = new Vector3(radius * cosYaw, radius * sinYaw, endZ);
                var v3 = new Vector3(radius * cosNextYaw, radius * sinNextYaw, endZ);

                DrawLine(v0, v1, colour);
                DrawLine(v2, v3, colour);
                DrawLine(v0, v2, colour);

                yaw = nextYaw;
                cosYaw = cosNextYaw;
                sinYaw = sinNextYaw;
            }
        }

        public void DrawSphere(Matrix4 transform, float radius, Colour colour)
        {
            PushMatrix();
            try
            {
                MultMatrix(transform);
                DrawHalfSphereImpl(radius, 0.0f, 1.0f, colour);
                DrawHalfSphereImpl(radius, 0.0f, -1.0f, colour);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawCylinder(Matrix4 transform, float radius, float length, Colour colour)
        {
            PushMatrix();
            try
            {
                MultMatrix(transform);
                DrawCylinderImpl(radius, -0.5f * length, 0.5f * length, colour);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawCapsule(Matrix4 transform, float radius, float length, Colour colour)
        {
            PushMatrix();
            try
            {
                MultMatrix(transform);
                DrawHalfSphereImpl(radius, -0.5f * length, -1.0f, colour);
                DrawCylinderImpl(radius, -0.5f * length, 0.5f * length, colour);
                DrawHalfSphereImpl(radius, 0.5f * length, 1.0f, colour);
            }
            finally
            {
                PopMatrix();
            }
        }

        public void DrawCone(Vector3 basePos, Vector3 tipPos, float radius, Colour colour)
        {
            var yawSteps = 16;
            var yawStep = (1.0f / (float)yawSteps) * 2.0f * Mathf.PI;

            var fwd = (basePos - tipPos).Normalise();
            var right = fwd.Cross(Vector3.YAxis).Normalise() * radius;
            var up = fwd.Cross(right).Normalise() * radius;

            var yaw = 0.0f;
            var cosYaw = Mathf.Cos(yaw);
            var sinYaw = Mathf.Sin(yaw);
            for (int y = 0; y < yawSteps; ++y)
            {
                var nextYaw = yaw + yawStep;
                var cosNextYaw = Mathf.Cos(nextYaw);
                var sinNextYaw = Mathf.Sin(nextYaw);

                var p0 = basePos + right * cosYaw + up * sinYaw;
                var p1 = basePos + right * cosNextYaw + up * sinNextYaw;
                DrawLine(tipPos, p0, colour);
                DrawLine(p0, p1, colour);

                yaw = nextYaw;
                cosYaw = cosNextYaw;
                sinYaw = sinNextYaw;
            }
        }

        public void DrawHull(Matrix4 transform, ConvexHull hull, Colour colour)
        {
            PushMatrix();
            try
            {

                MultMatrix(transform);
                var verts = new List<Vector3>();
                var indices = new List<int>();
                hull.BuildGeometry(verts, indices);
                for (int i = 0; i < indices.Count; ++i)
                {
                    int count = indices[i];
                    if (count > 0)
                    {
                        var lastIdx = indices[++i];
                        var lastVert = verts[lastIdx];
                        for (int j = 1; j < count; ++j)
                        {
                            var idx = indices[++i];
                            var vert = verts[idx];
                            DrawLine(lastVert, vert, colour);
                            lastVert = vert;
                        }
                    }
                }
            }
            finally
            {
                PopMatrix();
            }
        }

        public void Draw(IRenderer renderer, View view)
        {
            m_cameraUniforms.Data.ViewMatrix = view.Camera.ViewMatrix;
            m_cameraUniforms.Data.ProjectionMatrix = view.Camera.ProjectionMatrix;
            m_cameraUniforms.Data.CameraPosition = view.Camera.Position;
            m_cameraUniforms.Upload();
            
            m_flatEffect.ModelMatrix = Matrix4.Identity;
            m_flatEffect.CameraBlock = m_cameraUniforms;
            renderer.CurrentEffect = m_flatEffect.Instance;
            renderer.DepthTest = false;
            renderer.Draw(m_renderGeometry);
            renderer.DepthTest = true;
        }
    }
}
