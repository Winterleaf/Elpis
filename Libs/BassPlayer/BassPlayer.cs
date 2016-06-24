/*
 * Copyright 2012 - Adam Haile / Media Portal
 * http://adamhaile.net
 *
 * This file is part of BassPlayer.
 * BassPlayer is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * BassPlayer is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with BassPlayer. If not, see http://www.gnu.org/licenses/.
 * 
 * Note: Below is a heavily modified version of BassAudio.cs from
 * http://sources.team-mediaportal.com/websvn/filedetails.php?repname=MediaPortal&path=%2Ftrunk%2Fmediaportal%2FCore%2FMusicPlayer%2FBASS%2FBassAudio.cs
*/

using System;
using System.Linq;

namespace Elpis.BassPlayer
{
    /// <summary>
    ///     This singleton class is responsible for managing the BASS audio Engine object.
    /// </summary>
    //public class BassMusicPlayer
    //{
    //    #region Variables

    //    internal static BassAudioEngine _Player;
    //    private static Thread BassAsyncLoadThread;

    //    private static string _email = string.Empty;
    //    private static string _key = string.Empty;

    //    #endregion

    //    #region Constructors/Destructors

    //    // Singleton -- make sure we can't instantiate this class
    //    private BassMusicPlayer()
    //    {
    //    }

    //    #endregion

    //    #region Properties

    //    /// <summary>
    //    /// Returns the BassAudioEngine Object
    //    /// </summary>
    //    public static BassAudioEngine Player
    //    {
    //        get
    //        {
    //            if (_Player == null)
    //            {
    //                _Player = new BassAudioEngine(_email, _key);
    //            }

    //            return _Player;
    //        }
    //    }

    //    /// <summary>
    //    /// Returns a Boolean if the BASS Audio Engine is initialised
    //    /// </summary>
    //    public static bool Initialized
    //    {
    //        get { return _Player != null && _Player.Initialized; }
    //    }

    //    /// <summary>
    //    /// Is the BASS Engine Freed?
    //    /// </summary>
    //    public static bool BassFreed
    //    {
    //        get { return _Player.BassFreed; }
    //    }

    //    #endregion

    //    #region Public Methods

    //    /// <summary>
    //    /// Create the BASS Audio Engine Objects
    //    /// </summary>
    //    //public static void CreatePlayerAsync()
    //    //{
    //    //    if (_Player != null)
    //    //    {
    //    //        return;
    //    //    }
    //    //    ThreadStart ts = InternalCreatePlayerAsync;
    //    //    BassAsyncLoadThread = new Thread(ts);
    //    //    BassAsyncLoadThread.Name = "BassAudio";
    //    //    BassAsyncLoadThread.Start();
    //    //}
    //    public static void SetRegistration(string email, string key)
    //    {
    //        _email = email;
    //        _key = key;
    //    }

    //    /// <summary>
    //    /// Frees, the BASS Audio Engine.
    //    /// </summary>
    //    public static void FreeBass()
    //    {
    //        if (_Player == null)
    //        {
    //            return;
    //        }

    //        _Player.FreeBass();
    //    }

    //    #endregion

    //    #region Private Methods

    //    /// <summary>
    //    /// Thread for Creating the BASS Audio Engine objects.
    //    /// </summary>
    //    private static void InternalCreatePlayerAsync()
    //    {
    //        if (_Player == null)
    //        {
    //            _Player = new BassAudioEngine();
    //        }
    //    }

    //    #endregion
    //}
    public class BassException : System.Exception
    {
        public BassException() {}

        public BassException(string msg) : base(msg) {}
    }

    public class BassStreamException : System.Exception
    {
        public BassStreamException() {}

        public BassStreamException(string msg) : base(msg) {}

        public BassStreamException(string msg, Un4seen.Bass.BASSError error) : base(msg)
        {
            ErrorCode = error;
        }

        public Un4seen.Bass.BASSError ErrorCode { get; set; }
    }

    /// <summary>
    ///     Handles playback of Audio files and Internet streams via the BASS Audio Engine.
    /// </summary>
    public class BassAudioEngine : System.IDisposable // : IPlayer
    {
        #region Constructors/Destructors

        public BassAudioEngine(System.Collections.Generic.List<int> soundFontHandles, bool needUpdate, Un4seen.Bass.AddOn.Midi.BASS_MIDI_FONT[] soundFonts, string email = "", string key = "")
        {
            _soundFonts = soundFonts;
            _regEmail = email;
            _regKey = key;

            Initialize();
        }
        public BassAudioEngine( string email = "", string key = "")
        {
            
            _regEmail = email;
            _regKey = key;

            Initialize();
        }

        #endregion

        private Un4seen.Bass.DOWNLOADPROC _downloadProcDelegate;
        private Un4seen.Bass.SYNCPROC _metaTagSyncProcDelegate;
        private Un4seen.Bass.SYNCPROC _playbackEndProcDelegate;
        private Un4seen.Bass.SYNCPROC _playbackFadeOutProcDelegate;
        private Un4seen.Bass.SYNCPROC _playbackStreamFreedProcDelegate;

        public event PlaybackStartHandler PlaybackStart;

        public event PlaybackStopHandler PlaybackStop;

        public event PlaybackProgressHandler PlaybackProgress;

        public event TrackPlaybackCompletedHandler TrackPlaybackCompleted;

        public event CrossFadeHandler CrossFade;

        public event PlaybackStateChangedHandler PlaybackStateChanged;

        public event InternetStreamSongChangedHandler InternetStreamSongChanged;

        public event DownloadCompleteHandler DownloadComplete;

        public event DownloadCanceledHandler DownloadCanceled;

        #region Enums

        /// <summary>
        ///     The various States for Playback
        /// </summary>
        public enum PlayState
        {
            Init,
            Playing,
            Paused,
            Ended,
            Stopped
        }

        #region Nested type: PlayBackType

        /// <summary>
        ///     States, how the Playback is handled
        /// </summary>
        private enum PlayBackType
        {
            Normal = 0,
            Gapless = 1,
            Crossfade = 2
        }

        #endregion

        #region Nested type: Progress

        public class Progress
        {
            public System.TimeSpan TotalTime { get; set; }
            public System.TimeSpan ElapsedTime { get; set; }

            public System.TimeSpan RemainingTime => TotalTime - ElapsedTime;

            public double Percent
            {
                get
                {
                    if (TotalTime.Ticks == 0)
                        return 0.0;

                    return ElapsedTime.TotalSeconds/TotalTime.TotalSeconds*100;
                }
            }
        }

        #endregion

        #endregion

        #region Delegates

        public delegate void CrossFadeHandler(object sender, string filePath);

        public delegate void DownloadCanceledHandler(object sender, string downloadFile);

        public delegate void DownloadCompleteHandler(object sender, string downloadFile);

        public delegate void InternetStreamSongChangedHandler(object sender);

        public delegate void PlaybackProgressHandler(object sender, Progress prog);

        public delegate void PlaybackStartHandler(object sender, double duration);

        public delegate void PlaybackStateChangedHandler(object sender, PlayState oldState, PlayState newState);

        public delegate void PlaybackStopHandler(object sender);

        public delegate void TrackPlaybackCompletedHandler(object sender, string filePath);

        #endregion

        #region Variables

        private const int Maxstreams = 1;

        private readonly System.Collections.Generic.List<int> _decoderPluginHandles = new System.Collections.Generic.List<int>();

        private readonly System.Collections.Generic.List<System.Collections.Generic.List<int>> _streamEventSyncHandles =
            new System.Collections.Generic.List<System.Collections.Generic.List<int>>();

        private readonly System.Collections.Generic.List<int> _streams = new System.Collections.Generic.List<int>(Maxstreams);

        private readonly Un4seen.Bass.BASSTimer _updateTimer = new Un4seen.Bass.BASSTimer();

