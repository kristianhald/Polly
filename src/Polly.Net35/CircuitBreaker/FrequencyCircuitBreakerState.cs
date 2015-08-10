using System;
using System.Collections.Generic;
using System.Linq;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class FrequencyCircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfBreak;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly int _inTotalCallCount;

        private int _count;
        private readonly bool[] _failures;
        private int _writeLocation;

        private DateTime _blockedTill;
        private Exception _lastException;

        private readonly object _lock = new object();

        public FrequencyCircuitBreakerState(int exceptionsAllowedBeforeBreaking, int inTotalCallCount, TimeSpan durationOfBreak)
        {
            _durationOfBreak = durationOfBreak;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _inTotalCallCount = inTotalCallCount;

            _failures = new bool[inTotalCallCount];
            _writeLocation = 0;
            ResetMetric();

            _blockedTill = DateTime.MinValue;
        }

        public Exception LastException
        {
            get
            {
                using (TimedLock.Lock(_lock))
                {
                    return _lastException;
                }
            }
        }

        public bool IsBroken
        {
            get
            {
                using (TimedLock.Lock(_lock))
                {
                    return SystemClock.UtcNow() < _blockedTill;
                }
            }
        }

        public void Reset()
        {
            using (TimedLock.Lock(_lock))
            {
                _blockedTill = DateTime.MinValue;
                RecordOutcome(true);
            }
        }

        private void ResetMetric()
        {
            for (int i = 0; i < _inTotalCallCount; i++)
            {
                _failures[i] = false;
            }
        }

        private void RecordOutcome(bool success)
        {
            _failures[_writeLocation] = !success;
            _writeLocation = (_writeLocation + 1) % _inTotalCallCount;

        }

        public void TryBreak(Exception ex)
        {
            using (TimedLock.Lock(_lock))
            {
                _lastException = ex;

                if (_count < _exceptionsAllowedBeforeBreaking) { _count += 1; }

                RecordOutcome(false);

                if (_count >= _exceptionsAllowedBeforeBreaking)
                {
                    if (_failures.Count(s => s) > _exceptionsAllowedBeforeBreaking) // Could use a for loop to count rather than .Count(), given simplicity of _failures structure
                    {
                        BreakTheCircuit();
                    }
                }
            }
        }

        void BreakTheCircuit()
        {
            var willDurationTakeUsPastDateTimeMaxValue = _durationOfBreak > DateTime.MaxValue - SystemClock.UtcNow();
            _blockedTill = willDurationTakeUsPastDateTimeMaxValue ?
                               DateTime.MaxValue :
                               SystemClock.UtcNow() + _durationOfBreak;

            ResetMetric();
        }
    }
}