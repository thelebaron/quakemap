using System.Collections.Generic;
using Junk.Math;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;

namespace Junk.Sludge.Formats.Map.Objects
{
    public class Face : Surface
    {
        /// <summary>
        /// The plane for this face. The plane normal will face outwards in the direction the face is pointing.
        /// </summary>
        public Plane Plane;

        /// <summary>
        /// A list of the vertices in the face, in counter-clockwise order when looking at the visible side of the face.
        /// </summary>
        public List<float3> Vertices;

        /// <summary>
        /// A read-only list of vertices that were read from the face when parsing the map
        /// file. This is for information purposes only so that tooling can use these values
        /// if required. It is not used when the face is written back to a map format. This
        /// array will always either be null or will contain three or more vertices.
        /// </summary>
        public double3[] OriginalPlaneVertices;

        public Face()
        {
            Vertices = new List<float3>();
        }
    }
}