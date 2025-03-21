using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Math = System.Math;


namespace Junk.Sludge.Formats.Geometric
{
    /// <summary>
    /// An axis-aligned bounding box
    /// </summary>
    public class Box : IEquatable<Box>
    {
        /// <summary>
        /// An empty box with both the start and end vectors being zero.
        /// </summary>
        public static readonly Box Empty = new Box(float3.zero, float3.zero);

        /// <summary>
        /// The minimum corner of the box
        /// </summary>
        public float3 Start { get; }

        /// <summary>
        /// The maximum corner of the box
        /// </summary>
        public float3 End { get; }

        /// <summary>
        /// The center of the box
        /// </summary>
        public float3 Center => (Start + End) / 2f;

        /// <summary>
        /// The X value difference of this box
        /// </summary>
        public float Width => End.x - Start.x;

        /// <summary>
        /// The Y value difference of this box
        /// </summary>
        public float Length => End.y - Start.y;

        /// <summary>
        /// The Z value difference of this box
        /// </summary>
        public float Height => End.z - Start.z;

        /// <summary>
        /// Get the smallest dimension of this box
        /// </summary>
        public float SmallestDimension => System.Math.Min(Width, System.Math.Min(Length, Height));

        /// <summary>
        /// Get the largest dimension of this box
        /// </summary>
        public float LargestDimension => System.Math.Max(Width, System.Math.Max(Length, Height));

        /// <summary>
        /// Get the width (X), length (Y), and height (Z) of this box as a vector.
        /// </summary>
        public float3 Dimensions => new float3(Width, Length, Height);

        /// <summary>
        /// Create a box from the given start and end points.
        /// The resulting box is not guaranteed to have identical start and end vectors as provided - the
        /// resulting box will have the start and end points set to the true minimum/maximum values of
        /// each dimension (X, Y, Z).
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public Box(float3 start, float3 end) : this(new[] { start, end })
        {
        }

        /// <summary>
        /// Create a box from the given list of vectors
        /// </summary>
        /// <param name="vectors">The list of vectors to create the box from. There must be at least one vector in the list.</param>
        /// <exception cref="InvalidOperationException">If the list of vectors is empty</exception>
        public Box(IEnumerable<float3> vectors)
        {
            var list = vectors.ToList();
            if (!list.Any())
            {
                throw new ArgumentException("Cannot create a bounding box out of zero Vectors.", nameof(list));
            }
            var min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new float3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var vertex in list)
            {
                min.x = System.Math.Min(vertex.x, min.x);
                min.y = System.Math.Min(vertex.y, min.y);
                min.z = System.Math.Min(vertex.z, min.z);
                max.x = System.Math.Max(vertex.x, max.x);
                max.y = System.Math.Max(vertex.y, max.y);
                max.z = System.Math.Max(vertex.z, max.z);
            }
            Start = min;
            End = max;
        }

