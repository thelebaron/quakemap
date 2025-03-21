using Junk.Math;
using System;
using System.Drawing;
using System.Globalization;
using Junk.Sludge.Formats.Geometric;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Math = System.Math;

namespace Junk.Sludge.Formats
{
    public static class NumericsExtensions
    {
        public const float Epsilon = 0.0001f;

        // Vector2
        public static float3 ToVector3(this float2 self)
        {
            return new float3(self, 0);
        }

        // float3
        public static bool EquivalentTo(this float3 self, float3 test, float delta = Epsilon)
        {
            var xd = math.abs(self.x - test.x);
            var yd = math.abs(self.y - test.y);
            var zd = math.abs(self.z - test.z);
            return xd < delta && yd < delta && zd < delta;
        }

        public static bool EquivalentTo(this float3 self, double3 test, float delta = Epsilon)
        {
            var xd = math.abs(self.x - test.x);
            var yd = math.abs(self.y - test.y);
            var zd = math.abs(self.z - test.z);
            return xd < delta && yd < delta && zd < delta;
        }

        public static float3 Parse(string x, string y, string z, NumberStyles ns, IFormatProvider provider)
        {
            return new float3(float.Parse(x, ns, provider), float.Parse(y, ns, provider), float.Parse(z, ns, provider));
        }

        public static bool TryParse(string x, string y, string z, NumberStyles ns, IFormatProvider provider, out float3 vec)
        {
            if (float.TryParse(x, ns, provider, out var a) && float.TryParse(y, ns, provider, out var b) && float.TryParse(z, ns, provider, out var c))
            {
                vec = new float3(a, b, c);
                return true;
            }

            vec = float3.zero;
            return false;
        }


        /// <inheritdoc cref="float3.Normalize"/>
        public static float3 Normalise(this float3 self) => math.normalizesafe(self);

        /// <inheritdoc cref="math.abs"/>
        public static float3 Absolute(this float3 self) => math.abs(self);

        /// <inheritdoc cref="float3.Dot"/>
        public static float Dot(this float3 self, float3 other) => math.dot(self, other);

        /// <inheritdoc cref="float3.Cross"/>
        public static float3 Cross(this float3 self, float3 other) => math.cross(self, other);

        public static float3 Round(this float3 self, int num = 8) => new float3((float) System.Math.Round(self.x, num), (float) System.Math.Round(self.y, num), (float) System.Math.Round(self.z, num));

        /// <summary>
        /// Gets the axis closest to this vector
        /// </summary>
        /// <returns>math.right(), math.up(), or math.forward() depending on the given vector</returns>
        public static float3 ClosestAxis(this float3 self)
        {
            // VHE prioritises the axes in order of X, Y, Z.
            var norm = math.abs(self);

            if (norm.x >= norm.y && norm.x >= norm.z) return math.right();
            if (norm.y >= norm.z) return math.up();
            return math.forward();
        }

        public static double3 Todouble3(this float3 self)
        {
            return new double3(self.x, self.y, self.z);
        }

        public static float2 ToVector2(this float3 self)
        {
            return new float2(self.x, self.y);
        }

        // Vector4
        public static float4 ToVector4(this Color self)
        {
            return new float4(self.R, self.G, self.B, self.A) / 255f;
        }

        // Color
        public static Color ToColor(this float4 self)
        {
            var mul = self * 255;
            return Color.FromArgb((byte) mul.w, (byte) mul.x, (byte) mul.y, (byte) mul.z);
        }

        public static Color ToColor(this float3 self)
        {
            var mul = self * 255;
            return Color.FromArgb(255, (byte) mul.x, (byte) mul.y, (byte) mul.z);
        }

        // Plane
        
        /// <summary>
        /// Gets an arbitrary point on this plane.
        /// </summary>
        public static float3 GetPointOnPlane(this Plane plane)
        {
            return plane.Normal * -plane.Distance;
        }

        /// <summary>Finds if the given point is above, below, or on the plane.</summary>
        /// <param name="plane">The plane</param>
        /// <param name="co">The float3 to test</param>
        /// <param name="epsilon">Tolerance value</param>
        /// <returns>
        /// PlaneClassification.Back if float3 is below the plane<br />
        /// PlaneClassification.Front if float3 is above the plane<br />
        /// PlaneClassification.OnPlane if float3 is on the plane.
        /// </returns>
        public static PlaneClassification OnPlane(this Plane plane, float3 co, double epsilon = 0.0001d)
        {
            //eval (s = Ax + By + Cz + D) at point (x,y,z)
            //if s > 0 then point is "above" the plane (same side as normal)
            //if s < 0 then it lies on the opposite side
            //if s = 0 then the point (x,y,z) lies on the plane
            var res = DotCoordinate(plane, co);
            if (math.abs(res) < epsilon) return PlaneClassification.OnPlane;
            if (res < 0) return PlaneClassification.Back;
            return PlaneClassification.Front;
        }

