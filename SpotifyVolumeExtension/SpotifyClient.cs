using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System.Threading;
using System.Linq;

namespace SpotifyVolumeExtension
{
    public class SpotifyClient
    {
        private EventWaitHandle waitHandle = new AutoResetEvent(false);
        private ImplicitGrantAuth implicitAuth;
        private AutorizationCodeAuth auth;
        public SpotifyWebAPI Api { get; private set; }
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        private string _clientSecret = null; //Your Client-Secret here
        private AuthType authType;

        private bool AnyDeviceIsActive
        {
            get => Api.GetDevices().Devices?.Any(x => x.IsActive) ?? false;
        }
        
        public SpotifyClient(AuthType authType) //AuthType.Authorization requires your own Client-ID and Client-Secret to work.
        {
            if (authType == AuthType.Authorization && _clientSecret == null)
                throw new InvalidOperationException("Client secret must be provided with this token-type.");

            this.authType = authType;
            Authenticate(authType);
            waitHandle.WaitOne(); //Wait for response

            GetNewApiInstance();
        }

        public void Start()
        {
            Console.Write("[Spotify] Waiting for Spotify to start");
            while (!AnyDeviceIsActive)
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }
            Console.WriteLine("\n[Spotify] Successfully connected.");
        }

        private void Authenticate(AuthType authType)
        {
            if (authType == AuthType.Implicit)
            {
                implicitAuth = new ImplicitGrantAuth();
                implicitAuth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
                implicitAuth.RedirectUri = "http://localhost:80";
                implicitAuth.ClientId = _clientID;
                implicitAuth.OnResponseReceivedEvent += OnImplicitAuthResponse;
                implicitAuth.StartHttpServer();
                implicitAuth.DoAuth();
            }
            else
            {
                auth = new AutorizationCodeAuth();
                auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
                auth.RedirectUri = "http://localhost:80";
                auth.ClientId = _clientID;
                auth.OnResponseReceivedEvent += OnAuthResponse;
                auth.StartHttpServer();
                auth.DoAuth();
            }
        }

        private void OnAuthResponse(AutorizationCodeAuthResponse response)
        {
            token = auth.ExchangeAuthCode(response.Code, _clientSecret);
            auth.StopHttpServer();

            waitHandle.Set();
        }

        private void OnImplicitAuthResponse(Token token, string state)
        {
            implicitAuth.StopHttpServer();
            this.token = token;

            waitHandle.Set(); //Signal that authentication is done
        }

        private void GetNewApiInstance()
        {
            Api = new SpotifyWebAPI();
            Api.AccessToken = token.AccessToken;
            Api.TokenType = token.TokenType;
            Api.UseAuth = true;
        }

        public void RefreshToken()
        {
            if (token.IsExpired())
            {
                if (authType == AuthType.Authorization)
                {
                    token = auth.RefreshToken(token.RefreshToken, _clientSecret);
                    Api.AccessToken = token.AccessToken;
                }
                else
                {
                    Authenticate(authType);
                    waitHandle.WaitOne();
                }
                Console.WriteLine("[SpotifyClient] Refreshed token.");
            }
        }

        public PlaybackContext GetPlaybackContext()
        {
            return Api.GetPlayback();
        }

    }
}
