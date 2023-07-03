using Dan200.Core.Assets;
using Dan200.Core.Geometry;
using Dan200.Core.Main;
using Dan200.Core.Math;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Render
{
    //    A
    //  / | \
    // D <|  B
    //  \ | /
    //    C
    internal struct Edge
    {
        public int A;
        public int B;
        public int C;
        public int D;
    }

    internal class EdgeListBuilder
    {
        private Dictionary<Pair<int, int>, int> m_wipEdges; // (A,C) -> B
        private List<Edge> m_edges;

        public EdgeListBuilder()
        {
            m_wipEdges = new Dictionary<Pair<int, int>, int>();
            m_edges = new List<Edge>();
        }

        public void Clear()
        {
            m_wipEdges.Clear();
            m_edges.Clear();
        }

        public void AddEdge(int a, int b, int c)
        {
            int d;
            var caPair = Pair.Create(c, a);
            if (m_wipEdges.TryGetValue(caPair, out d))
            {
                App.Assert(d != b);

                // Found the second half of an edge, now we can add both edge entries
                var edge = new Edge();
                edge.A = a;
                edge.B = b;
                edge.C = c;
                edge.D = d;
                m_edges.Add(edge);

                var flippedEdge = new Edge();
                flippedEdge.A = c;
                flippedEdge.B = d;
                flippedEdge.C = a;
                flippedEdge.D = b;
                m_edges.Add(flippedEdge);

                m_wipEdges.Remove(caPair);
            }
            else
            {
                // Found the first half of an edge
                var acPair = Pair.Create(a, c);
                App.Assert(!m_wipEdges.ContainsKey(acPair));
                m_wipEdges.Add(acPair, b);
            }
        }

        public void AddTriangle(int a, int b, int c)
        {
            AddEdge(a, b, c);
            AddEdge(b, c, a);
            AddEdge(c, a, b);
        }

        public List<Edge> Finish()
        {
            // Finish all the half-edges by folding them in on themselves
            var edges = m_edges;
            foreach (var pair in m_wipEdges)
            {
                var edge = new Edge();
                edge.A = pair.Key.First;
                edge.B = pair.Value;
                edge.C = pair.Key.Second;
                edge.D = pair.Value;
                edges.Add(edge);
            }
            m_wipEdges.Clear();
            return edges;
        }
    }

    internal class Model : IBasicAsset
    {
        public const int MAX_GROUPS = 16;

        public static Model Get(string path)
        {
            return Assets.Assets.Get<Model>(path);
        }

        private class GroupData
        {
            public string Name;
            public string MaterialName;
            public int FirstIndex;
            public int IndexCount;
            public int FirstShadowIndex;
            public int ShadowIndexCount;
            public AABB BoundingBox;
            public Sphere BoundingSphere;
        }

        private class ModelData
        {
            public string MaterialFilePath;
            public Geometry<ModelVertex> Geometry;
            public Geometry<ShadowVertex> ShadowGeometry;
            public AABB BoundingBox;
            public Sphere BoundingSphere;
            public List<GroupData> Groups;
        }

        private string m_path;
        private Dictionary<string, int> m_groupLookup;
        private ModelData m_data;
        private IRenderGeometry<ModelVertex> m_geometry;
        private IRenderGeometry<ShadowVertex> m_shadowGeometry;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public int GroupCount
        {
            get
            {
                return m_data.Groups.Count;
            }
        }

        public AABB BoundingBox
        {
            get
            {
                return m_data.BoundingBox;
            }
        }

        public Sphere BoundingSphere
        {
            get
            {
                return m_data.BoundingSphere;
            }
        }

        public static object LoadData(Stream stream, string path)
        {
            // Parse the file
            var reader = new StreamReader(stream, Encoding.UTF8);
            var objFile = new OBJFile();
            objFile.Parse(reader);

            // Sort the groups in the file by material for easier rendering
            objFile.Groups.Sort(
                (a, b) => string.Compare(a.MaterialName, b.MaterialName, StringComparison.Ordinal)
            );

            // Parse the file
            var data = new ModelData();
            var dir = AssetPath.GetDirectoryName(path);
            data.MaterialFilePath = AssetPath.Combine(dir, objFile.MTLLib);
            data.Groups = new List<GroupData>();
            data.BoundingBox = AABB.Zero;
            data.BoundingSphere = Sphere.Zero;
            data.Geometry = new Geometry<ModelVertex>(Primitive.Triangles);
            data.ShadowGeometry = new Geometry<ShadowVertex>(Primitive.Triangles);
            foreach (var objGroup in objFile.Groups)
            {
                // Skip empty groups
                if (objGroup.Faces.Count == 0)
                {
                    continue;
                }

                // Check the group limit
                if (data.Groups.Count >= MAX_GROUPS)
                {
                    throw new InvalidDataException("Too many groups! Limit is " + MAX_GROUPS);
                }

                // Build the geometry
                byte groupIndex = (byte)data.Groups.Count;
                int firstGroupIndex = data.Geometry.IndexPos;
                int firstGroupVertex = data.Geometry.VertexPos;
                for (int i = 0; i < objGroup.Faces.Count; ++i)
                {
                    var objFace = objGroup.Faces[i];
                    if (objFace.VertexCount >= 3)
                    {
                        UnitVector3 faceNormal;
                        GetFaceInfo(objFile, objFace, out faceNormal);

                        // Add the verts
                        int firstVertIndex = data.Geometry.VertexPos;
                        for (int j = 0; j < objFace.VertexCount; ++j)
                        {
                            Vector3 pos;
                            UnitVector3? norm;
                            Vector2 texCoord;
                            GetVertInfo(objFile, objFace.FirstVertex + j, out pos, out texCoord, out norm);
                            data.Geometry.AddVertex(groupIndex, pos, norm.HasValue ? norm.Value : faceNormal, texCoord);
                        }

                        // Add the indexes (unwrap faces using a fan pattern)
                        for (int k = 2; k < objFace.VertexCount; ++k)
                        {
                            data.Geometry.AddIndex(firstVertIndex);
                            data.Geometry.AddIndex(firstVertIndex + k - 1);
                            data.Geometry.AddIndex(firstVertIndex + k);
                        }
                    }
                }
                int groupIndexCount = data.Geometry.IndexPos - firstGroupIndex;
                int groupVertexCount = data.Geometry.VertexPos - firstGroupVertex;

                // Build the shadow geometry:
                int firstGroupShadowIndex = data.ShadowGeometry.IndexPos;

                // Find all the edges
                var edgeBuilder = new EdgeListBuilder();
                for (int i = 0; i < objGroup.Faces.Count; ++i)
                {
                    var objFace = objGroup.Faces[i];
                    if (objFace.VertexCount >= 3)
                    {
                        int firstVertex = objFace.FirstVertex;
                        int vertexCount = objFace.VertexCount;
                        for (int j = 0; j < vertexCount; ++j)
                        {
                            edgeBuilder.AddEdge(
                                objFile.Verts[ firstVertex + j ].PositionIndex,
                                objFile.Verts[ firstVertex + (j + 2) % vertexCount ].PositionIndex,
                                objFile.Verts[ firstVertex + (j + 1) % vertexCount ].PositionIndex
                            );
                        }
                        /*
                        for (int j = 2; j < vertexCount; ++j)
                        {
                            edgeBuilder.AddTriangle(
                                objFile.Verts[firstVertex].PositionIndex,
                                objFile.Verts[firstVertex + j - 1].PositionIndex,
                                objFile.Verts[firstVertex + j].PositionIndex
                            );
                        }
                        */
                    }
                }

                // Add all the edge verts
                var edges = edgeBuilder.Finish();
                foreach(var edge in edges)
                {
                    Vector3 posA = objFile.Positions[edge.A - 1];
                    Vector3 posB = objFile.Positions[edge.B - 1];
                    Vector3 posC = objFile.Positions[edge.C - 1];
                    Vector3 posD = objFile.Positions[edge.D - 1];
                    posA.Z = -posA.Z;
                    posB.Z = -posB.Z;
                    posC.Z = -posC.Z;
                    posD.Z = -posD.Z;

                    // Add the edge verts
                    int firstVertex = data.ShadowGeometry.VertexPos;
                    data.ShadowGeometry.AddVertex(posA, posB, posC, posD, groupIndex, posA, 0.0f);
                    data.ShadowGeometry.AddVertex(posA, posB, posC, posD, groupIndex, posC, 0.0f);

                    int firstPushedVertex = data.ShadowGeometry.VertexPos;
                    data.ShadowGeometry.AddVertex(posA, posB, posC, posD, groupIndex, posA, 1.0f);
                    data.ShadowGeometry.AddVertex(posA, posB, posC, posD, groupIndex, posC, 1.0f);

                    // Add the edge triangles
                    data.ShadowGeometry.AddIndex(firstVertex);
                    data.ShadowGeometry.AddIndex(firstPushedVertex);
                    data.ShadowGeometry.AddIndex(firstVertex + 1);

                    data.ShadowGeometry.AddIndex(firstPushedVertex);
                    data.ShadowGeometry.AddIndex(firstPushedVertex + 1);
                    data.ShadowGeometry.AddIndex(firstVertex + 1);
                }
                int groupShadowIndexCount = data.ShadowGeometry.IndexPos - firstGroupShadowIndex;

                // Build the AABB
                var vert0 = data.Geometry.GetVertex(firstGroupVertex).Position;
                var boundingBox = new AABB(vert0, vert0);
                var boundingSphere = new Sphere(vert0, 0.0f);
                for (int i = 1; i < groupVertexCount; ++i)
                {
                    var vert = data.Geometry.GetVertex(firstGroupVertex + i).Position;
                    boundingBox.ExpandToFit(vert);
                    boundingSphere.MoveAndExpandToFit(vert);
                }

                // Prepare the group
                var group = new GroupData();
                group.Name = objGroup.Name;
                group.MaterialName = objGroup.MaterialName;
                group.BoundingBox = boundingBox;
                group.BoundingSphere = boundingSphere;
                group.FirstIndex = firstGroupIndex;
                group.IndexCount = groupIndexCount;
                group.FirstShadowIndex = firstGroupShadowIndex;
                group.ShadowIndexCount = groupShadowIndexCount;
                data.Groups.Add(group);

                // Expand the model AABB
                if (data.Groups.Count == 1)
                {
                    data.BoundingBox = boundingBox;
                    data.BoundingSphere = boundingSphere;
                }
                else
                {
                    data.BoundingBox.ExpandToFit(boundingBox);
                    data.BoundingSphere.MoveAndExpandToFit(boundingSphere);
                }
            }

            // Return the data
            return data;
        }

        public Model(string path, object data)
        {
            m_path = path;
            m_groupLookup = new Dictionary<string, int>();
            Load(data);
        }

        public void Reload(object data)
        {
            Unload();
            Load(data);
        }

        public void Dispose()
        {
            Unload();
        }

        private ConvexHull? BuildHull(OBJFile file, OBJFile.OBJGroup group)
        {
            var planes = new List<Plane>();
            foreach(var face in group.Faces)
            {
                if(face.VertexCount < 3)
                {
                    continue;
                }

                Vector3 pos0;
                UnitVector3? norm0;
                Vector2 texCoord0;
                GetVertInfo(file, face.FirstVertex, out pos0, out texCoord0, out norm0);

                UnitVector3 faceNormal;
                GetFaceInfo(file, face, out faceNormal);

                bool redundant = false;
                foreach (var otherPlane in planes)
                {
                    if(otherPlane.Normal == faceNormal)
                    {
                        redundant = true;
                        break;
                    }
                }
                if(!redundant)
                {
                    float distance = faceNormal.Dot(pos0);
                    planes.Add(new Plane(faceNormal, distance));
                }
            }
            if(planes.Count >= 4)
            {                
                var hull = new ConvexHull(planes.ToArray());
                hull.Optimise();
                return hull;
            }
            return null;
        }

        private void Load(object data)
        {
            // Store the data
            var modelData = (ModelData)data;
            m_data = modelData;
            m_groupLookup.Clear();
            for(int i=0; i<m_data.Groups.Count; ++i)
            {
                var group = m_data.Groups[i];
                m_groupLookup.Add(group.Name, i);
            }

            // Upload the geometry
            m_geometry = OpenGL.OpenGLRenderer.Instance.Upload( modelData.Geometry, RenderGeometryFlags.Default ); // TODO: Make generic
            m_shadowGeometry = OpenGL.OpenGLRenderer.Instance.Upload( modelData.ShadowGeometry, RenderGeometryFlags.Default ); // TODO: Make generic
        }

        private void Unload()
        {
            m_geometry.Dispose();
            m_shadowGeometry.Dispose();
        }

        public int GetGroupIndex(string name)
        {
            if (m_groupLookup.ContainsKey(name))
            {
                return m_groupLookup[name];
            }
            return -1;
        }

        public string GetGroupName(int groupIndex)
        {
            return m_data.Groups[groupIndex].Name;
        }

        public AABB GetGroupBoundingBox(int groupIndex)
        {
            return m_data.Groups[groupIndex].BoundingBox;
        }

		public IMaterial GetGroupMaterial(int groupIndex)
		{
			var materialFile = MaterialFile.Get(m_data.MaterialFilePath);
			return materialFile.GetMaterial(m_data.Groups[groupIndex].MaterialName);
		}

        public void Draw(IRenderer renderer, ModelEffectHelper effect, Matrix4[] transforms, bool[] visibility, Matrix3[] uvTransforms, ColourF[] colours, IMaterial[] materialOverrides)
        {
            // Set shared parameters
            effect.ModelMatrices = transforms;
            effect.UVMatrices = uvTransforms;
            effect.Colours = colours;

            // Draw
            var materialFile = MaterialFile.Get(m_data.MaterialFilePath);
            for (int i = 0; i < m_data.Groups.Count; ++i)
            {
                var group = m_data.Groups[i];
                if (visibility[i])
                {
                    // Set per-material parameters
                    var material = materialOverrides[i];
                    if (material == null)
                    {
                        material = GetGroupMaterial(i);
                    }
                    effect.Material = material;

                    // Draw this group and any subsequent ones that share the same material
                    int indexCount = group.IndexCount;
                    while (i < m_data.Groups.Count - 1 && visibility[i + 1])
                    {
                        var nextGroup = m_data.Groups[i + 1];
                        var nextGroupMaterial = materialOverrides[i + 1];
                        if (nextGroupMaterial == null)
                        {
                            nextGroupMaterial = materialFile.GetMaterial(nextGroup.MaterialName);
                        }
                        if (nextGroupMaterial == material)
                        {
                            indexCount += nextGroup.IndexCount;
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    renderer.DrawRange(m_geometry, group.FirstIndex, indexCount);
                }
            }
        }

        public void DrawShadows(IRenderer renderer, ModelShadowEffectHelper effect, Matrix4[] transforms, bool[] visibility)
        {
            // Set shared parameters
            effect.ModelMatrices = transforms;

			// Draw
			for (int i = 0; i < m_data.Groups.Count; ++i)
			{
				var group = m_data.Groups[i];
				if (visibility[i])
				{
					// Draw this group and all subsequent visible groups
					int indexCount = group.ShadowIndexCount;
					while (i < m_data.Groups.Count - 1 && visibility[i + 1])
					{
                        var nextGroup = m_data.Groups[i + 1];
                        indexCount += nextGroup.ShadowIndexCount;
						i++;
					}
					renderer.DrawRange(m_shadowGeometry, group.FirstShadowIndex, indexCount);
				}
			}
        }

        private static void GetVertPos(OBJFile file, int vertexIndex, out Vector3 o_pos)
        {
            var vertex = file.Verts[vertexIndex];
            var objPos = file.Positions[vertex.PositionIndex - 1];
            o_pos = new Vector3(objPos.X, objPos.Y, -objPos.Z);
        }

        private static void GetVertInfo(OBJFile file, int vertexIndex, out Vector3 o_pos, out Vector2 o_texCoord, out UnitVector3? o_normal)
        {
            var vertex = file.Verts[vertexIndex];
            var objPos = file.Positions[vertex.PositionIndex - 1];
            o_pos = new Vector3(objPos.X, objPos.Y, -objPos.Z);

            if (vertex.TexCoordIndex > 0)
            {
                var objTexCoord = file.TexCoords[vertex.TexCoordIndex - 1];
                o_texCoord = new Vector2(objTexCoord.X, 1.0f - objTexCoord.Y);
            }
            else
            {
                o_texCoord = new Vector2(0.5f, 0.5f);
            }

            if (vertex.NormalIndex > 0)
            {
                var objNorm = file.Normals[vertex.NormalIndex - 1];
                o_normal = UnitVector3.ConstructUnsafe(objNorm.X, objNorm.Y, -objNorm.Z);
            }
            else
            {
                o_normal = null;
            }
        }

        private static void GetFaceInfo(OBJFile file, OBJFile.OBJFace face, out UnitVector3 o_normal)
        {
            int firstVertex = face.FirstVertex;
            int vertexCount = face.VertexCount;
            App.Assert(vertexCount >= 3);

            var v0 = Vector3.Zero;
            var vn = Vector3.Zero;
            var pos0 = file.Positions[file.Verts[firstVertex].PositionIndex - 1];
            for (int i = 1; i < vertexCount; ++i)
            {
                var pos = file.Positions[file.Verts[firstVertex + i].PositionIndex - 1];
                if(v0.LengthSquared == 0.0f)
                {
                    v0 = pos - pos0;
                }
                else
                {
                    var v1 = pos - pos0;
                    vn = v0.Cross(v1);
                    if(vn.LengthSquared > 0.0f)
                    {
                        break;
                    }
                }
            }

            var normal = vn.Normalise();
            o_normal = UnitVector3.ConstructUnsafe(normal.X, normal.Y, -normal.Z); ;
        }
    }
}
