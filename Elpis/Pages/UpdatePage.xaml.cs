/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Elpis.Pages
{
    /// <summary>
    ///     Interaction logic for UpdatePage.xaml
    /// </summary>
    public partial class UpdatePage
    {
        public UpdatePage(UpdateSystem.UpdateCheck update)
        {
            InitializeComponent();

            _update = update;

            lblCurrVer.Content = _update.CurrentVersion.ToString();
            lblNewVer.Content = _update.NewVersion.ToString();
            txtReleaseNotes.Text = _update.ReleaseNotes;

            _update.DownloadProgress += _update_DownloadProgress;
            _update.DownloadComplete += _update_DownloadComplete;
        }

        #region Delegates

        public delegate void UpdateSelectionEventHandler(bool status);

        #endregion

        private readonly UpdateSystem.UpdateCheck _update;

        public event UpdateSelectionEventHandler UpdateSelectionEvent;

        public void DownloadUpdate()
        {
            string downloadDir = System.IO.Path.Combine(Config.ElpisAppData, "Updates");
            if (!System.IO.Directory.Exists(downloadDir))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(downloadDir);
                }
                catch (System.Exception ex)
                {
                    Util.Log.O("Trouble creating update directory! " + ex);
                    return;
                }
            }

            string downloadFile = System.IO.Path.Combine(downloadDir, "ElpisUpdate.exe");
            if (System.IO.File.Exists(downloadFile))
                System.IO.File.Delete(downloadFile);

            _update.DownloadUpdateAsync(downloadFile);
        }

        private void _update_DownloadComplete(bool error, System.Exception ex)
        {
            this.BeginDispatch(() =>
            {
                if (error)
                {
                    Util.Log.O("Error Downloading Update!");
                    lblDownloadStatus.Text = "Error downloading update. Please try again later.";
                    btnUpdate.Visibility = System.Windows.Visibility.Hidden;
                    btnLater.Content = "Close";
                }
                else
                {
                    System.Diagnostics.Process.Start(_update.UpdatePath);
                    SendUpdateSelection(true);
                }
            });
        }

        private void _update_DownloadProgress(int prog)
        {
            this.BeginDispatch(() =>
            {
                lblProgress.Content = prog + "%";
                progDownload.Value = prog;
            });
        }

        private void SendUpdateSelection(bool status)
        {
            if (UpdateSelectionEvent == null)
                UpdateSelectionEvent?.Invoke(status);
        }

        private void btnLater_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SendUpdateSelection(false);
        }

        private void btnUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //SendUpdateSelection(true);
            gridReleaseNotes.Visibility = System.Windows.Visibility.Hidden;
            gridDownload.Visibility = System.Windows.Visibility.Visible;
            DownloadUpdate();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            gridReleaseNotes.Visibility = System.Windows.Visibility.Visible;
            gridDownload.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}