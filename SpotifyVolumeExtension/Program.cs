using System.Windows.Forms;

namespace SpotifyVolumeExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            VolumeGuard vg = new VolumeGuard();
            vg.Start();
            SpotifyClient sc = new SpotifyClient();
            sc.Start();
            SpotifyVolumeController svc = new SpotifyVolumeController(sc);
            svc.Start();
            Application.Run();
        }
    }
}
