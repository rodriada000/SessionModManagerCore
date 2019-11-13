using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SessionMapSwitcherCore.Utils
{
    public class FileUtils
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToExclude, bool doContainsSearch)
        {
            if (filesToExclude == null)
            {
                filesToExclude = new List<string>();
            }

            if (foldersToExclude == null)
            {
                foldersToExclude = new List<string>();
            }

            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToExclude,
                ContainsSearchForFiles = doContainsSearch
            };

            try
            {
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void CopyDirectoryRecursively(string sourceDirName, string destDirName)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = false,
                CopySubFolders = true,
                ContainsSearchForFiles = false
            };

            try
            {
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName, List<string> filesToExclude, List<string> foldersToExclude, bool doContainsSearch)
        {
            if (filesToExclude == null)
            {
                filesToExclude = new List<string>();
            }

            if (foldersToExclude == null)
            {
                foldersToExclude = new List<string>();
            }

            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
                ExcludeFiles = filesToExclude,
                ExcludeFolders = foldersToExclude,
                ContainsSearchForFiles = doContainsSearch
            };

            try
            {
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal static void MoveDirectoryRecursively(string sourceDirName, string destDirName)
        {
            CopySettings settings = new CopySettings()
            {
                IsMovingFiles = true,
                CopySubFolders = true,
                ContainsSearchForFiles = false
            };

            try
            {
                CopyOrMoveDirectoryRecursively(sourceDirName, destDirName, settings);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static void CopyOrMoveDirectoryRecursively(string sourceDirName, string destDirName, CopySettings settings)
        {
            if (settings.ExcludeFiles == null)
            {
                settings.ExcludeFiles = new List<string>();
            }

            if (settings.ExcludeFolders == null)
            {
                settings.ExcludeFolders = new List<string>();
            }

            // reference: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (settings.ExcludeFile(file))
                {
                    continue; // skip file as it is excluded
                }

                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                }

                string temppath = Path.Combine(destDirName, file.Name);

                if (settings.IsMovingFiles)
                {
                    if (File.Exists(temppath))
                    {
                        File.Delete(temppath); // delete existing file before moving new file
                    }
                    file.MoveTo(temppath);
                }
                else
                {
                    file.CopyTo(temppath, true);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (settings.CopySubFolders)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();

                foreach (DirectoryInfo subdir in dirs)
                {
                    if (settings.ExcludeFolders.Contains(subdir.Name))
                    {
                        continue; // skip folder as it is excluded
                    }

                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    CopyOrMoveDirectoryRecursively(subdir.FullName, tempPath, settings);
                }
            }
        }


        /// <summary>
        /// Extract a zip file to a given path. Returns true on success.
        /// </summary>
        public static BoolWithMessage ExtractZipFile(string pathToZip, string extractPath)
        {
            try
            {
                Logger.Info($"extracting .zip {pathToZip} ...");

                using (ZipArchive archive = ZipFile.OpenRead(pathToZip))
                {
                    Logger.Info("... Opened .zip for read");

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fullFileName = Path.Combine(extractPath, entry.FullName);
                        string entryPath = Path.GetDirectoryName(fullFileName);

                        Logger.Info($"... {fullFileName} -> {entryPath}");

                        if (Directory.Exists(entryPath) == false)
                        {
                            Logger.Info($"... creating missing directory {entryPath}");
                            Directory.CreateDirectory(entryPath);
                        }

                        bool isFileToExtract = (Path.GetFileName(fullFileName) != "");

                        Logger.Info($"... {fullFileName}, isFileToExtract: {isFileToExtract}");

                        if (isFileToExtract)
                        {
                            Logger.Info($"...... extracting");
                            entry.ExtractToFile(Path.GetFullPath(fullFileName), overwrite: true);
                            Logger.Info($"......... extracted!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return new BoolWithMessage(false, e.Message);
            }

            return new BoolWithMessage(true);
        }

        public static BoolWithMessage ExtractRarFile(string pathToRar, string extractPath)
        {
            try
            {
                Logger.Info($"extracting .rar {pathToRar} ...");


                using (RarArchive archive = RarArchive.Open(pathToRar))
                {
                    Logger.Info("... Opened .rar for read");

                    foreach (RarArchiveEntry entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        Logger.Info($"...... extracting {entry.Key}");

                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });

                        Logger.Info($"......... extracted!");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return new BoolWithMessage(false, e.Message);
            }

            return new BoolWithMessage(true);
        }

        public static BoolWithMessage ExtractCompressedFile(string pathToFile, string extractPath)
        {
            if (pathToFile.EndsWith(".zip"))
            {
                return ExtractZipFile(pathToFile, extractPath);
            }
            else if (pathToFile.EndsWith(".rar"))
            {
                return ExtractRarFile(pathToFile, extractPath);
            }

            Logger.Warn($"Unsupported file type: {pathToFile}");
            return new BoolWithMessage(false, "Unsupported file type.");
        }


        public static List<string> GetAllFilesInDirectory(string directoryPath)
        {
            List<string> allFiles = new List<string>();

            if (Directory.Exists(directoryPath) == false)
            {
                return allFiles;
            }

            foreach (string file in Directory.GetFiles(directoryPath))
            {
                allFiles.Add(file);
            }

            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                List<string> subDirFiles = GetAllFilesInDirectory(dir);

                if (subDirFiles.Count > 0)
                {
                    allFiles.AddRange(subDirFiles);
                }
            }

            return allFiles;
        }

        public static BoolWithMessage DeleteMapFiles(MapMetaData metaData)
        {

            if (metaData == null)
            {
                return BoolWithMessage.False($"meta data is null");
            }

            if (metaData.FilePaths?.Count == 0)
            {
                return BoolWithMessage.False($"List of files to delete is unknown for {metaData.MapName}. You must manually delete the map files from the following folder: {metaData.MapFileDirectory}");
            }

            try
            {
                HashSet<string> possibleFoldersToDelete = new HashSet<string>(); // this will be a list of directories where files were deleted; if these directories are empty then they will be deleted

                foreach (string file in metaData.FilePaths)
                {
                    if (File.Exists(file))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        
                        if (possibleFoldersToDelete.Contains(fileInfo.DirectoryName) == false)
                        {
                            possibleFoldersToDelete.Add(fileInfo.DirectoryName);
                        }
                        

                        File.Delete(file);
                    }
                }

                // delete the possible empty directories
                List<string> filesInDir;

                foreach (string folder in possibleFoldersToDelete)
                {
                    // iteratively go up parent folder structure to delete empty folders after files have been deleted
                    string currentDir = folder;

                    if (Directory.Exists(currentDir) && currentDir != SessionPath.ToContent)
                    {
                        List<string> remainingFiles = FileUtils.GetAllFilesInDirectory(currentDir);

                        while (remainingFiles.Count == 0 && currentDir != SessionPath.ToContent)
                        {
                            string dirToDelete = currentDir;

                            DirectoryInfo dirInfo = new DirectoryInfo(currentDir);
                            currentDir = dirInfo.Parent.FullName; // get path to parent directory to check next

                            Directory.Delete(dirToDelete, true);

                            if (currentDir != SessionPath.ToContent)
                            {
                                remainingFiles = FileUtils.GetAllFilesInDirectory(currentDir); // get list of files from parent dir to check next
                            }
                        }
                    }
                }

                // lastly delete meta data file
                string pathToMetaData = Path.Combine(MetaDataManager.FullPathToMetaFolder, metaData.GetJsonFileName());
                if (File.Exists(pathToMetaData))
                {
                    File.Delete(pathToMetaData);
                }

                return BoolWithMessage.True($"{metaData.MapName} has been deleted!");
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to delete files: {e.Message}");
            }

        }
    }

}
