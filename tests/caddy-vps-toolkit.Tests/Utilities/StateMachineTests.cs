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
	/// <summary>
	/// Unit tests for the <see cref="StateMachine{TState, TTrigger}"/> class.
	/// Tests state transitions, validation, and callback behavior.
	/// </summary>
	public sealed class StateMachineTests
	{
		private enum State { Idle, Running, Paused, Stopped }
		private enum Trigger { Start, Pause, Resume, Stop, Reset }

		/// <summary>
		/// Creates a test state machine with predefined transitions for testing.
		/// </summary>
		/// <returns>A configured <see cref="StateMachine{TState, TTrigger}"/> instance.</returns>
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
			// Arrange
			var sm = new StateMachine<State, Trigger>(State.Idle);

			// Act & Assert
			sm.GetCurrentState().Should().Be(State.Idle);
		}

		[Fact]
		public void Fire_ValidTransition_ChangesState()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act
			var fired = sm.Fire(Trigger.Start);

			// Assert
			fired.Should().BeTrue();
			sm.GetCurrentState().Should().Be(State.Running);
		}

		[Fact]
		public void Fire_InvalidTransition_ReturnsFalseAndKeepsState()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act
			var fired = sm.Fire(Trigger.Stop); // Idle → Stop not configured

			// Assert
			fired.Should().BeFalse();
			sm.GetCurrentState().Should().Be(State.Idle);
		}

		[Fact]
		public void CanFire_ValidTrigger_ReturnsTrue()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act & Assert
			sm.CanFire(Trigger.Start).Should().BeTrue();
		}

		[Fact]
		public void CanFire_InvalidTrigger_ReturnsFalse()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act & Assert
			sm.CanFire(Trigger.Pause).Should().BeFalse();
		}

		[Fact]
		public void Fire_MultipleTransitions_TracksStateCorrectly()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act
			sm.Fire(Trigger.Start);
			sm.Fire(Trigger.Pause);
			sm.Fire(Trigger.Resume);
			sm.Fire(Trigger.Stop);

			// Assert
			sm.GetCurrentState().Should().Be(State.Stopped);
		}

		[Fact]
		public void OnEnter_CallbackInvokedOnStateEntry()
		{
			// Arrange
			var sm = BuildServiceMachine();
			bool enterCalled = false;
			sm.OnEnter(State.Running, () => enterCalled = true);

			// Act
			sm.Fire(Trigger.Start);

			// Assert
			enterCalled.Should().BeTrue();
		}

		[Fact]
		public void OnExit_CallbackInvokedOnStateExit()
		{
			// Arrange
			var sm = BuildServiceMachine();
			bool exitCalled = false;
			sm.OnExit(State.Idle, () => exitCalled = true);

			// Act
			sm.Fire(Trigger.Start);

			// Assert
			exitCalled.Should().BeTrue();
		}

		[Fact]
		public void OnEnter_NotCalledForInvalidTransition()
		{
			// Arrange
			var sm = BuildServiceMachine();
			bool enterCalled = false;
			sm.OnEnter(State.Running, () => enterCalled = true);

			// Act
			sm.Fire(Trigger.Stop); // invalid from Idle

			// Assert
			enterCalled.Should().BeFalse();
		}

		[Fact]
		public void Reset_SetsStateToGivenState()
		{
			// Arrange
			var sm = BuildServiceMachine();
			sm.Fire(Trigger.Start);
			sm.Fire(Trigger.Stop);

			// Act
			sm.Reset(State.Idle);

			// Assert
			sm.GetCurrentState().Should().Be(State.Idle);
		}

		[Fact]
		public void GetAvailableTransitions_ReturnsCorrectTriggers()
		{
			// Arrange
			var sm = BuildServiceMachine();
			sm.Fire(Trigger.Start); // now Running

			// Act
			var triggers = sm.GetAvailableTransitions();

			// Assert
			triggers.Should().Contain(Trigger.Pause);
			triggers.Should().Contain(Trigger.Stop);
			triggers.Should().NotContain(Trigger.Start);
			triggers.Should().NotContain(Trigger.Resume);
		}

		[Fact]
		public void GetAvailableTransitions_FromInitialState_ReturnsOnlyValidTriggers()
		{
			// Arrange
			var sm = BuildServiceMachine();

			// Act
			var triggers = sm.GetAvailableTransitions();

			// Assert
			triggers.Should().ContainSingle().Which.Should().Be(Trigger.Start);
		}
	}
}