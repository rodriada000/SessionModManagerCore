using System;
using System.Collections.Generic;
using System.Text;
using SessionAssetStore;

namespace SessionModManagerCore.Classes
{
    public class TextureMetaData
    {
        /// <summary>
        /// List of absolute paths to files that were copied for the texture
        /// </summary>
        public List<string> FilePaths { get; set; }

        /// <summary>
        /// Name of the asset file that this texture file came from
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Display name of the asset from the asset store
        /// </summary>
        public string Name { get; set; }

        public TextureMetaData()
        {
            FilePaths = new List<string>();
            AssetName = "";
            Name = "";
        }

        public TextureMetaData(Asset assetToInstall)
        {
            FilePaths = new List<string>();

            if (assetToInstall == null)
            {
                return;
            }

            AssetName = assetToInstall.AssetName;
            Name = assetToInstall.Name;
        }
    }
}
