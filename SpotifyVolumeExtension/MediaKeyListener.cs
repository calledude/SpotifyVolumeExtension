using System;
using LowLevelInput.Hooks;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyListener : IDisposable
    {
        public event Action<MediaKeyEventArgs> SubscribedKeyPressed;
        private readonly InputManager inputManager;
        private int presses;

        public MediaKeyListener()
        {
            inputManager = new InputManager();
            inputManager.Initialize();
            inputManager.CaptureMouseMove = false;
        }

        private void KeyPressedEvent(VirtualKeyCode key, KeyState state)
        {
            if (KeyState.Down == state)
            {
                presses++;
            }
            else if(KeyState.Up == state)
            {
                var eventArgs = new MediaKeyEventArgs()
                {
                    Presses = presses,
                    Key = key
                };
                SubscribedKeyPressed?.Invoke(eventArgs);
                presses = 0;
            }
        }

        public void SubscribeTo(VirtualKeyCode key)
        {
            inputManager.RegisterEvent(key, KeyPressedEvent);
        }

        public void Dispose()
        {
            inputManager.Dispose();
        }
    }
}