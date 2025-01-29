using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace CAPSLOCK
{
public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private LowLevelKeyboardHook keyboardHook;

        public Form1()
        {
            InitializeComponent();

            // Inicjalizacja NotifyIcon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Caps Lock Status";
            trayIcon.Visible = true;

            // Inicjalizacja ContextMenuStrip
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnExit);
            trayIcon.ContextMenuStrip = trayMenu;

            // Inicjalizacja globalnego hooka klawiatury
            keyboardHook = new LowLevelKeyboardHook();
            keyboardHook.OnKeyPressed += KeyboardHook_OnKeyPressed;
            keyboardHook.HookKeyboard();
        }

        private Icon CreateIcon(char Character, Color color)
        {
            // Create a 16x16 bitmap
            using (Bitmap bitmap = new Bitmap(16, 16))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Clear the background
                    graphics.Clear(Color.Transparent);

                    // Draw a "C" in the specified color
                    using (Font font = new Font("Calibri", 16, FontStyle.Bold))
                    using (Brush brush = new SolidBrush(color))
                    {
                        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        graphics.DrawString(Character.ToString(), font, brush, new PointF(-2, -6)); // Adjust position as needed
                    }
                }

                // Convert the bitmap to an icon
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }

        private void KeyboardHook_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (e.KeyPressed == Key.CapsLock)
            {
                CheckCAPSLOCK();
            }
        }

        private void CheckCAPSLOCK()
        {

            bool isCapsLockOn = Control.IsKeyLocked(Keys.CapsLock);

            if (isCapsLockOn)
            {
                trayIcon.Icon = CreateIcon('A',Color.LawnGreen); // Green "A" for Caps Lock ON
                trayIcon.Text = "Caps Lock: ON";
            }
            else
            {
                trayIcon.Icon = CreateIcon('a',Color.Gray); // Gray "a" for Caps Lock OFF
                trayIcon.Text = "Caps Lock: OFF";
            }

        }

        private void OnExit(object sender, EventArgs e)
        {
            keyboardHook.UnHookKeyboard();
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Ukryj formularz
            ShowInTaskbar = false; // Nie pokazuj w taskbarze
            base.OnLoad(e);
            CheckCAPSLOCK();
        }

    }

    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<KeyPressedArgs> OnKeyPressed;

        public LowLevelKeyboardHook()
        {
            _proc = HookCallback;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                OnKeyPressed?.Invoke(this, new KeyPressedArgs(key));
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public class KeyPressedArgs : EventArgs
    {
        public Key KeyPressed { get; private set; }

        public KeyPressedArgs(Key key)
        {
            KeyPressed = key;
        }
    }
}
