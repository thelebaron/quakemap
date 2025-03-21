using System;
using System.Collections.Generic;
using System.Linq;
using ScriptsSandbox.Util;
using Sledge.Formats.Map.Objects;
using Unity.Mathematics;
using UnityEngine;
using Plane = Unity.Mathematics.Geometry.Plane;

namespace BSP
{
    public static partial class Bsp
    {
        static readonly int CONTENTS_SOLID = 0;
        static readonly int CONTENTS_LAVA  = 1;
        static readonly int CONTENTS_SLIME = 2;
        static readonly int CONTENTS_WATER = 3;
        static readonly int CONTENTS_SKY   = 4;

        public const int PLANE_X    = 0;
        public const int PLANE_Y    = 1;
        public const int PLANE_Z    = 2;
        public const int PLANE_ANYX = 3;
        public const int PLANE_ANYY = 4;
        public const int PLANE_ANYZ = 5;

        public static bool approximately(float a, float b)
        {
            return (double)math.abs(b - a) < (double)math.max(1E-06f * math.max(math.abs(a), math.abs(b)), math.EPSILON_DBL * 8f);
        }

        public static void CheckFace(face_t f)
        {
            if (f.numpoints < 3)
            {
                throw new Exception($"CheckFace: {f.numpoints} points");
            }

            float3 facenormal = new float3(planes[f.planenum].normal.x, planes[f.planenum].normal.y, planes[f.planenum].normal.z);
            if (f.planeside != 0)
            {
                facenormal = -facenormal; // VectorSubtract(vec3_origin, facenormal, facenormal);
            }

            for (int i = 0; i < f.numpoints; i++)
            {
                float3 p1 = new float3(f.pts[i].x, f.pts[i].y, f.pts[i].z);
                for (int j1 = 0; j1 < 3; j1++)
                {
                    if (p1[j1] > BOGUS_RANGE || p1[j1] < -BOGUS_RANGE)
                    {
                        throw new Exception($"CheckFace: BUGUS_RANGE: {p1[j1]}");
                    }
                }

                int j = i + 1 == f.numpoints ? 0 : i + 1;

                // check the point is on the face plane
                float d = math.dot(p1, planes[f.planenum].normal) - planes[f.planenum].dist;
                if (d < -ON_EPSILON || d > ON_EPSILON)
                {
                    throw new Exception("CheckFace: point off plane");
                }

                // check the edge isn't degenerate
                float3 p2  = new float3(f.pts[j].x, f.pts[j].y, f.pts[j].z);
                float3 dir = p2 - p1; // VectorSubtract(p2, p1, dir);

                if (math.length(dir) < ON_EPSILON)
                {
                    throw new Exception("CheckFace: degenerate edge");
                }

                float3 edgenormal = math.cross(facenormal, dir); // CrossProduct(facenormal, dir, edgenormal);
                edgenormal = math.normalize(edgenormal);         // VectorNormalize(edgenormal);
                float edgedist = math.dot(p1, edgenormal);
                edgedist += ON_EPSILON;

                // all other points must be on front side
                for (int k = 0; k < f.numpoints; k++)
                {
                    if (k == i)
                        continue;
                    d = math.dot(f.pts[k], edgenormal);
                    if (d > edgedist)
                        throw new Exception("CheckFace: non-convex");
                }
            }
        }

        public static void ClearBounds(ref brushset_t bs)
        {
            for (int j = 0; j < 2; j++) // NUM_HULLS
            {
                for (int i = 0; i < 3; i++)
                {
                    bs.mins[i] = 99999;
                    bs.maxs[i] = -99999;
                }
            }
        }

        public static void AddToBounds(ref brushset_t bs, float3 v)
        {
            for (int i = 0; i < 3; i++)
            {
                if (v[i] < bs.mins[i])
                    bs.mins[i] = v[i];
                if (v[i] > bs.maxs[i])
                    bs.maxs[i] = v[i];
            }
        }


