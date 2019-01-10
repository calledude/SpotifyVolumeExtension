using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyMonitor
    {
        private SpotifyClient sc;
        private bool lastSpotifyStatus, playKeyIsToggled, Running;
        private static object m = new object();
        private static object start = new object();
        public event Action<bool> SpotifyStatusChanged;
        private MediaKeyListener mkl;
        private Process[] procs;
        private VolumeGuard vg;
        private SpotifyVolumeController svc;
        private static AutoResetEvent failure = new AutoResetEvent(false);

        public SpotifyMonitor(SpotifyClient sc, MediaKeyListener mkl)
        {
            this.sc = sc;
            this.mkl = mkl;

            vg = new VolumeGuard();
            svc = new SpotifyVolumeController(sc, mkl);

            vg.Start(this);
            svc.Start(this);

            mkl.PlayPausePressed += SetPlayKeyState;
            mkl.StopPressed += StopVolumeControllers;
            sc.NoActivePlayer += StopVolumeControllers;
            procs = Process.GetProcessesByName("Spotify");
        }

        public void Start()
        {
            lock (start)
            {
                if (Running) return;
                Running = true;
            }

            Console.WriteLine("[SpotifyMonitor] Waiting for Spotify to start...");
            while (!SpotifyIsRunning())
            {
                Thread.Sleep(750);
                procs = Process.GetProcessesByName("Spotify");
            }

            Console.WriteLine("[SpotifyMonitor] Spotify process detected.");
            procs[0].EnableRaisingEvents = true;
            procs[0].Exited += SpotifyExited;

            Console.WriteLine("[SpotifyMonitor] Waiting for music to start playing.");

            double sleep = 1;
            while (!playKeyIsToggled && !GetPlayingStatus())
            {
                if (failure.WaitOne(TimeSpan.FromMilliseconds(500 * sleep))) return;
                if (sleep < 20) sleep *= 1.5;
            }
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");

            new Thread(PollSpotifyStatus).Start();
        }

        //Checks if Spotify is running/playing music
        //if the status changes, subscribers (Volume controllers) to the event are alerted.
        private void PollSpotifyStatus()
        {
            while (Running)
            {
                lock (m)
                {
                    if (GetPlayingStatus() != lastSpotifyStatus)
                    {
                        AlertSpotifyStatus(!lastSpotifyStatus);
                    }
                }
                Thread.Sleep(15000);
            }
        }

        private void StopVolumeControllers()
        {
            AlertSpotifyStatus(false);
        }

        private void AlertSpotifyStatus(bool newState)
        {
            lock (m)
            {
                SpotifyStatusChanged?.Invoke(newState);
                lastSpotifyStatus = newState;
                playKeyIsToggled = newState;
            }
        }

        private void SetPlayKeyState()
        {
            lock (m)
            {
                if (SpotifyIsRunning())
                {
                    playKeyIsToggled = !playKeyIsToggled;
                    AlertSpotifyStatus(playKeyIsToggled);
                }
                else
                {
                    playKeyIsToggled = false;
                }
            }
        }

        private void SpotifyExited(object sender, EventArgs e)
        {
            failure.Reset();
            Console.WriteLine("[SpotifyMonitor] No Spotify process active.");
            lock (start)
            {
                Running = false;
            }

            while (procs.Length > 0) //Wait for all Spotify.exe processes to exit
            {
                procs = procs.Where(x => !x.HasExited).ToArray();
            }

            StopVolumeControllers();
            Start();
            failure.Set();
        }


        private bool GetPlayingStatus()
        {
            return SpotifyIsRunning() && IsPlayingMusic();
        }

        private bool SpotifyIsRunning()
        {   
            return procs.Any();
        }

        private bool IsPlayingMusic()
        {
            return sc.GetPlaybackContext().IsPlaying;
        }
    }
}
