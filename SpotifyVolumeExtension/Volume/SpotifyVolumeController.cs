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

		_lastVolume = Volume;
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
		_logger.LogTrace("Fetching baseline volume");
		var playbackContext = await _spotifyClient.GetPlaybackContext();
		while (!playbackContext.IsPlaying)
		{
			_logger.LogTrace("Failed to fetch baseline volume");
			await Task.Delay(500);
			playbackContext = await _spotifyClient.GetPlaybackContext();
		}

		_logger.LogTrace("Fetched baseline volume");
		return playbackContext.Device.VolumePercent.Value;
	}

	private async Task VolumeKeyPressed(MediaKeyEventArgs m)
	{
		if (m.Key == Key.VolumeUp)
			Volume += m.Presses;
		else
			Volume -= m.Presses;

		Volume = Math.Clamp(Volume, 0, 100);

		await SetNewVolume();
	}

	private void OnVolumeReport(int volume)
	{
		// Ugly hack because the Spotify API is still inconsistent with volume percentage.
		// You can set the volume to 23, later fetch it and Spotify reports it as being set to 22.
		// So we just ignore any manual volume changes that are less than 1 percentage points.
		// I guess in the grand scheme of things it's not _that_ big of a loss, still annoying though
		if (Math.Abs(volume - Volume) <= 1)
			return;

		_logger.LogInformation("Manual volume change detected. {currentKnownVolume}% -> {newVolume}%", Volume, volume);
		Volume = _lastVolume = volume;
	}

	protected override async Task SetNewVolume()
	{
		if (_lastVolume == Volume)
			return;

		var success = await _spotifyClient.SetVolume(Volume);

		if (success)
		{
			_logger.LogInformation("Changed volume to {volume}%", Volume);
			_lastVolume = Volume;
		}
		else
		{
			_logger.LogWarning("Failed to change volume.");
			Volume = _lastVolume;
		}
	}

	protected override void Dispose(bool disposing)
		=> _mediaKeyListener.Dispose();
}
