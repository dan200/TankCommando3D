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
    internal class Map : IBasicAsset
    {
        public static Map Get(string path)
        {
            return Assets.Assets.Get<Map>(path);
        }

        public struct DrawCall
        {
            public string TexturePath;
            public int FirstIndex;
            public int IndexCount;
        }

        private class EntityData
        {
            public Dictionary<string, string> Properties;
            public List<ConvexHull> BrushHulls;
            public List<AABB> BrushBoundingBoxes;
            public List<DrawCall> DrawCalls;
        }

        private class MapData
        {
            public List<EntityData> Entities;
            public Geometry<MapVertex> Geometry;
        }

        private string m_path;
        private List<EntityData> m_entities;
        private IRenderGeometry<MapVertex> m_geometry;

        public string Path
        {
            get
            {
                return m_path;
            }
        }       

        public int EntityCount
        {
            get
            {
                return m_entities.Count;
            }
        }

        private const float MAP_UNIT_SIZE = 0.0254f; // 1 inch

        private static Vector3 MapToGame(Vector3 v)
        {
            return new Vector3(v.X, v.Z, v.Y) * MAP_UNIT_SIZE;
        }

        private static UnitVector3 MapToGame(UnitVector3 v)
        {
            return UnitVector3.ConstructUnsafe(v.X, v.Z, v.Y);
        }

        private static int CompareByTexture(DrawCall a, DrawCall b)
        {
            return a.TexturePath.CompareTo(b.TexturePath);
        }

        public static object LoadData(Stream stream, string path)
        {
            // Parse the file
            var mapFile = new MAPFile();
            mapFile.Parse(stream);

            // Determine the WAD file used
            string wadName = "";
            foreach(var entity in mapFile.Entities)
            {
                string classname;
                if(entity.Properties.TryGetValue("classname", out classname) && classname == "worldspawn")
                {
                    string wadPath;
                    if(entity.Properties.TryGetValue("wad", out wadPath) && wadPath.Length > 0)
                    {
                        int slashIndex = wadPath.LastIndexOf('\\');
                        int dotIndex = wadPath.LastIndexOf('.');
                        slashIndex = (slashIndex >= 0) ? slashIndex : 0;
                        dotIndex = (dotIndex > slashIndex) ? dotIndex : (wadPath.Length - 1);
                        App.Assert(dotIndex > slashIndex);
                        wadName = wadPath.Substring(slashIndex + 1, dotIndex - (slashIndex + 1));
                    }
                    break;
                }
            }

            // Create the map data
            var mapData = new MapData();
            mapData.Entities = new List<EntityData>();
            mapData.Geometry = new Geometry<MapVertex>(Primitive.Triangles);

            // For each entity...
            var points = new List<Vector3>();
            var polygons = new List<int>();
            var unsortedDrawCalls = new List<DrawCall>();
            foreach (var entity in mapFile.Entities)
            {
                // Create the entity data
                var entityData = new EntityData();
                entityData.Properties = entity.Properties;
                entityData.BrushHulls = new List<ConvexHull>(entity.Brushes.Count);
                entityData.BrushBoundingBoxes = new List<AABB>(entity.Brushes.Count);
                entityData.DrawCalls = new List<DrawCall>();

                // For each brush...
                foreach (var brush in entity.Brushes)
                {
                    // Create the hull
                    var planes = new Plane[brush.Faces.Count];
                    for(int i=0; i<brush.Faces.Count; ++i)
                    {
                        var face = brush.Faces[i];
                        var pos0 = MapToGame( face.Pos0 );
                        var pos1 = MapToGame( face.Pos1 );
                        var pos2 = MapToGame( face.Pos2 );
                        var planeNormal = (pos1 - pos0).Cross(pos2 - pos0).Normalise();
                        var distance = pos0.Dot(planeNormal);
                        planes[i] = new Plane(planeNormal, distance);
                    }
                    var hull = new ConvexHull(planes);
                    entityData.BrushHulls.Add(hull);

                    // Calculate face geometry
                    hull.BuildGeometry(points, polygons);
                    App.Assert(points.Count >= 4);
                    App.Assert(polygons.Count >= brush.Faces.Count);

                    // Store the hull and AABB in world space
                    var aabb = new AABB(points[0], points[0]);
                    for(int i=1; i<points.Count; ++i)
                    {
                        aabb.ExpandToFit(points[i]);
                    }
                    App.Assert(aabb.Size.X > 0.0f && aabb.Size.Y > 0.0f && aabb.Size.Z > 0.0f);
                    App.Assert(aabb.Volume > 0.0f);
                    entityData.BrushBoundingBoxes.Add(aabb);

                    // For each face...
                    int faceIdx = 0;
                    for (int pos = 0; pos < polygons.Count; ++pos)
                    {
                        App.Assert(faceIdx < brush.Faces.Count);
                        int vertexCount = polygons[pos];
                        var face = brush.Faces[faceIdx];
                        if (vertexCount > 0 && face.TextureName != "null")
                        {
                            App.Assert(vertexCount >= 3);
                            var uAxis = MapToGame(face.UAxis);
                            var vAxis = MapToGame(face.VAxis);
                            var faceNormal = planes[faceIdx].Normal;

                            // Add the verts
                            int firstVertex = mapData.Geometry.VertexCount;
                            for (int j = 1; j <= vertexCount; ++j)
                            {
                                var position = points[polygons[pos + j]];
                                var uv = new Vector2(position.Dot(uAxis), position.Dot(vAxis)) / (face.UVScale * MAP_UNIT_SIZE);
                                uv += face.UVOffset;
                                position.X = Mathf.Round(position.X, MAP_UNIT_SIZE);
                                position.Y = Mathf.Round(position.Y, MAP_UNIT_SIZE);
                                position.Z = Mathf.Round(position.Z, MAP_UNIT_SIZE);

                                ref var vertex = ref mapData.Geometry.AddVertex();
                                vertex.Position = position;
                                vertex.Normal = faceNormal;
                                vertex.TexCoord = uv;
                            }

                            // Remember the verts for index generation later
                            var call = new DrawCall();
                            call.TexturePath = face.TextureName;
                            call.FirstIndex = firstVertex;
                            call.IndexCount = vertexCount;
                            unsortedDrawCalls.Add(call);
                        }

                        // Continue
                        pos += vertexCount;
                        faceIdx++;
                    }

                    points.Clear();
                    polygons.Clear();
                }

                // Generate indices and draw calls
                unsortedDrawCalls.Sort(CompareByTexture);
                var pendingDrawCall = new DrawCall();
                foreach(var call in unsortedDrawCalls)
                {
                    // Add the indices
                    int firstVertex = call.FirstIndex;
                    int vertexCount = call.IndexCount;
                    int firstIndex = mapData.Geometry.IndexCount;
                    for (int j = 2; j < vertexCount; ++j)
                    {
                        mapData.Geometry.AddIndex(firstVertex);
                        mapData.Geometry.AddIndex(firstVertex + j - 1);
                        mapData.Geometry.AddIndex(firstVertex + j);
                    }
                    int indexCount = mapData.Geometry.IndexCount - firstIndex;

                    // Build the draw call
                    var textureName = call.TexturePath;
                    if (pendingDrawCall.TexturePath != textureName)
                    {
                        if (pendingDrawCall.TexturePath != null)
                        {
                            pendingDrawCall.TexturePath = AssetPath.Combine(wadName, pendingDrawCall.TexturePath + ".png");
                            entityData.DrawCalls.Add(pendingDrawCall);
                        }
                        pendingDrawCall.TexturePath = textureName;
                        pendingDrawCall.FirstIndex = firstIndex;
                        pendingDrawCall.IndexCount = indexCount;
                    }
                    else
                    {
                        pendingDrawCall.IndexCount += indexCount;
                    }
                }
                unsortedDrawCalls.Clear();

                // Finish the last draw call
                if (pendingDrawCall.TexturePath != null)
                {
                    pendingDrawCall.TexturePath = AssetPath.Combine(wadName, pendingDrawCall.TexturePath + ".png");
                    entityData.DrawCalls.Add(pendingDrawCall);
                }

                // Store the entity data
                mapData.Entities.Add(entityData);
            }

            // Return the map data
            return mapData;
        }

        public Map(string path, object data)
        {
            m_path = path;
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

        private void Load(object data)
        {
            // Store the data
            var mapData = (MapData)data;
            m_entities = mapData.Entities;

            // Upload the geometry
            m_geometry = OpenGL.OpenGLRenderer.Instance.Upload(mapData.Geometry, RenderGeometryFlags.Default); // TODO: Make generic
        }

        private void Unload()
        {
            m_geometry.Dispose();
        }

        public List<ConvexHull> GetHulls(int entityIndex)
        {
            var entityData = m_entities[entityIndex];
            return entityData.BrushHulls;
        }

        public List<AABB> GetBoundingBoxes(int entityIndex)
        {
            var entityData = m_entities[entityIndex];
            return entityData.BrushBoundingBoxes;
        }

        public void DrawEntity(IRenderer renderer, MapEffectHelper effect, int entityIndex, in Matrix4 transform)
        {
            var entityData = m_entities[entityIndex];
            if (entityData.DrawCalls.Count > 0)
            {
                // Set shared parameters
                effect.ModelMatrix = transform;

                // Make the draw calls
                renderer.CurrentEffect = effect.Instance;
                foreach (var call in entityData.DrawCalls)
                {
                    effect.Texture = Texture.Get(call.TexturePath, true);
                    renderer.DrawRange(m_geometry, call.FirstIndex, call.IndexCount);
                }
            }
        }        
    }
}
