using System.Collections.Generic;
using Junk.Math;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace ScriptsSandbox.Util
{
    public static class TbMath
    {
        // returns null if no intersection, else intersection vertex.
        public static bool IntersectFace(brushFace f0, brushFace f1, brushFace f2, out float3 result)
        {
            result = float3.zero;
            var n0    = f0.plane.Normal;
            var n1    = f1.plane.Normal;
            var n2    = f2.plane.Normal;
            var denom = math.dot(math.cross(n0, n1), n2);
            if (denom > 0.0f)
            {
                result = (math.cross(n1, n2) * f0.plane.Distance + math.cross(n2, n0) * f1.plane.Distance + math.cross(n0, n1) * f2.plane.Distance) / denom;
                return true;
            }

            return false;
        }

        static bool VertexInHull(List<brushFace> faces, float3 vertex)
        {
            var CMP_EPSILON = 0.008f;
            foreach (var face in faces)
            {
                var proj = math.dot(face.plane.Normal, vertex);
                if (proj > face.plane.Distance && math.abs(face.plane.Distance - proj) > CMP_EPSILON)
                    return false;
            }

            return true;
        }

        public static void GenerateMesh_sledge(brushsolid brush, Material mMaterial)
        {
            var   faceCount = brush.faces.Count;
            float scale     = 0.03125f;
            
            // Process each face to collect vertices
            for (int i = 0; i < faceCount; i++)
            {
                var face                 = brush.faces[i];
                var textureSize          = new int2(64, 64);

                var vertices = new List<float3>();
                var uvs      = new List<float2>();
                var normals  = new List<float3>();
                var tangents = new List<float4>();
                
                // swap coordinates due to tb/quake difference
                foreach (var point in face.points)
                {
                    var vertex = new float3(point.x, point.y, point.z) * scale;
                    vertices.Add(vertex);
                }
                
                /*foreach (var point in face.points)
                {
                    var normal  = face.plane.Normal;
                    var uv      = GetStandardUV(vertex, face, textureSize);
                    
                    // New vertex
                    normal = math.normalize(normal);
                    vertices.Add(vertex);
                    normals.Add(normal);
                    uvs.Add(uv);
                }*/
                
                // Sort vertices in clockwise order for proper triangulation
                vertices = SortVertices(vertices, face.plane);
                
                // Create triangles using fan triangulation (as TrenchBroom does for OBJ export)
                var triangles = new List<int>();
                for (int t = 1; t < vertices.Count - 1; t++)
                {
                    triangles.Add(0);
                    triangles.Add(t);
                    triangles.Add(t + 1);
                }
                
                var mesh = new Mesh();
                mesh.vertices  = vertices.ToVector3Array();
                mesh.uv        = uvs.ToVector2Array();
                mesh.normals   = normals.ToVector3Array();
                mesh.tangents  = tangents.ToVector4Array();
                mesh.triangles = triangles.ToArray(); // Don't forget to set the triangles
                mesh.RecalculateBounds();
                
                var go   = new GameObject();
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                go.GetComponent<MeshFilter>().sharedMesh       = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = mMaterial;
            }
        }

        /// <summary>
        /// Sorts vertices in clockwise order around a normal, following the algorithm in the MAP file paper
        /// </summary>
        /// <param name="vertices">List of vertices to sort</param>
        /// <param name="plane">Plane of the face</param>
        /// <returns>Sorted list of vertices</returns>
        public static List<float3> SortVertices(List<float3> vertices, Unity.Mathematics.Geometry.Plane plane)
        {
            if (vertices.Count < 3)
                return vertices;

            // Calculate polygon center
            float3 center = GetCenterOfPolygon(vertices);
            
            // Make a copy of the vertices list
            List<float3> sortedVertices = new List<float3>();
            List<float3> remainingVertices = new List<float3>(vertices);
            
            // Start with first vertex
            sortedVertices.Add(remainingVertices[0]);
            remainingVertices.RemoveAt(0);
            
            const float Epsilon = 0.0001f;
            
            // For each remaining vertex position to fill
            while (remainingVertices.Count > 0)
            {
                // Get the current vector (from center to current vertex)
                float3 currentVector = math.normalize(sortedVertices[sortedVertices.Count - 1] - center);
                
                int nextIndex = 0;
                float largestAngle = -1.0f;
                
                // Create a plane perpendicular to the polygon along the current vector
                float3 planeNormal = plane.Normal;
                float planeDistance = -math.dot(planeNormal, sortedVertices[sortedVertices.Count - 1]);
                
                // Find vertex with smallest angle
                for (int i = 0; i < remainingVertices.Count; i++)
                {
                    float3 testVector = math.normalize(remainingVertices[i] - center);
                    
                    // Skip vertices more than 180° away
                    float planeDot = math.dot(planeNormal, remainingVertices[i]) + planeDistance;
                    if (planeDot < -Epsilon)
                        continue;
                        
                    float angle = math.dot(currentVector, testVector);
                    if (angle > largestAngle)
                    {
                        largestAngle = angle;
                        nextIndex = i;
                    }
                }
                
                // Add the vertex with the smallest angle to our sorted list
                sortedVertices.Add(remainingVertices[nextIndex]);
                remainingVertices.RemoveAt(nextIndex);
            }
            
            // Check if vertices need to be reversed based on normal direction
            float3 calculatedNormal = CalculatePolygonNormal(sortedVertices);
            if (math.dot(calculatedNormal, plane.Normal) < 0)
            {
                sortedVertices.Reverse();
            }
            
            return sortedVertices;
        }
        
        public static float3 GetCenterOfPolygon(List<float3> vertices)
        {
            var center = float3.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                center += vertices[i];
            }
            return center / vertices.Count;
        }
        /// <summary>
        /// Calculate the normal of a polygon using Newell's method
        /// </summary>
        private static float3 CalculatePolygonNormal(List<float3> vertices)
        {
            float3 normal = float3.zero;
            
            // Use Newell's method for calculating polygon normal
            for (int i = 0; i < vertices.Count; i++)
            {
                float3 current = vertices[i];
                float3 next = vertices[(i + 1) % vertices.Count];
                
                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }
            
            return math.normalize(normal);
        }
        
        // similar to func_godot with slightly different results
        public static float2 GetStandardUV(float3 vertex, Face face, int2 textureSize)
        {
            int textureWidth  = textureSize.x;
            int textureHeight = textureSize.y;
    
            // Get the face normal
            float3 normal = face.Plane.Normal.AsVector3(true);

            // Compute the best U and V axes dynamically based on the normal
            float3 uAxis, vAxis;

            // Find the dominant axis
            float absX = math.abs(normal.x);
            float absY = math.abs(normal.y);
            float absZ = math.abs(normal.z);

            if (absX >= absY && absX >= absZ) // X-dominant faces
            {
                // For X-facing, use Z as U and Y as V
                // For -X direction, flip the U axis
                float uSign = (normal.x >= 0) ? 1.0f : -1.0f;
                uAxis = new float3(0, 0, uSign);
                vAxis = new float3(0, 1, 0);
            }
            else if (absY >= absX && absY >= absZ) // Y-dominant faces
            {
                // For Y-facing, use X as U and Z as V
                // For -Y direction, flip the V axis
                float vSign = (normal.y >= 0) ? 1.0f : -1.0f;
                uAxis = new float3(1, 0, 0);
                vAxis = new float3(0, 0, vSign);
            }
            else // Z-dominant faces
            {
                // For Z-facing, use X as U and Y as V
                // For -Z direction, flip the U axis
                float uSign = (normal.z >= 0) ? 1.0f : -1.0f;
                uAxis = new float3(uSign, 0, 0);
                vAxis = new float3(0, 1, 0);
            }

            // Project the vertex onto the computed U and V axes
            float2 uvOut = new float2(
                math.dot(vertex, uAxis),
                math.dot(vertex, vAxis)
            );

            // Apply rotation
            float angle = math.radians(face.Rotation);
            uvOut = new float2(
                uvOut.x * math.cos(angle) - uvOut.y * math.sin(angle),
                uvOut.x * math.sin(angle) + uvOut.y * math.cos(angle)
            );

            // Apply scaling and shifting
            uvOut.x /= textureWidth;
            uvOut.y /= textureHeight;

            uvOut.x /= face.XScale;
            uvOut.y /= face.YScale;

            uvOut.x += face.XShift / textureWidth;
            uvOut.y += face.YShift / textureHeight;

            return uvOut;
        }
        
        // from func_godot
        public static float2 GetStandardUVSledge(float3 vertex, Face face, int2 textureSize)
        {
            int    textureWidth  = textureSize.x;
            int    textureHeight = textureSize.y;
            float2 uvOut;
            // Define vectors matching FuncGodot's coordinate system
            // These should match FuncGodot's constants
            float3 upVector      = new float3(0, 0, 1);
            float3 rightVector   = new float3(0, 1, 0);
            float3 forwardVector = new float3(1, 0, 0);
            
            double du = math.abs(math.dot(face.Plane.Normal.AsVector3(true), upVector));
            double dr = math.abs(math.dot(face.Plane.Normal.AsVector3(true), rightVector));
            double df = math.abs(math.dot(face.Plane.Normal.AsVector3(true), forwardVector));

            if (du >= dr && du >= df)
            {
                uvOut = new float2(vertex.x, -vertex.y);
            }
            else if (dr >= du && dr >= df)
            {
                uvOut = new float2(vertex.x, -vertex.z);
            }
            else
            {
                uvOut = new float2(vertex.y, -vertex.z);
            }

            float angle = math.radians(face.Rotation);
            uvOut = new float2(
                uvOut.x * math.cos(angle) - uvOut.y * math.sin(angle),
                uvOut.x * math.sin(angle) + uvOut.y * math.cos(angle)
            );

            uvOut.x /= textureWidth;
            uvOut.y /= textureHeight;

            uvOut.x /= face.XScale;
            uvOut.y /= face.YScale;

            uvOut.x += face.XShift / textureWidth;
            uvOut.y += face.YShift / textureHeight;

            return uvOut;
        }
        
        
        public static float2 GetStandardUVOriginal(float3 vertex, brushFace face, int2 textureSize)
        {
            int    textureWidth  = textureSize.x;
            int    textureHeight = textureSize.y;
            float2 uvOut;
            float  du = math.abs(math.dot(face.plane.Normal, maths.up));
            float  dr = math.abs(math.dot(face.plane.Normal, maths.right));
            float  df = math.abs(math.dot(face.plane.Normal, maths.forward));

            if (du >= dr && du >= df)
            {
                uvOut = new float2(vertex.x, -vertex.y);
            }
            else if (dr >= du && dr >= df)
            {
                uvOut = new float2(vertex.x, -vertex.z);
            }
            else
            {
                uvOut = new float2(vertex.y, -vertex.z);
            }

            float angle = math.radians(face.attributes.rotation);
            uvOut = new float2(
                uvOut.x * math.cos(angle) - uvOut.y * math.sin(angle),
                uvOut.x * math.sin(angle) + uvOut.y * math.cos(angle)
            );

            uvOut.x /= textureWidth;
            uvOut.y /= textureHeight;

            uvOut.x /= face.attributes.scale.x;
            uvOut.y /= face.attributes.scale.y;

            uvOut.x += face.attributes.offset.x / textureWidth;
            uvOut.y += face.attributes.offset.y / textureHeight;

            return uvOut;
        }

        public static float4 get_standard_tangent(brushFace face)
        {
            var du  = math.dot(face.plane.Normal, maths.up);
            var dr  = math.dot(face.plane.Normal, maths.right);
            var df  = math.dot(face.plane.Normal, maths.forward);
            var dua = math.abs(du);
            var dra = math.abs(dr);
            var dfa = math.abs(dra);

            var u_axis = float3.zero;
            var v_sign = 0f;

            if (dua >= dra && dua >= dfa)
            {
                u_axis = math.forward();
                v_sign = math.sign(du);
            }
            else if (dra >= dua && dra >= dfa)
            {
                u_axis = math.forward();
                v_sign = -math.sign(dr);
            }
            else if (dfa >= dua && dfa >= dra)
            {
                u_axis = math.right();
                v_sign = math.sign(df);
            }

            v_sign *= math.sign(face.attributes.scale.y);
            quaternion rotation = quaternion.AxisAngle(face.plane.Normal, face.attributes.rotation);
            u_axis = math.rotate(rotation, u_axis);
            return new float4(u_axis.x, u_axis.y, u_axis.z, v_sign);
        }
    }

    public static class Conversion
    {
        // Convert List<float3> to Vector3[]
        public static Vector3[] ToVector3Array(this List<float3> list)
        {
            var array = new Vector3[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                array[i] = new Vector3(list[i].x, list[i].y, list[i].z);
            }

            return array;
        }

        // Convert List<float2> to Vector2[]
        public static Vector2[] ToVector2Array(this List<float2> list)
        {
            var array = new Vector2[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                array[i] = new Vector2(list[i].x, list[i].y);
            }

            return array;
        }

        // Convert List<float4> to Vector4[]
        public static Vector4[] ToVector4Array(this List<float4> list)
        {
            var array = new Vector4[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                array[i] = new Vector4(list[i].x, list[i].y, list[i].z, list[i].w);
            }

            return array;
        }

        public static T[] GetArray<T>(this List<T> list)
        {
            var array = new T[list.Count];
            for (var index = 0; index < list.Count; index++)
            {
                array[index] = list[index];
            }

            return array;
        }
    }
}