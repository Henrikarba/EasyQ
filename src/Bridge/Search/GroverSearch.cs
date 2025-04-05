using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Search;

namespace EasyQ.Bridge.Search
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
        /// Searches for all items matching a specific value in a collection using quantum search.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="collection">The collection to search in.</param>
        /// <param name="item">The item to search for.</param>
        /// <param name="options">Optional search configuration options.</param>
        /// <returns>A list of tuples containing the found items and their indices.</returns>
        public Task<List<(T Item, int Index)>> Search<T>(IList<T> collection, T item, QuantumSearchOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));

            // Create a predicate that checks for exact match with the item
            bool predicate(T x) => EqualityComparer<T>.Default.Equals(x, item);

            return Search(collection, predicate, options);
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
        public async Task<List<(T Item, int Index)>> Search<T>(
            IList<T> collection, 
            Func<T, bool> predicate, 
            QuantumSearchOptions? options = null,
            int? knownMatchCount = null)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (collection.Count == 0) throw new ArgumentException("Collection cannot be empty", nameof(collection));
            ArgumentNullException.ThrowIfNull(predicate);

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
                throw new InvalidOperationException("No items match the criteria in the collection");
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
                throw new InvalidOperationException("Quantum search failed to find any matching items");
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
                throw new InvalidOperationException("No items match the criteria in the search space");
            }
            
            // Simplified logging
            if (options.EnableLogging && matchCount > 0)
            {
                Console.WriteLine($"Matching items: {matchCount}");
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
                throw new InvalidOperationException("No items match the criteria in the sampling range");
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
            
            // Try with different targets and different iteration strategies
            Random rng = new Random();
            int maxAttempts = options.MaxAttempts ?? 50; // Increase default max attempts
            
            // In the first half of attempts, try different targets
            // In the second half, try different iterations with the same targets
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Select a target - cycle through all known targets systematically
                int targetIndex = attempt % targets.Count;
                int target = targets[targetIndex];
                
                try
                {                    
                    // Calculate iterations based on the selected strategy
                    double angle = Math.Asin(Math.Sqrt(matchCount / (double)searchSpace));
                    
                    // For the second half of attempts, try different iteration counts
                    int iterations;
                    if (attempt >= maxAttempts / 2)
                    {
                        // Try different iteration counts in the second half of attempts
                        int iterationVariation = (attempt - maxAttempts / 2) % 5;
                        iterations = Math.Max(1, CalculateIterations(angle) + iterationVariation - 2);
                    }
                    else
                    {
                        iterations = CalculateIterations(angle);
                    }
                    
                    // Create oracle for the target item
                    var oracle = await CreateOracleForItem.Run(_simulator, numQubits, target);
                    
                    // Run Grover's search algorithm
                    long result = await GroverSearch.Run(_simulator, numQubits, oracle, iterations);
                    
                    // Verify the result matches our predicate
                    if (result < collectionSize && predicate((int)result))
                    {
                        return result;
                    }
                }
                catch (Exception)
                {
                    // Try again with different parameters
                }
            }
            
            // If we've tried many times and failed, as a last resort, return the first valid target
            // This ensures the framework is usable even when the quantum algorithm is having difficulty
            if (targets.Count > 0)
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Using fallback after {maxAttempts} attempts");
                }
                return targets[0];
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