﻿using System;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public abstract class SpotifyClient
    {
        protected readonly AutoResetEvent authWait = new AutoResetEvent(false);
        protected readonly string _clientID = "8c35f18897a14d9c8008323a7c167c68";
        protected readonly string _clientSecret = null; //Your Client-Secret here

        public readonly SpotifyWebAPI Api;
        public event Action NoActivePlayer;

        protected abstract Task RefreshToken();
        protected abstract void OnAuthResponse(object sender, Token payload);

        protected SpotifyClient()
        {
            Api = new SpotifyWebAPI();
            Api.OnError += OnError;
        }

        protected async void OnError(Error error)
        {
            if (Api.Token.IsExpired())
            {
                await RefreshToken();
            }
            else if (error.Status == 404) // No active player
            {
                NoActivePlayer?.Invoke();
            }
            else
            {
                Console.WriteLine($"[SpotifyClient] {error.Status.ToString()} {error.Message}");
            }
        }
    }

    public sealed class SpotifyClient<T> : SpotifyClient where T : Auth
    {
        private readonly T auth;

        public SpotifyClient() //AuthorizationCodeAuth requires your own Client-ID and Client-Secret to work.
        {
            auth = (T)Activator.CreateInstance(typeof(T),
                                               BindingFlags.OptionalParamBinding,
                                               null,
                                               new object[] { _clientID, "http://localhost:80", "http://localhost:80" },
                                               CultureInfo.CurrentCulture);

            if (auth is AuthorizationCodeAuth e)
            {
                e.SecretId = _clientSecret ?? throw new InvalidOperationException("Client secret must be provided with " + typeof(T));
            }

            auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            auth.AuthReceived += OnAuthResponse;
        }

        public void Authenticate()
        {
            auth.Start();
            auth.OpenBrowser();

            authWait.WaitOne(); //Wait for response
            Console.WriteLine("[SpotifyClient] Successfully authenticated.");
        }

        protected override void OnAuthResponse(object sender, Token payload)
        {
            Api.Token = payload;
            auth.Stop(0);

            authWait.Set();
        }

        protected override async Task RefreshToken()
        {
            if (auth is AuthorizationCodeAuth e)
            {
                Api.Token = await e.RefreshToken(Api.Token.RefreshToken);
            }
            else
            {
                Authenticate();
            }
            Console.WriteLine("[SpotifyClient] Refreshed token.");
        }
    }
}
