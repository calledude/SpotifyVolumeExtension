using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SpotifyVolumeExtension;

public static partial class ConsoleController
{
	private static readonly List<IDisposable> _disposables = [];

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

	static ConsoleController()
	{
		Console.Title = "SpotifyVolumeExtension";
		_notifyIcon.DoubleClick += ToggleVisibility;

		_ = NativeMethods.SetConsoleCtrlHandler(Handler, true);
	}

	public static void Start()
		=> Application.Run();

	public static void RegisterDisposables(params IDisposable[] disposables)
		=> _disposables.AddRange(disposables);

	private static void CleanExit(object? sender, EventArgs e)
	{
		Handler();
		Environment.Exit(0);
	}

	public static void Hide()
	{
		if (!_visible)
			return;

		ToggleVisibility(null, EventArgs.Empty);
	}

	private static void ToggleVisibility(object? sender, EventArgs e)
	{
		_visible = !_visible;
		_notifyIcon.ContextMenuStrip!.Items[0].Text = _visible ? "Hide" : "Show";
		NativeMethods.SetConsoleWindowVisibility(_visible);
	}

#pragma warning disable S3241 // Methods should not return values that are never used
	// This method signature looks exactly like this for a reason
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
#pragma warning restore S3241 // Methods should not return values that are never used

	private static partial class NativeMethods
	{
		[LibraryImport("Kernel32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool SetConsoleCtrlHandler(EventHandler handler, [MarshalAs(UnmanagedType.Bool)] bool add);

		[LibraryImport("user32.dll", EntryPoint = "FindWindowA", StringMarshalling = StringMarshalling.Utf16)]
		private static partial IntPtr FindWindow(string? lpClassName, string lpWindowName);

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

		public static void SetConsoleWindowVisibility(bool visible)
		{
			var hWnd = FindWindow(null, Console.Title);
			if (hWnd == IntPtr.Zero)
				return;

			ShowWindow(hWnd, visible ? 1 : 0);
			SetForegroundWindow(hWnd);
		}

		[LibraryImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool SetForegroundWindow(IntPtr hWnd);
	}
}