        public static int PlaneTypeForNormal(float3 normal)
        {
            if (approximately(normal.x, 1.0f))
                return PLANE_X;
            if (approximately(normal.y, 1.0f))
                return PLANE_Y;
            if (approximately(normal.z, 1.0f))
                return PLANE_Z;

            // The original code calls Error(...) if we encounter -1.0 anywhere.
            if (approximately(normal.x, -1.0f) || approximately(normal.y, -1.0f) || approximately(normal.z, -1.0f))
                throw new System.Exception("PlaneTypeForNormal: not a canonical vector");

            float ax = math.abs(normal.x);
            float ay = math.abs(normal.y);
            float az = math.abs(normal.z);

            if (ax >= ay && ax >= az)
                return PLANE_ANYX;
            if (ay >= ax && ay >= az)
                return PLANE_ANYY;
            return PLANE_ANYZ;
        }

        public static void NormalizePlane(ref plane_t dp)
        {
            // If any coordinate is exactly -1.0, flip it to +1.0 and invert dist.
            // This enforces a canonical direction (no -1.0 normal).
            if (approximately(dp.normal.x, -1.0f))
            {
                dp.normal.x = 1.0f;
                dp.dist     = -dp.dist;
            }

            if (Mathf.Approximately(dp.normal.y, -1.0f))
            {
                dp.normal.y = 1.0f;
                dp.dist     = -dp.dist;
            }

            if (Mathf.Approximately(dp.normal.z, -1.0f))
            {
                dp.normal.z = 1.0f;
                dp.dist     = -dp.dist;
            }

            // If the plane is aligned perfectly on an axis, set the type and finish.
            if (Mathf.Approximately(dp.normal.x, 1.0f))
            {
                dp.type = PLANE_X;
                return;
            }

            if (Mathf.Approximately(dp.normal.y, 1.0f))
            {
                dp.type = PLANE_Y;
                return;
            }

            if (Mathf.Approximately(dp.normal.z, 1.0f))
            {
                dp.type = PLANE_Z;
                return;
            }

            // Otherwise, figure out which axis is dominant.
            float ax = math.abs(dp.normal.x);
            float ay = math.abs(dp.normal.y);
            float az = math.abs(dp.normal.z);

            if (ax >= ay && ax >= az)
                dp.type = PLANE_ANYX;
            else if (ay >= ax && ay >= az)
                dp.type = PLANE_ANYY;
            else
                dp.type = PLANE_ANYZ;

            // For PLANE_ANYX/PLANE_ANYY/PLANE_ANYZ, ensure the normal is facing a canonical direction:
            // If its dominant component is negative, flip it to positive and invert dist.
            if ((dp.type == PLANE_ANYX && dp.normal.x < 0.0f) ||
                (dp.type == PLANE_ANYY && dp.normal.y < 0.0f) ||
                (dp.type == PLANE_ANYZ && dp.normal.z < 0.0f))
            {
                dp.normal = -dp.normal;
                dp.dist   = -dp.dist;
            }
        }

        // Finds or creates a plane that matches dplane (within ANGLEEPSILON, DISTEPSILON)
        // side is set to 0 if plane is oriented the same as dplane->normal,
        // or 1 if it is reversed.
        public static int FindPlane(plane_t dplane, out int side)
        {
            // Ensure dplane’s normal is normalized
            float length = math.length(dplane.normal);
            if (length < 1.0f - ANGLEEPSILON || length > 1.0f + ANGLEEPSILON)
                throw new System.Exception("FindPlane: normalization error");

            // Copy dplane and normalize it to a canonical orientation
            plane_t pl = dplane;
            NormalizePlane(ref pl);

            // side is 0 if pl.normal dot dplane.normal is > 0, else 1
            float dotCheck = math.dot(pl.normal, dplane.normal);
            side = (dotCheck > 0f) ? 0 : 1;

            // Look for a match among existing planes
            for (int i = 0; i < numbrushplanes; i++)
            {
                plane_t existing = planes[i];
                float   dot      = math.dot(existing.normal, pl.normal);
                // If their normals are basically the same...
                if (dot > 1.0f - ANGLEEPSILON)
                {
                    // ...then check if the dists match
                    if (math.abs(existing.dist - pl.dist) < DISTEPSILON)
                    {
                        // Found the plane that matches
                        return i;
                    }
                }
            }

            // If no match, we must add this as a new plane
            if (numbrushplanes == MAX_MAP_PLANES)
                throw new System.Exception("numbrushplanes == MAX_MAP_PLANES");

            planes[numbrushplanes] = pl;
            numbrushplanes++;
            return numbrushplanes - 1;
        }

