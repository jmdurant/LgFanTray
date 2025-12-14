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
        private const byte FanModeRegister = 0xCF;
        private const byte BacklightRegister = 0x72;
        private const string EcProbeFileName = "ec-probe.exe";
        private const string PipeName = "LgFanTray_Pipe";

        private readonly NotifyIcon notifyIcon;
        private readonly ToolStripMenuItem fanLowItem;
        private readonly ToolStripMenuItem fanNormalItem;
        private readonly ToolStripMenuItem fanHighItem;
        private readonly ToolStripMenuItem fanMaxItem;
        private readonly ToolStripMenuItem backlightOffItem;
        private readonly ToolStripMenuItem backlightLowItem;
        private readonly ToolStripMenuItem backlightHighItem;

        private readonly HotkeyManager hotkeyManager;
        private int hotkeyFanLowId;
        private int hotkeyFanNormalId;
        private int hotkeyFanHighId;
        private int hotkeyFanMaxId;
        private int hotkeyBacklightOffId;
        private int hotkeyBacklightLowId;
        private int hotkeyBacklightHighId;

        private Thread pipeThread;
        private volatile bool running = true;

        public TrayApplicationContext(FanMode? initialMode = null)
        {
            var menu = new ContextMenuStrip();

            // Fan controls
            var fanMenu = new ToolStripMenuItem("Fan Speed");
            fanLowItem = new ToolStripMenuItem("Low (Ctrl+Alt+1)", null, (s, e) => SetFanMode(FanMode.Low));
            fanNormalItem = new ToolStripMenuItem("Normal (Ctrl+Alt+2)", null, (s, e) => SetFanMode(FanMode.Normal));
            fanHighItem = new ToolStripMenuItem("High (Ctrl+Alt+3)", null, (s, e) => SetFanMode(FanMode.High));
            fanMaxItem = new ToolStripMenuItem("Max (Ctrl+Alt+4)", null, (s, e) => SetFanMode(FanMode.Max));
            fanMenu.DropDownItems.Add(fanLowItem);
            fanMenu.DropDownItems.Add(fanNormalItem);
            fanMenu.DropDownItems.Add(fanHighItem);
            fanMenu.DropDownItems.Add(fanMaxItem);

            // Backlight controls
            var backlightMenu = new ToolStripMenuItem("Keyboard Backlight");
            backlightOffItem = new ToolStripMenuItem("Off (Ctrl+Alt+5)", null, (s, e) => SetBacklight(BacklightLevel.Off));
            backlightLowItem = new ToolStripMenuItem("Low (Ctrl+Alt+6)", null, (s, e) => SetBacklight(BacklightLevel.Low));
            backlightHighItem = new ToolStripMenuItem("High (Ctrl+Alt+7)", null, (s, e) => SetBacklight(BacklightLevel.High));
            backlightMenu.DropDownItems.Add(backlightOffItem);
            backlightMenu.DropDownItems.Add(backlightLowItem);
            backlightMenu.DropDownItems.Add(backlightHighItem);

            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Exit());

            menu.Items.Add(fanMenu);
            menu.Items.Add(backlightMenu);
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
                SetFanMode(initialMode.Value);
            }
            else
            {
                RefreshCheckedMode();
            }
        }

        private void RegisterHotkeys()
        {
            int modifiers = HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_ALT;

            // Fan hotkeys
            try { hotkeyFanLowId = hotkeyManager.Register(modifiers, Keys.D1); } catch { }
            try { hotkeyFanNormalId = hotkeyManager.Register(modifiers, Keys.D2); } catch { }
            try { hotkeyFanHighId = hotkeyManager.Register(modifiers, Keys.D3); } catch { }
            try { hotkeyFanMaxId = hotkeyManager.Register(modifiers, Keys.D4); } catch { }

            // Backlight hotkeys
            try { hotkeyBacklightOffId = hotkeyManager.Register(modifiers, Keys.D5); } catch { }
            try { hotkeyBacklightLowId = hotkeyManager.Register(modifiers, Keys.D6); } catch { }
            try { hotkeyBacklightHighId = hotkeyManager.Register(modifiers, Keys.D7); } catch { }
        }

        private void OnHotkeyPressed(object sender, int id)
        {
            if (id == hotkeyFanLowId) SetFanMode(FanMode.Low);
            else if (id == hotkeyFanNormalId) SetFanMode(FanMode.Normal);
            else if (id == hotkeyFanHighId) SetFanMode(FanMode.High);
            else if (id == hotkeyFanMaxId) SetFanMode(FanMode.Max);
            else if (id == hotkeyBacklightOffId) SetBacklight(BacklightLevel.Off);
            else if (id == hotkeyBacklightLowId) SetBacklight(BacklightLevel.Low);
            else if (id == hotkeyBacklightHighId) SetBacklight(BacklightLevel.High);
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
                                notifyIcon.ContextMenuStrip.Invoke((Action)(() => SetFanMode(mode)));
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
            var ec = new EcProbeClient(EcProbeFileName);
            byte fanVal;
            byte backlightVal;
            string error;

            // Read fan mode
            if (ec.TryReadRegister(FanModeRegister, out fanVal, out error))
            {
                fanLowItem.Checked = fanVal == (byte)FanMode.Low;
                fanNormalItem.Checked = fanVal == (byte)FanMode.Normal;
                fanHighItem.Checked = fanVal == (byte)FanMode.High;
                fanMaxItem.Checked = fanVal == (byte)FanMode.Max;
            }

            // Read backlight level
            if (ec.TryReadRegister(BacklightRegister, out backlightVal, out error))
            {
                backlightOffItem.Checked = backlightVal == (byte)BacklightLevel.Off;
                backlightLowItem.Checked = backlightVal == (byte)BacklightLevel.Low;
                backlightHighItem.Checked = backlightVal == (byte)BacklightLevel.High;
            }

            notifyIcon.Text = string.Format("LG Fan Tray (Fan=0x{0:X2}, Light=0x{1:X2})", fanVal, backlightVal);
        }

        private void SetFanMode(FanMode mode)
        {
            string output;
            string error;
            var ec = new EcProbeClient(EcProbeFileName);

            if (!ec.TryWriteRegister(FanModeRegister, (byte)mode, out output, out error))
            {
                notifyIcon.ShowBalloonTip(3000, "LG Fan Tray", error ?? "Write failed", ToolTipIcon.Error);
                return;
            }

            RefreshCheckedMode();
        }

        private void SetBacklight(BacklightLevel level)
        {
            string output;
            string error;
            var ec = new EcProbeClient(EcProbeFileName);

            if (!ec.TryWriteRegister(BacklightRegister, (byte)level, out output, out error))
            {
                notifyIcon.ShowBalloonTip(3000, "LG Fan Tray", error ?? "Write failed", ToolTipIcon.Error);
                return;
            }

            RefreshCheckedMode();
        }

        private void Exit()
        {
            running = false;

            // Unregister fan hotkeys
            if (hotkeyFanLowId != 0) hotkeyManager.Unregister(hotkeyFanLowId);
            if (hotkeyFanNormalId != 0) hotkeyManager.Unregister(hotkeyFanNormalId);
            if (hotkeyFanHighId != 0) hotkeyManager.Unregister(hotkeyFanHighId);
            if (hotkeyFanMaxId != 0) hotkeyManager.Unregister(hotkeyFanMaxId);

            // Unregister backlight hotkeys
            if (hotkeyBacklightOffId != 0) hotkeyManager.Unregister(hotkeyBacklightOffId);
            if (hotkeyBacklightLowId != 0) hotkeyManager.Unregister(hotkeyBacklightLowId);
            if (hotkeyBacklightHighId != 0) hotkeyManager.Unregister(hotkeyBacklightHighId);

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
