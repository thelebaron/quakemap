using System.Collections.Generic;
using System.Linq;

using Junk.Sludge.Formats;
using Junk.Sludge.Formats.Geometric;
using Unity.Mathematics;

namespace Junk.Math
{
    /// <summary>
    /// Represents a coplanar, directed polygon with at least 3 vertices. Uses double precision floating points.
    /// </summary>
    public class Polygond
    {
        /// <summary>
        /// The vertices for the polygon, in counter-clockwise order when looking at the visible face of the polygon.
        /// </summary>
        public IReadOnlyList<double3> Vertices { get; }

        public Planed  Plane  => Planed.CreateFromVertices(Vertices[0], Vertices[1], Vertices[2]);
        public double3 Origin => Vertices.Aggregate(double3.zero, (x, y) => x + y) / Vertices.Count;


        /*public Polygond(IEnumerable<float3> vertices) : this(vertices.Select(x => x))
        {
            //
        }*/

        /// <summary>
        /// Creates a polygon from a list of points
        /// </summary>
        /// <param name="vertices">The vertices of the polygon</param>
        public Polygond(IEnumerable<double3> vertices)
        {
            Vertices = vertices.ToList();
        }

        /// <summary>
        /// Creates a polygon from a list of points
        /// </summary>
        /// <param name="vertices">The vertices of the polygon</param>
        public Polygond(params double3[] vertices)
        {
            Vertices = vertices.ToList();
        }
/*
        public Polygond(Plane plane, float radius = 1000000f) : this(plane, radius)
        {
            //
        }*/

        /// <summary>
        /// Creates a polygon from a plane and a radius.
        /// Expands the plane to the radius size to create a large polygon with 4 vertices.
        /// </summary>
        /// <param name="plane">The polygon plane</param>
        /// <param name="radius">The polygon radius</param>
        public Polygond(Planed plane, double radius = 1000000d)
        {
            // Get aligned up and right axes to the plane
            var direction = plane.GetClosestAxisToNormal();
            var up        = direction.Equals(MathPrecision.forward) ? MathPrecision.right : -MathPrecision.forward;
            var v         = math.dot(up, plane.Normal);// up.Dot(plane.Normal);
            //up = (up + -v * plane.Normal).Normalise();
            //var right = up.Cross(plane.Normal);
            up = math.normalize(up - v * plane.Normal);
            var right = math.cross(up, plane.Normal);

            up *= radius;
            right *= radius;

            var origin = plane.GetPointOnPlane();
            var verts = new List<double3>
            {
                origin - right - up, // Bottom left
                origin + right - up, // Bottom right
                origin + right + up, // Top right
                origin - right + up, // Top left
            };
            Vertices = verts.ToList();
        }

        public PlaneClassification ClassifyAgainstPlane(Planed p)
        {
            var count = Vertices.Count;
            var front = 0;
            var back = 0;
            var onplane = 0;

            foreach (var t in Vertices)
            {
                var test = p.OnPlane(t);

                // Vertices on the plane are both in front and behind the plane in this context
                if (test == PlaneClassification.Back) back++;
                if (test == PlaneClassification.Front) front++;
                if (test == PlaneClassification.OnPlane) onplane++;
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
        public bool Split(Planed clip, out Polygond back, out Polygond front)
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
        public bool Split(Planed clip, out Polygond back, out Polygond front, out Polygond coplanarBack, out Polygond coplanarFront)
        {
            const double epsilon = NumericsExtensions.Epsilon;

            var distances = Vertices.Select(clip.DotCoordinate).ToList();

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
                //if (Plane.Normal.Dot(clip.Normal) > 0) coplanarFront = this;
                if (math.dot(Plane.Normal, clip.Normal) > 0)
                    coplanarFront = this;
                else 
                    coplanarBack = this;
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
            var backVerts = new List<double3>();
            var frontVerts = new List<double3>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var j = (i + 1) % Vertices.Count;

                double3 s = Vertices[i], e = Vertices[j];
                double sd = distances[i], ed = distances[j];

                if (sd <= 0) backVerts.Add(s);
                if (sd >= 0) frontVerts.Add(s);

                if (sd < 0 && ed > 0 || ed < 0 && sd > 0)
                {
                    var t = sd / (sd - ed);
                    var intersect = s * (1 - t) + e * t;

                    // avoid round off error when possible
                    // (if we know the clipping plane is a unit vector, then the intersection point on that axis will always be the plane's distance from the origin)
                    if (clip.Normal.Equals(MathPrecision.right)) intersect    = new double3(-clip.Distance, intersect.y, intersect.z);
                    else if (clip.Normal.Equals(-MathPrecision.right)) intersect   = new double3(clip.Distance, intersect.y, intersect.z);
                    else if (clip.Normal.Equals(MathPrecision.up)) intersect       = new double3(intersect.x, -clip.Distance, intersect.z);
                    else if (clip.Normal.Equals(-MathPrecision.up)) intersect      = new double3(intersect.x, clip.Distance, intersect.z);
                    else if (clip.Normal.Equals(MathPrecision.forward)) intersect  = new double3(intersect.x, intersect.y, -clip.Distance);
                    else if (clip.Normal.Equals(-MathPrecision.forward)) intersect = new double3(intersect.x, intersect.y, clip.Distance);

                    backVerts.Add(intersect);
                    frontVerts.Add(intersect);
                }
            }

            back = new Polygond(backVerts.Select(x => new double3(x.x, x.y, x.z)));
            front = new Polygond(frontVerts.Select(x => new double3(x.x, x.y, x.z)));
            coplanarBack = coplanarFront = null;

            return true;
        }

        public Polygon ToPolygon()
        {
            return new Polygon(Vertices.Select(x => (float3)x));
        }

        public Polygond Rounded(int num)
        {
            return new Polygond(Vertices.Select(x => x.Round(num)));
        }
    }
}
