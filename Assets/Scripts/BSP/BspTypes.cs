using Unity.Mathematics;

namespace BSP
{
    // plane_t was a small record-type (no self-references), so a struct works fine:
    public struct plane_t
    {
        public float3 normal;
        public float  dist;
        public int    type;
    }

    // winding_t was similarly just an array of points:
    public class winding_t
    {
        public int numpoints;

        // The original code used a fixed array size [8].
        // In C#, you can store them in an array of length 8:
        public float3[] points;
    }

    // face_t uses linked-list-like pointers, so use a class to allow references:
    public class face_t
    {
        public face_t next; // in C, this was "struct visfacet_s *next;"

        public int planenum;
        public int planeside;

        public int texturenum;

        // contents[0] and contents[1] in the original
        public int[] contents = new int[2];

        public face_t original; // "struct visfacet_s *original;"
        public int    outputnumber;
        public int    numpoints;

        // In the original code: vec3_t pts[MAXEDGES] and edges[MAXEDGES].
        // We'll keep them as arrays in C#:
        public float3[] pts;   // array for face vertices
        public int[]    edges; // array for edge indices
        public float3   normal; // new for testing
    }

    // surface_t also uses references to linked structures
    public class surface_t
    {
        public surface_t next;
        public surface_t original;

        public int    planenum;
        public int    outputplanenum;
        public float3 mins;
        public float3 maxs;

        // The original type was "qboolean onnode;",
        // which is typically just an int or bool. We'll use a bool here:
        public bool onnode;

        public face_t faces; // pointer to a linked list of face_t
    }

    // node_t references children (pointers), so use a class:
    public class node_t
    {
        public float3 mins;
        public float3 maxs;

        public int planenum;
        public int outputplanenum;
        public int firstface;
        public int numfaces;

        // Each node has two children:
        public node_t[] children = new node_t[2];

        // For decision nodes, faces points to the dividing faces:
        public face_t faces;

        // For leaf nodes:
        public int contents;

        public face_t[] markfaces; // pointer array in C

        // "struct portal_s *portals;" would be another structure for portals
        // if you want that data structure, define it similarly and reference it here.
        public int visleafnum;
        public int valid;
        public int occupied;
    }

    // brush_t also keeps a linked-list pattern in C:
    public class brush_t
    {
        public brush_t next;
        public float3  mins;
        public float3  maxs;
        public face_t  faces;
        public int     contents;
    }

    // brushset_t is just a container struct in the original, but we can do a class
    // to match the other references:
    public class brushset_t
    {
        public float3  mins;
        public float3  maxs;
        public brush_t brushes; // linked list
    }

    // mface_t was a simple face struct for the "map brushes":
    public class mface_t
    {
        public mface_t next;
        public plane_t plane;
        public int     texinfo;
    }

    // mbrush_t likewise:
    public class mbrush_t
    {
        public mbrush_t next;
        public mface_t  faces;
    }

    // epair_t was a singly linked list of key/value pairs:
    public class epair_t
    {
        public epair_t next;
        public string  key;
        public string  value;
    }

    // entity_t referenced brushes and epairs:
    public class entity_t
    {
        public float3   origin;
        public mbrush_t brushes;
        public epair_t  epairs;
    }
}