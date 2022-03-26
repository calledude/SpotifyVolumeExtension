using Nito.AsyncEx;
using SpotifyAPI.Web.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension;

public sealed class SpotifyMonitor : IDisposable
{
	public StatusController StatusController { get; }

	private readonly SpotifyClient _spotifyClient;
	private readonly ProcessService _processService;
	private readonly AsyncMonitor _start;
	private CancellationTokenSource _cts;
	private readonly AsyncManualResetEvent _failure;
	private Task _pollTask;

	public SpotifyMonitor(SpotifyClient sc)
	{
		_processService = new ProcessService("Spotify");
		_processService.Exited += SpotifyExited;
		_start = new AsyncMonitor();
		_failure = new AsyncManualResetEvent(false);
		StatusController = new StatusController(this);

		_spotifyClient = sc ?? throw new ArgumentNullException(nameof(sc));
		sc.NoActivePlayer += CheckState;
	}

	public async Task Start()
	{
		Log("Waiting for Spotify to start...");
		await _processService.WaitForProcessToStart();
		Log("Spotify process detected.");

		await _spotifyClient.SetAutoRefresh(true);

		Log("Waiting for music to start playing.");
		if (!await TryWaitForPlaybackActivation())
			return;

		Log("Started. Now monitoring activity.");

		_cts = new CancellationTokenSource();
		_pollTask = Task.WhenAny(PollSpotifyStatus(), Task.Delay(Timeout.Infinite, _cts.Token));
	}

	private async Task<bool> TryWaitForPlaybackActivation()
	{
		double sleep = 1;

		while (!await StatusController.CheckStateImmediate())
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

	private static void Log(string message)
		=> Console.WriteLine($"[{nameof(SpotifyMonitor)}] {message}");

	private void CheckState()
		=> StatusController.CheckState();

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

	public async Task<PlaybackContext> GetPlaybackContext()
		=> await _spotifyClient.Api.GetPlaybackAsync();

	public async Task<bool> GetPlayingStatus()
		=> SpotifyIsRunning() && await IsPlayingMusic();

	public bool SpotifyIsRunning()
		=> _processService.ProcessIsRunning();

	private async Task<bool> IsPlayingMusic()
	{
		var pb = await Retry.Wrap(() => GetPlaybackContext());
		return pb?.IsPlaying ?? false;
	}

	public void Dispose()
	{
		_spotifyClient.Dispose();
		_cts?.Dispose();
		StatusController.Dispose();
	}
}
