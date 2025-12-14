using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LgFanTray
{
    public class HotkeyManager : NativeWindow, IDisposable
    {
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        private const int WM_HOTKEY = 0x0312;

        private int nextId = 1;

        public event EventHandler<int> HotkeyPressed;

        public HotkeyManager()
        {
            CreateHandle(new CreateParams());
        }

        public int Register(int modifiers, Keys key)
        {
            int id = nextId++;

            if (!RegisterHotKey(Handle, id, modifiers, (int)key))
            {
                throw new InvalidOperationException("Could not register hotkey");
            }

            return id;
        }

        public void Unregister(int id)
        {
            UnregisterHotKey(Handle, id);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                HotkeyPressed?.Invoke(this, id);
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            try
            {
                DestroyHandle();
            }
            catch
            {
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
