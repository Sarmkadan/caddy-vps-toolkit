#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using CaddyVpsToolkit.Utilities;
using FluentAssertions;
using Xunit;

namespace CaddyVpsToolkit.Tests.Utilities
{
    public sealed class StateMachineTests
    {
        private enum State { Idle, Running, Paused, Stopped }
        private enum Trigger { Start, Pause, Resume, Stop, Reset }

        private StateMachine<State, Trigger> BuildServiceMachine()
        {
            var sm = new StateMachine<State, Trigger>(State.Idle);
            sm.Configure(State.Idle, Trigger.Start, State.Running);
            sm.Configure(State.Running, Trigger.Pause, State.Paused);
            sm.Configure(State.Running, Trigger.Stop, State.Stopped);
            sm.Configure(State.Paused, Trigger.Resume, State.Running);
            sm.Configure(State.Paused, Trigger.Stop, State.Stopped);
            sm.Configure(State.Stopped, Trigger.Reset, State.Idle);
            return sm;
        }

        [Fact]
        public void GetCurrentState_InitialState_ReturnsInitialState()
        {
            var sm = new StateMachine<State, Trigger>(State.Idle);

            sm.GetCurrentState().Should().Be(State.Idle);
        }

        [Fact]
        public void Fire_ValidTransition_ChangesState()
        {
            var sm = BuildServiceMachine();

            var fired = sm.Fire(Trigger.Start);

            fired.Should().BeTrue();
            sm.GetCurrentState().Should().Be(State.Running);
        }

        [Fact]
        public void Fire_InvalidTransition_ReturnsFalseAndKeepsState()
        {
            var sm = BuildServiceMachine();

            var fired = sm.Fire(Trigger.Stop); // Idle → Stop not configured

            fired.Should().BeFalse();
            sm.GetCurrentState().Should().Be(State.Idle);
        }

        [Fact]
        public void CanFire_ValidTrigger_ReturnsTrue()
        {
            var sm = BuildServiceMachine();

            sm.CanFire(Trigger.Start).Should().BeTrue();
        }

        [Fact]
        public void CanFire_InvalidTrigger_ReturnsFalse()
        {
            var sm = BuildServiceMachine();

            sm.CanFire(Trigger.Pause).Should().BeFalse();
        }

        [Fact]
        public void Fire_MultipleTransitions_TracksStateCorrectly()
        {
            var sm = BuildServiceMachine();

            sm.Fire(Trigger.Start);
            sm.Fire(Trigger.Pause);
            sm.Fire(Trigger.Resume);
            sm.Fire(Trigger.Stop);

            sm.GetCurrentState().Should().Be(State.Stopped);
        }

        [Fact]
        public void OnEnter_CallbackInvokedOnStateEntry()
        {
            var sm = BuildServiceMachine();
            bool enterCalled = false;
            sm.OnEnter(State.Running, () => enterCalled = true);

            sm.Fire(Trigger.Start);

            enterCalled.Should().BeTrue();
        }

        [Fact]
        public void OnExit_CallbackInvokedOnStateExit()
        {
            var sm = BuildServiceMachine();
            bool exitCalled = false;
            sm.OnExit(State.Idle, () => exitCalled = true);

            sm.Fire(Trigger.Start);

            exitCalled.Should().BeTrue();
        }

        [Fact]
        public void OnEnter_NotCalledForInvalidTransition()
        {
            var sm = BuildServiceMachine();
            bool enterCalled = false;
            sm.OnEnter(State.Running, () => enterCalled = true);

            sm.Fire(Trigger.Stop); // invalid from Idle

            enterCalled.Should().BeFalse();
        }

        [Fact]
        public void Reset_SetsStateToGivenState()
        {
            var sm = BuildServiceMachine();
            sm.Fire(Trigger.Start);
            sm.Fire(Trigger.Stop);

            sm.Reset(State.Idle);

            sm.GetCurrentState().Should().Be(State.Idle);
        }

        [Fact]
        public void GetAvailableTransitions_ReturnsCorrectTriggers()
        {
            var sm = BuildServiceMachine();
            sm.Fire(Trigger.Start); // now Running

            var triggers = sm.GetAvailableTransitions();

            triggers.Should().Contain(Trigger.Pause);
            triggers.Should().Contain(Trigger.Stop);
            triggers.Should().NotContain(Trigger.Start);
            triggers.Should().NotContain(Trigger.Resume);
        }

        [Fact]
        public void GetAvailableTransitions_FromInitialState_ReturnsOnlyValidTriggers()
        {
            var sm = BuildServiceMachine();

            var triggers = sm.GetAvailableTransitions();

            triggers.Should().ContainSingle().Which.Should().Be(Trigger.Start);
        }
    }
}
