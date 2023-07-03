using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using Dan200.Core.Math;

#if GLES
using OpenTK.Graphics.ES20;
using BufferUsageHint = OpenTK.Graphics.ES20.BufferUsage;
using VertexAttribIPointerType = OpenTK.Graphics.ES20.VertexAttribPointerType;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Dan200.Core.Render
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class VertexAttributeAttribute : Attribute
    {
        public string Name;

        public VertexAttributeAttribute(string name)
        {
            Name = name;
        }
    }

    internal class VertexLayout
    {
        private class VertexLayoutCache<TVertex> where TVertex : struct, IVertex
        {
			public static readonly VertexLayout Instance = new VertexLayout(typeof(TVertex));
        }

        public static VertexLayout Get<TVertex>() where TVertex : struct, IVertex
        {
            return VertexLayoutCache<TVertex>.Instance;
        }

        internal struct Entry
        {
            public string Name;
            public IntPtr Offset;
            public VertexAttribPointerType GLElementType;
            public int ElementCount;
            public bool Normalised;
        }

        public readonly int Stride;
        public readonly Entry[] Entries;

        private VertexLayout(Type type)
        {
            if (type.StructLayoutAttribute.Value == LayoutKind.Auto || !type.IsValueType)
            {
                throw new InvalidDataException(string.Format(
                    "Invalid layout on vertex type {0}. Vertex types must use [StructLayout(LayoutType.Sequential)] or [StructLayout(LayoutType.Explicit)]",
                    type.Name
                ));
            }

            Stride = Marshal.SizeOf(type);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var entries = new List<Entry>(fields.Length);
            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<VertexAttributeAttribute>();
                if (attribute != null)
                {
                    var entry = new Entry();
                    entry.Name = attribute.Name;
                    entry.Offset = Marshal.OffsetOf(type, field.Name);
                    if (field.FieldType == typeof(float))
                    {
                        entry.GLElementType = VertexAttribPointerType.Float;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(Vector2) || field.FieldType == typeof(UnitVector2))
                    {
                        entry.GLElementType = VertexAttribPointerType.Float;
                        entry.ElementCount = 2;
                    }
                    else if (field.FieldType == typeof(Vector3) || field.FieldType == typeof(UnitVector3))
                    {
                        entry.GLElementType = VertexAttribPointerType.Float;
                        entry.ElementCount = 3;
                    }
                    else if (field.FieldType == typeof(Vector4) || field.FieldType == typeof(UnitVector4))
                    {
                        entry.GLElementType = VertexAttribPointerType.Float;
                        entry.ElementCount = 4;
                    }
                    else if (field.FieldType == typeof(Colour))
                    {
                        entry.GLElementType = VertexAttribPointerType.UnsignedByte;
                        entry.ElementCount = 4;
                        entry.Normalised = true;
                    }
                    else if (field.FieldType == typeof(byte))
                    {
                        entry.GLElementType = VertexAttribPointerType.UnsignedByte;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(sbyte))
                    {
						entry.GLElementType = VertexAttribPointerType.Byte;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(short))
                    {
                        entry.GLElementType = VertexAttribPointerType.Short;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(ushort))
                    {
						entry.GLElementType = VertexAttribPointerType.UnsignedShort;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        entry.GLElementType = VertexAttribPointerType.Int;
                        entry.ElementCount = 1;
                    }
                    else if (field.FieldType == typeof(uint))
                    {
						entry.GLElementType = VertexAttribPointerType.UnsignedInt;
                        entry.ElementCount = 1;
                    }
                    else
                    {
                        throw new InvalidDataException(string.Format(
                            "Unsupported type {0} for VertexAttribute field {1} on type {2}",
                            field.FieldType.Name, field.Name, type.Name
                        ));
                    }
                    entries.Add(entry);
                }
            }
            Entries = entries.ToArray();
        }
    }
}
