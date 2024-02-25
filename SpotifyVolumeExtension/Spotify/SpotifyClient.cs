using SpotifyAPI.Web;
using SpotifyVolumeExtension.Utilities;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Spotify;

public sealed class SpotifyClient
{
	private readonly Retry _retrier;

	private readonly SpotifyAPI.Web.SpotifyClient _client;

	public SpotifyClient(Retry retrier, SpotifyAPI.Web.SpotifyClient client)
	{
		_retrier = retrier;
		_client = client;
	}

	public async Task<CurrentlyPlayingContext?> GetPlaybackContext()
		=> await _retrier.Wrap(() => _client.Player.GetCurrentPlayback());

	public async Task<bool> SetVolume(int volumePercent)
		=> await _retrier.Wrap(() => _client.Player.SetVolume(new PlayerVolumeRequest(volumePercent)));
}
