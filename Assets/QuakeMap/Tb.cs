using System;
using System.Collections.Generic;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Plane = Unity.Mathematics.Geometry.Plane;
using Unity.Mathematics.Geometry;

// ReSharper disable InconsistentNaming

namespace ScriptsSandbox.QuakeMap
{
    public static partial class Tb
    {
        private static Material m_material;

        public class BrushObject
        {
            public int             entityIndex;
            public int             brushIndex;
            public List<BrushFace> faces;

            public BrushObject() => faces = new List<BrushFace>();
        }

        public struct BrushFace
        {
            public List<IndexedVertex> verts;
            public Plane plane;
            public string              materialName;
            public material            material;
        }

        public static void getMapfileData(List<Solid> solids, Material material)
        {
            m_material  = material;
            entityIndex = 0;
            brushIndex  = 0;
            m_normals   = new IndexMap<float3>();
            m_vertices  = new IndexMap<float3>();
            m_uvCoords  = new IndexMap<float2>();
            m_objects   = new List<BrushObject>();

            foreach (var solid in solids)
                doBrush(solid.GetBrushSolid());

            // convert brush to meshes

            var brushSolids = new List<brushsolid>();
            foreach (var solid in solids)
                brushSolids.Add(solid.GetBrushSolid());
            doMeshes(brushSolids);
            //doObj();
        }
        private static List<UnityEngine.Mesh> doMeshesPseudo()
        {
            var meshes = new List<UnityEngine.Mesh>();
            
            foreach (var brushObj in m_objects)
            {
                var mesh = new UnityEngine.Mesh();
                
                // Create lists to hold the mesh data
                var vertices = new List<Vector3>();
                var uvs = new List<Vector2>();
                var normals = new List<Vector3>();
                var triangles = new List<int>();
                
                // Process all vertices from our index map
                foreach (var vertex in m_vertices.List)
                {
                    // Match TrenchBroom's vertex transformation: swap Y and Z, negate Y
                    vertices.Add(new Vector3(vertex.x, vertex.z, -vertex.y));
                }
                
                // Process all UVs from our index map
                foreach (var uv in m_uvCoords.List)
                {
                    // Match TrenchBroom's UV transformation: negate Y
                    uvs.Add(new Vector2(uv.x, -uv.y));
                }
                
                // Process all normals from our index map
                foreach (var normal in m_normals.List)
                {
                    // Match TrenchBroom's normal transformation: swap Y and Z, negate Y
                    normals.Add(new Vector3(normal.x, normal.z, -normal.y));
                }
                
                // Generate triangles from the faces
                foreach (var face in brushObj.faces)
                {
                    // Skip faces with fewer than 3 vertices
                    if (face.verts.Count < 3)
                        continue;
                        
                    // Triangulate the face (assuming it's a convex polygon)
                    for (int i = 1; i < face.verts.Count - 1; i++)
                    {
                        // Unity uses clockwise winding order for front faces
                        // Use the vertex indices from our indexed vertices
                        triangles.Add(face.verts[0].Vertex);
                        triangles.Add(face.verts[i].Vertex);
                        triangles.Add(face.verts[i + 1].Vertex);
                    }
                }
                
                // Assign the data to the mesh
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
                
                // Optional: Recalculate bounds
                mesh.RecalculateBounds();
                
                // Add to our list
                meshes.Add(mesh);
            }
            
            return meshes;
        }
        private static List<Mesh> doMeshes(List<brushsolid> solids)
        {
            var meshes = new List<UnityEngine.Mesh>();
            
            foreach (var brush in solids)
            {
                TbMath.GenerateMesh_sledge(brush, m_material);


            }
            return meshes;
        }

        private static void doBrush(brushsolid brush)
        {
            m_currentBrush = new BrushObject
            {
                brushIndex  = brushIndex++,
                entityIndex = entityIndex++,
                faces       = new List<BrushFace>()
            };

            m_vertices.clearIndices();

            foreach (var face in brush.faces)
            {
                doBrushFace(face);
            }

            m_objects.Add(m_currentBrush);
            m_currentBrush = null;
        }

