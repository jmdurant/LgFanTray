using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;

namespace LgFanTray
{
    public class TrayApplicationContext : ApplicationContext
    {
        private const byte ModeRegister = 0xCF;
        private const string EcProbeFileName = "ec-probe.exe";
        private const string PipeName = "LgFanTray_Pipe";

        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripMenuItem lowItem;
        private readonly ToolStripMenuItem normalItem;
        private readonly ToolStripMenuItem highItem;
        private readonly ToolStripMenuItem maxItem;

        private readonly HotkeyManager hotkeyManager;
        private int hotkeyLowId;
        private int hotkeyNormalId;
        private int hotkeyHighId;
        private int hotkeyMaxId;

        private Thread pipeThread;
        private volatile bool running = true;

        public TrayApplicationContext(FanMode? initialMode = null)
        {
            var menu = new ContextMenuStrip();

            lowItem = new ToolStripMenuItem("Low (Ctrl+Alt+1)", null, (s, e) => SetMode(FanMode.Low));
            normalItem = new ToolStripMenuItem("Normal (Ctrl+Alt+2)", null, (s, e) => SetMode(FanMode.Normal));
            highItem = new ToolStripMenuItem("High (Ctrl+Alt+3)", null, (s, e) => SetMode(FanMode.High));
            maxItem = new ToolStripMenuItem("Max (Ctrl+Alt+4)", null, (s, e) => SetMode(FanMode.Max));

            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Exit());

            menu.Items.Add(lowItem);
            menu.Items.Add(normalItem);
            menu.Items.Add(highItem);
            menu.Items.Add(maxItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            Icon icon = null;
            try { icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            if (icon == null) { icon = SystemIcons.Application; }

            notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                Text = "LG Fan Tray",
                ContextMenuStrip = menu
            };

            // Register hotkeys
            hotkeyManager = new HotkeyManager();
            hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            RegisterHotkeys();

            // Start pipe listener for IPC
            pipeThread = new Thread(PipeListenerThread);
            pipeThread.IsBackground = true;
            pipeThread.Start();

            // Apply initial mode if specified, otherwise refresh display
            if (initialMode.HasValue)
            {
                SetMode(initialMode.Value);
            }
            else
            {
                RefreshCheckedMode();
            }
        }

        private void RegisterHotkeys()
        {
            int modifiers = HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT;

            try { hotkeyLowId = hotkeyManager.Register(modifiers, Keys.D1); } catch { }
            try { hotkeyNormalId = hotkeyManager.Register(modifiers, Keys.D2); } catch { }
            try { hotkeyHighId = hotkeyManager.Register(modifiers, Keys.D3); } catch { }
            try { hotkeyMaxId = hotkeyManager.Register(modifiers, Keys.D4); } catch { }
        }

        private void OnHotkeyPressed(object sender, int id)
        {
            if (id == hotkeyLowId) SetMode(FanMode.Low);
            else if (id == hotkeyNormalId) SetMode(FanMode.Normal);
            else if (id == hotkeyHighId) SetMode(FanMode.High);
            else if (id == hotkeyMaxId) SetMode(FanMode.Max);
        }

        private void PipeListenerThread()
        {
            while (running)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                    {
                        server.WaitForConnection();

                        using (var reader = new StreamReader(server))
                        {
                            string line = reader.ReadLine();
                            int modeValue;
                            if (int.TryParse(line, out modeValue))
                            {
                                FanMode mode = (FanMode)modeValue;
                                notifyIcon.ContextMenuStrip.Invoke((Action)(() => SetMode(mode)));
                            }
                        }
                    }
                }
                catch
                {
                    if (running)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private void RefreshCheckedMode()
        {
            byte val;
            string error;
            var ec = new EcProbeClient(EcProbeFileName);

            if (!ec.TryReadRegister(ModeRegister, out val, out error))
            {
                notifyIcon.ShowBalloonTip(3000, "LG Fan Tray", error ?? "Read failed", ToolTipIcon.Error);
                return;
            }

            lowItem.Checked = val == (byte)FanMode.Low;
            normalItem.Checked = val == (byte)FanMode.Normal;
            highItem.Checked = val == (byte)FanMode.High;
            maxItem.Checked = val == (byte)FanMode.Max;

            notifyIcon.Text = "LG Fan Tray (0xCF=" + val.ToString("X2") + ")";
        }

        private void SetMode(FanMode mode)
        {
            string output;
            string error;
            var ec = new EcProbeClient(EcProbeFileName);

            if (!ec.TryWriteRegister(ModeRegister, (byte)mode, out output, out error))
            {
                notifyIcon.ShowBalloonTip(3000, "LG Fan Tray", error ?? "Write failed", ToolTipIcon.Error);
                return;
            }

            RefreshCheckedMode();
        }

        private void Exit()
        {
            running = false;

            // Unregister hotkeys
            if (hotkeyLowId != 0) hotkeyManager.Unregister(hotkeyLowId);
            if (hotkeyNormalId != 0) hotkeyManager.Unregister(hotkeyNormalId);
            if (hotkeyHighId != 0) hotkeyManager.Unregister(hotkeyHighId);
            if (hotkeyMaxId != 0) hotkeyManager.Unregister(hotkeyMaxId);

            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                running = false;

                if (hotkeyManager != null)
                {
                    hotkeyManager.Dispose();
                }

                if (notifyIcon != null)
                {
                    notifyIcon.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