        /// <summary>
        /// Gets the point that the line intersects with this plane.
        /// </summary>
        /// <param name="plane">The plane</param>
        /// <param name="start">The start of the line to intersect with</param>
        /// <param name="end">The end of the line to intersect with</param>
        /// <param name="ignoreDirection">Set to true to ignore the direction
        /// of the plane and line when intersecting. Defaults to false.</param>
        /// <param name="ignoreSegment">Set to true to ignore the start and
        /// end points of the line in the intersection. Defaults to false.</param>
        /// <returns>The point of intersection, or null if the line does not intersect</returns>
        public static float3? GetIntersectionPoint(this Plane plane, float3 start, float3 end, bool ignoreDirection = false, bool ignoreSegment = false)
        {
            // http://softsurfer.com/Archive/algorithm_0104/algorithm_0104B.htm#Line%20Intersections
            // http://paulbourke.net/geometry/planeline/

            var dir = end - start;
            var denominator = plane.Normal.Dot(dir);
            var numerator = plane.Normal.Dot(GetPointOnPlane(plane) - start);
            if (math.abs(denominator) < 0.00001d || (!ignoreDirection && denominator < 0)) return null;
            var u = numerator / denominator;
            if (!ignoreSegment && (u < 0 || u > 1)) return null;
            return start + u * dir;
        }

        /// <summary>
        /// Project a point into the space of this plane. I.e. Get the point closest
        /// to the provided point that is on this plane.
        /// </summary>
        /// <param name="plane">The plane</param>
        /// <param name="point">The point to project</param>
        /// <returns>The point projected onto this plane</returns>
        public static float3 Project(this Plane plane, float3 point)
        {
            // http://www.gamedev.net/topic/262196-projecting-vector-onto-a-plane/
            // Projected = Point - ((Point - PointOnPlane) . Normal) * Normal
            return point - ((point - GetPointOnPlane(plane)).Dot(plane.Normal)) * plane.Normal;
        }

        /// <inheritdoc cref="Plane.DotCoordinate"/>
        public static float DotCoordinate(this Plane plane, float3 coordinate)
        {
            return Junk.Math.MathPrecision.DotCoordinate(plane, coordinate);
        }

        /// <summary>
        /// Gets the axis closest to the normal of this plane
        /// </summary>
        /// <returns>math.right(), math.up(), or math.forward() depending on the plane's normal</returns>
        public static float3 GetClosestAxisToNormal(this Plane plane) => ClosestAxis(plane.Normal);

        /// <summary>
        /// Intersects three planes and gets the point of their intersection.
        /// </summary>
        /// <returns>The point that the planes intersect at, or null if they do not intersect at a point.</returns>
        public static float3? IntersectPlanes(Plane p1, Plane p2, Plane p3)
        {
            // http://paulbourke.net/geometry/3planes/

            var c1 = p2.Normal.Cross(p3.Normal);
            var c2 = p3.Normal.Cross(p1.Normal);
            var c3 = p1.Normal.Cross(p2.Normal);

            var denom = p1.Normal.Dot(c1);
            if (denom < 0.00001d) return null; // No intersection, planes must be parallel

            var numer = (-p1.Distance * c1) + (-p2.Distance * c2) + (-p3.Distance * c3);
            return numer / denom;
        }

        public static bool EquivalentTo(this Plane plane, Plane other, float delta = 0.0001f)
        {
            return plane.Normal.EquivalentTo(other.Normal, delta)
                   && math.abs(plane.Distance - other.Distance) < delta;
        }

        public static Planed ToPlaned(this Plane plane)
        {
            return new Planed(plane.Normal.Todouble3(), plane.Distance);
        }

        public static Plane Flip(this Plane plane)
        {
            return new Plane(-plane.Normal, -plane.Distance);
        }

        // Matrix
        public static float3 Transform(this float4x4 self, float3 vector) => math.transform(self,vector);

        // https://github.com/ericwa/ericw-tools/blob/master/qbsp/map.cc @TextureAxisFromPlane
        // ReSharper disable once UnusedTupleComponentInReturnValue
        public static (float3 uAxis, float3 vAxis, float3 snappedNormal) GetQuakeTextureAxes(this Plane plane)
        {
            var baseaxis = new[]
            {
                new float3(0, 0, 1), new float3(1, 0, 0), new float3(0, -1, 0), // floor
                new float3(0, 0, -1), new float3(1, 0, 0), new float3(0, -1, 0), // ceiling
                new float3(1, 0, 0), new float3(0, 1, 0), new float3(0, 0, -1), // west wall
                new float3(-1, 0, 0), new float3(0, 1, 0), new float3(0, 0, -1), // east wall
                new float3(0, 1, 0), new float3(1, 0, 0), new float3(0, 0, -1), // south wall
                new float3(0, -1, 0), new float3(1, 0, 0), new float3(0, 0, -1) // north wall
            };

            var best = 0f;
            var bestaxis = 0;

            for (var i = 0; i < 6; i++)
            {
                var dot = plane.Normal.Dot(baseaxis[i * 3]);
                if (!(dot > best)) continue;

                best = dot;
                bestaxis = i;
            }

            return (baseaxis[bestaxis * 3 + 1], baseaxis[bestaxis * 3 + 2], baseaxis[bestaxis * 3]);
        }
    }
}