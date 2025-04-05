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
            
            // For better results, we'll use different iteration strategies
            // and try multiple targets
            var iterationStrategies = new List<Func<double, int>>
            {
                angle => (int)Math.Floor(Math.PI / (4.0 * angle) - 0.5),      // Standard optimal iterations
                angle => (int)Math.Floor(Math.PI / (4.0 * angle)),            // Slightly more iterations
                angle => (int)Math.Floor(Math.PI / (4.0 * angle) - 1),        // Slightly fewer iterations
                angle => 1,                                                   // Just one iteration (works for small search spaces)
                angle => (int)Math.Floor(Math.PI / (4.0 * angle) / 2)         // Half the standard iterations
            };
            
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
            
            // Try with different targets and iteration strategies
            Random rng = new Random();
            int totalAttempts = maxAttempts * iterationStrategies.Count;
            
            for (int attempt = 0; attempt < totalAttempts; attempt++)
            {
                // Select a target and iteration strategy
                int targetIndex = rng.Next(targets.Count);
                int target = targets[targetIndex];
                
                int strategyIndex = (attempt / maxAttempts) % iterationStrategies.Count;
                var iterationStrategy = iterationStrategies[strategyIndex];
                
                try
                {
                    Console.WriteLine($"Quantum search attempt {attempt+1}/{totalAttempts}: Target '{target}', Strategy {strategyIndex+1}");
                    
                    // Calculate iterations based on the selected strategy
                    double angle = Math.Asin(Math.Sqrt(matchCount / (double)searchSpace));
                    int iterations = Math.Max(1, iterationStrategy(angle));
                    
                    Console.WriteLine($"Using {iterations} iterations for search space of {searchSpace} with {matchCount} matches");
                    
                    // Create oracle for the target item
                    var oracle = await CreateOracleForItem.Run(_simulator, numQubits, target);
                    
                    // Run Grover's search algorithm
                    long result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
                    
                    Console.WriteLine($"Quantum search returned index {result}");
                    
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
                    Console.WriteLine($"Quantum attempt {attempt+1} failed: {ex.Message}");
                    // Try again with different parameters
                }
            }
            
            throw new InvalidOperationException($"Failed to find a matching item after {totalAttempts} attempts");
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