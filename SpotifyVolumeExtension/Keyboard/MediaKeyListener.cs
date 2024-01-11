using H.Hooks;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Keyboard;

public sealed class MediaKeyListener : IDisposable
{
	public event Func<MediaKeyEventArgs, Task>? SubscribedKeyPressed;

	private int _presses;
	private readonly Dictionary<Key, (TimeSpan, TimeSpan)> _debounceConfig;
	private DateTime _lastEvent;
	private readonly LowLevelKeyboardHook _keyboardHook;

	private readonly AsyncMonitor _lock;

	public MediaKeyListener(AsyncMonitor asyncMonitor, LowLevelKeyboardHook keyboardHook)
	{
		_lock = asyncMonitor;
		_debounceConfig = [];
		_keyboardHook = keyboardHook;
		_keyboardHook.OneUpEvent = false;
		_keyboardHook.Down += OnKeyDown;
		_keyboardHook.Up += OnKeyUp;
	}

	public void Run() => _keyboardHook.Start();

	private void OnKeyDown(object? sender, KeyboardEventArgs e)
	{
		if (!_debounceConfig.ContainsKey(e.CurrentKey))
			return;

		using var _ = _lock.Enter();

		++_presses;
	}

	private async void OnKeyUp(object? sender, KeyboardEventArgs e)
	{
		if (!_debounceConfig.TryGetValue(e.CurrentKey, out var value))
			return;

		var (minimumWait, penalty) = value;
		if (minimumWait != default
			&& penalty != default
			&& DateTime.UtcNow - _lastEvent < minimumWait)
		{
			await Task.Delay(penalty);
		}

		using var _ = await _lock.EnterAsync();

		if (SubscribedKeyPressed != null)
		{
			_lastEvent = DateTime.UtcNow;
			await SubscribedKeyPressed.Invoke(new MediaKeyEventArgs()
			{
				Presses = _presses,
				Key = e.CurrentKey
			});
		}

		_presses = 0;
	}

	public void SubscribeTo(Key key, (TimeSpan, TimeSpan) debounceConfig = default)
		=> _debounceConfig.Add(key, debounceConfig);

	public void Dispose()
		=> _keyboardHook.Dispose();
}
