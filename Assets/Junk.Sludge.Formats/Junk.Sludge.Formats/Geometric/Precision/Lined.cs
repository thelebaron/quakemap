using System;

using Junk.Sludge.Formats.Geometric;
using Unity.Mathematics;

namespace Junk.Math
{
    public class Lined
    {
        public double3 Start;
        public double3 End;

        public static readonly Lined AxisX = new Lined(double3.zero, MathPrecision.right);
        public static readonly Lined AxisY = new Lined(double3.zero, MathPrecision.up);
        public static readonly Lined AxisZ = new Lined(double3.zero, MathPrecision.forward);

        public Lined(double3 start, double3 end)
        {
            Start = start;
            End = end;
        }

        public Lined Reverse()
        {
            return new Lined(End, Start);
        }

        public double3 ClosestPoint(double3 point)
        {
            // http://paulbourke.net/geometry/pointline/

            var delta = End - Start;
            var den   = math.lengthsq(delta);
            if (math.abs(den) < 0.0001f) return Start; // Start and End are the same

            var numPoint = (point - Start) * delta;
            var num = numPoint.x + numPoint.y + numPoint.z;
            var u = num / den;

            if (u < 0) return Start; // Point is before the segment start
            if (u > 1) return End;   // Point is after the segment end
            return Start + u * delta;
        }

        public bool EquivalentTo(Lined other, float delta = 0.0001f)
        {
            return Start.EquivalentTo(other.Start, delta) && End.EquivalentTo(other.End, delta)
                || End.EquivalentTo(other.Start, delta) && Start.EquivalentTo(other.End, delta);
        }

        public Line ToLine()
        {
            return new Line((float3)Start, (float3)End);
        }

        private bool Equals(Lined other)
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
            if (obj.GetType() != typeof(Lined)) return false;
            return Equals((Lined)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(Lined left, Lined right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Lined left, Lined right)
        {
            return !Equals(left, right);
        }
    }
}
