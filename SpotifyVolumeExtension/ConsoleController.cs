using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    public sealed class ConsoleController
    {
        private static readonly List<IDisposable> _disposables = new List<IDisposable>();
        private static readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private static bool _visible = true;

        private delegate bool EventHandler();
        private static EventHandler _handler;

        public ConsoleController()
        {
            Console.Title = "SpotifyVolumeExtension";
            _notifyIcon.DoubleClick += ToggleVisibility;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, ToggleVisibility);
            contextMenu.Items.Add("Exit", null, CleanExit);

            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _notifyIcon.Visible = true;
            _notifyIcon.Text = Application.ProductName;

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
        }

        public void Start()
        {
            ToggleVisibility(null, null);
            Application.Run();
        }

        public void RegisterDisposables(params IDisposable[] disposables)
        {
            _disposables.AddRange(disposables);
        }

        private static void CleanExit(object sender, EventArgs e)
        {
            Handler();
            Environment.Exit(0);
        }

        private static void ToggleVisibility(object sender, EventArgs e)
        {
            _visible = !_visible;
            if (_visible) _notifyIcon.ContextMenuStrip.Items[0].Text = "Hide";
            else _notifyIcon.ContextMenuStrip.Items[0].Text = "Show";

            SetConsoleWindowVisibility(_visible);
        }

        private static bool Handler()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _notifyIcon.Dispose();
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
