using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyVolumeExtension.Utilities;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Spotify;

public sealed class SpotifyClient
{
	private readonly ILogger<SpotifyClient> _logger;
	private readonly Retry _retrier;

	private readonly SpotifyAPI.Web.SpotifyClient _client;
	public event Action? NoActivePlayer;

	public SpotifyClient(ILogger<SpotifyClient> logger, Retry retrier, SpotifyAPI.Web.SpotifyClient client)
	{
		_logger = logger;
		_retrier = retrier;
		_client = client;
	}

	public async Task<CurrentlyPlayingContext?> GetPlaybackContext()
		=> await _retrier.Wrap(() => _client.Player.GetCurrentPlayback());

	public async Task<bool> SetVolume(int volumePercent)
		=> await _retrier.Wrap(() => _client.Player.SetVolume(new PlayerVolumeRequest(volumePercent)));
}
