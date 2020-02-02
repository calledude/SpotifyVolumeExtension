using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyClient : IDisposable
    {
        private readonly string _name;
        private readonly AutoResetEvent _authWait;
        private readonly TokenSwapWebAPIFactory _authFactory;

        public SpotifyWebAPI Api { get; private set; }
        public event Action NoActivePlayer;

        public SpotifyClient()
        {
            _name = GetType().Name;

            _authWait = new AutoResetEvent(false);

            _authFactory = new TokenSwapWebAPIFactory("https://spotifyvolumeextension.herokuapp.com")
            {
                Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState,
                AutoRefresh = true,
                Timeout = 25
            };

            _authFactory.OnAccessTokenExpired += OnTokenExpired;
            _authFactory.OnAuthSuccess += OnAuthSuccess;
            _authFactory.OnTokenRefreshSuccess += OnTokenRefresh;
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

        public async Task Authenticate()
        {
            Log($"Trying to authenticate with Spotify. This might take up to {_authFactory.Timeout.ToString()} seconds");

            Api = await _authFactory.GetWebApiAsync();

            Api.OnError += OnError;

            _authWait.WaitOne(); //Wait for response
            Log("Successfully authenticated.");
        }

        public async void SetAutoRefresh(bool autoRefresh)
        {
            if (autoRefresh == _authFactory.AutoRefresh)
                return;

            Log($"Setting 'AutoRefresh' to: {autoRefresh.ToString()}");
            _authFactory.AutoRefresh = autoRefresh;

            if (autoRefresh && Api.Token.IsExpired())
            {
                await _authFactory.RefreshAuthAsync();
            }
        }

        private void OnTokenExpired(object sender, AccessTokenExpiredEventArgs e)
            => Log("Token expired.");

        private void OnTokenRefresh(object sender, AuthSuccessEventArgs e)
            => Log("Refreshed token.");

        private void OnAuthSuccess(object sender, AuthSuccessEventArgs e)
            => _authWait.Set();

        private void Log(string message)
            => Console.WriteLine($"[{_name}] {message}");

        public void Dispose()
        {
            _authWait.Dispose();
            Api.Dispose();
        }
    }
}
