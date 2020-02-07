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
                CatalogSettings currentSettings = JsonConvert.DeserializeObject<CatalogSettings>(fileContents);

                CatalogList = currentSettings.CatalogUrls.Select(c => new CatalogSubscriptionViewModel(c.Url, c.Name)).ToList();
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

            string name = "";

            try
            {
                string catalogStr = DownloadUtils.GetTextResponseFromUrl(newUrl, 5);
                AssetCatalog newCatalog = JsonConvert.DeserializeObject<AssetCatalog>(catalogStr);
                name = newCatalog.Name ?? "";
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Warn($"Failed to get catalog name from url {newUrl}");
            }

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
                    Url = c.Url
                }).ToList()
            };

            Directory.CreateDirectory(AssetStoreViewModel.AbsolutePathToStoreData);

            string contents = JsonConvert.SerializeObject(updatedSettings, Formatting.Indented);
            File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
        }
    }

    public class CatalogSubscriptionViewModel : ViewModelBase
    {
        private string _url;
        private string _name;

        public CatalogSubscriptionViewModel(string url, string name)
        {
            Url = url;
            Name = name;
        }

        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                NotifyPropertyChanged();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }
    }
}
