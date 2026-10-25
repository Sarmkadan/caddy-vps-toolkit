#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.Tasks;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Event bus for publishing and subscribing to domain events.
    /// Supports async event handlers and maintains handler registrations.
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
        void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent;
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent;
    }
}
