using Serilog;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan.Logging;
using System;
using System.Text;
using System.Threading;
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
		_server = new(new Uri("http://localhost/auth"), 4002);
		_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
		_channel = Channel.CreateBounded<AuthorizationCodeTokenResponse>(1);
	}

	public async Task<AuthorizationCodeTokenResponse> InitializeToken()
	{
		const int attemptTimeoutInSeconds = 25;
		var logger = Log.Logger.ForContext<TokenInitializer>();
		logger.Information("Trying to authenticate with Spotify. This might take up to {timeout} seconds", attemptTimeoutInSeconds);

		var builder = new StringBuilder(TokenSwapAuthenticator.AuthorizeUri);
		builder.Append("?response_type=code");
		builder.Append($"&redirect_uri={HttpUtility.UrlEncode(TokenSwapAuthenticator.ExchangeServerUrl)}");
		builder.Append($"&scope={HttpUtility.UrlEncode(string.Join(" ", Scopes.UserModifyPlaybackState, Scopes.UserReadPlaybackState))}");

		var authUri = new Uri(builder.ToString());
		BrowserUtil.Open(authUri);

		await _server.Start();

		while (true)
		{
			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(attemptTimeoutInSeconds));
				var tokenResponse = await _channel.Reader.ReadAsync(cts.Token).AsTask();
				logger.Information("Successfully authenticated.");
				return tokenResponse;
			}
			catch (OperationCanceledException)
			{
				logger.Warning("Authentication attempt timed out. Retrying.");
				BrowserUtil.Open(authUri);
			}
		}
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