        //---------------------------------------------------------------
        // CreateBrushFaces
        //---------------------------------------------------------------
        public static void CreateBrushFaces()
        {
            // Reset bounding box
            brush_mins = new float3(99999, 99999, 99999);
            brush_maxs = new float3(-99999, -99999, -99999);

            brush_faces = null;

            for (int i = 0; i < numbrushfaces; i++)
            {
                mface_t mf = faces[i];

                // Make a large base winding from the plane
                winding_t w = BaseWindingForPlane(mf.plane);

                // Clip that winding by every other brush face, except itself
                for (int j = 0; j < numbrushfaces && w != null; j++)
                {
                    if (j == i)
                        continue;

                    // Flip that plane, because we want the "back" side
                    plane_t flip = new plane_t
                    {
                        normal = -faces[j].plane.normal,
                        dist   = -faces[j].plane.dist
                    };

                    w = ClipWinding(w, flip, false);
                }

                // If winding was completely clipped away, skip
                if (w == null)
                    continue;

                // Keep this face
                face_t f = AllocFace();
                f.numpoints = w.numpoints;
                if (f.numpoints > MAXEDGES)
                    Error("f->numpoints > MAXEDGES");

                f.pts = new float3[f.numpoints];
                for (int j = 0; j < w.numpoints; j++)
                {
                    float3 pt = w.points[j];
                    // Round very close floats to integer if within ZERO_EPSILON
                    float3 rounded;
                    rounded.x = (math.abs(pt.x - math.round(pt.x)) < ZERO_EPSILON)
                        ? math.round(pt.x)
                        : pt.x;
                    rounded.y = (math.abs(pt.y - math.round(pt.y)) < ZERO_EPSILON)
                        ? math.round(pt.y)
                        : pt.y;
                    rounded.z = (math.abs(pt.z - math.round(pt.z)) < ZERO_EPSILON)
                        ? math.round(pt.z)
                        : pt.z;

                    f.pts[j] = rounded;

                    // Update brush min/max
                    brush_mins = math.min(brush_mins, rounded);
                    brush_maxs = math.max(brush_maxs, rounded);
                }

                FreeWinding(w);

                f.texturenum = mf.texinfo;
                int side;
                f.planenum  = FindPlane(mf.plane, out side);
                f.planeside = side;

                // Insert into linked list
                f.next      = brush_faces;
                brush_faces = f;

                // Validate
                CheckFace(f);
            }
        }

        //---------------------------------------------------------------
        // AddBrushPlane
        //---------------------------------------------------------------
        public static void AddBrushPlane(plane_t plane)
        {
            // This uses 'faces' array and 'numbrushfaces', assumed global
            if (numbrushfaces == MAX_FACES)
                Error("AddBrushPlane: numbrushfaces == MAX_FACES");

            float length = math.length(plane.normal);
            if (length < 0.999f || length > 1.001f)
                Error("AddBrushPlane: bad normal");

            // Check for duplicates
            for (int i = 0; i < numbrushfaces; i++)
            {
                plane_t existing = faces[i].plane;
                if (VectorCompare(existing.normal, plane.normal) &&
                    math.abs(existing.dist - plane.dist) < ON_EPSILON)
                {
                    // Already present
                    return;
                }
            }

            // Insert new plane
            faces[numbrushfaces].plane = plane;
            // This code sets the new face's texinfo from the first face's texinfo:
            faces[numbrushfaces].texinfo = faces[0].texinfo;
            numbrushfaces++;
        }

        // Helper function to compare float3 for equality within epsilon
        public static bool VectorCompare(float3 a, float3 b)
        {
            const float EPS = 0.000001f;
            return (math.abs(a.x - b.x) < EPS &&
                math.abs(a.y     - b.y) < EPS &&
                math.abs(a.z     - b.z) < EPS);
        }

