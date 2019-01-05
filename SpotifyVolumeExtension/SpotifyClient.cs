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
        private EventWaitHandle waitHandle = new AutoResetEvent(false);
        private Auth auth;
        public SpotifyWebAPI Api { get; private set; }
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        private string _clientSecret = null; //Your Client-Secret here
        private AuthType authType;
        private SpotifyMonitor sm;

        public bool MusicIsPlaying
        {
            get => GetPlaybackContext().IsPlaying;
        }
        
        public SpotifyClient(AuthType authType) //AuthType.Authorization requires your own Client-ID and Client-Secret to work.
        {
            if (authType == AuthType.Authorization && _clientSecret == null)
                throw new InvalidOperationException("Client secret must be provided with this token-type.");

            this.authType = authType;
            Authenticate(authType);

            Api = new SpotifyWebAPI();
            Api.OnError += OnError;
            Api.AccessToken = token.AccessToken;
            Api.TokenType = token.TokenType;
            Api.UseAuth = true;
        }

        public void Start(SpotifyMonitor sm)
        {
            this.sm = sm;
            Console.Write("[Spotify] Waiting for Spotify to start");
            while (!sm.GetPlayingStatus())
            {
                Console.Write(".");
                Thread.Sleep(3000);
            }
            Console.WriteLine("\n[Spotify] Successfully connected.");
        }

        private void Authenticate(AuthType authType)
        {
            if (authType == AuthType.Implicit)
                auth = new ImplicitGrantAuth();
            else
                auth = new AuthorizationCodeAuth();

            auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            auth.RedirectUri = "http://localhost:80";
            auth.ClientId = _clientID;
            auth.OnAuthReceivedEvent += OnAuthResponse;
            auth.StartHttpServer();
            auth.DoAuth();
            waitHandle.WaitOne(); //Wait for response
        }

        private void OnAuthResponse(AuthorizationData response)
        {
            if (auth is AuthorizationCodeAuth e)
                token = e.ExchangeAuthCode(response.Code, _clientSecret);
            else
                token = response.Token;

            auth.StopHttpServer();

            waitHandle.Set();
        }

        private void RefreshToken()
        {
            if (auth is AuthorizationCodeAuth e)
            {
                token = e.RefreshToken(token.RefreshToken, _clientSecret);
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
            else if(error.Status == 404)
            {
                sm.AlertSpotifyStatus(false);
            }
            Console.WriteLine($"{error.Status} {error.Message}");
        }

    }
}
