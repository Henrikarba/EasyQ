using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Algorithms;
using Xunit;
using Xunit.Abstractions;

namespace EasyQTests.Algorithms
{
    public class QuantumSearchTests
    {
        private readonly ITestOutputHelper _output;

        public QuantumSearchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Search_FindsExistingItem()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(products, "Camera");

            // Assert
            Assert.Equal("Camera", result.Item);
            Assert.Equal(2, result.Index);
            _output.WriteLine($"Found '{result.Item}' at index {result.Index}");
        }

        [Fact]
        public async Task Search_FindsItemInLargerCollection()
        {
            // Arrange
            var products = new List<string>
            {
                "Smartphone", "Laptop", "Tablet", "Headphones", "Smart Watch", "Camera", 
                "Printer", "Monitor", "Keyboard", "Mouse", "External Drive", "Router",
                "Speaker", "Microphone", "Charger", "Game Console", "VR Headset", "Drone"
            };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(products, "Router");

            // Assert
            Assert.Equal("Router", result.Item);
            Assert.Equal(11, result.Index);
            _output.WriteLine($"Found '{result.Item}' at index {result.Index}");
        }

        [Fact]
        public async Task Search_FindsFirstItem()
        {
            // Arrange - edge case with item at first position
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(products, "Laptop");

            // Assert
            Assert.Equal("Laptop", result.Item);
            Assert.Equal(0, result.Index);
            _output.WriteLine($"Found '{result.Item}' at index {result.Index} (first position)");
        }

        [Fact]
        public async Task Search_FindsLastItem()
        {
            // Arrange - edge case with item at last position
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(products, "Headphones");

            // Assert
            Assert.Equal("Headphones", result.Item);
            Assert.Equal(3, result.Index);
            _output.WriteLine($"Found '{result.Item}' at index {result.Index} (last position)");
        }

        [Fact]
        public async Task Search_WithNonPowerOf2Size()
        {
            // Arrange - edge case with collection size not a power of 2
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones", "Tablet" };
            _output.WriteLine($"Collection size: {products.Count} (not a power of 2)");
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(products, "Tablet");

            // Assert
            Assert.Equal("Tablet", result.Item);
            Assert.Equal(4, result.Index);
            _output.WriteLine($"Successfully found '{result.Item}' at index {result.Index}");
        }

        [Fact]
        public async Task Search_WithSingleItem()
        {
            // Arrange - edge case with collection size of 1
            var singleItem = new List<string> { "OnlyItem" };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(singleItem, "OnlyItem");

            // Assert
            Assert.Equal("OnlyItem", result.Item);
            Assert.Equal(0, result.Index);
            _output.WriteLine($"Successfully found single item at index {result.Index}");
        }

        [Fact]
        public async Task SearchWhere_FindsMatchingItem()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.SearchWhere(products, item => item.StartsWith("C"));

            // Assert
            Assert.Equal("Camera", result.Item);
            Assert.Equal(2, result.Index);
            _output.WriteLine($"Found '{result.Item}' at index {result.Index} matching predicate");
        }

        [Fact]
        public async Task SearchWhere_WithMultipleMatches()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();

            // Act - should find one of these items
            var result = await quantumSearch.SearchWhere(products, item => item.Length > 5);

            // Assert
            Assert.Contains(result.Item, new[] { "Laptop", "Camera", "Headphones" });
            _output.WriteLine($"Found '{result.Item}' at index {result.Index} matching predicate (one of multiple matches)");
        }

        [Fact]
        public async Task SearchWhere_WithComplexPredicate()
        {
            // Arrange
            var products = new List<string> 
            { 
                "Laptop", "Phone", "Camera", "Headphones", 
                "Smart Watch", "Keyboard", "Mouse", "Charger" 
            };
            using var quantumSearch = new QuantumSearch();

            // Act - find an item containing 'a' and with length > 6
            var result = await quantumSearch.SearchWhere(
                products, 
                item => item.Contains("a") && item.Length > 6
            );

            // Assert
            Assert.Contains(result.Item, new[] { "Laptop", "Camera", "Headphones", "Keyboard", "Smart Watch", "Charger" });
            _output.WriteLine($"Found '{result.Item}' matching complex predicate");
        }

        [Fact]
        public async Task Search_WithGenericTypes()
        {
            // Arrange
            var numbers = new List<int> { 10, 20, 30, 40, 50 };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(numbers, 30);

            // Assert
            Assert.Equal(30, result.Item);
            Assert.Equal(2, result.Index);
            _output.WriteLine($"Found number {result.Item} at index {result.Index}");
        }

        [Fact]
        public async Task SearchWhere_WithGenericTypes()
        {
            // Arrange
            var numbers = new List<int> { 11, 22, 33, 44, 55 };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.SearchWhere(numbers, n => n % 11 == 0);

            // Assert
            Assert.Contains(result.Item, new[] { 11, 22, 33, 44, 55 });
            _output.WriteLine($"Found number {result.Item} at index {result.Index} matching predicate");
        }

        [Fact]
        public async Task Search_WithCustomObjects()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 25 },
                new Person { Id = 2, Name = "Bob", Age = 30 },
                new Person { Id = 3, Name = "Charlie", Age = 35 },
                new Person { Id = 4, Name = "David", Age = 40 }
            };
            
            var personToFind = new Person { Id = 3, Name = "Charlie", Age = 35 };
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.Search(people, personToFind);

            // Assert
            Assert.Equal(personToFind.Id, result.Item.Id);
            Assert.Equal(2, result.Index);
            _output.WriteLine($"Found person {result.Item.Name} (ID: {result.Item.Id}) at index {result.Index}");
        }

        [Fact]
        public async Task SearchWhere_WithCustomObjects()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 25 },
                new Person { Id = 2, Name = "Bob", Age = 30 },
                new Person { Id = 3, Name = "Charlie", Age = 35 },
                new Person { Id = 4, Name = "David", Age = 40 }
            };
            
            using var quantumSearch = new QuantumSearch();

            // Act
            var result = await quantumSearch.SearchWhere(people, p => p.Age > 30);

            // Assert
            Assert.True(result.Item.Age > 30);
            _output.WriteLine($"Found person {result.Item.Name} with age {result.Item.Age} matching predicate");
        }

        [Fact]
        public async Task Search_ThrowsOnEmptyCollection()
        {
            // Arrange
            var emptyList = new List<string>();
            using var quantumSearch = new QuantumSearch();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                quantumSearch.Search(emptyList, "anything"));
        }

        [Fact]
        public async Task Search_ThrowsOnNullCollection()
        {
            // Arrange
            List<string> nullList = null;
            using var quantumSearch = new QuantumSearch();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                quantumSearch.Search(nullList, "anything"));
        }

        [Fact]
        public async Task Search_ThrowsOnItemNotFound()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera" };
            using var quantumSearch = new QuantumSearch();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                quantumSearch.Search(products, "NotInList"));
        }

        [Fact]
        public async Task SearchWhere_ThrowsWhenNoMatchingItems()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera" };
            using var quantumSearch = new QuantumSearch();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                quantumSearch.SearchWhere(products, item => item.StartsWith("Z")));
        }
    }

    // Example custom class for testing with complex objects
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        
        public override bool Equals(object obj)
        {
            if (obj is Person other)
            {
                return Id == other.Id && Name == other.Name && Age == other.Age;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Age);
        }
    }
}