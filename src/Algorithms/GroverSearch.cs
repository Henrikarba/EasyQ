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
    /// Designed for regular developers with no quantum computing knowledge.
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
        /// Searches for an item in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The found item and its index.</returns>
        public async Task<(T Item, int Index)> Search<T>(IList<T> collection, T item)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            
            // Find the index of the item
            int targetIndex = -1;
            for (int i = 0; i < collection.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(collection[i], item))
                {
                    targetIndex = i;
                    break;
                }
            }
            
            if (targetIndex == -1)
                throw new InvalidOperationException($"Item not found in the collection");
            
            // Perform quantum search
            int foundIndex = (int)await SearchInternalAsync(collection.Count, targetIndex);
            
            return (collection[foundIndex], foundIndex);
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
            
            // Count matches for estimating optimal iterations
            int matchCount = 0;
            List<int> matchingIndices = new List<int>();
            
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    matchCount++;
                    matchingIndices.Add(i);
                }
            }
            
            if (matchCount == 0)
                throw new InvalidOperationException("No items match the predicate in the collection");
            
            // Convert the predicate to work with indices
            Func<long, bool> indexPredicate = index => 
                index >= 0 && 
                index < collection.Count && 
                predicate(collection[(int)index]);
            
            // Calculate required search space size (next power of 2)
            long searchSpace = GetNextPowerOfTwo(collection.Count);
            
            // Perform quantum search with the predicate
            int foundIndex = (int)await SearchWithPredicateInternalAsync(
                searchSpace, 
                indexPredicate, 
                matchCount, 
                maxAttempts
            );
            
            return (collection[foundIndex], foundIndex);
        }
        
        /// <summary>
        /// Internal method that handles the quantum search implementation.
        /// </summary>
        private async Task<long> SearchInternalAsync(long collectionSize, long targetIndex)
        {
            // Calculate search space size (next power of 2)
            long searchSpace = GetNextPowerOfTwo(collectionSize);
            
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Calculate optimal number of iterations
            long iterations = CalculateOptimalIterations.Run(_simulator, searchSpace, 1).Result;
            
            // Create oracle for the target item
            var oracle = CreateOracleForItem.Run(_simulator, numQubits, targetIndex).Result;
            
            // Run Grover's search algorithm
            var result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
            
            // If the result is outside the collection range, retry with a different approach
            if (result >= collectionSize)
            {
                // Use predicate-based search to stay within bounds
                Func<long, bool> exactIndexPredicate = index => index == targetIndex;
                return await SearchWithPredicateInternalAsync(searchSpace, exactIndexPredicate, 1, 3);
            }
            
            return result;
        }
        
        /// <summary>
        /// Internal method that handles the quantum search with a predicate.
        /// </summary>
        private async Task<long> SearchWithPredicateInternalAsync(
            long searchSpace, 
            Func<long, bool> predicate, 
            long estimatedMatches = 1, 
            int maxAttempts = 5)
        {
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Calculate optimal number of iterations
            long iterations = CalculateOptimalIterations.Run(_simulator, searchSpace, estimatedMatches).Result;
            
            // Make multiple attempts since Grover's algorithm is probabilistic
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // For each attempt, find a random target that satisfies the predicate
                long target = -1;
                for (long i = 0; i < searchSpace; i++)
                {
                    if (predicate(i))
                    {
                        target = i;
                        break;
                    }
                }
                
                if (target == -1)
                {
                    throw new ArgumentException("No items match the predicate in the search space");
                }
                
                // Create oracle for the target item
                var oracle = CreateOracleForItem.Run(_simulator, numQubits, target).Result;
                
                // Run Grover's search algorithm
                var result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
                
                // Verify the result matches our predicate
                if (predicate(result))
                {
                    return result;
                }
            }
            
            throw new InvalidOperationException($"Failed to find a matching item after {maxAttempts} attempts");
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