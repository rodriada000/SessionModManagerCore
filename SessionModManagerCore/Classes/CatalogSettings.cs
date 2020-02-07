using Newtonsoft.Json;
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
                    Name = "SMM Default Catalog",
                    Url = _defaultCatalogUrl
                });
                addedDefaults = true;
            }

            if (addedDefaults)
            {
                string contents = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(AssetStoreViewModel.AbsolutePathToCatalogSettingsJson, contents);
            }
        }
    }
}
