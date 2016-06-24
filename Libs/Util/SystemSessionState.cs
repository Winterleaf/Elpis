namespace Util
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
            }
        }
    }
}