﻿using IniParser;
using IniParser.Model;
using SessionMapSwitcherCore.Classes.Interfaces;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionMapSwitcherCore.Classes
{
    /// <summary>
    /// Class to handle loading maps for EzPz patched games.
    /// </summary>
    public class EzPzMapSwitcher : IMapSwitcher
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<string> _loadedMapFiles;

        public List<MapListItem> DefaultMaps { get; private set; }
        internal MapListItem FirstLoadedMap { get; set; }

        public List<string> LoadedMapFiles
        {
            get
            {
                if (_loadedMapFiles == null)
                {
                    _loadedMapFiles = new List<string>();
                }
                return _loadedMapFiles;
            }
            set
            {
                _loadedMapFiles = value;
            }
        }

        public EzPzMapSwitcher()
        {
            DefaultMaps = new List<MapListItem>()
            {
                new MapListItem()
                {
                    GameDefaultMapSetting ="/Game/Tutorial/Intro/MAP_EntryPoint",
                    MapName = "Session Default Map - Brooklyn Banks",
                    PathToImage = Path.Combine(SessionPath.ToApplicationRoot, "Resources", "defaultMap1.png"),
                    IsDefaultMap = true
                },
                new MapListItem()
                {
                    GameDefaultMapSetting = "/Game/Art/Env/GYM/crea-turePark/GYM_crea-turePark_Persistent.GYM_crea-turePark_Persistent",
                    GlobalDefaultGameModeSetting = "/Game/Data/PBP_InGameSessionGameMode.PBP_InGameSessionGameMode_C",
                    PathToImage = Path.Combine(SessionPath.ToApplicationRoot, "Resources", "defaultMap2.png"),
                    MapName = "Crea-ture Dev Park",
                    IsDefaultMap = true
                }
            };
        }

        public List<MapListItem> GetDefaultSessionMaps()
        {
            return DefaultMaps;
        }

        public MapListItem GetFirstLoadedMap()
        {
            return FirstLoadedMap;
        }

        public bool CopyMapFilesToNYCFolder(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            Logger.Info($"Copying Map Files for {map.MapName}");


            // copy all files related to map to game directory
            foreach (string fileName in Directory.GetFiles(map.DirectoryPath))
            {
                if (fileName.Contains(map.MapName))
                {
                    FileInfo fi = new FileInfo(fileName);
                    string fullTargetFilePath = SessionPath.ToNYCFolder;


                    if (SessionPath.IsSessionRunning())
                    {
                        // While Session is running the map files must be copied as NYC01_Persistent so when the user leaves the apartment the custom map is loaded
                        fullTargetFilePath = Path.Combine(fullTargetFilePath, "NYC01_Persistent");

                        if (fileName.Contains("_BuiltData"))
                        {
                            fullTargetFilePath += $"_BuiltData{fi.Extension}";
                        }
                        else
                        {
                            fullTargetFilePath += fi.Extension;
                        }
                    }
                    else
                    {
                        fullTargetFilePath = Path.Combine(fullTargetFilePath, fi.Name);
                    }


                    Logger.Info($"... copying {fileName} -> {fullTargetFilePath}");
                    File.Copy(fileName, fullTargetFilePath, overwrite: true);
                    LoadedMapFiles.Add(fullTargetFilePath);
                }
            }

            return true;
        }

        public BoolWithMessage LoadMap(MapListItem map)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Cannot Load: 'Path to Session' is invalid.");
            }

            if (map == null)
            {
                return BoolWithMessage.False("Cannot Load: map is null");
            }

            if (SessionPath.IsSessionRunning() == false || FirstLoadedMap == null)
            {
                FirstLoadedMap = map;
            }

            if (Directory.Exists(SessionPath.ToNYCFolder) == false)
            {
                Directory.CreateDirectory(SessionPath.ToNYCFolder);
            }

            if (map.IsDefaultMap)
            {
                return LoadDefaultMap(map);
            }

            try
            {
                // delete session map file / custom maps from game 
                DeleteMapFilesFromNYCFolder();
                string selectedMapPath;

                if (SessionPath.IsSessionRunning())
                {
                    // .. when the game is running the map file is renamed to NYC01_Persistent and copied to NYC folder so it can load when you leave the apartment
                    CopyMapFilesToNYCFolder(map);

                    // update the ini file with the new map path
                    selectedMapPath = "/Game/Art/Env/NYC/NYC01_Persistent";
                }
                else
                {
                    selectedMapPath = map.MapPathForIni;
                }

                SetGameDefaultMapSetting(selectedMapPath);

                return BoolWithMessage.True($"{map.MapName} Loaded!");
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to load {map.MapName}: {e.Message}");
            }
        }

        public BoolWithMessage LoadDefaultMap(MapListItem defaultMap)
        {
            try
            {
                DeleteMapFilesFromNYCFolder();

                SetGameDefaultMapSetting(defaultMap.GameDefaultMapSetting, defaultMap.GlobalDefaultGameModeSetting);

                return BoolWithMessage.True($"{defaultMap.MapName} Loaded!");

            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to load {defaultMap.MapName} : {e.Message}");
            }
        }

        /// <summary>
        /// Deletes all files in the Content/Art/Env/NYC folder
        /// </summary>
        public void DeleteMapFilesFromNYCFolder()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return;
            }

            // check if RMS is loaded
            if (RMSToolsuiteLoader.LoadedToolsuiteFiles.Count == 0)
            {
                RMSToolsuiteLoader.IsLoaded();
            }

            foreach (string fileName in Directory.GetFiles(SessionPath.ToNYCFolder))
            {
                if (RMSToolsuiteLoader.LoadedToolsuiteFiles.Contains(fileName))
                {
                    continue; // don't delete files associated with RMS
                }

                Logger.Info($"... deleting file {fileName}");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }

            LoadedMapFiles.Clear();
        }

        /// <summary>
        /// Checks .ini file for the map that will load on game start
        /// </summary>
        public string GetGameDefaultMapSetting()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return "";
            }

            CreateDefaultUserEngineIniFile();

            if (!File.Exists(SessionPath.ToUserEngineIniFile))
            {
                return "";
            }

            try
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData iniFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);

                return iniFile["/Script/EngineSettings.GameMapsSettings"]["GameDefaultMap"];
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return "";
            }
        }


        public bool SetGameDefaultMapSetting(string defaultMapValue, string defaultGameModeValue = "")
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return false;
            }

            CreateDefaultUserEngineIniFile();

            if (!File.Exists(SessionPath.ToUserEngineIniFile))
            {
                return false;
            }

            try
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData iniFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);

                iniFile["/Script/EngineSettings.GameMapsSettings"]["GameDefaultMap"] = defaultMapValue;

                if (!string.IsNullOrEmpty(defaultGameModeValue))
                {
                    iniFile["/Script/EngineSettings.GameMapsSettings"]["GlobalDefaultGameMode"] = defaultGameModeValue;
                }
                else if (iniFile["/Script/EngineSettings.GameMapsSettings"].ContainsKey("GlobalDefaultGameMode"))
                {
                    iniFile["/Script/EngineSettings.GameMapsSettings"].RemoveKey("GlobalDefaultGameMode");
                }

                parser.WriteFile(SessionPath.ToUserEngineIniFile, iniFile);

                Logger.Info($"... GameDefaultMap set to {defaultMapValue}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Creates UserEngine.ini file if does not exist
        /// </summary>
        public static void CreateDefaultUserEngineIniFile(bool deleteExisting = false)
        {
            if (File.Exists(SessionPath.ToUserEngineIniFile))
            {
                if (!deleteExisting)
                {
                    return; // already exists and not deleting so just return
                }

                File.Delete(SessionPath.ToUserEngineIniFile);
            }

            string defaultIniValues = @"[/Script/EngineSettings.GameMapsSettings]
GameDefaultMap = /Game/Tutorial/Intro/MAP_EntryPoint";

            if (!Directory.Exists(SessionPath.ToConfig))
            {
                Directory.CreateDirectory(SessionPath.ToConfig);
            }

            File.WriteAllText(SessionPath.ToUserEngineIniFile, defaultIniValues);
        }
    }
}
