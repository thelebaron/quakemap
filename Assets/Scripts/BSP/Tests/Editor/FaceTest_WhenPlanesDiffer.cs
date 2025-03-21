using NUnit.Framework;
using Unity.Mathematics;

namespace BSP.Tests.Editor
{
    [TestFixture]
    public class FaceTest_WhenPlanesDiffer : BaseFaceTests
    {
        [Test]
        public void TryMerge_ReturnsFalse_WhenPlanesDiffer()
        {
            var f1 = new face_t
            {
                planenum   = 0,
                planeside  = 0,
                texturenum = 3,
                numpoints  = 3,
                pts = new float3[]
                {
                    new float3(0, 0, 0),
                    new float3(1, 0, 0),
                    new float3(0, 1, 0)
                }
            };

            var f2 = new face_t
            {
                planenum   = 1, // different planenum
                planeside  = 0,
                texturenum = 3,
                numpoints  = 3,
                pts = new float3[]
                {
                    new float3(0, 0, 0),
                    new float3(-1, 0, 0),
                    new float3(0, -1, 0)
                }
            };

            //Bsp.planes    = new plane_t[2];
            Bsp.planes[0] = new plane_t { normal = new float3(0, 0, 1), dist  = 0 };
            Bsp.numbrushplanes++;
            Bsp.planes[1] = new plane_t { normal = new float3(0, 0, -1), dist = 0 };
            Bsp.numbrushplanes++;

            bool result = Bsp.TryMerge(f1, f2);

            Assert.IsFalse(result);
        }
    }
}