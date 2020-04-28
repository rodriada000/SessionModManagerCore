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
            List<string> defaultCatalogs = new List<string>()
            {
                _defaultCatalogUrl,
                "https://pastebin.com/raw/AEyARZAM", // - redgouf catalog
                "https://pastebin.com/raw/D4e1dfZ6", // - Wattie's catalog
                "https://pastebin.com/raw/FLa6yWDB", // - Rume's catalog
                "https://pastebin.com/raw/L9HMs5mu", // - san van community center 
                "https://pastebin.com/raw/GWsWTZA8", // - Spargo808 catalog
                "https://pastebin.com/raw/XFgc8PTN", // - Onkel catalog
                "https://pastebin.com/raw/kTU8BUDM", // - SLS London by dga
                "https://pastebin.com/raw/n4jXqiMC", // - ReDann22's catalog
                "https://pastebin.com/raw/KX9cKt1M", // - GHFear's catalog
                "https://pastebin.com/raw/ib6Bbqdp", // - Dizzy's catalog
                "https://pastebin.com/raw/ge1BLWrh", // - Dizzy Skateboarding Co. catalog
                "https://pastebin.com/raw/9xdZpnQk", // - Otter's catalog
                "https://pastebin.com/raw/eEp2SKKf", // - JammieDodgers catalog
                "https://pastebin.com/raw/Gs7HVBNA", // - haens_daempf catalog
                "https://pastebin.com/raw/hPFif3cf", // - Colyns catalog
                "https://pastebin.com/raw/aFeLjau8", // - FlyRCs catalog
            };

            foreach (string url in defaultCatalogs)
            {
                if (!settings.CatalogUrls.Any(c => c.Url == url))
                {
                    settings.CatalogUrls.Add(new CatalogSubscription()
                    {
                        Name = GetNameFromAssetCatalog(url),
                        Url = url,
                    });
                    addedDefaults = true;
                }
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
