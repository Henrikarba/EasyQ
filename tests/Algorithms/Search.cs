using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Algorithms;

namespace EasyQTests
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
            // Run all search tests without asking for user input
            await RunSingleItemSearchTests();
            Console.WriteLine();
            await RunMultipleMatchesSearchTests();
        }

        private async Task RunSingleItemSearchTests()
        {
            Console.WriteLine("\nSingle Item Search Tests");
            Console.WriteLine("------------------------");
            
            // Create a mock database of names (16 names - power of 2 for quantum search)
            var nameDatabase = new List<string>
            {
                "Alice", "Bob", "Charlie", "David",
                "Emma", "Frank", "Grace", "Henry",
                "Isabella", "Jack", "Katherine", "Liam",
                "Mia", "Noah", "Olivia", "Peter"
            };
            
            Console.WriteLine("Mock Database Contents:");
            for (int i = 0; i < nameDatabase.Count; i++)
            {
                Console.WriteLine($"[{i}] {nameDatabase[i]}");
            }
            
            using var searcher = new QuantumSearch();
            
            // Test 1: Search for a specific name by value
            Console.WriteLine("\n1. Searching for a specific name");
            string targetName = "Grace";
            Console.WriteLine($"Target: '{targetName}'");

            var result = await searcher.Search(nameDatabase, targetName);
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            // Test 2: Search in a non-power-of-2 database
            Console.WriteLine("\n2. Searching in a non-power-of-2 database");
            
            // Create a database with a non-power-of-2 size
            var nonPowerOfTwoDatabase = new List<string>(nameDatabase);
            nonPowerOfTwoDatabase.Add("Quinn");
            nonPowerOfTwoDatabase.Add("Rachel");
            nonPowerOfTwoDatabase.Add("Sam");
            
            Console.WriteLine($"Database size: {nonPowerOfTwoDatabase.Count} (not a power of 2)");
            
            targetName = "Mia";
            Console.WriteLine($"Target: '{targetName}'");

            result = await searcher.Search(nonPowerOfTwoDatabase, targetName);
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            // Test 3: Search for a person object
            Console.WriteLine("\n3. Searching for a person object");
            
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 30 },
                new Person { Id = 2, Name = "Bob", Age = 25 },
                new Person { Id = 3, Name = "Charlie", Age = 30 },
                new Person { Id = 4, Name = "David", Age = 35 }
            };
            
            var targetPerson = new Person { Id = 3, Name = "Charlie", Age = 30 };
            Console.WriteLine($"Target: Person with Id={targetPerson.Id}, Name='{targetPerson.Name}', Age={targetPerson.Age}");
            
            var personResult = await searcher.Search(people, targetPerson);
            Console.WriteLine($"Found person with Name='{personResult.Item.Name}' at index {personResult.Index}");
        }

        private async Task RunMultipleMatchesSearchTests()
        {
            Console.WriteLine("\nMultiple Matches Search Tests");
            Console.WriteLine("----------------------------");
            
            // Create a mock database with duplicate entries
            var productDatabase = new List<string>
            {
                "Laptop", "Phone", "Camera", "Headphones", 
                "Tablet", "Camera", "Smart Watch", "Camera",
                "Drone", "Phone", "Laptop", "Speakers"
            };
            
            Console.WriteLine("Product Database Contents:");
            for (int i = 0; i < productDatabase.Count; i++)
            {
                Console.WriteLine($"[{i}] {productDatabase[i]}");
            }
            
            using var searcher = new QuantumSearch();
            
            // Test 1: Find all occurrences of "Camera"
            Console.WriteLine("\n1. Finding all occurrences of 'Camera'");
            string targetProduct = "Camera";
            
            Console.WriteLine($"Target: '{targetProduct}'");
            var results = await searcher.SearchAll(productDatabase, targetProduct);
            
            Console.WriteLine($"Found {results.Count} occurrences of '{targetProduct}':");
            
            foreach (var match in results)
            {
                Console.WriteLine($" - Found at index {match.Index}");
            }
            
            // Test 2: Find all products with length <= 5
            Console.WriteLine("\n2. Finding all products with length <= 5");
            Console.WriteLine("Target: Products with name length <= 5");
            
            Func<string, bool> shortNamePredicate = product => product.Length <= 5;
            results = await searcher.SearchAllWhere(productDatabase, shortNamePredicate);
            
            Console.WriteLine($"Found {results.Count} products with short names:");
            
            foreach (var match in results)
            {
                Console.WriteLine($" - '{match.Item}' (length: {match.Item.Length}) at index {match.Index}");
            }
            
            // Test 3: Find all items that start with specific letters
            Console.WriteLine("\n3. Finding all products that start with 'P' or 'S'");
            Console.WriteLine("Target: Products starting with 'P' or 'S'");
            
            Func<string, bool> startsWithPorS = product => 
                product.StartsWith("P", StringComparison.OrdinalIgnoreCase) || 
                product.StartsWith("S", StringComparison.OrdinalIgnoreCase);
            
            results = await searcher.SearchAllWhere(productDatabase, startsWithPorS);
            
            Console.WriteLine($"Found {results.Count} matching products:");
            
            foreach (var match in results)
            {
                Console.WriteLine($" - '{match.Item}' at index {match.Index}");
            }
            
            // Test 4: Find all people with the same age
            Console.WriteLine("\n4. Finding all people with the same age");
            
            var people = new List<Person>
            {
                new Person { Id = 1, Name = "Alice", Age = 30 },
                new Person { Id = 2, Name = "Bob", Age = 25 },
                new Person { Id = 3, Name = "Charlie", Age = 30 },
                new Person { Id = 4, Name = "David", Age = 40 },
                new Person { Id = 5, Name = "Emma", Age = 30 },
                new Person { Id = 6, Name = "Frank", Age = 25 }
            };
            
            Console.WriteLine("People in database:");
            foreach (var person in people)
            {
                Console.WriteLine($" - ID: {person.Id}, Name: {person.Name}, Age: {person.Age}");
            }
            
            Console.WriteLine("Target: People age 30");
            
            var peopleResults = await searcher.SearchAllWhere(people, p => p.Age == 30);
            
            Console.WriteLine($"Found {peopleResults.Count} people who are 30 years old:");
            
            foreach (var match in peopleResults)
            {
                Console.WriteLine($" - ID: {match.Item.Id}, Name: {match.Item.Name} at index {match.Index}");
            }
        }
    }
}