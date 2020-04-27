using IniParser;
using IniParser.Model;
using SessionMapSwitcherCore.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SessionMapSwitcherCore.Classes
{
    public static class GameSettingsManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static double _gravity;
        public static double Gravity { get => _gravity; set => _gravity = value; }

        public static bool SkipIntroMovie { get; set; }

        public static int ObjectCount { get; set; }

        public static string PathToObjectPlacementFile
        {
            get
            {
                return Path.Combine(new string[] { SessionPath.ToContent, "ObjectPlacement", "Blueprints", "PBP_ObjectPlacementInventory.uexp" });
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

                if (UnpackUtils.IsSessionUnpacked())
                {
                    engineFile = parser.ReadFile(SessionPath.ToDefaultEngineIniFile);
                }
                else if (UeModUnlocker.IsGamePatched())
                {
                    EzPzMapSwitcher.CreateDefaultUserEngineIniFile();
                    engineFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);
                }

                string gravitySetting = null;
                try
                {
                    gravitySetting = engineFile["/Script/Engine.PhysicsSettings"]["DefaultGravityZ"];
                }
                catch (Exception) { };

                if (String.IsNullOrWhiteSpace(gravitySetting))
                {
                    gravitySetting = "-980";
                }

                double.TryParse(gravitySetting, out _gravity);

                SkipIntroMovie = IsSkippingMovies();

                GetObjectCountFromFile();

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                Gravity = -980;
                SkipIntroMovie = false;

                Logger.Error(e);
                return BoolWithMessage.False($"Could not get game settings: {e.Message}");
            }
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

        public static BoolWithMessage ValidateAndUpdateGravityAndSkipMoviesSettings(string gravityText, bool skipMovie)
        {
            if (SessionPath.IsSessionPathValid() == false)
            {
                return BoolWithMessage.False("Session Path invalid.");
            }

            // remove trailing 0's from float value for it to parse correctly
            int indexOfDot = gravityText.IndexOf(".");
            if (indexOfDot >= 0)
            {
                gravityText = gravityText.Substring(0, indexOfDot);
            }

            if (float.TryParse(gravityText, out float gravityFloat) == false)
            {
                return BoolWithMessage.False("Invalid Gravity setting.");
            }

            try
            {
                var parser = new FileIniDataParser();
                parser.Parser.Configuration.AllowDuplicateKeys = true;
                IniData engineFile = null;

                if (UnpackUtils.IsSessionUnpacked())
                {
                    engineFile = parser.ReadFile(SessionPath.ToDefaultEngineIniFile);
                }
                else if (UeModUnlocker.IsGamePatched())
                {
                    EzPzMapSwitcher.CreateDefaultUserEngineIniFile();
                    engineFile = parser.ReadFile(SessionPath.ToUserEngineIniFile);
                }

                engineFile["/Script/Engine.PhysicsSettings"]["DefaultGravityZ"] = gravityText;

                if (UnpackUtils.IsSessionUnpacked())
                {
                    parser.WriteFile(SessionPath.ToDefaultEngineIniFile, engineFile);
                }
                else if (UeModUnlocker.IsGamePatched())
                {
                    parser.WriteFile(SessionPath.ToUserEngineIniFile, engineFile);
                }                

                RenameMoviesFolderToSkipMovies(skipMovie);

                Gravity = gravityFloat;
                SkipIntroMovie = skipMovie;
            }
            catch (Exception e)
            {
                Logger.Error(e);
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

            if (DoesObjectPlacementFileExist() == false)
            {
                return BoolWithMessage.False("Object Count file does not exist.");
            }

            try
            {
                using (var stream = new FileStream(PathToObjectPlacementFile, FileMode.Open, FileAccess.Read))
                {
                    stream.Position = 352; // 352 (0x00000160) is the first address of a object count
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

            if (DoesObjectPlacementFileExist() == false)
            {
                return BoolWithMessage.False("Object Count file does not exist.");
            }

            // this is a list of addresses where the item count for placeable objects are stored in the .uexp file
            // ... if this file is modified then these addresses will NOT match so it is important to not mod/change the PBP_ObjectPlacementInventory file (until further notice...)
            // ... as of Session 0.0.0.5 these addresses are shifted by one
            List<int> addresses = new List<int>() { 352, 616, 682, 748, 880, 946, 1012, 1078, 1144, 1210, 1276, 1342, 1408, 1474, 1540, 1606, 1672, 1738, 1804, 1870, 1936, 2002, 2068 };

            try
            {
                using (var stream = new FileStream(PathToObjectPlacementFile, FileMode.Open, FileAccess.ReadWrite))
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

        public static bool DoesObjectPlacementFileExist()
        {
            return File.Exists(PathToObjectPlacementFile);
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
            return Directory.Exists(SessionPath.ToMovies.Replace("Movies", "Movies_SKIP"));
        }
    }
}
