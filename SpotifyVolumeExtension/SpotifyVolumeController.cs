using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private int lastVolume;
        private int spotifyVolume;
        private DateTime lastVolumeUpdate;
        private Timer blockTimer;
        private bool blockUpdates;
        private object o = new object();
        private object b = new object();
        private bool Running;

        public SpotifyVolumeController(SpotifyClient sc, MediaKeyListener mkl)
        {
            this.sc = sc;
            this.mkl = mkl;
            blockTimer = new Timer(UnblockUpdates);
        }

        public void Start(SpotifyMonitor sm)
        {
            sm.SpotifyStatusChanged += ToggleVolumeController;
        }

        private void ToggleVolumeController(bool status)
        {
            if (status != Running)
            {
                if (status)
                {
                    lastVolume = spotifyVolume = GetCurrentVolume(); //Get initial spotify-volume
                    mkl.MediaKeyPressed += ChangeSpotifyVolume;
                }
                else
                {
                    mkl.MediaKeyPressed -= ChangeSpotifyVolume;
                }
                Running = status;
                Console.WriteLine("[SpotifyVolumeController] " + (status ? "Started." : "Stopped."));
            }
        }

        private void UpdateVolume()
        {
            lock (b)
            {
                if (blockUpdates) return;

                if (DateTime.Now - lastVolumeUpdate < TimeSpan.FromMilliseconds(50))
                {
                    //Block function with flag
                    blockUpdates = true;
                    blockTimer.Change(350, Timeout.Infinite);
                    return;
                }
            }

            lock (o)
            {
                if (lastVolume != spotifyVolume)
                {
                    if(!SetNewVolume(spotifyVolume))
                    {
                        spotifyVolume = lastVolume;
                    }
                    lastVolume = spotifyVolume;
                }
            }

            lock (b) lastVolumeUpdate = DateTime.Now;
        }

        private void UnblockUpdates(object sender)
        {
            lock (b)
            {
                blockUpdates = false;
            }
            UpdateVolume();
        }

        //Spotify web api might not be fast enough to realize we have begun playing music
        //therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
        private int GetCurrentVolume()
        {
            var playbackContext = sc.GetPlaybackContext();
            while (playbackContext.Device == null)
            {
                Thread.Sleep(500);
                playbackContext = sc.GetPlaybackContext();
            }
            return playbackContext.Device.VolumePercent;
        }

        private void ChangeSpotifyVolume(MediaKeyEventArgs m)
        {
            lock (o)
            {
                if (m.IsVolumeUp) spotifyVolume += m.Presses;
                else spotifyVolume -= m.Presses;

                if (spotifyVolume > 100) spotifyVolume = 100;
                else if (spotifyVolume < 0) spotifyVolume = 0;
            }
            UpdateVolume();
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
