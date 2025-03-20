using System;
using System.IO;
using System.Numerics;
using UnityEngine;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;

namespace QuakeMapTools
{
    [RequireComponent(typeof(QuakeMap))]
    public class QuakeMapCubeGenerator : MonoBehaviour
    {
        [Header("Cube Settings")]
        public UnityEngine.Vector3 cubeSize = new UnityEngine.Vector3(64, 64, 64);
        public UnityEngine.Vector3       cubeOrigin  = UnityEngine.Vector3.zero;
        public string                    textureName = "128_honey_2";
        public QuakeMap quakeMap;
        
        [ContextMenu("Add Cube To Map")]
        public void AddCubeToMap()
        {
            if(quakeMap.Map==null)
                return;
            
            // Create and add a cube solid
            var cubeSolid = CreateCubeSolid();
            quakeMap.Map.Worldspawn.Children.Add(cubeSolid);

            Debug.Log("Added Cube To Map");
        }
        
        private Solid CreateCubeSolid()
        {
            // Generate a new unique ID
            int id = UnityEngine.Random.Range(1, int.MaxValue);
            
            // Create a new solid
            var solid = new Solid();
            
            // Calculate half-size for easier vertex calculations
            var halfSize = new System.Numerics.Vector3(
                cubeSize.x / 2, 
                cubeSize.y / 2, 
                cubeSize.z / 2
            );

            const float scaleOffset = 0.03125f;

            // Convert Unity origin to Quake coordinates (swap Y and Z, then apply scale)
            var origin = new System.Numerics.Vector3(
                cubeOrigin.x / scaleOffset,
                cubeOrigin.z / scaleOffset, // Unity's Y is Quake's Z
                cubeOrigin.y / scaleOffset  // Unity's Z is Quake's Y
            );
            
            // Add the six faces of the cube (each face is a plane with vertices)
            solid.Faces.Add(CreateFace(new[] {  // Top face (+Z in Quake)
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, halfSize.Z)
            }, new System.Numerics.Vector3(0, 0, 1)));
            
            solid.Faces.Add(CreateFace(new[] {  // Bottom face (-Z in Quake)
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z)
            }, new System.Numerics.Vector3(0, 0, -1)));
            
            solid.Faces.Add(CreateFace(new[] {  // Front face (+Y in Quake)
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, -halfSize.Z)
            }, new System.Numerics.Vector3(0, 1, 0)));
            
            solid.Faces.Add(CreateFace(new[] {  // Back face (-Y in Quake)
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z)
            }, new System.Numerics.Vector3(0, -1, 0)));
            
            solid.Faces.Add(CreateFace(new[] {  // Right face (+X in Quake)
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(halfSize.X, -halfSize.Y, halfSize.Z)
            }, new System.Numerics.Vector3(1, 0, 0)));
            
            solid.Faces.Add(CreateFace(new[] {  // Left face (-X in Quake)
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, halfSize.Y, -halfSize.Z),
                origin + new System.Numerics.Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z)
            }, new System.Numerics.Vector3(-1, 0, 0)));
            
            return solid;
        }
        
        private Face CreateFace(System.Numerics.Vector3[] vertices, System.Numerics.Vector3 normal)
        {
            // Create a new face with the specified vertices and normal
            var face = new Face { TextureName = textureName };
            
            // Add vertices in the correct winding order
            foreach (var vertex in vertices)
            {
                face.Vertices.Add(vertex);
            }
            
            // Calculate the plane equation: ax + by + cz + d = 0
            // where (a,b,c) is the normal vector and d is the distance from origin
            float d = -System.Numerics.Vector3.Dot(normal, vertices[0]);
            face.Plane = new System.Numerics.Plane
            {
                Normal = normal,
                D = d
            };
            
            // Set up texture mapping
            SetupTextureMapping(face, normal);
            
            return face;
        }
        
        private void SetupTextureMapping(Face face, System.Numerics.Vector3 normal)
        {
            // Determine U and V axes based on the normal
            System.Numerics.Vector3 uAxis, vAxis;
            
            // Find the dominant axis of the normal
            float absX = Math.Abs(normal.X);
            float absY = Math.Abs(normal.Y);
            float absZ = Math.Abs(normal.Z);
            
            if (absZ >= absX && absZ >= absY)
            {
                // Z is dominant, use X and Y for texture coordinates
                uAxis = new System.Numerics.Vector3(1, 0, 0);
                vAxis = new System.Numerics.Vector3(0, -1, 0);
            }
            else if (absY >= absX)
            {
                // Y is dominant, use X and Z for texture coordinates
                uAxis = new System.Numerics.Vector3(1, 0, 0);
                vAxis = new System.Numerics.Vector3(0, 0, -1);
            }
            else
            {
                // X is dominant, use Y and Z for texture coordinates
                uAxis = new System.Numerics.Vector3(0, 1, 0);
                vAxis = new System.Numerics.Vector3(0, 0, -1);
            }
            
            // Set texture mapping properties
            face.UAxis = uAxis;
            face.VAxis = vAxis;
            face.XScale = 0.25f; // Standard texture scale
            face.YScale = 0.25f;
            face.XShift = 0;
            face.YShift = 0;
            face.Rotation = 0;
            face.LightmapScale = 16; // Standard lightmap scale
        }
    }
}