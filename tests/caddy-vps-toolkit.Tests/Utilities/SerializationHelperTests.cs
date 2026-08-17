using System;
using Xunit;
using CaddyVpsToolkit.Utilities;
using System.Text.Json;

namespace CaddyVpsToolkit.Tests.Utilities
{
    public class SerializationHelperTests
    {
        private class Node
        {
            public string? Name { get; set; }
            public Node? Next { get; set; }
        }

        [Fact]
        public void ToJson_WithCyclicGraph_ThrowsInvalidOperationException()
        {
            var node1 = new Node { Name = "node1" };
            var node2 = new Node { Name = "node2" };
            node1.Next = node2;
            node2.Next = node1; // cycle

            var ex = Assert.Throws<InvalidOperationException>(() => SerializationHelper.ToJson(node1));
            Assert.IsType<JsonException>(ex.InnerException);
            Assert.Contains("cycle", ex.InnerException!.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
