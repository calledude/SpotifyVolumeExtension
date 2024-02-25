using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Spotify;

public class TokenSwapAuthenticator : IAuthenticator
{
	public const string ExchangeServerUrl = "https://spotifyvolumeextension.azurewebsites.net";
	public static readonly string AuthorizeUri = ExchangeServerUrl + "/authorize";
	public static readonly string SwapUri = ExchangeServerUrl + "/swap";

	private static readonly Uri _refreshUri = new(ExchangeServerUrl + "/refresh");
	private readonly ILogger<TokenSwapAuthenticator> _logger;
	private readonly AuthorizationCodeTokenResponse _initialToken;

	public TokenSwapAuthenticator(AuthorizationCodeTokenResponse initialToken, ILogger<TokenSwapAuthenticator> logger)
	{
		_initialToken = initialToken;
		_logger = logger;
	}

	public async Task Apply(IRequest request, IAPIConnector apiConnector)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (_initialToken.IsExpired)
		{
			_logger.LogInformation("Token expired.");

			var tokenRequest = new TokenSwapRefreshRequest(_refreshUri, _initialToken.RefreshToken);
			var refreshedToken = await OAuthClient.RequestToken(tokenRequest, apiConnector);

			_initialToken.AccessToken = refreshedToken.AccessToken;
			_initialToken.CreatedAt = refreshedToken.CreatedAt;
			_initialToken.ExpiresIn = refreshedToken.ExpiresIn;
			_initialToken.Scope = refreshedToken.Scope;
			_initialToken.TokenType = refreshedToken.TokenType;
			if (refreshedToken.RefreshToken is not null)
			{
				_initialToken.RefreshToken = refreshedToken.RefreshToken;
			}

			_logger.LogInformation("Refreshed token.");
		}

		request.Headers["Authorization"] = $"{_initialToken.TokenType} {_initialToken.AccessToken}";
	}
}
