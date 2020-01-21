using LowLevelInput.Hooks;
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
        private readonly object _start = new object();
        private readonly MediaKeyListener _mkl;
        private Process[] _procs;
        private readonly AutoResetEvent _shouldExit = new AutoResetEvent(false);
        private Thread _pollThread;
        private readonly StatusController _statusController;
        private readonly AsyncManualResetEvent _failure = new AsyncManualResetEvent(false);

        public SpotifyMonitor(SpotifyClient sc)
        {
            _statusController = new StatusController(this);

            _sc = sc ?? throw new ArgumentNullException(nameof(sc));
            sc.NoActivePlayer += CheckState;

            _mkl = new MediaKeyListener();

            _mkl.SubscribeTo(VirtualKeyCode.MediaPlayPause);
            _mkl.SubscribeTo(VirtualKeyCode.MediaStop);
            _mkl.SubscribedKeyPressed += _ => CheckState();

            _procs = Process.GetProcessesByName("Spotify");
        }

        public async void Start()
        {
            Console.WriteLine("[SpotifyMonitor] Waiting for Spotify to start...");
            while (!SpotifyIsRunning())
            {
                await Task.Delay(750);
                _procs = Process.GetProcessesByName("Spotify");
            }

            Console.WriteLine("[SpotifyMonitor] Spotify process detected.");
            _procs[0].EnableRaisingEvents = true;
            _procs[0].Exited += SpotifyExited;

            Console.WriteLine("[SpotifyMonitor] Waiting for music to start playing.");

            double sleep = 1;
            while (!await GetPlayingStatus())
            {
                var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(500 * sleep));
                var failureWaitTask = _failure.WaitAsync();
                var completedTask = await Task.WhenAny(timeoutTask, failureWaitTask);

                if (completedTask == failureWaitTask) return;
                if (sleep < 20) sleep *= 1.5;
            }
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");

            _pollThread = new Thread(PollSpotifyStatus);
            _pollThread.Start();
        }

        //Checks if Spotify is running/playing music
        //if the status changes, subscribers (Volume controllers) to the event are alerted.
        private void PollSpotifyStatus()
        {
            do
            {
                CheckState();
            } while (!_shouldExit.WaitOne(15000));
        }

        private void CheckState() => _statusController.CheckState();

        private void SpotifyExited(object sender, EventArgs e)
        {
            Console.WriteLine("[SpotifyMonitor] No Spotify process active.");
            CheckState();
            _shouldExit.Set();
            _pollThread?.Join();
            _failure.Set();

            lock (_start)
            {
                _failure.Reset();

                while (_procs.Length > 0) //Wait for all Spotify.exe processes to exit
                {
                    _procs = _procs.AsParallel().Where(x => !x.HasExited).ToArray();
                }

                Start();
            }
        }

        public async Task<bool> GetPlayingStatus()
            => SpotifyIsRunning() && await IsPlayingMusic();

        private bool SpotifyIsRunning()
            => _procs.Count(x => !x.HasExited) > 0;

        private async Task<bool> IsPlayingMusic()
        {
            var pb = await _sc.Api.GetPlaybackAsync();
            return pb.IsPlaying;
        }

        public void Dispose()
        {
            _sc.Dispose();
            _shouldExit.Dispose();
            _mkl.Dispose();
        }
    }
}
