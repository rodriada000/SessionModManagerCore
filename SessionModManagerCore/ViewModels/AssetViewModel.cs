using SessionAssetStore;
using SessionMapSwitcherCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class AssetViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _name;
        private string _author;
        private string _description;
        private string _assetCategory;
        private bool _isSelected;

        internal Asset Asset { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
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

        public string Author
        {
            get { return _author; }
            set
            {
                _author = value;
                NotifyPropertyChanged();
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyPropertyChanged();
            }
        }

        public string AssetCategory
        {
            get { return _assetCategory; }
            set
            {
                _assetCategory = value;
                NotifyPropertyChanged();
            }
        }

        public AssetViewModel(Asset asset)
        {
            this.Asset = asset;
            Name = asset.Name;
            Description = asset.Description;
            Author = asset.Author;
            AssetCategory = asset.Category;
            IsSelected = false;
        }

        public AssetViewModel(string name, string author, string description)
        {
            this.Asset = new Asset(name, description, author, name, "", "");
            this.Name = name;
            this.Author = author;
            this.Description = description;
            this.IsSelected = false;
        }
    }
}
