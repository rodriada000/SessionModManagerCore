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
            //InitInstalledTextures();
        }




    }
}
