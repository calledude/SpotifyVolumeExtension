using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using Open.WinKeyboardHook;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyListener
    {
        public event Action<MediaKeyEventArgs> SubscribedKeyPressed;
        private readonly KeyboardInterceptor key;
        private int presses = 0;
        private readonly List<Keys> subscribedKeys;
        private readonly Thread mklThread;

        public MediaKeyListener()
        {
            subscribedKeys = new List<Keys>();
            key = new KeyboardInterceptor();
            key.KeyDown += key_KeyDown;
            key.KeyUp += key_KeyUp;

            mklThread = new Thread(() =>
            {
                key.StartCapturing();
                Application.Run();
            });
        }

        public void Start()
        {
            if (mklThread.ThreadState == ThreadState.Running)
                throw new InvalidOperationException("MediaKeyListener is already started");
            mklThread.Start();
        }

        public void SubscribeTo(Keys key) => subscribedKeys.Add(key);

        private void key_KeyUp(object sender, KeyEventArgs e)
        {
            if(subscribedKeys.Any(key => key == e.KeyCode))
            {
                var eventArgs = new MediaKeyEventArgs()
                {
                    Presses = presses,
                    Key = e.KeyCode
                };
                SubscribedKeyPressed?.Invoke(eventArgs);
                presses = 0;
            }
        }

        private void key_KeyDown(object sender, KeyEventArgs e)
        {
            if (subscribedKeys.Any(key => key == e.KeyCode))
            {
                presses++;
            }
        }
    }
}
