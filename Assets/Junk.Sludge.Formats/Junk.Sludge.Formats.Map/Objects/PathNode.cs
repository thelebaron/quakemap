using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Junk.Sludge.Formats.Map.Objects
{
    public class PathNode
    {
        public float3                     Position;
        public int                        ID;
        public string                     Name;
        public Dictionary<string, string> Properties;
        public Color                      Color;

        public PathNode()
        {
            Properties = new Dictionary<string, string>();
        }
    }
}