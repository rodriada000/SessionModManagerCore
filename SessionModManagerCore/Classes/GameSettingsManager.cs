using IniParser;
using IniParser.Model;
using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace SessionMapSwitcherCore.Classes
{
    public enum VideoSettingsOptions
    {
        Low,
        Medium,
        High,
        Epic
    }

    public static class GameSettingsManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static bool SkipIntroMovie { get; set; }

        public static bool EnableDBuffer { get; set; }
        public static bool EnableLightPropagationVolume { get; set; }

        public static int ObjectCount { get; set; }

        private static bool _enableVsync;
        private static int _fullscreenMode;
        private static int _textureQuality;
        private static int _shadingQuality;
        private static int _viewDistanceQuality;
        private static int _antiAliasingQuality;
        private static int _shadowQuality;
        private static int _foliageQuality;
        private static int _postProcessQuality;
        private static int _effectsQuality;

        public static int FrameRateLimit { get; set; }

        public static int FullscreenMode { get => _fullscreenMode; set => _fullscreenMode = value; }

        public static bool EnableVsync { get => _enableVsync; set => _enableVsync = value; }

        public static string ResolutionSizeX { get; set; }
        public static string ResolutionSizeY { get; set; }

        public static int TextureQuality { get => _textureQuality; set => _textureQuality = value; }
        public static int ShadingQuality { get => _shadingQuality; set => _shadingQuality = value; }
        public static int ViewDistanceQuality { get => _viewDistanceQuality; set => _viewDistanceQuality = value; }
        public static int AntiAliasingQuality { get => _antiAliasingQuality; set => _antiAliasingQuality = value; }
        public static int ShadowQuality { get => _shadowQuality; set => _shadowQuality = value; }
        public static int FoliageQuality { get => _foliageQuality; set => _foliageQuality = value; }
        public static int PostProcessQuality { get => _postProcessQuality; set => _postProcessQuality = value; }
        public static int EffectsQuality { get => _effectsQuality; set => _effectsQuality = value; }


        public static string PathToInventorySaveSlotFile
        {
            get
            {
                return Path.Combine(SessionPath.ToSaveGamesFolder, "PlayerInventorySaveSlot.sav");
            }
        }

        public static string PathToGameUserSettingsFile
        {
            get
            {
                return Path.Combine(SessionPath.ToLocalAppDataConfigFolder, "GameUserSettings.ini");
            }
        }

        public static BoolWithMessage RefreshGameSettingsFromIniFiles()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            try
            {
                IniData engineFile = null;
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;

                EzPzMapSwitcher.CreateDefaultUserEngineIniFile();
                engineFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);

                GetRenderSettingsFromIniFile(engineFile);

                SkipIntroMovie = IsSkippingMovies();

                GetObjectCountFromFile();

                GetVideoSettingsFromFile();

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                SkipIntroMovie = false;

                Logger.Error(e);
                return BoolWithMessage.False($"Could not get game settings: {e.Message}");
            }
        }

        private static void GetRenderSettingsFromIniFile(IniData engineFile)
        {
            string setting = null;
            bool parsedBool = false;

            try
            {
                setting = engineFile["/Script/Engine.RendererSettings"]["r.LightPropagationVolume"];
            }
            catch (Exception) { };

            if (String.IsNullOrWhiteSpace(setting))
            {
                setting = "false";
            }

            bool.TryParse(setting, out parsedBool);
            EnableLightPropagationVolume = parsedBool;


            try
            {
                setting = null;
                parsedBool = false;
                setting = engineFile["/Script/Engine.RendererSettings"]["r.DBuffer"];
            }
            catch (Exception) { };

            if (String.IsNullOrWhiteSpace(setting))
            {
                setting = "true";
            }

            bool.TryParse(setting, out parsedBool);
            EnableDBuffer = parsedBool;
        }

        /// <summary>
        /// validates the object count and then writes to the correct file to update object count.
        /// </summary>
        public static BoolWithMessage ValidateAndUpdateObjectCount(string objectCountText)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            if (int.TryParse(objectCountText, out int parsedObjCount) == false)
            {
                return BoolWithMessage.False("Invalid Object Count setting.");
            }

            if (parsedObjCount <= 0 || parsedObjCount > 65535)
            {
                return BoolWithMessage.False("Object Count must be between 0 and 65535.");
            }


            try
            {
                BoolWithMessage didSetCount = SetObjectCountInFile(objectCountText);
                return didSetCount;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return BoolWithMessage.False($"Failed to update object count: {e.Message}");
            }
        }

        /// <summary>
        /// updates various settings in UserEngine.ini and rename 'Movies' folder to skip movies if enabled
        /// </summary>
        public static BoolWithMessage ValidateAndUpdateGameSettings(bool skipMovie, bool enableLpv, bool enableDBuffer)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            try
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData engineFile = null;

                EzPzMapSwitcher.CreateDefaultUserEngineIniFile();
                engineFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);

                engineFile["/Script/Engine.RendererSettings"]["r.LightPropagationVolume"] = enableLpv.ToString();
                engineFile["/Script/Engine.RendererSettings"]["r.DBuffer"] = enableDBuffer.ToString();

                parser.WriteFile(SessionPath.ToUserEngineIniFile, engineFile);

                RenameMoviesFolderToSkipMovies(skipMovie);

                // update in-memory static data members
                SkipIntroMovie = skipMovie;
                EnableLightPropagationVolume = enableLpv;
                EnableDBuffer = enableDBuffer;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Warn("Re-creating UserEngine.ini file in case it is corrupted");
                EzPzMapSwitcher.CreateDefaultUserEngineIniFile(deleteExisting: true);

                return BoolWithMessage.False($"Failed to update gravity and/or skip movie: {e.Message}");
            }

            return BoolWithMessage.True();
        }


        /// <summary>
        /// Get the Object Placement count from the file (only reads the first address) and set <see cref="ObjectCount"/>
        /// </summary>
        internal static BoolWithMessage GetObjectCountFromFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            if (DoesInventorySaveFileExist() == false)
            {
                return BoolWithMessage.False("Object Count file does not exist.");
            }

            try
            {
                List<int> fileAddresses = GetQuantityFileAddresses();

                if (fileAddresses.Count == 0)
                {
                    return BoolWithMessage.False($"Failed to find address in file - {PathToInventorySaveSlotFile}");
                }

                using (var stream = new FileStream(PathToInventorySaveSlotFile, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = fileAddresses[0];
                    int byte1 = stream.ReadByte();
                    int byte2 = stream.ReadByte();
                    byte[] byteArray;

                    // convert two bytes to a hex string. if the second byte is less than 16 than swap the bytes due to reasons....
                    if (byte2 == 0)
                    {
                        byteArray = new byte[] { 0x00, Byte.Parse(byte1.ToString()) };
                    }
                    else if (byte2 < 16)
                    {
                        byteArray = new byte[] { Byte.Parse(byte2.ToString()), Byte.Parse(byte1.ToString()) };
                    }
                    else
                    {
                        byteArray = new byte[] { Byte.Parse(byte1.ToString()), Byte.Parse(byte2.ToString()) };
                    }
                    string hexString = BitConverter.ToString(byteArray).Replace("-", "");

                    // convert the hex string to base 10 int value
                    ObjectCount = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);

                    return BoolWithMessage.True();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get object count");
                return BoolWithMessage.False($"Failed to get object count: {e.Message}");
            }
        }

        /// <summary>
        /// Updates the PBP_ObjectPlacementInventory.uexp file with the new object count value (every placeable object is updated with new count).
        /// This works by converting <paramref name="objectCountText"/> to bytes and writing the bytes to specific addresses in the file.
        /// </summary>
        internal static BoolWithMessage SetObjectCountInFile(string objectCountText)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            if (DoesInventorySaveFileExist() == false)
            {
                return BoolWithMessage.False("Object Count file does not exist.");
            }

            // as of Session 0.0.0.7 the quantity is saved in the player inventory .sav file. So we need to dynamically find all the addresses for each DIY item in the player inventory
            List<int> addresses = GetQuantityFileAddresses();

            if (!File.Exists(PathToInventorySaveSlotFile + ".smm.bak"))
            {
                // create a backup before modifying the file in case we break someones inventory....
                File.Copy(PathToInventorySaveSlotFile, PathToInventorySaveSlotFile + ".smm.bak");
            }

            try
            {
                using (var stream = new FileStream(PathToInventorySaveSlotFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    // convert the base 10 int into a hex string (e.g. 10 => 'A' or 65535 => 'FF')
                    string hexValue = int.Parse(objectCountText).ToString("X");

                    // convert the hext string into a byte array that will be written to the file
                    byte[] bytes = StringToByteArray(hexValue);

                    if (hexValue.Length == 3)
                    {
                        // swap bytes around for some reason when the hex string is only 3 characters long... big-endian little-endian??
                        byte temp = bytes[1];
                        bytes[1] = bytes[0];
                        bytes[0] = temp;
                    }

                    // loop over every address so every placeable object is updated with new item count
                    foreach (int fileAddress in addresses)
                    {
                        stream.Position = fileAddress;
                        stream.WriteByte(bytes[0]);

                        // when object count is less than 16 than the byte array will only have 1 byte so write null in next byte position
                        if (bytes.Length > 1)
                        {
                            stream.WriteByte(bytes[1]);
                        }
                        else
                        {
                            stream.WriteByte(0x00);
                        }
                    }

                    stream.Flush(); // ensure file is written to
                }

                ObjectCount = int.Parse(objectCountText); // set in-memory setting to new value written to file
                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to set object count");
                return BoolWithMessage.False($"Failed to set object count: {e.Message}");
            }
        }

        private static byte[] StringToByteArray(String hex)
        {
            // reference: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa

            if (hex.Length % 2 != 0)
            {
                // pad with '0' for odd length strings like 'A' so it becomes '0A' or '1A4' => '01A4'
                hex = '0' + hex;
            }

            int numChars = hex.Length;
            byte[] bytes = new byte[numChars / 2];
            for (int i = 0; i < numChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static List<int> GetQuantityFileAddresses()
        {
            List<int> foundQtyAddresses = new List<int>();

            if (SessionPath.IsSessionPathValid() == false)
            {
                return foundQtyAddresses;
            }

            if (DoesInventorySaveFileExist() == false)
            {
                return foundQtyAddresses;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(PathToInventorySaveSlotFile);
                string hexString = BitConverter.ToString(fileBytes);
                List<string> hexFileArray = hexString.Split('-').ToList();

                string hexToFind = "51-75-61-6e-74-69-74-79-00-0c-00-00-00-49-6e-74-50-72-6f-70-65-72-74-79-00-04-00-00-00-00-00-00-00-00".ToUpper();
                List<string> hexArray = hexToFind.Split('-').ToList();

                int address = 0;

                do
                {
                    address = FindSequenceInArray(hexFileArray, hexArray, address);

                    if (address != -1)
                    {
                        foundQtyAddresses.Add((address + hexArray.Count));
                    }

                } while (address != -1);

                return foundQtyAddresses;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to get object count");
                return foundQtyAddresses;
            }
        }

        public static int FindSequenceInArray(List<string> arrayToSearch, List<string> sequence, int startIndex = 0)
        {
            if (startIndex == 0)
            {
                startIndex = arrayToSearch.Count - 1;
            }
            // reference: https://stackoverflow.com/questions/55150204/find-subarray-in-array-in-c-sharp
            // iterate backwards, stop if the rest of the array is shorter than needle (i >= needle.Length)
            for (int i = startIndex; i >= sequence.Count - 1; i--)
            {
                bool found = true;
                // also iterate backwards through needle, stop if elements do not match (!found)
                for (int j = sequence.Count - 1; j >= 0 && found; j--)
                {
                    // compare needle's element with corresponding element of haystack
                    found = arrayToSearch[i - (sequence.Count - 1 - j)] == sequence[j];
                }

                if (found)
                {
                    // result was found, i is now the index of the last found element, so subtract needle's length - 1
                    return i - (sequence.Count - 1);
                }
            }

            // not found, return -1
            return -1;
        }

        public static bool DoesInventorySaveFileExist()
        {
            return File.Exists(PathToInventorySaveSlotFile);
        }

        /// <summary>
        /// If skipping movies then renames 'Movies' folder to 'Movies_SKIP'.
        /// If not skipping then renames folder to 'Movies'
        /// </summary>
        public static BoolWithMessage RenameMoviesFolderToSkipMovies(bool skipMovies)
        {
            try
            {
                string movieSkipFolderPath = SessionPath.ToMovies.Replace("Movies", "Movies_SKIP");

                if (skipMovies)
                {
                    if (Directory.Exists(SessionPath.ToMovies))
                    {
                        if (Directory.Exists(movieSkipFolderPath)) // in the case that "Movies" and "Movies_SKIP" folder both exist then make sure the existing _SKIP folder is deleted before renaming folder
                        {
                            Directory.Delete(movieSkipFolderPath, true);
                        }

                        Directory.Move(SessionPath.ToMovies, movieSkipFolderPath);
                    }
                }
                else
                {
                    if (Directory.Exists(movieSkipFolderPath))
                    {
                        Directory.Move(movieSkipFolderPath, SessionPath.ToMovies);
                    }
                }

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to rename Movies folder");
                return BoolWithMessage.False($"Failed to rename Movies folder: {e.Message}");
            }

        }

        public static bool IsSkippingMovies()
        {
            return Directory.Exists(SessionPath.ToMovies.Replace("Movies", "Movies_SKIP")) && !Directory.Exists(SessionPath.ToMovies);
        }

        public static BoolWithMessage GetVideoSettingsFromFile()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            if (!Directory.Exists(SessionPath.ToLocalAppDataConfigFolder) || !File.Exists(PathToGameUserSettingsFile))
            {
                return BoolWithMessage.True();
            }

            try
            {
                IniData settingsFile = null;
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                parser.Parser.Configuration.AllowCreateSectionsOnFly = true;

                settingsFile = parser.ReadFile(PathToGameUserSettingsFile);

                if (settingsFile["/Script/Engine.GameUserSettings"].ContainsKey("FrameRateLimit"))
                {
                    float.TryParse(settingsFile["/Script/Engine.GameUserSettings"]["FrameRateLimit"], out float frameRate);
                    FrameRateLimit = (int)frameRate;
                }

                if (settingsFile["/Script/Engine.GameUserSettings"].ContainsKey("bUseVSync"))
                {
                    bool.TryParse(settingsFile["/Script/Engine.GameUserSettings"]["bUseVSync"], out _enableVsync);
                }

                if (settingsFile["/Script/Engine.GameUserSettings"].ContainsKey("ResolutionSizeX"))
                {
                    ResolutionSizeX = settingsFile["/Script/Engine.GameUserSettings"]["ResolutionSizeX"];
                }

                if (settingsFile["/Script/Engine.GameUserSettings"].ContainsKey("ResolutionSizeY"))
                {
                    ResolutionSizeY = settingsFile["/Script/Engine.GameUserSettings"]["ResolutionSizeY"];
                }

                FullscreenMode = 0;
                if (settingsFile["/Script/Engine.GameUserSettings"].ContainsKey("FullscreenMode"))
                {
                    int.TryParse(settingsFile["/Script/Engine.GameUserSettings"]["FullscreenMode"], out _fullscreenMode);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.TextureQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.TextureQuality"], out _textureQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.ShadingQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.ShadingQuality"], out _shadingQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.ViewDistanceQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.ViewDistanceQuality"], out _viewDistanceQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.AntiAliasingQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.AntiAliasingQuality"], out _antiAliasingQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.ShadowQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.ShadowQuality"], out _shadowQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.FoliageQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.FoliageQuality"], out _foliageQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.PostProcessQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.PostProcessQuality"], out _postProcessQuality);
                }

                if (settingsFile["ScalabilityGroups"].ContainsKey("sg.EffectsQuality"))
                {
                    int.TryParse(settingsFile["ScalabilityGroups"]["sg.EffectsQuality"], out _effectsQuality);
                }

            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return BoolWithMessage.False(e.Message);
            }

            return BoolWithMessage.True();
        }

        public static BoolWithMessage SaveVideoSettingsToDisk()
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            if (!Directory.Exists(SessionPath.ToLocalAppDataConfigFolder) || !File.Exists(PathToGameUserSettingsFile))
            {
                return BoolWithMessage.False("Cannot save video settings due to missing config file. Make sure to have start the game at least once.");
            }

            try
            {
                IniData settingsFile = null;
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                parser.Parser.Configuration.AllowCreateSectionsOnFly = true;

                settingsFile = parser.ReadFile(PathToGameUserSettingsFile);

                settingsFile["/Script/Engine.GameUserSettings"]["FrameRateLimit"] = FrameRateLimit.ToString();
                settingsFile["/Script/Engine.GameUserSettings"]["bUseVSync"] = EnableVsync.ToString();

                settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("LastUserConfirmedResolutionSizeX");
                settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("LastUserConfirmedResolutionSizeY");
                settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("LastConfirmedFullscreenMode");

                if (ResolutionSizeX == null || ResolutionSizeY == null)
                {
                    settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("ResolutionSizeX");
                    settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("ResolutionSizeY");
                }
                else
                {
                    settingsFile["/Script/Engine.GameUserSettings"]["ResolutionSizeX"] = ResolutionSizeX;
                    settingsFile["/Script/Engine.GameUserSettings"]["ResolutionSizeY"] = ResolutionSizeY;
                }

                if (FullscreenMode == 0)
                {
                    settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("FullscreenMode");
                    settingsFile["/Script/Engine.GameUserSettings"].RemoveKey("PreferredFullscreenMode");
                }
                else
                {
                    settingsFile["/Script/Engine.GameUserSettings"]["FullscreenMode"] = FullscreenMode.ToString();
                    settingsFile["/Script/Engine.GameUserSettings"]["PreferredFullscreenMode"] = FullscreenMode.ToString();
                }

                settingsFile["ScalabilityGroups"]["sg.TextureQuality"] = TextureQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.ShadingQuality"] = ShadingQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.ViewDistanceQuality"] = ViewDistanceQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.AntiAliasingQuality"] = AntiAliasingQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.ShadowQuality"] = ShadowQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.FoliageQuality"] = FoliageQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.PostProcessQuality"] = PostProcessQuality.ToString();
                settingsFile["ScalabilityGroups"]["sg.EffectsQuality"] = EffectsQuality.ToString();

                parser.WriteFile(PathToGameUserSettingsFile, settingsFile);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return BoolWithMessage.False(e.Message);
            }

            return BoolWithMessage.True();
        }
    }
}
