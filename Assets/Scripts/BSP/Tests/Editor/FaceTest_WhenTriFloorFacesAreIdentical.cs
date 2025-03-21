using NUnit.Framework;
using Unity.Mathematics;

namespace BSP.Tests.Editor
{
    [TestFixture]
    public class FaceTest_WhenTriFloorFacesAreIdentical : BaseFaceTests
    {
        [Test]
        public void TryMerge_WhenFloorTriangleIdentical()
        {
            var f1 = GetTriangleQuakeFloor();
            var f2 = GetTriangleQuakeFloor();
       
            SetupQuakePlane(0, f1.normal);

            bool result = Bsp.TryMerge(f1, f2);

            Assert.IsTrue(result);
            Assert.AreEqual(3, f1.numpoints);
        }
    }
}