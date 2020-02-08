using Newtonsoft.Json;
using SessionMapSwitcherCore.Utils;
using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public class CatalogSettings
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string _defaultCatalogUrl = "https://raw.githubusercontent.com/rodriada000/SessionCustomMapReleases/master/DefaultSMMCatalog.json";
        public List<CatalogSubscription> CatalogUrls { get; set; }

        public CatalogSettings()
        {
            CatalogUrls = new List<CatalogSubscription>();
        }

        /// <summary>
        /// Ensures all default catalog urls are in the users catalog settings
        /// </summary>
        internal static void AddDefaults(CatalogSettings settings)
        {
            bool addedDefaults = false;

            if (!settings.CatalogUrls.Any(c => c.Url == _defaultCatalogUrl))
            {
                settings.CatalogUrls.Add(new CatalogSubscription()
                {
                    Name = GetNameFromAssetCatalog(_defaultCatalogUrl),
                    Url = _defaultCatalogUrl
                });
                addedDefaults = true;
            }

            string redGoufDefaultCatalog = "https://pastebin.com/raw/AEyARZAM";
            if (!settings.CatalogUrls.Any(c => c.Url == redGoufDefaultCatalog))
            {
                settings.CatalogUrls.Add(new CatalogSubscription()
                {
                    Name = GetNameFromAssetCatalog(redGoufDefaultCatalog),
                    Url = redGoufDefaultCatalog
                });
                addedDefaults = true;
            }

            if (addedDefaults)
            {
                string contents = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
            }
        }

        internal static string GetNameFromAssetCatalog(string url)
        {
            string name = "";

            try
            {
                string catalogStr = DownloadUtils.GetTextResponseFromUrl(url, 5);
                AssetCatalog newCatalog = JsonConvert.DeserializeObject<AssetCatalog>(catalogStr);
                name = newCatalog.Name ?? "";
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Logger.Warn($"Failed to get catalog name from url {url}");
            }

            return name;
        }
    }
}
