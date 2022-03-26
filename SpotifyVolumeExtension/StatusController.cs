using H.Hooks;
using Nito.AsyncEx;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

namespace SpotifyVolumeExtension;

public sealed class StatusController : IDisposable
{
	private bool _lastState;
	private readonly SpotifyMonitor _sm;
	private readonly MediaKeyListener _mkl;
	private readonly ConcurrentQueue<Func<Task>> _apiCallQueue;
	private readonly Timer _queueTimer;
	private readonly AsyncMonitor _startLock;

	public event Action<int> VolumeReport;

	public StatusController(SpotifyMonitor sm)
	{
		_startLock = new AsyncMonitor();
		_apiCallQueue = new ConcurrentQueue<Func<Task>>();
		_queueTimer = new Timer(500);
		_queueTimer.Elapsed += RunQueuedApiCalls;
		_queueTimer.Enabled = true;

		_mkl = new MediaKeyListener();
		_mkl.Run();

		_mkl.SubscribeTo(Key.MediaPlayPause);
		_mkl.SubscribeTo(Key.MediaStop);
		_mkl.SubscribedKeyPressed += CheckStateInternal;

		_sm = sm ?? throw new ArgumentNullException(nameof(sm));
	}

	private async void RunQueuedApiCalls(object sender, ElapsedEventArgs e)
	{
		if (_apiCallQueue.TryDequeue(out var task))
		{
			await task.Invoke();
		}
	}

	private async Task CheckStateInternal(MediaKeyEventArgs m)
	{
		var newState = m.Key switch
		{
			Key.MediaPlayPause => !_lastState,
			Key.MediaStop => false,
			// This can quite literally never happen but the compiler won't shut up about it
			_ => throw new InvalidOperationException()
		};

		// Announce state change, wait 500ms, check again to make sure it was the correct one
		// This is a bit of a hack to enable more responsive volume-lock toggling
		await OnStateChange(newState, null);

		await Task.Delay(500);
		CheckState();
	}

	public async Task<bool> CheckStateImmediate()
	{
		if (_sm.SpotifyIsRunning())
		{
			var playbackContext = await _sm.GetPlaybackContext();
			await OnStateChange(playbackContext.IsPlaying, playbackContext);
		}
		else
		{
			await OnStateChange(false, null);
		}

		return _lastState;
	}

	public void CheckState()
	{
		if (!_apiCallQueue.IsEmpty)
			return;

		_apiCallQueue.Enqueue(CheckStateImmediate);
	}

	private async Task OnStateChange(bool newState, PlaybackContext context)
	{
		using (_ = await _startLock.EnterAsync())
		{
			if (context?.Device != default)
				VolumeReport?.Invoke(context.Device.VolumePercent);

			if (newState == _lastState)
				return;

			_lastState = newState;
			if (newState)
			{
				await VolumeController.StartAll();
			}
			else
			{
				VolumeController.StopAll();
			}
		}
	}

	public void Dispose()
	{
		_mkl.Dispose();
		_queueTimer.Dispose();
	}
}