        private readonly float[,] _mixingMatrix = {
            {1, 0}, // left front out = left in
            {0, 1}, // right front out = right in
            {1, 0}, // centre out = left in
            {0, 1}, // LFE out = right in
            {1, 0}, // left rear/side out = left in
            {0, 1}, // right rear/side out = right in
            {1, 0}, // left-rear center out = left in
            {0, 1} // right-rear center out = right in
        };

        private readonly string _regEmail;
        private readonly string _regKey;

        private int _currentStreamIndex;

        public bool NotifyPlaying = true;
        private int _bufferingMs = 5000;

        private bool _mixing;
        private bool _softStop = true;
        private string _soundDevice = "";

        private int _streamVolume = 100;

        private string _downloadFile = String.Empty;
        private bool _downloadFileComplete;
        private System.IO.FileStream _downloadStream;
        private Un4seen.Bass.Misc.DSP_Gain _gain;

        private int _mixer;
        private int _progUpdateInterval = 500; //update every 500 ms
        private Un4seen.Bass.AddOn.Tags.TAG_INFO _tagInfo;

        // Midi File support
        private readonly Un4seen.Bass.AddOn.Midi.BASS_MIDI_FONT[] _soundFonts;

        //Registration

        #endregion

        #region Properties

        public string SoundDevice
        {
            get { return _soundDevice; }
            set { ChangeOutputDevice(value); }
        }

        /// <summary>
        ///     Returns, if the player is in initialising stage
        /// </summary>
        public bool Initializing => State == PlayState.Init;

        /// <summary>
        ///     Returns the Duration of an Audio Stream
        /// </summary>
        public double Duration
        {
            get
            {
                int stream = GetCurrentStream();

                if (stream == 0)
                {
                    return 0;
                }

                double duration = GetTotalStreamSeconds(stream);

                return duration;
            }
        }

        /// <summary>
        ///     Returns the Current Position in the Stream
        /// </summary>
        public double CurrentPosition
        {
            get
            {
                int stream = GetCurrentStream();

                if (stream == 0)
                {
                    return 0;
                }

                long pos = Un4seen.Bass.Bass.BASS_ChannelGetPosition(stream); // position in bytes

                double curPosition = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, pos);
                    // the elapsed time length

                return curPosition;
            }
        }

        public int ProgressUpdateInterval
        {
            get { return _progUpdateInterval; }
            set
            {
                _progUpdateInterval = value;
                _updateTimer.Interval = _progUpdateInterval;
            }
        }

        /// <summary>
        ///     Returns the Current Play State
        /// </summary>
        public PlayState State { get; private set; } = PlayState.Init;

        /// <summary>
        ///     Has the Playback Ended?
        /// </summary>
        public bool Ended => State == PlayState.Ended;

        /// <summary>
        ///     Is Playback Paused?
        /// </summary>
        public bool Paused => State == PlayState.Paused;

        /// <summary>
        ///     Is the Player Playing?
        /// </summary>
        public bool Playing => State == PlayState.Playing || State == PlayState.Paused;

        /// <summary>
        ///     Is Player Stopped?
        /// </summary>
        public bool Stopped => State == PlayState.Init || State == PlayState.Stopped;

        /// <summary>
        ///     Returns the File, currently played
        /// </summary>
        public string CurrentFile { get; private set; } = String.Empty;

