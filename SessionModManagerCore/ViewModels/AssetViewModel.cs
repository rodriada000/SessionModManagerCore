using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class AssetViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static string dateTimeFormat = "ddd, dd MMM yy HH:mm";

        private string _name;
        private string _author;
        private string _description;
        private string _assetCategory;
        private string _updatedDate;
        private bool _isSelected;
        private string _version;

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

        public string UpdatedDate
        {
            get { return _updatedDate; }
            set
            {
                _updatedDate = value;
                NotifyPropertyChanged();
            }
        }

        public string Version
        {
            get { return _version; }
            set
            {
                _version = value;
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
            UpdatedDate = asset.UpdatedDate == DateTime.MinValue ? "" : asset.UpdatedDate.ToLocalTime().ToString(AssetViewModel.dateTimeFormat);

            Version = asset.Version <= 0 ? "1" : asset.Version.ToString();
            IsSelected = false;
        }
    }
}