        //---------------------------------------------------------------
        // TestAddPlane
        //---------------------------------------------------------------
        public static void TestAddPlane(plane_t plane)
        {
            // Check if already in the brush's faces
            for (int i = 0; i < numbrushfaces; i++)
            {
                plane_t pl = faces[i].plane;

                // Same orientation
                if (VectorCompare(plane.normal, pl.normal) &&
                    math.abs(plane.dist - pl.dist) < ON_EPSILON)
                {
                    return;
                }

                // Opposite orientation
                float3 inv = -plane.normal;
                if (VectorCompare(inv, pl.normal) &&
                    math.abs(plane.dist + pl.dist) < ON_EPSILON)
                {
                    return;
                }
            }

            // Check all corner points in hull_corners
            int[] counts = new int[3]; // 0 = front, 1 = back, 2 = on
            int   ctotal = num_hull_points * 8;

            // We'll walk hull_corners in sets of float3
            for (int i = 0; i < ctotal; i++)
            {
                float3 corner = hull_corners[i];
                float  d      = math.dot(corner, plane.normal) - plane.dist;
                if (d < -ON_EPSILON)
                {
                    if (counts[0] > 0) // We have both front + back
                        return;
                    counts[1]++;
                }
                else if (d > ON_EPSILON)
                {
                    if (counts[1] > 0)
                        return;
                    counts[0]++;
                }
                else
                {
                    counts[2]++;
                }
            }

            // If there are front points, we want the plane normal facing them; 
            // if we have back points, flip it
            if (counts[0] != 0)
            {
                // keep as-is
            }
            else
            {
                // flip plane
                plane_t flip = new plane_t
                {
                    normal = -plane.normal,
                    dist   = -plane.dist
                };
                plane = flip;
            }

            AddBrushPlane(plane);
        }

        //---------------------------------------------------------------
        // AddHullPoint
        //---------------------------------------------------------------
        public static int AddHullPoint(float3 p, int hullnum)
        {
            // Check for duplicates
            for (int i = 0; i < num_hull_points; i++)
            {
                if (VectorCompare(p, hull_points[i]))
                    return i;
            }

            // Put it in hull_points
            hull_points[num_hull_points] = p;

            // Fill out the hull_corners array for that point
            // Each hullpoint has 8 corners (2x2x2):
            int cornerBase = num_hull_points * 8;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                float3 corner = new float3(
                    p.x + hull_size[hullnum, x].x,
                    p.y + hull_size[hullnum, y].y,
                    p.z + hull_size[hullnum, z].z
                );
                hull_corners[cornerBase++] = corner;
            }

            num_hull_points++;
            if (num_hull_points >= MAX_HULL_POINTS)
                Error("MAX_HULL_POINTS exceeded");

            return num_hull_points - 1;
        }

        //---------------------------------------------------------------
        // AddHullEdge
        //---------------------------------------------------------------
        public static void AddHullEdge(float3 p1, float3 p2, int hullnum)
        {
            int pt1 = AddHullPoint(p1, hullnum);
            int pt2 = AddHullPoint(p2, hullnum);

            // Check if we already have that edge
            for (int i = 0; i < num_hull_edges; i++)
            {
                bool sameForward  = (hull_edges[i, 0] == pt1 && hull_edges[i, 1] == pt2);
                bool sameBackward = (hull_edges[i, 0] == pt2 && hull_edges[i, 1] == pt1);
                if (sameForward || sameBackward)
                    return; // already added
            }

            if (num_hull_edges >= MAX_HULL_EDGES)
                Error("MAX_HULL_EDGES exceeded");

            hull_edges[num_hull_edges, 0] = pt1;
            hull_edges[num_hull_edges, 1] = pt2;
            num_hull_edges++;

            // Build an edge vector
            float3 edgevec = math.normalize(p1 - p2);

            // For each axis a, we do a +/- expansions (the loops a,b,c,d,e in the original code)
            for (int a = 0; a < 3; a++)
            {
                int b = (a + 1) % 3;
                int c = (a + 2) % 3;
                for (int d = 0; d <= 1; d++)
                for (int e = 0; e <= 1; e++)
                {
                    float3 planeorg = p1;
                    // planeorg[b] += hull_size[hullnum][d][b]; // in original code
                    // planeorg[c] += hull_size[hullnum][e][c];
                    // We'll do an inline version:
                    planeorg[b] += hull_size[hullnum, d][b];
                    planeorg[c] += hull_size[hullnum, e][c];

                    float3 planevec = new float3(0, 0, 0);
                    planevec[a] = 1f;

                    // cross(planevec, edgevec) -> plane.normal
                    float3 normal = math.normalize(math.cross(planevec, edgevec));
                    float  l      = math.length(normal);
                    if (l < 1f - ANGLEEPSILON || l > 1f + ANGLEEPSILON)
                        continue;

                    plane_t plane = new plane_t();
                    plane.normal = normal;
                    plane.dist   = math.dot(planeorg, normal);

                    TestAddPlane(plane);
                }
            }
        }


