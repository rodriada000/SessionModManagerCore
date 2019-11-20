using Amazon.S3.Model;
using Newtonsoft.Json;
using SessionAssetStore;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using SessionModManagerCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.ViewModels
{
    public class AssetStoreViewModel : ViewModelBase
    {
        #region Fields

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string storeFolderName = "store_data";

        public const string downloadsFolderName = "temp_downloads";

        public const string thumbnailFolderName = "thumbnails";

        public const string defaultInstallStatusValue = "Installed / Not Installed";
        public const string defaultAuthorValue = "Show All";

        private StorageManager _assetManager;
        private string _userMessage;
        private string _installButtonText;
        private string _removeButtonText;
        private Stream _imageSource;
        private string _selectedDescription;
        private string _selectedAuthor;
        private AuthorDropdownViewModel _authorToFilterBy;
        private string _selectedInstallStatus;
        private bool _fetchAllPreviewImages;
        private bool _deleteDownloadAfterInstall;
        private bool _isInstallButtonEnabled;
        private bool _isRemoveButtonEnabled;
        private bool _isLoadingManifests;
        private bool _isInstallingAsset;
        private bool _isLoadingImage;
        private List<AssetViewModel> _filteredAssetList;
        private List<AssetViewModel> _allAssets;
        private List<AuthorDropdownViewModel> _authorList;
        private List<string> _installStatusList;


        private object filteredListLock = new object();
        private object allListLock = new object();
        private object manifestFileLock = new object();

        private bool _displayAll;
        private bool _displayMaps;
        private bool _displayDecks;
        private bool _displayGriptapes;
        private bool _displayTrucks;
        private bool _displayWheels;
        private bool _displayHats;
        private bool _displayShirts;
        private bool _displayPants;
        private bool _displayShoes;

        #endregion

        #region Properties

        public static string AbsolutePathToStoreData
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, storeFolderName);
            }
        }

        public static string AbsolutePathToThumbnails
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, thumbnailFolderName);
            }
        }

        public static string AbsolutePathToTempDownloads
        {
            get
            {
                return Path.Combine(AbsolutePathToStoreData, downloadsFolderName);
            }
        }

        public static string AbsolutePathToTempManifests
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, StorageManager.MANIFESTS_TEMP);
            }
        }

        public bool DisplayAll
        {
            get { return _displayAll; }
            set
            {
                _displayAll = value;

                // setting the private variables so the list is not refreshed for every category until the end
                _displayMaps = value;
                _displayDecks = value;
                _displayGriptapes = value;
                _displayTrucks = value;
                _displayWheels = value;
                _displayHats = value;
                _displayShirts = value;
                _displayPants = value;
                _displayShoes = value;

                RaisePropertyChangedEventsForCategories();
                LazilyGetSelectedManifestsAndRefreshFilteredAssetList();
                UpdateAppSettingsWithSelectedCategories();
            }
        }

        public bool DisplayMaps
        {
            get { return _displayMaps; }
            set
            {
                if (_displayMaps != value)
                {
                    _displayMaps = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Maps);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMapsChecked, DisplayMaps.ToString());
                }
            }
        }

        public bool DisplayDecks
        {
            get { return _displayDecks; }
            set
            {
                if (_displayDecks != value)
                {
                    _displayDecks = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Decks);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreDecksChecked, DisplayDecks.ToString());
                }
            }
        }
        public bool DisplayGriptapes
        {
            get { return _displayGriptapes; }
            set
            {
                if (_displayGriptapes != value)
                {
                    _displayGriptapes = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Griptapes);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreGriptapesChecked, DisplayGriptapes.ToString());
                }
            }
        }
        public bool DisplayTrucks
        {
            get { return _displayTrucks; }
            set
            {
                if (_displayTrucks != value)
                {
                    _displayTrucks = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Trucks);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreTrucksChecked, DisplayTrucks.ToString());
                }
            }
        }
        public bool DisplayWheels
        {
            get { return _displayWheels; }
            set
            {
                if (_displayWheels != value)
                {
                    _displayWheels = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Wheels);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreWheelsChecked, DisplayWheels.ToString());
                }
            }
        }

        public bool DisplayHats
        {
            get { return _displayHats; }
            set
            {
                if (_displayHats != value)
                {
                    _displayHats = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Hats);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreHatsChecked, DisplayHats.ToString());
                }
            }
        }
        public bool DisplayShirts
        {
            get { return _displayShirts; }
            set
            {
                if (_displayShirts != value)
                {
                    _displayShirts = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Shirts);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShirtsChecked, DisplayShirts.ToString());
                }
            }
        }
        public bool DisplayPants
        {
            get { return _displayPants; }
            set
            {
                if (_displayPants != value)
                {
                    _displayPants = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Pants);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStorePantsChecked, DisplayPants.ToString());
                }
            }
        }
        public bool DisplayShoes
        {
            get { return _displayShoes; }
            set
            {
                if (_displayShoes != value)
                {
                    _displayShoes = value;
                    NotifyPropertyChanged();
                    LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory.Shoes);
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShoesChecked, DisplayShoes.ToString());
                }
            }
        }

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                Logger.Info($"UserMessage = {_userMessage}");
                NotifyPropertyChanged();
            }
        }

        public string InstallButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_installButtonText))
                    _installButtonText = "Install Asset";
                return _installButtonText;
            }
            set
            {
                _installButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallButtonEnabled
        {
            get
            {
                if (IsInstallingAsset)
                    return false;

                return _isInstallButtonEnabled;
            }
            set
            {
                _isInstallButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public string RemoveButtonText
        {
            get
            {
                if (String.IsNullOrEmpty(_removeButtonText))
                    _removeButtonText = "Remove Asset";
                return _removeButtonText;
            }
            set
            {
                _removeButtonText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsRemoveButtonEnabled
        {
            get
            {
                if (IsInstallingAsset)
                    return false;

                return _isRemoveButtonEnabled;
            }
            set
            {
                _isRemoveButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallingAsset
        {
            get
            {
                return _isInstallingAsset;
            }
            set
            {
                _isInstallingAsset = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsRemoveButtonEnabled));
                NotifyPropertyChanged(nameof(IsInstallButtonEnabled));
            }
        }

        public bool FetchAllPreviewImages
        {
            get { return _fetchAllPreviewImages; }
            set
            {
                if (_fetchAllPreviewImages != value)
                {
                    _fetchAllPreviewImages = value;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.FetchAllPreviewImages, _fetchAllPreviewImages.ToString());
                }

                if (_fetchAllPreviewImages)
                {
                    DownloadAllPreviewImagesAsync();
                }
            }
        }

        public bool DeleteDownloadAfterInstall
        {
            get { return _deleteDownloadAfterInstall; }
            set
            {
                if (_deleteDownloadAfterInstall != value)
                {
                    _deleteDownloadAfterInstall = value;
                    AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.DeleteDownloadAfterAssetInstall, _deleteDownloadAfterInstall.ToString());
                }
            }
        }


        public bool IsLoadingImage
        {
            get
            {
                return _isLoadingImage;
            }
            set
            {
                _isLoadingImage = value;
                NotifyPropertyChanged();
            }
        }


        public Stream PreviewImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedAuthor
        {
            get { return _selectedAuthor; }
            set
            {
                _selectedAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string SelectedDescription
        {
            get { return _selectedDescription; }
            set
            {
                _selectedDescription = value;
                NotifyPropertyChanged();
            }
        }

        public AuthorDropdownViewModel AuthorToFilterBy
        {
            get
            {
                if (_authorToFilterBy == null)
                    _authorToFilterBy = new AuthorDropdownViewModel(defaultAuthorValue, 0);

                return _authorToFilterBy;
            }
            set
            {
                if (_authorToFilterBy != value)
                {
                    _authorToFilterBy = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                }
            }
        }

        public string SelectedInstallStatus
        {
            get
            {
                if (String.IsNullOrEmpty(_selectedInstallStatus))
                    _selectedInstallStatus = "All";
                return _selectedInstallStatus;
            }
            set
            {
                if (_selectedInstallStatus != value)
                {
                    _selectedInstallStatus = value;
                    NotifyPropertyChanged();
                    RefreshFilteredAssetList();
                }
            }
        }


        public bool IsLoadingManifests
        {
            get { return _isLoadingManifests; }
            set
            {
                _isLoadingManifests = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotLoadingManifests));
            }
        }

        public bool IsNotLoadingManifests
        {
            get { return !_isLoadingManifests; }
        }

        public bool IsManifestsDownloaded { get; set; }

        public bool HasAuthenticated { get; set; }


        /// <summary>
        /// used to determine if list of available maps should be refreshed when switching back to the main window
        /// </summary>
        public bool HasDownloadedMap { get; set; }

        public StorageManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                    _assetManager = new StorageManager();

                return _assetManager;
            }
        }

        public List<AssetViewModel> AllAssets
        {
            get
            {
                if (_allAssets == null)
                    _allAssets = new List<AssetViewModel>();
                return _allAssets;
            }
            set
            {
                lock (allListLock)
                {
                    _allAssets = value;
                }
            }
        }

        public List<AssetViewModel> FilteredAssetList
        {
            get
            {
                if (_filteredAssetList == null)
                    _filteredAssetList = new List<AssetViewModel>();
                return _filteredAssetList;
            }
            set
            {
                lock (filteredListLock)
                {
                    _filteredAssetList = value;
                }
                NotifyPropertyChanged();
            }
        }

        public AssetViewModel SelectedAsset
        {
            get
            {
                lock (filteredListLock)
                {
                    if (FilteredAssetList.Where(a => a.IsSelected).Count() > 1)
                    {
                        FilteredAssetList.ForEach(a => a.IsSelected = false);
                    }

                    return FilteredAssetList.Where(a => a.IsSelected).FirstOrDefault();
                }
            }
        }

        public List<AuthorDropdownViewModel> AuthorList
        {
            get
            {
                if (_authorList == null)
                    _authorList = new List<AuthorDropdownViewModel>();

                return _authorList;
            }
            set
            {
                _authorList = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> InstallStatusList
        {
            get
            {
                if (_installStatusList == null)
                    _installStatusList = new List<string>();

                return _installStatusList;
            }
            set
            {
                _installStatusList = value;
                NotifyPropertyChanged();
            }
        }


        #endregion


        public AssetStoreViewModel()
        {
            IsManifestsDownloaded = false;
            IsLoadingManifests = false;
            IsInstallingAsset = false;
            HasAuthenticated = false;
            DisplayMaps = true;
            SetSelectedCategoriesFromAppSettings();
            FetchAllPreviewImages = AppSettingsUtil.GetAppSetting(SettingKey.FetchAllPreviewImages).Equals("true", StringComparison.OrdinalIgnoreCase);

            if (AppSettingsUtil.GetAppSetting(SettingKey.DeleteDownloadAfterAssetInstall) == "")
            {
                DeleteDownloadAfterInstall = true; // default to true if does not exist in app config
            }
            else
            {
                DeleteDownloadAfterInstall = AppSettingsUtil.GetAppSetting(SettingKey.DeleteDownloadAfterAssetInstall).Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            InstallStatusList = new List<string>() { defaultInstallStatusValue, "Installed", "Not Installed" };
            SelectedInstallStatus = defaultInstallStatusValue;

            AuthorList = new List<AuthorDropdownViewModel>() { new AuthorDropdownViewModel(defaultAuthorValue, 0) };
            AuthorToFilterBy = AuthorList[0];
        }

        private void LazilyGetSelectedManifestsAndRefreshFilteredAssetList()
        {
            var selectedCategories = GetSelectedCategories();
            List<AssetCategory> categoriesToDownload = selectedCategories.Where(c => ManifestFilesExists(c) == false).ToList();

            if (selectedCategories.Count == 0 || categoriesToDownload.Count == 0)
            {
                RefreshFilteredAssetList();
                return;
            }

            if (HasAuthenticated == false)
            {
                TryAuthenticate();
            }

            IsLoadingManifests = true;
            UserMessage = "Fetching latest asset manifests ...";

            var fileWriteTask = AssetManager.GetAssetManifestsAsync(categoriesToDownload, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"Downloading manifest {p.Key}: {p.TransferredBytes:0.00} / {p.TotalBytes:0.00} Bytes..."))
                                            .ContinueWith((taskResult) => ManifestDownloadCompleted(taskResult))
                                            .ConfigureAwait(false);
        }

        private void LazilyGetManifestsAndRefreshFilteredAssetList(AssetCategory category)
        {
            if (ManifestFilesExists(category))
            {
                //manifest files already downloaded so just refresh list
                RefreshFilteredAssetList();
                return;
            }

            if (HasAuthenticated == false)
            {
                TryAuthenticate();
            }

            IsLoadingManifests = true;
            UserMessage = "Fetching latest asset manifests ...";

            var fileWriteTask = AssetManager.GetAssetManifestsAsync(category, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"Downloading manifest {p.Key}: {p.TransferredBytes:0.00} / {p.TotalBytes:0.00} Bytes..."))
                                            .ContinueWith((taskResult) => ManifestDownloadCompleted(taskResult))
                                            .ConfigureAwait(false);
        }

        private bool ManifestFilesExists(AssetCategory category)
        {
            string pathToManifestFiles = Path.Combine(AbsolutePathToTempManifests, category.Value);

            return Directory.Exists(pathToManifestFiles) && Directory.GetFiles(pathToManifestFiles).Length > 0;
        }

        /// <summary>
        /// Downloads manifest files and refreshes <see cref="FilteredAssetList"/> after successful download.
        /// </summary>
        /// <param name="forceRefresh"> force download the manifests again</param>
        /// <param name="getSelectedOnly"> only download manifests for the selected asset categories </param>
        public void GetManifestsAsync(bool forceRefresh = false, bool getSelectedOnly = false)
        {
            if (forceRefresh == false && IsManifestsDownloaded)
            {
                return; // the manifests will not be re-downloaded because it has been downloaded once and not forced
            }

            if (HasAuthenticated == false)
            {
                TryAuthenticate();
            }

            IsLoadingManifests = true;
            UserMessage = "Fetching latest asset manifests ...";
            List<AssetCategory> categoriesToGet = new List<AssetCategory>();

            if (getSelectedOnly)
            {
                categoriesToGet = GetSelectedCategories();
            }
            else
            {
                categoriesToGet = AssetManager.GetAllCategories();
            }

            var fileTask = AssetManager.GetAssetManifestsAsync(categoriesToGet, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"Downloading manifest {p.Key}: {p.TransferredBytes:0.00} / {p.TotalBytes:0.00} Bytes..."))
                                       .ContinueWith((taskResult) => ManifestDownloadCompleted(taskResult))
                                       .ConfigureAwait(false);
        }

        private void ManifestDownloadCompleted(Task taskResult)
        {
            if (taskResult.IsFaulted)
            {
                UserMessage = "An error occurred fetching manifests ...";
                Logger.Error(taskResult.Exception, "failed to get all manifests");
            }
            else
            {
                IsManifestsDownloaded = true;
                UserMessage = "Manifests downloaded ...";
                RefreshFilteredAssetList(checkForFileChanges: true);

                if (FetchAllPreviewImages)
                {
                    DownloadAllPreviewImagesAsync();
                }
            }

            IsLoadingManifests = false;
        }

        public void RefreshFilteredAssetList(bool checkForFileChanges = false)
        {
            List<AssetCategory> categories = GetSelectedCategories();
            List<AssetViewModel> newList = new List<AssetViewModel>();

            foreach (AssetCategory cat in categories)
            {
                LoadAssetsFromManifest(cat, checkForFileChanges);
                newList.AddRange(GetAssetsByCategory(cat));
            }


            RefreshAuthorList();

            if (AuthorToFilterBy.Author != defaultAuthorValue)
            {
                newList = newList.Where(a => a.Author == AuthorToFilterBy.Author).ToList();
            }


            if (SelectedInstallStatus != defaultInstallStatusValue)
            {
                // read currently installed textures/map from files into memory so checking each asset is quicker
                InstalledTexturesMetaData installedTextures = MetaDataManager.LoadTextureMetaData();
                List<MapMetaData> installedMaps = MetaDataManager.GetAllMetaDataForMaps();

                switch (SelectedInstallStatus)
                {
                    case "Installed":
                        newList = newList.Where(a => IsAssetInstalled(a, installedMaps, installedTextures)).ToList();
                        break;

                    case "Not Installed":
                        newList = newList.Where(a => IsAssetInstalled(a, installedMaps, installedTextures) == false).ToList();
                        break;

                    default:
                        break;
                }
            }


            FilteredAssetList = newList;

            if (FilteredAssetList.Count == 0 && GetSelectedCategories().Count() == 0)
            {
                UserMessage = "Check categories to view the list of downloadable assets ...";
            }
        }

        /// <summary>
        /// Updates <see cref="AuthorList"/> with distinct sorted list of authors in <see cref="AllAssets"/>
        /// </summary>
        private void RefreshAuthorList()
        {
            List<AuthorDropdownViewModel> newAuthorList = new List<AuthorDropdownViewModel>();

            // use GroupBy to get count of assets per author
            foreach (IGrouping<string, AssetViewModel> author in AllAssets.GroupBy(a => a.Author))
            {
                newAuthorList.Add(new AuthorDropdownViewModel(author.Key, author.Count()));
            }

            newAuthorList = newAuthorList.OrderBy(a => a.Author).ToList();

            newAuthorList.Insert(0, new AuthorDropdownViewModel(defaultAuthorValue, 0));

            AuthorList = newAuthorList;

            //clear selection if selected author not in list
            if (AuthorList.Any(a => a.Author == AuthorToFilterBy.Author) == false)
            {
                AuthorToFilterBy = AuthorList[0];
            }
        }

        public void RefreshPreviewForSelected()
        {
            SelectedAuthor = SelectedAsset?.Author;
            SelectedDescription = SelectedAsset?.Description;

            bool isInstalled = IsSelectedAssetInstalled();

            IsInstallButtonEnabled = !isInstalled;
            IsRemoveButtonEnabled = isInstalled;

            RefreshInstallButtonText();
            GetSelectedPreviewImageAsync();
        }

        private bool IsSelectedAssetInstalled()
        {
            if (SelectedAsset == null)
            {
                return false;
            }

            // pass in null to read from json files to check if asset is installed
            return IsAssetInstalled(SelectedAsset, null, null);
        }

        private bool IsAssetInstalled(AssetViewModel asset, List<MapMetaData> mapMetaData, InstalledTexturesMetaData installedTextures)
        {
            if (asset.AssetCategory == AssetCategory.Maps.Value)
            {
                if (mapMetaData == null)
                {
                    mapMetaData = MetaDataManager.GetAllMetaDataForMaps();
                }

                return mapMetaData.Any(m => m.AssetName == asset.Asset.AssetName);
            }
            else
            {
                return MetaDataManager.GetTextureMetaDataByName(asset.Asset.AssetName, installedTextures) != null;
            }
        }

        /// <summary>
        /// Returns list of AssetViewModels by category that exist in <see cref="AllAssets"/>
        /// </summary>
        private IEnumerable<AssetViewModel> GetAssetsByCategory(AssetCategory cat)
        {
            IEnumerable<AssetViewModel> assetsInCategory = new List<AssetViewModel>();

            lock (allListLock)
            {
                assetsInCategory = AllAssets.Where(a => a.AssetCategory == cat.Value);
            }

            return assetsInCategory;
        }

        /// <summary>
        /// Returns list of <see cref="AssetCategory"/> that are set to true to display
        /// by checking <see cref="DisplayDecks"/>, <see cref="DisplayGriptapes"/>, etc.
        /// </summary>
        private List<AssetCategory> GetSelectedCategories()
        {
            List<AssetCategory> selectedCategories = new List<AssetCategory>();

            if (DisplayDecks)
            {
                selectedCategories.Add(AssetCategory.Decks);
            }
            if (DisplayGriptapes)
            {
                selectedCategories.Add(AssetCategory.Griptapes);
            }
            if (DisplayHats)
            {
                selectedCategories.Add(AssetCategory.Hats);
            }
            if (DisplayMaps)
            {
                selectedCategories.Add(AssetCategory.Maps);
            }
            if (DisplayPants)
            {
                selectedCategories.Add(AssetCategory.Pants);
            }
            if (DisplayShirts)
            {
                selectedCategories.Add(AssetCategory.Shirts);
            }
            if (DisplayShoes)
            {
                selectedCategories.Add(AssetCategory.Shoes);
            }
            if (DisplayTrucks)
            {
                selectedCategories.Add(AssetCategory.Trucks);
            }
            if (DisplayWheels)
            {
                selectedCategories.Add(AssetCategory.Wheels);
            }

            return selectedCategories;
        }

        /// <summary>
        /// Loads assets from the manifest files for a specific category into <see cref="AllAssets"/>.
        /// Assets will be reloaded if there are new manifest files or changes to the manifest files.
        /// </summary>
        private void LoadAssetsFromManifest(AssetCategory category, bool checkForFileChanges)
        {
            bool hasFileChanges = false;

            if (checkForFileChanges)
            {
                hasFileChanges = HasNewAssetManifests(category) || HasAssetManifestsChanged(category);
            }

            if (IsAssetsLoaded(category) == false || hasFileChanges)
            {
                List<Asset> assets = new List<Asset>();

                lock (manifestFileLock)
                {
                    assets = AssetManager.GenerateAssets(category);
                }

                // remove existing assets to avoid duplicates or stale data
                lock (allListLock)
                {
                    AllAssets.RemoveAll((a) => a.AssetCategory == category.Value);
                }

                // add new assets loaded generated from manifest files
                foreach (Asset asset in assets)
                {
                    if (asset == null)
                        continue;

                    lock (allListLock)
                    {
                        AllAssets.Add(new AssetViewModel(asset));
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if there exists more manifest files for a category versus the count 
        /// of assets in <see cref="AllAssets"/>
        /// </summary>
        private bool HasNewAssetManifests(AssetCategory category)
        {
            string pathToManifests = Path.Combine(AbsolutePathToTempManifests, category.Value);

            if (Directory.Exists(pathToManifests) == false)
            {
                return false;
            }

            int manifestFileCount = 0;

            lock (manifestFileLock)
            {
                manifestFileCount = Directory.GetFiles(pathToManifests).Length;
            }

            int assetInMemoryCount = GetAssetsByCategory(category).Count();

            return assetInMemoryCount != manifestFileCount;
        }

        /// <summary>
        /// Checks if the in-memory asset object has different values than the manifest file
        /// </summary>
        private bool HasAssetManifestsChanged(AssetCategory category)
        {
            string pathToManifests = Path.Combine(AbsolutePathToTempManifests, category.Value);

            if (Directory.Exists(pathToManifests) == false)
            {
                return false;
            }

            string[] manifestFiles = { };

            try
            {
                lock (manifestFileLock)
                {
                    manifestFiles = Directory.GetFiles(pathToManifests);
                }

                List<AssetViewModel> inMemoryAssets = GetAssetsByCategory(category).ToList();


                foreach (string file in manifestFiles)
                {
                    string fileContents;

                    lock (manifestFileLock)
                    {
                        fileContents = File.ReadAllText(file);
                    }

                    Asset assetFromFile = JsonConvert.DeserializeObject<Asset>(fileContents);

                    Asset assetFromMemory = inMemoryAssets.Where(a => a.Asset.AssetName == assetFromFile.AssetName).Select(a => a.Asset).FirstOrDefault();

                    if (assetFromMemory == null)
                    {
                        return true; // the asset does not exist in memory which is a change so return true
                    }

                    bool hasChanged = (assetFromMemory.Author != assetFromFile.Author ||
                                       assetFromMemory.Category != assetFromFile.Category ||
                                       assetFromMemory.Description != assetFromFile.Description ||
                                       assetFromMemory.Name != assetFromFile.Name ||
                                       assetFromMemory.Thumbnail != assetFromFile.Thumbnail);

                    if (hasChanged)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to check if assets changed from manifest");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there are ANY assets in <see cref="AllAssets"/> based on the given category
        /// </summary>
        private bool IsAssetsLoaded(AssetCategory category)
        {
            bool hasAssets = false;

            lock (allListLock)
            {
                hasAssets = AllAssets.Any(a => a.AssetCategory == category.Value);
            }

            return hasAssets;
        }

        public void TryAuthenticate()
        {
            try
            {
                AssetManager.Authenticate();
                HasAuthenticated = true;
            }
            catch (AggregateException e)
            {
                UserMessage = $"Failed to authenticate to asset store: {e.InnerException?.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to authenticate to asset store: {e.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
        }

        private void GetSelectedPreviewImageAsync()
        {
            if (SelectedAsset == null)
            {
                return;
            }

            IsLoadingImage = true;
            UserMessage = "Fetching preview image ...";

            Task t = Task.Factory.StartNew(() =>
            {
                CreateRequiredFolders();

                string pathToThumbnail = Path.Combine(AbsolutePathToThumbnails, SelectedAsset.Asset.Thumbnail);

                if (File.Exists(pathToThumbnail) == false)
                {
                    System.Runtime.CompilerServices.ConfiguredTaskAwaitable downloadTask = AssetManager.DownloadAssetThumbnailAsync(SelectedAsset.Asset, pathToThumbnail, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"Fetching preview image: {p.TransferredBytes / 1000:0.00} / {p.TotalBytes / 1000:0.00} KB..."), true).ConfigureAwait(false);
                    downloadTask.GetAwaiter().GetResult();
                }
                else
                {
                    UserMessage = "";
                }

                if (PreviewImageSource != null)
                {
                    PreviewImageSource.Close();
                    PreviewImageSource = null;
                }

                using (FileStream stream = File.Open(pathToThumbnail, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    PreviewImageSource = new MemoryStream();
                    stream.CopyTo(PreviewImageSource);
                }

                PreviewImageSource = new MemoryStream(File.ReadAllBytes(pathToThumbnail));
            });

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    UserMessage = "Failed to get preview image.";
                    PreviewImageSource = null;
                    Logger.Error(taskResult.Exception);
                }

                IsLoadingImage = false;
            });

        }

        private void DownloadAllPreviewImagesAsync()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                CreateRequiredFolders();

                if (HasAuthenticated == false)
                {
                    TryAuthenticate();
                }

                foreach (AssetViewModel asset in AllAssets.ToList())
                {
                    string pathToThumbnail = Path.Combine(AbsolutePathToThumbnails, asset.Asset.Thumbnail);

                    if (File.Exists(pathToThumbnail) == false)
                    {
                        System.Runtime.CompilerServices.ConfiguredTaskAwaitable downloadTask = AssetManager.DownloadAssetThumbnailAsync(asset.Asset, pathToThumbnail, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"fetching preview image: {p.TransferredBytes / 1000:0.00} / {p.TotalBytes / 1000:0.00} KB..."), true).ConfigureAwait(false);
                        downloadTask.GetAwaiter().GetResult();
                    }
                }
            });
        }

        public void RefreshInstallButtonText()
        {
            if (SelectedAsset == null)
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
                return;
            }

            string assetCatName = SelectedAsset.AssetCategory;

            Dictionary<string, string> categoryToText = new Dictionary<string, string>();
            categoryToText.Add(AssetCategory.Decks.Value, "Deck");
            categoryToText.Add(AssetCategory.Griptapes.Value, "Griptape");
            categoryToText.Add(AssetCategory.Hats.Value, "Hat");
            categoryToText.Add(AssetCategory.Maps.Value, "Map");
            categoryToText.Add(AssetCategory.Pants.Value, "Pants");
            categoryToText.Add(AssetCategory.Shirts.Value, "Shirt");
            categoryToText.Add(AssetCategory.Shoes.Value, "Shoes");
            categoryToText.Add(AssetCategory.Trucks.Value, "Trucks");
            categoryToText.Add(AssetCategory.Wheels.Value, "Wheels");

            if (categoryToText.ContainsKey(assetCatName))
            {
                InstallButtonText = $"Install {categoryToText[assetCatName]}";
                RemoveButtonText = $"Remove {categoryToText[assetCatName]}";
            }
            else
            {
                InstallButtonText = "Install Asset";
                RemoveButtonText = "Remove Asset";
            }
        }

        /// <summary>
        /// Main method for downloading and installing the selected asset asynchronously.
        /// </summary>
        public void DownloadSelectedAssetAsync()
        {
            CreateRequiredFolders();

            IsInstallingAsset = true;
            AssetViewModel assetToDownload = SelectedAsset; // get the selected asset currently in-case user selection changes while download occurs
            string pathToDownload = Path.Combine(AbsolutePathToTempDownloads, assetToDownload.Asset.AssetName);


            Task downloadTask = AssetManager.DownloadAssetAsync(assetToDownload.Asset, pathToDownload, new EventHandler<WriteObjectProgressArgs>((o, p) => UserMessage = $"Downloading {assetToDownload.Name}: {p.TransferredBytes / 1000000:0.00} / {p.TotalBytes / 1000000:0.00} MB | {p.PercentDone}%..."), true);

            downloadTask.ContinueWith((result) =>
            {
                if (result.IsFaulted)
                {
                    UserMessage = $"Failed to install asset ...";
                    Logger.Error(result.Exception);
                    IsInstallingAsset = false;
                    return;
                }

                UserMessage = $"Installing asset: {assetToDownload.Name} ... ";
                Task installTask = Task.Factory.StartNew(() =>
                {
                    InstallDownloadedAsset(assetToDownload, pathToDownload);
                });

                installTask.ContinueWith((installResult) =>
                {
                    if (installResult.IsFaulted)
                    {
                        UserMessage = $"Failed to install asset ...";
                        Logger.Error(result.Exception);
                        IsInstallingAsset = false;
                        return;
                    }

                    // lastly delete downloaded file
                    if (DeleteDownloadAfterInstall && File.Exists(pathToDownload))
                    {
                        File.Delete(pathToDownload);
                    }

                    IsInstallingAsset = false;

                    RefreshPreviewForSelected();

                    // refresh list if filtering by installed or uninstalled
                    if (SelectedInstallStatus != defaultInstallStatusValue)
                    {
                        RefreshFilteredAssetList();
                    }

                    if (assetToDownload.AssetCategory == AssetCategory.Maps.Value)
                    {
                        HasDownloadedMap = true;
                    }

                });
            });
        }

        /// <summary>
        /// Logic for determining how to install downloaded asset. (maps and textures are installed differently)
        /// </summary>
        /// <param name="assetToInstall"> asset being installed </param>
        /// <param name="pathToDownload"> absolute path to the downloaded asset file </param>
        private void InstallDownloadedAsset(AssetViewModel assetToInstall, string pathToDownload)
        {
            if (assetToInstall.AssetCategory == AssetCategory.Maps.Value)
            {
                // import map
                ComputerImportViewModel importViewModel = new ComputerImportViewModel()
                {
                    IsZipFileImport = true,
                    PathInput = pathToDownload,
                    AssetToImport = assetToInstall.Asset
                };
                Task<BoolWithMessage> importTask = importViewModel.ImportMapAsync();
                importTask.Wait();

                if (importTask.Result.Result)
                {
                    UserMessage = $"Successfully installed {assetToInstall.Name}!";
                }
                else
                {
                    UserMessage = $"Failed to install {assetToInstall.Name}: {importTask.Result.Message}";
                    Logger.Warn($"install failed: {importTask.Result.Message}");
                }
            }
            else
            {
                // replace texture
                TextureReplacerViewModel replacerViewModel = new TextureReplacerViewModel()
                {
                    PathToFile = pathToDownload,
                    AssetToInstall = assetToInstall.Asset
                };
                replacerViewModel.MessageChanged += TextureReplacerViewModel_MessageChanged;
                replacerViewModel.ReplaceTextures();
                replacerViewModel.MessageChanged -= TextureReplacerViewModel_MessageChanged;
            }
        }

        private void TextureReplacerViewModel_MessageChanged(string message)
        {
            UserMessage = message;
        }

        public void CreateRequiredFolders()
        {
            Directory.CreateDirectory(AbsolutePathToStoreData);
            Directory.CreateDirectory(AbsolutePathToThumbnails);
            Directory.CreateDirectory(AbsolutePathToTempDownloads);
        }

        /// <summary>
        /// Deletes the selected asset files from Session folders
        /// </summary>
        public void RemoveSelectedAsset()
        {
            AssetViewModel assetToRemove = SelectedAsset;
            BoolWithMessage deleteResult = BoolWithMessage.False("");

            if (assetToRemove.AssetCategory == AssetCategory.Maps.Value)
            {
                MapMetaData mapToDelete = MetaDataManager.GetAllMetaDataForMaps()?.Where(m => m.AssetName == assetToRemove.Asset.AssetName).FirstOrDefault();

                if (mapToDelete == null)
                {
                    UserMessage = "Failed to find meta data to delete map files ...";
                    return;
                }

                deleteResult = MetaDataManager.DeleteMapFiles(mapToDelete);
            }
            else
            {
                TextureMetaData textureToDelete = MetaDataManager.GetTextureMetaDataByName(assetToRemove.Asset.AssetName);

                if (textureToDelete == null)
                {
                    UserMessage = $"Failed to find meta data to delete texture files for {assetToRemove.Asset.AssetName}...";
                    return;
                }

                deleteResult = MetaDataManager.DeleteTextureFiles(textureToDelete);
            }


            UserMessage = deleteResult.Message;

            if (deleteResult.Result)
            {
                RefreshPreviewForSelected();

                // refresh list if filtering by installed or uninstalled
                if (SelectedInstallStatus != defaultInstallStatusValue)
                {
                    RefreshFilteredAssetList();
                }
            }
        }

        /// <summary>
        /// deletes the asset from the asset store if user has rights to asset
        /// </summary>
        public void DeleteSelectedAssetFromAssetStore()
        {
            AssetViewModel assetToDelete = SelectedAsset;

            if (assetToDelete == null)
            {
                UserMessage = "Cannot delete, selected asset is null.";
                return;
            }

            string manifestFileName = assetToDelete.Asset.AssetName.Replace(".zip", ".json");
            manifestFileName = manifestFileName.Replace(".rar", ".json");

            try
            {
                // use UploadAssetViewModel because the StorageManager will authenticate using uploader credentials instead of standard read-only credentials
                UploadAssetViewModel uploadViewModel = new UploadAssetViewModel();
                uploadViewModel.TryAuthenticate();

                if (uploadViewModel.HasAuthenticated)
                {
                    string selectedBucket = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreSelectedBucket);

                    if (String.IsNullOrEmpty(selectedBucket))
                    {
                        UserMessage = "Failed to delete. Click 'Upload Asset' and set the Bucket before deleting your assets.";
                        return;
                    }

                    uploadViewModel.AssetManager.DeleteAsset(selectedBucket, manifestFileName, assetToDelete.Asset);
                    UserMessage = $"Successfully deleted {assetToDelete.Name} from Asset Store!";
                    GetManifestsAsync(forceRefresh: true, getSelectedOnly: true);
                }
                else
                {
                    UserMessage = "Failed to authenticate credentials. Click 'Upload Asset' to set uploader credentials before deleting.";
                }
            }
            catch (AggregateException e)
            {
                UserMessage = $"An error occurred deleting the asset from the store: {e.InnerException?.Message}";
                Logger.Error(e, "Failed to delete asset from storage");
                return;
            }
            catch (Exception e)
            {
                UserMessage = $"An error occurred deleting the asset from the store: {e.Message}";
                Logger.Error(e, "Failed to delete asset from storage");
                return;
            }
        }

        private void UpdateAppSettingsWithSelectedCategories()
        {
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreDecksChecked, DisplayDecks.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreGriptapesChecked, DisplayGriptapes.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreHatsChecked, DisplayHats.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreMapsChecked, DisplayMaps.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStorePantsChecked, DisplayPants.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShirtsChecked, DisplayShirts.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreShoesChecked, DisplayShoes.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreTrucksChecked, DisplayTrucks.ToString());
            AppSettingsUtil.AddOrUpdateAppSettings(SettingKey.AssetStoreWheelsChecked, DisplayWheels.ToString());
        }

        private void SetSelectedCategoriesFromAppSettings()
        {
            _displayDecks = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreDecksChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayGriptapes = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreGriptapesChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayHats = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreHatsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayMaps = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreMapsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayPants = AppSettingsUtil.GetAppSetting(SettingKey.AssetStorePantsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayShirts = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreShirtsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayShoes = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreShoesChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayTrucks = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreTrucksChecked).Equals("true", StringComparison.OrdinalIgnoreCase);
            _displayWheels = AppSettingsUtil.GetAppSetting(SettingKey.AssetStoreWheelsChecked).Equals("true", StringComparison.OrdinalIgnoreCase);

            _displayAll = DisplayDecks && DisplayGriptapes && DisplayHats && DisplayMaps && DisplayPants && DisplayShirts && DisplayShoes && DisplayTrucks && DisplayWheels;

            RaisePropertyChangedEventsForCategories();
            LazilyGetSelectedManifestsAndRefreshFilteredAssetList();
        }

        private void RaisePropertyChangedEventsForCategories()
        {
            NotifyPropertyChanged(nameof(DisplayAll));
            NotifyPropertyChanged(nameof(DisplayMaps));
            NotifyPropertyChanged(nameof(DisplayDecks));
            NotifyPropertyChanged(nameof(DisplayGriptapes));
            NotifyPropertyChanged(nameof(DisplayTrucks));
            NotifyPropertyChanged(nameof(DisplayWheels));
            NotifyPropertyChanged(nameof(DisplayHats));
            NotifyPropertyChanged(nameof(DisplayShirts));
            NotifyPropertyChanged(nameof(DisplayPants));
            NotifyPropertyChanged(nameof(DisplayShoes));
        }

        public List<string> GetAvailableBuckets()
        {
            if (HasAuthenticated == false)
            {
                return new List<string>();
            }

            try
            {
                var t = AssetManager.ListBucketsAsync().ConfigureAwait(false);
                return t.GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Logger.Warn(e, "failed to get buckets");
                return new List<string>();
            }
        }
    }
}
