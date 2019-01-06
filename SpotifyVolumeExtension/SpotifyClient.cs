using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyClient
    {
        private EventWaitHandle authWait = new AutoResetEvent(false);
        private Auth auth;
        public SpotifyWebAPI Api { get; private set; }
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        private string _clientSecret = null; //Your Client-Secret here
        private AuthType authType;
        public event Action NoActivePlayer;

        public SpotifyClient(AuthType authType) //AuthType.Authorization requires your own Client-ID and Client-Secret to work.
        {
            if (authType == AuthType.Authorization && _clientSecret == null)
                throw new InvalidOperationException("Client secret must be provided with this token-type.");

            this.authType = authType;
        }

        public void Start()
        {
            Authenticate(authType);

            Api = new SpotifyWebAPI();
            Api.OnError += OnError;
            Api.AccessToken = token.AccessToken;
            Api.TokenType = token.TokenType;
            Api.UseAuth = true;

            Console.Clear();
            Console.WriteLine("[Spotify] Successfully connected.");
        }

        private void Authenticate(AuthType authType)
        {
            if (authType == AuthType.Implicit)

                auth = new ImplicitGrantAuth(_clientID, "http://localhost:80", "http://localhost:80");
            else
            {
                auth = new AuthorizationCodeAuth("http://localhost:80", "http://localhost:80");
                auth.SecretId = _clientSecret;
            }

            auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            auth.RedirectUri = "http://localhost:80";
            auth.ClientId = _clientID;
            auth.AuthReceived += OnAuthResponse;
            auth.Start();
            auth.OpenBrowser();

            authWait.WaitOne(); //Wait for response
        }

        private void OnAuthResponse(object sender, Token payload)
        {
            token = payload;
            auth.Stop(0);

            authWait.Set();
        }

        private async void RefreshToken()
        {
            if (auth is AuthorizationCodeAuth e)
            {
                await e.RefreshToken(token.RefreshToken);
            }
            else
            {
                Authenticate(authType);
            }
            Api.AccessToken = token.AccessToken;
            Console.WriteLine("[SpotifyClient] Refreshed token.");
        }

        public PlaybackContext GetPlaybackContext()
        {
            return Api.GetPlayback();
        }

        private void OnError(Error error)
        {
            if (token.IsExpired()) RefreshToken();
            else if(error.Status == 404) // No active player
            {
                NoActivePlayer?.Invoke();
            }
            Console.WriteLine($"{error.Status} {error.Message}");
        }

    }
}
