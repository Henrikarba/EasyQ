using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyQ.Algorithms;

namespace EasyQTests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("EasyQ Framework Demo - Search in Mock Database");
            Console.WriteLine("=============================================");
            
            try
            {
                await RunNameDatabaseDemo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running demos: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static async Task RunNameDatabaseDemo()
        {
            Console.WriteLine("\nName Database Search Demo");
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
            
            // Demo 1: Search for a specific name by value
            Console.WriteLine("\n1. Searching for a specific name (Grace)");
            string targetName = "Grace";
            
            Console.WriteLine($"Target: '{targetName}'");
            Console.WriteLine("Running quantum search to find this name...");
            
            var startTime = DateTime.Now;
            var result = await searcher.Search(nameDatabase, targetName);
            var duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            // Demo 2: Search for a name with specific criteria
            Console.WriteLine("\n2. Searching for a name starting with 'K'");
            
            // Create a predicate function: find names starting with K
            Func<string, bool> startsWithK = name => 
                name.StartsWith("K", StringComparison.OrdinalIgnoreCase);
            
            // Count matches for optimal iterations
            var matchingNames = nameDatabase.Where(name => 
                name.StartsWith("K", StringComparison.OrdinalIgnoreCase)).ToList();
            
            Console.WriteLine($"Expected matches: {string.Join(", ", matchingNames)}");
            Console.WriteLine("Running quantum search with predicate...");
            
            startTime = DateTime.Now;
            result = await searcher.SearchWhere(nameDatabase, startsWithK);
            duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            if (startsWithK(result.Item))
            {
                Console.WriteLine("Success! The result matches our criteria.");
            }
            else
            {
                Console.WriteLine("Warning: The result does not match our criteria.");
            }
            
            // Demo 3: Search with a more complex predicate
            Console.WriteLine("\n3. Searching for a name with 5+ characters and containing 'a'");
            
            Func<string, bool> complexPredicate = name => 
                name.Length >= 5 && name.Contains("a", StringComparison.OrdinalIgnoreCase);
            
            startTime = DateTime.Now;
            result = await searcher.SearchWhere(nameDatabase, complexPredicate);
            duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            if (complexPredicate(result.Item))
            {
                Console.WriteLine("Success! The result matches our complex criteria.");
            }
            else
            {
                Console.WriteLine("Warning: The result does not match our criteria.");
            }
            
            // Demo 4: Search in a non-power-of-2 database
            Console.WriteLine("\n4. Searching in a non-power-of-2 database");
            
            // Create a database with a non-power-of-2 size
            var nonPowerOfTwoDatabase = new List<string>(nameDatabase);
            nonPowerOfTwoDatabase.Add("Quinn");
            nonPowerOfTwoDatabase.Add("Rachel");
            nonPowerOfTwoDatabase.Add("Sam");
            
            Console.WriteLine($"Database size: {nonPowerOfTwoDatabase.Count} (not a power of 2)");
            
            targetName = "Quinn";
            startTime = DateTime.Now;
            result = await searcher.Search(nonPowerOfTwoDatabase, targetName);
            duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found '{result.Item}' at index {result.Index}");
            
            Console.WriteLine("\nDemos completed successfully!");
        }
    }
}