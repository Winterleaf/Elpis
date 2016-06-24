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
    ///     Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage
    {
        public LoginPage(PandoraSharpPlayer.Player player, Config config)
        {
            _config = config;

            _player = player;
            _player.ConnectionEvent += _player_ConnectionEvent;

            InitializeComponent();
        }

        #region Delegates

        public delegate void ConnectingEventHandler();

        #endregion

        private const string InitEmail = "enter email address";
        private const string InitPass = "enter password";

        public Util.ErrorCodes ErrorCode { get; set; } = Util.ErrorCodes.Success;

        public bool LoginFailed { get; set; }

        private readonly Config _config;
        private readonly PandoraSharpPlayer.Player _player;

        public event ConnectingEventHandler ConnectingEvent;

        private void ShowError()
        {
            this.BeginDispatch(() =>
            {
                lblError.Text = Util.Errors.GetErrorMessage(ErrorCode);
                //WaitScreen.Visibility = Visibility.Hidden;
            });
        }

        private void _player_ConnectionEvent(object sender, bool state, Util.ErrorCodes code)
        {
            if (!state)
            {
                LoginFailed = true;
                Util.Log.O("Connection Error: {0} - {1}", code.ToString(), Util.Errors.GetErrorMessage(code));

                ErrorCode = code;
                ShowError();
            }
            else
            {
                this.BeginDispatch(() =>
                {
                    _config.Fields.Login_Email = _player.Email;
                    _config.Fields.Login_Password = _player.Password;

                    //In case AudioFormat was changed because user does not have subscription
                    _config.Fields.Pandora_AudioFormat = _player.AudioFormat;

                    _config.SaveConfig();
                });
            }
        }

        public void Login()
        {
            LoginFailed = false;
            //WaitScreen.Visibility = Visibility.Visible;
            _player.Connect(txtEmail.Text, txtPassword.Password);
            ConnectingEvent?.Invoke();
        }

        private void btnLogin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Login();
        }

        private void txtEmail_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                HidePasswordMask();
                txtPassword.Focus();
                txtPassword.SelectAll();
            }
        }

        private void txtPassword_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Login();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            txtEmail.Text = _config.Fields.Login_Email;
            if (txtEmail.Text == string.Empty)
            {
                txtEmail.Foreground = Resources["ShadeMediumBrush"] as System.Windows.Media.Brush;
                txtEmail.Text = InitEmail;
            }

            txtPassword.Password = _config.Fields.Login_Password;
            if (txtPassword.Password == string.Empty)
            {
                txtPasswordMask.Text = InitPass;
                txtPasswordMask.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                txtPasswordMask.Visibility = System.Windows.Visibility.Hidden;
            }

            lblError.Text = string.Empty;

            if (!LoginFailed && _config.Fields.Login_AutoLogin && !string.IsNullOrEmpty(_config.Fields.Login_Email) &&
                !string.IsNullOrEmpty(_config.Fields.Login_Password))
            {
                Login();
            }

            if (LoginFailed)
                ShowError();

            txtEmail.Focus();
            txtEmail.SelectAll();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.BeginDispatch(() => WaitScreen.Visibility = System.Windows.Visibility.Hidden);
        }

        private void ClearEmail()
        {
            if (txtEmail.Text == InitEmail)
            {
                txtEmail.Foreground = Resources["MainFontBrush"] as System.Windows.Media.Brush;
                txtEmail.Text = string.Empty;
            }
        }

        private void txtEmail_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ClearEmail();
        }

        private void txtEmail_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ClearEmail();
        }

        private void HidePasswordMask()
        {
            if (txtPasswordMask.Visibility == System.Windows.Visibility.Visible)
                txtPasswordMask.Visibility = System.Windows.Visibility.Hidden;
            txtPassword.Focus();
        }

        private void txtPasswordMask_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            HidePasswordMask();
        }

        private void txtPassword_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            HidePasswordMask();
        }

        private void Register_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
    }
}