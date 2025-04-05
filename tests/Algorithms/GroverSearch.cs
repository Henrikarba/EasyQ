using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EasyQ.Algorithms;
using Xunit;
using Xunit.Abstractions;

namespace EasyQTests.Algorithms
{
    public class MultipleMatchesTests
    {
        private readonly ITestOutputHelper _output;

        public MultipleMatchesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task SearchAll_FindsMultipleOccurrences()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Camera", "Phone", "Camera", "Headphones", "Camera" };
            using var quantumSearch = new QuantumSearch();
            var searchItem = "Camera";
            
            _output.WriteLine($"Searching for all occurrences of: {searchItem}");
            _output.WriteLine($"Collection: {string.Join(", ", products)}");

            // Act
            var results = await quantumSearch.SearchAll(products, searchItem);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.Index == 1);
            Assert.Contains(results, r => r.Index == 3);
            Assert.Contains(results, r => r.Index == 5);
            
            _output.WriteLine($"Found {results.Count} occurrences:");
            foreach (var result in results)
            {
                _output.WriteLine($"- '{result.Item}' at index {result.Index}");
            }
        }

        [Fact]
        public async Task SearchAllWhere_FindsAllMatchingItems()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones", "Tablet", "Smart Watch" };
            using var quantumSearch = new QuantumSearch();
            
            _output.WriteLine($"Searching for all items with length > 6");
            _output.WriteLine($"Collection: {string.Join(", ", products)}");

            // Act
            var results = await quantumSearch.SearchAllWhere(products, item => item.Length > 6);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.Item == "Headphones");
            Assert.Contains(results, r => r.Item == "Smart Watch");
            Assert.Contains(results, r => r.Item == "Laptop");
            
            _output.WriteLine($"Found {results.Count} matches:");
            foreach (var result in results)
            {
                _output.WriteLine($"- '{result.Item}' at index {result.Index}");
            }
        }

        [Fact]
        public async Task SearchAll_WithNoMatches_Throws()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();
            var searchItem = "NotInList";
            
            _output.WriteLine($"Testing: SearchAll for '{searchItem}' with no matches should throw");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                quantumSearch.SearchAll(products, searchItem));
                
            _output.WriteLine($"Successfully threw InvalidOperationException as expected");
        }

        [Fact]
        public async Task SearchAllWhere_WithNoMatches_Throws()
        {
            // Arrange
            var products = new List<string> { "Laptop", "Phone", "Camera", "Headphones" };
            using var quantumSearch = new QuantumSearch();
            
            _output.WriteLine($"Testing: SearchAllWhere with no matches should throw");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                quantumSearch.SearchAllWhere(products, item => item.Length > 20));
                
            _output.WriteLine($"Successfully threw InvalidOperationException as expected");
        }
        
        [Fact]
        public async Task SearchAllWhere_WithCustomObjects()
        {
            // Arrange
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 30 },
                new Person { Id = 2, Name = "Bob", Age = 25 },
                new Person { Id = 3, Name = "Charlie", Age = 30 },
                new Person { Id = 4, Name = "David", Age = 35 },
                new Person { Id = 5, Name = "Emma", Age = 30 }
            };
            
            using var quantumSearch = new QuantumSearch();
            
            _output.WriteLine($"Searching for all people with Age = 30");

            // Act
            var results = await quantumSearch.SearchAllWhere(people, p => p.Age == 30);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Contains(results, r => r.Item.Name == "Alice");
            Assert.Contains(results, r => r.Item.Name == "Charlie");
            Assert.Contains(results, r => r.Item.Name == "Emma");
            
            _output.WriteLine($"Found {results.Count} people with age 30:");
            foreach (var result in results)
            {
                _output.WriteLine($"- {result.Item.Name} (ID: {result.Item.Id}) at index {result.Index}");
            }
        }
        
        [Fact]
        public async Task SearchAll_SingleItem_ReturnsOneResult()
        {
            // Arrange
            var numbers = new List<int> { 10, 20, 30, 40, 50 };
            using var quantumSearch = new QuantumSearch();
            var searchItem = 30;
            
            _output.WriteLine($"Searching for all occurrences of: {searchItem}");
            _output.WriteLine($"Collection has only one occurrence");

            // Act
            var results = await quantumSearch.SearchAll(numbers, searchItem);

            // Assert
            Assert.Single(results);
            Assert.Equal(searchItem, results[0].Item);
            Assert.Equal(2, results[0].Index);
            
            _output.WriteLine($"Found 1 occurrence at index {results[0].Index}");
        }
    }
    
    // Example custom class for testing with complex objects
    public class Person
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Age { get; set; }
        
        public override bool Equals(object? obj)
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