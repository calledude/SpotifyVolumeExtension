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
        private ImplicitGrantAuth auth;
        public SpotifyWebAPI Api { get; private set; }
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";

        private bool AnyDeviceIsActive
        {
            get => Api.GetDevices().Devices?.Any(x => x.IsActive) ?? false;
        }
        
        public SpotifyClient()
        {
            Authenticate();
            waitHandle.WaitOne(); //Wait for response
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

        private void Authenticate()
        {
            auth = new ImplicitGrantAuth();
            auth.RedirectUri = "http://localhost:80";
            auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            auth.ClientId = _clientID;
            auth.OnResponseReceivedEvent += OnAuthResponse;
            auth.StartHttpServer();
            auth.DoAuth();
        }

        private void OnAuthResponse(Token token, string state)
        {
            this.token = token;
            auth.StopHttpServer();

            Api = new SpotifyWebAPI();
            Api.AccessToken = token.AccessToken;
            Api.TokenType = token.TokenType;

            waitHandle.Set(); //Signal that authentication is done
        }

        public void RefreshToken()
        {
            if (token.IsExpired())
            {
                Authenticate();
                waitHandle.WaitOne();
                Console.WriteLine("[SpotifyClient] Refreshed token.");
            }
        }

        public PlaybackContext GetPlaybackContext()
        {
            return Api.GetPlayback();
        }

    }
}
