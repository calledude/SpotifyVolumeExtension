using System;

namespace SpotifyVolumeExtension
{
    public class SpotifyVolumeController
    {
        private MediaKeyListener mkl;
        private SpotifyClient sc;
        private int lastVolume = 0;
        private int spotifyVolume = 0;

        public SpotifyVolumeController(SpotifyClient sc)
        {
            this.sc = sc;
            mkl = new MediaKeyListener();
            mkl.MediaKeyPressed += ChangeSpotifyVolume;

        }

        private void UpdateVolume()
        {
            if (lastVolume != spotifyVolume)
            {
                lastVolume = spotifyVolume;
                SetNewVolume(spotifyVolume);
            }
        }

        public void Start()
        {
            mkl.Start();

            var pc = sc.GetPlaybackContext();
            spotifyVolume = pc.Device.VolumePercent; //Get initial spotify-volume

            Console.WriteLine("[SpotifyVolumeController] Started.");
        }

        private void ChangeSpotifyVolume(MediaKeyEventArgs m)
        {
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
        }

        private void SetNewVolume(int volume)
        {
            var error = sc.Api.SetVolume(volume);
            if (error.HasError())
            {
                Console.WriteLine(error.Error.Status);
            }
        }
    }
}
