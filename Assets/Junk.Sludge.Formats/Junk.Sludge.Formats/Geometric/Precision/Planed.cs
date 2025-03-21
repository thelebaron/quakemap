
using Junk.Sludge.Formats.Geometric;
using Unity.Mathematics;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace Junk.Math
{
    /// <summary>
    /// Defines a plane in the form Ax + By + Cz + D = 0. Uses double precision floating points.
    /// </summary>
    public struct Planed
    {
        public double3 Normal;
        public double  Distance;

        /*public Planed(double3 normal, float d) : this(normal, d)
        {
            //
        }*/

        public Planed(double3 normal, double distance)
        {
            Normal = math.normalize(normal);
            Distance = distance;
        }

        public static Plane Plane(Planed planed)
        {
            return new Plane((float3)planed.Normal, (float3)planed.Distance);
        }
        
        public static implicit operator Plane(Planed planed)
        {
            return new Plane((float3)planed.Normal, (float3)planed.Distance);
        }

        public static implicit operator Planed((double3 normal, double d) planed)
        {
            return new Planed(planed.normal, planed.d);
        }
            
        public static implicit operator Planed(Plane plane)
        {
            return new Planed(plane.Normal, plane.Distance);
        }

        /// <summary>
        /// Gets an arbitrary point on this plane.
        /// </summary>
        public double3 GetPointOnPlane()
        {
            return Normal * -Distance;
        }

        public static Planed CreateFromVertices(float3 p1, float3 p2, float3 p3)
        {
            return Planed.CreateFromVertices( (double3)p1,  (double3)p2, (double3)p3);
        }

        /// <summary>
        /// Create a plane from 3 vertices. Assumes that the vertices are ordered counter-clockwise.
        /// </summary>
        public static Planed CreateFromVertices(double3 p1, double3 p2, double3 p3)
        {
            var a = p2 - p1;
            var b = p3 - p1;
            
            var normal = math.normalize(math.cross(a, b));
            var d      = -math.dot(normal, p1);
            
            return new Planed(normal, d);
        }

        /// <summary>Finds if the given point is above, below, or on the plane.</summary>
        /// <param name="co">The float3 to test</param>
        /// <param name="epsilon">Tolerance value</param>
        /// <returns>
        /// PlaneClassification.Back if float3 is below the plane<br />
        /// PlaneClassification.Front if float3 is above the plane<br />
        /// PlaneClassification.OnPlane if float3 is on the plane.
        /// </returns>
        public PlaneClassification OnPlane(double3 co, double epsilon = 0.0001d)
        {
            //eval (s = Ax + By + Cz + D) at point (x,y,z)
            //if s > 0 then point is "above" the plane (same side as normal)
            //if s < 0 then it lies on the opposite side
            //if s = 0 then the point (x,y,z) lies on the plane
            var res = DotCoordinate(co);
            if (math.abs(res) < epsilon) return PlaneClassification.OnPlane;
            if (res < 0) return PlaneClassification.Back;
            return PlaneClassification.Front;
        }

        /// <summary>
        /// Gets the point that the line intersects with this plane.
        /// </summary>
        /// <param name="start">The start of the line to intersect with</param>
        /// <param name="end">The end of the line to intersect with</param>
        /// <param name="ignoreDirection">Set to true to ignore the direction
        /// of the plane and line when intersecting. Defaults to false.</param>
        /// <param name="ignoreSegment">Set to true to ignore the start and
        /// end points of the line in the intersection. Defaults to false.</param>
        /// <returns>The point of intersection, or null if the line does not intersect</returns>
        public double3? GetIntersectionPoint(double3 start, double3 end, bool ignoreDirection = false, bool ignoreSegment = false)
        {
            // http://softsurfer.com/Archive/algorithm_0104/algorithm_0104B.htm#Line%20Intersections
            // http://paulbourke.net/geometry/planeline/

            var dir         = end - start;
            var denominator = math.dot(Normal,dir);
            var numerator   = math.dot(Normal,(GetPointOnPlane() - start));
            if (math.abs(denominator) < 0.00001d || !ignoreDirection && denominator < 0) return null;
            var u = numerator / denominator;
            if (!ignoreSegment && (u < 0 || u > 1)) return null;
            return start + u * dir;
        }

        /// <summary>
        /// Project a point into the space of this plane. I.e. Get the point closest
        /// to the provided point that is on this plane.
        /// </summary>
        /// <param name="point">The point to project</param>
        /// <returns>The point projected onto this plane</returns>
        public double3 Project(double3 point)
        {
            // http://www.gamedev.net/topic/262196-projecting-vector-onto-a-plane/
            // Projected = Point - ((Point - PointOnPlane) . Normal) * Normal
            return point - math.dot(point - GetPointOnPlane(), Normal) * Normal;
        }

        /// <summary>Evaluates the value of the plane formula at the given coordinate.</summary>
        /// <remarks>Returns the dot product of a specified three-dimensional vector and the normal vector of this plane plus the distance (<see cref="System.Numerics.Plane.Distance" />) value of the plane.</remarks>
        public double DotCoordinate(double3 co)
        {
            return math.dot(Normal, co) + Distance;
        }

        /// <summary>
        /// Gets the axis closest to the normal of this plane
        /// </summary>
        /// <returns>float3.UnitX, math.up(), or math.forward() depending on the plane's normal</returns>
        public double3 GetClosestAxisToNormal()
        {
            // VHE prioritises the axes in order of X, Y, Z.
            var norm = math.abs(Normal);

            if (norm.x >= norm.y && norm.x >= norm.z) return MathPrecision.right;
            if (norm.y >= norm.z) return MathPrecision.up;
            return MathPrecision.forward;
        }
        

        public Planed Clone()
        {
            return new Planed(Normal, Distance);
        }

        /// <summary>
        /// Intersects three planes and gets the point of their intersection.
        /// </summary>
        /// <returns>The point that the planes intersect at, or null if they do not intersect at a point.</returns>
        public static double3? Intersect(Planed p1, Planed p2, Planed p3)
        {
            var c1 = math.cross(p2.Normal, p3.Normal);
            var c2 = math.cross(p3.Normal, p1.Normal);
            var c3 = math.cross(p1.Normal, p2.Normal);

            var denom = math.dot(p1.Normal, c1);
            if (denom < 0.00001d) 
                return null; // No intersection, planes must be parallel

            var numer = -p1.Distance * c1 + -p2.Distance * c2 + -p3.Distance * c3;
            return numer / denom;
        }


        public bool EquivalentTo(Planed other, double delta = 0.0001d)
        {
            return Normal.EquivalentTo(other.Normal, delta)
                   && math.abs(Distance - other.Distance) < delta;
        }

        public Unity.Mathematics.Geometry.Plane ToPlane()
        {
            return new Unity.Mathematics.Geometry.Plane((float3)Normal, (float) Distance);
        }

        public Planed Flip()
        {
            return new Planed(-Normal, -Distance);
        }
    }
}
