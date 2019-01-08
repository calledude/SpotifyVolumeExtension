using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    class ConsoleController
    {
        static NotifyIcon notifyIcon = new NotifyIcon();
        static bool Visible = true;

        private delegate bool EventHandler();
        static EventHandler _handler;

        public ConsoleController()
        {
            Console.Title = "SpotifyVolumeExtension";
            notifyIcon.DoubleClick += (s, e) =>
            {
                ToggleVisibility(s, e);
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, ToggleVisibility);
            contextMenu.Items.Add("Exit", null, CleanExit);

            notifyIcon.ContextMenuStrip = contextMenu;

            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
        }

        public void Start()
        {
            ToggleVisibility(null, null);
            Application.Run();
        }

        private static void CleanExit(object sender, EventArgs e)
        {
            Handler();
            Environment.Exit(0);
        }

        private static void ToggleVisibility(object sender, EventArgs e)
        {
            Visible = !Visible;
            if (Visible) notifyIcon.ContextMenuStrip.Items[0].Text = "Hide";
            else notifyIcon.ContextMenuStrip.Items[0].Text = "Show";
            
            SetConsoleWindowVisibility(Visible);
        }

        private static bool Handler()
        {
            notifyIcon.Dispose();
            Application.Exit();
            return true;
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void SetConsoleWindowVisibility(bool visible)
        {
            IntPtr hWnd = FindWindow(null, Console.Title);
            if (hWnd != IntPtr.Zero)
            {
                if (visible) ShowWindow(hWnd, 1); //1 = SW_SHOWNORMAL           
                else ShowWindow(hWnd, 0); //0 = SW_HIDE               
            }
        }
    }
}
