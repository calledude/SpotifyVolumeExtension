using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public abstract class SpotifyClient : IDisposable
    {
        protected AutoResetEvent AuthWait { get; } = new AutoResetEvent(false);
        protected string ClientID { get; } = "8c35f18897a14d9c8008323a7c167c68";
        protected string ClientSecret { get; } = null; //Your Client-Secret here

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

        public void Dispose()
        {
            AuthWait.Dispose();
            Api.Dispose();
        }
    }

    public sealed class SpotifyClient<T> : SpotifyClient where T : Auth
    {
        private readonly T _auth;

        public SpotifyClient() //AuthorizationCodeAuth requires your own Client-ID and Client-Secret to work.
        {
            _auth = (T)Activator.CreateInstance(typeof(T),
                                               BindingFlags.OptionalParamBinding,
                                               null,
                                               new object[] { ClientID, "http://localhost:80", "http://localhost:80" },
                                               CultureInfo.CurrentCulture);

            if (_auth is AuthorizationCodeAuth e)
            {
                e.SecretId = ClientSecret ?? throw new InvalidOperationException("Client secret must be provided with " + typeof(T));
            }

            _auth.Scope = Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState;
            _auth.AuthReceived += OnAuthResponse;
        }

        public void Authenticate()
        {
            _auth.Start();
            _auth.OpenBrowser();

            AuthWait.WaitOne(); //Wait for response
            Console.WriteLine("[SpotifyClient] Successfully authenticated.");
        }

        protected override void OnAuthResponse(object sender, Token payload)
        {
            Api.Token = payload;
            _auth.Stop(0);

            AuthWait.Set();
        }

        protected override async Task RefreshToken()
        {
            if (_auth is AuthorizationCodeAuth e)
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
