using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SpotifyVolumeExtension.Spotify;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Monitoring;

public sealed class SpotifyMonitor : IDisposable
{
	private readonly StatusController _statusController;
	private readonly SpotifyClient _spotifyClient;
	private readonly ILogger<SpotifyMonitor> _logger;
	private readonly ProcessMonitorService _processMonitorService;
	private readonly AsyncMonitor _start;
	private CancellationTokenSource _cts;
	private readonly AsyncManualResetEvent _failure;
	private Task _pollTask;

	public SpotifyMonitor(
		SpotifyClient spotifyClient,
		ProcessMonitorService processMonitorService,
		AsyncMonitor asyncMonitor,
		StatusController statusController,
		ILogger<SpotifyMonitor> logger)
	{
		_logger = logger;

		_processMonitorService = processMonitorService;
		_processMonitorService.Exited += SpotifyExited;
		_start = asyncMonitor;
		_statusController = statusController;

		_spotifyClient = spotifyClient;
		_spotifyClient.NoActivePlayer += CheckState;

		_failure = new AsyncManualResetEvent(false);
	}

	public async Task Start()
	{
		_logger.LogInformation("Waiting for Spotify to start...");
		await _processMonitorService.WaitForProcessToStart();
		_logger.LogInformation("Spotify process detected.");

		await _spotifyClient.SetAutoRefresh(true);

		_logger.LogInformation("Waiting for music to start playing.");
		if (!await TryWaitForPlaybackActivation())
			return;

		_logger.LogInformation("Started. Now monitoring activity.");

		_cts = new CancellationTokenSource();
		_pollTask = Task.WhenAny(PollSpotifyStatus(), Task.Delay(Timeout.Infinite, _cts.Token));
	}

	private async Task<bool> TryWaitForPlaybackActivation()
	{
		double sleep = 1;

		while (!await _statusController.CheckStateImmediate())
		{
			var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(500 * sleep));
			var failureWaitTask = _failure.WaitAsync();
			var completedTask = await Task.WhenAny(timeoutTask, failureWaitTask);

			if (completedTask == failureWaitTask)
				return false;
			if (sleep < 20)
				sleep *= 1.35;
		}

		return true;
	}

	//Checks if Spotify is running/playing music
	//if the status changes, subscribers (Volume controllers) to the event are alerted.
	private async Task PollSpotifyStatus()
	{
		do
		{
			CheckState();
			await Task.Delay(15000);
		} while (!_cts.IsCancellationRequested);
	}

	private void CheckState()
		=> _statusController.CheckState();

	private async void SpotifyExited(object sender, EventArgs e)
	{
		CheckState();

		_cts?.Cancel();

		if (_pollTask != null)
		{
			await _pollTask;
			_pollTask.Dispose();
		}

		_failure.Set();

		await _spotifyClient.SetAutoRefresh(false);

		using (_ = await _start.EnterAsync())
		{
			_failure.Reset();
			await Start();
		}
	}

	public async Task<bool> GetPlayingStatus()
		=> SpotifyIsRunning() && await IsPlayingMusic();

	public bool SpotifyIsRunning()
		=> _processMonitorService.ProcessIsRunning();

	private async Task<bool> IsPlayingMusic()
	{
		var pb = await _spotifyClient.GetPlaybackContext();
		return pb?.IsPlaying ?? false;
	}

	public void Dispose()
	{
		_spotifyClient.Dispose();
		_cts?.Dispose();
		_statusController.Dispose();
	}
}
