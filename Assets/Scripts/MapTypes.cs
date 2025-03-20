using System.Collections.Generic;
using QuakeMapVisualization;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Plane = UnityEngine.Plane;

namespace ScriptsSandbox.Util
{
    public class QPolygon
    {
        public List<QVertex> Vertices = new List<QVertex>();

        public Plane  Plane;
        public float3 Origin;
    }

    public class QVertex
    {
        public float3 Position;
        public float  TextureU;
        public float  TextureV;
    }
}