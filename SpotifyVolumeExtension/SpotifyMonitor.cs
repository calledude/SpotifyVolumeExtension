using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyMonitor : IDisposable
    {
        private readonly SpotifyClient _sc;
        private readonly AsyncMonitor _start;
        private CancellationTokenSource _cts;
        private readonly StatusController _statusController;
        private readonly AsyncManualResetEvent _failure;
        private Process[] _procs;
        private Task _pollTask;

        public SpotifyMonitor(SpotifyClient sc)
        {
            _start = new AsyncMonitor();
            _failure = new AsyncManualResetEvent(false);
            _statusController = new StatusController(this);

            _sc = sc ?? throw new ArgumentNullException(nameof(sc));
            sc.NoActivePlayer += CheckState;

            _procs = Process.GetProcessesByName("Spotify");
        }

        public async Task Start()
        {
            Log("Waiting for Spotify to start...");
            await WaitForSpotifyProcess();
            Log("Spotify process detected.");

            _sc.SetAutoRefresh(true);

            Log("Waiting for music to start playing.");
            if (!await TryWaitForPlaybackActivation())
                return;

            Log("Started. Now monitoring activity.");

            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(PollSpotifyStatus, _cts.Token);
        }

        private async Task<bool> TryWaitForPlaybackActivation()
        {
            double sleep = 1;
            while (!await GetPlayingStatus())
            {
                var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(500 * sleep));
                var failureWaitTask = _failure.WaitAsync();
                var completedTask = await Task.WhenAny(timeoutTask, failureWaitTask);

                if (completedTask == failureWaitTask) return false;
                if (sleep < 20) sleep *= 1.5;
            }

            return true;
        }

        private async Task WaitForSpotifyProcess()
        {
            while (!SpotifyIsRunning())
            {
                await Task.Delay(750);
                _procs = Process.GetProcessesByName("Spotify");
            }

            _procs[0].EnableRaisingEvents = true;
            _procs[0].Exited += SpotifyExited;
        }

        //Checks if Spotify is running/playing music
        //if the status changes, subscribers (Volume controllers) to the event are alerted.
        private async Task PollSpotifyStatus()
        {
            do
            {
                CheckState();
                await Task.Delay(15000);
            } while (!_cts.IsCancellationRequested);
        }

        private void Log(string message)
            => Console.WriteLine($"[{GetType().Name}] {message}");

        private void CheckState()
            => _statusController.CheckState();

        private async void SpotifyExited(object sender, EventArgs e)
        {
            Log("No Spotify process active.");
            CheckState();

            _cts?.Cancel();

            if (_pollTask != null)
            {
                await _pollTask;
                _pollTask.Dispose();
            }

            _failure.Set();

            _sc.SetAutoRefresh(false);

            using (_ = await _start.EnterAsync())
            {
                _failure.Reset();

                //Wait for all Spotify.exe processes to exit
                while (SpotifyIsRunning())
                {
                    await Task.Delay(50);
                }

                await Start();
            }
        }

        public async Task<bool> GetPlayingStatus()
            => SpotifyIsRunning() && await IsPlayingMusic();

        private bool SpotifyIsRunning()
            => _procs.Count(x => !x.HasExited) > 0;

        private async Task<bool> IsPlayingMusic()
        {
            var pb = await Retry.Wrap(() => _sc.Api.GetPlaybackAsync());
            return pb.IsPlaying;
        }

        public void Dispose()
        {
            _sc.Dispose();
            _statusController.Dispose();
        }
    }
}
