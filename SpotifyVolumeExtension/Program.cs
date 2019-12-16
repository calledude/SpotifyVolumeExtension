using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public static class Program
    {
        public static async Task Main()
        {
            var sc = new SpotifyClient();
            await sc.Authenticate();

            var sm = new SpotifyMonitor(sc);
            sm.Start();

            var cc = new ConsoleController();
            cc.RegisterDisposable(sc);
            cc.RegisterDisposable(sm);
            cc.Start();
        }
    }
}
