using System;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Algorithms;

namespace EasyQ.Algorithms
{
    /// <summary>
    /// Provides a bridge between C# and the Q# implementation of Grover's search algorithm.
    /// Allows regular developers to use quantum search without understanding quantum operations.
    /// </summary>
    public class GroverSearchBridge
    {
        private readonly QuantumSimulator _simulator;
        
        /// <summary>
        /// Initializes a new instance of the GroverSearchBridge class.
        /// </summary>
        public GroverSearchBridge()
        {
            _simulator = new QuantumSimulator();
        }

        /// <summary>
        /// Searches for a specific item in an unstructured database using Grover's algorithm.
        /// </summary>
        /// <param name="searchSpace">The size of the search space (must be a power of 2)</param>
        /// <param name="targetItem">The target item to find (must be less than searchSpace)</param>
        /// <param name="iterations">Optional: Number of Grover iterations to perform. If not specified, uses the optimal number.</param>
        /// <returns>The index of the found item</returns>
        public async Task<long> SearchAsync(long searchSpace, long targetItem, long? iterations = null)
        {
            ValidateInputs(searchSpace, targetItem);
            
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Calculate optimal number of iterations if not specified
            long iterationsToUse = iterations ?? GetOptimalIterationCount(searchSpace, 1);
            
            // Create oracle for the target item
            var oracle = CreateOracleForItem.Run(_simulator, numQubits, targetItem).Result;
            
            // Run Grover's search algorithm
            var result = await GroverSearch.Run(_simulator, numQubits, oracle, iterationsToUse);
            
            return result;
        }
        
        /// <summary>
        /// Searches for items matching a predicate in an unstructured database using Grover's algorithm.
        /// </summary>
        /// <param name="searchSpace">The size of the search space (must be a power of 2)</param>
        /// <param name="predicate">A function that determines if an item matches the search criteria</param>
        /// <param name="estimatedMatches">Estimated number of items that match the predicate</param>
        /// <param name="maxAttempts">Maximum number of search attempts</param>
        /// <returns>An item that matches the predicate</returns>
        public async Task<long> SearchWithPredicateAsync(long searchSpace, Func<long, bool> predicate, long estimatedMatches = 1, int maxAttempts = 10)
        {
            ValidateInputs(searchSpace, 0);
            
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Calculate optimal number of iterations
            long iterations = GetOptimalIterationCount(searchSpace, estimatedMatches);
            
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
        /// Calculates the optimal number of Grover iterations for a given problem size.
        /// </summary>
        private long GetOptimalIterationCount(long searchSpace, long estimatedMatches)
        {
            return CalculateOptimalIterations.Run(_simulator, searchSpace, estimatedMatches).Result;
        }
        
        /// <summary>
        /// Validates the inputs for the search algorithm.
        /// </summary>
        private void ValidateInputs(long searchSpace, long targetItem)
        {
            if (searchSpace <= 0)
            {
                throw new ArgumentException("Search space must be positive", nameof(searchSpace));
            }
            
            if (!IsPowerOfTwo(searchSpace))
            {
                throw new ArgumentException("Search space must be a power of 2", nameof(searchSpace));
            }
            
            if (targetItem < 0 || targetItem >= searchSpace)
            {
                throw new ArgumentException($"Target item must be between 0 and {searchSpace - 1}", nameof(targetItem));
            }
        }
        
        /// <summary>
        /// Checks if a number is a power of 2.
        /// </summary>
        private bool IsPowerOfTwo(long x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }
        
        /// <summary>
        /// Disposes the quantum simulator.
        /// </summary>
        public void Dispose()
        {
            _simulator?.Dispose();
        }
    }
}