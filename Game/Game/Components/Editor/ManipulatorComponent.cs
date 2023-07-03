using System;
using Dan200.Core.Components.Core;
using Dan200.Core.Geometry;
using Dan200.Core.Input;
using Dan200.Core.Interfaces;
using Dan200.Core.Level;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Render;
using Dan200.Core.Systems;
using Dan200.Core.Util;

namespace Dan200.Game.Components.Editor
{
    internal struct ManipulatorComponentData
    {
    }

    [RequireSystem(typeof(GUISystem))]
    [RequireComponent(typeof(TransformComponent))]
    [RequireComponent(typeof(EditorComponent))]
    internal class ManipulatorComponent : Component<ManipulatorComponentData>
    {
        private GUISystem m_gui;
        private TransformComponent m_transform;
        private EditorComponent m_editor;

        private enum CoordinateSpace
        {
            Local,
            Global,
        }
        private CoordinateSpace m_coordinateSpace;

        private enum DragState
        {
            None,
            Plane,
            Axis,
        }
        private DragState m_dragState;
        private Plane m_dragPlane;
        private Vector3 m_dragAxis;
        private Vector3 m_lastDragPosition;

        protected override void OnInit(in ManipulatorComponentData properties)
        {
            m_gui = Level.GetSystem<GUISystem>();
            m_transform = Entity.GetComponent<TransformComponent>();
            m_editor = Entity.GetComponent<EditorComponent>();
            m_coordinateSpace = CoordinateSpace.Local;
        }

        protected override void OnShutdown()
        {
        }

        private bool ProjectOntoAxis(Vector2 screenPosition, Camera camera, Vector3 origin, Vector3 dir, out Vector3 o_posOnAxis)
        {
            var right = camera.Transform.Forward.Cross(dir);
            var norm = dir.Cross(right);
            if (norm.Length > 0.0f)
            {
                var plane = new Plane(norm.Normalise(), origin);
                return ProjectOntoPlane(screenPosition, camera, plane, out o_posOnAxis);
            }
            o_posOnAxis = default(Vector3);
            return false;
        }

        private bool ProjectOntoPlane(Vector2 screenPosition, Camera camera, Plane plane, out Vector3 o_posOnPlane)
        {
            // Convert screen coords to camera space direction
            float aspect = m_gui.Screen.Width / m_gui.Screen.Height;
            float x = (screenPosition.X / (0.5f * m_gui.Screen.Width)) - 1.0f;
            float y = (screenPosition.Y / (0.5f * m_gui.Screen.Height)) - 1.0f;

            var dirCS = new Vector3(
                (float)(Math.Tan(0.5f * camera.FOV)) * (x * aspect),
                -(float)(Math.Tan(0.5f * camera.FOV)) * y,
                1.0f
            ).Normalise();

            // Convert camera space direction to world space ray
            var pos = camera.Transform.Position;
            var dir = camera.Transform.ToWorldDir(dirCS);

            // Project vector onto plane
            var distanceAbovePlane = pos.Dot(plane.Normal) - plane.DistanceFromOrigin;
            var dirDot = dir.Dot(-plane.Normal);
            if (dirDot != 0.0f && Mathf.Sign(dirDot) == Mathf.Sign(distanceAbovePlane))
            {
                var posOnPlane = pos + dir * (distanceAbovePlane / dirDot);
                o_posOnPlane = posOnPlane;
                return true;
            }

            o_posOnPlane = default(Vector3);
            return false;
        }

