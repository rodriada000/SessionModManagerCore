using SessionMapSwitcherCore.Classes;
using SessionModManagerCore.Classes;
using System;

namespace SessionModManagerCore.ViewModels
{
    public class GameSettingsViewModel : ViewModelBase
    {
        private string _objectCountText;
        private bool _skipMovieIsChecked;
        private bool _dBufferIsChecked;
        private bool _lightPropagationVolumeIsChecked;

        public string ObjectCountText
        {
            get { return _objectCountText; }
            set
            {
                _objectCountText = value;
                NotifyPropertyChanged();
            }
        }

        public bool SkipMovieIsChecked
        {
            get { return _skipMovieIsChecked; }
            set
            {
                _skipMovieIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool DBufferIsChecked
        {
            get { return _dBufferIsChecked; }
            set
            {
                _dBufferIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool LightPropagationVolumeIsChecked
        {
            get { return _lightPropagationVolumeIsChecked; }
            set
            {
                _lightPropagationVolumeIsChecked = value;
                NotifyPropertyChanged();
            }
        }


        public GameSettingsViewModel()
        {
            RefreshGameSettings();
        }

        public void RefreshGameSettings()
        {
            BoolWithMessage result = GameSettingsManager.RefreshGameSettingsFromIniFiles();

            if (result.Result == false)
            {
                MessageService.Instance.ShowMessage(result.Message);
            }

            ObjectCountText = GameSettingsManager.ObjectCount.ToString();
            SkipMovieIsChecked = GameSettingsManager.SkipIntroMovie;
            LightPropagationVolumeIsChecked = GameSettingsManager.EnableLightPropagationVolume;
            DBufferIsChecked = GameSettingsManager.EnableDBuffer;
        }

        public bool UpdateGameSettings()
        {
            string returnMessage = "";

            BoolWithMessage didSetSettings = GameSettingsManager.ValidateAndUpdateGameSettings(SkipMovieIsChecked, LightPropagationVolumeIsChecked, DBufferIsChecked);
            BoolWithMessage didSetObjCount = BoolWithMessage.True(); // set to true by default in case the user does not have the file to modify


            if (GameSettingsManager.DoesInventorySaveFileExist())
            {
                didSetObjCount = GameSettingsManager.ValidateAndUpdateObjectCount(ObjectCountText);

                if (didSetObjCount.Result == false)
                {
                    returnMessage += didSetObjCount.Message;
                }
            }

            if (didSetSettings.Result == false)
            {
                returnMessage += didSetSettings.Message;
                MessageService.Instance.ShowMessage(returnMessage);
                return false;
            }


            returnMessage = "Game settings updated!";

            if (GameSettingsManager.DoesInventorySaveFileExist() == false)
            {
                returnMessage += " Object count cannot be changed until a .sav file exists.";
            }

            if (SessionPath.IsSessionRunning())
            {
                returnMessage += " Restart the game for changes to take effect.";
            }

            MessageService.Instance.ShowMessage(returnMessage);



            return didSetSettings.Result && didSetObjCount.Result;
        }
    }
}
