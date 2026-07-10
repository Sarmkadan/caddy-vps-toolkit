# StateMachineTests

`StateMachineTests` is the unit test suite for the `StateMachine` class in the `caddy-vps-toolkit` project. It validates state transitions, trigger eligibility, callback invocations, and reset behaviour, ensuring the state machine correctly enforces its configuration under both valid and invalid operations.

## API

### public void GetCurrentState_InitialState_ReturnsInitialState
Verifies that a newly constructed state machine reports the expected initial state via `GetCurrentState()`. No parameters. Does not return a value; asserts equality between the actual current state and the configured initial state.

### public void Fire_ValidTransition_ChangesState
Confirms that invoking `Fire` with a trigger that has a configured transition from the current state causes the state machine to move to the target state. No parameters. Asserts that `GetCurrentState()` returns the new state after the call.

### public void Fire_InvalidTransition_ReturnsFalseAndKeepsState
Ensures that calling `Fire` with a trigger not valid for the current state returns `false` and leaves the current state unchanged. No parameters. Asserts the return value is `false` and that `GetCurrentState()` still reports the original state.

### public void CanFire_ValidTrigger_ReturnsTrue
Validates that `CanFire` returns `true` when queried with a trigger that is permitted from the current state. No parameters. Asserts the boolean result.

### public void CanFire_InvalidTrigger_ReturnsFalse
Validates that `CanFire` returns `false` when queried with a trigger that has no transition defined from the current state. No parameters. Asserts the boolean result.

### public void Fire_MultipleTransitions_TracksStateCorrectly
Exercises a sequence of valid `Fire` calls and checks that the state machine tracks the correct state after each step. No parameters. Asserts the state after every transition in the chain.

### public void OnEnter_CallbackInvokedOnStateEntry
Verifies that an `OnEnter` callback registered for a target state is invoked exactly once when a valid transition enters that state. No parameters. Typically asserts a side effect or counter incremented by the callback.

### public void OnExit_CallbackInvokedOnStateExit
Verifies that an `OnExit` callback registered for the source state is invoked exactly once when a valid transition leaves that state. No parameters. Typically asserts a side effect or counter incremented by the callback.

### public void OnEnter_NotCalledForInvalidTransition
Ensures that `OnEnter` callbacks are not invoked when `Fire` is called with an invalid trigger and the transition does not occur. No parameters. Asserts that callback side effects remain absent.

### public void Reset_SetsStateToGivenState
Confirms that calling `Reset` with a specific state forces the state machine into that state regardless of its current state. No parameters. Asserts `GetCurrentState()` returns the state passed to `Reset`.

### public void GetAvailableTransitions_ReturnsCorrectTriggers
Checks that `GetAvailableTransitions` returns the set of triggers that are valid from the current state. No parameters. Asserts the collection contents match the expected triggers.

### public void GetAvailableTransitions_FromInitialState_ReturnsOnlyValidTriggers
Validates that immediately after construction `GetAvailableTransitions` returns exactly the triggers permitted from the initial state, with no extras. No parameters. Asserts the collection contents.

## Usage

```csharp
// Example 1: Basic state transition with guard checks
var sm = new StateMachine<LightState, LightTrigger>(
    LightState.Off,
    new Dictionary<LightState, Dictionary<LightTrigger, LightState>>
    {
        { LightState.Off, new Dictionary<LightTrigger, LightState> { { LightTrigger.TurnOn, LightState.On } } },
        { LightState.On,  new Dictionary<LightTrigger, LightState> { { LightTrigger.TurnOff, LightState.Off } } }
    });

// Verify initial state
Assert.AreEqual(LightState.Off, sm.GetCurrentState());

// Check available triggers from Off
var available = sm.GetAvailableTransitions();
CollectionAssert.AreEquivalent(new[] { LightTrigger.TurnOn }, available);

// Valid transition
bool canTurnOn = sm.CanFire(LightTrigger.TurnOn); // true
bool fired = sm.Fire(LightTrigger.TurnOn);         // true
Assert.AreEqual(LightState.On, sm.GetCurrentState());

// Invalid transition attempt
bool canTurnOnAgain = sm.CanFire(LightTrigger.TurnOn); // false
bool firedAgain = sm.Fire(LightTrigger.TurnOn);         // false
Assert.AreEqual(LightState.On, sm.GetCurrentState());   // state unchanged
```

```csharp
// Example 2: Callbacks and reset
var enteredStates = new List<string>();
var exitedStates = new List<string>();

var sm = new StateMachine<string, string>(
    "idle",
    new Dictionary<string, Dictionary<string, string>>
    {
        { "idle", new Dictionary<string, string> { { "start", "running" } } },
        { "running", new Dictionary<string, string> { { "stop", "idle" } } }
    });

sm.OnEnter("running", () => enteredStates.Add("running"));
sm.OnExit("idle", () => exitedStates.Add("idle"));

// Fire valid transition; callbacks should fire
sm.Fire("start");
Assert.Contains("running", enteredStates);
Assert.Contains("idle", exitedStates);

// Reset to a different state
sm.Reset("running");
Assert.AreEqual("running", sm.GetCurrentState());

// Invalid trigger does not invoke OnEnter
sm.Fire("start"); // "start" not valid from "running"
Assert.AreEqual(1, enteredStates.Count); // no additional entry
```

## Notes

- All test methods are parameterless and rely on fixture setup (typically in a constructor or `[SetUp]` method) to provide a configured `StateMachine` instance. The fixture defines states, triggers, transitions, and optional callbacks.
- `Fire` returning `false` always implies the state remains unchanged; no partial transition or callback leakage occurs.
- `Reset` bypasses transition rules and callbacks—`OnExit`/`OnEnter` are not invoked during a reset. Tests for `Reset` should confirm only the state change.
- `GetAvailableTransitions` returns a snapshot of valid triggers at the moment of the call. If the state changes, a subsequent call reflects the new state’s triggers.
- These tests are single-threaded by nature (standard unit test execution). The `StateMachine` implementation itself is not guaranteed thread-safe unless explicitly documented; concurrent `Fire` or `Reset` calls from multiple threads would require external synchronisation.
