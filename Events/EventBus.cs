// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Event base class for all domain events
    /// </summary>
    public abstract class DomainEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public string AggregateId { get; set; }
    }

    /// <summary>
    /// Interface for event handlers
    /// </summary>
    public interface IEventHandler<in TEvent> where TEvent : DomainEvent
    {
        Task HandleAsync(TEvent @event);
    }

    /// <summary>
    /// Event bus for publishing and subscribing to domain events.
    /// Supports async event handlers and maintains handler registrations.
    /// This is a simple in-memory implementation suitable for single-server deployments.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
        void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    }

    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new();
        private readonly object _lockObject = new();

        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<object>();

                _handlers[eventType].Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent
        {
            if (handler == null)
                return;

            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                if (_handlers.TryGetValue(eventType, out var handlers))
                    handlers.Remove(handler);
            }
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            List<IEventHandler<TEvent>> handlers;
            lock (_lockObject)
            {
                var eventType = typeof(TEvent);
                if (!_handlers.TryGetValue(eventType, out var rawHandlers))
                    return; // No handlers subscribed

                handlers = rawHandlers.Cast<IEventHandler<TEvent>>().ToList();
            }

            // Execute handlers in parallel for better performance
            var tasks = handlers.Select(h => h.HandleAsync(@event)).ToList();
            await Task.WhenAll(tasks);
        }

        public int GetSubscriberCount<TEvent>() where TEvent : DomainEvent
        {
            lock (_lockObject)
            {
                return _handlers.TryGetValue(typeof(TEvent), out var handlers)
                    ? handlers.Count
                    : 0;
            }
        }
    }
}
