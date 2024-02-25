using SpotifyAPI.Web;
using SpotifyVolumeExtension.Utilities;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Spotify;

public sealed class SpotifyApiClient
{
	private readonly Retry _retrier;

	private readonly SpotifyClient _client;

	public SpotifyApiClient(Retry retrier, SpotifyClient client)
	{
		_retrier = retrier;
		_client = client;
	}

	public async Task<CurrentlyPlayingContext?> GetPlaybackContext()
		=> await _retrier.Wrap(() => _client.Player.GetCurrentPlayback());

	public async Task<bool> SetVolume(int volumePercent)
		=> await _retrier.Wrap(() => _client.Player.SetVolume(new PlayerVolumeRequest(volumePercent)));
}
