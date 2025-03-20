using System.Collections.Generic;
using QuakeMapVisualization;
using ScriptsSandbox.Util;
using Sledge.Formats.Map.Objects;
using Tools;
using Unity.Mathematics;

namespace Mapping
{
    public static class Trenchbroom
    {
        /// <summary>
        /// Calculates Paraxial UV coordinates that match TrenchBroom's implementation
        /// This is the corrected version that aligns with TrenchBroom's ParaxialUVCoordSystem
        /// </summary>
        /*public static float2 GetParaxialUV(float3 vertex, Face face, int2 textureSize)
        {
            float3 normal = QuakeUVMapper.AsVector3(face.Plane.Normal, true);
            
            // Step 1: Determine the plane normal index
            int planeNormIndex = ParaxialUVSystem.planeNormalIndex(normal);
            
            // Step 2: Get base U and V axes
            var (uAxis, vAxis, pAxis) = ParaxialUVSystem.axes(planeNormIndex);
            
            // Step 3: Apply rotation to the base axes if needed
            if (math.abs(face.Rotation) > 0.001f)
            {
                (uAxis, vAxis) = RotateAxes(uAxis, vAxis, pAxis, face.Rotation);
            }
            
            // Calculate UV coordinates by projecting the vertex onto the U and V axes
            float2 uvCoords = new float2(
                math.dot(vertex, uAxis) / face.XScale,
                math.dot(vertex, vAxis) / face.YScale
            );
            
            // Apply texture shifts
            uvCoords.x += face.XShift / face.XScale;
            uvCoords.y += face.YShift / face.YScale;
            
            // Normalize by texture size
            uvCoords.x /= textureSize.x;
            uvCoords.y /= textureSize.y;
            
            // Special case: invert rotation for certain plane normals
            // This matches TrenchBroom's behavior where rotation is inverted for even-indexed normals
            bool rotationInverted = planeNormIndex % 2 == 0;
            if (rotationInverted)
            {
                // For OBJ export compatibility (matching TrenchBroom's OBJ exporter)
                // Flip Y coordinate for correct display in external programs
                uvCoords.y = -uvCoords.y;
            }
            
            return uvCoords;
        }
        

        /// <summary>
        /// Rotates the UV axes by the given angle in degrees around the normal
        /// </summary>
        private static (float3 u, float3 v) RotateAxes(float3 u, float3 v, float3 normal, float angleDegrees)
        {
            if (math.abs(angleDegrees) < 0.001f)
                return (u, v);

            // Convert angle to radians
            float angleRadians = math.radians(angleDegrees);
            
            // Calculate the rotation quaternion
            quaternion rotation = quaternion.AxisAngle(normal, angleRadians);
            
            // Apply rotation to axes
            float3 rotatedU = math.normalize(math.rotate(rotation, u));
            float3 rotatedV = math.normalize(math.rotate(rotation, v));
            
            return (rotatedU, rotatedV);
        }*/
        
    }
}