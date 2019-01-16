using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private DateTime lastVolumeChange;
        private int lastVolume;
        private int spotifyVolume;
        private object o = new object();
        private bool Running;
        private AutoResetEvent waitForKeyPress = new AutoResetEvent(false);
        private Thread volThread;

        public SpotifyVolumeController(SpotifyClient sc, MediaKeyListener mkl)
        {
            this.sc = sc;
            this.mkl = mkl;
        }

        public void Start(SpotifyMonitor sm)
        {
            sm.SpotifyStatusChanged += ToggleVolumeController;
        }

        private void ToggleVolumeController(bool status)
        {
            if (status != Running)
            {
                Running = status;
                if (status)
                {
                    lastVolume = spotifyVolume = GetCurrentVolume(); //Get initial spotify-volume
                    mkl.MediaKeyPressed += VolumeKeyPressed;
                    volThread = new Thread(UpdateVolume);
                    volThread.Start();
                }
                else
                {
                    waitForKeyPress.Set();
                    mkl.MediaKeyPressed -= VolumeKeyPressed;
                    volThread.Join();
                }
                Console.WriteLine("[SpotifyVolumeController] " + (status ? "Started." : "Stopped."));
            }
        }

        private void UpdateVolume()
        {
            while (Running)
            {
                waitForKeyPress.WaitOne();
                if (DateTime.Now - lastVolumeChange < TimeSpan.FromMilliseconds(50))
                {
                    Thread.Sleep(250);
                }

                lock (o)
                {
                    if (lastVolume != spotifyVolume)
                    {
                        if (!SetNewVolume(spotifyVolume))
                        {
                            spotifyVolume = lastVolume;
                        }
                        lastVolumeChange = DateTime.Now;
                        lastVolume = spotifyVolume;
                    }
                }
            }

        }

        //Spotify web api might not be fast enough to realize we have begun playing music
        //therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
        private int GetCurrentVolume()
        {
            var playbackContext = sc.Api.GetPlayback();
            while (playbackContext.Device == null)
            {
                Thread.Sleep(500);
                playbackContext = sc.Api.GetPlayback();
            }
            return playbackContext.Device.VolumePercent;
        }

        private void VolumeKeyPressed(MediaKeyEventArgs m)
        {
            lock (o)
            {
                if (m.IsVolumeUp) spotifyVolume += m.Presses;
                else spotifyVolume -= m.Presses;

                if (spotifyVolume > 100) spotifyVolume = 100;
                else if (spotifyVolume < 0) spotifyVolume = 0;
            }
            waitForKeyPress.Set();
        }

        private bool SetNewVolume(int volume)
        {
            var err = sc.Api.SetVolume(volume);
            if(err.Error == null)
            {
                Console.WriteLine($"[SpotifyVolumeController] Changed volume to {volume}%");
                return true;
            }
            return false;
        }
    }
}
