using Newtonsoft.Json;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SessionModManagerCore.ViewModels
{
    public class ManageCatalogViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool _isAdding;

        private List<CatalogSubscriptionViewModel> _catalogList;
        private string _newUrlText;

        public string NewUrlText
        {
            get { return _newUrlText; }
            set
            {
                _newUrlText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsAdding
        {
            get { return _isAdding; }
            set
            {
                _isAdding = value;
                NotifyPropertyChanged();
            }
        }

        public List<CatalogSubscriptionViewModel> CatalogList
        {
            get
            {
                if (_catalogList == null)
                    _catalogList = new List<CatalogSubscriptionViewModel>();

                return _catalogList;
            }
            set
            {
                _catalogList = value;
                NotifyPropertyChanged();
            }
        }

        public ManageCatalogViewModel()
        {
            NewUrlText = "";
            IsAdding = false;
            ReloadCatalogList();
        }

        private void ReloadCatalogList()
        {
            CatalogList = new List<CatalogSubscriptionViewModel>();

            if (File.Exists(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson))
            {
                string fileContents = File.ReadAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson);

                try
                {
                    CatalogSettings currentSettings = JsonConvert.DeserializeObject<CatalogSettings>(fileContents);
                    CatalogList = currentSettings.CatalogUrls.Select(c => new CatalogSubscriptionViewModel(c)).ToList();
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
        }

        public void TrySaveCatalog()
        {
            try
            {
                WriteToFile();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void ToggleActivationForAll(bool isActive)
        {
            foreach (var cat in CatalogList)
            {
                cat.IsActive = isActive;
            }
        }

        public void AddUrl(string newUrl)
        {
            if (CatalogList.Any(c => c.Url.Equals(newUrl, StringComparison.InvariantCultureIgnoreCase)))
            {
                return; // duplicate url
            }

            if (string.IsNullOrWhiteSpace(newUrl))
            {
                return;
            }
            string name = CatalogSettings.GetNameFromAssetCatalog(newUrl);

            CatalogList.Add(new CatalogSubscriptionViewModel(newUrl, name));

            WriteToFile();

            ReloadCatalogList();
        }

        public void RemoveUrl(CatalogSubscriptionViewModel url)
        {
            bool didRemove = CatalogList.Remove(url);

            if (didRemove)
            {
                WriteToFile();
                ReloadCatalogList();
            }
        }

        private void WriteToFile()
        {
            CatalogSettings updatedSettings = new CatalogSettings()
            {
                CatalogUrls = CatalogList.Select(c => new CatalogSubscription()
                {
                    Name = c.Name,
                    Url = c.Url,
                    IsActive = c.IsActive
                }).ToList()
            };

            Directory.CreateDirectory(AssetStoreViewModel.AbsolutePathToStoreData);

            string contents = JsonConvert.SerializeObject(updatedSettings, Formatting.Indented);
            File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
        }
    }
}
