namespace Elpis
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application, Microsoft.Shell.ISingleInstanceApp
    {
        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(System.Collections.Generic.IList<string> args)
        {
            return HandleCommandLine(args);
        }

        #endregion

        [System.STAThread]
        public static void Main()
        {
            if (!Microsoft.Shell.SingleInstance<App>.InitializeAsFirstInstance("ElpisInstance")) return;
            App application = new App();
            application.Init();
            application.Run();

            // Allow single instance code to perform cleanup operations
            Microsoft.Shell.SingleInstance<App>.Cleanup();
        }

        public void Init()
        {
            InitializeComponent();
        }

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            HandleCommandLine(System.Environment.GetCommandLineArgs());
            if (Elpis.MainWindow.Clo.ShowHelp)
                Current.Shutdown();
        }

        private void ShowHelp(Util.OptionSet optionSet, string msg = null)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            optionSet.WriteOptionDescriptions(sw);
            string output = sw.ToString();
            if (msg != null)
                output += "\r\n\r\n" + msg;
            System.Windows.MessageBox.Show(output, "Elpis Options");
        }

        public bool HandleCommandLine(System.Collections.Generic.IList<string> args)
        {
            CommandLineOptions clo = new CommandLineOptions();
            Util.OptionSet p =
                new Util.OptionSet().Add("c|config=", "a {CONFIG} file to load ",
                    delegate(string v) { clo.ConfigPath = v; })
                    .Add("h|?|help", "show this message and exit", delegate(string v) { clo.ShowHelp = v != null; })
                    .Add("playpause", "toggles playback", delegate(string v) { clo.TogglePlayPause = v != null; })
                    .Add("next", "skips current track", delegate(string v) { clo.SkipTrack = v != null; })
                    .Add("thumbsup", "rates the song as suitable for this station",
                        delegate(string v) { clo.DoThumbsUp = v != null; })
                    .Add("thumbsdown", "rates the song as unsuitable for this station",
                        delegate(string v) { clo.DoThumbsDown = v != null; })
                    .Add("s|station=", "starts station \"{STATIONNAME}\" - puts quotes around station names with spaces",
                        delegate(string v) { clo.StationToLoad = v; });

            try
            {
                p.Parse(args);
            }
            catch (Util.OptionException e)
            {
                clo.ShowHelp = true;
                Elpis.MainWindow.SetCommandLine(clo);
                ShowHelp(p, e.Message);
            }

            Elpis.MainWindow.SetCommandLine(clo);

            if (clo.ShowHelp)
            {
                ShowHelp(p);
            }
            else
            {
                ((MainWindow) MainWindow)?.DoCommandLine();
            }

            return true;
        }
    }
}