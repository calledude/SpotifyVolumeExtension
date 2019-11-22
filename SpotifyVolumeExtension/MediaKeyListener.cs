using LowLevelInput.Hooks;
using System;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyListener : IDisposable
    {
        public event Action<MediaKeyEventArgs> SubscribedKeyPressed;
        private readonly InputManager _inputManager;
        private int _presses;

        public MediaKeyListener()
        {
            _inputManager = new InputManager();
            _inputManager.Initialize(false);
        }

        private void KeyPressedEvent(VirtualKeyCode key, KeyState state)
        {
            if (KeyState.Up == state)
            {
                SubscribedKeyPressed?.Invoke(new MediaKeyEventArgs()
                {
                    Presses = _presses,
                    Key = key
                });
                _presses = 0;
            }
            else if (KeyState.Down == state)
            {
                _presses++;
            }
        }

        public void SubscribeTo(VirtualKeyCode key)
        {
            _inputManager.RegisterEvent(key, KeyPressedEvent);
        }

        public void Dispose()
        {
            _inputManager.Dispose();
        }
    }
}