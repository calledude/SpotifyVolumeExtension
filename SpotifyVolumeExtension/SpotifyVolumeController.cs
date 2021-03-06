﻿using LowLevelInput.Hooks;
using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class SpotifyVolumeController : VolumeController
    {
        private readonly MediaKeyListener _mkl;
        private readonly SpotifyClient _spotifyClient;
        private int _lastVolume;

        public SpotifyVolumeController(SpotifyClient sc)
        {
            _spotifyClient = sc;
            _mkl = new MediaKeyListener();

            var debounceConfig = (TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250));
            _mkl.SubscribeTo(VirtualKeyCode.VolumeUp, debounceConfig);
            _mkl.SubscribeTo(VirtualKeyCode.VolumeDown, debounceConfig);
        }

        protected override async Task Start()
        {
            await base.Start();

            _lastVolume = BaselineVolume;
            _mkl.SubscribedKeyPressed += VolumeKeyPressed;
        }

        protected override void Stop()
        {
            _mkl.SubscribedKeyPressed -= VolumeKeyPressed;
            base.Stop();
        }

        //Spotify web api might not be fast enough to realize we have begun playing music
        //therefore we wait for it to catch up. This happens when we press the play-key just as spotify is starting.
        protected override async Task<int> GetBaselineVolume()
        {
            var playbackContext = await Retry.Wrap(() => _spotifyClient.Api.GetPlaybackAsync());
            while (playbackContext?.Device == null)
            {
                await Task.Delay(500);
                playbackContext = await Retry.Wrap(() => _spotifyClient.Api.GetPlaybackAsync());
            }
            return playbackContext.Device.VolumePercent;
        }

        private async Task VolumeKeyPressed(MediaKeyEventArgs m)
        {
            using (_ = await _lock.EnterAsync())
            {
                if (m.Key == VirtualKeyCode.VolumeUp) BaselineVolume += m.Presses;
                else BaselineVolume -= m.Presses;

                if (BaselineVolume > 100) BaselineVolume = 100;
                else if (BaselineVolume < 0) BaselineVolume = 0;

                await SetNewVolume();
            }
        }

        protected override async Task SetNewVolume()
        {
            if (_lastVolume == BaselineVolume)
                return;

            var err = await Retry.Wrap(() => _spotifyClient.Api.SetVolumeAsync(BaselineVolume));

            if (err != null && err.Error == null)
            {
                Console.WriteLine($"[{Name}] Changed volume to {BaselineVolume.ToString()}%");
                _lastVolume = BaselineVolume;
            }
            else
            {
                Console.WriteLine($"[{Name}] Failed to change volume.");
                BaselineVolume = _lastVolume;
            }
        }

        protected override void Dispose(bool disposing)
            => _mkl.Dispose();
    }
}
