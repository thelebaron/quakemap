using Unity.Mathematics;

namespace BSP
{
    public static partial class Bsp
    {
        //
        // C# translations of two routines from csg4.c, using face_t/plane_t structures.
        //
        // The original code uses global or file-static variables like 'inside' and 'outside',
        // which are linked lists of face_t. We do the same here, storing them in static
        // variables. Adjust this to your code’s architecture.
        //
        // Dependencies:
        //  - face_t struct with pts[] array and next pointer
        //  - plane_t normal/dist
        //  - AllocFace, FreeFace, DotProduct, etc.
        //  - Possibly uses an int planenum with global planes[] if that’s how your engine is structured.
        //  - references to “outside” and “inside” lists of faces that get clipped.
        //  - usage of “precedence” logic for special plane-handling
        //  - side arrays for point classification
        //

        // For classification constants:
        public const int SIDE_FRONT = 0;
        public const int SIDE_BACK  = 1;
        public const int SIDE_ON    = 2;

        // Global references that csg4 uses:
        public static face_t inside;  // linked list of face_t that are "inside"
        public static face_t outside; // linked list of face_t that are "outside"

        // Helper for finalizing the face's array
        private static float3[] TrimFacePoints(float3[] pts, int count)
        {
            if (count < 3)
                return null; // degenerate
            float3[] trimmed = new float3[count];
            for (int i = 0; i < count; i++)
                trimmed[i] = pts[i];
            return trimmed;
        }

        public static void FreeFace(face_t f)
        {
            // In original C code, it frees memory. In C#, you can remove references or
            // do nothing since GC is automatic. We'll just zero out pointers:
            f.pts       = null;
            f.numpoints = -1; // mark invalid
            f.next      = null;
        }

        //
        // SplitFace
        //
        // Splits a face by a plane, returning two face fragments:
        //   front => the portion in front (or on-plane) of 'split'
        //   back  => the portion behind 'split'
        // If the face is wholly in front, 'front' = the face, 'back' = null, etc.
        // If the face is wholly behind, 'front' = null, 'back' = face.
        //
        // The original code calls FreeFace(in) after splitting, and creates new face_t
        // for each fragment. We'll mimic that. You can also store the results differently
        // in your code.
        //
        public static void SplitFace(face_t inFace, plane_t split, out face_t front, out face_t back)
        {
            float[] dists  = new float[inFace.numpoints + 1];
            int[]   sides  = new int[inFace.numpoints   + 1];
            int[]   counts = new int[3] { 0, 0, 0 }; // front/back/on counters

            // Determine sides for each vertex
            for (int i = 0; i < inFace.numpoints; i++)
            {
                float3 p   = inFace.pts[i];
                float  dot = math.dot(p, split.normal) - split.dist;

                if (dot > ON_EPSILON)
                {
                    sides[i] = SIDE_FRONT;
                }
                else if (dot < -ON_EPSILON)
                {
                    sides[i] = SIDE_BACK;
                }
                else
                {
                    sides[i] = SIDE_ON;
                }

                dists[i] = dot;
                counts[sides[i]]++;
            }

            // Wrap around
            dists[inFace.numpoints] = dists[0];
            sides[inFace.numpoints] = sides[0];

            // If entire face is on one side or the other, easy:
            if (counts[SIDE_FRONT] == 0)
            {
                // wholly behind
                front = null;
                back  = inFace;
                return;
            }

            if (counts[SIDE_BACK] == 0)
            {
                // wholly in front
                front = inFace;
                back  = null;
                return;
            }

            // We must split
            // We'll create two new faces from 'inFace' then free the original
            face_t newBack  = NewFaceFromFace(inFace);
            face_t newFront = NewFaceFromFace(inFace);

            newBack.numpoints  = 0;
            newFront.numpoints = 0;
            newBack.pts        = new float3[inFace.numpoints + 4];
            newFront.pts       = new float3[inFace.numpoints + 4];

            // Distribute the points and generate splits
            for (int i = 0; i < inFace.numpoints; i++)
            {
                float3 p1    = inFace.pts[i];
                int    side1 = sides[i];

                // If the point is not behind, copy to the front face
                if (side1 != SIDE_BACK)
                {
                    newFront.pts[newFront.numpoints++] = p1;
                }

                // If the point is not in front, copy to the back face
                if (side1 != SIDE_FRONT)
                {
                    newBack.pts[newBack.numpoints++] = p1;
                }

                // If edges cross the plane, generate an intersection
                int next = (i + 1) % inFace.numpoints;
                if (sides[i] == SIDE_ON || sides[next] == SIDE_ON || sides[i] == sides[next])
                    continue;

                // p2 is next point
                float3 p2  = inFace.pts[next];
                float  dot = dists[i] / (dists[i] - dists[next]);
                float3 mid = p1 + dot * (p2 - p1);

                newFront.pts[newFront.numpoints++] = mid;
                newBack.pts[newBack.numpoints++]   = mid;
            }

            // If no points, we might have degenerate faces
            // Build final arrays for them
            newFront.pts = TrimFacePoints(newFront.pts, newFront.numpoints);
            newBack.pts  = TrimFacePoints(newBack.pts, newBack.numpoints);

            // free the original face
            FreeFace(inFace);

            front = newFront.numpoints >= 3 ? newFront : null;
            back  = newBack.numpoints  >= 3 ? newBack : null;
        }


        // In the original code, the snippet calls "newf = NewFaceFromFace(inFace)" etc.
        // We'll define that quickly:
        public static face_t NewFaceFromFace(face_t source)
        {
            // duplicates the non-point fields
            face_t f = AllocFace(); // create fresh face object
            f.planenum    = source.planenum;
            f.planeside   = source.planeside;
            f.texturenum  = source.texturenum;
            f.contents[0] = source.contents[0];
            f.contents[1] = source.contents[1];
            f.original    = source.original;
            // We'll set numpoints and pts in the caller
            return f;
        }


        /*
        =================
        ClipInside

        Clips all of the faces in the inside list, possibly moving them to the
        outside list or splitting it into a piece in each list.

        Faces exactly on the plane will stay inside unless overdrawn by later brush

        frontside is the side of the plane that holds the outside list
        =================
        */
        public static void ClipInside(int splitplane, int frontside, bool precedence)
        {
            face_t   f, next;
            face_t[] frags = new face_t[2];
            face_t   insidelist;
            plane_t  split;

            split = planes[splitplane];

            insidelist = null;
            for (f = inside; f != null; f = next)
            {
                next = f.next;

                if (f.planenum == splitplane)
                {
                    // exactly on, handle special
                    if (frontside != f.planeside || precedence)
                    {
                        // always clip off opposite facing
                        frags[frontside]     = null;
                        frags[frontside ^ 1] = f;
                    }
                    else
                    {
                        // leave it on the outside
                        frags[frontside]     = f;
                        frags[frontside ^ 1] = null;
                    }
                }
                else
                {
                    // proper split
                    SplitFace(f, split, out frags[0], out frags[1]);
                }

                if (frags[frontside] != null)
                {
                    frags[frontside].next = outside;
                    outside               = frags[frontside];
                }

                if (frags[frontside ^ 1] != null)
                {
                    frags[frontside ^ 1].next = insidelist;
                    insidelist                = frags[frontside ^ 1];
                }
            }

            inside = insidelist;
        }
        
        
        
    }
}