#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace CaddyVpsToolkit.Utilities
{
    /// <summary>
    /// Represents a simple state machine with transitions, guard clauses, and entry/exit callbacks.
    /// </summary>
    /// <typeparam name="TState">The type representing the states.</typeparam>
    /// <typeparam name="TTrigger">The type representing the triggers.</typeparam>
    public sealed class StateMachine<TState, TTrigger>
    {
        private TState _currentState;
        private readonly Dictionary<(TState, TTrigger), TState> _transitions = new();
        private readonly Dictionary<(TState, TTrigger), Func<bool>> _guardClauses = new();
        private readonly Dictionary<TState, Action> _onEnterCallbacks = new();
        private readonly Dictionary<TState, Action> _onExitCallbacks = new();
        private Action<TState, TState, TTrigger>? _onTransitionCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine{TState, TTrigger}"/> class.
        /// </summary>
        /// <param name="initialState">The initial state of the machine.</param>
        public StateMachine(TState initialState)
        {
            _currentState = initialState;
        }

        /// <summary>
        /// Configures a transition between states triggered by a specific trigger.
        /// </summary>
        /// <param name="from">The starting state.</param>
        /// <param name="trigger">The trigger that initiates the transition.</param>
        /// <param name="to">The resulting state.</param>
        public void Configure(TState from, TTrigger trigger, TState to)
        {
            _transitions[(from, trigger)] = to;
            _guardClauses.Remove((from, trigger));
        }

        /// <summary>
        /// Configures a transition between states triggered by a specific trigger, with a guard clause.
        /// </summary>
        /// <param name="from">The starting state.</param>
        /// <param name="trigger">The trigger that initiates the transition.</param>
        /// <param name="to">The resulting state.</param>
        /// <param name="guardClause">A function that returns true if the transition is allowed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="guardClause"/> is null.</exception>
        public void Configure(TState from, TTrigger trigger, TState to, Func<bool> guardClause)
        {
            ArgumentNullException.ThrowIfNull(guardClause);

            _transitions[(from, trigger)] = to;
            _guardClauses[(from, trigger)] = guardClause;
        }

        /// <summary>
        /// Registers a callback to be executed when entering a specific state.
        /// </summary>
        /// <param name="state">The state to associate the callback with.</param>
        /// <param name="callback">The action to perform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
        public void OnEnter(TState state, Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _onEnterCallbacks[state] = callback;
        }

        /// <summary>
        /// Registers a callback to be executed when exiting a specific state.
        /// </summary>
        /// <param name="state">The state to associate the callback with.</param>
        /// <param name="callback">The action to perform.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
        public void OnExit(TState state, Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _onExitCallbacks[state] = callback;
        }

        /// <summary>
        /// Registers a callback to be executed when a transition occurs.
        /// </summary>
        /// <param name="callback">The action to perform, receiving previous state, current state, and trigger as parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback"/> is null.</exception>
        public void OnTransition(Action<TState, TState, TTrigger> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _onTransitionCallback = callback;
        }

        /// <summary>
        /// Determines if a trigger can be fired in the current state, considering guard clauses.
        /// </summary>
        /// <param name="trigger">The trigger to check.</param>
        /// <returns>True if the transition is valid; otherwise, false.</returns>
        public bool CanFire(TTrigger trigger)
        {
            if (!_transitions.ContainsKey((_currentState, trigger)))
                return false;

            if (_guardClauses.TryGetValue((_currentState, trigger), out var guardClause) && guardClause != null)
                return guardClause();

            return true;
        }

        /// <summary>
        /// Attempts to fire a trigger, transitioning the state machine if valid.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <returns>True if the transition was successful; otherwise, false.</returns>
        public bool Fire(TTrigger trigger)
        {
            if (!CanFire(trigger))
                return false;

            var nextState = _transitions[(_currentState, trigger)];

            // Call exit callback
            if (_onExitCallbacks.TryGetValue(_currentState, out var exitCallback))
                exitCallback?.Invoke();

            var previousState = _currentState;
            _currentState = nextState;

            // Call transition callback
            _onTransitionCallback?.Invoke(previousState, _currentState, trigger);

            // Call enter callback
            if (_onEnterCallbacks.TryGetValue(_currentState, out var enterCallback))
                enterCallback?.Invoke();

            return true;
        }

        /// <summary>
        /// Gets the current state of the state machine.
        /// </summary>
        /// <returns>The current state.</returns>
        public TState GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// Manually forces the state machine to a specific state, bypassing all transitions.
        /// </summary>
        /// <param name="state">The target state.</param>
        public void Reset(TState state)
        {
            _currentState = state;
        }

        /// <summary>
        /// Gets a list of available triggers in the current state.
        /// </summary>
        /// <returns>A list of available triggers.</returns>
        public List<TTrigger> GetAvailableTransitions()
        {
            var available = new List<TTrigger>();
            foreach (var key in _transitions.Keys)
            {
                if (key.Item1.Equals(_currentState))
                {
                    if (!_guardClauses.TryGetValue(key, out var guardClause) || guardClause())
                        available.Add(key.Item2);
                }
            }
            return available;
        }
    }
}
