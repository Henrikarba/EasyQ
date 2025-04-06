using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Bridge.Search;

namespace EasyQTests.Search
{
    // Person class for the demo
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

    public class SearchTests : ITestRunner
    {
        public string Name => "Quantum Search Tests";
        public string Description => "Run all quantum search tests";

        public async Task RunAsync()
        {
            // Run all search tests
            await RunSimpleSearchTests();
            Console.WriteLine();
            await RunPredicateSearchTests();
            Console.WriteLine();
            await RunOptionsExamples();
        }

        private async Task RunSimpleSearchTests()
        {
            Console.WriteLine("\nSimple Search Tests");
            Console.WriteLine("------------------");
            
            // Create a mock database of names
            var nameDatabase = new List<string>
            {
                "Alice", "Bob", "Charlie", "David",
                "Emma", "Frank", "Grace", "Henry",
                "Isabella", "Jack", "Katherine", "Liam",
                "Mia", "Noah", "Olivia", "Peter"
            };
            
            Console.WriteLine($"Database size: {nameDatabase.Count} items");
            
            // Create options with logging enabled for test visibility
            var options = new QuantumSearchOptions
            {
                EnableLogging = true,
                MaxAttempts = 100 // Increase max attempts to improve success rate
            };
            
            using var searcher = new QuantumSearch(options);
            
            // Test 1: Search for a specific name
            Console.WriteLine("\n1. Searching for 'Grace'");
            
            var results = await searcher.Search(nameDatabase, "Grace");
            
            Console.WriteLine($"Found {results.Count} results:");
            foreach (var result in results)
            {
                Console.WriteLine($"- '{result.Item}' at index {result.Index}");
            }
            
            // Test 2: Search in a non-power-of-2 database
            Console.WriteLine("\n2. Searching in a non-power-of-2 database");
            
            // Create a database with a non-power-of-2 size
            var nonPowerOfTwoDatabase = new List<string>(nameDatabase);
            nonPowerOfTwoDatabase.Add("Quinn");
            nonPowerOfTwoDatabase.Add("Rachel");
            nonPowerOfTwoDatabase.Add("Sam");
            
            Console.WriteLine($"Database size: {nonPowerOfTwoDatabase.Count} (not a power of 2)");
            
            results = await searcher.Search(nonPowerOfTwoDatabase, "Mia");
            
            Console.WriteLine($"Found {results.Count} results:");
            foreach (var result in results)
            {
                Console.WriteLine($"- '{result.Item}' at index {result.Index}");
            }
            
            // Test 3: Search for a name that appears multiple times
            Console.WriteLine("\n3. Searching in a database with duplicates");
            
            var duplicateDatabase = new List<string>
            {
                "Alice", "Bob", "Charlie", "Alice",
                "Emma", "Alice", "Grace", "Henry"
            };
            
            Console.WriteLine($"Database size: {duplicateDatabase.Count} items");
            
            results = await searcher.Search(duplicateDatabase, "Alice");
            
            Console.WriteLine($"Found {results.Count} occurrences of 'Alice':");
            foreach (var result in results)
            {
                Console.WriteLine($"- At index {result.Index}");
            }
        }

        private async Task RunPredicateSearchTests()
        {
            Console.WriteLine("\nPredicate Search Tests");
            Console.WriteLine("---------------------");
            
            // Create a mock database with various entries
            var productDatabase = new List<string>
            {
                "Laptop", "Phone", "Camera", "Headphones", 
                "Tablet", "Camera", "Smart Watch", "Camera",
                "Drone", "Phone", "Laptop", "Speakers"
            };
            
            Console.WriteLine($"Database size: {productDatabase.Count} items");
            
            var options = new QuantumSearchOptions
            {
                EnableLogging = true,
                SamplingStrategy = SamplingStrategy.FullScan
            };
            
            using var searcher = new QuantumSearch(options);
            
            // Test 1: Find all products with length <= 5
            Console.WriteLine("\n1. Finding all products with name length <= 5");
            
            Func<string, bool> shortNamePredicate = product => product.Length <= 5;
            var results = await searcher.Search(productDatabase, shortNamePredicate);
            
            Console.WriteLine($"Found {results.Count} products with short names:");
            foreach (var match in results)
            {
                Console.WriteLine($"- '{match.Item}' (length: {match.Item.Length}) at index {match.Index}");
            }
            
            // Test 2: Find all items that start with specific letters
            Console.WriteLine("\n2. Finding all products that start with 'P' or 'S'");
            
            Func<string, bool> startsWithPorS = product => 
                product.StartsWith("P", StringComparison.OrdinalIgnoreCase) || 
                product.StartsWith("S", StringComparison.OrdinalIgnoreCase);
            
            results = await searcher.Search(productDatabase, startsWithPorS);
            
            Console.WriteLine($"Found {results.Count} matching products:");
            foreach (var match in results)
            {
                Console.WriteLine($"- '{match.Item}' at index {match.Index}");
            }
            
            // Test 3: Find all people with the same age
            Console.WriteLine("\n3. Finding all people with age 30");
            
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 30 },
                new Person { Id = 2, Name = "Bob", Age = 25 },
                new Person { Id = 3, Name = "Charlie", Age = 30 },
                new Person { Id = 4, Name = "David", Age = 40 },
                new Person { Id = 5, Name = "Emma", Age = 30 },
                new Person { Id = 6, Name = "Frank", Age = 25 }
            };
            
