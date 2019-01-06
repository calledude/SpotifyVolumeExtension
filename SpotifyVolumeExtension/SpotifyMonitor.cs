using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyMonitor
    {
        private SpotifyClient sc;
        private bool lastSpotifyStatus, playKeyIsToggled;
        private static object m = new object();
        public event Action<bool> SpotifyStatusChanged;
        private MediaKeyListener mkl;

        public SpotifyMonitor(SpotifyClient sc, MediaKeyListener mkl)
        {
            this.sc = sc;
            this.mkl = mkl;
            mkl.PlayPausePressed += SetPlayKeyState;
            sc.NoActivePlayer += StopVolumeControllers;
        }

        public void Start(VolumeGuard vg, SpotifyVolumeController svc)
        {
            Console.WriteLine("[SpotifyMonitor] Waiting for Spotify to start...");
            vg.Start(this);
            svc.Start(this);

            while (!playKeyIsToggled && !GetPlayingStatus())
            {
                Thread.Sleep(2000);
            }
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");

            new Thread(PollSpotifyStatus).Start();
        }

        //Checks if Spotify is running/playing music
        //if the status changes, subscribers (Volume controllers) to the event are alerted.
        private void PollSpotifyStatus()
        {
            while (true)
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

        private bool GetPlayingStatus()
        {
            return SpotifyIsRunning() && IsPlayingMusic();
        }

        private bool SpotifyIsRunning()
        {
            var spotifyProcesses = Process.GetProcessesByName("Spotify");
            return spotifyProcesses.Any();
        }

        private bool IsPlayingMusic()
        {
            return sc.GetPlaybackContext().IsPlaying;
        }
    }
}
