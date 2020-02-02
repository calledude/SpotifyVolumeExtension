using LowLevelInput.Hooks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyListener : IDisposable
    {
        public event Action<MediaKeyEventArgs> SubscribedKeyPressed;
        private readonly InputManager _inputManager;
        private int _presses;
        private readonly Dictionary<VirtualKeyCode, (TimeSpan, TimeSpan)> _debounceConfig;
        private DateTime _lastEvent;

        public MediaKeyListener()
        {
            _inputManager = new InputManager();
            _inputManager.Initialize(false);

            _debounceConfig = new Dictionary<VirtualKeyCode, (TimeSpan, TimeSpan)>();
        }

        private async void KeyPressedEvent(VirtualKeyCode key, KeyState state)
        {
            if (KeyState.Up == state)
            {
                var (minimumWait, penalty) = _debounceConfig[key];
                if (minimumWait != default
                    && penalty != default
                    && DateTime.Now - _lastEvent < minimumWait)
                {
                    await Task.Delay(penalty);
                }

                SubscribedKeyPressed?.Invoke(new MediaKeyEventArgs()
                {
                    Presses = _presses,
                    Key = key
                });

                _lastEvent = DateTime.Now;
                _presses = 0;
            }
            else if (KeyState.Down == state)
            {
                _presses++;
            }
        }

        public void SubscribeTo(VirtualKeyCode key, (TimeSpan, TimeSpan) debounceConfig = default)
        {
            _debounceConfig.Add(key, debounceConfig);
            _inputManager.RegisterEvent(key, KeyPressedEvent);
        }

        public void Dispose()
        {
            _inputManager.Dispose();
        }
    }
}