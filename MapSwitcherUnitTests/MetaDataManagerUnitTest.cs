using System;
using System.Collections.Generic;
using System.IO;
using MapSwitcherUnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;

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

        [TestMethod]
        public void Test_CreateMapMetaData_Returns_Correct_FilePaths()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            string pathToMapImporting = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "cool_valid_map");

            MapMetaData expectedResult = new MapMetaData()
            {
                FilePaths = new List<string>() { Path.Combine(SessionPath.ToContent, "coolmap.uexp"), Path.Combine(SessionPath.ToContent, "coolmap.umap"),
                                                 Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.uexp"), Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.uasset"),
                                                 Path.Combine(SessionPath.ToContent, "coolmap_BuiltData.ubulk")}
            };

            MapMetaData actualResult = MetaDataManager.CreateMapMetaData(pathToMapImporting);

            actualResult.FilePaths.TrueForAll(s => expectedResult.FilePaths.Contains(s));
            expectedResult.FilePaths.TrueForAll(s => actualResult.FilePaths.Contains(s));
        }

        [TestMethod]
        public void Test_CreateMapMetaData_In_SubFolder_Returns_Correct_FilePaths()
        {
            SessionPath.ToSession = TestPaths.ToSessionTestFolder;
            string pathToMapImporting = Path.Combine(TestPaths.ToTestFilesFolder, "Mock_Map_Files", "some_folder");

            MapMetaData expectedResult = new MapMetaData()
            {
                FilePaths = new List<string>() { Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap.uexp"), 
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap.umap"),
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.uexp"), 
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.uasset"),
                                                 Path.Combine(SessionPath.ToContent, "cool_valid_map", "coolmap_BuiltData.ubulk")}
            };

            MapMetaData actualResult = MetaDataManager.CreateMapMetaData(pathToMapImporting);

            actualResult.FilePaths.TrueForAll(s => expectedResult.FilePaths.Contains(s));
            expectedResult.FilePaths.TrueForAll(s => actualResult.FilePaths.Contains(s));
        }
    }
}
