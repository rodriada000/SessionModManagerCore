using SessionMapSwitcherCore.Classes;
using SessionMapSwitcherCore.ViewModels;
using SessionModManagerCore.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SessionModManagerCore.ViewModels
{
    public class ManageTexturesViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private List<InstalledTextureItemViewModel> _installedTextures;
        private string _statusMessage;

        public List<InstalledTextureItemViewModel> InstalledTextures
        {
            get
            {
                if (_installedTextures == null)
                    _installedTextures = new List<InstalledTextureItemViewModel>();

                return _installedTextures;
            }
            set
            {
                _installedTextures = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                Logger.Info($"StatusMessage = {_statusMessage}");
                NotifyPropertyChanged();
            }
        }

        public InstalledTextureItemViewModel SelectedTexture
        {
            get
            {
                return InstalledTextures.Where(t => t.IsSelected).FirstOrDefault();
            }
        }

        public ManageTexturesViewModel()
        {
            StatusMessage = null;
            InitInstalledTextures();
        }

        /// <summary>
        /// Reads installed_textures.json meta data and initializes <see cref="InstalledTextures"/> with results
        /// </summary>
        private void InitInstalledTextures()
        {
            InstalledTexturesMetaData installedMetaData = MetaDataManager.LoadTextureMetaData();

            List<InstalledTextureItemViewModel> textures = new List<InstalledTextureItemViewModel>();

            foreach (TextureMetaData item in installedMetaData.InstalledTextures)
            {
                textures.Add(new InstalledTextureItemViewModel(item));
            }

            InstalledTextures = textures.OrderBy(t => t.TextureName).ToList();
        }

        public void RemoveSelectedTexture()
        {
            InstalledTextureItemViewModel textureToRemove = SelectedTexture;

            if (textureToRemove == null)
            {
                Logger.Warn("textureToRemove is null");
                return;
            }

            BoolWithMessage deleteResult = MetaDataManager.DeleteTextureFiles(textureToRemove.MetaData);

            if (deleteResult.Result)
            {
                StatusMessage = $"Successfully removed {textureToRemove.TextureName}!";
                InitInstalledTextures();
            }
            else
            {
                StatusMessage = $"Failed to remove texture: {deleteResult.Message}";
            }
        }
    }

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
