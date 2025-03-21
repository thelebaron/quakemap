using System;
using System.Collections.Generic;

using System.Text;
using Unity.Mathematics;

namespace Junk.Sludge.Formats.Map.Objects
{
    [Serializable]
    public class BackgroundImage
    {
        public ViewportType Viewport;
        public string       Path;
        public double       Scale;
        public byte         Luminance;
        public FilterMode   Filter;
        public bool         InvertColours;
        public float2       Offset;
    }
}