        public const float ON_EPSILON     = 0.05f;
        public const float BOGUS_RANGE    = 18000f;
        public const int   MAX_MAP_PLANES = 1024; // Or whatever the max is 8192?

        // Adjust these as necessary in your larger codebase:
        public const float ANGLEEPSILON = 0.00001f;
        public const float DISTEPSILON  = 0.01f;


        public static void Init()
        {
            planes         = new plane_t[MAX_MAP_PLANES];
            numbrushplanes = 0;
        }
        // Global plane storage:
        public static plane_t[] planes         = new plane_t[MAX_MAP_PLANES];
        public static int       numbrushplanes = 0;


        public const  int       MAX_FACES    = 128; // or whatever your actual limit is
        public const  float     ZERO_EPSILON = 0.001f;
        public static int       numbrushfaces;
        public static mface_t[] faces = new mface_t[MAX_FACES];
        public static float3    brush_mins, brush_maxs;
        public static face_t    brush_faces     = null; // linked list of face_t
        public static int       MAXEDGES        = 32;
        public static int       MAX_HULL_POINTS = 32;
        public static int       MAX_HULL_EDGES  = 64;
        public static int       num_hull_points;
        public static int       num_hull_edges;
        public static float3[]  hull_points = new float3[MAX_HULL_POINTS];

        public static float3[,] hull_size = new float3[3, 2]
        {
            { new float3(0, 0, 0), new float3(0, 0, 0) },
            { new float3(-16, -16, -32), new float3(16, 16, 24) },
            { new float3(-32, -32, -64), new float3(32, 32, 24) }
        };

        public static float3[] hull_corners = new float3[MAX_HULL_POINTS * 8];
        public static int[,]   hull_edges   = new int[MAX_HULL_EDGES, 2];

        public static face_t AllocFace()
        {
            // Allocate however you like; typical approach is `new face_t()`
            return new face_t();
        }

        public static void  Error(string message) => throw new System.Exception(message);
        public static float Q_rint(float x)       => math.round(x); // or do your own version


