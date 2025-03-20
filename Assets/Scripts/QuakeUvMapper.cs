using System;
using System.Collections.Generic;
using ScriptsSandbox.Util;
using Sledge.Formats.Geometric;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;

namespace QuakeMapVisualization
{
    public static class QuakeUVMapper
    {
        public static Vector3 AsVector3(this System.Numerics.Vector3 self, bool reorderQuakeToUnity = false)
        {
            if (reorderQuakeToUnity)
            {
                return new Vector3(self.X, self.Z, self.Y);
            }
            
            return new Vector3(self.X, self.Y, self.Z);
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Calculates proper UV coordinates for Quake map faces based on TrenchBroom's calculation method
        /// </summary>
        public static Vector2 CalculateFaceUV(Vector3 vertex, Face face, Vector2 textureSize)
        {
            // Important: The vertices coming in are already transformed from Quake to Unity coordinate system
            // This means Y and Z have been swapped
            
            // Calculate base UV coordinates using dot products
            float u = Vector3.Dot(vertex, face.UAxis.AsVector3()) / face.XScale;
            float v = Vector3.Dot(vertex, face.VAxis.AsVector3()) / face.YScale;
            
            // Apply texture shifts (offsets)
            u += face.XShift;
            v += face.YShift;
            
            // At this point we have u,v in absolute texel space - now we need to normalize
            // and handle rotation properly
            
            // First, normalize by texture size
            float normalizedU = u / textureSize.x;
            float normalizedV = v / textureSize.y;
            
            // Handle rotation - the rotation needs to happen in normalized UV space
            if (Mathf.Abs(face.Rotation) > 0.001f) // Only rotate if we actually have rotation
            {
                // Convert rotation angle to radians (negative because of coordinate system differences)
                float radians = -face.Rotation * Mathf.Deg2Rad;
                
                // Rotation calculation based on TrenchBroom's approach
                // We don't need to rotate around a specific center point - the UV axes already 
                // handle the proper pivot for rotation
                float cosAngle = Mathf.Cos(radians);
                float sinAngle = Mathf.Sin(radians);
                
                float rotatedU = normalizedU * cosAngle - normalizedV * sinAngle;
                float rotatedV = normalizedU * sinAngle + normalizedV * cosAngle;
                
                normalizedU = rotatedU;
                normalizedV = rotatedV;
            }
            
            // Return the final UV coordinates
            return new Vector2(normalizedU, normalizedV);
        }
        
        /// <summary>
        /// Direct implementation based on original TrenchbroomUVCalculation method
        /// with fixes for proper rotation
        /// </summary>
        public static Vector2 CalculateTrenchBroomStyleUV(Vector3 vertex, Face face, Vector2 textureSize)
        {
            // Calculate the base UVs in absolute texel space
            float u = Vector3.Dot(vertex, face.UAxis.AsVector3()) / face.XScale + face.XShift;
            float v = Vector3.Dot(vertex, face.VAxis.AsVector3()) / face.YScale + face.YShift;
            
            // Normalize by texture dimensions
            u /= textureSize.x;
            v /= textureSize.y;
            
            // Handle rotation
            if (Mathf.Abs(face.Rotation) > 0.001f)
            {
                // Convert to radians with correct sign
                float angle = -face.Rotation * Mathf.Deg2Rad;
                
                // Apply rotation matrix
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);
                
                float newU = u * cosAngle - v * sinAngle;
                float newV = u * sinAngle + v * cosAngle;
                
                u = newU;
                v = newV;
            }
            
            return new Vector2(u, v);
        }
    }
}