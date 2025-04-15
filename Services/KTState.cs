using Timer = System.Timers.Timer;
using System.Diagnostics;
using Microsoft.JSInterop;

namespace KTSimulator.Services
{
    public class KTState
    {
        public bool IsInBunker { get; private set; } = true;
        public string CurrentCity { get; private set; } = "Oslo";

        public event Action? OnChange;

        private readonly Timer _timer;
        private Stopwatch? stopwatch;
        private readonly IJSRuntime _js;

        public List<(DateTime Timestamp, TimeSpan Duration)> BunkerLog { get; } = new();

        private string timerDisplay = "00:00.000";

        public KTState(IJSRuntime js)
        {
            _js = js;

            _timer = new Timer(50); // 20 oppdateringer per sekund
            _timer.Elapsed += (s, e) =>
            {
                if (!IsInBunker && stopwatch?.IsRunning == true)
                {
                    timerDisplay = stopwatch.Elapsed.ToString("mm\\:ss\\.fff");
                    NotifyStateChanged();
                }
            };
            _timer.Start();
        }

        public string GetFormattedTimer()
        {
            return timerDisplay;
        }

        public void ToggleBunker()
        {
            IsInBunker = !IsInBunker;

            if (IsInBunker)
            {
                if (stopwatch != null && stopwatch.IsRunning)
                {
                    stopwatch.Stop();

                    // Logg til Supabase
                    var durationMs = (int)stopwatch.Elapsed.TotalMilliseconds;
                    _ = _js.InvokeVoidAsync("logTime", durationMs);

                    BunkerLog.Insert(0, (DateTime.Now, stopwatch.Elapsed));
                }

                stopwatch = null;
                timerDisplay = "00:00.000";
            }
            else
            {
                stopwatch = Stopwatch.StartNew();
            }

            NotifyStateChanged();
        }

        public void FlyTo(string city)
        {
            if (!string.IsNullOrEmpty(city))
            {
                CurrentCity = city;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
