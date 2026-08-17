using System;
using System.Threading.Tasks;
using CaddyVpsToolkit.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CaddyVpsToolkit.Tests.Events
{
    [TestClass]
    public class EventBusTests
    {
        [TestMethod]
        public async Task PublishAsync_AllHandlersExecutedEvenIfOneFails()
        {
            // Arrange
            var eventBus = new EventBus();
            var handler1 = new TestEventHandler();
            var handler2 = new FailingTestEventHandler();
            var handler3 = new TestEventHandler();

            eventBus.Subscribe<TestEvent>(handler1);
            eventBus.Subscribe<TestEvent>(handler2);
            eventBus.Subscribe<TestEvent>(handler3);

            // Act
            await eventBus.PublishAsync(new TestEvent());

            // Assert
            Assert.IsTrue(handler1.Handled);
            Assert.IsTrue(handler2.Handled);
            Assert.IsTrue(handler3.Handled);
        }

        private class TestEvent : DomainEvent { }

        private class TestEventHandler : IEventHandler<TestEvent>
        {
            public bool Handled { get; private set; }

            public async Task HandleAsync(TestEvent @event)
            {
                Handled = true;
            }
        }

        private class FailingTestEventHandler : IEventHandler<TestEvent>
        {
            public bool Handled { get; private set; }

            public async Task HandleAsync(TestEvent @event)
            {
                Handled = true;
                throw new Exception("Test exception");
            }
        }
    }
}
