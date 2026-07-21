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
    /// Simple state machine implementation with transitions and callbacks.
    /// Useful for managing state workflows and validating allowed transitions.
    /// </summary>
    public sealed class StateMachine<TState, TTrigger>
    {
        private TState _currentState;
        private readonly Dictionary<(TState, TTrigger), TState> _transitions = new();
        private readonly Dictionary<(TState, TTrigger), Func<bool>> _guardClauses = new();
        private readonly Dictionary<TState, Action> _onEnterCallbacks = new();
        private readonly Dictionary<TState, Action> _onExitCallbacks = new();
        private Action<TState, TState, TTrigger>? _onTransitionCallback;

        public StateMachine(TState initialState)
        {
            _currentState = initialState;
        }

        public void Configure(TState from, TTrigger trigger, TState to)
        {
            _transitions[(from, trigger)] = to;
            _guardClauses.Remove((from, trigger));
        }

        public void Configure(TState from, TTrigger trigger, TState to, Func<bool> guardClause)
        {
            _transitions[(from, trigger)] = to;
            _guardClauses[(from, trigger)] = guardClause;
        }

        public void OnEnter(TState state, Action callback)
        {
            _onEnterCallbacks[state] = callback;
        }

        public void OnExit(TState state, Action callback)
        {
            _onExitCallbacks[state] = callback;
        }

        public void OnTransition(Action<TState, TState, TTrigger> callback)
        {
            _onTransitionCallback = callback;
        }

        public bool CanFire(TTrigger trigger)
        {
            if (!_transitions.ContainsKey((_currentState, trigger)))
                return false;

            if (_guardClauses.TryGetValue((_currentState, trigger), out var guardClause) && guardClause != null)
                return guardClause();

            return true;
        }

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

        public TState GetCurrentState()
        {
            return _currentState;
        }

        public void Reset(TState state)
        {
            _currentState = state;
        }

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