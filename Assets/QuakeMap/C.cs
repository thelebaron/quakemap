using System.Runtime.CompilerServices;
using Junk.Math;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace ScriptsSandbox.QuakeMap
{
    public static class C
    {
        // from numerics
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane Transform(Plane plane, float4x4 matrix)
        {
            var m = math.inverse(matrix);

            float x = plane.Normal.x, y = plane.Normal.y, z = plane.Normal.z, w = plane.Distance;

            return new Plane(
                x * m.c0.x + y * m.c1.x + z * m.c2.x + w * m.c3.x,
                x * m.c0.y + y * m.c1.y + z * m.c2.y + w * m.c3.y,
                x * m.c0.z + y * m.c1.z + z * m.c2.z + w * m.c3.z,
                x * m.c0.w + y * m.c1.w + z * m.c2.w + w * m.c3.w);
        }
        
        public static float4 Transform(float4 plane, float4x4 matrix)
        {
            // Extract normal and distance from the plane equation (Ax + By + Cz + D = 0)
            float3 normal = plane.xyz;
            float  d      = plane.w;

            // Transform the normal using the inverse transpose of the matrix
            float3 transformedNormal = math.normalize(math.mul(math.transpose(math.inverse(matrix)), new float4(normal, 0)).xyz);

            // Transform a point on the plane
            float3 pointOnPlane     = normal * -d;
            float3 transformedPoint = math.mul(matrix, new float4(pointOnPlane, 1)).xyz;

            // Recalculate D for the transformed plane equation
            float transformedD = -math.dot(transformedNormal, transformedPoint);

            return new float4(transformedNormal, transformedD);
        }
    

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateRotatedUV(in brushFace face, out float3 rotatedUAxis, out float3 rotatedVAxis)
        {
            // Determine the dominant axis of the normal vector
            static float3 GetRotationAxis(float3 normal)
            {
                var abs = math.abs(normal);
                if (abs.x > abs.y && abs.x > abs.z)
                    return maths.right;
                else if (abs.y > abs.z)
                    return maths.up;
                else
                    return maths.forward;
            }

            // Apply scaling to the axes
            float3 scaledUAxis = face.attributes.uAxis / face.attributes.scale.x;
            float3 scaledVAxis = face.attributes.vAxis / face.attributes.scale.y;

            // Determine the rotation axis based on the face normal
            var rotationAxis   = GetRotationAxis(face.plane.Normal);
            var rotationMatrix = float4x4.AxisAngle(rotationAxis, face.attributes.rotation * math.TORADIANS);
            rotatedUAxis = math.transform(rotationMatrix, scaledUAxis);
            rotatedVAxis = math.transform(rotationMatrix, scaledVAxis);
        }
        
        public static float2 CalculateUV(in brushFace face, in float3 vertex, in float2 textureSize, in float3 rotatedUAxis, in float3 rotatedVAxis)
        {
            float2 uv;
            uv.x =  vertex.x * rotatedUAxis.x + vertex.y * rotatedUAxis.y + vertex.z * rotatedUAxis.z;
            uv.y =  vertex.x * rotatedVAxis.x + vertex.y * rotatedVAxis.y + vertex.z * rotatedVAxis.z;
            uv.x += face.attributes.offset.x;
            uv.y += face.attributes.offset.y;
            uv.x /= textureSize.x;
            uv.y /= textureSize.y;
            return uv;
        }
        
        // Convert from System.Numerics.Vector3 to UnityEngine.Vector3
        // with optional coordinate swapping for TrenchBroom compatibility
        public static float3 asfloat3(this System.Numerics.Vector3 vec, bool swapYZ = false)
        {
            if (swapYZ)
            {
                // This matches TrenchBroom's OBJ export: x, z, -y
                return new float3(vec.X, vec.Z, vec.Y);
            }
            else
            {
                return new float3(vec.X, vec.Y, vec.Z);
            }
        }
    }
}