        public bool HandleMouseInput(IMouse mouse, Camera camera)
        {
            var transform = m_transform.Transform;
            if(m_coordinateSpace == CoordinateSpace.Global)
            {
                transform.Rotation = Matrix3.Identity;
            }

            var distance = (transform.Position - camera.Transform.Position).Length;
            var screenHeightAtDistance = 2.0f * (distance * Mathf.Tan(camera.FOV * 0.5f));
            var radius = 0.25f * screenHeightAtDistance;
            var tabRadius = radius * 0.33f;
            var axisThickness = radius * 0.1f;

            App.DebugDraw.PushMatrix();
            App.DebugDraw.MultMatrix(transform);

            App.DebugDraw.DrawLine(Vector3.Zero, new Vector3(radius, 0.0f, 0.0f), Colour.Red);
            App.DebugDraw.DrawLine(new Vector3(0.0f, tabRadius, 0.0f), new Vector3(0.0f, tabRadius, tabRadius), Colour.Red);
            App.DebugDraw.DrawLine(new Vector3(0.0f, 0.0f, tabRadius), new Vector3(0.0f, tabRadius, tabRadius), Colour.Red);

            App.DebugDraw.DrawLine(Vector3.Zero, new Vector3(0.0f, radius, 0.0f), Colour.Green);
            App.DebugDraw.DrawLine(new Vector3(tabRadius, 0.0f, 0.0f), new Vector3(tabRadius, 0.0f, tabRadius), Colour.Green);
            App.DebugDraw.DrawLine(new Vector3(0.0f, 0.0f, tabRadius), new Vector3(tabRadius, 0.0f, tabRadius), Colour.Green);

            App.DebugDraw.DrawLine(Vector3.Zero, new Vector3(0.0f, 0.0f, radius), Colour.Blue);
            App.DebugDraw.DrawLine(new Vector3(tabRadius, 0.0f, 0.0f), new Vector3(tabRadius, tabRadius, 0.0f), Colour.Blue);
            App.DebugDraw.DrawLine(new Vector3(0.0f, tabRadius, 0.0f), new Vector3(tabRadius, tabRadius, 0.0f), Colour.Blue);

            App.DebugDraw.PopMatrix();

            var left = mouse.GetInput(MouseButton.Left);
            var mousePos = m_gui.Screen.WindowToScreen(mouse.Position);
            if (left.Pressed)
            {
                var xAxis = transform.Right;
                var yAxis = transform.Up;
                var zAxis = transform.Forward;
                var yzPlane = new Plane(xAxis, transform.Position);
                var xzPlane = new Plane(yAxis, transform.Position);
                var xyPlane = new Plane(zAxis, transform.Position);
                Vector3 posClicked;

                // Axes
                if (ProjectOntoAxis(mousePos, camera, transform.Position, xAxis, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.X >= 0.0f && localPos.X <= radius &&
                        new Vector2(localPos.Y, localPos.Z).Length <= axisThickness)
                    {
                        m_dragState = DragState.Axis;
                        m_dragAxis = xAxis;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }

                if (ProjectOntoAxis(mousePos, camera, transform.Position, yAxis, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.Y >= 0.0f && localPos.Y <= radius &&
                        new Vector2(localPos.X, localPos.Z).Length <= axisThickness)
                    {
                        m_dragState = DragState.Axis;
                        m_dragAxis = yAxis;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }

                if (ProjectOntoAxis(mousePos, camera, transform.Position, zAxis, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.Z >= 0.0f && localPos.Z <= radius &&
                        new Vector2(localPos.X, localPos.Y).Length <= axisThickness)
                    {
                        m_dragState = DragState.Axis;
                        m_dragAxis = zAxis;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }

                // Planes
                if (ProjectOntoPlane(mousePos, camera, yzPlane, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.Y >= 0.0f && localPos.Y <= tabRadius &&
                        localPos.Z >= 0.0f && localPos.Z <= tabRadius)
                    {
                        m_dragState = DragState.Plane;
                        m_dragPlane = yzPlane;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }

                if (ProjectOntoPlane(mousePos, camera, xzPlane, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.X >= 0.0f && localPos.X <= tabRadius &&
                        localPos.Z >= 0.0f && localPos.Z <= tabRadius)
                    {
                        m_dragState = DragState.Plane;
                        m_dragPlane = xzPlane;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }

                if (ProjectOntoPlane(mousePos, camera, xyPlane, out posClicked))
                {
                    var localPos = transform.ToLocalPos(posClicked);
                    if (localPos.X >= 0.0f && localPos.X <= tabRadius &&
                        localPos.Y >= 0.0f && localPos.Y <= tabRadius)
                    {
                        m_dragState = DragState.Plane;
                        m_dragPlane = xyPlane;
                        m_lastDragPosition = posClicked;
                        return true;
                    }
                }
            }
            else if(left.Held)
            {
                if (m_dragState == DragState.Axis)
                {
                    Vector3 posClicked;
                    if (ProjectOntoAxis(mousePos, camera, m_lastDragPosition, m_dragAxis, out posClicked))
                    {
                        Move(m_dragAxis * (posClicked - m_lastDragPosition).Dot(m_dragAxis));
                        m_lastDragPosition = posClicked;
                    }
                    return true;
                }
                else if (m_dragState == DragState.Plane)
                {
                    Vector3 posClicked;
                    if (ProjectOntoPlane(mousePos, camera, m_dragPlane, out posClicked))
                    {
                        Move(posClicked - m_lastDragPosition);
                        m_lastDragPosition = posClicked;
                    }
                    return true;
                }
            }
            else if(left.Released)
            {
                if (m_dragState != DragState.None)
                {
                    m_dragState = DragState.None;
                    return true;
                }
            }
            return false;
        }

        private void Move(Vector3 delta)
        {
            var transform = m_transform.Transform;
            transform.Position += delta;
            ApplyTransform(transform);
        }

        private void ApplyTransform(in Matrix4 transform)
        {
            // TODO: Unhardcode these property names
            m_transform.Transform = transform;
            if (m_editor.Prefab.Properties.ContainsKey("Position"))
            {
                m_editor.Properties["Position"] = m_transform.LocalPosition.ToLuaValue();
            }
            if (m_editor.Prefab.Properties.ContainsKey("Rotation"))
            {
                var angles = m_transform.LocalTransform.GetRotationAngles() * Mathf.RADIANS_TO_DEGREES;
                m_editor.Properties["Rotation"] = angles.ToLuaValue();
            }
            m_editor.ReInit();
        }
    }
}
