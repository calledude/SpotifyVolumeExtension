using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyMonitor
    {
        private SpotifyClient sc;
        private bool lastSpotifyStatus;
        private bool started;
        private static object m = new object();
        public event Action<bool> SpotifyStatusChanged;
        private static SpotifyMonitor sm;

        SpotifyMonitor(SpotifyClient sc)
        {
            this.sc = sc;
        }

        public static SpotifyMonitor GetMonitorInstance(SpotifyClient sc)
        {
            lock (m)
            {
                if (sm == null) sm = new SpotifyMonitor(sc);
                return sm;
            }
        }

        public void Start()
        {
            if (started) return;
            started = true;
            Console.WriteLine("[SpotifyMonitor] Started. Now monitoring activity.");
            new Thread(() =>
            {
                while (true)
                {
                    if (GetPlayingStatus() != lastSpotifyStatus)
                    {
                        AlertSpotifyStatus(!lastSpotifyStatus);
                    }
                    Thread.Sleep(15000);
                }
            }).Start();
        }

        internal void AlertSpotifyStatus(bool newState)
        {
            lock (m)
            {
                SpotifyStatusChanged?.Invoke(newState);
                lastSpotifyStatus = newState;
            }
        }

        public bool GetPlayingStatus()
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
            return sc.MusicIsPlaying;
        }
    }
}
