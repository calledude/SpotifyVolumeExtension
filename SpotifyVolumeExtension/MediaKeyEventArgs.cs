using System;

namespace SpotifyVolumeExtension
{
    public class MediaKeyEventArgs : EventArgs
    {
        public MediaKeyEventArgs()
        {
            When = DateTime.Now;
        }
        public bool IsVolumeUp { get; set; }
        public int Presses { get; set; }
        public DateTime When { get; }
    }
}
