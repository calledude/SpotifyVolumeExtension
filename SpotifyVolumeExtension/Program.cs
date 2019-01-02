using System.Windows.Forms;
using SpotifyAPI.Web;
namespace SpotifyVolumeExtension
{
    class Program
    {
        static void Main(string[] args)
        {
            VolumeGuard vg = new VolumeGuard();
            vg.Start();
            SpotifyClient sc = new SpotifyClient(AuthType.Implicit);
            sc.Start();
            SpotifyVolumeController svc = new SpotifyVolumeController(sc);
            svc.Start();
            Application.Run();
        }
    }
}
