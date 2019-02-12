using System;
using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    public sealed class MediaKeyEventArgs : EventArgs
    {
        public MediaKeyEventArgs()
        {
            When = DateTime.Now;
        }

        public Keys Key { get; set; }
        public int Presses { get; set; }
        public DateTime When { get; }
    }
}
