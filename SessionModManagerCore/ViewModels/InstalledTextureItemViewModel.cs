using SessionModManagerCore.Classes;

namespace SessionModManagerCore.ViewModels
{
    public class InstalledTextureItemViewModel : ViewModelBase
    {
        private string _textureName;
        private bool _isSelected;

        public TextureMetaData MetaData { get; set; }

        public string TextureName
        {
            get { return _textureName; }
            set
            {
                _textureName = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public InstalledTextureItemViewModel(TextureMetaData metaData)
        {
            this.IsSelected = false;
            this.MetaData = metaData;
            TextureName = this.MetaData.Name == null ? this.MetaData.AssetName : this.MetaData.Name;
        }

    }
}
