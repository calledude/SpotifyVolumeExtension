using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
	public static class ConsoleController
	{
		private static readonly List<IDisposable> _disposables = new();

		private static readonly NotifyIcon _notifyIcon = new()
		{
			ContextMenuStrip = new ContextMenuStrip
			{
				Items =
				{
					{ "Show", null, ToggleVisibility },
					{ "Exit", null, CleanExit }
				}
			},

			Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
			Visible = true,
			Text = Application.ProductName
		};

		private static bool _visible = true;

		private delegate bool EventHandler();

		private static readonly EventHandler _handler = new(Handler);

		static ConsoleController()
		{
			Console.Title = "SpotifyVolumeExtension";
			_notifyIcon.DoubleClick += ToggleVisibility;

			SetConsoleCtrlHandler(_handler, true);
		}

		public static void Start()
			=> Application.Run();

		public static void RegisterDisposables(params IDisposable[] disposables)
			=> _disposables.AddRange(disposables);

		private static void CleanExit(object sender, EventArgs e)
		{
			Handler();
			Environment.Exit(0);
		}

		public static void Hide()
		{
			if (!_visible)
				return;

			ToggleVisibility(null, null);
		}

		private static void ToggleVisibility(object sender, EventArgs e)
		{
			_visible = !_visible;
			if (_visible)
				_notifyIcon.ContextMenuStrip.Items[0].Text = "Hide";
			else
				_notifyIcon.ContextMenuStrip.Items[0].Text = "Show";

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
			var hWnd = FindWindow(null, Console.Title);
			if (hWnd == IntPtr.Zero)
				return;

			ShowWindow(hWnd, visible ? 1 : 0);
			SetForegroundWindow(hWnd);
		}

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);
	}
}
