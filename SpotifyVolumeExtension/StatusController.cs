using System;
using System.Threading.Tasks;

namespace SpotifyVolumeExtension
{
    public sealed class StatusController
    {
        private readonly SpotifyMonitor sm;
        private bool lastState;

        public StatusController(SpotifyMonitor sm)
        {
            this.sm = sm ?? throw new ArgumentNullException(nameof(sm));
        }

        public void CheckState()
        {
            Task.Run(async () =>
            {
                //Give spotify a chance to catch up, in case 'state' is in fact the state we are looking to change to. 
                await Task.Delay(500);
                var state = await sm.GetPlayingStatus();

                if (state == lastState) return;

                lastState = state;
                OnStateChange(state);
            });
        }

        private void OnStateChange(bool newState)
        {
            if (newState)
            {
                VolumeController.StartAll();
            }
            else
            {
                VolumeController.StopAll();
            }
        }
    }
}
