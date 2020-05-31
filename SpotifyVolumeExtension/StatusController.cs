using LowLevelInput.Hooks;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

namespace SpotifyVolumeExtension
{
    public sealed class StatusController : IDisposable
    {
        private readonly SpotifyMonitor _sm;
        private readonly MediaKeyListener _mkl;
        private bool _lastState;
        private readonly ConcurrentQueue<Func<Task>> _apiCallQueue;
        private readonly Timer _queueTimer;
        private readonly AsyncMonitor _startLock;

        public StatusController(SpotifyMonitor sm)
        {
            _startLock = new AsyncMonitor();
            _apiCallQueue = new ConcurrentQueue<Func<Task>>();
            _queueTimer = new Timer(500);
            _queueTimer.Elapsed += RunQueuedApiCalls;
            _queueTimer.Enabled = true;

            _mkl = new MediaKeyListener();

            _mkl.SubscribeTo(VirtualKeyCode.MediaPlayPause);
            _mkl.SubscribeTo(VirtualKeyCode.MediaStop);
            _mkl.SubscribedKeyPressed += CheckStateInternal;

            _sm = sm ?? throw new ArgumentNullException(nameof(sm));
        }

        private async void RunQueuedApiCalls(object sender, ElapsedEventArgs e)
        {
            if (_apiCallQueue.TryDequeue(out var task))
            {
                await task.Invoke();
            }
        }

        private async Task CheckStateInternal(MediaKeyEventArgs m)
        {
            bool newState;
            if (m.Key == VirtualKeyCode.MediaPlayPause)
            {
                newState = !_lastState;
            }
            else //VirtualKeyCode.MediaStop
            {
                newState = false;
            }

            // Announce state change, wait 500ms, check again to make sure it was the correct one
            // This is a bit of a hack to enable more responsive volume-lock toggling
            await OnStateChange(newState);

            await Task.Delay(500);
            CheckState();
        }

        public void CheckState()
        {
            if (!_apiCallQueue.IsEmpty)
                return;

            _apiCallQueue.Enqueue(async () =>
            {
                var state = await _sm.GetPlayingStatus();
                await OnStateChange(state);
            });
        }

        private async Task OnStateChange(bool newState)
        {
            if (newState == _lastState)
                return;

            _lastState = newState;
            using (_ = await _startLock.EnterAsync())
            {
                if (newState)
                {
                    await VolumeController.StartAll();
                }
                else
                {
                    VolumeController.StopAll();
                }
            }
        }

        public void Dispose()
            => _mkl.Dispose();
    }
}
