using System;
using System.Collections.Generic;
using System.Linq;
using QuakeMapVisualization;
using Sledge.Formats.Geometric;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;

namespace ScriptsSandbox.QuakeMap
{
        public static class VectorExtensions
        {
            // Convert from System.Numerics.Vector3 to UnityEngine.Vector3
            // with optional coordinate swapping for TrenchBroom compatibility
            public static UnityEngine.Vector3 AsVector3(this System.Numerics.Vector3 vec, bool swapYZ = false)
            {
                if (swapYZ)
                {
                    // This matches TrenchBroom's OBJ export: x, z, -y
                    return new UnityEngine.Vector3(vec.X, vec.Z, vec.Y);
                }
                else
                {
                    return new UnityEngine.Vector3(vec.X, vec.Y, vec.Z);
                }
            }
        
            // Helper to create an easy-to-read string representation of a vector
            public static string ToFormattedString(this UnityEngine.Vector3 vec)
            {
                return $"({vec.x:F4}, {vec.y:F4}, {vec.z:F4})";
            }
        
            // Helper to create an easy-to-read string representation of a UV coordinate
            public static string ToFormattedString(this UnityEngine.Vector2 vec)
            {
                return $"({vec.x:F4}, {vec.y:F4})";
            }
        }
        public static class QMeshing
    {
        // Add this method to your QMeshing class
        public static Tuple<List<float3>, List<int>> SortVerticesWithIndices(List<float3> vertices, List<int> indices, Plane plane)
        {
            // Create pairs of vertices and their original indices
            var pairs = new List<Tuple<float3, int>>();
            for (int i = 0; i < vertices.Count; i++)
            {
                pairs.Add(new Tuple<float3, int>(vertices[i], indices[i]));
            }
    
            // Use the same sorting logic as your original SortVertices method
            // but applied to the pairs
    
            // Extract sorted vertices and indices
            var sortedVertices = new List<float3>();
            var sortedIndices  = new List<int>();
    
            foreach (var pair in pairs)
            {
                sortedVertices.Add(pair.Item1);
                sortedIndices.Add(pair.Item2);
            }
    
            return new Tuple<List<float3>, List<int>>(sortedVertices, sortedIndices);
        }
        // The BaseAxes array from TrenchBroom's ParaxialUVCoordSystem
        private static readonly Vector3[] BaseAxes = new Vector3[] 
        {
            new Vector3(0.0f, 0.0f, 1.0f),  // normal
            new Vector3(1.0f, 0.0f, 0.0f),  // uAxis
            new Vector3(0.0f, -1.0f, 0.0f), // vAxis
            
            new Vector3(0.0f, 0.0f, -1.0f), // normal
            new Vector3(1.0f, 0.0f, 0.0f),  // uAxis
            new Vector3(0.0f, -1.0f, 0.0f), // vAxis
            
            new Vector3(1.0f, 0.0f, 0.0f),  // normal
            new Vector3(0.0f, 1.0f, 0.0f),  // uAxis
            new Vector3(0.0f, 0.0f, -1.0f), // vAxis
            
            new Vector3(-1.0f, 0.0f, 0.0f), // normal
            new Vector3(0.0f, 1.0f, 0.0f),  // uAxis
            new Vector3(0.0f, 0.0f, -1.0f), // vAxis
            
            new Vector3(0.0f, 1.0f, 0.0f),  // normal
            new Vector3(1.0f, 0.0f, 0.0f),  // uAxis
            new Vector3(0.0f, 0.0f, -1.0f), // vAxis
            
            new Vector3(0.0f, -1.0f, 0.0f), // normal
            new Vector3(1.0f, 0.0f, 0.0f),  // uAxis
            new Vector3(0.0f, 0.0f, -1.0f)  // vAxis
        };

        // Determine the plane normal index for paraxial mapping
        public static int GetPlaneNormalIndex(Vector3 normal)
        {
            int bestIndex = 0;
            float bestDot = 0;
            
            // Check against each of the 6 possible plane normals (each set of 3 entries)
            for (int i = 0; i < 6; i++)
            {
                float curDot = math.dot(normal, BaseAxes[i * 3]);
                if (curDot > bestDot)
                {
                    bestDot = curDot;
                    bestIndex = i;
                }
            }
            
            return bestIndex;
        }

