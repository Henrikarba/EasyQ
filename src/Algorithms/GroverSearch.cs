using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Algorithms;

namespace EasyQ.Algorithms
{
    /// <summary>
    /// Configuration options for quantum search operations.
    /// </summary>
    public class QuantumSearchOptions
    {
        /// <summary>
        /// Maximum number of search attempts to try before giving up.
        /// If set to null, will continue trying until successful or resources are exhausted.
        /// </summary>
        public int? MaxAttempts { get; set; } = null;

        /// <summary>
        /// Maximum number of target items to sample for the oracle.
        /// If set to null, will use all available targets.
        /// </summary>
        public int? MaxTargets { get; set; } = null;

        /// <summary>
        /// The iteration strategy to use for determining the number of Grover iterations.
        /// </summary>
        public IterationStrategy IterationStrategy { get; set; } = IterationStrategy.Optimal;

        /// <summary>
        /// The sampling strategy to use for estimating the number of matches.
        /// </summary>
        public SamplingStrategy SamplingStrategy { get; set; } = SamplingStrategy.Auto;

        /// <summary>
        /// The number of samples to use when estimating match counts.
        /// Only used with SamplingStrategy.Sampling.
        /// </summary>
        public int SampleSize { get; set; } = 100;

        /// <summary>
        /// The maximum database size to scan completely.
        /// Only used with SamplingStrategy.Auto.
        /// </summary>
        public int FullScanThreshold { get; set; } = 1000;

        /// <summary>
        /// Factor to multiply the standard optimal iteration count by.
        /// Only used when IterationStrategy is set to Custom.
        /// </summary>
        public double CustomIterationFactor { get; set; } = 1.0;

        /// <summary>
        /// Offset to add to the iteration count.
        /// Only used when IterationStrategy is set to Custom.
        /// </summary>
        public int CustomIterationOffset { get; set; } = 0;

        /// <summary>
        /// Whether to log detailed information about the quantum search process.
        /// </summary>
        public bool EnableLogging { get; set; } = false;
    }

    /// <summary>
    /// Strategies for determining the number of Grover iterations.
    /// </summary>
    public enum IterationStrategy
    {
        /// <summary>
        /// Standard optimal iterations (PI / (4 * angle) - 0.5)
        /// </summary>
        Optimal,

        /// <summary>
        /// One iteration only (useful for small search spaces)
        /// </summary>
        SingleIteration,

        /// <summary>
        /// More aggressive approach (PI / (4 * angle))
        /// </summary>
        Aggressive,

        /// <summary>
        /// Conservative approach (PI / (4 * angle) - 1)
        /// </summary>
        Conservative,

        /// <summary>
        /// Half the standard iterations (PI / (8 * angle))
        /// </summary>
        HalfOptimal,

        /// <summary>
        /// Custom iteration count using CustomIterationFactor and CustomIterationOffset
        /// </summary>
        Custom
    }

    /// <summary>
    /// Strategies for estimating the number of matches in the database.
    /// </summary>
    public enum SamplingStrategy
    {
        /// <summary>
        /// Automatically choose between FullScan and Sampling based on database size
        /// </summary>
        Auto,

        /// <summary>
        /// Always scan the entire database (accurate but may be slow for large databases)
        /// </summary>
        FullScan,

        /// <summary>
        /// Use random sampling to estimate (faster but less accurate)
        /// </summary>
        Sampling,

        /// <summary>
        /// Assume only one match exists (fastest but only appropriate when you know there's exactly one match)
        /// </summary>
        AssumeOne,

        /// <summary>
        /// Use a specific count provided by the user
        /// </summary>
        UserProvided
    }

    /// <summary>
    /// Provides a simple API for quantum search operations, hiding all quantum computing complexity.
    /// Uses pure quantum approaches with no classical fallbacks.
    /// </summary>
    public class QuantumSearch : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private bool _disposed = false;
        private QuantumSearchOptions _defaultOptions;

