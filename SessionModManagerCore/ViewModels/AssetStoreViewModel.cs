using SessionAssetStore;
using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.Utils;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SessionMapSwitcherCore.ViewModels
{
    public class AssetStoreViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private StorageManager _assetManager;
        private string _userMessage;
        private string _installButtonText;
        private string _imageSource;
        private string _selectedDescription;
        private string _selectedAuthor;
        private bool _isLoadingManifests;
        

        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                NotifyPropertyChanged();
            }
        }

        public string Author
        {
            get { return _selectedAuthor; }
            set
            {
                _selectedAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string Description
        {
            get { return _selectedDescription; }
            set
            {
                _selectedDescription = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsLoadingManifests
        {
            get { return _isLoadingManifests; }
            set
            {
                _isLoadingManifests = value;
                NotifyPropertyChanged();
            }
        }


        public StorageManager AssetManager
        {
            get
            {
                if (_assetManager == null)
                    _assetManager = new StorageManager();

                return _assetManager;
            }
        }

        public AssetStoreViewModel()
        {
            TryAuthenticate();
            GetAllManifestsAsync();
        }

        private void GetAllManifestsAsync()
        {
            IsLoadingManifests = true;
            UserMessage = "Fetching latest asset manifests ...";

            Task t = Task.Factory.StartNew(() => AssetManager.GetAllAssetManifests());

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    UserMessage = "An error occurred fetching manifests ...";
                    Logger.Error(taskResult.Exception, "failed to get all manifests");
                }

                IsLoadingManifests = false;
            });
        }

        private void TryAuthenticate()
        {
            try
            {
                AssetManager.Authenticate();
            }
            catch (Exception e)
            {
                UserMessage = $"Failed to authenticate to asset store: {e.Message}";
                Logger.Error(e, "Failed to authenticate to asset store");
            }
        }
    }
}
