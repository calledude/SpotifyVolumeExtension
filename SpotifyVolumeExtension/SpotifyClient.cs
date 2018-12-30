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
        public SpotifyWebAPI Api { get; }
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";

        private bool AnyDeviceIsActive
        {
            get => Api.GetDevices().Devices?.Any(x => x.IsActive) ?? false;
        }
        
        public SpotifyClient()
        {
            Authorize();
            waitHandle.WaitOne(); //Wait for response

            Api = new SpotifyWebAPI();
            Api.AccessToken = token.AccessToken;
            Api.TokenType = token.TokenType;
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

        private void Authorize()
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
            waitHandle.Set(); //Signal that authorization is done
        }

        public PlaybackContext GetPlaybackContext()
        {
            return Api.GetPlayback();
        }

    }
}
