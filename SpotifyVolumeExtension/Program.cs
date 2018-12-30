using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            
            SpotifyClient sc = new SpotifyClient();
            sc.Start();
            VolumeGuard vg = new VolumeGuard(sc);
            vg.Start();
            Application.Run();
        }
    }
}
