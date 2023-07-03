using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Dan200.Core.Math;
using Dan200.Core.Render.OpenGL;

namespace Dan200.Core.Render
{
    internal class UniformBlockLayout
    {
        private class UniformBlockLayoutCache<TUniformData> where TUniformData : struct, IUniformData
        {
            public static readonly UniformBlockLayout Instance = new UniformBlockLayout(typeof(TUniformData));
        }

        public static UniformBlockLayout Get<TUniformData>() where TUniformData : struct, IUniformData
        {
            return UniformBlockLayoutCache<TUniformData>.Instance;
        }

        internal struct Entry
        {
            public int SrcOffset;
            public int DstOffset;
            public int Size;
        }

        public bool IsIdentity
        {
            get
            {
                return
                    Entries.Length == 1 &&
                    Entries[0].SrcOffset == 0 &&
                    Entries[0].DstOffset == 0 &&
                    SrcSize == DstSize;
            }
        }

        public readonly int SrcSize;
        public readonly int DstSize;
        public readonly Entry[] Entries;

        private UniformBlockLayout(Type type)
        {
            if (type.StructLayoutAttribute.Value == LayoutKind.Auto || !type.IsValueType)
            {
                throw new InvalidDataException(string.Format(
                    "Invalid layout on uniform block type {0}. Uniform block types must use [StructLayout(LayoutType.Sequential)] or [StructLayout(LayoutType.Explicit)]",
                    type.Name
                ));
            }

            // Get fields
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length == 0)
            {
                // No fields: just upload a one byte buffer
                DstSize = 1;
                Entries = new Entry[0];
            }
            else
            {
                // Fields exist: measure them
                var entries = new List<Entry>(fields.Length);
                var dstPos = 0;
                foreach (var field in fields)
                {
                    var fieldOffset = (int)Marshal.OffsetOf(type, field.Name);
                    if (field.FieldType == typeof(Matrix3))
                    {
                        // Matrix3 has padding on each row, so we need to split it up
                        var entry = new Entry();
                        entry.SrcOffset = fieldOffset;
                        entry.DstOffset = dstPos;
                        entry.Size = 12;
                        entries.Add(entry);

                        var entry2 = new Entry();
                        entry2.SrcOffset = fieldOffset + 12;
                        entry2.DstOffset = dstPos + 16;
                        entry2.Size = 12;
                        entries.Add(entry2);

                        var entry3 = new Entry();
                        entry3.SrcOffset = fieldOffset + 24;
                        entry3.DstOffset = dstPos + 32;
                        entry3.Size = 12;
                        entries.Add(entry3);
                    }
                    else
                    {
                        // Other types can be blitted in one block
                        var entry = new Entry();
                        entry.SrcOffset = fieldOffset;
                        entry.DstOffset = dstPos;
                        entry.Size = (int)Marshal.SizeOf(field.FieldType);
                        entries.Add(entry);
                    }
                    dstPos += GLUtils.Std140SizeOf(field.FieldType);
                }

                // Merge consecutive entries
                var previous = entries[0];
                for (int i = 1; i < entries.Count; ++i)
                {
                    var current = entries[i];
                    if ((current.SrcOffset - previous.SrcOffset) ==
                        (current.DstOffset - previous.DstOffset))
                    {
                        previous.Size = (current.DstOffset + current.Size) - previous.DstOffset;
                        entries[i - 1] = previous;
                        entries.RemoveAt(i);
                        --i;
                    }
                    else
                    {
                        previous = current;
                    }
                }

                // Store the entries
                SrcSize = Marshal.SizeOf(type);
                DstSize = dstPos;
                Entries = entries.ToArray();
            }
        }
    }
}
