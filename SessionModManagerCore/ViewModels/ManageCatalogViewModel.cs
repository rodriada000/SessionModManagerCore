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

        private List<string> _catalogList;
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

        public List<string> CatalogList
        {
            get
            {
                if (_catalogList == null)
                    _catalogList = new List<string>();

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
            CatalogList = new List<string>();

            if (File.Exists(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson))
            {
                string fileContents = File.ReadAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson);
                CatalogList = JsonConvert.DeserializeObject<CatalogSettings>(fileContents).CatalogUrls;
            }
        }

        public void AddUrl(string newUrl)
        {
            if (CatalogList.Any(c => c.Equals(newUrl, StringComparison.InvariantCultureIgnoreCase)))
            {
                return; // duplicate url
            }

            CatalogList.Add(newUrl);

            WriteToFile();

            ReloadCatalogList();
        }

        public void RemoveUrl(string url)
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
                CatalogUrls = CatalogList
            };

            Directory.CreateDirectory(AssetStoreViewModel.AbsolutePathToStoreData);

            string contents = JsonConvert.SerializeObject(updatedSettings);
            File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
        }
    }
}
