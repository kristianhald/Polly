using System;
using System.Collections;
using System.Collections.Generic;
using Polly.Utilities;

namespace Polly.CircuitBreaker
{
    internal class FrequencyCircuitBreakerState : ICircuitBreakerState
    {
        private readonly TimeSpan _durationOfExceptionRelevance;
        private readonly TimeSpan _durationOfBreak;
        private readonly int _exceptionsAllowedBeforeBreaking;
        private DateTime _blockedTill;
        private Exception _lastException;
        private readonly object _lock = new object();
        private readonly Queue<DateTime> exceptionTimes;

        public FrequencyCircuitBreakerState(TimeSpan durationOfExceptionRelevance, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            _durationOfBreak = durationOfBreak;
            _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            _durationOfExceptionRelevance = durationOfExceptionRelevance;
            exceptionTimes = new Queue<DateTime>(exceptionsAllowedBeforeBreaking);

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

                _lastException = new InvalidOperationException("This exception should never be thrown");
            }
        }

        public void TryBreak(Exception ex)
        {
            using (TimedLock.Lock(_lock))
            {
                _lastException = ex;

                DateTime timeOfThisException = SystemClock.UtcNow();
                exceptionTimes.Enqueue(timeOfThisException);

                if (exceptionTimes.Count >= _exceptionsAllowedBeforeBreaking)
                {
                    while (exceptionTimes.Count > _exceptionsAllowedBeforeBreaking) { exceptionTimes.Dequeue(); }
                    DateTime timeNExceptionsAgo = exceptionTimes.Dequeue();

                    if (DateTime.MinValue + _durationOfExceptionRelevance >= timeOfThisException ||
                        timeNExceptionsAgo >= timeOfThisException - _durationOfExceptionRelevance)
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
        }
    }
}