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
        private SpotifyWebAPI api;
        private Token token;
        private string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        private int spotifyVolume = 0;
        public int Volume
        {
            get => spotifyVolume;
            set
            {
                spotifyVolume = value;
                if (spotifyVolume > 100) spotifyVolume = 100;
                if (spotifyVolume < 0) spotifyVolume = 0;
                api.SetVolume(spotifyVolume);
            }
        }
        
        public SpotifyClient()
        {
            Authorize();
            waitHandle.WaitOne(); //Wait for response

            api = new SpotifyWebAPI();
            api.AccessToken = token.AccessToken;
            api.TokenType = token.TokenType;
        }

        public void Start()
        {
            Console.Write("[Spotify] Waiting for Spotify to start");
            var pc = GetPlaybackContext();
            SetBaselineVolume(pc);
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


        private PlaybackContext GetPlaybackContext()
        {
            while (!api.GetDevices().Devices.Any(x => x.IsActive))
            {
                Console.Write(".");
                Thread.Sleep(1000);
            }
            return api.GetPlayback();
        }

        private void SetBaselineVolume(PlaybackContext pc)
        {
            spotifyVolume = pc.Device.VolumePercent;
        }
    }
}
