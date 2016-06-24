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

namespace Elpis.Wpf.Pages
{
    /// <summary>
    ///     Interaction logic for Search.xaml
    /// </summary>
    public partial class Search
    {
        public Search(PandoraSharpPlayer.Player player)
        {
            _player = player;
            InitializeComponent();

            _player.SearchResult += _player_SearchResult;
            _player.ExceptionEvent += _player_ExceptionEvent;
        }

        private const string InitialSearchText = "Enter Artist, Track or Composer";

        public SearchMode SearchMode { get; set; }
        public PandoraSharp.Station VarietyStation { get; set; }

        private readonly PandoraSharpPlayer.Player _player;

        public event CancelHandler Cancel;
        public event AddVarietyHandler AddVariety;

        private void _player_ExceptionEvent(object sender, Util.ErrorCodes code, System.Exception ex)
        {
            ShowWait(false);
        }

        private void RunSearch(string query)
        {
            if (query == string.Empty || string.IsNullOrWhiteSpace(query)) return;
            lblNoResults.Visibility = System.Windows.Visibility.Collapsed;
            ShowWait(true);
            _player.StationSearchNew(query);
        }

        private void ShowWait(bool state)
        {
            this.BeginDispatch(
                () =>
                {
                    WaitScreen.Visibility = state
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                });
        }

        private void _player_SearchResult(object sender,
            System.Collections.Generic.List<PandoraSharp.SearchResult> result)
        {
            this.BeginDispatch(() =>
            {
                if (result.Count == 0)
                    lblNoResults.Visibility = System.Windows.Visibility.Visible;

                ResultItems.ItemsSource = result;
                ResultScroller.ScrollToHome();
                ShowWait(false);
            });
        }

        private void btnSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RunSearch(txtSearch.Text);
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cancel?.Invoke(this);
        }

        private void Grid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PandoraSharp.SearchResult result =
                (PandoraSharp.SearchResult) ((System.Windows.Controls.Grid) sender).DataContext;

            if (SearchMode == SearchMode.NewStation)
            {
                ShowWait(true);
                _player.CreateStation(result);
            }
            else
            {
                VarietyStation?.AddVariety(result);

                AddVariety?.Invoke(this);
            }
        }

        private void txtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                RunSearch(txtSearch.Text);
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            lblNoResults.Visibility = System.Windows.Visibility.Collapsed;
            txtSearch.Text = InitialSearchText;
            ResultScroller.ScrollToHome();
            ShowWait(false);
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowWait(false);
        }

        private void ClearSearchBox()
        {
            if (txtSearch.Text == InitialSearchText)
                txtSearch.Text = string.Empty;
        }

        private void txtSearch_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClearSearchBox();
        }

        #region Delegates

        public delegate void CancelHandler(object sender);

        public delegate void AddVarietyHandler(object sender);

        #endregion
    }
}