        /// <summary>
        /// Create a box from the given list of boxes
        /// </summary>
        /// <param name="boxes">The list of boxes to create the box from. There must be at least one box in the list.</param>
        /// <exception cref="InvalidOperationException">If the list of boxes is empty</exception>
        public Box(IEnumerable<Box> boxes)
        {
            var list = boxes.ToList();
            if (!list.Any())
            {
                throw new ArgumentException("Cannot create a bounding box out of zero other boxes.", nameof(boxes));
            }
            var min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new float3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var box in list)
            {
                min.x = System.Math.Min(box.Start.x, min.x);
                min.y = System.Math.Min(box.Start.y, min.y);
                min.z = System.Math.Min(box.Start.z, min.z);
                max.x = System.Math.Max(box.End.x, max.x);
                max.y = System.Math.Max(box.End.y, max.y);
                max.z = System.Math.Max(box.End.z, max.z);
            }
            Start = min;
            End = max;
        }

        /// <summary>
        /// The box is considered empty if the width, height, and length are all less than the provided epsilon
        /// </summary>
        /// <returns>True if the box is empty</returns>
        public bool IsEmpty(float epsilon = 0.0001f)
        {
            return math.abs(Width) < epsilon && math.abs(Height) < epsilon && math.abs(Length) < epsilon;
        }

        /// <summary>
        /// Get the 8 corners of the box
        /// </summary>
        /// <returns>A list of 8 points</returns>
        public IEnumerable<float3> GetBoxPoints()
        {
            yield return new float3(Start.x, End.y, End.z);
            yield return End;
            yield return new float3(Start.x, Start.y, End.z);
            yield return new float3(End.x, Start.y, End.z);

            yield return new float3(Start.x, End.y, Start.z);
            yield return new float3(End.x, End.y, Start.z);
            yield return Start;
            yield return new float3(End.x, Start.y, Start.z);
        }

        /// <summary>
        /// Create a polyhedron from this box
        /// </summary>
        /// <returns>This box as a polyhedron</returns>
        public Polyhedron ToPolyhedron()
        {
            return new Polyhedron(GetBoxFaces());
        }

        /// <summary>
        /// Get the 6 planes representing the sides of this box
        /// </summary>
        /// <returns>A list of 6 planes</returns>
        public IEnumerable<Plane> GetBoxPlanes()
        {
            return GetBoxFaces().Select(x => x.Plane);
        }

        /// <summary>
        /// Get the 6 polygons representing the sides of this box
        /// </summary>
        /// <returns>A list of 6 polygons</returns>
        public IEnumerable<Polygon> GetBoxFaces()
        {
            var topLeftBack = new float3(Start.x, End.y, End.z);
            var topRightBack = End;
            var topLeftFront = new float3(Start.x, Start.y, End.z);
            var topRightFront = new float3(End.x, Start.y, End.z);

            var bottomLeftBack = new float3(Start.x, End.y, Start.z);
            var bottomRightBack = new float3(End.x, End.y, Start.z);
            var bottomLeftFront = Start;
            var bottomRightFront = new float3(End.x, Start.y, Start.z);

            return new[]
            {
                new Polygon(bottomLeftFront, bottomRightFront, topRightFront, topLeftFront),
                new Polygon(bottomRightBack, bottomLeftBack, topLeftBack, topRightBack),
                new Polygon(bottomLeftBack, bottomLeftFront, topLeftFront, topLeftBack),
                new Polygon(bottomRightFront, bottomRightBack, topRightBack, topRightFront),
                new Polygon(topLeftFront, topRightFront, topRightBack, topLeftBack),
                new Polygon(bottomLeftBack, bottomRightBack, bottomRightFront, bottomLeftFront),
            };
        }

        /// <summary>
        /// Get the 12 lines representing the edges of this box
        /// </summary>
        /// <returns>A list of 12 lines</returns>
        public IEnumerable<Line> GetBoxLines()
        {
            var topLeftBack = new float3(Start.x, End.y, End.z);
            var topRightBack = End;
            var topLeftFront = new float3(Start.x, Start.y, End.z);
            var topRightFront = new float3(End.x, Start.y, End.z);

            var bottomLeftBack = new float3(Start.x, End.y, Start.z);
            var bottomRightBack = new float3(End.x, End.y, Start.z);
            var bottomLeftFront = Start;
            var bottomRightFront = new float3(End.x, Start.y, Start.z);

            yield return new Line(topLeftBack, topRightBack);
            yield return new Line(topLeftFront, topRightFront);
            yield return new Line(topLeftBack, topLeftFront);
            yield return new Line(topRightBack, topRightFront);

            yield return new Line(topLeftBack, bottomLeftBack);
            yield return new Line(topLeftFront, bottomLeftFront);
            yield return new Line(topRightBack, bottomRightBack);
            yield return new Line(topRightFront, bottomRightFront);

            yield return new Line(bottomLeftBack, bottomRightBack);
            yield return new Line(bottomLeftFront, bottomRightFront);
            yield return new Line(bottomLeftBack, bottomLeftFront);
            yield return new Line(bottomRightBack, bottomRightFront);
        }

        /// <summary>
        /// Returns true if this box overlaps the given box in any way
        /// </summary>
        public bool IntersectsWith(Box that)
        {
            if (Start.x >= that.End.x) return false;
            if (that.Start.x >= End.x) return false;

            if (Start.y >= that.End.y) return false;
            if (that.Start.y >= End.y) return false;

            if (Start.z >= that.End.z) return false;
            if (that.Start.z >= End.z) return false;

            return true;
        }

        /// <summary>
        /// Returns true if this box is completely inside the given box
        /// </summary>
        public bool ContainedWithin(Box that)
        {
            if (Start.x < that.Start.x) return false;
            if (Start.y < that.Start.y) return false;
            if (Start.z < that.Start.z) return false;

            if (End.x > that.End.x) return false;
            if (End.y > that.End.y) return false;
            if (End.z > that.End.z) return false;

            return true;
        }

        /* http://www.gamedev.net/community/forums/topic.asp?topic_id=338987 */
        /// <summary>
        /// Returns true if this box intersects the given line
        /// </summary>
        public bool IntersectsWith(Line that)
        {
            var start = that.Start;
            var finish = that.End;

            if (start.x < Start.x && finish.x < Start.x) return false;
            if (start.x > End.x && finish.x > End.x) return false;

            if (start.y < Start.y && finish.y < Start.y) return false;
            if (start.y > End.y && finish.y > End.y) return false;

            if (start.z < Start.z && finish.z < Start.z) return false;
            if (start.z > End.z && finish.z > End.z) return false;

            var d = (finish - start) / 2;
            var e = (End - Start) / 2;
            var c = start + d - ((Start + End) / 2);
            var ad = d.Absolute();

            if (math.abs(c.x) > e.x + ad.x) return false;
            if (math.abs(c.y) > e.y + ad.y) return false;
            if (math.abs(c.z) > e.z + ad.z) return false;

            var dca = d.Cross(c).Absolute();

            if (dca.x > e.y * ad.z + e.z * ad.y) return false;
            if (dca.y > e.z * ad.x + e.x * ad.z) return false;
            if (dca.z > e.x * ad.y + e.y * ad.x) return false;

            return true;
        }

        /// <summary>
        /// Returns true if the given Vector3 is inside this box.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Vector3IsInside(float3 c)
        {
            return c.x >= Start.x && c.y >= Start.y && c.z >= Start.z
                   && c.x <= End.x && c.y <= End.y && c.z <= End.z;
        }

        /// <summary>
        /// Transform this box. Each corner of the box will be transformed, and then a new box will be created using those points.
        /// The dimensions of the resulting box may change if the transform isn't a simple translation.
        /// </summary>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>A new box after the transformation has been applied</returns>
        public Box Transform(float4x4 transform)
        {
            return new Box(GetBoxPoints().Select(x => math.transform(transform,x)));
        }

        /// <summary>
        /// Create a copy of this box.
        /// </summary>
        /// <returns>A copy of the box</returns>
        public Box Clone()
        {
            return new Box(Start, End);
        }

        public bool Equals(Box other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Box)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(Box left, Box right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Box left, Box right)
        {
            return !Equals(left, right);
        }
    }
}
