using System;
using System.Timers;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        static private MediaKeyListener mkl;
        private SpotifyClient sc;
        private int lastVolume;
        private int spotifyVolume;
        private DateTime lastVolumePress;
        private Timer blockTimer;
        private bool blockUpdates;

        public SpotifyVolumeController(SpotifyClient sc)
        {
            this.sc = sc;
            mkl = new MediaKeyListener();
            mkl.MediaKeyPressed += ChangeSpotifyVolume;
            blockTimer = new Timer(500);
            blockTimer.Elapsed += UnblockUpdates;
            blockTimer.AutoReset = false;
        }

        public void Start()
        {
            mkl.Start();

            var pc = sc.GetPlaybackContext();
            spotifyVolume = pc.Device.VolumePercent; //Get initial spotify-volume

            Console.WriteLine("[SpotifyVolumeController] Started.");
        }

        private void UpdateVolume()
        {
            if (blockUpdates) return;
            if (lastVolume != spotifyVolume)
            {
                lastVolume = spotifyVolume;
                SetNewVolume(spotifyVolume);
            }
        }

        private void UnblockUpdates(object sender, ElapsedEventArgs e)
        {
            blockUpdates = false;
            UpdateVolume();
        }


        private void ChangeSpotifyVolume(MediaKeyEventArgs m)
        {
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
            UpdateVolume();
            lastVolumePress = DateTime.Now;
        }

        private void SetNewVolume(int volume)
        {
            var error = sc.Api.SetVolume(volume);
            if (error.HasError())
            {
                Console.WriteLine(error.Error.Message);
            }
            else
            {
                Console.WriteLine($"[SpotifyVolumeController] Changed volume to {spotifyVolume}%");
            }
        }
    }
}
