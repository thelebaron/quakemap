using System.Drawing;
using Unity.Mathematics;


namespace Junk.Sludge.Formats.Map.Objects
{
    public class Camera
    {
        public float3 EyePosition  { get; set; }
        public float3 LookPosition { get; set; }
        public bool   IsActive     { get; set; }
        public Color  Color        { get; set; }
    }
}