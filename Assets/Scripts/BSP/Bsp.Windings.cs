using Unity.Mathematics;

namespace BSP
{
    public static partial class Bsp
    {
        //
        // Example C# versions of the Quake qbsp Winding utilities:
        //  - FreeWinding
        //  - CopyWinding
        //  - ClipWinding
        //
        // These rely on the winding_t/plane_t structs, and various helpers like
        // math.dot, etc. Adjust them to match your environment.
        //

        public static void FreeWinding(winding_t w)
        {
            // In classic C code, this would free memory. In C#, garbage
            // collection handles that, so we can null out the array or do
            // nothing. We can also reset fields to mark it invalid:
            w.points    = null;
            w.numpoints = 0;
        }

        // Makes a new winding that is a duplicate of the input
        public static winding_t CopyWinding(winding_t src)
        {
            winding_t dst = new winding_t();
            dst.numpoints = src.numpoints;
            dst.points    = new float3[src.numpoints];
            for (int i = 0; i < src.numpoints; i++)
            {
                dst.points[i] = src.points[i];
            }

            return dst;
        }

        /*
        ====================
        ClipWinding

        Clips winding_t 'src' by the plane 'split'. If 'keepon' is true,
        points exactly on the plane go to the front side. Returns a new winding.

        This is similar to the Quake code that divides a polygon winding
        by a plane, building a new winding containing only the front portion.
        ====================
        */
        public static winding_t ClipWinding(winding_t src, plane_t split, bool keepon)
        {
            if (src == null)
                return null;

            // Distances/sides for each vertex of the source winding
            float[] dists = new float[src.numpoints + 1];
            int[]   sides = new int[src.numpoints   + 1];

            // Evaluate each point against the plane
            for (int i = 0; i < src.numpoints; i++)
            {
                float dot = math.dot(src.points[i], split.normal) - split.dist;
                dists[i] = dot;
                if (dot > ON_EPSILON)
                    sides[i] = 0; // FRONT
                else if (dot < -ON_EPSILON)
                    sides[i] = 1; // BACK
                else
                    sides[i] = keepon ? 0 : 2; // '0' if keepon is true, else 'ON plane'
            }

            // Wrap around to first point
            dists[src.numpoints] = dists[0];
            sides[src.numpoints] = sides[0];

            // Count front/back
            int frontCount = 0, backCount = 0;
            for (int i = 0; i < src.numpoints; i++)
            {
                if (sides[i] == 0)
                    frontCount++;
                else if (sides[i] == 1)
                    backCount++;
            }

            // If nothing is on front side, entire winding is clipped away
            if (frontCount == 0)
            {
                FreeWinding(src);
                return null;
            }

            // If nothing ended up on the back side, then the winding is fully in front
            if (backCount == 0)
                return src;

            // Build a new winding for the front side
            // We'll guess it can hold up to src.numpoints+4 in worst-case splits
            winding_t dst = new winding_t();
            dst.points = new float3[src.numpoints + 4];
            int dstCount = 0;

            // Walk each edge and see if we cross the plane
            for (int i = 0; i < src.numpoints; i++)
            {
                float3 p1    = src.points[i];
                int    side1 = sides[i];

                // If p1 is on the front (or on-plane with keepon), add it
                if (side1 != 1) // not back side
                {
                    dst.points[dstCount++] = p1;
                }

                int next = (i + 1) % src.numpoints;
                if (side1 == 2) // 'on-plane' with keepon=false => skip intersection
                    continue;

                int side2 = sides[next];
                if (side2 == 2) // also on-plane => skip intersection
                    continue;
                if (side2 == side1)
                    continue; // both on same side, no crossing

                // compute intersection point
                float  dot = dists[i] / (dists[i] - dists[next]);
                float3 p2  = src.points[next];
                float3 mid = p1 + dot * (p2 - p1);

                dst.points[dstCount++] = mid;
            }

            dst.numpoints = dstCount;
            // If we ended up with fewer than 3 points, it's invalid
            if (dst.numpoints < 3)
            {
                // The winding basically collapsed
                FreeWinding(src);
                FreeWinding(dst);
                return null;
            }

            // Trim any unused array space
            float3[] finalPoints = new float3[dst.numpoints];
            for (int i = 0; i < dst.numpoints; i++)
                finalPoints[i] = dst.points[i];
            dst.points = finalPoints;

            // free original
            FreeWinding(src);
            return dst;
        }

