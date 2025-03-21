
using Unity.Mathematics;

namespace Junk.Sludge.Formats.Map.Objects
{
    public class Surface
    {
        public string TextureName;

        public float3 UAxis  = math.right(); // Vector3.UnitX;
        public float3 VAxis  = math.back();  //-Vector3.UnitZ;
        public float  XScale = 1;
        public float  YScale = 1;
        public float  XShift;
        public float  YShift;
        public float  Rotation;

        public int   ContentFlags;
        public int   SurfaceFlags;
        public float Value;

        public float  LightmapScale;
        public string SmoothingGroups;
    }
}