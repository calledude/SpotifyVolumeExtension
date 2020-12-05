using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyClient : IDisposable
    {
        private readonly TokenSwapWebAPIFactory _authFactory;
        private readonly AutoResetEvent _authWait;

        public SpotifyWebAPI Api { get; private set; }
        public event Action NoActivePlayer;

        public SpotifyClient()
        {
            _authFactory = new TokenSwapWebAPIFactory("https://spotifyvolumeextension.herokuapp.com")
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

        private void OnError(Error error)
        {
            if (error.Status == 404) // No active player
            {
                NoActivePlayer?.Invoke();
            }
            else if (error.Status == 401)
            {
                return;
            }
            else
            {
                Log($"{error.Status.ToString()} {error.Message}");
            }
        }

        public void Authenticate()
        {
            Log($"Trying to authenticate with Spotify. This might take up to {_authFactory.Timeout.ToString()} seconds");

            Api = _authFactory.GetWebApi();

            _authWait.WaitOne();

            Api.OnError += OnError;
            Api.UseAutoRetry = true;
        }

        public async void SetAutoRefresh(bool autoRefresh)
        {
            if (autoRefresh == _authFactory.AutoRefresh)
                return;

            Log($"Setting 'AutoRefresh' to: {autoRefresh.ToString()}");
            _authFactory.AutoRefresh = autoRefresh;

            if (autoRefresh && Api.Token.IsExpired())
            {
                await Retry.Wrap(() => _authFactory.RefreshAuthAsync());
            }
        }

        private void OnTokenExpired(object sender, AccessTokenExpiredEventArgs e)
            => Log("Token expired.");

        private void OnTokenRefresh(object sender, AuthSuccessEventArgs e)
            => Log("Refreshed token.");

        private void OnAuthSuccess(object sender, AuthSuccessEventArgs e)
        {
            _authWait.Set();
            Log("Successfully authenticated.");
        }

        private async void OnAuthFailure(object sender, AuthFailureEventArgs e)
        {
            if (Api == default)
            {
                Log($"Authentication failed: {e.Error} - Retrying.");

                Api = _authFactory.GetWebApi();
                return;
            }

            if (Api.Token == default || Api.Token.IsExpired())
            {
                Log($"Refreshing token failed: {e.Error} - Retrying.");
                await Retry.Wrap(() => _authFactory.RefreshAuthAsync());
            }
        }

        private void Log(string message)
            => Console.WriteLine($"[{nameof(SpotifyClient)}] {message}");

        public void Dispose()
            => Api.Dispose();
    }
}
