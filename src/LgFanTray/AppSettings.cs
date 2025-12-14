namespace LgFanTray
{
    public class AppSettings
    {
        public AppSettings()
        {
            EcProbePath = "ec-probe.exe";
            DefaultMode = FanMode.Max;
            ApplyDefaultOnStartup = true;

            HotkeyLowEnabled = true;
            HotkeyNormalEnabled = true;
            HotkeyHighEnabled = true;
            HotkeyMaxEnabled = true;

            HotkeyModifiers = HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT;
            HotkeyLowKey = (int)System.Windows.Forms.Keys.D1;
            HotkeyNormalKey = (int)System.Windows.Forms.Keys.D2;
            HotkeyHighKey = (int)System.Windows.Forms.Keys.D3;
            HotkeyMaxKey = (int)System.Windows.Forms.Keys.D4;
        }

        public int SettingsVersion { get; set; }

        public string EcProbePath { get; set; }

        public FanMode DefaultMode { get; set; }
        public bool ApplyDefaultOnStartup { get; set; }

        public bool HotkeyLowEnabled { get; set; }
        public bool HotkeyNormalEnabled { get; set; }
        public bool HotkeyHighEnabled { get; set; }
        public bool HotkeyMaxEnabled { get; set; }

        public int HotkeyModifiers { get; set; }
        public int HotkeyLowKey { get; set; }
        public int HotkeyNormalKey { get; set; }
        public int HotkeyHighKey { get; set; }
        public int HotkeyMaxKey { get; set; }
    }
}
