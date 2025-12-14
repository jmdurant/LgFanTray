using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;

namespace LgFanTray
{
    public class Program
    {
        private const string MutexName = "LgFanTray_SingleInstance";
        private const string PipeName = "LgFanTray_Pipe";
        private const byte BacklightRegister = 0x72;
        private const string EcProbeFileName = "ec-probe.exe";

        [STAThread]
        public static void Main(string[] args)
        {
            FanMode? requestedFanMode;
            BacklightLevel? requestedBacklight;
            ParseArgs(args, out requestedFanMode, out requestedBacklight);

            // Handle backlight-only commands without starting the tray app
            if (requestedBacklight.HasValue && !requestedFanMode.HasValue)
            {
                SetBacklightDirect(requestedBacklight.Value);
                return;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (createdNew)
                {
                    // First instance - run the tray app
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayApplicationContext(requestedFanMode));
                }
                else
                {
                    // Another instance is running - send command via pipe
                    if (requestedFanMode.HasValue)
                    {
                        SendCommandToRunningInstance(requestedFanMode.Value);
                    }
                }
            }
        }

        private static void ParseArgs(string[] args, out FanMode? fanMode, out BacklightLevel? backlight)
        {
            fanMode = null;
            backlight = null;

            if (args == null || args.Length == 0)
            {
                return;
            }

            string arg = args[0].ToLowerInvariant().TrimStart('-', '/');

            switch (arg)
            {
                // Fan modes
                case "1":
                case "fan1":
                case "fanlow":
                    fanMode = FanMode.Low;
                    break;
                case "2":
                case "fan2":
                case "fannormal":
                    fanMode = FanMode.Normal;
                    break;
                case "3":
                case "fan3":
                case "fanhigh":
                    fanMode = FanMode.High;
                    break;
                case "4":
                case "fan4":
                case "fanmax":
                    fanMode = FanMode.Max;
                    break;
                case "applydefault":
                    fanMode = FanMode.Max;
                    break;

                // Backlight levels
                case "5":
                case "lightoff":
                case "backlightoff":
                    backlight = BacklightLevel.Off;
                    break;
                case "6":
                case "lightlow":
                case "backlightlow":
                    backlight = BacklightLevel.Low;
                    break;
                case "7":
                case "lighthigh":
                case "backlighthigh":
                    backlight = BacklightLevel.High;
                    break;
            }
        }

        private static void SetBacklightDirect(BacklightLevel level)
        {
            var ec = new EcProbeClient(EcProbeFileName);
            string output;
            string error;
            ec.TryWriteRegister(BacklightRegister, (byte)level, out output, out error);
        }

        private static void SendCommandToRunningInstance(FanMode mode)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(1000);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine(((int)mode).ToString());
                        writer.Flush();
                    }
                }
            }
            catch
            {
                // Ignore errors - running instance may not be listening yet
            }
        }
    }
}
