using Unity.Mathematics;
using UnityEngine;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace Junk.Math
{
    public static class MathPrecision
    {
        public static float3 Multiply(float3 a, float3 b)
        {
            return Vector3.Scale(a, b);
        }
        
        public static float DotCoordinate(Plane plane, float3 point)
        {
            return math.dot(plane.Normal, point) + plane.Distance;
        }
        
        /// <summary>
        /// Gets an arbitrary point on this plane.
        /// </summary>
        public static float3 GetPointOnPlane(this Plane plane)
        {
            return plane.Normal * -plane.Distance;
        }
        
        // Vector3
        public static bool EquivalentTo(this float3 self, float3 test, float delta = math.EPSILON)
        {
            var xd = math.abs(self.x - test.x);
            var yd = math.abs(self.y - test.y);
            var zd = math.abs(self.z - test.z);
            return xd < delta && yd < delta && zd < delta;
        }
        
        // Given three points that would go to System.Numerics.Plane.CreateFromVertices
        public static Plane CreateFromPoints(float3 p1, float3 p2, float3 p3)
        {
            // For Unity.Mathematics:
            float3 vector1 = p2 - p1;
            float3 vector2 = p3 - p1;
            return new Plane(vector1, vector2, p1);
        }
        
        public static double3 right   => new double3(1, 0, 0);
        public static double3 forward => new double3(0, 0, 1);
        public static double3 up      => new double3(0, 1, 0);
        
        public static bool EquivalentTo(this double3 self, double3 test, double delta = math.EPSILON_DBL)
        {
            var xd = math.abs(self.x - test.x);
            var yd = math.abs(self.y - test.y);
            var zd = math.abs(self.z - test.z);
            return xd < delta && yd < delta && zd < delta;
        }
        
        /*public static double3 Round(this double3 value, int num = 8)
        {
            return new double3(math.round(value.x, num), math.round(value.y, num), math.round(value.z, num));
        }*/
        public static double3 Round(this double3 vector3d, int num = 8)
        {
            return new double3(System.Math.Round(vector3d.x, num), System.Math.Round(vector3d.y, num), System.Math.Round(vector3d.z, num));
        }
    }
}