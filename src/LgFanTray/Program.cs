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

        [STAThread]
        public static void Main(string[] args)
        {
            FanMode? requestedMode = ParseArgs(args);

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (createdNew)
                {
                    // First instance - run the tray app
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayApplicationContext(requestedMode));
                }
                else
                {
                    // Another instance is running - send command via pipe
                    if (requestedMode.HasValue)
                    {
                        SendCommandToRunningInstance(requestedMode.Value);
                    }
                }
            }
        }

        private static FanMode? ParseArgs(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            string arg = args[0].ToLowerInvariant().TrimStart('-', '/');

            switch (arg)
            {
                case "1":
                case "low":
                    return FanMode.Low;
                case "2":
                case "normal":
                    return FanMode.Normal;
                case "3":
                case "high":
                    return FanMode.High;
                case "4":
                case "max":
                    return FanMode.Max;
                case "applydefault":
                    return FanMode.Max; // Default mode on startup
                default:
                    return null;
            }
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
