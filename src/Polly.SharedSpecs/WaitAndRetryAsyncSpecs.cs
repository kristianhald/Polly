﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Polly.Specs.Helpers;
using Polly.Utilities;
using Xunit;

using Scenario = Polly.Specs.Helpers.PolicyExtensions.ExceptionAndOrCancellationScenario;

namespace Polly.Specs
{
    public class WaitAndRetryAsyncSpecs : IDisposable
    {
        public WaitAndRetryAsyncSpecs()
        {
            // do nothing on call to sleep
            SystemClock.SleepAsync = (_, __) => Task.FromResult(0);
        }

        [Fact]
        public void Should_throw_when_sleep_durations_is_null_without_context()
        {
            Action<Exception, TimeSpan> onRetry = (_, __) => { };

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .WaitAndRetryAsync(null, onRetry);

            policy.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("sleepDurations");
        }

        [Fact]
        public void Should_throw_when_onretry_action_is_null_without_context()
        {
            Action<Exception, TimeSpan> nullOnRetry = null;

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>(), nullOnRetry);

            policy.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("onRetry");
        }
        
        [Fact]
        public void Should_not_throw_when_specified_exception_thrown_same_number_of_times_as_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>(3))
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_not_throw_when_one_of_the_specified_exceptions_thrown_same_number_of_times_as_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .Or<ArgumentException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>(3))
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_not_throw_when_specified_exception_thrown_less_number_of_times_than_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>(2))
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_not_throw_when_one_of_the_specified_exceptions_thrown_less_number_of_times_than_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .Or<ArgumentException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>(2))
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_throw_when_specified_exception_thrown_more_times_than_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>(3 + 1))
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_throw_when_one_of_the_specified_exceptions_are_thrown_more_times_than_there_are_sleep_durations()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .Or<ArgumentException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>(3 + 1))
                  .ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Should_throw_when_exception_thrown_is_not_the_specified_exception_type()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>());

            policy.Awaiting(x => x.RaiseExceptionAsync<NullReferenceException>())
                  .ShouldThrow<NullReferenceException>();
        }

        [Fact]
        public void Should_throw_when_exception_thrown_is_not_one_of_the_specified_exception_types()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .Or<ArgumentException>()
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>());

            policy.Awaiting(x => x.RaiseExceptionAsync<NullReferenceException>())
                  .ShouldThrow<NullReferenceException>();
        }

        [Fact]
        public void Should_throw_when_specified_exception_predicate_is_not_satisfied()
        {
            var policy = Policy
                .Handle<DivideByZeroException>(e => false)
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>());

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();
        }

        [Fact]
        public void Should_throw_when_none_of_the_specified_exception_predicates_are_satisfied()
        {
            var policy = Policy
                .Handle<DivideByZeroException>(e => false)
                .Or<ArgumentException>(e => false)
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>());

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>())
                  .ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Should_not_throw_when_specified_exception_predicate_is_satisfied()
        {
            var policy = Policy
                .Handle<DivideByZeroException>(e => true)
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_not_throw_when_one_of_the_specified_exception_predicates_are_satisfied()
        {
            var policy = Policy
                .Handle<DivideByZeroException>(e => true)
                .Or<ArgumentException>(e => true)
               .WaitAndRetryAsync(new[]
                {
                   1.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>())
                  .ShouldNotThrow();
        }

        [Fact]
        public async Task Should_sleep_for_the_specified_duration_each_retry_when_specified_exception_thrown_same_number_of_times_as_there_are_sleep_durations()
        {
            var totalTimeSlept = 0;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            SystemClock.SleepAsync = (span, ct) =>
            {
                totalTimeSlept += span.Seconds;
                return Task.FromResult(0);
            };

            await policy.RaiseExceptionAsync<DivideByZeroException>(3);

            totalTimeSlept.Should()
                          .Be(1 + 2 + 3);
        }

        [Fact]
        public void Should_sleep_for_the_specified_duration_each_retry_when_specified_exception_thrown_more_number_of_times_than_there_are_sleep_durations()
        {
            var totalTimeSlept = 0;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            SystemClock.SleepAsync = (span, ct) =>
            {
                totalTimeSlept += span.Seconds;
                return Task.FromResult(0);
            };

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>(3 + 1))
                  .ShouldThrow<DivideByZeroException>();

            totalTimeSlept.Should()
                          .Be(1 + 2 + 3);
        }

        [Fact]
        public void Should_sleep_for_the_specified_duration_each_retry_when_specified_exception_thrown_less_number_of_times_than_there_are_sleep_durations()
        {
            var totalTimeSlept = 0;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                });

            SystemClock.SleepAsync = (span, ct) =>
            {
                totalTimeSlept += span.Seconds;
                return Task.FromResult(0);
            };

            policy.RaiseExceptionAsync<DivideByZeroException>(2);

            totalTimeSlept.Should()
                          .Be(1 + 2);
        }

        [Fact]
        public void Should_not_sleep_if_no_retries()
        {
            var totalTimeSlept = 0;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>());

            SystemClock.SleepAsync = (span, ct) =>
            {
                totalTimeSlept += span.Seconds;
                return Task.FromResult(0);
            };

            policy.Awaiting(x => x.RaiseExceptionAsync<NullReferenceException>())
                  .ShouldThrow<NullReferenceException>();

            totalTimeSlept.Should()
                          .Be(0);
        }

        [Fact]
        public void Should_call_onretry_on_each_retry_with_the_current_timespan()
        {
            var expectedRetryCounts = new []
                {
                    1.Seconds(), 
                    2.Seconds(), 
                    3.Seconds()
                };

            var retryTimeSpans = new List<TimeSpan>();

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                }, (_, timeSpan) => retryTimeSpans.Add(timeSpan));

            policy.RaiseExceptionAsync<DivideByZeroException>(3);

            retryTimeSpans.Should()
                       .ContainInOrder(expectedRetryCounts);
        }

        [Fact]
        public void Should_call_onretry_on_each_retry_with_the_current_exception()
        {
            var expectedExceptions = new object[] { "Exception #1", "Exception #2", "Exception #3" };
            var retryExceptions = new List<Exception>();

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                   1.Seconds(),
                   2.Seconds(),
                   3.Seconds()
                }, (exception, _) => retryExceptions.Add(exception));

            policy.RaiseExceptionAsync<DivideByZeroException>(3, (e, i) => e.HelpLink = "Exception #" + i);

            retryExceptions
                .Select(x => x.HelpLink)
                .Should()
                .ContainInOrder(expectedExceptions);
        }

        [Fact]
        public void Should_not_call_onretry_when_no_retries_are_performed()
        {
            var retryExceptions = new List<Exception>();

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(Enumerable.Empty<TimeSpan>(), (exception, _) => retryExceptions.Add(exception));

            policy.Awaiting(x => x.RaiseExceptionAsync<ArgumentException>())
                  .ShouldThrow<ArgumentException>();

            retryExceptions.Should().BeEmpty();
        }

        [Fact]
        public void Should_create_new_state_for_each_call_to_policy()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                    1.Seconds()
                });

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldNotThrow();

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldNotThrow();
        }

        [Fact]
        public void Should_call_onretry_with_the_passed_context()
        {
            IDictionary<string, object> contextData = null;

            ContextualPolicy policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                    1.Seconds(),
                    2.Seconds(),
                    3.Seconds()
                }, (_, __, context) => contextData = context);

            policy.RaiseExceptionAsync<DivideByZeroException>(
                new { key1 = "value1", key2 = "value2" }.AsDictionary()
                );

            contextData.Should()
                .ContainKeys("key1", "key2").And
                .ContainValues("value1", "value2");
        }

        [Fact]
        public void Context_should_be_empty_if_execute_not_called_with_any_data()
        {
            Context capturedContext = null;

            ContextualPolicy policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                    1.Seconds(),
                    2.Seconds(),
                    3.Seconds()
                }, (_, __, context) => capturedContext = context);
            policy.RaiseExceptionAsync<DivideByZeroException>();

            capturedContext.Should()
                           .BeEmpty();
        }

        [Fact]
        public void Should_create_new_context_for_each_call_to_execute()
        {
            string contextValue = null;

            ContextualPolicy policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[]
                {
                    1.Seconds()
                },
                (_, __, context) => contextValue = context["key"].ToString());

            policy.RaiseExceptionAsync<DivideByZeroException>(
                new { key = "original_value" }.AsDictionary()
            );

            contextValue.Should().Be("original_value");

            policy.RaiseExceptionAsync<DivideByZeroException>(
                new { key = "new_value" }.AsDictionary()
            );

            contextValue.Should().Be("new_value");
        }

        [Fact]
        public void Should_throw_when_retry_count_is_less_than_zero_without_context()
        {
            Action<Exception, TimeSpan> onRetry = (_, __) => { };

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .WaitAndRetryAsync(-1, _ => new TimeSpan(), onRetry);
                                           
            policy.ShouldThrow<ArgumentOutOfRangeException>().And                  
                  .ParamName.Should().Be("retryCount");
        }

        [Fact]
        public void Should_throw_when_sleep_duration_provider_is_null_without_context()
        {
            Action<Exception, TimeSpan> onRetry = (_, __) => { };

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .WaitAndRetryAsync(1, null, onRetry);

            policy.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("sleepDurationProvider");
        }

        [Fact]
        public void Should_throw_when_onretry_action_is_null_without_context_when_using_provider_overload()
        {
            Action<Exception, TimeSpan> nullOnRetry = null;

            Action policy = () => Policy
                                      .Handle<DivideByZeroException>()
                                      .WaitAndRetryAsync(1, _ => new TimeSpan(), nullOnRetry);

            policy.ShouldThrow<ArgumentNullException>().And
                  .ParamName.Should().Be("onRetry");
        }

        [Fact]
        public void Should_calculate_retry_timespans_from_current_retry_attempt_and_timespan_provider()
        {
            var expectedRetryCounts = new[]
                {
                    2.Seconds(), 
                    4.Seconds(), 
                    8.Seconds(), 
                    16.Seconds(), 
                    32.Seconds() 
                };

            var retryTimeSpans = new List<TimeSpan>();

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(5, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                    (_, timeSpan) => retryTimeSpans.Add(timeSpan)
                );

            policy.RaiseExceptionAsync<DivideByZeroException>(5);

            retryTimeSpans.Should()
                       .ContainInOrder(expectedRetryCounts);
        }

        [Fact]
        public void Should_not_call_onretry_when_retry_count_is_zero()
        {
            bool retryInvoked = false;

            Action<Exception, TimeSpan> onRetry = (_, __) => { retryInvoked = true; };

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(0, retryAttempt => TimeSpan.FromSeconds(1), onRetry);

            policy.Awaiting(x => x.RaiseExceptionAsync<DivideByZeroException>())
                  .ShouldThrow<DivideByZeroException>();

            retryInvoked.Should().BeFalse();
        }

        [Fact]
        public void Should_execute_action_when_non_faulting_and_cancellationtoken_not_cancelled()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 0,
                AttemptDuringWhichToCancel = null,
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldNotThrow();

            attemptsInvoked.Should().Be(1);
        }

        [Fact]
        public void Should_execute_all_tries_when_faulting_and_cancellationtoken_not_cancelled()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = null,
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<DivideByZeroException>();

            attemptsInvoked.Should().Be(1 + 3);
        }

        [Fact]
        public void Should_not_execute_action_when_cancellationtoken_cancelled_before_execute()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = null, // Cancellation token cancelled manually below - before any scenario execution.
            };

            cancellationTokenSource.Cancel();

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(0);
        }

        [Fact]
        public void Should_report_cancellation_during_otherwise_non_faulting_action_execution_and_cancel_further_retries_when_user_delegate_observes_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 0,
                AttemptDuringWhichToCancel = 1,
                ActionObservesCancellation = true
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(1);
        }

        [Fact]
        public void Should_report_cancellation_during_faulting_initial_action_execution_and_cancel_further_retries_when_user_delegate_observes_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = 1,
                ActionObservesCancellation = true
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(1); 
        }

        [Fact]
        public void Should_report_cancellation_during_faulting_initial_action_execution_and_cancel_further_retries_when_user_delegate_does_not_observe_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = 1,
                ActionObservesCancellation = false
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(1);
        }

        [Fact]
        public void Should_report_cancellation_during_faulting_retried_action_execution_and_cancel_further_retries_when_user_delegate_observes_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = 2,
                ActionObservesCancellation = true
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(2);
        }

        [Fact]
        public void Should_report_cancellation_during_faulting_retried_action_execution_and_cancel_further_retries_when_user_delegate_does_not_observe_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = 2,
                ActionObservesCancellation = false
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(2);
        }

        [Fact]
        public void Should_report_cancellation_during_faulting_last_retry_execution_when_user_delegate_does_not_observe_cancellationtoken()
        {
            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = 1 + 3,
                ActionObservesCancellation = false
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(1 + 3);
        }

        [Fact]
        public void Should_honour_cancellation_immediately_during_wait_phase_of_waitandretry()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            SystemClock.SleepAsync = Task.Delay;

            TimeSpan shimTimeSpan = TimeSpan.FromSeconds(0.2); // Consider increasing shimTimeSpan if test fails transiently in different environments.
            TimeSpan retryDelay = shimTimeSpan + shimTimeSpan + shimTimeSpan;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { retryDelay });

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 1,
                AttemptDuringWhichToCancel = null, // Cancellation invoked after delay - see below.
                ActionObservesCancellation = false
            };

            cancellationTokenSource.CancelAfter(shimTimeSpan);

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                    .ShouldThrow<TaskCanceledException>()
                    .And.CancellationToken.Should().Be(cancellationToken);
            watch.Stop();

            attemptsInvoked.Should().Be(1);

            watch.Elapsed.Should().BeLessThan(retryDelay);
            watch.Elapsed.Should().BeCloseTo(shimTimeSpan, precision: (int)(shimTimeSpan.TotalMilliseconds) / 4);  // Consider increasing shimTimeSpan, or loosening precision, if test fails transiently in different environments.
        }

        [Fact]
        public void Should_report_cancellation_after_faulting_action_execution_and_cancel_further_retries_if_onRetry_invokes_cancellation()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var policy = Policy
                .Handle<DivideByZeroException>()
                .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() },
                (_, __) =>
                {
                    cancellationTokenSource.Cancel();
                });

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 1 + 3,
                AttemptDuringWhichToCancel = null, // Cancellation during onRetry instead - see above.
                ActionObservesCancellation = false
            };

            policy.Awaiting(x => x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException>(scenario, cancellationTokenSource, onExecute))
                .ShouldThrow<TaskCanceledException>()
                .And.CancellationToken.Should().Be(cancellationToken);

            attemptsInvoked.Should().Be(1);
        }

        [Fact]
        public void Should_execute_func_returning_value_when_cancellationtoken_not_cancelled()
        {
            var policy = Policy
               .Handle<DivideByZeroException>()
               .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            bool? result = null;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 0,
                AttemptDuringWhichToCancel = null
            };

            policy.Awaiting(async x => result = await x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException, bool>(scenario, cancellationTokenSource, onExecute, true).ConfigureAwait(false))
                .ShouldNotThrow();

            result.Should().BeTrue();

            attemptsInvoked.Should().Be(1);
        }

        [Fact]
        public void Should_honour_and_report_cancellation_during_func_execution()
        {
            var policy = Policy
               .Handle<DivideByZeroException>()
               .WaitAndRetryAsync(new[] { 1.Seconds(), 2.Seconds(), 3.Seconds() });

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            int attemptsInvoked = 0;
            Action onExecute = () => attemptsInvoked++;

            bool? result = null;

            Scenario scenario = new Scenario
            {
                NumberOfTimesToRaiseException = 0,
                AttemptDuringWhichToCancel = 1,
                ActionObservesCancellation = true
            };

            policy.Awaiting(async x => result = await x.RaiseExceptionAndOrCancellationAsync<DivideByZeroException, bool>(scenario, cancellationTokenSource, onExecute, true).ConfigureAwait(false))
                .ShouldThrow<TaskCanceledException>().And.CancellationToken.Should().Be(cancellationToken);

            result.Should().Be(null);

            attemptsInvoked.Should().Be(1);
        }
        
        public void Dispose()
        {
            SystemClock.Reset();
        }
    }
}