            Console.WriteLine($"Database size: {people.Count} people");
            
            // For this test, we know the exact number of matches (3 people with age 30)
            var peopleResults = await searcher.Search(people, p => p.Age == 30, knownMatchCount: 3);
            
            Console.WriteLine($"Found {peopleResults.Count} people who are 30 years old:");
            foreach (var match in peopleResults)
            {
                Console.WriteLine($"- ID: {match.Item.Id}, Name: {match.Item.Name} at index {match.Index}");
            }
        }
        
        private async Task RunOptionsExamples()
        {
            Console.WriteLine("\nSearch Options Examples");
            Console.WriteLine("=====================");
            
            // Create a database with words (easier to understand than numbers)
            var fruitDatabase = new List<string>
            {
                "apple", "banana", "cherry", "date", "elderberry",
                "fig", "grape", "honeydew", "imbe", "jackfruit",
                "kiwi", "lemon", "mango", "nectarine", "orange",
                "peach", "quince", "raspberry", "strawberry", "tangerine",
                "ugli", "vanilla", "watermelon", "xigua", "yuzu",
                "zucchini", "apricot", "blackberry", "coconut", "dragonfruit"
            };
            
            Console.WriteLine($"Database contains {fruitDatabase.Count} fruit names");
            
            // Example 1: Default options (simplest case)
            Console.WriteLine("\nExample 1: Using default options");
            
            using (var searcher = new QuantumSearch())
            {
                Console.WriteLine("Searching for fruits starting with 'a'...");
                
                var results = await searcher.Search(fruitDatabase, s => s.StartsWith("a"));
                
                Console.WriteLine($"Found {results.Count} results:");
                foreach (var result in results)
                {
                    Console.WriteLine($"- {result.Item}");
                }
            }
            
            // Example 2: Enable logging to see what's happening
            Console.WriteLine("\nExample 2: Enable logging");
            
            var loggingOptions = new QuantumSearchOptions
            {
                EnableLogging = true
            };
            
            using (var searcher = new QuantumSearch(loggingOptions))
            {
                Console.WriteLine("Searching for fruits containing 'berry'...");
                
                var results = await searcher.Search(fruitDatabase, s => s.Contains("berry"));
                
                Console.WriteLine($"Found {results.Count} results");
            }
            
            // Example 3: Using a specific iteration strategy
            Console.WriteLine("\nExample 3: Using different iteration strategies");
            
            Console.WriteLine("\n3.1: Using SingleIteration strategy (fastest but less accurate)");
            var fastOptions = new QuantumSearchOptions
            {
                EnableLogging = true,
                IterationStrategy = IterationStrategy.SingleIteration
            };
            
            using (var searcher = new QuantumSearch(fastOptions))
            {
                var results = await searcher.Search(fruitDatabase, s => s.Length > 9);
                Console.WriteLine($"Found {results.Count} fruits with names longer than 9 characters");
            }
            
            Console.WriteLine("\n3.2: Using Aggressive strategy (more iterations, higher accuracy)");
            var accurateOptions = new QuantumSearchOptions
            {
                EnableLogging = true,
                IterationStrategy = IterationStrategy.Aggressive
            };
            
            using (var searcher = new QuantumSearch(accurateOptions))
            {
                var results = await searcher.Search(fruitDatabase, s => s.Length > 9);
                Console.WriteLine($"Found {results.Count} fruits with names longer than 9 characters");
            }
            
            // Example 4: Using different sampling strategies
            Console.WriteLine("\nExample 4: Using different sampling strategies");
            
            Console.WriteLine("\n4.1: Using FullScan strategy (exact count but slower)");
            var fullScanOptions = new QuantumSearchOptions
            {
                EnableLogging = true,
                SamplingStrategy = SamplingStrategy.FullScan
            };
            
            using (var searcher = new QuantumSearch(fullScanOptions))
            {
                var results = await searcher.Search(fruitDatabase, s => s.Contains("e"));
                Console.WriteLine($"Found {results.Count} fruits containing the letter 'e'");
            }
            
            Console.WriteLine("\n4.2: Using Sampling strategy (estimated count but faster)");
            var samplingOptions = new QuantumSearchOptions
            {
                EnableLogging = true,
                SamplingStrategy = SamplingStrategy.Sampling,
                SampleSize = 10  // Only check 10 random items to estimate total
            };
            
            using (var searcher = new QuantumSearch(samplingOptions))
            {
                var results = await searcher.Search(fruitDatabase, s => s.Contains("e"));
                Console.WriteLine($"Found {results.Count} fruits containing the letter 'e'");
            }
            
            // Example 5: When you know exactly how many matches exist
            Console.WriteLine("\nExample 5: When you know exactly how many matches exist");
            
            var knownCountOptions = new QuantumSearchOptions
            {
                EnableLogging = true,
                SamplingStrategy = SamplingStrategy.UserProvided,
                SampleSize = 3  // We know there are exactly 3 fruits starting with 'p'
            };
            
            using (var searcher = new QuantumSearch(knownCountOptions))
            {
                var results = await searcher.Search(fruitDatabase, s => s.StartsWith("p"));
                Console.WriteLine($"Found {results.Count} fruits starting with 'p'");
            }
            
            Console.WriteLine("\nOptions Examples Completed");
        }
    }
}