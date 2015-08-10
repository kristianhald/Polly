using System;
using Polly.CircuitBreaker;

namespace Polly
{
    /// <summary>
    /// Fluent API for defining a Circuit Breaker <see cref="Policy"/>. 
    /// </summary>
    public static class CircuitBreakerSyntaxAsync
    {
        /// <summary>
        /// <para> Builds a <see cref="Policy"/> that will function like a Circuit Breaker.</para>
        /// <para>The circuit will break after <paramref name="exceptionsAllowedBeforeBreaking"/>
        /// exceptions that are handled by this policy are raised. The circuit will stay
        /// broken for the <paramref name="durationOfBreak"/>. Any attempt to execute this policy
        /// while the circuit is broken, will immediately throw a <see cref="BrokenCircuitException"/> containing the exception 
        /// that broke the circuit.
        /// </para>
        /// <para>If the first action after the break duration period results in an exception, the circuit will break
        /// again for another <paramref name="durationOfBreak"/>, otherwise it will reset.
        /// </para>
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions that are allowed before opening the circuit.</param>
        /// <param name="durationOfBreak">The duration the circuit will stay open before resetting.</param>
        /// <returns>The policy instance.</returns>
        /// <remarks>(see "Release It!" by Michael T. Nygard fi)</remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">exceptionsAllowedBeforeBreaking;Value must be greater than zero.</exception>
        public static Policy CircuitBreakerAsync(this PolicyBuilder policyBuilder, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak)
        {
            if (exceptionsAllowedBeforeBreaking <= 0) throw new ArgumentOutOfRangeException("exceptionsAllowedBeforeBreaking", "Value must be greater than zero.");

            var policyState = new CircuitBreakerState(exceptionsAllowedBeforeBreaking, durationOfBreak);
            return new Policy(action => CircuitBreakerPolicy.ImplementationAsync(action, policyBuilder.ExceptionPredicates, policyState));

        }

        /// <summary>
        /// <para> Builds a <see cref="Policy" /> that will function like a Circuit Breaker.</para>
        /// <para>The circuit will break if exceptions that are handled by this policy are raised such that
        /// <paramref name="exceptionsAllowedBeforeBreaking" /> or more occur in any run of <paramref name="inTotalCallCount" /> calls.  For example, you might define that your failure threshold is 3/10, to break on 3 exceptions in any 10 calls.  The circuit will stay
        /// broken for the <paramref name="durationOfBreak" />. Any attempt to execute this policy
        /// while the circuit is broken, will immediately throw a <see cref="BrokenCircuitException" />
        /// containing the exception that broke the circuit.
        /// </para>
        /// <para>After the break duration, the circuit will open again.  Subsequent calls will break the circuit according to the original criteria: if the proportion of handled failures exceeds <paramref name="exceptionsAllowedBeforeBreaking" /> per <paramref name="inTotalCallCount" /> calls.
        /// </para>
        /// </summary>
        /// <param name="policyBuilder">The policy builder.</param>
        /// <param name="exceptionsAllowedBeforeBreaking">The number of exceptions that are allowed before opening the circuit.</param>
        /// <param name="inTotalCallCount">A total call count which the number of exceptions should be considered a proportion of.</param>
        /// <param name="durationOfBreak">The duration the circuit will stay open before resetting.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">exceptionsAllowedBeforeBreaking;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">inTotalCallCount;Value must be greater than exceptionsAllowedBeforeBreaking.</exception>
        /// <remarks>(see "Release It!" by Michael T. Nygard fi)</remarks>
        public static Policy CircuitBreaker(this PolicyBuilder policyBuilder, int exceptionsAllowedBeforeBreaking, int inTotalCallCount, TimeSpan durationOfBreak)
        {
            if (exceptionsAllowedBeforeBreaking <= 0) throw new ArgumentOutOfRangeException("exceptionsAllowedBeforeBreaking", "Value must be greater than zero.");
            if (!(exceptionsAllowedBeforeBreaking < inTotalCallCount)) throw new ArgumentOutOfRangeException("inTotalCallCount", "Value must be greater than exceptionsAllowedBeforeBreaking.");

            var policyState = new FrequencyCircuitBreakerState(exceptionsAllowedBeforeBreaking, inTotalCallCount, durationOfBreak);
            return new Policy(action => CircuitBreakerPolicy.ImplementationAsync(action, policyBuilder.ExceptionPredicates, policyState));
        }
    }
}