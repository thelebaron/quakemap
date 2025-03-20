using System.Collections.Generic;
using QuakeMapVisualization;
using ScriptsSandbox.QuakeMap;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;

namespace Mapping
{
    public static class Trenchbroom
    {
        /// <summary>
        /// Calculates Paraxial UV coordinates that match TrenchBroom's implementation
        /// This is the corrected version that aligns with TrenchBroom's ParaxialUVCoordSystem
        /// </summary>
        public static float2 GetParaxialUV(float3 vertex, Face face, int2 textureSize)
        {
            float3 normal = QuakeUVMapper.AsVector3(face.Plane.Normal, true);
            
            // Step 1: Determine the plane normal index
            int planeNormIndex = PlaneNormalIndex(normal);
            
            // Step 2: Get base U and V axes
            var (uAxis, vAxis, pAxis) = GetAxes(planeNormIndex);
            
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
        
                // BaseAxes array that is used in the TrenchBroom implementation
        // This matches the axes array in ParaxialUVCoordSystem.cpp
        private static readonly float3[] BaseAxes = new float3[]
        {
            new float3(0.0f, 0.0f, 1.0f),  // 0
            new float3(1.0f, 0.0f, 0.0f),  // 1
            new float3(0.0f, -1.0f, 0.0f), // 2
            new float3(0.0f, 0.0f, -1.0f), // 3
            new float3(1.0f, 0.0f, 0.0f),  // 4
            new float3(0.0f, -1.0f, 0.0f), // 5
            new float3(1.0f, 0.0f, 0.0f),  // 6
            new float3(0.0f, 1.0f, 0.0f),  // 7
            new float3(0.0f, 0.0f, -1.0f), // 8
            new float3(-1.0f, 0.0f, 0.0f), // 9
            new float3(0.0f, 1.0f, 0.0f),  // 10
            new float3(0.0f, 0.0f, -1.0f), // 11
            new float3(0.0f, 1.0f, 0.0f),  // 12
            new float3(1.0f, 0.0f, 0.0f),  // 13
            new float3(0.0f, 0.0f, -1.0f), // 14
            new float3(0.0f, -1.0f, 0.0f), // 15
            new float3(1.0f, 0.0f, 0.0f),  // 16
            new float3(0.0f, 0.0f, -1.0f)  // 17
        };

        /// <summary>
        /// Determines the index of the plane normal in the BaseAxes array
        /// This matches the TrenchBroom implementation in ParaxialUVCoordSystem::planeNormalIndex
        /// </summary>
        private static int PlaneNormalIndex(float3 normal)
        {
            int bestIndex = 0;
            float bestDot = 0;
            
            for (int i = 0; i < 6; ++i)
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

        /// <summary>
        /// Gets the U, V, and P axes for the given plane normal index
        /// This matches the TrenchBroom implementation in ParaxialUVCoordSystem::axes
        /// </summary>
        private static (float3 u, float3 v, float3 p) GetAxes(int index)
        {
            return (
                BaseAxes[index * 3 + 1],
                BaseAxes[index * 3 + 2],
                BaseAxes[(index / 2) * 6]
            );
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
        }
        
    }
}