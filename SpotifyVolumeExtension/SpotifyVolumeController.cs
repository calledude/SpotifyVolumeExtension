using SpotifyAPI.Web.Models;
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
        private DateTime lastVolumePress;
        private Timer blockTimer;
        private bool blockUpdates;
        private PlaybackContext playbackContext;
        private Semaphore sem = new Semaphore(1, 1);

        public SpotifyVolumeController(SpotifyClient sc)
        {
            this.sc = sc;
            mkl = new MediaKeyListener();
            mkl.MediaKeyPressed += ChangeSpotifyVolume;
            blockTimer = new Timer(UnblockUpdates);
        }

        public void Start(SpotifyMonitor sm)
        {
            sm.SpotifyStatusChanged += ToggleVolumeController;
        }

        private void ToggleVolumeController(bool status)
        {
            if (status)
            {
                spotifyVolume = GetCurrentVolume(); //Get initial spotify-volume
                mkl.Start();
            }
            else
            {
                mkl.Stop();
            }
            Console.WriteLine("[SpotifyVolumeController] " + (status ? "Started." : "Stopped."));
        }

        private void UpdateVolume()
        {
            if (blockUpdates) return;

            sem.WaitOne();
            if (lastVolume != spotifyVolume)
            {
                lastVolume = spotifyVolume;
                SetNewVolume(spotifyVolume);
            }
            sem.Release();
        }

        private void UnblockUpdates(object sender)
        {
            blockUpdates = false;
            UpdateVolume();
        }

        private int GetCurrentVolume()
        {
            playbackContext = sc.GetPlaybackContext();
            return playbackContext.Device.VolumePercent;
        }

        private void ChangeSpotifyVolume(MediaKeyEventArgs m)
        {
            sem.WaitOne();
            if (m.When - lastVolumePress < TimeSpan.FromMilliseconds(50))
            {
                //Block function with flag
                blockUpdates = true;
                blockTimer.Change(500, Timeout.Infinite);
            }

            if (m.IsVolumeUp)
            {
                spotifyVolume += m.Presses;
            }
            else
            {
                spotifyVolume -= m.Presses;
            }

            if (spotifyVolume > 100)
            {
                spotifyVolume = 100;
            }
            else if (spotifyVolume < 0)
            {
                spotifyVolume = 0;
            }
            sem.Release();
            UpdateVolume();
            lastVolumePress = DateTime.Now;
        }

        private void SetNewVolume(int volume)
        {
            var err = sc.Api.SetVolume(volume);
            if(err.Error == null)
            {
                Console.WriteLine($"[SpotifyVolumeController] Changed volume to {spotifyVolume}%");
            }
        }
    }
}
