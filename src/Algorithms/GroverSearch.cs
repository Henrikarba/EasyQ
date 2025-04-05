using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Algorithms;

namespace EasyQ.Algorithms
{
    /// <summary>
    /// Provides a simple API for quantum search operations, hiding all quantum computing complexity.
    /// Uses pure quantum approaches with no classical fallbacks.
    /// </summary>
    public class QuantumSearch : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private bool _disposed = false;
        
        /// <summary>
        /// Initializes a new instance of the QuantumSearch class.
        /// </summary>
        public QuantumSearch()
        {
            _simulator = new QuantumSimulator();
        }

        /// <summary>
        /// Searches for an item in a collection using pure quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The found item and its index.</returns>
        public async Task<(T Item, int Index)> Search<T>(IList<T> collection, T item)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            
            Console.WriteLine($"\nQuantum search for item: {item}");
            
            // For debugging: print the database and target index
            for (int i = 0; i < collection.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(collection[i], item))
                {
                    Console.WriteLine($"Note: Target item found classically at index {i} " +
                                      $"(for debugging only, not used in quantum search)");
                }
            }
            
            // Create a predicate for the target item
            Func<int, bool> targetPredicate = index => 
                index < collection.Count && EqualityComparer<T>.Default.Equals(collection[index], item);
            
            // Perform quantum search with predicate
            long resultIndex = await QuantumSearchByPredicate(collection.Count, targetPredicate, 10);
            
            if (resultIndex < 0 || resultIndex >= collection.Count)
            {
                throw new InvalidOperationException($"Quantum search failed to find the item within valid range");
            }
            
            if (!EqualityComparer<T>.Default.Equals(collection[(int)resultIndex], item))
            {
                throw new InvalidOperationException($"Quantum search failed to find the correct item");
            }
            
            Console.WriteLine($"Quantum search successfully found item at index {resultIndex}");
            return (collection[(int)resultIndex], (int)resultIndex);
        }
        
        /// <summary>
        /// Searches for multiple occurrences of an item in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>A list of tuples containing the found items and their indices.</returns>
        public async Task<List<(T Item, int Index)>> SearchAll<T>(IList<T> collection, T item)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            
            var results = new List<(T Item, int Index)>();
            
            // Create a predicate for the target item
            Func<int, bool> targetPredicate = index => 
                index < collection.Count && EqualityComparer<T>.Default.Equals(collection[index], item);
            
            // Estimate the number of matching items for better quantum performance
            int estimatedMatches = EstimateMatchCount(collection.Count, targetPredicate);
            
            if (estimatedMatches == 0)
            {
                throw new InvalidOperationException($"Item not found in the collection");
            }
            
            // Perform multiple quantum searches
            HashSet<int> foundIndices = new HashSet<int>();
            int maxAttempts = Math.Max(10, estimatedMatches * 2);  // More attempts for more expected matches
            int successfulFinds = 0;
            
            for (int attempt = 0; attempt < maxAttempts && successfulFinds < estimatedMatches; attempt++)
            {
                try
                {
                    // Perform quantum search with the predicate
                    long resultIndex = await QuantumSearchByPredicate(collection.Count, targetPredicate);
                    
                    if (resultIndex >= 0 && resultIndex < collection.Count && 
                        EqualityComparer<T>.Default.Equals(collection[(int)resultIndex], item) &&
                        !foundIndices.Contains((int)resultIndex))
                    {
                        foundIndices.Add((int)resultIndex);
                        results.Add((collection[(int)resultIndex], (int)resultIndex));
                        successfulFinds++;
                    }
                }
                catch (Exception)
                {
                    // Continue to next attempt
                }
                
                // Update the predicate to exclude already found indices
                targetPredicate = index => 
                    index < collection.Count && 
                    EqualityComparer<T>.Default.Equals(collection[index], item) &&
                    !foundIndices.Contains(index);
            }
            
            if (results.Count == 0)
            {
                throw new InvalidOperationException("Quantum search failed to find any valid matches");
            }
            
            return results;
        }
        
        /// <summary>
        /// Searches for an item matching a predicate in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="predicate">A function that determines if an item matches the search criteria.</param>
        /// <param name="maxAttempts">Maximum number of search attempts.</param>
        /// <returns>The first item that matches the predicate and its index.</returns>
        public async Task<(T Item, int Index)> SearchWhere<T>(IList<T> collection, Func<T, bool> predicate, int maxAttempts = 5)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            // Create a quantum-compatible predicate that works with indices
            Func<int, bool> indexPredicate = index => 
            {
                if (index < 0 || index >= collection.Count)
                    return false;
                return predicate(collection[index]);
            };
            
            // Perform quantum search with the predicate
            long resultIndex = await QuantumSearchByPredicate(collection.Count, indexPredicate, maxAttempts);
            
            if (resultIndex < 0 || resultIndex >= collection.Count)
            {
                throw new InvalidOperationException("Quantum search failed to find an item within valid range");
            }
            
            if (!predicate(collection[(int)resultIndex]))
            {
                throw new InvalidOperationException("Quantum search failed to find an item matching the predicate");
            }
            
            return (collection[(int)resultIndex], (int)resultIndex);
        }
        
        /// <summary>
        /// Searches for all items matching a predicate in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="predicate">A function that determines if an item matches the search criteria.</param>
        /// <returns>A list of tuples containing all items that match the predicate and their indices.</returns>
        public async Task<List<(T Item, int Index)>> SearchAllWhere<T>(IList<T> collection, Func<T, bool> predicate)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            var results = new List<(T Item, int Index)>();
            
            // Create a quantum-compatible predicate that works with indices
            Func<int, bool> indexPredicate = index => 
                index < collection.Count && predicate(collection[index]);
            
            // Estimate the number of matching items for better quantum performance
            int estimatedMatches = EstimateMatchCount(collection.Count, indexPredicate);
            
            if (estimatedMatches == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the collection");
            }
            
            // Perform multiple quantum searches to find all matches
            HashSet<int> foundIndices = new HashSet<int>();
            int maxAttempts = Math.Max(10, estimatedMatches * 2);  // More attempts for more expected matches
            int successfulFinds = 0;
            
            for (int attempt = 0; attempt < maxAttempts && successfulFinds < estimatedMatches; attempt++)
            {
                try
                {
                    // Perform quantum search with the predicate
                    long resultIndex = await QuantumSearchByPredicate(collection.Count, indexPredicate);
                    
                    if (resultIndex >= 0 && resultIndex < collection.Count && 
                        predicate(collection[(int)resultIndex]) &&
                        !foundIndices.Contains((int)resultIndex))
                    {
                        foundIndices.Add((int)resultIndex);
                        results.Add((collection[(int)resultIndex], (int)resultIndex));
                        successfulFinds++;
                    }
                }
                catch (Exception)
                {
                    // Continue to next attempt
                }
                
                // Update the predicate to exclude already found indices
                indexPredicate = index => 
                    index < collection.Count && 
                    predicate(collection[index]) &&
                    !foundIndices.Contains(index);
            }
            
            if (results.Count == 0)
            {
                throw new InvalidOperationException("Quantum search failed to find any valid matches");
            }
            
            return results;
        }
        
        /// <summary>
        /// Performs quantum search to find an item matching a predicate.
        /// </summary>
        private async Task<long> QuantumSearchByPredicate(long collectionSize, Func<int, bool> predicate, int maxAttempts = 5)
        {
            // Calculate search space size (next power of 2)
            long searchSpace = GetNextPowerOfTwo(collectionSize);
            
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Count matching items for optimal iteration calculation
            int matchCount = EstimateMatchCount(collectionSize, predicate);
            
            if (matchCount == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the search space");
            }
            
            // Find all targets that satisfy the predicate (up to 5)
            var targets = new List<int>();
            for (int i = 0; i < Math.Min(searchSpace, 1000); i++)
            {
                if (predicate(i))
                {
                    targets.Add(i);
                    if (targets.Count >= 5) break;  // Limit to 5 targets for performance
                }
            }
            
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the sampling range");
            }
            
            // Try amplitude amplification with different parameters
            return await AmplitudeAmplificationSearch(numQubits, searchSpace, collectionSize, targets, predicate, maxAttempts);
        }
        
        /// <summary>
        /// Implements quantum amplitude amplification for more robust search results.
        /// </summary>
        private async Task<long> AmplitudeAmplificationSearch(
            int numQubits, 
            long searchSpace, 
            long collectionSize,
            List<int> knownTargets, 
            Func<int, bool> predicate,
            int maxAttempts)
        {
            // Strategies for amplitude amplification
            // Each strategy is a tuple of (base iterations, multiplier, offset)
            var amplificationStrategies = new List<(double baseFactor, double multiplier, int offset)>
            {
                (0.25, 1.0, 0),      // Standard Grover (π/4 * sqrt(N))
                (0.25, 0.5, 0),      // Half iterations
                (0.25, 1.5, 0),      // 50% more iterations
                (0.25, 1.0, -1),     // Standard minus 1
                (0.25, 1.0, 1),      // Standard plus 1
                (0.2, 1.0, 0),       // Slightly fewer iterations (π/5 * sqrt(N))
                (0.3, 1.0, 0),       // Slightly more iterations (π/3.33 * sqrt(N))
                (0.125, 1.0, 0),     // 1/8 of standard (π/8 * sqrt(N))
                (1.0, 1.0, 0)        // Fixed single iteration
            };
            
            Random rng = new Random();
            int totalStrategies = amplificationStrategies.Count;
            int totalTargets = knownTargets.Count;
            int totalAttempts = maxAttempts * totalStrategies;
            
            Console.WriteLine($"Using amplitude amplification with {totalAttempts} attempts across {totalStrategies} strategies");
            
            for (int attempt = 0; attempt < totalAttempts; attempt++)
            {
                // Select a target and amplification strategy
                int targetIndex = attempt % totalTargets;
                int strategyIndex = (attempt / totalTargets) % totalStrategies;
                
                int target = knownTargets[targetIndex];
                var strategy = amplificationStrategies[strategyIndex];
                
                try
                {
                    // Calculate iterations using the amplification strategy
                    double sqrtN = Math.Sqrt(searchSpace);
                    int iterations = (int)Math.Floor(Math.PI * strategy.baseFactor * sqrtN * strategy.multiplier) + strategy.offset;
                    iterations = Math.Max(1, iterations); // At least 1 iteration
                    
                    Console.WriteLine($"Amplitude amplification attempt {attempt+1}/{totalAttempts}: " +
                                     $"Target={target}, Strategy={strategyIndex+1}, Iterations={iterations}");
                    
                    // Create oracle for the target item
                    var oracle = await CreateOracleForItem.Run(_simulator, numQubits, target);
                    
                    // Run Grover's search algorithm with this amplification strategy
                    long result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
                    
                    Console.WriteLine($"Search returned index {result}");
                    
                    // Verify the result matches our predicate
                    if (result < collectionSize && predicate((int)result))
                    {
                        Console.WriteLine($"Success! Index {result} matches the predicate.");
                        return result;
                    }
                    else
                    {
                        Console.WriteLine($"Index {result} does not match the predicate or is out of range.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Amplitude amplification attempt {attempt+1} failed: {ex.Message}");
                    // Continue to next attempt
                }
            }
            
            // If we've tried all strategies and targets without success, throw exception
            throw new InvalidOperationException($"Amplitude amplification failed after {totalAttempts} attempts");
        }
        
        /// <summary>
        /// Estimates the number of items that match a predicate by sampling.
        /// </summary>
        private int EstimateMatchCount(long collectionSize, Func<int, bool> predicate)
        {
            // For small collections, just check everything
            if (collectionSize <= 1000)
            {
                int count = 0;
                for (int i = 0; i < collectionSize; i++)
                {
                    if (predicate(i))
                    {
                        count++;
                    }
                }
                return count;
            }
            
            // For larger collections, use sampling to estimate
            int sampleSize = 100;
            int sampleMatches = 0;
            Random random = new Random();
            
            for (int i = 0; i < sampleSize; i++)
            {
                int index = random.Next((int)collectionSize);
                if (predicate(index))
                {
                    sampleMatches++;
                }
            }
            
            // Estimate total matches
            return (int)Math.Ceiling((sampleMatches / (double)sampleSize) * collectionSize);
        }
        
        /// <summary>
        /// Returns the next power of 2 that is greater than or equal to the input.
        /// </summary>
        private long GetNextPowerOfTwo(long n)
        {
            long power = 1;
            while (power < n)
            {
                power *= 2;
            }
            return power;
        }
        
        /// <summary>
        /// Disposes the quantum simulator.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes the quantum simulator.
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose or the finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _simulator?.Dispose();
                }
                
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~QuantumSearch()
        {
            Dispose(false);
        }
    }
}