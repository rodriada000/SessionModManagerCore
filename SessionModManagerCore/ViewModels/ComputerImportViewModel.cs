﻿using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class ComputerImportViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly List<string> FilesToExclude = new List<string>() { "DefaultEngine.ini", "DefaultGame.ini" };
        public static readonly List<string> StockFoldersToExclude = new List<string> { "Data", "ObjectPlacement"};


        private bool _isZipFileImport;
        private bool _isImporting;
        private string _userMessage;
        private string _pathInput;

        public string PathToTempUnzipFolder
        {
            get
            {
                return Path.Combine(SessionPath.ToSessionGame, "Temp_Unzipped");
            }
        }

        public bool IsZipFileImport
        {
            get { return _isZipFileImport; }
            set
            {
                _isZipFileImport = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PathLabel));
            }
        }

        public bool IsImporting
        {
            get { return _isImporting; }
            set
            {
                _isImporting = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotImporting));
            }
        }

        public bool IsNotImporting
        {
            get
            {
                return !IsImporting;
            }
        }

        public string PathLabel
        {
            get
            {
                if (IsZipFileImport)
                {
                    return "Path To Zip/Rar File:";
                }
                else
                {
                    return "Folder Path To Map Files:";
                }
            }
        }

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string PathInput
        {
            get { return _pathInput; }
            set
            {
                _pathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToFileOrFolder
        {
            get
            {
                if (String.IsNullOrEmpty(PathInput))
                {
                    return "";
                }

                if (PathInput.EndsWith("\\"))
                {
                    PathInput = PathInput.TrimEnd('\\');
                }

                if (PathInput.EndsWith("/"))
                {
                    PathInput = PathInput.TrimEnd('/');
                }

                return PathInput;
            }
        }

        public Asset AssetToImport { get; set; }

        public ComputerImportViewModel()
        {
            this.UserMessage = "";
            this.IsZipFileImport = false;
            this.PathInput = "";
        }

        public void BeginImportMapAsync()
        {
            UserMessage = "Importing Map ...";

            if (IsZipFileImport)
            {
                UserMessage += " Unzipping and copying can take a couple of minutes depending on the .zip file size.";
            }
            else
            {
                UserMessage += " Copying can take a couple of minutes depending on the amount of files to copy.";
                PathInput = EnsurePathToMapFilesIsCorrect(PathToFileOrFolder);
            }

            IsImporting = true;

            Task<BoolWithMessage> task = ImportMapAsync();

            task.ContinueWith((antecedent) =>
            {
                IsImporting = false;

                if (antecedent.IsFaulted)
                {
                    Logger.Warn(antecedent.Exception, "import task uncaught exception");
                    UserMessage = "An error occurred importing the map.";
                    return;
                }

                if (antecedent.Result.Result)
                {
                    UserMessage = "Map Imported!";
                }
                else
                {
                    UserMessage = $"Failed to import map: {antecedent.Result.Message}";
                }
            });
        }

        /// <summary>
        /// This will check if the folder path to a map has the 'Content' folder and returns path to the maps 'Content folder if so
        /// </summary>
        /// <returns>
        /// "pathToMapFiles/Content" if Content folder exists; otherwise original pathToMapFiles string is returned
        /// </returns>
        private string EnsurePathToMapFilesIsCorrect(string pathToMapFiles)
        {
            string pathToContent = Path.Combine(pathToMapFiles, "Content");

            if (Directory.Exists(pathToContent))
            {
                return pathToContent;
            }

            return pathToMapFiles;
        }

        internal Task<BoolWithMessage> ImportMapAsync()
        {
            Task<BoolWithMessage> task = Task.Factory.StartNew(() =>
            {
                string sourceFolderToCopy;

                if (IsZipFileImport)
                {
                    // extract compressed file before copying
                    if (File.Exists(PathToFileOrFolder) == false)
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not exist.");
                    }

                    // extract files first before copying
                    BoolWithMessage didExtract = BoolWithMessage.False("");

                    try
                    {
                        Directory.CreateDirectory(PathToTempUnzipFolder);
                        didExtract = FileUtils.ExtractCompressedFile(PathToFileOrFolder, PathToTempUnzipFolder);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "failed to extract zip");
                        didExtract.Message = e.Message;
                    }


                    if (didExtract.Result == false)
                    {
                        UserMessage = $"Failed to extract file: {didExtract.Message}";
                        return BoolWithMessage.False($"Failed to extract: {didExtract.Message}.");
                    }

                    sourceFolderToCopy = EnsurePathToMapFilesIsCorrect(PathToTempUnzipFolder);
                }
                else
                {
                    // validate folder exists and contains a valid map file
                    if (Directory.Exists(PathToFileOrFolder) == false)
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not exist.");
                    }

                    sourceFolderToCopy = PathToFileOrFolder;

                    bool hasValidMap = MetaDataManager.DoesValidMapExistInFolder(sourceFolderToCopy);

                    if (hasValidMap == false)
                    {
                        return BoolWithMessage.False($"{PathToFileOrFolder} does not contain a valid .umap file to import.");
                    }
                }

                // create meta data for new map and save to disk
                MapMetaData metaData = MetaDataManager.CreateMapMetaData(sourceFolderToCopy, true);

                if (AssetToImport != null)
                {
                    metaData.AssetName = AssetToImport.ID;
                    
                    if (File.Exists(AssetToImport.PathToDownloadedImage))
                    {
                        metaData.PathToImage = AssetToImport.PathToDownloadedImage;
                    }
                }

                if (IsZipFileImport == false && metaData != null)
                {
                    metaData.OriginalImportPath = sourceFolderToCopy;
                }

                MetaDataManager.SaveMapMetaData(metaData);

                // copy/move files
                if (IsZipFileImport)
                {
                    FileUtils.MoveDirectoryRecursively(sourceFolderToCopy, SessionPath.ToContent, filesToExclude: FilesToExclude, foldersToExclude: StockFoldersToExclude, doContainsSearch: false);
                }
                else
                {
                    FileUtils.CopyDirectoryRecursively(sourceFolderToCopy, SessionPath.ToContent, filesToExclude: FilesToExclude, foldersToExclude: StockFoldersToExclude, doContainsSearch: false);
                }


                if (IsZipFileImport && Directory.Exists(PathToTempUnzipFolder))
                {
                    // remove unzipped temp files
                    Directory.Delete(PathToTempUnzipFolder, true);
                }

                return BoolWithMessage.True();

            });

            return task;
        }
    }
}
