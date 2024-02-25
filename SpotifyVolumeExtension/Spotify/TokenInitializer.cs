using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan.Logging;
using System;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;

namespace SpotifyVolumeExtension.Spotify;
public sealed class TokenInitializer : IDisposable
{
	private readonly EmbedIOAuthServer _server;
	private readonly Channel<AuthorizationCodeTokenResponse> _channel;

	public TokenInitializer()
	{
		Logger.NoLogging();
		_server = new EmbedIOAuthServer(new Uri("http://localhost/auth"), 4002);
		_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
		_channel = Channel.CreateBounded<AuthorizationCodeTokenResponse>(1);
	}

	public async Task<AuthorizationCodeTokenResponse> InitializeToken()
	{
		//TODO: Need to implement retrying behavior
		//_logger.LogInformation("Trying to authenticate with Spotify. This might take up to {timeout} seconds", 25);

		// TODO: Abstract this away into a separate class akin to LoginRequest
		var builder = new StringBuilder(TokenSwapAuthenticator.AuthorizeUri);
		builder.Append($"?response_type=code");
		builder.Append($"&redirect_uri={HttpUtility.UrlEncode(TokenSwapAuthenticator.ExchangeServerUri)}");
		builder.Append($"&scope={HttpUtility.UrlEncode(string.Join(" ", Scopes.UserModifyPlaybackState, Scopes.UserReadPlaybackState))}");

		BrowserUtil.Open(new Uri(builder.ToString()));

		await _server.Start();

		return await _channel.Reader.ReadAsync();
	}

	private async Task OnAuthorizationCodeReceived(object obj, AuthorizationCodeResponse code)
	{
		await _server.Stop();

		var oauth = new OAuthClient(SpotifyClientConfig.CreateDefault());
		var tokenRequest = new TokenSwapTokenRequest(new Uri(TokenSwapAuthenticator.SwapUri), code.Code);

		var tokenResponse = await oauth.RequestToken(tokenRequest);
		await _channel.Writer.WriteAsync(tokenResponse);
		_channel.Writer.Complete();
	}

	public void Dispose()
	{
		_server.AuthorizationCodeReceived -= OnAuthorizationCodeReceived;
		_server.Dispose();
	}
}
