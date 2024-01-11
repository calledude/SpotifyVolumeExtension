using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using SpotifyVolumeExtension.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension.Spotify;

public sealed class SpotifyClient : IDisposable
{
	private readonly ILogger<SpotifyClient> _logger;
	private readonly Retry _retrier;
	private readonly TokenSwapWebAPIFactory _authFactory;
	private readonly AutoResetEvent _authWait;

	private SpotifyWebAPI? Api { get; set; }
	public event Action? NoActivePlayer;

	public SpotifyClient(ILogger<SpotifyClient> logger, Retry retrier)
	{
		_logger = logger;
		_retrier = retrier;
		_authFactory = new TokenSwapWebAPIFactory("https://spotifyvolumeextension.azurewebsites.net")
		{
			Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState,
			AutoRefresh = true,
			Timeout = 25
		};

		_authFactory.OnAccessTokenExpired += OnTokenExpired;
		_authFactory.OnAuthSuccess += OnAuthSuccess;
		_authFactory.OnTokenRefreshSuccess += OnTokenRefresh;
		_authFactory.OnAuthFailure += OnAuthFailure;

		_authWait = new AutoResetEvent(false);
	}

	public async Task<PlaybackContext?> GetPlaybackContext()
		=> await _retrier.Wrap(() => Api?.GetPlaybackAsync() ?? Task.FromResult<PlaybackContext?>(null));

	public async Task<ErrorResponse?> SetVolume(int volumePercent)
		=> await _retrier.Wrap(() => Api?.SetVolumeAsync(volumePercent) ?? Task.FromResult<ErrorResponse?>(null));

	private void OnError(Error error)
	{
		if (error.Status == ErrorCode.TokenExpired)
			return;

		if (error.Status == ErrorCode.NoActivePlayer)
		{
			NoActivePlayer?.Invoke();
		}
		else
		{
			_logger.LogError("{StatusCode} {Message}", error.Status, error.Message);
		}
	}

	public void Authenticate()
	{
		_logger.LogInformation("Trying to authenticate with Spotify. This might take up to {timeout} seconds", _authFactory.Timeout);

		Api = _authFactory.GetWebApi();

		_authWait.WaitOne();

		Api.OnError += OnError;
		Api.UseAutoRetry = true;
	}

	public async Task SetAutoRefresh(bool autoRefresh)
	{
		if (autoRefresh == _authFactory.AutoRefresh)
			return;

		_logger.LogInformation("Setting 'AutoRefresh' to: {autoRefresh}", autoRefresh);
		_authFactory.AutoRefresh = autoRefresh;

		if (autoRefresh && (Api?.Token.IsExpired() ?? true))
		{
			await _retrier.Wrap(() => _authFactory.RefreshAuthAsync());
		}
	}

	private void OnTokenExpired(object? sender, AccessTokenExpiredEventArgs e)
		=> _logger.LogInformation("Token expired.");

	private void OnTokenRefresh(object? sender, AuthSuccessEventArgs e)
		=> _logger.LogInformation("Refreshed token.");

	private void OnAuthSuccess(object? sender, AuthSuccessEventArgs e)
	{
		_authWait.Set();
		_logger.LogInformation("Successfully authenticated.");
	}

	private async void OnAuthFailure(object? sender, AuthFailureEventArgs e)
	{
		if (Api == default)
		{
			_logger.LogWarning("Authentication failed: {error} - Retrying.", e.Error);

			Api = _authFactory.GetWebApi();
			return;
		}

		if (Api?.Token.IsExpired() ?? true)
		{
			_logger.LogWarning("Refreshing token failed: {error} - Retrying.", e.Error);
			await _retrier.Wrap(() => _authFactory.RefreshAuthAsync());
		}
	}

	public void Dispose()
	{
		Api?.Dispose();
		_authWait.Dispose();
	}
}
