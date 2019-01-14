using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private int accumulatedVolumePresses;
        private DateTime lastVolumeKeyPress;
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
                    mkl.MediaKeyPressed += VolumeKeyPressed;
                }
                else
                {
                    mkl.MediaKeyPressed -= VolumeKeyPressed;
                }
                Running = status;
                Console.WriteLine("[SpotifyVolumeController] " + (status ? "Started." : "Stopped."));
            }
        }

        private void UpdateVolume()
        {
            lock (o)
            {
                if (accumulatedVolumePresses != 0)
                {
                    SetNewVolume(accumulatedVolumePresses);
                }
                accumulatedVolumePresses = 0;
            }

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
            var playbackContext = sc.Api.GetPlayback();
            while (playbackContext.Device == null)
            {
                Thread.Sleep(500);
                playbackContext = sc.Api.GetPlayback();
            }
            return playbackContext.Device.VolumePercent;
        }

        private void VolumeKeyPressed(int presses)
        {
            lock (o)
            {
                accumulatedVolumePresses += presses;
            }

            lock (b)
            {
                if (DateTime.Now - lastVolumeKeyPress < TimeSpan.FromMilliseconds(50))
                {
                    //Block function with flag
                    blockUpdates = true;
                    blockTimer.Change(350, Timeout.Infinite);
                }

                if (!blockUpdates)
                {
                    UpdateVolume();
                    lastVolumeKeyPress = DateTime.Now;
                }
            }
        }

        private void SetNewVolume(int steps)
        {
            var newVol = steps + GetCurrentVolume();
            if (newVol >= 0 && newVol <= 100)
            {
                var err = sc.Api.SetVolume(newVol);
                if (err.Error == null)
                {
                    Console.WriteLine($"[SpotifyVolumeController] Changed volume to {newVol}%");
                }
            }
        }
    }
}