        // Extract the UV axes for a given plane normal index
        public static (Vector3 uAxis, Vector3 vAxis) GetTextureAxes(int planeNormalIndex)
        {
            return (
                BaseAxes[planeNormalIndex * 3 + 1], // uAxis
                BaseAxes[planeNormalIndex * 3 + 2]  // vAxis
            );
        }

        // Calculate texture coordinates exactly as TrenchBroom does in BrushFace.cpp
        public static float2 CalculateTextureCoordinatesWithFace(
            Vector3 vertex, Face face, float2 textureSize)
        {
            // First, we need the proper texture axes based on the face normal
            int planeNormalIndex = GetPlaneNormalIndex(face.Plane.Normal.AsVector3(true));
            var (uAxis, vAxis) = GetTextureAxes(planeNormalIndex);
            
            // Apply rotation if needed
            if (math.abs(face.Rotation) > 0.001f)
            {
                // We need to determine if rotation is inverted based on the plane normal
                bool isRotationInverted = planeNormalIndex % 2 == 0;
                
                // Get the normal for rotation
                Vector3 normal = BaseAxes[planeNormalIndex * 3];
                
                float angle = isRotationInverted ? -face.Rotation : face.Rotation;
                Quaternion rotation = Quaternion.AngleAxis(angle, normal);
                
                uAxis = rotation * uAxis;
                vAxis = rotation * vAxis;
            }
            
            // Calculate dot products for texture coordinates
            float dotU = math.dot(vertex, uAxis);
            float dotV = math.dot(vertex, vAxis);
            
            // Calculate the texture coordinates using the formula from TrenchBroom
            float u = dotU / face.XScale;
            float v = dotV / face.YScale;
            
            // Apply texture shift
            u += face.XShift;
            v += face.YShift;
            
            // Normalize by texture size
            u /= textureSize.x;
            v /= textureSize.y;
            
            // Flip the V coordinate as TrenchBroom does in ObjSerializer.cpp
            return new float2(u, -v);
        }

        /// <summary>
        /// Normalizes texture coordinates for a polygon to bring them closer to 0
        /// </summary>
        public static void NormalizeTexcoords(List<Vector2> uvs)
        {
            // Normalize U coordinates
            float nearestU = uvs[0].x;
            int indexOfNearestU = 0;
            bool needsNormalization = false;
    
            // Find the U coordinate closest to 0 but outside the -1 to 1 range
            for (int i = 0; i < uvs.Count; i++)
            {
                if (math.abs(uvs[i].x) > 1)
                {
                    if (math.abs(uvs[i].x) < math.abs(nearestU))
                    {
                        indexOfNearestU = i;
                        nearestU = uvs[i].x;
                        needsNormalization = true;
                    }
                }
            }
    
            // If normalization is needed, subtract the nearest value from all coordinates
            if (needsNormalization)
            {
                for (int i = 0; i < uvs.Count; i++)
                {
                    uvs[i] = new Vector2(uvs[i].x - nearestU, uvs[i].y);
                }
            }
    
            // Normalize V coordinates (same process)
            float nearestV = uvs[0].y;
            int indexOfNearestV = 0;
            needsNormalization = false;
    
            for (int i = 0; i < uvs.Count; i++)
            {
                if (math.abs(uvs[i].y) > 1)
                {
                    if (math.abs(uvs[i].y) < math.abs(nearestV))
                    {
                        indexOfNearestV = i;
                        nearestV = uvs[i].y;
                        needsNormalization = true;
                    }
                }
            }
    
            if (needsNormalization)
            {
                for (int i = 0; i < uvs.Count; i++)
                {
                    uvs[i] = new Vector2(uvs[i].x, uvs[i].y - nearestV);
                }
            }
        }
        
        public static UnityEngine.Plane ToUnityPlane(this System.Numerics.Plane plane)
        {
            var p = new Plane();
            p.normal = new Vector3(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
            p.distance = plane.D;
            return p;
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
        /// Sorts vertices in clockwise order around a normal, following the algorithm in the MAP file paper
        /// </summary>
        /// <param name="vertices">List of vertices to sort</param>
        /// <param name="plane">Plane of the face</param>
        /// <returns>Sorted list of vertices</returns>
        public static List<float3> SortVertices(List<float3> vertices, Plane plane)
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
                float3 planeNormal = plane.normal;
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
            if (math.dot(calculatedNormal, plane.normal) < 0)
            {
                sortedVertices.Reverse();
            }
            
            return sortedVertices;
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
    }

}