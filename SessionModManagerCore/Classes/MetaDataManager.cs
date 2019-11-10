using Newtonsoft.Json;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SessionMapSwitcherCore.Classes
{
    public class MetaDataManager
    {
        public const string MetaFolderName = "MapSwitcherMetaData";

        public static string FullPathToMetaFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToContent, MetaFolderName);
            }
        }

        /// <summary>
        /// Creates a .meta file in the folder 'MapSwitcherMetaData' to store the original import source folder location.
        /// </summary>
        internal static BoolWithMessage TrackMapLocation(string mapName, string sourceFolderToCopy)
        {
            string umapExt = ".umap";

            try
            {
                if (mapName.EndsWith(umapExt))
                {
                    mapName = mapName.Substring(0, mapName.Length - umapExt.Length);
                }

                CreateMetaDataFolder();

                string trackingFileName = Path.Combine(FullPathToMetaFolder, $".meta_{mapName}");
                File.WriteAllText(trackingFileName, sourceFolderToCopy);
                return new BoolWithMessage(true);
            }
            catch (Exception e)
            {
                return new BoolWithMessage(false, e.Message);
            }
        }

        internal static string GetOriginalImportLocation(string mapName)
        {
            try
            {
                string trackingFileName = Path.Combine(FullPathToMetaFolder, $".meta_{mapName}");

                return File.ReadAllText(trackingFileName);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static bool IsImportLocationStored(string mapName)
        {
            return GetOriginalImportLocation(mapName) != "";
        }

        /// <summary>
        /// Creates a file 'customNames.meta' if it does not exist and writes
        /// the custom names of maps to the file.
        /// </summary>
        /// <returns> true if file updated; false if exception thrown </returns>
        /// <remarks>
        /// The map directory and map name is used as the Key to the custom name and is written to the file like so:
        /// MapDirectory | MapName | CustomName | IsHidden
        /// </remarks>
        public static bool WriteCustomMapPropertiesToFile(IEnumerable<MapListItem> maps)
        {
            try
            {
                CreateMetaDataFolder();

                List<string> linesToWrite = new List<string>();

                foreach (MapListItem map in maps)
                {
                    // only write maps to meta data file that users have set custom properties for
                    if (String.IsNullOrWhiteSpace(map.CustomName) == false || map.IsHiddenByUser)
                    {
                        linesToWrite.Add(map.MetaData);
                    }
                }

                string pathToMetaFile = Path.Combine(FullPathToMetaFolder, "customNames.meta");

                if (File.Exists(pathToMetaFile))
                {
                    File.Delete(pathToMetaFile);
                }

                File.WriteAllLines(pathToMetaFile, linesToWrite.ToArray());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the custom map names from 'customNames.meta' file and updates
        /// list of maps with their custom names.
        /// </summary>
        /// <param name="maps"></param>
        /// <remarks>
        /// customNames.meta uses the map directory and the map name as the Key to find the correct custom map name
        /// </remarks>
        internal static void SetCustomPropertiesForMaps(IEnumerable<MapListItem> maps)
        {
            try
            {
                string pathToMetaFile = Path.Combine(FullPathToMetaFolder, "customNames.meta");

                if (File.Exists(pathToMetaFile) == false)
                {
                    return;
                }

                foreach (string line in File.ReadAllLines(pathToMetaFile))
                {
                    string[] parts = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    string dirPath = parts[0].Trim();
                    string mapName = parts[1].Trim();
                    string customName = parts[2].Trim();
                    string isHidden = parts[3].Trim();


                    MapListItem foundMap = maps.Where(m => m.DirectoryPath == dirPath && m.MapName == mapName).FirstOrDefault();

                    if (foundMap != null)
                    {
                        foundMap.CustomName = customName;
                        foundMap.IsHiddenByUser = (isHidden.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// Creates folder to Meta data folder if it does not exists
        /// </summary>
        internal static void CreateMetaDataFolder()
        {
            if (Directory.Exists(FullPathToMetaFolder) == false)
            {
                Directory.CreateDirectory(FullPathToMetaFolder);
            }
        }

        public static MapMetaData CreateMapMetaData(string sourceMapFolder)
        {
            MapMetaData metaData = new MapMetaData();

            MapListItem validMap = GetFirstValidMapInFolder(sourceMapFolder);

            metaData.MapName = validMap.MapName;
            metaData.MapFileDirectory = ReplaceSourceMapPathWithPathToContent(sourceMapFolder, validMap.DirectoryPath);

            metaData.FilePaths = FileUtils.GetAllFilesInDirectory(sourceMapFolder);

            // modify file paths to match the target folder Session "Content" folder
            for (int i = 0; i < metaData.FilePaths.Count; i++)
            {
                metaData.FilePaths[i] = ReplaceSourceMapPathWithPathToContent(sourceMapFolder, metaData.FilePaths[i]);
            }

            metaData.IsHiddenByUser = false;

            return metaData;
        }

        /// <summary>
        /// Returns a new absolute file path replacing <paramref name="sourceMapFolder"/> with <see cref="SessionPath.ToContent"/>
        /// </summary>
        /// <param name="sourceMapFolder"> path to replace with <see cref="SessionPath.ToContent"/>. </param>
        /// <param name="absoluteFilePath"> path to file that is in the <paramref name="sourceMapFolder"/>. </param>
        private static string ReplaceSourceMapPathWithPathToContent(string sourceMapFolder, string absoluteFilePath)
        {
            if (absoluteFilePath.IndexOf(sourceMapFolder) < 0)
            {
                return "";
            }

            int startIndex = sourceMapFolder.Length + absoluteFilePath.IndexOf(sourceMapFolder);
            string relativePath = absoluteFilePath.Substring(startIndex + 1);

            return Path.Combine(SessionPath.ToContent, relativePath);
        }

        public static MapListItem GetFirstValidMapInFolder(string sourceMapFolder)
        {
            foreach (string file in Directory.GetFiles(sourceMapFolder))
            {
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.Extension == ".umap")
                {
                    MapListItem map = new MapListItem()
                    {
                        FullPath = file,
                        MapName = fileInfo.NameWithoutExtension()
                    };
                    map.Validate();

                    if (map.IsValid)
                    {
                        return map;
                    }
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceMapFolder))
            {
                MapListItem validMap = GetFirstValidMapInFolder(dir);

                if (validMap != null)
                {
                    return validMap;
                }
            }

            return null;
        }

        public static MapMetaData LoadMapMetaData(MapListItem mapItem)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(mapItem.DirectoryPath);

                string fileName = $"{dirInfo.Name}_{mapItem.MapName}_meta.json";
                string fileContents = File.ReadAllText(fileName);

                return JsonConvert.DeserializeObject<MapMetaData>(fileContents);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void SaveMapMetaData(MapMetaData metaData)
        {
            try
            {
                string fileName = metaData.GetJsonFileName();

                string jsonToSave = JsonConvert.SerializeObject(metaData);

                File.WriteAllText(Path.Combine(FullPathToMetaFolder, fileName), jsonToSave);
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}
