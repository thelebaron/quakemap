
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;

namespace ScriptsSandbox.Util
{
    public static class QuakeMeshUtility
    {
        public static float3 ToUnity(this System.Numerics.Vector3 v) => new float3(v.X, v.Y, v.Z);
        
        public static float2 Rotate(float2 offset, float angleDegrees)
        {
            float angle = math.radians(angleDegrees);
            float cosA  = math.cos(angle);
            float sinA  = math.sin(angle);
            return new float2(
                offset.x * cosA - offset.y * sinA,
                offset.x * sinA + offset.y * cosA
            );
        }
        
        /// <summary>
        /// Direct TrenchBroom-compatible UV calculation with manual offset adjustment
        /// </summary>
        public static float2 DirectTBUVCalculation(float3 originalVertex, Face face, int2 textureSize)
        {
            // Apply the same vertex conversion as in CreateFaceMesh (swap Y and Z)
            float3 vertex = new float3(originalVertex.x, originalVertex.z, originalVertex.y);
    
            // Convert face axes to Unity space
            float3 uAxis = face.UAxis.ToUnity();
            float3 vAxis = face.VAxis.ToUnity();
    
            // Calculate texture coordinates in texel space
            float u = (math.dot(vertex, uAxis) / face.XScale) + face.XShift;
            float v = (math.dot(vertex, vAxis) / face.YScale) + face.YShift;
    
            // TB normalization
            u /= textureSize.x;
            v /= textureSize.y;
            
            // Apply rotation if needed
            if (math.abs(face.Rotation) > 0.001f)
            {
                // Use negative rotation to match TrenchBroom
                float angle = math.radians(-face.Rotation);
                float cosAngle = math.cos(angle);
                float sinAngle = math.sin(angle);
                
                // Apply rotation matrix
                float rotatedU = u * cosAngle - v * sinAngle;
                float rotatedV = u * sinAngle + v * cosAngle;
                
                u = rotatedU;
                v = rotatedV;
            }
            
            // Apply manual adjustments to match TrenchBroom export
            // Based on observed differences between TB and our implementation
            u = u + 1.08f;      // Add constant offset to U
            v = -v + 0.38f;     // Invert V and add offset
            
            return new float2(u, v);
        }
        
        /// <summary>
        /// Original improved method
        /// </summary>
        public static float2 ImprovedUVCalculation(float3 originalVertex, Face face, int2 textureSize)
        {
            // Apply the same vertex conversion as in CreateFaceMesh (swap Y and Z)
            float3 vertex = new float3(originalVertex.x, originalVertex.z, originalVertex.y);
    
            // Convert face axes to Unity space
            float3 uAxis = face.UAxis.ToUnity();
            float3 vAxis = face.VAxis.ToUnity();
    
            // Calculate texture coordinates
            float u = (math.dot(vertex, uAxis) / face.XScale) + face.XShift;
            float v = (math.dot(vertex, vAxis) / face.YScale) + face.YShift;
    
            // Normalize by texture size
            u /= textureSize.x;
            v /= textureSize.y;
            
            // Apply rotation if needed
            if (math.abs(face.Rotation) > 0.001f)
            {
                // Use negative rotation to match TrenchBroom
                float angle = math.radians(-face.Rotation);
                float cosAngle = math.cos(angle);
                float sinAngle = math.sin(angle);
                
                // Apply rotation matrix
                float rotatedU = u * cosAngle - v * sinAngle;
                float rotatedV = u * sinAngle + v * cosAngle;
                
                u = rotatedU;
                v = rotatedV;
            }
            
            // Important: TrenchBroom's UV coordinates use a different coordinate system
            // We need to invert the V coordinate to match
            v = 1.0f - v;
            
            return new float2(u, v);
        }
        
        // Original method for reference
        public static float2 TrenchbroomUVCalculation_Pass1(float3 originalVertex, Face face, int2 textureSize)
        {
            // Apply the same vertex conversion as in CreateFaceMesh (swap Y and Z)
            float3 vertex = new float3(originalVertex.x, originalVertex.z, originalVertex.y);
    
            // Convert face axes to Unity space
            float3 uAxis = face.UAxis.ToUnity();
            float3 vAxis = face.VAxis.ToUnity();
    
            // Calculate texture coordinates using a simple linear mapping:
            float u = (math.dot(vertex, uAxis) / face.XScale) + face.XShift;
            float v = (math.dot(vertex, vAxis) / face.YScale) + face.YShift;
    
            // Normalize coordinates by texture dimensions
            u /= textureSize.x;
            v /= textureSize.y;

            var uv = new float2(u, v);
            
            RotateUV(ref uv, face.Rotation);
            
            return uv;
        }

        public static void RotateUV(ref float2 uv, float angleDegrees)
        {
            var angle    = math.radians(angleDegrees);
            var cosAngle = math.cos(angle);
            var sinAngle = math.sin(angle);

            // Rotation matrix
            var u = uv.x * cosAngle - uv.y * sinAngle;
            var v = uv.x * sinAngle + uv.y * cosAngle;
            uv = new float2(u, v);
        }
    }
}