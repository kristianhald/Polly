﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Polly.CircuitBreaker;
using Polly.Specs.Helpers;
using Polly.Utilities;
using Xunit;

namespace Polly.Specs
{
    public class TimesliceCircuitBreakerSpecs : IDisposable
    {
        #region Configuration tests

        [Fact]
        public void Should_be_able_to_handle_a_duration_of_timespan_maxvalue()
        {
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.MaxValue);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_throw_if_failure_threshold_is_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("failureThreshold");
        }

        [Fact]
        public void Should_throw_if_failure_threshold_is_less_than_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(-0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("failureThreshold");
        }

        [Fact]
        public void Should_be_able_to_handle_a_failure_threshold_of_one()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(1.0, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            action.ShouldNotThrow();
        }

        [Fact]
        public void Should_throw_if_failure_threshold_is_greater_than_one()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(1.01, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("failureThreshold");
        }

        [Fact]
        public void Should_throw_if_timeslice_duration_is_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.Zero, 4, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("timesliceDuration");
        }

        [Fact]
        public void Should_throw_if_timeslice_duration_is_less_than_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, -TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("timesliceDuration");
        }

        [Fact]
        public void Should_throw_if_minimum_throughput_is_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 0, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("minimumThroughput");
        }

        [Fact]
        public void Should_throw_if_minimum_throughput_is_less_than_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), -1, TimeSpan.FromSeconds(30));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("minimumThroughput");
        }

        [Fact]
        public void Should_throw_if_duration_of_break_is_less_than_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, -TimeSpan.FromSeconds(1));

            action.ShouldThrow<ArgumentOutOfRangeException>()
                .And.ParamName.Should()
                .Be("durationOfBreak");
        }

        [Fact]
        public void Should_be_able_to_handle_a_duration_of_break_of_zero()
        {
            Action action = () => Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.Zero);

            action.ShouldNotThrow();
        }

        [Fact]
        public void Should_initialise_to_closed_state()
        {
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));

            breaker.CircuitState.Should().Be(CircuitState.Closed);
        }

        #endregion

        #region Circuit-breaker threshold-to-break tests

        // Tests on the TimesliceCircuitBreaker operation typically use a breaker: 
        // - with a failure threshold of >=50%, 
        // - and a throughput threshold of 4
        // - across a ten-second period.
        // These provide easy values for testing for failure and throughput thresholds each being met and non-met, in combination.

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_if_failure_threshold_exceeded_and_throughput_threshold_equalled_within_timeslice()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>()
                .WithMessage("The circuit is now open and is not allowing calls.")
                .WithInnerException<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

        }

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_if_failure_threshold_exceeded_though_not_all_are_failures_and_throughput_threshold_equalled_within_timeslice()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Three of four actions in this test throw handled failures.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);
            // No adjustment to SystemClock.UtcNow, so all exceptions were raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>()
                .WithMessage("The circuit is now open and is not allowing calls.")
                .WithInnerException<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

        }

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_if_failure_threshold_equalled_and_throughput_threshold_equalled_within_timeslice()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Two of four actions in this test throw handled failures.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);
            // No adjustment to SystemClock.UtcNow, so all exceptions were raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>()
                .WithMessage("The circuit is now open and is not allowing calls.")
                .WithInnerException<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

        }

        [Fact]
        public void Should_not_open_circuit_if_failure_threshold_exceeded_but_throughput_threshold_not_met_before_timeslice_expires()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var timesliceDuration = TimeSpan.FromSeconds(10);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: timesliceDuration,
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Four of four actions in this test throw handled failures; but only the first three within the timeslice.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // Adjust SystemClock so that timeslice (clearly) expires; fourth exception thrown in next-recorded timeslice.
            SystemClock.UtcNow = () => time.Add(timesliceDuration).Add(timesliceDuration);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

        }

        [Fact]
        public void Should_not_open_circuit_if_failure_threshold_exceeded_but_throughput_threshold_not_met_before_timeslice_expires__even_if_timeslice_expires_only_exactly()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var timesliceDuration = TimeSpan.FromSeconds(10);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: timesliceDuration,
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Four of four actions in this test throw handled failures; but only the first three within the timeslice.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // Adjust SystemClock so that timeslice (just) expires; fourth exception thrown in following timeslice.
            SystemClock.UtcNow = () => time.Add(timesliceDuration);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

        }

        [Fact]
        public void Should_open_circuit_with_the_last_raised_exception_if_failure_threshold_equalled_and_throughput_threshold_equalled_even_if_only_just_within_timeslice()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var timesliceDuration = TimeSpan.FromSeconds(10);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: timesliceDuration,
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // Adjust SystemClock so that timeslice doesn't quite expire; fourth exception thrown in same timeslice.
            SystemClock.UtcNow = () => time.AddTicks(timesliceDuration.Ticks - 1);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>()
                .WithMessage("The circuit is now open and is not allowing calls.")
                .WithInnerException<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

        }

        [Fact]
        public void Should_not_open_circuit_if_failure_threshold_not_met_and_throughput_threshold_not_met()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // One of three actions in this test throw handled failures.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            // No adjustment to SystemClock.UtcNow, so all exceptions were raised within same timeslice
        }

        [Fact]
        public void Should_not_open_circuit_if_failure_threshold_not_met_but_throughput_threshold_met_before_timeslice_expires()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // One of four actions in this test throw handled failures.
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldNotThrow();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            // No adjustment to SystemClock.UtcNow, so all exceptions were raised within same timeslice
        }

        [Fact]
        public void Should_not_open_circuit_if_exceptions_raised_are_not_one_of_the_the_specified_exceptions()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .Or<ArgumentOutOfRangeException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<ArgumentNullException>())
                .ShouldThrow<ArgumentNullException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<ArgumentNullException>())
                .ShouldThrow<ArgumentNullException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<ArgumentNullException>())
                .ShouldThrow<ArgumentNullException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<ArgumentNullException>())
                .ShouldThrow<ArgumentNullException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
        }

        #endregion

        #region Circuit-breaker open->half-open->open/closed tests

        [Fact]
        public void Should_halfopen_circuit_after_the_specified_duration_has_passed()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_open_circuit_again_after_the_specified_duration_has_passed_if_the_next_call_raises_an_exception()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);

            // first call after duration raises an exception, so circuit should open again
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>();
            
        }

        [Fact]
        public void Should_reset_circuit_after_the_specified_duration_has_passed_if_the_next_call_does_not_raise_an_exception()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);

            // first call after duration is successful, so circuit should reset
            breaker.Execute(() => {});
            breaker.CircuitState.Should().Be(CircuitState.Closed);
        }

        #endregion

        #region Isolate and reset tests

        [Fact]
        public void Should_open_circuit_and_block_calls_if_manual_override_open()
        {
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(30));
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // manually break circuit
            breaker.Isolate();
            breaker.CircuitState.Should().Be(CircuitState.Isolated);

            // circuit manually broken: execution should be blocked; even non-exception-throwing executions should not reset circuit
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldThrow<IsolatedCircuitException>();
            breaker.CircuitState.Should().Be(CircuitState.Isolated);
        }

        [Fact]
        public void Should_hold_circuit_open_despite_elapsed_time_if_manual_override_open()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak);
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Isolate();
            breaker.CircuitState.Should().Be(CircuitState.Isolated);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);
            breaker.CircuitState.Should().Be(CircuitState.Isolated);
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldThrow<IsolatedCircuitException>();
        }

        [Fact]
        public void Should_close_circuit_again_on_reset_after_manual_override()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak);
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Isolate();
            breaker.CircuitState.Should().Be(CircuitState.Isolated);
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldThrow<IsolatedCircuitException>();

            breaker.Reset();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            breaker.Invoking(x => x.Execute(() => { })).ShouldNotThrow();
        }

        [Fact]
        public void Should_be_able_to_reset_automatically_opened_circuit_without_specified_duration_passing()
        {
            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            // reset circuit, with no time having passed
            breaker.Reset();
            SystemClock.UtcNow().Should().Be(time);
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            breaker.Invoking(x => x.Execute(() => { })).ShouldNotThrow();
        }

        #endregion

        #region State-change delegate tests

        [Fact]
        public void Should_not_call_onreset_on_initialise()
        {
            Action<Exception, TimeSpan> onBreak = (_, __) => { };
            bool onResetCalled = false;
            Action onReset = () => { onResetCalled = true; };

            var durationOfBreak = TimeSpan.FromSeconds(30);
            Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak, onBreak, onReset);

            onResetCalled.Should().BeFalse();
        }

        [Fact]
        public void Should_call_onbreak_when_breaking_circuit_automatically()
        {
            bool onBreakCalled = false;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled = true; };
            Action onReset = () => { };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            onBreakCalled.Should().BeTrue();
        }

        [Fact]
        public void Should_call_onbreak_when_breaking_circuit_manually()
        {
            bool onBreakCalled = false;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled = true; };
            Action onReset = () => { };

            var durationOfBreak = TimeSpan.FromSeconds(30);
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak, onBreak, onReset);

            onBreakCalled.Should().BeFalse();

            breaker.Isolate();

            onBreakCalled.Should().BeTrue();
        }

        [Fact]
        public void Should_call_onbreak_when_breaking_circuit_first_time_but_not_for_subsequent_calls_through_open_circuit()
        {
            int onBreakCalled = 0;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled++; };
            Action onReset = () => { };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            onBreakCalled.Should().Be(1);

            // call through circuit when already broken - should not retrigger onBreak 
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<BrokenCircuitException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);
            onBreakCalled.Should().Be(1);
        }

        [Fact]
        public void Should_call_onreset_when_automatically_closing_circuit_but_not_when_halfopen()
        {
            int onBreakCalled = 0;
            int onResetCalled = 0;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled++; };
            Action onReset = () => { onResetCalled++; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak,
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            onBreakCalled.Should().Be(1);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);
            // but not yet reset
            onResetCalled.Should().Be(0);

            // first call after duration is successful, so circuit should reset
            breaker.Execute(() => { });
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onResetCalled.Should().Be(1);
        }

        [Fact]
        public void Should_not_call_onreset_on_successive_successful_calls()
        {
            Action<Exception, TimeSpan> onBreak = (_, __) => { };
            bool onResetCalled = false;
            Action onReset = () => { onResetCalled = true; };

            var durationOfBreak = TimeSpan.FromSeconds(30);
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak, onBreak, onReset);

            onResetCalled.Should().BeFalse();

            breaker.Execute(() => { });
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onResetCalled.Should().BeFalse();

            breaker.Execute(() => { });
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onResetCalled.Should().BeFalse();
        }

        [Fact]
        public void Should_call_onhalfopen_when_automatically_transitioning_to_halfopen_due_to_subsequent_execution()
        {
            int onBreakCalled = 0;
            int onResetCalled = 0;
            int onHalfOpenCalled = 0;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled++; };
            Action onReset = () => { onResetCalled++; };
            Action onHalfOpen = () => { onHalfOpenCalled++; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak,
                    onBreak: onBreak,
                    onReset: onReset,
                    onHalfOpen: onHalfOpen
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            onBreakCalled.Should().Be(1);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);
            // duration has passed, circuit now half open
            onHalfOpenCalled.Should().Be(0); // not yet transitioned to half-open, because we have not queried state

            // first call after duration is successful, so circuit should reset
            breaker.Execute(() => { });
            onHalfOpenCalled.Should().Be(1); // called as action was placed for execution
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onResetCalled.Should().Be(1); // called after action succeeded
        }

        [Fact]
        public void Should_call_onhalfopen_when_automatically_transitioning_to_halfopen_due_to_state_read()
        {
            int onBreakCalled = 0;
            int onHalfOpenCalled = 0;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled++; };
            Action onReset = () => { };
            Action onHalfOpen = () => { onHalfOpenCalled++; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak,
                    onBreak: onBreak,
                    onReset: onReset,
                    onHalfOpen: onHalfOpen
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            onBreakCalled.Should().Be(0);

            // No adjustment to SystemClock.UtcNow, so all exceptions raised within same timeslice

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            onBreakCalled.Should().Be(1);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);
            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);
            onHalfOpenCalled.Should().Be(1);
        }

        [Fact]
        public void Should_call_onreset_when_manually_resetting_circuit()
        {
            int onBreakCalled = 0;
            int onResetCalled = 0;
            Action<Exception, TimeSpan> onBreak = (_, __) => { onBreakCalled++; };
            Action onReset = () => { onResetCalled++; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);
            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, durationOfBreak, onBreak, onReset);

            onBreakCalled.Should().Be(0);
            breaker.Isolate();
            onBreakCalled.Should().Be(1);

            breaker.CircuitState.Should().Be(CircuitState.Isolated);
            breaker.Invoking(x => x.Execute(() => { }))
                .ShouldThrow<IsolatedCircuitException>();

            onResetCalled.Should().Be(0);
            breaker.Reset();
            onResetCalled.Should().Be(1);

            breaker.CircuitState.Should().Be(CircuitState.Closed);
            breaker.Invoking(x => x.Execute(() => { })).ShouldNotThrow();
        }

        #region Tests that supplied context is passed to stage-change delegates

        [Fact]
        public void Should_call_onbreak_with_the_passed_context()
        {
            IDictionary<string, object> contextData = null;

            Action<Exception, TimeSpan, Context> onBreak = (_, __, context) => { contextData = context; };
            Action<Context> onReset = _ => { };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>(
                new { key1 = "value1", key2 = "value2" }.AsDictionary()
                )).ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);

            contextData.Should()
                .ContainKeys("key1", "key2").And
                .ContainValues("value1", "value2");
        }

        [Fact]
        public void Should_call_onreset_with_the_passed_context()
        {
            IDictionary<string, object> contextData = null;

            Action<Exception, TimeSpan, Context> onBreak = (_, __, ___) => { };
            Action<Context> onReset = context => { contextData = context; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak,
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);

            SystemClock.UtcNow = () => time.Add(durationOfBreak);
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);


            // first call after duration should invoke onReset, with context
            breaker.Execute(() => { }, new { key1 = "value1", key2 = "value2" }.AsDictionary());
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            contextData.Should()
                .ContainKeys("key1", "key2").And
                .ContainValues("value1", "value2");
        }

        [Fact]
        public void Context_should_be_empty_if_execute_not_called_with_any_context_data()
        {
            IDictionary<string, object> contextData = new { key1 = "value1", key2 = "value2" }.AsDictionary();

            Action<Exception, TimeSpan, Context> onBreak = (_, __, context) => { contextData = context; };
            Action<Context> onReset = _ => { };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();

            breaker.CircuitState.Should().Be(CircuitState.Open);

            contextData.Should().BeEmpty();
        }

        [Fact]
        public void Should_create_new_context_for_each_call_to_execute()
        {
            string contextValue = null;

            Action<Exception, TimeSpan, Context> onBreak =
                (_, __, context) => { contextValue = context.ContainsKey("key") ? context["key"].ToString() : null; };
            Action<Context> onReset =
                context => { contextValue = context.ContainsKey("key") ? context["key"].ToString() : null; };

            var time = 1.January(2000);
            SystemClock.UtcNow = () => time;

            var durationOfBreak = TimeSpan.FromSeconds(30);

            CircuitBreakerPolicy breaker = Policy
                .Handle<DivideByZeroException>()
                .TimesliceCircuitBreaker(
                    failureThreshold: 0.5,
                    timesliceDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: 4,
                    durationOfBreak: durationOfBreak,
                    onBreak: onBreak,
                    onReset: onReset
                );

            // Four of four actions in this test throw handled failures.
            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>())
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Closed);

            breaker.Invoking(x => x.RaiseException<DivideByZeroException>(new { key = "original_value" }.AsDictionary()))
                .ShouldThrow<DivideByZeroException>();
            breaker.CircuitState.Should().Be(CircuitState.Open);
            contextValue.Should().Be("original_value");

            SystemClock.UtcNow = () => time.Add(durationOfBreak);

            // duration has passed, circuit now half open
            breaker.CircuitState.Should().Be(CircuitState.HalfOpen);
            // but not yet reset

            // first call after duration is successful, so circuit should reset
            breaker.Execute(() => { }, new { key = "new_value" }.AsDictionary());
            breaker.CircuitState.Should().Be(CircuitState.Closed);
            contextValue.Should().Be("new_value");
        }

        #endregion

        #endregion

        public void Dispose()
        {
            SystemClock.Reset();
        }
    }
}