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
    public class StateMachine<TState, TTrigger>
    {
        private TState _currentState;
        private readonly Dictionary<(TState, TTrigger), TState> _transitions = new();
        private readonly Dictionary<TState, Action> _onEnterCallbacks = new();
        private readonly Dictionary<TState, Action> _onExitCallbacks = new();

        public StateMachine(TState initialState)
        {
            _currentState = initialState;
        }

        public void Configure(TState from, TTrigger trigger, TState to)
        {
            _transitions[(from, trigger)] = to;
        }

        public void OnEnter(TState state, Action callback)
        {
            _onEnterCallbacks[state] = callback;
        }

        public void OnExit(TState state, Action callback)
        {
            _onExitCallbacks[state] = callback;
        }

        public bool CanFire(TTrigger trigger)
        {
            return _transitions.ContainsKey((_currentState, trigger));
        }

        public bool Fire(TTrigger trigger)
        {
            if (!CanFire(trigger))
                return false;

            var nextState = _transitions[(_currentState, trigger)];

            // Call exit callback
            if (_onExitCallbacks.TryGetValue(_currentState, out var exitCallback))
                exitCallback?.Invoke();

            _currentState = nextState;

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
                    available.Add(key.Item2);
            }
            return available;
        }
    }
}
