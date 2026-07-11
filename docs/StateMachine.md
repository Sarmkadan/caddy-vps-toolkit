# StateMachine
The `StateMachine` class is a fundamental component of the `caddy-vps-toolkit` project, designed to manage and execute state transitions based on predefined triggers and rules. It provides a structured approach to handling complex state changes, allowing for more organized and maintainable code.

## API
* `public StateMachine`: The constructor initializes a new instance of the `StateMachine` class.
* `public void Configure`: Configures the state machine with the necessary settings and rules. This method does not take any parameters and does not return a value.
* `public void OnEnter`: Called when the state machine enters a new state. This method does not take any parameters and does not return a value.
* `public void OnExit`: Called when the state machine exits the current state. This method does not take any parameters and does not return a value.
* `public bool CanFire`: Determines whether a transition can be fired based on the current state and trigger. This method does not take any parameters and returns a boolean value indicating whether the transition is possible.
* `public bool Fire`: Attempts to fire a transition based on the current state and trigger. This method does not take any parameters and returns a boolean value indicating whether the transition was successful.
* `public TState GetCurrentState`: Retrieves the current state of the state machine. This method does not take any parameters and returns the current state.
* `public void Reset`: Resets the state machine to its initial state. This method does not take any parameters and does not return a value.
* `public List<TTrigger> GetAvailableTransitions`: Retrieves a list of available transitions based on the current state. This method does not take any parameters and returns a list of triggers.

## Usage
The following examples demonstrate how to use the `StateMachine` class:
```csharp
// Example 1: Basic state machine usage
var stateMachine = new StateMachine();
stateMachine.Configure();
stateMachine.OnEnter();
var currentState = stateMachine.GetCurrentState();
if (stateMachine.CanFire())
{
    stateMachine.Fire();
}
```

```csharp
// Example 2: Using the state machine with transitions
var stateMachine = new StateMachine();
stateMachine.Configure();
var availableTransitions = stateMachine.GetAvailableTransitions();
foreach (var trigger in availableTransitions)
{
    if (stateMachine.CanFire())
    {
        stateMachine.Fire();
        break;
    }
}
stateMachine.Reset();
```

## Notes
The `StateMachine` class is designed to be used in a single-threaded environment. If used in a multi-threaded environment, proper synchronization mechanisms should be implemented to ensure thread safety. Additionally, the `CanFire` and `Fire` methods may throw exceptions if the state machine is not properly configured or if the transition rules are not met. It is recommended to handle these exceptions accordingly to prevent unexpected behavior. The `Reset` method should be used with caution, as it resets the state machine to its initial state, potentially losing any progress or data.
