using NUnit.Framework;
using Unity.Mathematics;

namespace BSP.Tests.Editor
{
    [TestFixture]
    public class FaceTest_WhenQuadFacesAreIdentical : BaseFaceTests
    {
        [Test]
        public void TryMerge_ReturnsTrue_WhenFacesAreIdentical()
        {
            // Create a rectangular quad with CORRECT WINDING ORDER
            // Points must be in counter-clockwise or clockwise order around perimeter
            // Create an offset quad that will sort correctly
            var points = new float3[]
            {
                new float3(1, 0, 1), // Center-ish point
                new float3(3, 0, 1), // Right
                new float3(2, 0, 3), // Top
                new float3(0, 0, 2)  // Left
            };
            var f1 = new face_t
            {
                planenum   = 0,
                planeside  = 0,
                texturenum = 1,
                numpoints  = points.Length,
                pts        = (float3[])points.Clone()
            };

            var f2 = new face_t
            {
                planenum   = 0,
                planeside  = 0,
                texturenum = 1,
                numpoints  = points.Length,
                pts        = (float3[])points.Clone()
            };

            // Use a normal that actually matches the plane of the points
            Bsp.planes[0] = new plane_t { normal = new float3(0, 1, 0), dist = 0 };
            Bsp.numbrushplanes++;

            bool result = Bsp.TryMerge(f1, f2);

            Assert.IsTrue(result);
            Assert.AreEqual(4, f1.numpoints);
        }
    }
}