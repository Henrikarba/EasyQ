using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Cryptography;

namespace EasyQ.Bridge.Cryptography
{
    /// <summary>
    /// Provides a simple API for quantum random number generation, hiding all quantum computing complexity.
    /// Uses pure quantum approaches with no classical fallbacks to generate true randomness.
    /// </summary>
    /// <remarks>
    /// Quantum random number generation provides true randomness based on quantum measurement,
    /// which is fundamentally different from classical pseudo-random number generators.
    /// This makes it suitable for cryptographic applications and any scenario where
    /// true unpredictability is required.
    /// </remarks>
    public class QuantumRng : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private bool _disposed = false;
        private readonly bool _enableLogging;
        private readonly int _maxAttempts;

        /// <summary>
        /// Initializes a new instance of the QuantumRng class.
        /// </summary>
        public QuantumRng() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the QuantumRng class with logging option.
        /// </summary>
        /// <param name="enableLogging">Whether to log operations to the console.</param>
        /// <param name="maxAttempts">Maximum number of attempts before giving up.</param>
        public QuantumRng(bool enableLogging, int maxAttempts = 3)
        {
            _simulator = new QuantumSimulator();
            _enableLogging = enableLogging;
            _maxAttempts = maxAttempts;
        }

        /// <summary>
        /// Generates a true random integer between the specified minimum (inclusive) and maximum (inclusive) values
        /// using quantum measurement.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A true random integer in the specified range.</returns>
        /// <remarks>
        /// This function leverages quantum properties to generate truly random numbers,
        /// unlike classical algorithms that can only produce pseudo-random numbers.
        /// </remarks>
        public async Task<int> RandomInt(int min, int max)
        {
            if (min >= max)
            {
                throw new ArgumentException("Minimum value must be less than maximum value");
            }
            
            if (_enableLogging)
            {
                Console.WriteLine($"Generating quantum random integer between {min} and {max}...");
            }
            
            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    // Call the quantum operation which returns a long
                    long longResult = await GenerateRandomInt.Run(_simulator, min, max);
                    
                    // Check if the result is in the valid int range
                    if (longResult < int.MinValue || longResult > int.MaxValue)
                    {
                        throw new OverflowException($"Quantum operation returned a value {longResult} which is outside the valid int range");
                    }
                    
                    // Cast to int (safe since we've checked the range)
                    int result = (int)longResult;
                    
                    if (_enableLogging)
                    {
                        Console.WriteLine($"Generated true random integer: {result}");
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}. Retrying...");
                    }
                    
                    if (attempt == _maxAttempts)
                    {
                        throw new InvalidOperationException($"Failed to generate random integer between {min} and {max} after multiple attempts", ex);
                    }
                }
            }
            
            throw new InvalidOperationException("Failed to generate random integer");
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
        ~QuantumRng()
        {
            Dispose(false);
        }
    }
}