        private static int               entityIndex;
        private static int               brushIndex;
        private static IndexMap<float3>  m_normals  = new IndexMap<float3>();
        private static IndexMap<float3>  m_vertices = new IndexMap<float3>();
        private static IndexMap<float2>  m_uvCoords = new IndexMap<float2>();
        private static BrushObject       m_currentBrush;
        private static List<BrushObject> m_objects;

        private static void doBrushFace(brushFace face)
        {
            var normal          = face.plane.Normal;
            var normalIndex     = m_normals.Index(normal);
            var indexedVertices = new List<IndexedVertex>();
            foreach (var vertex in face.points)
            {
                var position      = vertex.xyz;
                var uvCoords      = face.uvCoords(position);
                var vertexIndex   = m_vertices.Index(position);
                var uvCoordsIndex = m_uvCoords.Index(uvCoords);
                indexedVertices.Add(new IndexedVertex(vertexIndex, uvCoordsIndex, normalIndex));
            }

            // Add this face to the current brush
            m_currentBrush.faces.Add(new BrushFace
            {
                verts        = indexedVertices,
                plane        = face.plane,
                materialName = face.attributes.materialname,
                material     = face.material
            });
        }
    }


    // lowercase are the cpp derriviative types, uppercase export specific
    public struct material
    {
        // dummy data
    }

    [Serializable]
    public class attributes
    {
        public float2 scale; // should be initalized with 1,1
        public float2 offset;
        public float3 uAxis;
        public float3 vAxis;
        public float  rotation;
        public string materialname;
        public float2 textureSize;
        public Color  color;
    }

    [Serializable]
    public class brushsolid
    {
        public List<brushFace> faces;
        public float3          position;
        public float3          rotation;
        public float3          scale;
    }

    /// <summary>
    /// implements reordering from tb to unity
    /// </summary>
    [Serializable]
    public class brushFace
    {
        public Plane        plane;
        public List<float3> points;
        public attributes   attributes;
        public material     material;

        /*ParaxialUVCoordSystem.cpp
         vm::vec2f ParaxialUVCoordSystem::uvCoords(
          const vm::vec3d& point,
          const BrushFaceAttributes& attribs,
          const vm::vec2f& textureSize) const
        {
          return (computeUVCoords(point, attribs.scale()) + attribs.offset()) / textureSize;
        }*/
        public float2 uvCoords(float3 point)
        {
            return (computeUVCoords(point, attributes.uAxis, attributes.vAxis, attributes.scale) + attributes.offset) / attributes.textureSize;
        }

        private static float2 computeUVCoords(float3 point, float3 uAxis, float3 vAxis, float2 scale)
        {
            return new float2(math.dot(point, safeScaleAxis(uAxis, scale.x)),
                math.dot(point, safeScaleAxis(vAxis, scale.y)));
        }

        // This method mimics safeScaleAxis: divides the vector by the "scaled" factor.
        public static float3 safeScaleAxis(float3 axis, float factor)
        {
            // Convert factor to the same type and use SafeScale to avoid division by zero.
            return axis / safeScale(factor);
        }

        // This method mimics safeScale: if value is (nearly) zero, returns 1.0f; otherwise, returns value.
        public static float safeScale(float value)
        {
            return math.abs(value) < math.EPSILON ? 1.0f : value;
        }
    }

    public struct IndexedVertex
    {
        public int Vertex;
        public int UvCoords;
        public int Normal;

        public IndexedVertex(int vertex, int uvCoords, int normal)
        {
            Vertex   = vertex;
            UvCoords = uvCoords;
            Normal   = normal;
        }
    }

    public class IndexMap<T>
    {
        private Dictionary<T, int> map  = new Dictionary<T, int>();
        private List<T>            list = new List<T>();

        public List<T> List => list;

        public int Index(T value)
        {
            if (!map.TryGetValue(value, out int index))
            {
                index = list.Count;
                list.Add(value);
                map[value] = index;
            }

            return index;
        }

        public void clearIndices()
        {
            map.Clear();
        }
    }
}