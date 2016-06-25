using System;
using Microsoft.Win32;

namespace Elpis.Util
{
    public class SystemSessionState
    {
        public SystemSessionState()
        {
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        public delegate void SystemLockedEvent();

        public delegate void SystemUnlockedEvent();

        public event SystemLockedEvent SystemLocked;
        public event SystemUnlockedEvent SystemUnlocked;

        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case Microsoft.Win32.SessionSwitchReason.SessionLock:
                    SystemLocked?.Invoke();
                    break;
                case Microsoft.Win32.SessionSwitchReason.SessionUnlock:
                    SystemUnlocked?.Invoke();
                    break;
                case SessionSwitchReason.ConsoleConnect:
                    break;
                case SessionSwitchReason.ConsoleDisconnect:
                    break;
                case SessionSwitchReason.RemoteConnect:
                    break;
                case SessionSwitchReason.RemoteDisconnect:
                    break;
                case SessionSwitchReason.SessionLogon:
                    break;
                case SessionSwitchReason.SessionLogoff:
                    break;
                case SessionSwitchReason.SessionRemoteControl:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}