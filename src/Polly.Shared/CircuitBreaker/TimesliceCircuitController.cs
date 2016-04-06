using System;
using System.Collections.Generic;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class TimesliceCircuitController : CircuitStateController
    {
        private readonly HealthMetrics _metrics;
        private readonly double _failureThreshold;
        private readonly int _minimumThroughput;

        private class HealthMetricSlice // If only one metric at a time is ever retained, this could be removed (for performance) and the properties incorporated in to the parent class.
        {
            public int Successes { get; set; }
            public int Failures { get; set; }
            public long StartedAt { get; set; }
        }

        private class HealthMetric
        {
            public int Successes { get; set; }
            public int Failures { get; set; }
        }

        private class HealthMetrics
        {
            private readonly long _timeDuration;
            private readonly long _timesliceDuration;

            private HealthMetricSlice _currentSlice;
            private readonly Queue<HealthMetricSlice> _slices = new Queue<HealthMetricSlice>();

            public HealthMetrics(TimeSpan timesliceDuration)
            {
                _timeDuration = timesliceDuration.Ticks;
                _timesliceDuration = _timeDuration / 10; // At the moment it always selects 10 buckets
            }

            public void IncrementSuccess_NeedsLock()
            {
                // (future enhancement) Any operation in this method disposing of an existing _metric could emit it to a delegate, for health-monitoring capture ...

                ActualiseCurrentSlice_NeedsLock();

                _currentSlice.Successes++;
            }

            public void IncrementFailure_NeedsLock()
            {
                ActualiseCurrentSlice_NeedsLock();

                _currentSlice.Failures++;
            }

            public void Reset_NeedsLock()
            {
                _currentSlice = null;
                _slices.Clear();
            }

            public HealthMetric GetHealthCounts_NeedsLock()
            {
                long now = SystemClock.UtcNow().Ticks;
                while (now - _slices.Peek().StartedAt >= _timeDuration)
                    _slices.Dequeue();

                int successes = 0;
                int failures = 0;
                foreach (var slice in _slices)
                {
                    successes += slice.Successes;
                    failures += slice.Failures;
                }

                return new HealthMetric
                {
                    Successes = successes,
                    Failures = failures
                };
            }

            private void ActualiseCurrentSlice_NeedsLock()
            {
                long now = SystemClock.UtcNow().Ticks;
                if (_currentSlice == null || now - _currentSlice.StartedAt >= _timesliceDuration)
                {
                    _currentSlice = new HealthMetricSlice { StartedAt = now };
                    _slices.Enqueue(_currentSlice);
                }
            }
        }

        public TimesliceCircuitController(double failureThreshold, TimeSpan timesliceDuration, int minimumThroughput, TimeSpan durationOfBreak, Action<Exception, TimeSpan, Context> onBreak, Action<Context> onReset, Action onHalfOpen) : base(durationOfBreak, onBreak, onReset, onHalfOpen)
        {
            _metrics = new HealthMetrics(timesliceDuration);
            _failureThreshold = failureThreshold;
            _minimumThroughput = minimumThroughput;
        }

        public override void OnCircuitReset(Context context)
        {
            using (TimedLock.Lock(_lock))
            {
                // Is only null during initialization of the current class
                // as the variable is not set, before the base class calls
                // current method from constructor.
                _metrics?.Reset_NeedsLock();

                ResetInternal_NeedsLock(context);
            }
        }

        public override void OnActionSuccess(Context context)
        {
            using (TimedLock.Lock(_lock))
            {
                if (_circuitState == CircuitState.HalfOpen) { OnCircuitReset(context); }

                _metrics.IncrementSuccess_NeedsLock();
            }
        }

        public override void OnActionFailure(Exception ex, Context context)
        {
            using (TimedLock.Lock(_lock))
            {
                _lastException = ex;

                if (_circuitState == CircuitState.HalfOpen)
                {
                    Break_NeedsLock(context);
                    return;
                }

                _metrics.IncrementFailure_NeedsLock();

                var metric = _metrics.GetHealthCounts_NeedsLock();
                int throughput = metric.Failures + metric.Successes;
                if (throughput >= _minimumThroughput && ((double)metric.Failures) / throughput >= _failureThreshold)
                {
                    Break_NeedsLock(context);
                }
                
            }
        }
    }
}