        //---------------------------------------------------------------
        // ExpandBrush
        //---------------------------------------------------------------
        public static void ExpandBrush(int hullnum)
        {
            // Clear hull points/edges
            num_hull_points = 0;
            num_hull_edges  = 0;

            // Create hull points from all the points of brush_faces
            for (face_t f = brush_faces; f != null; f = f.next)
            {
                for (int i = 0; i < f.numpoints; i++)
                {
                    AddHullPoint(f.pts[i], hullnum);
                }
            }

            // Adjust each plane by offset
            for (int i = 0; i < numbrushfaces; i++)
            {
                plane_t p      = faces[i].plane;
                float3  corner = float3.zero;

                // If p->normal[x] > 0, corner[x] = hull_size[hullnum][1][x], else [0][x]
                for (int x = 0; x < 3; x++)
                {
                    float n = p.normal[x];
                    if (n > 0f)
                        corner[x] = hull_size[hullnum, 1][x];
                    else if (n < 0f)
                        corner[x] = hull_size[hullnum, 0][x];
                }

                // add DotProduct(corner, p->normal) to p->dist
                float offset = math.dot(corner, p.normal);
                p.dist += offset;

                // store back
                faces[i].plane = p;
            }

            // Add any axis planes not already in the brush to bevel corners
            for (int x = 0; x < 3; x++)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    plane_t plane = new plane_t { normal = float3.zero };
                    float3  n     = plane.normal;
                    n[x]         = s;
                    plane.normal = n;

                    if (s == -1)
                    {
                        plane.dist = -brush_mins[x] + -hull_size[hullnum, 0][x];
                    }
                    else
                    {
                        plane.dist = brush_maxs[x] + hull_size[hullnum, 1][x];
                    }

                    AddBrushPlane(plane);
                }
            }

            // Add all edge bevels
            for (face_t f = brush_faces; f != null; f = f.next)
            {
                for (int i = 0; i < f.numpoints; i++)
                {
                    float3 p1 = f.pts[i];
                    float3 p2 = f.pts[(i + 1) % f.numpoints];
                    AddHullEdge(p1, p2, hullnum);
                }
            }
        }

        //---------------------------------------------------------------
        // LoadBrush
        //---------------------------------------------------------------
        public static brush_t LoadBrush(mbrush_t mb, int hullnum)
        {
            // The original code uses various global data like miptex, texinfo, etc.
            // For demonstration, we'll assume you can detect contents from the texture name.
            // The code snippet is shortened to illustrate structure.

            int contents = CONTENTS_SOLID; // default
            // Suppose 'mb->faces->texinfo' points to a texture index, name could define water/sky

            // ...
            // The code checks if (some name is "clip") or star-lava/slime => set contents
            // If (hullnum != 0 and contents != SOLID/SKY) => return null

            // Prepare brush_faces from the faces in mbrush_t
            brush_faces   = null;
            numbrushfaces = 0;
            for (mface_t f = mb.faces; f != null; f = f.next)
            {
                faces[numbrushfaces] = f; // copy struct
                if (hullnum != 0)
                {
                    // zero out texinfo if needed
                    faces[numbrushfaces].texinfo = 0;
                }

                numbrushfaces++;
            }

            // Create polygon faces from those planes
            CreateBrushFaces();
            if (brush_faces == null)
            {
                // "WARNING: couldn't create brush faces"
                return null;
            }

            if (hullnum != 0)
            {
                ExpandBrush(hullnum);
                CreateBrushFaces();
            }

            // Build the brush object
            brush_t b = new brush_t();
            b.contents = contents;
            b.faces    = brush_faces;
            b.mins     = brush_mins;
            b.maxs     = brush_maxs;

            return b;
        }

        //---------------------------------------------------------------
        // Brush_DrawAll
        //---------------------------------------------------------------
        public static void Brush_DrawAll(brushset_t bs)
        {
            for (brush_t b = bs.brushes; b != null; b = b.next)
            {
                for (face_t f = b.faces; f != null; f = f.next)
                {
                    // calls a draw function for face
                    Draw_DrawFace(f);
                }
            }
        }

        //---------------------------------------------------------------
        // Brush_LoadEntity
        //---------------------------------------------------------------
        public static brushset_t Brush_LoadEntity(entity_t ent, int hullnum)
        {
            brushset_t bset = new brushset_t();
            // We assume ClearBounds, AddToBounds are implemented similarly
            ClearBounds(bset);

            brush_t water           = null;
            brush_t other           = null;
            int     numbrushesLocal = 0;

            // For each map brush in the entity:
            for (mbrush_t mbr = ent.brushes; mbr != null; mbr = mbr.next)
            {
                brush_t b = LoadBrush(mbr, hullnum);
                if (b == null)
                    continue;

                numbrushesLocal++;

                // if b->contents is not solid, put it in water list
                if (b.contents != CONTENTS_SOLID)
                {
                    b.next = water;
                    water  = b;
                }
                else
                {
                    b.next = other;
                    other  = b;
                }

                AddToBounds(bset, b.mins);
                AddToBounds(bset, b.maxs);
            }

            // Insert water brushes at the start
            brush_t current = water;
            while (current != null)
            {
                brush_t nxt = current.next;
                current.next = other;
                other        = current;
                current      = nxt;
            }

            bset.brushes = other;

            // Possibly store it in a global brushset = bset
            // Then draw them:
            Brush_DrawAll(bset);

            // console print: ("%i brushes read\n", numbrushesLocal)
            return bset;
        }


        // Example placeholder for clearing bounds:
        public static void ClearBounds(brushset_t bs)
        {
            bs.mins = new float3(99999, 99999, 99999);
            bs.maxs = new float3(-99999, -99999, -99999);
        }

        // Example placeholder for adding a bounding box:
        public static void AddToBounds(brushset_t bs, float3 point)
        {
            bs.mins = math.min(bs.mins, point);
            bs.maxs = math.max(bs.maxs, point);
        }

        // Example placeholder for drawing a face:
        public static void Draw_DrawFace(face_t f)
        {
            // your rendering code here
        }


        

        public static Plane ToUnityPlane(this System.Numerics.Plane plane)
        {
            var p = new Plane();
            p.Normal   = new float3(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
            p.Distance = plane.D;
            return p;
        }
    }
}