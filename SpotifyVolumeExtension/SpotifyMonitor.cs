using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Nito.AsyncEx;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyMonitor
    {
        private readonly SpotifyClient sc;
        private readonly object start = new object();
        private readonly MediaKeyListener mkl;
        private Process[] procs;
        private readonly VolumeController vg, svc;
        private readonly AutoResetEvent shouldExit = new AutoResetEvent(false);
        private Thread pollThread;
        private readonly StatusController statusController;
        private readonly AsyncManualResetEvent failure = new AsyncManualResetEvent(false);

        public SpotifyMonitor(SpotifyClient sc)
        {
            statusController = new StatusController(this);

            this.sc = sc ?? throw new ArgumentNullException(nameof(sc));
            sc.NoActivePlayer += CheckState;

            vg = new VolumeGuard();
            svc = new SpotifyVolumeController(sc);

            mkl = new MediaKeyListener();
            mkl.SubscribeTo(Keys.MediaPlayPause);
            mkl.SubscribeTo(Keys.MediaStop);
            mkl.SubscribedKeyPressed += (_) => CheckState();
            mkl.Start();

            procs = Process.GetProcessesByName("Spotify");
        }

        public async void Start()
        {
            Console.WriteLine("[SpotifyMonitor] Waiting for Spotify to start...");
            while (!SpotifyIsRunning())
            {
                await Task.Delay(750);
                procs = Process.GetProcessesByName("Spotify");
            }

            Console.WriteLine("[SpotifyMonitor] Spotify process detected.");
            procs[0].EnableRaisingEvents = true;
            procs[0].Exited += SpotifyExited;

            Console.WriteLine("[SpotifyMonitor] Waiting for music to start playing.");

            double sleep = 1;
            while (!await GetPlayingStatus())
            {
                var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(500*sleep));
                var failureWaitTask = failure.WaitAsync();
                var completedTask = await Task.WhenAny(timeoutTask, failureWaitTask);

                if (completedTask == failureWaitTask) return;
                if (sleep < 20) sleep *= 1.5;
            }
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");

            pollThread = new Thread(PollSpotifyStatus);
            pollThread.Start();
        }

        //Checks if Spotify is running/playing music
        //if the status changes, subscribers (Volume controllers) to the event are alerted.
        private void PollSpotifyStatus()
        {
            do
            {
                CheckState();
            } while (!shouldExit.WaitOne(15000));
        }

        private void CheckState() => statusController.CheckState();

        private void SpotifyExited(object sender, EventArgs e)
        {
            Console.WriteLine("[SpotifyMonitor] No Spotify process active.");
            CheckState();
            shouldExit.Set();
            pollThread?.Join();
            failure.Set();

            lock(start)
            {
                failure.Reset();

                while (procs.Length > 0) //Wait for all Spotify.exe processes to exit
                {
                    procs = procs.AsParallel().Where(x => !x.HasExited).ToArray();
                }

                Start();
            }
        }

        public async Task<bool> GetPlayingStatus() => SpotifyIsRunning() && await IsPlayingMusic();

        private bool SpotifyIsRunning() => procs.Any(x => !x.HasExited);

        private async Task<bool> IsPlayingMusic()
        {
            var pb =  await sc.Api.GetPlaybackAsync();
            return pb.IsPlaying;
        }
    }
}
