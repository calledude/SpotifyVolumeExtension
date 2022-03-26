using H.Hooks;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension;

public sealed class SpotifyVolumeController : VolumeController
{
	private readonly MediaKeyListener _mkl;
	private readonly StatusController _statusController;
	private readonly SpotifyClient _spotifyClient;
	private int _lastVolume;

	public SpotifyVolumeController(SpotifyClient sc, StatusController statusController)
	{
		_statusController = statusController;
		_spotifyClient = sc;
		_mkl = new MediaKeyListener();

		var debounceConfig = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250));
		_mkl.SubscribeTo(Key.VolumeUp, debounceConfig);
		_mkl.SubscribeTo(Key.VolumeDown, debounceConfig);
	}

	protected override async Task Start()
	{
		await base.Start();

		_mkl.Run();

		_lastVolume = BaselineVolume;
		_mkl.SubscribedKeyPressed += VolumeKeyPressed;
		_statusController.VolumeReport += OnVolumeReport;
	}

	protected override void Stop()
	{
		_mkl.SubscribedKeyPressed -= VolumeKeyPressed;
		_statusController.VolumeReport -= OnVolumeReport;
		base.Stop();
	}

	//Spotify web api might not be fast enough to realize we have begun playing music
	//therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
	protected override async Task<int> GetBaselineVolume()
	{
		var playbackContext = await Retry.Wrap(() => _spotifyClient.Api.GetPlaybackAsync());
		while (playbackContext?.Device == null)
		{
			await Task.Delay(500);
			playbackContext = await Retry.Wrap(() => _spotifyClient.Api.GetPlaybackAsync());
		}
		return playbackContext.Device.VolumePercent;
	}

	private async Task VolumeKeyPressed(MediaKeyEventArgs m)
	{
		if (m.Key == Key.VolumeUp) BaselineVolume += m.Presses;
		else BaselineVolume -= m.Presses;

		BaselineVolume = Math.Clamp(BaselineVolume, 0, 100);

		await SetNewVolume();
	}

	private void OnVolumeReport(int volume)
	{
		// Ugly hack because the Spotify API is still inconsistent with volume percentage.
		// You can set the volume to 23, later fetch it and Spotify reports it as being set to 22.
		// So we just ignore any manual volume changes that are less than 1 percentage points.
		// I guess in the grand scheme of things it's not _that_ big of a loss, still annoying though
		if (Math.Abs(volume - BaselineVolume) <= 1)
			return;

		Console.WriteLine($"[{Name}] Manual volume change detected. {BaselineVolume}% -> {volume}%");
		BaselineVolume = _lastVolume = volume;
	}

	protected override async Task SetNewVolume()
	{
		if (_lastVolume == BaselineVolume)
			return;

		var err = await Retry.Wrap(() => _spotifyClient.Api.SetVolumeAsync(BaselineVolume));

		if (err != null && err.Error == null)
		{
			Console.WriteLine($"[{Name}] Changed volume to {BaselineVolume}%");
			_lastVolume = BaselineVolume;
		}
		else
		{
			Console.WriteLine($"[{Name}] Failed to change volume.");
			BaselineVolume = _lastVolume;
		}
	}

	protected override void Dispose(bool disposing)
		=> _mkl.Dispose();
}
