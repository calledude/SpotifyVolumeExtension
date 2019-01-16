using System;
using System.Threading;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private int accumulatedVolumePresses;
        private object o = new object();
        private AutoResetEvent waitForKeyPress = new AutoResetEvent(false);
        private DateTime lastVolumeChange;
        private bool Running;

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
                    mkl.MediaKeyPressed += VolumeKeyPressed;
                    new Thread(UpdateVolume).Start();
                }
                else
                {
                    mkl.MediaKeyPressed -= VolumeKeyPressed;
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
                    Thread.Sleep(350);
                }

                lock (o)
                {
                    if (accumulatedVolumePresses != 0)
                    {
                        SetNewVolume(accumulatedVolumePresses);
                        lastVolumeChange = DateTime.Now;
                    }
                    accumulatedVolumePresses = 0;
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

        private void VolumeKeyPressed(int presses)
        {
            lock (o)
            {
                accumulatedVolumePresses += presses;
            }
            waitForKeyPress.Set();
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
