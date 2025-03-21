using NUnit.Framework;

namespace BSP.Tests.Editor
{
    [TestFixture]
    public class FaceTest_TryMerge_ReturnsTrue_WhenIdenticalTriangles : BaseFaceTests
    {
        [Test]
        public void TryMerge_ReturnsTrue_WhenIdenticalTriangles()
        {
            var f1 = GetTriangleQuake();
            var f2 = GetTriangleQuake();

            SetupQuakePlane();

            bool result = Bsp.TryMerge(f1, f2);

            Assert.IsTrue(result);
            Assert.AreEqual(3, f1.numpoints);
        }
    }
}