        //---------------------------------------------------------------
        // BaseWindingForPlane
        //---------------------------------------------------------------
        public static winding_t BaseWindingForPlane(plane_t p)
        {
            // This code sets up a big quad (4 corners) far out on the plane
            // The original code picks a major axis, picks vup accordingly, etc.
            // We'll do a simplified approach:

            // find the major axis
            float max    = -99999f;
            int   xIndex = -1;
            for (int i = 0; i < 3; i++)
            {
                float v = math.abs(p.normal[i]);
                if (v > max)
                {
                    max    = v;
                    xIndex = i;
                }
            }

            if (xIndex == -1)
                Error("BaseWindingForPlane: no axis found");

            float3 vup = float3.zero;
            switch (xIndex)
            {
                case 0:
                case 1:
                    vup.z = 1f;
                    break;
                case 2:
                    vup.x = 1f;
                    break;
            }

            float dot = math.dot(vup, p.normal);
            vup = vup - dot * p.normal;
            vup = math.normalize(vup);

            float3 vright = math.cross(vup, p.normal);
            vright = math.normalize(vright);

            // org = plane.normal * plane.dist
            float3 org = p.normal * p.dist;

            // build a new winding with 4 points
            winding_t w = new winding_t();
            w.numpoints = 4;
            w.points    = new float3[4];

            float BOGUS_RANGE = 99999; // or match the original
            // point 0
            w.points[0] = org + vup * BOGUS_RANGE + vright * BOGUS_RANGE;
            // point 1
            w.points[1] = org + vup * BOGUS_RANGE - vright * BOGUS_RANGE;
            // point 2
            w.points[2] = org - vup * BOGUS_RANGE - vright * BOGUS_RANGE;
            // point 3
            w.points[3] = org - vup * BOGUS_RANGE + vright * BOGUS_RANGE;

            return w;
        }

        //---------------------------------------------------------------
        // CheckWinding
        //---------------------------------------------------------------
        public static void CheckWinding(winding_t w)
        {
            if (w.numpoints < 3)
                Error($"CheckWinding: {w.numpoints} points");

            // get plane normal for the winding
            float3 facenormal;
            float  dist;
            // The original used WindingPlane(w, facenormal, &d). You'd implement that:
            (facenormal, dist) = WindingPlane(w);

            for (int i = 0; i < w.numpoints; i++)
            {
                int    next = (i + 1) % w.numpoints;
                float3 p1   = w.points[i];
                float3 p2   = w.points[next];

                // check degenerate
                float3 dir = p2 - p1;
                if (math.length(dir) < ON_EPSILON)
                    Error("CheckWinding: degenerate edge");

                // cross(facenormal, dir) => edgenormal
                float3 edgenormal = math.cross(facenormal, dir);
                edgenormal = math.normalize(edgenormal);

                float edgedist = math.dot(p1, edgenormal);
                edgedist += ON_EPSILON;

                // all other points must be on front side
                for (int j = 0; j < w.numpoints; j++)
                {
                    if (j == i || j == next)
                        continue;
                    float d = math.dot(w.points[j], edgenormal);
                    if (d > edgedist)
                        Error("CheckWinding: non-convex");
                }
            }
        }

        // Example of how you might implement WindingPlane:
        private static (float3 normal, float dist) WindingPlane(winding_t w)
        {
            // Take first three points to define a plane
            float3 v1     = w.points[1] - w.points[0];
            float3 v2     = w.points[2] - w.points[0];
            float3 normal = math.normalize(math.cross(v1, v2));
            float  dist   = math.dot(w.points[0], normal);
            return (normal, dist);
        }
    }
}