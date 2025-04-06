using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQTests.Cryptography;
using EasyQTests.Search;

namespace EasyQTests
{
    // Interface for test runners
    public interface ITestRunner
    {
        string Name { get; }
        string Description { get; }
        Task RunAsync();
    }

    // Main program with simple menu
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("EasyQ Framework Test Suite");
            Console.WriteLine("==========================");

            // Create a list of available test runners
            var testRunners = new List<ITestRunner>
            {
                new SearchTests(),
                new KeyDistributionTests(),
                // Add more test runners here as needed
            };

            while (true)
            {
                // Display menu
                Console.WriteLine("\nAvailable Tests:");
                for (int i = 0; i < testRunners.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {testRunners[i].Name} - {testRunners[i].Description}");
                }
                Console.WriteLine("0. Exit");

                // Get user selection
                Console.Write("\nSelect a test to run (0-{0}): ", testRunners.Count);
                if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 0 || selection > testRunners.Count)
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                    continue;
                }

                // Exit if selected
                if (selection == 0)
                {
                    break;
                }

                // Run selected test
                try
                {
                    Console.Clear();
                    Console.WriteLine($"Running: {testRunners[selection - 1].Name}");
                    Console.WriteLine(new string('-', 40));
                    
                    await testRunners[selection - 1].RunAsync();
                    
                    Console.WriteLine(new string('-', 40));
                    Console.WriteLine("Test completed. Press any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running test: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }
    }
}