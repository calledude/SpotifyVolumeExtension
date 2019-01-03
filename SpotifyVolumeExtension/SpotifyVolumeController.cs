using SpotifyAPI.Web.Models;
using System;
using System.Timers;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController : IObserver<SpotifyStatusChanged>
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private volatile int lastVolume;
        private volatile int spotifyVolume;
        private DateTime lastVolumePress;
        private Timer blockTimer;
        private bool blockUpdates;
        private PlaybackContext playbackContext;
        private System.Threading.Semaphore sem = new System.Threading.Semaphore(1, 1);

        public SpotifyVolumeController(SpotifyClient sc)
        {
            this.sc = sc;
            mkl = new MediaKeyListener();
            mkl.MediaKeyPressed += ChangeSpotifyVolume;

            blockTimer = new Timer(500);
            blockTimer.Elapsed += UnblockUpdates;
            blockTimer.AutoReset = false;
        }

        public void Start(SpotifyMonitor sm)
        {
            spotifyVolume = GetCurrentVolume(); //Get initial spotify-volume
            sm.Subscribe(this);
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

        private void UnblockUpdates(object sender, ElapsedEventArgs e)
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
                blockTimer.Start();
            }

            if (m.Key == KeyType.Up)
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
            var error = sc.Api.SetVolume(volume);
            if (error.HasError())
            {
                sc.HandleError(error);
            }
            else
            {
                Console.WriteLine($"[SpotifyVolumeController] Changed volume to {spotifyVolume}%");
            }
        }

        public void OnNext(SpotifyStatusChanged value)
        {
            if (value.Status)
            {
                mkl.Start();
                spotifyVolume = GetCurrentVolume();
            }
            else
            {
                mkl.Stop();
            }
            Console.WriteLine("[SpotifyVolumeController] " + (value.Status ? "Started." : "Stopped."));
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.StackTrace);
        }

        public void OnCompleted()
        {
            Console.WriteLine("[VolumeGuard] Stopped.");
        }
    }
}
