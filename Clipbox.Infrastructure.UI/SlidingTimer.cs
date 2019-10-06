using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Threading;

namespace Clipbox.Infrastructure.UI
{
    /// <summary>
    /// A timer with sliding interval, which fires its Elapsed event
    /// after certain period of inactivity. This class is entirely improvised, but
    /// another implementation can be found here:
    /// https://www.codeproject.com/Tips/709218/Sliding-Timer
    /// TODO: see if the link above will provide a better solution.
    /// </summary>
    public class SlidingTimer : IDisposable
    {
        private const int DEFAULT_INTERVAL_MS = 50;

        private DispatcherTimer _t;

        private object _lockHandle;

        private TimeSpan _interval;

        private volatile bool _isTimerActive;
        
        private ConcurrentBag<DateTime> _slideCache;

        public event EventHandler Elapsed;

        public SlidingTimer()
        {
            _lockHandle = new object();

            _interval = TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_MS);

            _slideCache = new ConcurrentBag<DateTime>();
            _t = new DispatcherTimer
            {
                IsEnabled = true,
                Interval = _interval
            };

            _t.Tick += _t_Tick;
        }

        private void _t_Tick(object sender, EventArgs e)
        {
            lock (_lockHandle)
            {
                if (_isTimerActive)
                {
                    // TODO: ConcurrentBag does not guarantee the order of the items,
                    // so what does Last() extention method has in this context?
                    // Will it really return the latest item, or I will have to
                    // order the collection first?
                    //if (_slideCache.Any(c => c.Add(_interval) > DateTime.Now))
                    DateTime lastSlideDateTime = _slideCache.OrderBy(t => t).LastOrDefault();
                    if (lastSlideDateTime != default(DateTime) &&
                        lastSlideDateTime.Add(_interval) > DateTime.Now)
                    {
                        Restart();
                        return;
                    }
                    else if (_slideCache?.Count > 0)
                    {
                        _slideCache = new ConcurrentBag<DateTime>();
                        _isTimerActive = false;

                        Elapsed?.Invoke(this, new EventArgs());
                    }
                }
            }
        }

        public double IntervalMs
        {
            get
            {
                return _interval.TotalMilliseconds;
            }
            set
            {
                if (_interval.TotalMilliseconds != value)
                {
                    _interval = TimeSpan.FromMilliseconds(value);
                    _t.Interval = _interval;
                }
            }
        }

        /// <summary>
        /// Postpones the firing of the "Elapsed" event by the interval in the IntervalMs property.
        /// This operation does not do any explicit locking, so it can complete as fast as possible.
        /// </summary>
        public void Slide()
        {
            // If anyone calls Slide() after the Ellapsed event has fired,
            // start waiting again...
            if (!_isTimerActive)
            {
                _isTimerActive = true;
            }

            // I hope that the internal locking that goes on in ConcurrentBag<T>.Add()
            // will not slow the method down. From what I have read on the topic, it shouldn't.
            _slideCache.Add(DateTime.Now);
        }

        private void Restart()
        {
            _t.Stop();
            _t.Start();
        }

        public void Dispose()
        {
            _t.Stop();
            _t.Tick -= _t_Tick;
        }
    }
}
