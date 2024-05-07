using System;

namespace CaddyVpsToolkit.Events
{
    /// <summary>
    /// Provides extension methods for <see cref="DomainEvent"/> that offer convenient operations
    /// for working with domain events.
    /// </summary>
    public static class DomainEventExtensions
    {
        /// <summary>
        /// Gets the elapsed time since the event occurred.
        /// </summary>
        /// <param name="domainEvent">The event instance.</param>
        /// <returns>A <see cref="TimeSpan"/> representing how long ago the event happened.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/> is <c>null</c>.</exception>
        public static TimeSpan GetAge(this DomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            return DateTime.UtcNow - domainEvent.OccurredAt;
        }

        /// <summary>
        /// Returns a detailed, single-line string representation of the event.
        /// </summary>
        /// <param name="domainEvent">The event instance.</param>
        /// <returns>A formatted string containing the key properties.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/> is <c>null</c>.</exception>
        public static string ToDetailedString(this DomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            return $"EventId: {domainEvent.EventId}, AggregateId: {domainEvent.AggregateId}, OccurredAt: {domainEvent.OccurredAt:O}";
        }

        /// <summary>
        /// Determines whether the event occurred within the specified maximum age.
        /// </summary>
        /// <param name="domainEvent">The event instance.</param>
        /// <param name="maxAge">The maximum allowed age.</param>
        /// <returns><see langword="true"/> if the event is newer than <paramref name="maxAge"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="domainEvent"/> is <c>null</c>.</exception>
        public static bool IsRecent(this DomainEvent domainEvent, TimeSpan maxAge)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            return domainEvent.GetAge() <= maxAge;
        }

        /// <summary>
        /// Checks whether two events belong to the same aggregate.
        /// </summary>
        /// <param name="domainEvent">The first event.</param>
        /// <param name="other">The second event to compare with.</param>
        /// <returns><see langword="true"/> if both events have the same <c>AggregateId</c>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Either <paramref name="domainEvent"/> or <paramref name="other"/> is <c>null</c>.</exception>
        public static bool HasSameAggregate(this DomainEvent domainEvent, DomainEvent other)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            ArgumentNullException.ThrowIfNull(other);
            return string.Equals(domainEvent.AggregateId, other.AggregateId, StringComparison.Ordinal);
        }
    }
}
