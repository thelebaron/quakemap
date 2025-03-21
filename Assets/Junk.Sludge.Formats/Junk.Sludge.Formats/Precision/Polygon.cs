using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace Junk.Sludge.Formats.Precision
{
    /// <summary>
    /// Represents a coplanar, directed polygon with at least 3 vertices. Uses high-precision value types.
    /// </summary>
    public class Polygon
    {
        public IReadOnlyList<float3> Vertices { get; }

        public Plane Plane => new Plane(Vertices[0], Vertices[1], Vertices[2]);
        public float3 Origin => Vertices.Aggregate(float3.zero, (x, y) => x + y) / Vertices.Count;

        /// <summary>
        /// Creates a polygon from a list of points
        /// </summary>
        /// <param name="vertices">The vertices of the polygon</param>
        public Polygon(IEnumerable<float3> vertices)
        {
            Vertices = vertices.ToList();
        }

        /// <summary>
        /// Creates a polygon from a plane and a radius.
        /// Expands the plane to the radius size to create a large polygon with 4 vertices.
        /// </summary>
        /// <param name="plane">The polygon plane</param>
        /// <param name="radius">The polygon radius</param>
        public Polygon(Plane plane, double radius = 1000000d)
        {
            // Get aligned up and right axes to the plane
            var direction = plane.GetClosestAxisToNormal();
            var tempV = direction.Equals(math.forward()) ? -math.up() : -math.forward();
            var up = tempV.Cross(plane.Normal).Normalise();
            var right = plane.Normal.Cross(up).Normalise();
            
            // Calculate a point on the plane (equivalent to PointOnPlane)
            float3 pointOnPlane = plane.Normal * -plane.Distance;

            var verts = new List<float3>
            {
                pointOnPlane + right + up, // Top right
                pointOnPlane - right + up, // Top left
                pointOnPlane - right - up, // Bottom left
                pointOnPlane + right - up, // Bottom right
            };
            
            var origin = verts.Aggregate(float3.zero, (x, y) => x + y) / verts.Count;
            Vertices = verts.Select(x => math.normalizesafe(x - origin) * (float)radius + origin).ToList();
        }

        public PlaneClassification ClassifyAgainstPlane(Plane p)
        {
            var count = Vertices.Count;
            var front = 0;
            var back = 0;
            var onplane = 0;

            foreach (var t in Vertices)
            {
                var test = p.OnPlane(t);

                // Vertices on the plane are both in front and behind the plane in this context
                if (test <= 0) back++;
                if (test >= 0) front++;
                if (test == 0) onplane++;
            }

            if (onplane == count) return PlaneClassification.OnPlane;
            if (front == count) return PlaneClassification.Front;
            if (back == count) return PlaneClassification.Back;
            return PlaneClassification.Spanning;
        }

        /// <summary>
        /// Splits this polygon by a clipping plane, returning the back and front planes.
        /// The original polygon is not modified.
        /// </summary>
        /// <param name="clip">The clipping plane</param>
        /// <param name="back">The back polygon</param>
        /// <param name="front">The front polygon</param>
        /// <returns>True if the split was successful</returns>
        public bool Split(Plane clip, out Polygon back, out Polygon front)
        {
            return Split(clip, out back, out front, out _, out _);
        }

        /// <summary>
        /// Splits this polygon by a clipping plane, returning the back and front planes.
        /// The original polygon is not modified.
        /// </summary>
        /// <param name="clip">The clipping plane</param>
        /// <param name="back">The back polygon</param>
        /// <param name="front">The front polygon</param>
        /// <param name="coplanarBack">If the polygon rests on the plane and points backward, this will not be null</param>
        /// <param name="coplanarFront">If the polygon rests on the plane and points forward, this will not be null</param>
        /// <returns>True if the split was successful</returns>
        public bool Split(Plane clip, out Polygon back, out Polygon front, out Polygon coplanarBack, out Polygon coplanarFront)
        {
            const double epsilon   = 0.1d;
            
            //var          distances = Vertices.Select(clip.EvalAtPoint).ToList();
            var          distances = Vertices.Select(v => clip.SignedDistanceToPoint(v)).ToList();
            
            int cb = 0, cf = 0;
            for (var i = 0; i < distances.Count; i++)
            {
                if (distances[i] < -epsilon) cb++;
                else if (distances[i] > epsilon) cf++;
                else distances[i] = 0;
            }

            // Check non-spanning cases
            if (cb == 0 && cf == 0)
            {
                // Co-planar
                back = front = coplanarBack = coplanarFront = null;
                if (Plane.Normal.Dot(clip.Normal) > 0) coplanarFront = this;
                else coplanarBack = this;
                return false;
            }
            else if (cb == 0)
            {
                // All vertices in front
                back = coplanarBack = coplanarFront = null;
                front = this;
                return false;
            }
            else if (cf == 0)
            {
                // All vertices behind
                front = coplanarBack = coplanarFront = null;
                back = this;
                return false;
            }

            // Get the new front and back vertices
            var backVerts = new List<float3>();
            var frontVerts = new List<float3>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var j = (i + 1) % Vertices.Count;

                float3 s = Vertices[i], e = Vertices[j];
                double sd = distances[i], ed = distances[j];

                if (sd <= 0) backVerts.Add(s);
                if (sd >= 0) frontVerts.Add(s);

                if ((sd < 0 && ed > 0) || (ed < 0 && sd > 0))
                {
                    double t         = sd / (sd - ed);
                    float3 intersect = s * (1 - (float)t) + e * (float)t;

                    backVerts.Add(intersect);
                    frontVerts.Add(intersect);
                }
            }
            
            back = new Polygon(backVerts.Select(x => new float3(x.x, x.y, x.z)));
            // front = new Polygon(frontVerts.Select(x => new float3(x.x, x.y, x.z)));
            front = null; // we throw away the front, why bother
            coplanarBack = coplanarFront = null;

            return true;
        }
    }
}
