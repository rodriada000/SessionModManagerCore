using SessionModManagerCore.ViewModels;
using System;
using System.ComponentModel;

namespace SessionModManagerCore.Classes
{
    public class AssetDownloader
    {
        private static AssetDownloader _instance;
        public static AssetDownloader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AssetDownloader();
                }

                return _instance;
            }
        }

        public DownloadItemViewModel Download(string link, string saveFilePath, string description, Action onCancel, Action onComplete)
        {
            DownloadLocationType type;
            string location;

            if (!AssetCatalog.TryParseDownloadUrl(link, out type, out location))
            {
                return null;
            }

            Action onError = () =>
            {
            };


            DownloadItemViewModel newDownload = new DownloadItemViewModel()
            {
                ItemName = description,
                OnCancel = onCancel,
                OnError = onError,
                OnComplete = onComplete,
                DownloadSpeed = "Calculating ...",
                DownloadType = DownloadType.Asset
            };

            switch (type)
            {
                case DownloadLocationType.Url:
                    using (var wc = new System.Net.WebClient())
                    {
                        newDownload.PerformCancel = () =>
                        {
                            wc.CancelAsync();
                            newDownload.OnCancel?.Invoke();
                        };
                        wc.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
                        wc.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
                        wc.DownloadFileAsync(new Uri(location), saveFilePath, newDownload);
                    }

                    break;

                case DownloadLocationType.GDrive:
                    var gd = new GDrive();
                    newDownload.PerformCancel = () =>
                    {
                        gd.CancelAsync();
                        newDownload.OnCancel?.Invoke();
                    };
                    gd.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
                    gd.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
                    gd.Download(location, saveFilePath, newDownload);
                    break;

            }

            newDownload.IsStarted = true;
            return newDownload;
        }

        public void Download(DownloadItemViewModel newDownload)
        {
            DownloadLocationType type;
            string location;

            if (!AssetCatalog.TryParseDownloadUrl(newDownload.DownloadUrl, out type, out location))
            {
                return;
            }

            switch (type)
            {
                case DownloadLocationType.Url:
                    using (var wc = new System.Net.WebClient())
                    {
                        newDownload.PerformCancel = () =>
                        {
                            wc.CancelAsync();
                            newDownload.OnCancel?.Invoke();
                        };
                        wc.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
                        wc.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
                        wc.DownloadFileAsync(new Uri(location), newDownload.SaveFilePath, newDownload);
                    }

                    break;

                case DownloadLocationType.GDrive:
                    var gd = new GDrive();
                    newDownload.PerformCancel = () =>
                    {
                        gd.CancelAsync();
                        newDownload.OnCancel?.Invoke();
                    };
                    gd.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(_wc_DownloadProgressChanged);
                    gd.DownloadFileCompleted += new AsyncCompletedEventHandler(_wc_DownloadFileCompleted);
                    gd.Download(location, newDownload.SaveFilePath, newDownload);
                    break;

            }

            newDownload.IsStarted = true;
        }


        void _wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DownloadItemViewModel item = (DownloadItemViewModel)e.UserState;
            if (e.Cancelled)
            {
                if (sender is System.Net.WebClient)
                {
                    (sender as System.Net.WebClient).Dispose();
                }
                item.OnCancel?.Invoke();
            }
            else if (e.Error != null)
            {
                item.OnError?.Invoke();
                string msg = $"Error {item.ItemName} - {e.Error.GetBaseException().Message}";
            }
            else
            {
                item.OnComplete?.Invoke();
            }
        }

        void _wc_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            DownloadItemViewModel item = (DownloadItemViewModel)e.UserState;
            int prog = e.ProgressPercentage;
            if ((e.TotalBytesToReceive < 0) && (sender is GDrive))
            {
                prog = (int)(100 * e.BytesReceived / (sender as GDrive).GetContentLength());
            }
            UpdateDownloadProgress(item, prog, e.BytesReceived);
        }

        private void UpdateDownloadProgress(DownloadItemViewModel item, int percentDone, long bytesReceived)
        {
            if (item.PercentComplete != percentDone)
            {
                item.PercentComplete = percentDone;
            }

            TimeSpan interval = DateTime.Now - item.LastCalc;

            if ((interval.TotalSeconds >= 5))
            {
                if (bytesReceived > 0)
                {
                    item.DownloadSpeed = (((bytesReceived - item.LastBytes) / 1024.0) / interval.TotalSeconds).ToString("0.0") + "KB/s";
                    item.LastBytes = bytesReceived;
                }

                item.LastCalc = DateTime.Now;
            }
        }

    }
}
