using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public class SpotifyClient
    {
        private EventWaitHandle authWait = new AutoResetEvent(false);
        private Auth auth;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        private string _clientSecret = null; //Your Client-Secret here

        public event Action NoActivePlayer;
        public SpotifyWebAPI Api { get; private set; }

        public SpotifyClient(AuthType authType) //AuthType.Authorization requires your own Client-ID and Client-Secret to work.
        {
            if (authType == AuthType.Authorization && _clientSecret == null)
                throw new InvalidOperationException("Client secret must be provided with this token-type.");

            if (authType == AuthType.Implicit)
                auth = new ImplicitGrantAuth(_clientID, "http://localhost:80", "http://localhost:80");
            else
                auth = new AuthorizationCodeAuth(_clientID, _clientSecret, "http://localhost:80", "http://localhost:80");

            auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            auth.AuthReceived += OnAuthResponse;

            Api = new SpotifyWebAPI();
            Api.OnError += OnError;
        }

        public void Start()
        {
            Authenticate();

            Console.WriteLine("[SpotifyClient] Successfully connected.");
        }

        private void Authenticate()
        {
            auth.Start();
            auth.OpenBrowser();

            authWait.WaitOne(); //Wait for response
        }

        private void OnAuthResponse(object sender, Token payload)
        {
            Api.Token = payload;
            auth.Stop(0);

            authWait.Set();
        }

        private void RefreshToken()
        {
            if (auth is AuthorizationCodeAuth e)
            {
                Api.Token = e.RefreshToken(Api.Token.RefreshToken).Result;
            }
            else
            {
                Authenticate();
            }

            Console.WriteLine("[SpotifyClient] Refreshed token.");
        }

        private void OnError(Error error)
        {
            if (Api.Token.IsExpired())
            {
                RefreshToken();
            }
            else if (error.Status == 404) // No active player
            {
                NoActivePlayer?.Invoke();
            }
            else
            {
                Console.WriteLine($"[SpotifyClient] {error.Status} {error.Message}");
            }
        }

    }
}
