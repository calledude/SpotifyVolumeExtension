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
        private readonly AutoResetEvent _authWait;
        private readonly TokenSwapWebAPIFactory _authFactory;

        public SpotifyWebAPI Api { get; private set; }
        public event Action NoActivePlayer;

        public SpotifyClient()
        {
            _authWait = new AutoResetEvent(false);

            _authFactory = new TokenSwapWebAPIFactory("https://spotifyvolumeextension.herokuapp.com")
            {
                Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState,
                AutoRefresh = true
            };

            _authFactory.OnAuthSuccess += OnAuthSuccess;
            _authFactory.OnTokenRefreshSuccess += OnTokenRefresh;
        }


        private void OnError(Error error)
        {
            if (error.Status == 404) // No active player
            {
                NoActivePlayer?.Invoke();
            }
            else
            {
                Console.WriteLine($"[SpotifyClient] {error.Status.ToString()} {error.Message}");
            }
        }

        public async Task Authenticate()
        {
            Api = await _authFactory.GetWebApiAsync();

            Api.OnError += OnError;

            _authWait.WaitOne(); //Wait for response
            Console.WriteLine("[SpotifyClient] Successfully authenticated.");
        }

        private void OnTokenRefresh(object sender, TokenSwapWebAPIFactory.AuthSuccessEventArgs e)
        {
            Console.WriteLine("[SpotifyClient] Refreshed token.");
        }

        private void OnAuthSuccess(object sender, TokenSwapWebAPIFactory.AuthSuccessEventArgs e)
        {
            _authWait.Set();
        }

        public void Dispose()
        {
            _authWait.Dispose();
            Api.Dispose();
        }
    }
}
