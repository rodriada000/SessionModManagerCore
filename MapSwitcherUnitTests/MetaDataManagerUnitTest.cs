using System;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;

namespace MapSwitcherUnitTests
{
    [TestClass]
    public class MetaDataManagerUnitTest
    {
        [TestMethod]
        public void Test_GetFirstValidMapInFolder_ValidMap_ReturnsMapListItem()
        {
            MapListItem actualResult = MetaDataManager.GetFirstValidMapInFolder(Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files"));

            Assert.AreEqual("testmap", actualResult.MapName);
        }
    }
}
