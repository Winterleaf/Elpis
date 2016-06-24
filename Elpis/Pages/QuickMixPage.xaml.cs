using System.Linq;

namespace Elpis.Pages
{
    /// <summary>
    ///     Interaction logic for QuickMixPage.xaml
    /// </summary>
    public partial class QuickMixPage : System.Windows.Controls.UserControl
    {
        public QuickMixPage(PandoraSharpPlayer.Player player)
        {
            _player = player;
            _player.QuickMixSavedEvent += _player_QuickMixSavedEvent;
            _player.ExceptionEvent += _player_ExceptionEvent;
            InitializeComponent();
        }

        public delegate void CancelHandler();

        public delegate void CloseHandler();

        private readonly PandoraSharpPlayer.Player _player;

        public event CancelHandler CancelEvent;
        public event CloseHandler CloseEvent;

        private void _player_ExceptionEvent(object sender, Util.ErrorCodes code, System.Exception ex)
        {
            ShowWait(false);
        }

        private void _player_QuickMixSavedEvent(object sender)
        {
            ShowWait(false);
            CloseEvent?.Invoke();
        }

        public void ShowWait(bool state)
        {
            this.BeginDispatch(
                () =>
                {
                    WaitScreen.Visibility = state
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                });
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CancelEvent?.Invoke();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowWait(false);

            System.Collections.Generic.IEnumerable<PandoraSharp.Station> subList = from s in _player.Stations
                where !s.IsQuickMix
                select s;

            StationItems.ItemsSource = subList;
        }

        private void btnSave_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowWait(true);
            _player.SaveQuickMix();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowWait(false);
        }
    }
}