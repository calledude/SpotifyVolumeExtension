using H.Hooks;
using Microsoft.Extensions.Logging;
using SpotifyVolumeExtension.Keyboard;
using SpotifyVolumeExtension.Monitoring;
using SpotifyVolumeExtension.Spotify;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Volume;

public sealed class SpotifyVolumeController : VolumeControllerBase
{
	private readonly MediaKeyListener _mediaKeyListener;
	private readonly ILogger<SpotifyVolumeController> _logger;
	private readonly StatusController _statusController;
	private readonly SpotifyClient _spotifyClient;
	private int _lastVolume;

	public SpotifyVolumeController(
		SpotifyClient spotifyClient,
		StatusController statusController,
		MediaKeyListener mediaKeyListener,
		ILogger<SpotifyVolumeController> logger) : base(logger)
	{
		_statusController = statusController;
		_spotifyClient = spotifyClient;
		_mediaKeyListener = mediaKeyListener;
		_logger = logger;

		var debounceConfig = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250));
		_mediaKeyListener.SubscribeTo(Key.VolumeUp, debounceConfig);
		_mediaKeyListener.SubscribeTo(Key.VolumeDown, debounceConfig);
	}

	public override async Task Start()
	{
		await base.Start();

		_mediaKeyListener.Run();

		_lastVolume = BaselineVolume;
		_mediaKeyListener.SubscribedKeyPressed += VolumeKeyPressed;
		_statusController.VolumeReport += OnVolumeReport;
	}

	public override void Stop()
	{
		_mediaKeyListener.SubscribedKeyPressed -= VolumeKeyPressed;
		_statusController.VolumeReport -= OnVolumeReport;
		base.Stop();
	}

	//Spotify web api might not be fast enough to realize we have begun playing music
	//therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
	protected override async Task<int> GetBaselineVolume()
	{
		var playbackContext = await _spotifyClient.GetPlaybackContext();
		while (playbackContext?.Device == null)
		{
			await Task.Delay(500);
			playbackContext = await _spotifyClient.GetPlaybackContext();
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

		_logger.LogInformation("Manual volume change detected. {currentKnownVolume}% -> {newVolume}%", BaselineVolume, volume);
		BaselineVolume = _lastVolume = volume;
	}

	protected override async Task SetNewVolume()
	{
		if (_lastVolume == BaselineVolume)
			return;

		var err = await _spotifyClient.SetVolume(BaselineVolume);

		if (err != null && err.Error == null)
		{
			_logger.LogInformation("Changed volume to {volume}%", BaselineVolume);
			_lastVolume = BaselineVolume;
		}
		else
		{
			_logger.LogWarning("Failed to change volume.");
			BaselineVolume = _lastVolume;
		}
	}

	protected override void Dispose(bool disposing)
		=> _mediaKeyListener.Dispose();
}
