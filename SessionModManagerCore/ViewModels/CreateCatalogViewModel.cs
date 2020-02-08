using Newtonsoft.Json;
using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class CreateCatalogViewModel : ViewModelBase
    {
        private string _selectedAssetName;
        private string _selectedAssetAuthor;
        private string _selectedAssetDescription;
        private string _selectedAssetCategory;
        private string _selectedAssetUpdatedDate;
        private string _selectedAssetVersion;
        private string _selectedAssetID;
        private string _selectedAssetImageUrl;
        private string _selectedAssetDownloadType;
        private string _selectedAssetDownloadUrl;
        private List<string> _downloadTypeList;
        private List<string> _categoryList;
        private List<AssetViewModel> _assetList;
        private AssetViewModel _selectedAsset;

        internal Asset AssetToEdit { get; set; }

        public AssetViewModel SelectedAsset
        {
            get { return _selectedAsset; }
            set
            {
                if (_selectedAsset != value)
                {
                    if (AssetToEdit != null)
                    {
                        _selectedAsset.Asset = AssetToEdit;
                        _selectedAsset.Name = AssetToEdit.Name;
                        _selectedAsset.Author = AssetToEdit.Author;
                        _selectedAsset.AssetCategory = AssetToEdit.Category;
                        _selectedAsset.Version = AssetToEdit.Version.ToString();
                        _selectedAsset.UpdatedDate = AssetToEdit.UpdatedDate.ToString(AssetViewModel.dateTimeFormat);
                    }


                    _selectedAsset = value;

                    if (_selectedAsset != null)
                    {
                        AssetToEdit = _selectedAsset.Asset;
                        SelectedAssetAuthor = AssetToEdit.Author;
                        SelectedAssetCategory = AssetToEdit.Category;
                        SelectedAssetDescription = AssetToEdit.Description;
                        SelectedAssetID = AssetToEdit.ID;
                        SelectedAssetImageUrl = AssetToEdit.PreviewImage;
                        SelectedAssetName = AssetToEdit.Name;
                        SelectedAssetUpdatedDate = AssetToEdit.UpdatedDate.ToString(AssetViewModel.dateTimeFormat);
                        SelectedAssetVersion = AssetToEdit.Version.ToString();

                        AssetCatalog.TryParseDownloadUrl(AssetToEdit.DownloadLink, out DownloadLocationType downloadType, out string url);
                        SelectedAssetDownloadUrl = url;

                        SelectedAssetDownloadType = "Url";
                        if (downloadType == DownloadLocationType.GDrive)
                        {
                            SelectedAssetDownloadType = "Google Drive";
                        }
                    }
                    else
                    {
                        ClearSelectedAsset();
                    }


                    NotifyPropertyChanged();
                }
            }
        }

        public string SelectedAssetName
        {
            get { return _selectedAssetName; }
            set
            {
                _selectedAssetName = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.Name = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetAuthor
        {
            get { return _selectedAssetAuthor; }
            set
            {
                _selectedAssetAuthor = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.Author = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetDescription
        {
            get { return _selectedAssetDescription; }
            set
            {
                _selectedAssetDescription = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.Description = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetCategory
        {
            get { return _selectedAssetCategory; }
            set
            {
                _selectedAssetCategory = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.Category = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetUpdatedDate
        {
            get { return _selectedAssetUpdatedDate; }
            set
            {
                _selectedAssetUpdatedDate = value;

                if (AssetToEdit != null)
                {
                    DateTime.TryParse(value, out DateTime newDate);
                    AssetToEdit.UpdatedDate = newDate;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetVersion
        {
            get { return _selectedAssetVersion; }
            set
            {
                _selectedAssetVersion = value;

                if (AssetToEdit != null)
                {
                    double.TryParse(value, out double newVersion);
                    AssetToEdit.Version = newVersion;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetID
        {
            get { return _selectedAssetID; }
            set
            {
                _selectedAssetID = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.ID = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetImageUrl
        {
            get { return _selectedAssetImageUrl; }
            set
            {
                _selectedAssetImageUrl = value;

                if (AssetToEdit != null)
                {
                    AssetToEdit.PreviewImage = value;
                }

                NotifyPropertyChanged();
            }
        }

        public string SelectedAssetDownloadType
        {
            get { return _selectedAssetDownloadType; }
            set
            {
                _selectedAssetDownloadType = value;
                SelectedAssetDownloadUrl = SelectedAssetDownloadUrl; // trigger the url to change format
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DownloadText));
                NotifyPropertyChanged(nameof(DownloadTooltip));

            }
        }

        public string SelectedAssetDownloadUrl
        {
            get { return _selectedAssetDownloadUrl; }
            set
            {
                _selectedAssetDownloadUrl = value;

                if (SelectedAssetDownloadType == "Url")
                {
                    AssetToEdit.DownloadLink = AssetCatalog.FormatUrl(value);
                }
                else if (SelectedAssetDownloadType == "Google Drive")
                {
                    AssetToEdit.DownloadLink = $"rsmm://GDrive/{value}";
                }

                NotifyPropertyChanged();
            }
        }

        public string DownloadText
        {
            get
            {
                if (SelectedAssetDownloadType == "Url")
                {
                    return "Url:";
                }
                else if (SelectedAssetDownloadType == "Google Drive")
                {
                    return "Drive ID:";
                }

                return "Url:";
            }
        }

        public string DownloadTooltip
        {
            get
            {
                if (SelectedAssetDownloadType == "Url")
                {
                    return "Enter the url to the direct download";
                }

                return "Enter the google drive id of the download (found in the google drive url)";
            }
        }

        public List<string> DownloadTypeList
        {
            get { return _downloadTypeList; }
            set
            {
                _downloadTypeList = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> CategoryList
        {
            get { return _categoryList; }
            set
            {
                _categoryList = value;
                NotifyPropertyChanged();
            }
        }

        public List<AssetViewModel> AssetList
        {
            get { return _assetList; }
            set
            {
                _assetList = value;
                NotifyPropertyChanged();
            }
        }

        public CreateCatalogViewModel()
        {
            CategoryList = new List<string>()
            {
                AssetCategory.Characters.Value,
                AssetCategory.Decks.Value,
                AssetCategory.Griptapes.Value,
                AssetCategory.Hats.Value,
                AssetCategory.Maps.Value,
                AssetCategory.Meshes.Value,
                AssetCategory.Pants.Value,
                AssetCategory.Shirts.Value,
                AssetCategory.Shoes.Value,
                AssetCategory.Trucks.Value,
                AssetCategory.Wheels.Value
            };

            DownloadTypeList = new List<string>()
            {
                "Url",
                "Google Drive"
            };

            AssetList = new List<AssetViewModel>();
            ClearSelectedAsset();
        }

        public BoolWithMessage ImportCatalog(string pathToCatalog)
        {
            try
            {
                AssetCatalog catalog = JsonConvert.DeserializeObject<AssetCatalog>(File.ReadAllText(pathToCatalog));

                AssetList = catalog.Assets.Select(a => new AssetViewModel(a)).ToList();
                ClearSelectedAsset();

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to import catalog: {e.Message}");
            }
        }

        public BoolWithMessage ExportCatalog(string savePath)
        {
            try
            {
                AssetCatalog catalog = new AssetCatalog()
                {
                    Name = "",
                    Assets = AssetList.Select(a => a.Asset).ToList()
                };

                string fileContents = JsonConvert.SerializeObject(catalog);
                File.WriteAllText(savePath, fileContents);

                return BoolWithMessage.True();
            }
            catch (Exception e)
            {
                return BoolWithMessage.False($"Failed to export catalog: {e.Message}");
            }
        }

        public void ClearSelectedAsset()
        {
            AssetToEdit = null;
            SelectedAssetAuthor = "";
            SelectedAssetCategory = "";
            SelectedAssetDescription = "";
            SelectedAssetDownloadType = "";
            SelectedAssetDownloadUrl = "";
            SelectedAssetID = "";
            SelectedAssetImageUrl = "";
            SelectedAssetName = "";
            SelectedAssetUpdatedDate = "";
            SelectedAssetVersion = "";
        }
    }
}
