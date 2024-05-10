using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Notifications
{
	/// <summary>
	/// Extension methods that add convenient functionality to <see cref="Notification"/>.
	/// </summary>
	public static class NotificationExtensions
	{
		/// <summary>
		/// Adds or updates a metadata entry and returns the notification for fluent chaining.
		/// </summary>
		/// <param name="notification">The notification instance to modify.</param>
		/// <param name="key">The metadata key to add or update.</param>
		/// <param name="value">The metadata value to set.</param>
		/// <returns>The modified notification for fluent chaining.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or empty.</exception>
		public static Notification AddMetadata(this Notification notification, string key, string value)
		{
			ArgumentNullException.ThrowIfNull(notification);
			ArgumentException.ThrowIfNullOrEmpty(key);

			// Ensure the dictionary is instantiated.
			notification.Metadata ??= new Dictionary<string, string>();

			notification.Metadata[key] = value ?? string.Empty;
			return notification;
		}

		/// <summary>
		/// Removes a metadata entry if it exists and returns the notification for fluent chaining.
		/// </summary>
		/// <param name="notification">The notification instance to modify.</param>
		/// <param name="key">The metadata key to remove.</param>
		/// <returns>The modified notification for fluent chaining.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or empty.</exception>
		public static Notification RemoveMetadata(this Notification notification, string key)
		{
			ArgumentNullException.ThrowIfNull(notification);
			ArgumentException.ThrowIfNullOrEmpty(key);

			notification.Metadata?.Remove(key);
			return notification;
		}

		/// <summary>
		/// Retrieves a metadata value by key. Returns <c>null</c> if the key does not exist.
		/// </summary>
		/// <param name="notification">The notification instance to query.</param>
		/// <param name="key">The metadata key to retrieve.</param>
		/// <returns>The metadata value if found; otherwise, <see langword="null"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or empty.</exception>
		public static string? GetMetadataValue(this Notification notification, string key)
		{
			ArgumentNullException.ThrowIfNull(notification);
			ArgumentException.ThrowIfNullOrEmpty(key);

			return notification.Metadata?.TryGetValue(key, out var value) == true
				? value
				: null;
		}

		/// <summary>
		/// Returns a concise, human‑readable summary of the notification.
		/// </summary>
		/// <param name="notification">The notification to summarize.</param>
		/// <returns>A formatted summary string.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="notification"/> is <see langword="null"/>.</exception>
		public static string ToSummaryString(this Notification notification)
		{
			ArgumentNullException.ThrowIfNull(notification);

			return $"[Id:{notification.Id}] \"{notification.Title ?? string.Empty}\" " +
				$"(Priority:{notification.Priority}, Created:{notification.CreatedAt:u})" ;
		}
	}
}