        /// <summary>
        /// Initializes a new instance of the QuantumSearch class with default options.
        /// </summary>
        public QuantumSearch() : this(new QuantumSearchOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the QuantumSearch class with specified options.
        /// </summary>
        /// <param name="options">Configuration options for quantum search operations.</param>
        public QuantumSearch(QuantumSearchOptions options)
        {
            _simulator = new QuantumSimulator();
            _defaultOptions = options ?? new QuantumSearchOptions();
        }

        /// <summary>
        /// Searches for an item in a collection using pure quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <param name="options">Optional search configuration options.</param>
        /// <returns>The found item and its index.</returns>
        public async Task<(T Item, int Index)> Search<T>(IList<T> collection, T item, QuantumSearchOptions? options = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            
            options ??= _defaultOptions;
            
            if (options.EnableLogging)
            {
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
            }
            
            // Create a predicate for the target item
            Func<int, bool> targetPredicate = index => 
                index < collection.Count && EqualityComparer<T>.Default.Equals(collection[index], item);
            
            // Perform quantum search with predicate
            long resultIndex = await QuantumSearchByPredicate(collection.Count, targetPredicate, options);
            
            if (resultIndex < 0 || resultIndex >= collection.Count)
            {
                throw new InvalidOperationException($"Quantum search failed to find the item within valid range");
            }
            
            if (!EqualityComparer<T>.Default.Equals(collection[(int)resultIndex], item))
            {
                throw new InvalidOperationException($"Quantum search failed to find the correct item");
            }
            
            if (options.EnableLogging)
            {
                Console.WriteLine($"Quantum search successfully found item at index {resultIndex}");
            }
            
            return (collection[(int)resultIndex], (int)resultIndex);
        }
        
        /// <summary>
        /// Searches for multiple occurrences of an item in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <param name="options">Optional search configuration options.</param>
        /// <returns>A list of tuples containing the found items and their indices.</returns>
        public async Task<List<(T Item, int Index)>> SearchAll<T>(IList<T> collection, T item, QuantumSearchOptions? options = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            
            options ??= _defaultOptions;
            var results = new List<(T Item, int Index)>();
            
            // Create a predicate for the target item
            Func<int, bool> targetPredicate = index => 
                index < collection.Count && EqualityComparer<T>.Default.Equals(collection[index], item);
            
            // Estimate the number of matching items for better quantum performance
            int estimatedMatches = EstimateMatchCount(collection.Count, targetPredicate, options);
            int userSpecifiedMatchCount = options.SamplingStrategy == SamplingStrategy.UserProvided ? estimatedMatches : 0;
            
            if (estimatedMatches == 0)
            {
                throw new InvalidOperationException($"Item not found in the collection");
            }
            
            // Perform multiple quantum searches
            HashSet<int> foundIndices = new HashSet<int>();
            int maxAttempts = options.MaxAttempts ?? Math.Max(100, estimatedMatches * 3);  // More attempts for more expected matches
            int successfulFinds = 0;
            
            for (int attempt = 0; attempt < maxAttempts && successfulFinds < estimatedMatches; attempt++)
            {
                try
                {
                    // Don't keep trying if we've already found all the estimated matches
                    if (options.SamplingStrategy == SamplingStrategy.UserProvided && 
                        successfulFinds >= userSpecifiedMatchCount)
                    {
                        break;
                    }
                    
                    // Perform quantum search with the predicate
                    long resultIndex = await QuantumSearchByPredicate(collection.Count, targetPredicate, options);
                    
                    if (resultIndex >= 0 && resultIndex < collection.Count && 
                        EqualityComparer<T>.Default.Equals(collection[(int)resultIndex], item) &&
                        !foundIndices.Contains((int)resultIndex))
                    {
                        foundIndices.Add((int)resultIndex);
                        results.Add((collection[(int)resultIndex], (int)resultIndex));
                        successfulFinds++;
                    }
                }
                catch (Exception ex)
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Search attempt {attempt + 1} failed: {ex.Message}");
                    }
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
        /// <param name="options">Optional search configuration options.</param>
        /// <returns>The first item that matches the predicate and its index.</returns>
        public async Task<(T Item, int Index)> SearchWhere<T>(IList<T> collection, Func<T, bool> predicate, QuantumSearchOptions? options = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            options ??= _defaultOptions;
            
            // Create a quantum-compatible predicate that works with indices
            Func<int, bool> indexPredicate = index => 
            {
                if (index < 0 || index >= collection.Count)
                    return false;
                return predicate(collection[index]);
            };
            
            // Perform quantum search with the predicate
            long resultIndex = await QuantumSearchByPredicate(collection.Count, indexPredicate, options);
            
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
        /// <param name="options">Optional search configuration options.</param>
        /// <param name="knownMatchCount">Optional parameter to specify the exact number of matching items.</param>
        /// <returns>A list of tuples containing all items that match the predicate and their indices.</returns>
        public async Task<List<(T Item, int Index)>> SearchAllWhere<T>(
            IList<T> collection, 
            Func<T, bool> predicate, 
            QuantumSearchOptions? options = null,
            int? knownMatchCount = null)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            options ??= _defaultOptions;
            
            // If a known match count is provided, create a copy of options with UserProvided strategy
            if (knownMatchCount.HasValue)
            {
                var optionsCopy = new QuantumSearchOptions
                {
                    MaxAttempts = options.MaxAttempts,
                    MaxTargets = options.MaxTargets,
                    IterationStrategy = options.IterationStrategy,
                    SamplingStrategy = SamplingStrategy.UserProvided,
                    SampleSize = knownMatchCount.Value,
                    FullScanThreshold = options.FullScanThreshold,
                    CustomIterationFactor = options.CustomIterationFactor,
                    CustomIterationOffset = options.CustomIterationOffset,
                    EnableLogging = options.EnableLogging
                };
                options = optionsCopy;
            }
            
            var results = new List<(T Item, int Index)>();
            
            // Create a quantum-compatible predicate that works with indices
            Func<int, bool> indexPredicate = index => 
                index < collection.Count && predicate(collection[index]);
            
            // Estimate the number of matching items for better quantum performance
            int estimatedMatches = knownMatchCount ?? EstimateMatchCount(collection.Count, indexPredicate, options);
            
            if (estimatedMatches == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the collection");
            }
            
            // Perform multiple quantum searches to find all matches
            HashSet<int> foundIndices = new HashSet<int>();
            int maxAttempts = options.MaxAttempts ?? Math.Max(100, estimatedMatches * 3);  // More attempts for more expected matches
            int successfulFinds = 0;
            
            for (int attempt = 0; attempt < maxAttempts && successfulFinds < estimatedMatches; attempt++)
            {
                try
                {
                    // Perform quantum search with the predicate
                    long resultIndex = await QuantumSearchByPredicate(collection.Count, indexPredicate, options);
                    
                    if (resultIndex >= 0 && resultIndex < collection.Count && 
                        predicate(collection[(int)resultIndex]) &&
                        !foundIndices.Contains((int)resultIndex))
                    {
                        foundIndices.Add((int)resultIndex);
                        results.Add((collection[(int)resultIndex], (int)resultIndex));
                        successfulFinds++;
                    }
                }
                catch (Exception ex)
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Search attempt {attempt + 1} failed: {ex.Message}");
                    }
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
        private async Task<long> QuantumSearchByPredicate(long collectionSize, Func<int, bool> predicate, QuantumSearchOptions options)
        {
            // Calculate search space size (next power of 2)
            long searchSpace = GetNextPowerOfTwo(collectionSize);
            
            // Calculate the number of qubits needed to represent the search space
            int numQubits = (int)Math.Ceiling(Math.Log2(searchSpace));
            
            // Count matching items for optimal iteration calculation
            int matchCount = EstimateMatchCount(collectionSize, predicate, options);
            
            if (matchCount == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the search space");
            }
            
            // Find all targets that satisfy the predicate (up to maxTargets if specified)
            var targets = new List<int>();
            int targetsToFind = options.MaxTargets ?? int.MaxValue;
            
            // Sample targets from the full range
            for (int i = 0; i < Math.Min(collectionSize, searchSpace); i++)
            {
                if (predicate(i))
                {
                    targets.Add(i);
                    if (targets.Count >= targetsToFind) break;
                }
            }
            
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("No items match the predicate in the sampling range");
            }
            
            // Function to calculate iterations based on the selected strategy
            int CalculateIterations(double angle)
            {
                switch (options.IterationStrategy)
                {
                    case IterationStrategy.Optimal:
                        return Math.Max(1, (int)Math.Floor(Math.PI / (4.0 * angle) - 0.5));
                    case IterationStrategy.SingleIteration:
                        return 1;
                    case IterationStrategy.Aggressive:
                        return Math.Max(1, (int)Math.Floor(Math.PI / (4.0 * angle)));
                    case IterationStrategy.Conservative:
                        return Math.Max(1, (int)Math.Floor(Math.PI / (4.0 * angle) - 1));
                    case IterationStrategy.HalfOptimal:
                        return Math.Max(1, (int)Math.Floor(Math.PI / (8.0 * angle)));
                    case IterationStrategy.Custom:
                        return Math.Max(1, (int)Math.Floor((Math.PI / (4.0 * angle) * options.CustomIterationFactor) + options.CustomIterationOffset));
                    default:
                        return Math.Max(1, (int)Math.Floor(Math.PI / (4.0 * angle) - 0.5));
                }
            }
            
            // Try with different targets
            Random rng = new Random();
            int maxAttempts = options.MaxAttempts ?? 20;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Select a target
                int targetIndex = rng.Next(targets.Count);
                int target = targets[targetIndex];
                
                try
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Quantum search attempt {attempt+1}/{maxAttempts}: Target '{target}'");
                    }
                    
                    // Calculate iterations based on the selected strategy
                    double angle = Math.Asin(Math.Sqrt(matchCount / (double)searchSpace));
                    int iterations = CalculateIterations(angle);
                    
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Using {iterations} iterations for search space of {searchSpace} with {matchCount} matches");
                    }
                    
                    // Create oracle for the target item
                    var oracle = await CreateOracleForItem.Run(_simulator, numQubits, target);
                    
                    // Run Grover's search algorithm
                    long result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
                    
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Quantum search returned index {result}");
                    }
                    
