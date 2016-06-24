namespace RequestDecrypter
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            int unix = PandoraSharp.Time.Unix();
            System.Console.WriteLine(unix);
        }

        private void btnDecrypt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //txtDecrypted.Text =
            string text = PandoraSharp.Crypto.OutKey.Decrypt(txtEncrypted.Text);
        }
    }
}