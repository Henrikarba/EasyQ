using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EasyQ.Bridge.Cryptography;

namespace EasyQTests.Cryptography
{
    public class RngTests : ITestRunner
    {
        public string Name => "Quantum Random Number Generator Tests";
        public string Description => "Tests for quantum random number generation";

        public async Task RunAsync()
        {
            // Run all RNG tests
            await BasicRandomGenerationTest();
            Console.WriteLine();
            await DistributionTest();
            Console.WriteLine();
            await BoundaryTest();
        }

        private async Task BasicRandomGenerationTest()
        {
            Console.WriteLine("\nBasic Random Number Generation Test");
            Console.WriteLine("-----------------------------------");
            
            // Create RNG with logging enabled for test visibility
            using var rng = new QuantumRng(enableLogging: true);
            
            Console.WriteLine("Generating a series of random numbers between 1 and 100:");
            
            // Generate and display a few random numbers
            for (int i = 0; i < 5; i++)
            {
                int value = await rng.RandomInt(1, 100);
                Console.WriteLine($"Random number {i+1}: {value}");
            }
        }
        
        private async Task DistributionTest()
        {
            Console.WriteLine("\nDistribution Test");
            Console.WriteLine("-----------------");
            
            // Create RNG without logging to speed up generation
            using var rng = new QuantumRng(enableLogging: false);
            
            // Number of samples to test
            int sampleSize = 100;
            int min = 1;
            int max = 6;
            
            Console.WriteLine($"Testing distribution of {sampleSize} random numbers from {min} to {max}");
            Console.WriteLine("(Simulating dice rolls)");
            
            // Create histogram of results
            var histogram = new Dictionary<int, int>();
            for (int i = min; i <= max; i++)
            {
                histogram[i] = 0;
            }
            
            // Generate random numbers and count occurrences
            for (int i = 0; i < sampleSize; i++)
            {
                int value = await rng.RandomInt(min, max);
                histogram[value]++;
            }
            
            // Display histogram
            Console.WriteLine("\nDistribution results:");
            Console.WriteLine("|-------|-----------|-----------|");
            Console.WriteLine("| Value | Count     | Frequency |");
            Console.WriteLine("|-------|-----------|-----------|");
            
            foreach (var pair in histogram.OrderBy(x => x.Key))
            {
                double frequency = (double)pair.Value / sampleSize;
                // Display with bar chart
                string bar = new string('#', (int)(frequency * 50));
                Console.WriteLine($"| {pair.Key,5} | {pair.Value,9} | {frequency:P1} {bar}");
            }
            
            Console.WriteLine("|-------|-----------|-----------|");
            
            // Calculate expected frequency for a fair distribution
            double expectedFrequency = 1.0 / (max - min + 1);
            Console.WriteLine($"Expected frequency for a fair distribution: {expectedFrequency:P1}");
            
            // Calculate chi-square statistic for simple fairness check
            double chiSquare = 0;
            int expectedCount = sampleSize / (max - min + 1);
            
            foreach (var count in histogram.Values)
            {
                chiSquare += Math.Pow(count - expectedCount, 2) / expectedCount;
            }
            
            Console.WriteLine($"Chi-square statistic: {chiSquare:F2}");
            Console.WriteLine("Note: For a true fair distribution, chi-square should be close to the degrees of freedom");
            Console.WriteLine($"Degrees of freedom: {max - min}");
        }
        
        private async Task BoundaryTest()
        {
            Console.WriteLine("\nBoundary Test");
            Console.WriteLine("-------------");
            
            using var rng = new QuantumRng(enableLogging: true);
            
            // Test minimum and maximum integer boundaries
            Console.WriteLine("Testing with extreme minimum and maximum values:");
            
            try
            {
                Console.WriteLine("Generating a random number between int.MinValue and int.MinValue+10:");
                int value = await rng.RandomInt(int.MinValue, int.MinValue + 10);
                Console.WriteLine($"Generated value: {value}");
                Console.WriteLine($"Value in expected range: {value >= int.MinValue && value <= int.MinValue + 10}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when testing minimum boundary: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("\nGenerating a random number between int.MaxValue-10 and int.MaxValue:");
                int value = await rng.RandomInt(int.MaxValue - 10, int.MaxValue);
                Console.WriteLine($"Generated value: {value}");
                Console.WriteLine($"Value in expected range: {value >= int.MaxValue - 10 && value <= int.MaxValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when testing maximum boundary: {ex.Message}");
            }
            
            // Test large range
            try
            {
                Console.WriteLine("\nGenerating a random number with a very large range (-1000000000 to 1000000000):");
                int value = await rng.RandomInt(-1000000000, 1000000000);
                Console.WriteLine($"Generated value: {value}");
                Console.WriteLine($"Value in expected range: {value >= -1000000000 && value <= 1000000000}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when testing large range: {ex.Message}");
            }
            
            // Test invalid input (min > max)
            try
            {
                Console.WriteLine("\nTesting invalid input (min > max):");
                await rng.RandomInt(100, 1);
                Console.WriteLine("Test failed: Expected an exception but none was thrown.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Correctly threw exception: {ex.Message}");
            }
        }
    }
}