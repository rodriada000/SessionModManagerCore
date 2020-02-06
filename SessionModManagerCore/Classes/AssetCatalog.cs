using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.Classes
{
    public enum DownloadLocationType
    {
        INVALID,
        Url,
        MegaSharedFolder, //Format: SharedFolderLink,FileIDString,HintFileName
        GDrive,
    }

    public class AssetCatalog
    {
        public List<Asset> Assets { get; set; }

        private Dictionary<string, Asset> _lookup;

        public Asset GetAsset(string assetID)
        {
            if (_lookup == null)
            {
                _lookup = Assets.ToDictionary(m => m.ID, m => m);
            }

            Asset mod;
            _lookup.TryGetValue(assetID, out mod);

            return mod;
        }

        public static AssetCatalog Merge(AssetCatalog c1, AssetCatalog c2)
        {
            Dictionary<string, Asset> assets = c1.Assets.ToDictionary(m => m.ID, m => m);

            foreach (var otherAsset in c2.Assets)
            {
                Asset m;
                if (assets.TryGetValue(otherAsset.ID, out m))
                {
                    if (otherAsset.Version > m.Version)
                    {
                        assets[otherAsset.ID] = otherAsset;
                    }
                }
                else
                {
                    assets[otherAsset.ID] = otherAsset;
                }
            }

            return new AssetCatalog() { Assets = assets.Values.ToList() };
        }

        public static bool TryParseDownloadUrl(string link, out DownloadLocationType type, out string url)
        {
            if (link.StartsWith("rsmm://", StringComparison.InvariantCultureIgnoreCase)) link = link.Substring(7);
            string[] parts = link.Split(new[] { '/' }, 2);
            type = DownloadLocationType.INVALID; url = null;

            if (parts.Length < 2) return false;
            if (!Enum.TryParse(parts[0], out type)) return false;

            url = parts[1];
            int dpos = url.IndexOf('$');
            if (dpos >= 0) url = url.Substring(0, dpos) + "://" + url.Substring(dpos + 1);
            return true;
        }
    }
}
