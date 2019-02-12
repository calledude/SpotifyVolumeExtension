using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    public sealed class ConsoleController
    {
        private static readonly NotifyIcon notifyIcon = new NotifyIcon();
        private static bool Visible = true;

        private delegate bool EventHandler();
        private static EventHandler _handler;

        public ConsoleController()
        {
            Console.Title = "SpotifyVolumeExtension";
            notifyIcon.DoubleClick += ToggleVisibility;

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
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void SetConsoleWindowVisibility(bool visible)
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
