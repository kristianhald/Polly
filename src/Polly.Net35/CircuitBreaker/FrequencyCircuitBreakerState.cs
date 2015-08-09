using System;
using System.Collections.Generic;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class FrequencyCircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfBreak;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private readonly long _durationOfExceptionRelevance;
        private readonly long _thresholdAverageTicksBetweenExceptions;

        private int _count;
        private long _averageTicksBetweenExceptions;
        private long _ticksLastException;

        private DateTime _blockedTill;
        private Exception _lastException;

        private readonly object _lock = new object();

        public FrequencyCircuitBreakerState(TimeSpan durationOfExceptionRelevance, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            _durationOfBreak = durationOfBreak;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _durationOfExceptionRelevance = durationOfExceptionRelevance.Ticks;
            _thresholdAverageTicksBetweenExceptions = _durationOfExceptionRelevance / (_exceptionsAllowedBeforeBreaking - 1);

            ResetCalculation();

            Reset();
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
            }
        }

        private void ResetCalculation()
        {
            _count = 0;
            _averageTicksBetweenExceptions = 0;
            _lastException = new InvalidOperationException("This exception should never be thrown");
        }

        public void TryBreak(Exception ex)
        {
            using (TimedLock.Lock(_lock))
            {
                _lastException = ex;

                // If the gap between the two most recent exceptions exceeds the duration of exception relevance, the threshold frequency to trigger breaking the circuit cannot possibly have been met.  The timing of this (and past) exceptions is therefore irrelevant, and we can reset the average calculations and return more quickly.  This clause additionally avoids large outliers affecting the averaging.  In a CircuitBreaker configured to break on N exceptions per minute, we should discard an (irrelevant) outlier of an hour rather than have it affect ongoing averages. 
                if (SystemClock.UtcNow().Ticks - _ticksLastException > _durationOfExceptionRelevance)
                {
                    ResetCalculation();
                    return;
                }

                if (_count < _exceptionsAllowedBeforeBreaking) { _count += 1; }

                if (_count > 1)
                {
                    _averageTicksBetweenExceptions = (
                        (_averageTicksBetweenExceptions * (_count - 2))
                        + (SystemClock.UtcNow().Ticks - _ticksLastException))
                        / (_count - 1);
                }

                _ticksLastException = SystemClock.UtcNow().Ticks;

                if (_count >= _exceptionsAllowedBeforeBreaking
                    && _averageTicksBetweenExceptions < _thresholdAverageTicksBetweenExceptions)
                {
                    BreakTheCircuit();
                }
            }
        }

        void BreakTheCircuit()
        {
            var willDurationTakeUsPastDateTimeMaxValue = _durationOfBreak > DateTime.MaxValue - SystemClock.UtcNow();
            _blockedTill = willDurationTakeUsPastDateTimeMaxValue ?
                               DateTime.MaxValue :
                               SystemClock.UtcNow() + _durationOfBreak;
            ResetCalculation();
        }
    }
}