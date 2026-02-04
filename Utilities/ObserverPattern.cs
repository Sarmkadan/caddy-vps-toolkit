#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Observer pattern implementation for loose coupling.
    /// Observable notifies registered observers of changes.
    /// </summary>
    public interface IObserver<T>
    {
        void Update(T subject);
    }

    public interface IObservable<T>
    {
        void Attach(IObserver<T> observer);
        void Detach(IObserver<T> observer);
        void NotifyObservers();
    }

    /// <summary>
    /// Generic observable implementation
    /// </summary>
    public sealed class Observable<T> : IObservable<T> where T : class
    {
        private readonly List<IObserver<T>> _observers = new();
        private readonly object _lockObject = new();
        protected T _state;

        public Observable(T initialState)
        {
            _state = initialState;
        }

        public void Attach(IObserver<T> observer)
        {
            if (observer is null)
                throw new ArgumentNullException(nameof(observer));

            lock (_lockObject)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
            }
        }

        public void Detach(IObserver<T> observer)
        {
            if (observer is null)
                return;

            lock (_lockObject)
            {
                _observers.Remove(observer);
            }
        }

        public void NotifyObservers()
        {
            List<IObserver<T>> observersCopy;
            lock (_lockObject)
            {
                observersCopy = new List<IObserver<T>>(_observers);
            }

            foreach (var observer in observersCopy)
            {
                observer.Update(_state);
            }
        }

        public T GetState()
        {
            lock (_lockObject)
            {
                return _state;
            }
        }

        public void SetState(T state)
        {
            lock (_lockObject)
            {
                _state = state;
            }
            NotifyObservers();
        }

        public int GetObserverCount()
        {
            lock (_lockObject)
            {
                return _observers.Count;
            }
        }
    }

    /// <summary>
    /// Simple subscription-based observer alternative to interfaces
    /// </summary>
    public sealed class Subject<T>
    {
        private readonly List<Action<T>> _subscribers = new();
        private readonly object _lockObject = new();

        public void Subscribe(Action<T> handler)
        {
            if (handler is null)
                return;

            lock (_lockObject)
            {
                _subscribers.Add(handler);
            }
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (handler is null)
                return;

            lock (_lockObject)
            {
                _subscribers.Remove(handler);
            }
        }

        public void Publish(T value)
        {
            List<Action<T>> subscribersCopy;
            lock (_lockObject)
            {
                subscribersCopy = new List<Action<T>>(_subscribers);
            }

            foreach (var subscriber in subscribersCopy)
            {
                try
                {
                    subscriber?.Invoke(value);
                }
                catch
                {
                    // Silently ignore subscriber exceptions to prevent cascade failures
                }
            }
        }

        public int GetSubscriberCount()
        {
            lock (_lockObject)
            {
                return _subscribers.Count;
            }
        }
    }
}
