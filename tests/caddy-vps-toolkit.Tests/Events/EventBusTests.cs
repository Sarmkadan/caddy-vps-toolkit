#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CaddyVpsToolkit.Events;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CaddyVpsToolkit.Tests.Events
{
    /// <summary>
    /// Tests for the EventBus class.
    /// </summary>
    public sealed class EventBusTests
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventBusTests"/> class.
        /// </summary>
        private readonly EventBus _bus = new();

        // ── Subscribe / Publish ──────────────────────────────────────────────

        [Fact]
        public async Task PublishAsync_WithSubscriber_InvokesHandler()
        {
            /// <summary>
            /// Verifies that the EventBus invokes the handler when publishing an event with a subscriber.
            /// </summary>
            var handler = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            _bus.Subscribe(handler);
            var evt = new ServiceCreatedEvent { ServiceName = "api", Port = 8080 };

            await _bus.PublishAsync(evt);

            await handler.Received(1).HandleAsync(evt);
        }

        [Fact]
        public async Task PublishAsync_NoSubscribers_DoesNotThrow()
        {
            /// <summary>
            /// Verifies that the EventBus does not throw when publishing an event with no subscribers.
            /// </summary>
            var evt = new ServiceDeletedEvent { ServiceName = "orphan" };

            Func<Task> act = () => _bus.PublishAsync(evt);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
        {
            /// <summary>
            /// Verifies that the EventBus throws an ArgumentNullException when publishing a null event.
            /// </summary>
            Func<Task> act = () => _bus.PublishAsync<ServiceCreatedEvent>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task PublishAsync_MultipleSubscribers_AllHandlersInvoked()
        {
            /// <summary>
            /// Verifies that the EventBus invokes all handlers when publishing an event with multiple subscribers.
            /// </summary>
            var h1 = Substitute.For<IEventHandler<ServiceStatusChangedEvent>>();
            var h2 = Substitute.For<IEventHandler<ServiceStatusChangedEvent>>();
            _bus.Subscribe(h1);
            _bus.Subscribe(h2);
            var evt = new ServiceStatusChangedEvent { ServiceName = "svc" };

            await _bus.PublishAsync(evt);

            await h1.Received(1).HandleAsync(evt);
            await h2.Received(1).HandleAsync(evt);
        }

        // ── Unsubscribe ──────────────────────────────────────────────────────

        [Fact]
        public async Task Unsubscribe_RemovesHandler_NotInvokedOnNextPublish()
        {
            /// <summary>
            /// Verifies that the EventBus removes the handler when unsubscribing and does not invoke it on the next publish.
            /// </summary>
            var handler = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            _bus.Subscribe(handler);
            _bus.Unsubscribe(handler);

            await _bus.PublishAsync(new ServiceCreatedEvent { ServiceName = "svc", Port = 80 });

            await handler.DidNotReceive().HandleAsync(Arg.Any<ServiceCreatedEvent>());
        }

        [Fact]
        public void Unsubscribe_NullHandler_DoesNotThrow()
        {
            /// <summary>
            /// Verifies that the EventBus does not throw when unsubscribing a null handler.
            /// </summary>
            Action act = () => _bus.Unsubscribe<ServiceCreatedEvent>(null!);

            act.Should().NotThrow();
        }

        // ── Subscribe ────────────────────────────────────────────────────────

        [Fact]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            /// <summary>
            /// Verifies that the EventBus throws an ArgumentNullException when subscribing a null handler.
            /// </summary>
            Action act = () => _bus.Subscribe<ServiceCreatedEvent>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        // ── GetSubscriberCount ───────────────────────────────────────────────

        [Fact]
        public void GetSubscriberCount_NoSubscribers_ReturnsZero()
        {
            /// <summary>
            /// Verifies that the EventBus returns 0 when there are no subscribers.
            /// </summary>
            _bus.GetSubscriberCount<ServiceCreatedEvent>().Should().Be(0);
        }

        [Fact]
        public void GetSubscriberCount_AfterSubscription_ReturnsCorrectCount()
        {
            /// <summary>
            /// Verifies that the EventBus returns the correct count of subscribers after subscribing.
            /// </summary>
            var h1 = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            var h2 = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            _bus.Subscribe(h1);
            _bus.Subscribe(h2);

            _bus.GetSubscriberCount<ServiceCreatedEvent>().Should().Be(2);
        }

        [Fact]
        public void GetSubscriberCount_AfterUnsubscribe_DecreasesCount()
        {
            /// <summary>
            /// Verifies that the EventBus decreases the count of subscribers after unsubscribing.
            /// </summary>
            var handler = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            _bus.Subscribe(handler);
            _bus.Unsubscribe(handler);

            _bus.GetSubscriberCount<ServiceCreatedEvent>().Should().Be(0);
        }

        // ── Event isolation ──────────────────────────────────────────────────

        [Fact]
        public async Task PublishAsync_DifferentEventTypes_OnlyCorrectHandlerInvoked()
        {
            /// <summary>
            /// Verifies that the EventBus invokes only the correct handler when publishing different event types.
            /// </summary>
            var createHandler = Substitute.For<IEventHandler<ServiceCreatedEvent>>();
            var deleteHandler = Substitute.For<IEventHandler<ServiceDeletedEvent>>();
            _bus.Subscribe(createHandler);
            _bus.Subscribe(deleteHandler);

            await _bus.PublishAsync(new ServiceCreatedEvent { ServiceName = "s", Port = 80 });

            await createHandler.Received(1).HandleAsync(Arg.Any<ServiceCreatedEvent>());
            await deleteHandler.DidNotReceive().HandleAsync(Arg.Any<ServiceDeletedEvent>());
        }

        // ── Concurrency ──────────────────────────────────────────────────────

        [Fact]
        public async Task PublishAsync_ConcurrentPublishes_AllHandlersInvoked()
        {
            /// <summary>
            /// Verifies that the EventBus invokes all handlers when publishing concurrently.
            /// </summary>
            int invokeCount = 0;
            var handler = Substitute.For<IEventHandler<ServiceHealthCheckFailedEvent>>();
            handler
                .When(h => h.HandleAsync(Arg.Any<ServiceHealthCheckFailedEvent>()))
                .Do(_ => Interlocked.Increment(ref invokeCount));
            _bus.Subscribe(handler);

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_bus.PublishAsync(new ServiceHealthCheckFailedEvent
                {
                    ServiceName = "svc",
                    ErrorMessage = "error",
                    ConsecutiveFailures = i
                }));
            }

            await Task.WhenAll(tasks);

            invokeCount.Should().Be(10);
        }
    }
}
