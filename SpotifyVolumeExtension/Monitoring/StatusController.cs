using H.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using SpotifyAPI.Web;
using SpotifyVolumeExtension.Keyboard;
using SpotifyVolumeExtension.Spotify;
using SpotifyVolumeExtension.Volume;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace SpotifyVolumeExtension.Monitoring;

public sealed class StatusController : IDisposable
{
	private bool _lastState;
	private readonly ProcessMonitorService _processMonitorService;
	private readonly SpotifyApiClient _spotifyClient;
	private readonly IServiceProvider _serviceProvider;
	private readonly MediaKeyListener _mediaKeyListener;
	private readonly ConcurrentQueue<Func<Task>> _apiCallQueue;
	private readonly Timer _queueTimer;
	private readonly AsyncMonitor _startLock;

	public event Action<int>? VolumeReport;

	public StatusController(
		ProcessMonitorService processMonitorService,
		SpotifyApiClient spotifyClient,
		AsyncMonitor asyncMonitor,
		MediaKeyListener mediaKeyListener,
		IServiceProvider serviceProvider)
	{
		_startLock = asyncMonitor;
		_apiCallQueue = [];
		_queueTimer = new(500);
		_queueTimer.Elapsed += RunQueuedApiCalls;
		_queueTimer.Enabled = true;

		_mediaKeyListener = mediaKeyListener;
		_mediaKeyListener.Run();

		_mediaKeyListener.SubscribeTo(Key.MediaPlayPause);
		_mediaKeyListener.SubscribeTo(Key.MediaStop);
		_mediaKeyListener.SubscribedKeyPressed += CheckStateInternal;

		_processMonitorService = processMonitorService;
		_spotifyClient = spotifyClient;
		_serviceProvider = serviceProvider;
	}

	private async void RunQueuedApiCalls(object? sender, ElapsedEventArgs e)
	{
		if (!_apiCallQueue.TryDequeue(out var task))
			return;

		await task.Invoke();
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
		if (_processMonitorService.ProcessIsRunning())
		{
			var playbackContext = await _spotifyClient.GetCurrentPlayback();
			await OnStateChange(playbackContext?.IsPlaying ?? _lastState, playbackContext);
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

	private async Task OnStateChange(bool newState, CurrentlyPlayingContext? context)
	{
		using (_ = await _startLock.EnterAsync())
		{
			if (context?.Device.VolumePercent is not null)
			{
				VolumeReport?.Invoke(context.Device.VolumePercent.Value);
			}

			if (newState == _lastState)
				return;

			_lastState = newState;
			var volumeControllers = _serviceProvider.GetServices<VolumeControllerBase>();
			if (newState)
			{
				await Task.WhenAll(volumeControllers.Select(x => x.Start()));
			}
			else
			{
				foreach (var vc in volumeControllers)
				{
					vc.Stop();
				}
			}
		}
	}

	public void Dispose()
	{
		_mediaKeyListener.Dispose();
		_queueTimer.Dispose();
	}
}
