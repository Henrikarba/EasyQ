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
            
            using var searcher = new GroverSearchBridge();
            
            // Demo 1: Search for a specific name by index
            Console.WriteLine("\n1. Searching for a specific name (Grace)");
            string targetName = "Grace";
            long targetIndex = nameDatabase.IndexOf(targetName);
            
            Console.WriteLine($"Target: '{targetName}' is at index {targetIndex}");
            Console.WriteLine("Running quantum search to find this index...");
            
            var startTime = DateTime.Now;
            var result = await searcher.SearchAsync(nameDatabase.Count, targetIndex);
            var duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found index: {result}, Name: {nameDatabase[(int)result]}");
            
            // Demo 2: Search for a name with specific criteria
            Console.WriteLine("\n2. Searching for a name starting with 'K'");
            
            // Create a predicate function: find names starting with K
            Func<long, bool> startsWithK = index => 
                index >= 0 && 
                index < nameDatabase.Count && 
                nameDatabase[(int)index].StartsWith("K", StringComparison.OrdinalIgnoreCase);
            
            // Count matches for optimal iterations
            var matchingNames = nameDatabase.Where(name => 
                name.StartsWith("K", StringComparison.OrdinalIgnoreCase)).ToList();
            
            Console.WriteLine($"Expected matches: {string.Join(", ", matchingNames)}");
            Console.WriteLine("Running quantum search with predicate...");
            
            startTime = DateTime.Now;
            result = await searcher.SearchWithPredicateAsync(
                nameDatabase.Count, 
                startsWithK,
                estimatedMatches: matchingNames.Count,
                maxAttempts: 5
            );
            duration = DateTime.Now - startTime;
            
            Console.WriteLine($"Search completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found index: {result}, Name: {nameDatabase[(int)result]}");
            
            if (startsWithK(result))
            {
                Console.WriteLine("Success! The result matches our criteria.");
            }
            else
            {
                Console.WriteLine("Warning: The result does not match our criteria.");
            }
            
            // Demo 3: Compare with classical search
            Console.WriteLine("\n3. Classical vs Quantum Search Comparison");
            
            // Classical search (linear)
            Console.WriteLine("Running classical linear search...");
            startTime = DateTime.Now;
            
            long classicalResult = -1;
            for (int i = 0; i < nameDatabase.Count; i++)
            {
                if (nameDatabase[i] == targetName)
                {
                    classicalResult = i;
                    break;
                }
            }
            
            var classicalDuration = DateTime.Now - startTime;
            Console.WriteLine($"Classical search completed in {classicalDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found index: {classicalResult}");
            
            // Quantum search
            Console.WriteLine("Running quantum search...");
            startTime = DateTime.Now;
            result = await searcher.SearchAsync(nameDatabase.Count, targetIndex);
            var quantumDuration = DateTime.Now - startTime;
            Console.WriteLine($"Quantum search completed in {quantumDuration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"Found index: {result}");
            
            Console.WriteLine("\nNote: For small datasets, classical search might be faster due to quantum simulation overhead.");
            Console.WriteLine("For large datasets, quantum search would show a theoretical quadratic speedup.");
        }
    }
}