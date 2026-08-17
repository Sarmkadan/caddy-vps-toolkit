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
            // Copy the list to avoid mutation‑during‑enumeration issues
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

        /// <summary>
        /// Attach an observer and receive an <see cref="IDisposable"/> token.
        /// Disposing the token automatically detaches the observer, preventing leaks.
        /// </summary>
        public IDisposable AttachWithToken(IObserver<T> observer)
        {
            Attach(observer);
            return new ObserverToken(this, observer);
        }

        private sealed class ObserverToken : IDisposable
        {
            private readonly Observable<T> _observable;
            private IObserver<T>? _observer;

            public ObserverToken(Observable<T> observable, IObserver<T> observer)
            {
                _observable = observable;
                _observer = observer;
            }

            public void Dispose()
            {
                var obs = Interlocked.Exchange(ref _observer, null);
                if (obs != null)
                {
                    _observable.Detach(obs);
                }
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
            // Copy the list to avoid mutation‑during‑enumeration issues
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

        /// <summary>
        /// Subscribe with a disposable token. Disposing the token automatically unsubscribes.
        /// </summary>
        public IDisposable SubscribeWithToken(Action<T> handler)
        {
            Subscribe(handler);
            return new SubscriptionToken(this, handler);
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private readonly Subject<T> _subject;
            private Action<T>? _handler;

            public SubscriptionToken(Subject<T> subject, Action<T> handler)
            {
                _subject = subject;
                _handler = handler;
            }

            public void Dispose()
            {
                var h = Interlocked.Exchange(ref _handler, null);
                if (h != null)
                {
                    _subject.Unsubscribe(h);
                }
            }
        }
    }
}
