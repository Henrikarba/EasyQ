using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyQ.Algorithms;
using Xunit;
using Xunit.Abstractions;

namespace EasyQTests.Algorithms
{
    public class NameDatabaseTests
    {
        private readonly ITestOutputHelper _output;
        private readonly List<string> _nameDatabase;

        public NameDatabaseTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Mock database of names - 16 names (power of 2 for quantum search)
            _nameDatabase = new List<string>
            {
                "Alice", "Bob", "Charlie", "David",
                "Emma", "Frank", "Grace", "Henry",
                "Isabella", "Jack", "Katherine", "Liam",
                "Mia", "Noah", "Olivia", "Peter"
            };
            
            _output.WriteLine($"Initialized name database with {_nameDatabase.Count} names:");
            _output.WriteLine(string.Join(", ", _nameDatabase));
        }

        [Fact]
        public async Task SearchForSpecificName_ShouldFindCorrectIndex()
        {
            // Arrange
            using var searcher = new GroverSearchBridge();
            string targetName = "Grace";
            long targetIndex = _nameDatabase.IndexOf(targetName);
            long databaseSize = _nameDatabase.Count;
            
            _output.WriteLine($"Searching for name '{targetName}' (index {targetIndex})");

            // Act
            var result = await searcher.SearchAsync(databaseSize, targetIndex);
            string foundName = _nameDatabase[(int)result];

            // Assert
            _output.WriteLine($"Found index: {result}, Name: {foundName}");
            Assert.Equal(targetIndex, result);
            Assert.Equal(targetName, foundName);
        }

        [Fact]
        public async Task SearchForNameStartingWithM_ShouldFindMatch()
        {
            // Arrange
            using var searcher = new GroverSearchBridge();
            long databaseSize = _nameDatabase.Count;
            
            // Predicate: Find a name that starts with 'M'
            Func<long, bool> startsWithM = index => 
                index >= 0 && 
                index < _nameDatabase.Count && 
                _nameDatabase[(int)index].StartsWith("M", StringComparison.OrdinalIgnoreCase);
            
            // Count how many names match our criteria (for optimal iterations)
            var matchingNames = _nameDatabase.Where(name => name.StartsWith("M", StringComparison.OrdinalIgnoreCase)).ToList();
            long estimatedMatches = matchingNames.Count;
            
            _output.WriteLine($"Searching for a name starting with 'M' (expecting {estimatedMatches} matches)");
            _output.WriteLine($"Possible matches: {string.Join(", ", matchingNames)}");

            // Act
            var result = await searcher.SearchWithPredicateAsync(databaseSize, startsWithM, estimatedMatches);
            string foundName = _nameDatabase[(int)result];

            // Assert
            _output.WriteLine($"Found index: {result}, Name: {foundName}");
            Assert.True(startsWithM(result), $"The name '{foundName}' should start with 'M'");
        }
        
        [Fact]
        public async Task SearchForLongName_ShouldFindMatch()
        {
            // Arrange
            using var searcher = new GroverSearchBridge();
            long databaseSize = _nameDatabase.Count;
            
            // Predicate: Find a name that is at least 8 characters long
            Func<long, bool> isLongName = index => 
                index >= 0 && 
                index < _nameDatabase.Count && 
                _nameDatabase[(int)index].Length >= 8;
            
            // Count how many names match our criteria
            var matchingNames = _nameDatabase.Where(name => name.Length >= 8).ToList();
            long estimatedMatches = matchingNames.Count;
            
            _output.WriteLine($"Searching for a name with 8+ characters (expecting {estimatedMatches} matches)");
            _output.WriteLine($"Possible matches: {string.Join(", ", matchingNames)}");

            // Act
            var result = await searcher.SearchWithPredicateAsync(databaseSize, isLongName, estimatedMatches);
            string foundName = _nameDatabase[(int)result];

            // Assert
            _output.WriteLine($"Found index: {result}, Name: {foundName}");
            Assert.True(isLongName(result), $"The name '{foundName}' should be at least 8 characters long");
            Assert.True(foundName.Length >= 8, $"Name length: {foundName.Length}");
        }
    }
}