        /// <summary>
        ///     Gets/Sets the Playback Volume
        /// </summary>
        public int Volume
        {
            get { return _streamVolume; }
            set
            {
                if (_streamVolume == value) return;
                if (value > 100)
                {
                    value = 100;
                }

                if (value < 0)
                {
                    value = 0;
                }

                _streamVolume = value;
                _streamVolume = value;
                Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_GVOL_STREAM, _streamVolume*100);
            }
        }

        /// <summary>
        ///     Returns the Playback Speed
        /// </summary>
        public int Speed { get; set; } = 1;

        /// <summary>
        ///     Gets/Sets the Crossfading Interval
        /// </summary>
        public int CrossFadeIntervalMs { get; set; } = 4000;

        /// <summary>
        ///     Gets/Sets the Buffering of BASS Streams
        /// </summary>
        public int BufferingMs
        {
            get { return _bufferingMs; }
            set
            {
                if (_bufferingMs == value)
                {
                    return;
                }

                _bufferingMs = value;
                Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_BUFFER, _bufferingMs);
            }
        }

        /// <summary>
        ///     Returns the instance of the Visualisation Manager
        /// </summary>
        //R//
        //public IVisualizationManager IVizManager
        //{
        //  get { return VizManager; }
        //}
        public bool IsRadio { get; private set; }

        /// <summary>
        ///     Returns the Playback Type
        /// </summary>
        public int PlaybackType { get; private set; }

        /// <summary>
        ///     Returns the instance of the Video Window
        /// </summary>
        /// <summary>
        ///     Is the Audio Engine initialised
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        ///     Is Crossfading enabled
        /// </summary>
        public bool CrossFading { get; private set; }

        /// <summary>
        ///     Is Crossfading enabled
        /// </summary>
        public bool CrossFadingEnabled => CrossFadeIntervalMs > 0;

        /// <summary>
        ///     Is BASS freed?
        /// </summary>
        public bool BassFreed { get; private set; }

        /// <summary>
        ///     Returns the Stream, currently played
        /// </summary>
        public int CurrentAudioStream => GetCurrentVizStream();

        #endregion

        #region Methods

        /// <summary>
        ///     Release the Player
        /// </summary>
        public void Dispose()
        {
            if (!Stopped) // Check if stopped already to avoid that Stop() is called two or three times
            {
                Stop(true);
            }
            _proxyValue.Free();
        }

        /// <summary>
        ///     Dispose the BASS Audio engine. Free all BASS and Visualisation related resources
        /// </summary>
        public void DisposeAndCleanUp()
        {
            Dispose();
            // Clean up BASS Resources
            try
            {
                // Some Winamp dsps might raise an exception when closing
                Un4seen.Bass.AddOn.WaDsp.BassWaDsp.BASS_WADSP_Free();
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.ToString());
                throw new BassException(ex.ToString());
            }
            if (_mixer != 0)
            {
                Un4seen.Bass.Bass.BASS_ChannelStop(_mixer);
            }

            Un4seen.Bass.Bass.BASS_Stop();
            Un4seen.Bass.Bass.BASS_Free();

            foreach (int stream in _streams)
            {
                FreeStream(stream);
            }

            foreach (int pluginHandle in _decoderPluginHandles)
            {
                Un4seen.Bass.Bass.BASS_PluginFree(pluginHandle);
            }
        }

        /// <summary>
        ///     The BASS engine itself is not initialised at this stage, since it may cause S/PDIF for Movies not working on some
        ///     systems.
        /// </summary>
        private void Initialize()
        {
            try
            {
                Log.Info("BASS: Initialize BASS environment ...");
                LoadSettings();

                //TODO: Make this configurable
                if (_regEmail != String.Empty)
                    Un4seen.Bass.BassNet.Registration(_regEmail, _regKey);

                // Set the Global Volume. 0 = silent, 10000 = Full
                // We get 0 - 100 from Configuration, so multiply by 100
                Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_GVOL_STREAM, _streamVolume*100);

                if (_mixing)
                {
                    // In case of mixing use a Buffer of 500ms only, because the Mixer plays the complete bufer, before for example skipping
                    BufferingMs = 500;
                }
                else
                {
                    Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_BUFFER, _bufferingMs);
                }

                for (int i = 0; i < Maxstreams; i++)
                {
                    _streams.Add(0);
                }

                _playbackFadeOutProcDelegate = PlaybackFadeOutProc;
                _playbackEndProcDelegate = PlaybackEndProc;
                _playbackStreamFreedProcDelegate = PlaybackStreamFreedProc;
                _metaTagSyncProcDelegate = MetaTagSyncProc;

                _downloadProcDelegate = DownloadProc;

                _streamEventSyncHandles.Add(new System.Collections.Generic.List<int>());
                _streamEventSyncHandles.Add(new System.Collections.Generic.List<int>());

                LoadAudioDecoderPlugins();

                Log.Info("BASS: Initializing BASS environment done.");

                Initialized = true;
                BassFreed = true;
            }

            catch (System.Exception ex)
            {
                Log.Error("BASS: Initialize thread failed.  Reason: {0}", ex.Message);
                throw new BassException("BASS: Initialize thread failed.  Reason: " + ex);
            }
        }

        /// <summary>
        ///     Free BASS, when not playing Audio content, as it might cause S/PDIF output stop working
        /// </summary>
        public void FreeBass()
        {
            if (BassFreed) return;
            Log.Info("BASS: Freeing BASS. Non-audio media playback requested.");

            if (_mixer != 0)
            {
                Un4seen.Bass.Bass.BASS_ChannelStop(_mixer);
                _mixer = 0;
            }

            Un4seen.Bass.Bass.BASS_Free();
            BassFreed = true;
        }

        /// <summary>
        ///     Init BASS, when a Audio file is to be played
        /// </summary>
        public void InitBass()
        {
            try
            {
                Log.Info("BASS: Initializing BASS audio engine...");

                Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_DEV_DEFAULT, true);
                    //Allows following Default device (Win 7 Only)
                int soundDevice = GetSoundDevice();

                bool initOk = Un4seen.Bass.Bass.BASS_Init(soundDevice, 44100, Un4seen.Bass.BASSInit.BASS_DEVICE_DEFAULT | Un4seen.Bass.BASSInit.BASS_DEVICE_LATENCY,
                    System.IntPtr.Zero);
                if (initOk)
                {
                    // Create an 8 Channel Mixer, which should be running until stopped.
                    // The streams to play are added to the active screen
                    if (_mixing && _mixer == 0)
                    {
                        _mixer = Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(44100, 8,
                            Un4seen.Bass.BASSFlag.BASS_MIXER_NONSTOP | Un4seen.Bass.BASSFlag.BASS_STREAM_AUTOFREE);
                    }

                    _updateTimer.Interval = _progUpdateInterval;
                    _updateTimer.Tick += OnUpdateTimerTick;

                    Log.Info("BASS: Initialization done.");
                    Initialized = true;
                    BassFreed = false;
                }
                else
                {
                    Un4seen.Bass.BASSError error = Un4seen.Bass.Bass.BASS_ErrorGetCode();
                    Log.Error("BASS: Error initializing BASS audio engine {0}", System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error));
                    throw new System.Exception("Init Error: " + error);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("BASS: Initialize failed. Reason: {0}", ex.Message);
                throw new BassException("BASS: Initialize failed. Reason: }" + ex.Message);
            }
        }

        private System.Runtime.InteropServices.GCHandle _proxyValue;

        public void SetProxy(string address, int port, string user = "", string password = "")
        {
            string proxy = address + ":" + port;
            if (user != "")
                proxy = user + ":" + password + "@" + proxy;

            byte[] proxyBytes = System.Text.Encoding.Default.GetBytes(proxy);
            _proxyValue = System.Runtime.InteropServices.GCHandle.Alloc(proxyBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            Un4seen.Bass.Bass.BASS_SetConfigPtr(Un4seen.Bass.BASSConfig.BASS_CONFIG_NET_PROXY, _proxyValue.AddrOfPinnedObject());

            //_proxyValue.Free();
        }

        /// <summary>
        ///     Get the Sound devive as set in the Configuartion
        /// </summary>
        /// <returns></returns>
        private int GetSoundDevice()
        {
            int sounddevice = -1;
            // Check if the specified Sounddevice still exists
            if (_soundDevice == "Default")
            {
                Log.Info("BASS: Using default Sound Device");
                sounddevice = -1;
            }
            else
            {
                Un4seen.Bass.BASS_DEVICEINFO[] soundDeviceDescriptions = Un4seen.Bass.Bass.BASS_GetDeviceInfos();
                bool foundDevice = false;
                for (int i = 0; i < soundDeviceDescriptions.Length; i++)
                {
                    if (soundDeviceDescriptions[i].name == _soundDevice)
                    {
                        foundDevice = true;
                        sounddevice = i;
                        break;
                    }
                }
                if (!foundDevice)
                {
                    Log.Warn("BASS: specified Sound device does not exist. Using default Sound Device");
                    sounddevice = -1;
                }
                else
                {
                    Log.Info("BASS: Using Sound Device {0}", _soundDevice);
                }
            }
            return sounddevice;
        }

        /// <summary>
        ///     Load Settings
        /// </summary>
        private void LoadSettings()
        {
            //TODO - Load Settings

            //using (Profile.Settings xmlreader = new Profile.MPSettings())
            //{
            _soundDevice = "Default";
            //xmlreader.GetValueAsString("audioplayer", "sounddevice", "Default Sound Device");

            _streamVolume = 100; // xmlreader.GetValueAsInt("audioplayer", "streamOutputLevel", 85);
            _bufferingMs = 5000; // xmlreader.GetValueAsInt("audioplayer", "buffering", 5000);

            if (_bufferingMs <= 0)
            {
                _bufferingMs = 1000;
            }

            else if (_bufferingMs > 8000)
            {
                _bufferingMs = 8000;
            }

            CrossFadeIntervalMs = 0; //xmlreader.GetValueAsInt("audioplayer", "crossfade", 4000);

            if (CrossFadeIntervalMs < 0)
            {
                CrossFadeIntervalMs = 0;
            }

            else if (CrossFadeIntervalMs > 16000)
            {
                CrossFadeIntervalMs = 16000;
            }

            _softStop = true; //xmlreader.GetValueAsBool("audioplayer", "fadeOnStartStop", true);

            _mixing = false; //xmlreader.GetValueAsBool("audioplayer", "mixing", false);

            if (CrossFadeIntervalMs == 0)
            {
                PlaybackType = (int) PlayBackType.Normal;
                CrossFadeIntervalMs = 100;
            }
            else
            {
                PlaybackType = (int) PlayBackType.Crossfade;
            }
            //}
        }

        /// <summary>
        ///     Return the BASS Stream to be used for Visualisation purposes.
        ///     We will extract the WAVE and FFT data to be provided to the Visualisation Plugins
        ///     In case of Mixer active, we need to return the Mixer Stream.
        ///     In all other cases the current actove stream is used.
        /// </summary>
        /// <returns></returns>
        internal int GetCurrentVizStream()
        {
            if (_streams.Count == 0)
            {
                return -1;
            }

            return _mixing ? _mixer : GetCurrentStream();
        }

        /// <summary>
        ///     Returns the Current Stream
        /// </summary>
        /// <returns></returns>
        internal int GetCurrentStream()
        {
            if (_streams.Count == 0)
            {
                return -1;
            }

            if (_currentStreamIndex < 0)
            {
                _currentStreamIndex = 0;
            }

            else if (_currentStreamIndex >= _streams.Count)
            {
                _currentStreamIndex = _streams.Count - 1;
            }

            return _streams[_currentStreamIndex];
        }

        /// <summary>
        ///     Returns the Next Stream
        /// </summary>
        /// <returns></returns>
        private int GetNextStream()
        {
            int currentStream = GetCurrentStream();

            if (currentStream == -1)
            {
                return -1;
            }

            if (currentStream == 0 || Un4seen.Bass.Bass.BASS_ChannelIsActive(currentStream) == Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED)
            {
                return currentStream;
            }

            _currentStreamIndex++;

            if (_currentStreamIndex >= _streams.Count)
            {
                _currentStreamIndex = 0;
            }

            return _streams[_currentStreamIndex];
        }

        private void UpdateProgress(int stream)
        {
            if (PlaybackProgress != null)
            {
                System.TimeSpan totaltime = new System.TimeSpan(0, 0, (int) GetTotalStreamSeconds(stream));
                System.TimeSpan elapsedtime = new System.TimeSpan(0, 0, (int) GetStreamElapsedTime(stream));
                PlaybackProgress(this, new Progress {TotalTime = totaltime, ElapsedTime = elapsedtime});
            }
        }

        private void GetProgressInternal()
        {
            int stream = GetCurrentStream();

            if (StreamIsPlaying(stream))
            {
                UpdateProgress(stream);
            }
            else
            {
                _updateTimer.Stop();
            }
        }

        public void GetProgress()
        {
            System.Threading.Tasks.Task.Factory.StartNew(GetProgressInternal);
        }

        /// <summary>
        ///     Timer to update the Playback Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUpdateTimerTick(object sender, System.EventArgs e)
        {
            GetProgressInternal();
        }

        /// <summary>
        ///     Load External BASS Audio Decoder Plugins
        /// </summary>
        private void LoadAudioDecoderPlugins()
        {
            //In this case, only load AAC to save load time
            Log.Info("BASS: Loading AAC Decoder");

            string decoderFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof (BassAudioEngine)).Location);
            if (decoderFolderPath == null)
            {
                Log.Error(@"BASS: Unable to load AAC decoder.");
                throw new BassException(@"BASS: Unable to load AAC decoder.");
            }

            string aacDecoder = System.IO.Path.Combine(decoderFolderPath, "bass_aac.dll");

            int pluginHandle;
            if ((pluginHandle = Un4seen.Bass.Bass.BASS_PluginLoad(aacDecoder)) != 0)
            {
                _decoderPluginHandles.Add(pluginHandle);
                Log.Debug("BASS: Added DecoderPlugin: {0}", aacDecoder);
            }
            else
            {
                Log.Error(@"BASS: Unable to load AAC decoder.");
                throw new BassException(@"BASS: Unable to load AAC decoder.");
            }

            /*
            if (!Directory.Exists(decoderFolderPath))
            {
                Log.Error(@"BASS: Unable to find decoders path.");
                throw new BassException(@"BASS: Unable to find decoders path.");
            }

            var dirInfo = new DirectoryInfo(decoderFolderPath);
            FileInfo[] decoders = dirInfo.GetFiles();

            int pluginHandle = 0;
            int decoderCount = 0;

            foreach (FileInfo file in decoders)
            {
                if (Path.GetExtension(file.Name).ToLower() != ".dll")
                {
                    continue;
                }

                pluginHandle = Bass.BASS_PluginLoad(file.FullName);

                if (pluginHandle != 0)
                {
                    DecoderPluginHandles.Add(pluginHandle);
                    decoderCount++;
                    Log.Debug("BASS: Added DecoderPlugin: {0}", file.FullName);
                }

                else
                {
                    Log.Debug("BASS: Unable to load: {0}", file.FullName);
                }
            }

            if (decoderCount > 0)
            {
                Log.Info("BASS: Loaded {0} Audio Decoders.", decoderCount);
            }

            else
            {
                Log.Error(@"BASS: No audio decoders were loaded. Confirm decoders are present in path.");
                throw new BassException(@"BASS: No audio decoders were loaded. Confirm decoders are present in path.");
            }
             * */
        }

        public void SetGain(double gainDb)
        {
            if (_gain == null)
            {
                _gain = new Un4seen.Bass.Misc.DSP_Gain();
            }

            if (gainDb > 60.0)
                gainDb = 60.0;

            if (System.Math.Abs(gainDb) < 0.0000d)
            {
                _gain.SetBypass(true);
            }
            else
            {
                _gain.SetBypass(false);
                _gain.Gain_dBV = gainDb;
            }
        }

        private void FinalizeDownloadStream()
        {
            if (_downloadStream == null) return;
            lock (_downloadStream)
            {
                if (!_downloadFileComplete)
                    DownloadCanceled?.Invoke(this, _downloadFile);

                _downloadStream.Flush();
                _downloadStream.Close();
                _downloadStream = null;

                _downloadFile = String.Empty;
                _downloadFileComplete = false;
            }
        }

        private void SetupDownloadStream(string outputFile)
        {
            FinalizeDownloadStream();
            _downloadFile = outputFile;
            _downloadFileComplete = false;
            _downloadStream = new System.IO.FileStream(outputFile, System.IO.FileMode.Create);
        }

        public bool PlayStreamWithDownload(string url, string outputFile, double gainDb)
        {
            SetGain(gainDb);
            return PlayStreamWithDownload(url, outputFile);
        }

        public bool PlayStreamWithDownload(string url, string outputFile)
        {
            FinalizeDownloadStream();
            SetupDownloadStream(outputFile);
            return Play(url);
        }

        public bool Play(string filePath, double gainDb)
        {
            SetGain(gainDb);
            return Play(filePath);
        }

        /// <summary>
        ///     Starts Playback of the given file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool Play(string filePath)
        {
            if (!Initialized)
            {
                return false;
            }

            try
            {
                _updateTimer.Stop();
            }
            catch
            {
                throw new BassStreamException("Bass Error: Update Timer Error");
            }
            int stream = GetCurrentStream();

            bool doFade = false;
            bool result = true;
            Speed = 1; // Set playback Speed to normal speed

            try
            {
                if (Paused || (String.Compare(filePath.ToLower(), CurrentFile.ToLower(), System.StringComparison.Ordinal) == 0 && stream != 0))
                {
                    bool doReturn = !Paused;
                    // Selected file is equal to current stream
                    if (State == PlayState.Paused)
                    {
                        // Resume paused stream
                        if (_softStop)
                        {
                            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 1, 500);
                        }
                        else
                        {
                            Un4seen.Bass.Bass.BASS_ChannelSetAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 1);
                        }

                        result = Un4seen.Bass.Bass.BASS_Start();

                        if (result)
                        {
                            State = PlayState.Playing;
                            _updateTimer.Start();
                            PlaybackStateChanged?.Invoke(this, PlayState.Paused, State);
                        }

                        if (doReturn)
                            return result;
                    }
                }

                if (stream != 0 && StreamIsPlaying(stream))
                {
                    int oldStream = stream;
                    double oldStreamDuration = GetTotalStreamSeconds(oldStream);
                    double oldStreamElapsedSeconds = GetStreamElapsedTime(oldStream);
                    double crossFadeSeconds = CrossFadeIntervalMs;

                    if (crossFadeSeconds > 0)
                        crossFadeSeconds = crossFadeSeconds/1000.0;

                    if (oldStreamDuration - (oldStreamElapsedSeconds + crossFadeSeconds) > -1)
                    {
                        FadeOutStop(oldStream);
                    }
                    else
                    {
                        Un4seen.Bass.Bass.BASS_ChannelStop(oldStream);
                    }

                    doFade = true;
                    stream = GetNextStream();

                    if (stream != 0 || StreamIsPlaying(stream))
                    {
                        FreeStream(stream);
                    }
                }

                if (stream != 0)
                {
                    if (!Stopped) // Check if stopped already to avoid that Stop() is called two or three times
                    {
                        Stop(true);
                    }
                    FreeStream(stream);
                }

                State = PlayState.Init;

                // Make sure Bass is ready to begin playing again
                Un4seen.Bass.Bass.BASS_Start();

                if (filePath != String.Empty)
                {
                    // Turn on parsing of ASX files
                    Un4seen.Bass.Bass.BASS_SetConfig(Un4seen.Bass.BASSConfig.BASS_CONFIG_NET_PLAYLIST, 2);

                    Un4seen.Bass.BASSFlag streamFlags;
                    if (_mixing)
                    {
                        streamFlags = Un4seen.Bass.BASSFlag.BASS_STREAM_DECODE | Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT;
                        // Don't use the BASS_STREAM_AUTOFREE flag on a decoding channel. will produce a BASS_ERROR_NOTAVAIL
                    }
                    else
                    {
                        streamFlags = Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT | Un4seen.Bass.BASSFlag.BASS_STREAM_AUTOFREE;
                    }

                    CurrentFile = filePath;

                    IsRadio = false;

                    if (filePath.ToLower().Contains(@"http://") || filePath.ToLower().Contains(@"https://") || filePath.ToLower().StartsWith("mms") ||
                        filePath.ToLower().StartsWith("rtsp"))
                    {
                        IsRadio = true; // We're playing Internet Radio Stream

                        stream = Un4seen.Bass.Bass.BASS_StreamCreateURL(filePath, 0, streamFlags, _downloadProcDelegate, System.IntPtr.Zero);

                        if (stream != 0)
                        {
                            // Get the Tags and set the Meta Tag SyncProc
                            _tagInfo = new Un4seen.Bass.AddOn.Tags.TAG_INFO(filePath);
                            SetStreamTags(stream);

                            if (Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromURL(stream, _tagInfo))
                            {
                                GetMetaTags();
                            }

                            Un4seen.Bass.Bass.BASS_ChannelSetSync(stream, Un4seen.Bass.BASSSync.BASS_SYNC_META, 0, _metaTagSyncProcDelegate, System.IntPtr.Zero);
                        }
                        Log.Debug("BASSAudio: Webstream found - trying to fetch stream {0}", System.Convert.ToString(stream));
                    }
                    else if (IsModFile(filePath))
                    {
                        // Load a Mod file
                        stream = Un4seen.Bass.Bass.BASS_MusicLoad(filePath, 0, 0,
                            Un4seen.Bass.BASSFlag.BASS_SAMPLE_SOFTWARE | Un4seen.Bass.BASSFlag.BASS_SAMPLE_FLOAT | Un4seen.Bass.BASSFlag.BASS_MUSIC_AUTOFREE |
                            Un4seen.Bass.BASSFlag.BASS_MUSIC_PRESCAN | Un4seen.Bass.BASSFlag.BASS_MUSIC_RAMP, 0);
                    }
                    else
                    {
                        // Create a Standard Stream
                        stream = Un4seen.Bass.Bass.BASS_StreamCreateFile(filePath, 0, 0, streamFlags);
                    }

                    // Is Mixing, then we create a mixer channel and assign the stream to the mixer
                    if (_mixing && stream != 0)
                    {
                        // Do an upmix of the stereo according to the matrix. 
                        // Now Plugin the stream to the mixer and set the mixing matrix
                        Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(_mixer, stream,
                            Un4seen.Bass.BASSFlag.BASS_MIXER_MATRIX | Un4seen.Bass.BASSFlag.BASS_STREAM_AUTOFREE | Un4seen.Bass.BASSFlag.BASS_MIXER_NORAMPIN |
                            Un4seen.Bass.BASSFlag.BASS_MIXER_BUFFER);
                        Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetMatrix(stream, _mixingMatrix);
                    }

                    _streams[_currentStreamIndex] = stream;

                    if (stream != 0)
                    {
                        // When we have a MIDI file, we need to assign the sound banks to the stream
                        if (IsMidiFile(filePath) && _soundFonts != null)
                        {
                            Un4seen.Bass.AddOn.Midi.BassMidi.BASS_MIDI_StreamSetFonts(stream, _soundFonts, _soundFonts.Length);
                        }

                        _streamEventSyncHandles[_currentStreamIndex] = RegisterPlaybackEvents(stream, _currentStreamIndex);

                        if (doFade && CrossFadeIntervalMs > 0)
                        {
                            CrossFading = true;

                            // Reduce the stream volume to zero so we can fade it in...
                            Un4seen.Bass.Bass.BASS_ChannelSetAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 0);

                            // Fade in from 0 to 1 over the _CrossFadeIntervalMS duration 
                            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 1, CrossFadeIntervalMs);
                        }
                    }
                    else
                    {
                        Un4seen.Bass.BASSError error = Un4seen.Bass.Bass.BASS_ErrorGetCode();
                        Log.Error("BASS: Unable to create Stream for {0}.  Reason: {1}.", filePath, System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error));
                        throw new BassStreamException("Bass Error: Unable to create stream - " + System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error),
                            error);
                    }

                    bool playbackStarted;
                    if (_mixing)
                    {
                        playbackStarted = Un4seen.Bass.Bass.BASS_ChannelIsActive(_mixer) == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING || Un4seen.Bass.Bass.BASS_ChannelPlay(_mixer, false);
                    }
                    else
                    {
                        playbackStarted = Un4seen.Bass.Bass.BASS_ChannelPlay(stream, false);
                    }

                    if (stream != 0 && playbackStarted)
                    {
                        Log.Info("BASS: playback started");

                        PlayState oldState = State;
                        State = PlayState.Playing;

                        _updateTimer.Start();

                        if (oldState != State)
                        {
                            PlaybackStateChanged?.Invoke(this, oldState, State);
                        }

                        PlaybackStart?.Invoke(this, GetTotalStreamSeconds(stream));
                    }

                    else
                    {
                        Un4seen.Bass.BASSError error = Un4seen.Bass.Bass.BASS_ErrorGetCode();
                        Log.Error("BASS: Unable to play {0}.  Reason: {1}.", filePath, System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error));
                        throw new BassStreamException("Bass Error: Unable to play - " + System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error), error);

                        // Release all of the sync proc handles
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("BASS: Play caused an exception:  {0}.", ex);

                if (ex.GetType() == typeof (BassStreamException))
                    throw;

                throw new BassException("BASS: Play caused an exception: " + ex);
            }

            return result;
        }

        /// <summary>
        ///     Is this a MOD file?
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsModFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath);
            if (extension == null) return false;
            string ext = extension.ToLower();

            switch (ext)
            {
                case ".mod":
                case ".mo3":
                case ".it":
                case ".xm":
                case ".s3m":
                case ".mtm":
                case ".umx":
                    return true;

                default:
                    return false;
            }
            
        }

        /// <summary>
        ///     Is this a MIDI file?
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsMidiFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath);
            if (extension == null) return false;
            string ext = extension.ToLower();

            switch (ext)
            {
                case ".midi":
                case ".mid":
                case ".rmi":
                case ".kar":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        ///     Register the various Playback Events
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        private System.Collections.Generic.List<int> RegisterPlaybackEvents(int stream, int streamIndex)
        {
            if (stream == 0)
            {
                return null;
            }

            System.Collections.Generic.List<int> syncHandles = new System.Collections.Generic.List<int>
            {
                RegisterPlaybackFadeOutEvent(stream, streamIndex, CrossFadeIntervalMs),
                RegisterPlaybackEndEvent(stream, streamIndex),
                RegisterStreamFreedEvent(stream)
            };

            // Don't register the fade out event for last.fm radio, as it causes problems
            // if (!_isLastFMRadio)


            return syncHandles;
        }

        /// <summary>
        ///     Register the Fade out Event
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamIndex"></param>
        /// <param name="fadeOutMs"></param>
        /// <returns></returns>
        private int RegisterPlaybackFadeOutEvent(int stream, int streamIndex, int fadeOutMs)
        {
            long len = Un4seen.Bass.Bass.BASS_ChannelGetLength(stream); // length in bytes
            double totaltime = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, len); // the total time length
            double fadeOutSeconds = 0;

            if (fadeOutMs > 0)
                fadeOutSeconds = fadeOutMs/1000.0;

            long bytePos = Un4seen.Bass.Bass.BASS_ChannelSeconds2Bytes(stream, totaltime - fadeOutSeconds);

            int syncHandle = Un4seen.Bass.Bass.BASS_ChannelSetSync(stream, Un4seen.Bass.BASSSync.BASS_SYNC_ONETIME | Un4seen.Bass.BASSSync.BASS_SYNC_POS, bytePos,
                _playbackFadeOutProcDelegate, System.IntPtr.Zero);

            if (syncHandle == 0)
            {
                Log.Debug("BASS: RegisterPlaybackFadeOutEvent of stream {0} failed with error {1}", stream,
                    System.Enum.GetName(typeof (Un4seen.Bass.BASSError), Un4seen.Bass.Bass.BASS_ErrorGetCode()));
            }

            return syncHandle;
        }

        /// <summary>
        ///     Register the Playback end Event
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        private int RegisterPlaybackEndEvent(int stream, int streamIndex = -1)
        {
            int syncHandle = Un4seen.Bass.Bass.BASS_ChannelSetSync(stream, Un4seen.Bass.BASSSync.BASS_SYNC_ONETIME | Un4seen.Bass.BASSSync.BASS_SYNC_END, 0,
                _playbackEndProcDelegate, System.IntPtr.Zero);

            if (syncHandle == 0)
            {
                Log.Debug("BASS: RegisterPlaybackEndEvent of stream {0} failed with error {1}", stream,
                    System.Enum.GetName(typeof (Un4seen.Bass.BASSError), Un4seen.Bass.Bass.BASS_ErrorGetCode()));
            }

            return syncHandle;
        }

        /// <summary>
        ///     Register Stream Free Event
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private int RegisterStreamFreedEvent(int stream)
        {
            int syncHandle = Un4seen.Bass.Bass.BASS_ChannelSetSync(stream, Un4seen.Bass.BASSSync.BASS_SYNC_FREE, 0, _playbackStreamFreedProcDelegate,
                System.IntPtr.Zero);

            if (syncHandle == 0)
            {
                Log.Debug("BASS: RegisterStreamFreedEvent of stream {0} failed with error {1}", stream,
                    System.Enum.GetName(typeof (Un4seen.Bass.BASSError), Un4seen.Bass.Bass.BASS_ErrorGetCode()));
            }

            return syncHandle;
        }

        ///// <summary>
        /////     Unregister the Playback Events
        ///// </summary>
        ///// <param name="stream"></param>
        ///// <param name="syncHandles"></param>
        ///// <returns></returns>
        //private bool UnregisterPlaybackEvents(int stream, System.Collections.Generic.List<int> syncHandles)
        //{
        //    try
        //    {
        //        foreach (int syncHandle in syncHandles.Where(syncHandle => syncHandle != 0)) {
        //            Un4seen.Bass.Bass.BASS_ChannelRemoveSync(stream, syncHandle);
        //        }
        //    }

        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        /// <summary>
        ///     Free a Stream
        /// </summary>
        /// <param name="stream"></param>
        private void FreeStream(int stream)
        {
            int streamIndex = -1;

            for (int i = 0; i < _streams.Count; i++)
            {
                if (_streams[i] != stream) continue;
                streamIndex = i;
                break;
            }

            if (streamIndex != -1)
            {
                System.Collections.Generic.List<int> eventSyncHandles = _streamEventSyncHandles[streamIndex];

                foreach (int syncHandle in eventSyncHandles)
                {
                    Un4seen.Bass.Bass.BASS_ChannelRemoveSync(stream, syncHandle);
                }
            }

            Un4seen.Bass.Bass.BASS_StreamFree(stream);
            

            CrossFading = false; // Set crossfading to false, Play() will update it when the next song starts
        }

        /// <summary>
        ///     Is stream Playing?
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool StreamIsPlaying(int stream)
        {
            return stream != 0 && (Un4seen.Bass.Bass.BASS_ChannelIsActive(stream) == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING);
        }

        /// <summary>
        ///     Get Total Seconds of the Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private double GetTotalStreamSeconds(int stream)
        {
            if (stream == 0)
            {
                return 0;
            }

            // length in bytes
            long len = Un4seen.Bass.Bass.BASS_ChannelGetLength(stream);

            // the total time length
            double totaltime = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, len);
            return totaltime;
        }

        /// <summary>
        ///     Retrieve the elapsed time
        /// </summary>
        /// <returns></returns>
        private double GetStreamElapsedTime()
        {
            return GetStreamElapsedTime(GetCurrentStream());
        }

        /// <summary>
        ///     Retrieve the elapsed time
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private double GetStreamElapsedTime(int stream)
        {
            if (stream == 0)
            {
                return 0;
            }

            // position in bytes
            long pos = Un4seen.Bass.Bass.BASS_ChannelGetPosition(stream);

            // the elapsed time length
            double elapsedtime = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, pos);
            return elapsedtime;
        }

        private void DownloadProc(System.IntPtr buffer, int length, System.IntPtr user)
        {
            if (_downloadStream == null)
                return;

            Log.Debug("DownloadProc: " + length);
            try
            {
                if (buffer != System.IntPtr.Zero)
                {
                    byte[] managedBuffer = new byte[length];
                    System.Runtime.InteropServices.Marshal.Copy(buffer, managedBuffer, 0, length);
                    _downloadStream.Write(managedBuffer, 0, length);
                    _downloadStream.Flush();
                }
                else
                {
                    _downloadFileComplete = true;
                    string file = _downloadFile;

                    FinalizeDownloadStream();

                    DownloadComplete?.Invoke(this, file);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error("BASS: Exception in DownloadProc: {0} {1}", ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        ///     Fade Out  Procedure
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        private void PlaybackFadeOutProc(int handle, int stream, int data, System.IntPtr userData)
        {
            Log.Debug("BASS: PlaybackFadeOutProc of stream {0}", stream);

            CrossFade?.Invoke(this, CurrentFile);

            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, -1, CrossFadeIntervalMs);
            bool removed = Un4seen.Bass.Bass.BASS_ChannelRemoveSync(stream, handle);
            if (removed)
            {
                Log.Debug("BassAudio: *** BASS_ChannelRemoveSync in PlaybackFadeOutProc");
            }
        }

        /// <summary>
        ///     Playback end Procedure
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        private void PlaybackEndProc(int handle, int stream, int data, System.IntPtr userData)
        {
            Log.Debug("BASS: PlaybackEndProc of stream {0}", stream);

            TrackPlaybackCompleted?.Invoke(this, CurrentFile);

            bool removed = Un4seen.Bass.Bass.BASS_ChannelRemoveSync(stream, handle);
            if (removed)
            {
                Log.Debug("BassAudio: *** BASS_ChannelRemoveSync in PlaybackEndProc");
            }
        }

        /// <summary>
        ///     Stream Freed Proc
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        private void PlaybackStreamFreedProc(int handle, int stream, int data, System.IntPtr userData)
        {
            //Util.Log.O("PlaybackStreamFreedProc");
            Log.Debug("BASS: PlaybackStreamFreedProc of stream {0}", stream);

            HandleSongEnded(false);

            for (int i = 0; i < _streams.Count; i++)
            {
                if (stream != _streams[i]) continue;
                _streams[i] = 0;
                break;
            }
        }

        /// <summary>
        ///     Gets the tags from the Internet Stream.
        /// </summary>
        /// <param name="stream"></param>
        private void SetStreamTags(int stream)
        {
            //TODO - Make this output to something useful??
            string[] tags = Un4seen.Bass.Bass.BASS_ChannelGetTagsICY(stream);
            if (tags != null)
            {
                foreach (string item in tags)
                {
                    if (item.ToLower().StartsWith("icy-name:"))
                    {
                        //GUIPropertyManager.SetProperty("#Play.Current.Album", item.Substring(9));
                    }

                    if (item.ToLower().StartsWith("icy-genre:"))
                    {
                        //GUIPropertyManager.SetProperty("#Play.Current.Genre", item.Substring(10));
                    }

                    Log.Info("BASS: Connection Information: {0}", item);
                }
            }
            else
            {
                tags = Un4seen.Bass.Bass.BASS_ChannelGetTagsHTTP(stream);
                if (tags != null)
                {
                    foreach (string item in tags)
                    {
                        Log.Info("BASS: Connection Information: {0}", item);
                    }
                }
            }
        }

        /// <summary>
        ///     This Callback Procedure is called by BASS, once a song changes.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <param name="user"></param>
        private void MetaTagSyncProc(int handle, int channel, int data, System.IntPtr user)
        {
            // BASS_SYNC_META is triggered on meta changes of SHOUTcast streams
            if (_tagInfo.UpdateFromMETA(Un4seen.Bass.Bass.BASS_ChannelGetTags(channel, Un4seen.Bass.BASSTag.BASS_TAG_META), false, false))
            {
                GetMetaTags();
            }
        }

        /// <summary>
        ///     Set the Properties out of the Tags
        /// </summary>
        private void GetMetaTags()
        {
            // There seems to be an issue with setting correctly the title via taginfo
            // So let's filter it out ourself
            string title = _tagInfo.title;
            int streamUrlIndex = title.IndexOf("';StreamUrl=", System.StringComparison.Ordinal);
            if (streamUrlIndex > -1)
            {
                title = _tagInfo.title.Substring(0, streamUrlIndex);
            }

            Log.Info("BASS: Internet Stream. New Song: {0} - {1}", _tagInfo.artist, title);

            InternetStreamSongChanged?.Invoke(this);
        }

        private void HandleSongEnded(bool bManualStop, bool songSkipped = false)
        {
            Log.Debug("BASS: HandleSongEnded - manualStop: {0}, CrossFading: {1}", bManualStop, CrossFading);
            PlayState oldState = State;

            if (!bManualStop)
            {
                //if (_CrossFading)
                //{
                //    _State = PlayState.Playing;
                //}
                //else
                //{
                CurrentFile = "";
                State = PlayState.Ended;

                //}
            }
            else
            {
                State = songSkipped ? PlayState.Init : PlayState.Stopped;
            }

            Util.Log.O("BASS: Playstate Changed - " + State);

            if (oldState != State)
            {
                PlaybackStateChanged?.Invoke(this, oldState, State);
            }

            FinalizeDownloadStream();
            CrossFading = false; // Set crossfading to false, Play() will update it when the next song starts
        }

        /// <summary>
        ///     Fade out Song
        /// </summary>
        /// <param name="stream"></param>
        private void FadeOutStop(int stream)
        {
            Log.Debug("BASS: FadeOutStop of stream {0}", stream);

            if (!StreamIsPlaying(stream))
            {
                return;
            }

            //int level = Bass.BASS_ChannelGetLevel(stream);
            Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, -1, CrossFadeIntervalMs);
        }

        /// <summary>
        ///     Pause Playback
        /// </summary>
        public void PlayPause()
        {
            CrossFading = false;
            int stream = GetCurrentStream();

            Log.Debug("BASS: Pause of stream {0}", stream);
            try
            {
                PlayState oldPlayState = State;

                if (oldPlayState == PlayState.Ended || oldPlayState == PlayState.Init)
                {
                    return;
                }

                if (oldPlayState == PlayState.Paused)
                {
                    State = PlayState.Playing;

                    if (_softStop)
                    {
                        // Fade-in over 500ms
                        Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 1, 500);
                        Un4seen.Bass.Bass.BASS_Start();
                    }

                    else
                    {
                        Un4seen.Bass.Bass.BASS_ChannelSetAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 1);
                        Un4seen.Bass.Bass.BASS_Start();
                    }

                    _updateTimer.Start();
                }

                else
                {
                    State = PlayState.Paused;
                    _updateTimer.Stop();

                    if (_softStop)
                    {
                        // Fade-out over 500ms
                        Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, 0, 500);

                        // Wait until the slide is done
                        while (Un4seen.Bass.Bass.BASS_ChannelIsSliding(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL))
                            System.Threading.Thread.Sleep(20);

                        Un4seen.Bass.Bass.BASS_Pause();
                    }

                    else
                    {
                        Un4seen.Bass.Bass.BASS_Pause();
                    }
                }

                if (oldPlayState != State)
                {
                    PlaybackStateChanged?.Invoke(this, oldPlayState, State);
                }
            }

            catch (System.Exception)
            {
                //?
            }
        }

        /// <summary>
        ///     Stopping Playback
        /// </summary>
        public void Stop(bool songSkipped = false)
        {
            CrossFading = false;

            int stream = GetCurrentStream();
            Log.Debug("BASS: Stop of stream {0}", stream);
            try
            {
                _updateTimer.Stop();
                if (_softStop)
                {
                    Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL, -1, 500);

                    // Wait until the slide is done
                    while (Un4seen.Bass.Bass.BASS_ChannelIsSliding(stream, Un4seen.Bass.BASSAttribute.BASS_ATTRIB_VOL))
                        System.Threading.Thread.Sleep(20);
                }
                if (_mixing)
                {
                    Un4seen.Bass.Bass.BASS_ChannelStop(stream);
                    Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelRemove(stream);
                }
                else
                {
                    Un4seen.Bass.Bass.BASS_ChannelStop(stream);
                }

                PlaybackStop?.Invoke(this);

                HandleSongEnded(true, songSkipped);
            }

            catch (System.Exception ex)
            {
                Log.Error("BASS: Stop command caused an exception - {0}", ex.Message);
                throw new BassException("BASS: Stop command caused an exception - }" + ex.Message);
            }

            NotifyPlaying = false;
        }

        /// <summary>
        ///     Is Seeking enabled
        /// </summary>
        /// <returns></returns>
        public bool CanSeek()
        {
            return true;
        }

        /// <summary>
        ///     Seek Forward in the Stream
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool SeekForward(int ms)
        {
            if (Speed == 1) // not to exhaust log when ff
                Log.Debug("BASS: SeekForward for {0} ms", System.Convert.ToString(ms));
            CrossFading = false;

            if (State != PlayState.Playing)
            {
                return false;
            }

            if (ms <= 0)
            {
                return false;
            }

            try
            {
                int stream = GetCurrentStream();
                long len = Un4seen.Bass.Bass.BASS_ChannelGetLength(stream); // length in bytes
                double totaltime = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, len); // the total time length

                long pos = _mixing ? Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelGetPosition(stream) : Un4seen.Bass.Bass.BASS_ChannelGetPosition(stream);

                double timePos = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, pos);
                double offsetSecs = ms/1000.0;

                if (timePos + offsetSecs >= totaltime)
                {
                    return false;
                }

                if (_mixing)
                {
                    Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetPosition(stream,
                        Un4seen.Bass.Bass.BASS_ChannelSeconds2Bytes(stream, timePos + offsetSecs));
                    // the elapsed time length
                }
                else
                    Un4seen.Bass.Bass.BASS_ChannelSetPosition(stream, timePos + offsetSecs); // the elapsed time length
            }

            catch
            {
                //
            }

            return false;
        }

        /// <summary>
        ///     Seek Backwards within the stream
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public bool SeekReverse(int ms)
        {
            if (Speed == 1) // not to exhaust log
                Log.Debug("BASS: SeekReverse for {0} ms", System.Convert.ToString(ms));
            CrossFading = false;

            if (State != PlayState.Playing)
            {
                return false;
            }

            if (ms <= 0)
            {
                return false;
            }

            int stream = GetCurrentStream();

            try
            {
                //long len = Bass.BASS_ChannelGetLength(stream); // length in bytes

                long pos = _mixing ? Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelGetPosition(stream) : Un4seen.Bass.Bass.BASS_ChannelGetPosition(stream);

                double timePos = Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(stream, pos);
                double offsetSecs = ms/1000.0;

                if (timePos - offsetSecs <= 0)
                {
                    return false;
                }

                if (_mixing)
                {
                    Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetPosition(stream,
                        Un4seen.Bass.Bass.BASS_ChannelSeconds2Bytes(stream, timePos - offsetSecs));
                    // the elapsed time length
                }
                else
                    Un4seen.Bass.Bass.BASS_ChannelSetPosition(stream, timePos - offsetSecs); // the elapsed time length
            }

            catch
            {
             //   result = false;
            }

            return false;
        }

        /// <summary>
        ///     Seek to a specific position in the stream
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool SeekToTimePosition(int position)
        {
            Log.Debug("BASS: SeekToTimePosition: {0} ", System.Convert.ToString(position));
            CrossFading = false;

            bool result = true;

            try
            {
                int stream = GetCurrentStream();

                if (StreamIsPlaying(stream))
                {
                    if (_mixing)
                    {
                        Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelSetPosition(stream, Un4seen.Bass.Bass.BASS_ChannelSeconds2Bytes(stream, position));
                    }
                    else
                    {
                        Un4seen.Bass.Bass.BASS_ChannelSetPosition(stream, (float) position);
                    }
                }
            }

            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        ///     Seek Relative in the Stream
        /// </summary>
        /// <param name="dTime"></param>
        public void SeekRelative(double dTime)
        {
            CrossFading = false;

            if (State != PlayState.Init)
            {
                double dCurTime = GetStreamElapsedTime();

                dTime = dCurTime + dTime;

                if (dTime < 0.0d)
                {
                    dTime = 0.0d;
                }

                if (dTime < Duration)
                {
                    SeekToTimePosition((int) dTime);
                }
            }
        }

        /// <summary>
        ///     Seek Absoluet in the Stream
        /// </summary>
        /// <param name="dTime"></param>
        public void SeekAbsolute(double dTime)
        {
            CrossFading = false;

            if (State != PlayState.Init)
            {
                if (dTime < 0.0d)
                {
                    dTime = 0.0d;
                }

                if (dTime < Duration)
                {
                    SeekToTimePosition((int) dTime);
                }
            }
        }

        /// <summary>
        ///     Seek Relative Percentage
        /// </summary>
        /// <param name="iPercentage"></param>
        public void SeekRelativePercentage(int iPercentage)
        {
            CrossFading = false;

            if (State == PlayState.Init) return;
            GetStreamElapsedTime();
            double dDuration = Duration;
            double fOnePercentDuration = Duration/100.0d;

            double dSeekPercentageDuration = fOnePercentDuration*iPercentage;
            double dPositionMs = dDuration += dSeekPercentageDuration;

            if (dPositionMs < 0)
            {
                dPositionMs = 0d;
            }

            if (dPositionMs > dDuration)
            {
                //Something wrong here?
                dPositionMs = dDuration;
            }

            SeekToTimePosition((int) dDuration);
        }

        /// <summary>
        ///     Seek Absolute Percentage
        /// </summary>
        /// <param name="iPercentage"></param>
        public void SeekAsolutePercentage(int iPercentage)
        {
            CrossFading = false;

            if (State == PlayState.Init) return;
            if (iPercentage < 0)
            {
                iPercentage = 0;
            }

            if (iPercentage >= 100)
            {
                iPercentage = 100;
            }

            if (iPercentage == 0)
            {
                SeekToTimePosition(0);
            }

            else
            {
                SeekToTimePosition((int) (Duration*(iPercentage/100d)));
            }
        }

        /// <summary>
        ///     Return the dbLevel to be used by a VUMeter
        /// </summary>
        /// <param name="dbLevelL"></param>
        /// <param name="dbLevelR"></param>
        public void Rms(out double dbLevelL, out double dbLevelR)
        {
            // Find out with which stream to deal with
            int level = _mixing ? Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelGetLevel(GetCurrentStream()) : Un4seen.Bass.Bass.BASS_ChannelGetLevel(GetCurrentStream());

            int peakL = Un4seen.Bass.Utils.LowWord32(level);
            int peakR = Un4seen.Bass.Utils.HighWord32(level);

            double dbLeft = Un4seen.Bass.Utils.LevelToDB(peakL, 65535);
            double dbRight = Un4seen.Bass.Utils.LevelToDB(peakR, 65535);

            dbLevelL = dbLeft;
            dbLevelR = dbRight;
        }

        public System.Collections.Generic.IList<string> GetOutputDevices()
        {
            Un4seen.Bass.BASS_DEVICEINFO[] soundDeviceDescriptions = Un4seen.Bass.Bass.BASS_GetDeviceInfos();

            System.Collections.Generic.List<string> deviceList = (from a in soundDeviceDescriptions select a.name).ToList();

            return deviceList;
        }

        private void ChangeOutputDevice(string newOutputDevice)
        {
            if (newOutputDevice == null)
                throw new BassException("Null value provided to ChangeOutputDevice(string)");

            // Attempt to find the device number for the given string
            int oldDeviceId = Un4seen.Bass.Bass.BASS_GetDevice();
            int newDeviceId = -1;
            Un4seen.Bass.BASS_DEVICEINFO[] soundDeviceDescriptions = Un4seen.Bass.Bass.BASS_GetDeviceInfos();
            for (int i = 0; i < soundDeviceDescriptions.Length; i++)
            {
                if (newOutputDevice.Equals(soundDeviceDescriptions[i].name))
                    newDeviceId = i;
            }
            if (newDeviceId == -1)
                throw new BassException("Cannot find an output device matching description [" + newOutputDevice + "]");

            Log.Info("BASS: Old device ID " + oldDeviceId);
            Log.Info("BASS: New device ID " + newDeviceId);

            // Make sure we're actually changing devices
            if (oldDeviceId == newDeviceId) return;

            // Initialize the new device
            Un4seen.Bass.BASS_DEVICEINFO info = Un4seen.Bass.Bass.BASS_GetDeviceInfo(newDeviceId);
            if (!info.IsInitialized)
            {
                Log.Info("BASS: Initializing new device ID " + newDeviceId);
                bool initOk = Un4seen.Bass.Bass.BASS_Init(newDeviceId, 44100, Un4seen.Bass.BASSInit.BASS_DEVICE_DEFAULT | Un4seen.Bass.BASSInit.BASS_DEVICE_LATENCY,
                    System.IntPtr.Zero);
                if (!initOk)
                {
                    Un4seen.Bass.BASSError error = Un4seen.Bass.Bass.BASS_ErrorGetCode();
                    throw new BassException("Cannot initialize output device [" + newOutputDevice + "], error is [" +
                                            System.Enum.GetName(typeof (Un4seen.Bass.BASSError), error) + "]");
                }
            }

            // If anything is playing, move the stream to the new output device
            if (State == PlayState.Playing)
            {
                Log.Info("BASS: Moving current stream to new device ID " + newDeviceId);
                int stream = GetCurrentStream();
                Un4seen.Bass.Bass.BASS_ChannelSetDevice(stream, newDeviceId);
            }

            // If the previous device was init'd, free it
            if (oldDeviceId >= 0)
            {
                info = Un4seen.Bass.Bass.BASS_GetDeviceInfo(oldDeviceId);
                if (info.IsInitialized)
                {
                    Log.Info("BASS: Freeing device " + oldDeviceId);
                    Un4seen.Bass.Bass.BASS_SetDevice(oldDeviceId);
                    Un4seen.Bass.Bass.BASS_Free();
                    Un4seen.Bass.Bass.BASS_SetDevice(newDeviceId);
                }
            }

            _soundDevice = newOutputDevice;
        }

        #endregion
    }
}