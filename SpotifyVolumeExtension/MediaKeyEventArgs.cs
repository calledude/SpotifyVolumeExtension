using LowLevelInput.Hooks;
using System;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyEventArgs : EventArgs
    {
        public MediaKeyEventArgs()
        {
            When = DateTime.Now;
        }

        public VirtualKeyCode Key { get; set; }
        public int Presses { get; set; }
        public DateTime When { get; }
    }
}
