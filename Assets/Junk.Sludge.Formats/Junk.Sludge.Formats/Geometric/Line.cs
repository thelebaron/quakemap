using System;
using Junk.Math;
using Unity.Mathematics;

namespace Junk.Sludge.Formats.Geometric
{
    public class Line
    {
        public float3 Start;
        public float3 End;

        public static readonly Line AxisX = new Line(float3.zero, math.right());
        public static readonly Line AxisY = new Line(float3.zero,math.up());
        public static readonly Line AxisZ = new Line(float3.zero, math.forward());

        public Line(float3 start, float3 end)
        {
            Start = start;
            End   = end;
        }

        public Line Reverse()
        {
            return new Line(End, Start);
        }

        public float3 ClosestPoint(float3 point)
        {
            // http://paulbourke.net/geometry/pointline/

            var delta = End - Start;
            var den   = math.lengthsq(delta);
            if (math.abs(den) < 0.0001f) return Start; // Start and End are the same

            var numPoint = Junk.Math.MathPrecision.Multiply(point - Start, delta);
            var num      = numPoint.x + numPoint.y + numPoint.z;
            var u        = num / den;

            if (u < 0) return Start; // Point is before the segment start
            if (u > 1) return End;   // Point is after the segment end
            return Start + u * delta;
        }

        public bool EquivalentTo(Line other, float delta = 0.0001f)
        {
            return Start.EquivalentTo(other.Start, delta) && End.EquivalentTo(other.End, delta)
                || End.EquivalentTo(other.Start, delta) && Start.EquivalentTo(other.End, delta);
        }

        public Lined ToLined()
        {
            return new Lined(Start.Todouble3(), End.Todouble3());
        }

        private bool Equals(Line other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Start, Start) && Equals(other.End, End)
                || Equals(other.End, Start) && Equals(other.Start, End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Line)) return false;
            return Equals((Line)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(Line left, Line right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Line left, Line right)
        {
            return !Equals(left, right);
        }
    }
}
