﻿/*
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
    ///     Interaction logic for About.xaml
    /// </summary>
    public partial class ErrorPage
    {
        public ErrorPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region Delegates

        public delegate void CloseEvent(bool hardFail);

        #endregion

        private bool _hardFail;

        public event CloseEvent ErrorClose;

        public void SetError(string desc, bool hardFail, System.Exception ex)
        {
            _hardFail = hardFail;
            this.BeginDispatch(() =>
            {
                txtError.Visibility = System.Windows.Visibility.Collapsed;
                txtDescription.Text = desc;
                txtHardFail.Visibility = hardFail
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
                string error = string.Empty;
                if (ex != null) error = ex.ToString();

                btnShowException.Visibility = error == string.Empty
                    ? System.Windows.Visibility.Hidden
                    : System.Windows.Visibility.Visible;
                txtError.Text = error;
            });
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ErrorClose?.Invoke(_hardFail);
        }

        private void btnShowException_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.BeginDispatch(() => txtError.Visibility = System.Windows.Visibility.Visible);
        }

        private void BugReport_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Windows.Clipboard.SetText(txtError.Text);
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
    }
}