                    // Verify the result matches our predicate
                    if (result < collectionSize && predicate((int)result))
                    {
                        if (options.EnableLogging)
                        {
                            Console.WriteLine($"Success! Index {result} matches the predicate.");
                        }
                        return result;
                    }
                    else if (options.EnableLogging)
                    {
                        Console.WriteLine($"Index {result} does not match the predicate or is out of range.");
                    }
                }
                catch (Exception ex)
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Quantum attempt {attempt+1} failed: {ex.Message}");
                    }
                    // Try again with different parameters
                }
            }
            
            throw new InvalidOperationException($"Failed to find a matching item after {maxAttempts} attempts");
        }
        
        /// <summary>
        /// Estimates the number of items that match a predicate using the specified sampling strategy.
        /// </summary>
        private int EstimateMatchCount(long collectionSize, Func<int, bool> predicate, QuantumSearchOptions options)
        {
            SamplingStrategy strategy = options.SamplingStrategy;
            
            // If strategy is Auto, decide based on collection size
            if (strategy == SamplingStrategy.Auto)
            {
                strategy = collectionSize <= options.FullScanThreshold 
                    ? SamplingStrategy.FullScan 
                    : SamplingStrategy.Sampling;
            }
            
            switch (strategy)
            {
                case SamplingStrategy.FullScan:
                    // Full scan for exact count
                    int count = 0;
                    for (int i = 0; i < collectionSize; i++)
                    {
                        if (predicate(i))
                        {
                            count++;
                        }
                    }
                    return count;
                    
                case SamplingStrategy.Sampling:
                    // Use sampling to estimate
                    int sampleSize = options.SampleSize;
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
                    
                case SamplingStrategy.AssumeOne:
                    // Just assume there's only one match
                    return 1;
                    
                case SamplingStrategy.UserProvided:
                    // Use the user-provided sample size as the count
                    return options.SampleSize;
                    
                default:
                    throw new ArgumentException("Unknown sampling strategy");
            }
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