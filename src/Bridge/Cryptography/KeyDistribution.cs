using Microsoft.Quantum.Simulation.Simulators;
using EasyQ.Quantum.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyQ.Bridge.Cryptography
{
    /// <summary>
    /// Configuration options for quantum key distribution operations.
    /// </summary>
    public class QuantumKeyDistributionOptions
    {
        /// <summary>
        /// The desired final key length in bits.
        /// </summary>
        public int KeyLength { get; set; } = 256;

        /// <summary>
        /// Maximum error rate permitted before considering the channel compromised.
        /// Error rates above this threshold will abort the key exchange.
        /// </summary>
        public double ErrorThreshold { get; set; } = 0.10;

        /// <summary>
        /// Percentage of bits to sacrifice for error detection (0.0 to 1.0).
        /// Higher values provide stronger security at the cost of shorter keys.
        /// </summary>
        public double SamplePercentage { get; set; } = 0.25;

        /// <summary>
        /// Number of bits to use in the initial exchange.
        /// A higher number increases the chance of generating a successful key.
        /// </summary>
        public int InitialBits { get; set; } = 0;  // 0 means automatic calculation (4x desired key length)

        /// <summary>
        /// Determines if detailed protocol steps should be logged.
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// The maximum number of exchange attempts before giving up.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;
    }

    /// <summary>
    /// Result of a quantum key distribution operation.
    /// </summary>
    public class KeyDistributionResult
    {
        /// <summary>
        /// Whether the key exchange was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The generated key as a byte array, or null if exchange failed.
        /// </summary>
        public byte[]? Key { get; }

        /// <summary>
        /// The detected error rate during reconciliation.
        /// </summary>
        public double ErrorRate { get; }

        /// <summary>
        /// Reason for failure if Success is false.
        /// </summary>
        public string? FailureReason { get; }

        /// <summary>
        /// Number of raw bits exchanged before sifting.
        /// </summary>
        public int RawBitsExchanged { get; }

        /// <summary>
        /// Number of bits after sifting (where bases matched).
        /// </summary>
        public int SiftedBitsCount { get; }

        /// <summary>
        /// Number of bits sacrificed for error detection.
        /// </summary>
        public int BitsUsedForErrorDetection { get; }

        /// <summary>
        /// Constructor for successful key distribution.
        /// </summary>
        internal KeyDistributionResult(
            byte[] key, 
            double errorRate, 
            int rawBitsExchanged, 
            int siftedBitsCount, 
            int bitsUsedForErrorDetection)
        {
            Success = true;
            Key = key;
            ErrorRate = errorRate;
            FailureReason = null;
            RawBitsExchanged = rawBitsExchanged;
            SiftedBitsCount = siftedBitsCount;
            BitsUsedForErrorDetection = bitsUsedForErrorDetection;
        }

        /// <summary>
        /// Constructor for failed key distribution.
        /// </summary>
        internal KeyDistributionResult(
            string failureReason, 
            double errorRate = 0.0, 
            int rawBitsExchanged = 0, 
            int siftedBitsCount = 0, 
            int bitsUsedForErrorDetection = 0)
        {
            Success = false;
            Key = null;
            ErrorRate = errorRate;
            FailureReason = failureReason;
            RawBitsExchanged = rawBitsExchanged;
            SiftedBitsCount = siftedBitsCount;
            BitsUsedForErrorDetection = bitsUsedForErrorDetection;
        }
    }

    /// <summary>
    /// Provides a simple API for quantum key distribution operations using BB84 protocol,
    /// hiding quantum computing complexity from users.
    /// </summary>
    public class QuantumKeyDistribution : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private readonly QuantumKeyDistributionOptions _defaultOptions;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the QuantumKeyDistribution class with default options.
        /// </summary>
        public QuantumKeyDistribution() : this(new QuantumKeyDistributionOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the QuantumKeyDistribution class with the specified options.
        /// </summary>
        /// <param name="options">Configuration options for key distribution operations.</param>
        public QuantumKeyDistribution(QuantumKeyDistributionOptions options)
        {
            _simulator = new QuantumSimulator();
            _defaultOptions = options ?? new QuantumKeyDistributionOptions();
        }

        /// <summary>
        /// Executes the BB84 quantum key distribution protocol.
        /// </summary>
        /// <param name="options">Optional operation-specific configuration. If null, default options are used.</param>
        /// <returns>A KeyDistributionResult containing the key or failure information.</returns>
        public async Task<KeyDistributionResult> GenerateKeyAsync(QuantumKeyDistributionOptions? options = null)
        {
            options ??= _defaultOptions;

            // Calculate initial bits if not specified
            int initialBits = options.InitialBits > 0 
                ? options.InitialBits 
                : 4 * options.KeyLength; // Default to 4x the desired key length

            // Calculate sample size based on percentage
            int sampleSize = (int)(initialBits * options.SamplePercentage);

            int attempts = 0;
            Exception? lastException = null;

            while (attempts < options.MaxAttempts)
            {
                try
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Attempt {attempts + 1}: Starting BB84 protocol with {initialBits} initial bits");
                    }

                    // Execute the quantum key distribution protocol
                    var (success, errorRate, keyBits) = await BB84Protocol.Run(
                        _simulator,
                        options.KeyLength,
                        false, // No eavesdropping in production runs
                        options.ErrorThreshold);

                    // Calculate bits used for protocols
                    int rawBitsExchanged = initialBits;
                    int siftedBitsCount = keyBits.Length + sampleSize; // Approximate
                    int bitsUsedForErrorDetection = sampleSize;

                    if (!success)
                    {
                        if (options.EnableLogging)
                        {
                            Console.WriteLine($"Key distribution failed: Error rate {errorRate:P2} exceeds threshold {options.ErrorThreshold:P2}");
                        }
                        return new KeyDistributionResult(
                            $"Error rate {errorRate:P2} exceeds threshold {options.ErrorThreshold:P2}. Possible eavesdropping detected.",
                            errorRate,
                            rawBitsExchanged,
                            siftedBitsCount,
                            bitsUsedForErrorDetection);
                    }

                    // Check if we have enough bits for the requested key
                    if (keyBits.Length < options.KeyLength)
                    {
                        if (options.EnableLogging)
                        {
                            Console.WriteLine($"Warning: Generated key is shorter than requested ({keyBits.Length} vs {options.KeyLength} bits)");
                        }
                    }

                    // Convert bit array to byte array
                    byte[] keyBytes = BoolArrayToByteArray(keyBits);

                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Key distribution successful. Error rate: {errorRate:P2}");
                    }

                    return new KeyDistributionResult(
                        keyBytes,
                        errorRate,
                        rawBitsExchanged,
                        siftedBitsCount,
                        bitsUsedForErrorDetection);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Attempt {attempts + 1} failed: {ex.Message}");
                    }
                    attempts++;
                }
            }

            // All attempts failed
            return new KeyDistributionResult(
                $"Key distribution failed after {options.MaxAttempts} attempts: {lastException?.Message ?? "Unknown error"}");
        }

        /// <summary>
        /// Simulates key distribution with an eavesdropper to demonstrate quantum security features.
        /// For demonstration or testing only - not intended for actual secure key generation.
        /// </summary>
        /// <param name="options">Optional operation-specific configuration. If null, default options are used.</param>
        /// <returns>A task representing the key distribution operation with an eavesdropper.</returns>
        public async Task<KeyDistributionResult> SimulateWithEavesdropperAsync(QuantumKeyDistributionOptions? options = null)
        {
            options ??= _defaultOptions;

            try
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine("Simulating BB84 protocol with eavesdropper");
                }

                // Execute the quantum key distribution protocol with an eavesdropper
                var (success, errorRate, keyBits) = await BB84Protocol.Run(
                    _simulator,
                    options.KeyLength,
                    true, // Simulate eavesdropping
                    options.ErrorThreshold);

                // Calculate approximate protocol statistics
                int initialBits = options.InitialBits > 0 ? options.InitialBits : 4 * options.KeyLength;
                int sampleSize = (int)(initialBits * options.SamplePercentage);
                int siftedBitsCount = keyBits.Length + sampleSize; // Approximate

                if (!success)
                {
                    if (options.EnableLogging)
                    {
                        Console.WriteLine($"Eavesdropper detected! Error rate: {errorRate:P2}");
                    }
                    return new KeyDistributionResult(
                        "Eavesdropper detected",
                        errorRate,
                        initialBits,
                        siftedBitsCount,
                        sampleSize);
                }

                // This case is unlikely but possible if the eavesdropping wasn't detected
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Warning: Eavesdropper not detected. Error rate: {errorRate:P2}");
                }

                // Convert bit array to byte array
                byte[] keyBytes = BoolArrayToByteArray(keyBits);

                return new KeyDistributionResult(
                    keyBytes,
                    errorRate,
                    initialBits,
                    siftedBitsCount,
                    sampleSize);
            }
            catch (Exception ex)
            {
                return new KeyDistributionResult($"Simulation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies if a key distribution channel is secure by checking for eavesdropping.
        /// </summary>
        /// <param name="options">Optional operation-specific configuration. If null, default options are used.</param>
        /// <returns>A tuple containing (isSecure, errorRate).</returns>
        public async Task<(bool isSecure, double errorRate)> VerifyChannelSecurityAsync(QuantumKeyDistributionOptions? options = null)
        {
            options ??= _defaultOptions;

            try
            {
                // Use a smaller key for faster verification
                var verificationOptions = new QuantumKeyDistributionOptions
                {
                    KeyLength = Math.Min(64, options.KeyLength),
                    ErrorThreshold = options.ErrorThreshold,
                    SamplePercentage = Math.Max(0.5, options.SamplePercentage), // Use more samples for verification
                    InitialBits = options.InitialBits,
                    EnableLogging = options.EnableLogging
                };

                // Run the protocol without generating a full key
                var (success, errorRate, _) = await BB84Protocol.Run(
                    _simulator,
                    verificationOptions.KeyLength,
                    false, // No simulated eavesdropping
                    verificationOptions.ErrorThreshold);

                if (options.EnableLogging)
                {
                    if (success)
                    {
                        Console.WriteLine($"Channel appears secure. Error rate: {errorRate:P2}");
                    }
                    else
                    {
                        Console.WriteLine($"Channel appears compromised. Error rate: {errorRate:P2}");
                    }
                }

                return (success, errorRate);
            }
            catch (Exception ex)
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Channel verification failed: {ex.Message}");
                }
                return (false, 1.0); // Assume worst case
            }
        }

        /// <summary>
        /// Estimates the number of initial bits needed for a successful key generation.
        /// </summary>
        /// <param name="keyLength">The desired key length in bits.</param>
        /// <param name="expectedErrorRate">The expected error rate of the quantum channel.</param>
        /// <returns>The recommended number of initial bits.</returns>
        public static int EstimateRequiredInitialBits(int keyLength, double expectedErrorRate)
        {
            // Base multiplier - we need more than the key length due to sifting
            double baseFactor = 4.0;

            // Additional factor based on expected error rate
            double errorFactor = 1.0 / (1.0 - Math.Min(0.9, expectedErrorRate));

            // Calculate and round up to the nearest multiple of 8
            int estimatedBits = (int)Math.Ceiling(keyLength * baseFactor * errorFactor);
            return ((estimatedBits + 7) / 8) * 8; // Round up to multiple of 8
        }

        /// <summary>
        /// Converts a boolean array to a byte array. Each byte contains 8 bits.
        /// </summary>
        /// <param name="bits">The boolean array representing bits.</param>
        /// <returns>A byte array containing the packed bits.</returns>
        private static byte[] BoolArrayToByteArray(bool[] bits)
        {
            // Calculate the number of bytes needed
            int numBytes = (bits.Length + 7) / 8;
            byte[] bytes = new byte[numBytes];

            // Pack bits into bytes
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << (i % 8));
                }
            }

            return bytes;
        }

        /// <summary>
        /// Determines if a specific error rate is acceptable for a secure quantum channel.
        /// </summary>
        /// <param name="errorRate">The measured error rate.</param>
        /// <returns>True if the error rate is within acceptable limits for a secure channel.</returns>
        public static bool IsErrorRateAcceptable(double errorRate)
        {
            // For BB84, theoretical threshold for detecting eavesdropping is around 11%
            // We use a more conservative threshold for production systems
            const double MAX_ACCEPTABLE_ERROR = 0.08; // 8%
            return errorRate <= MAX_ACCEPTABLE_ERROR;
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
        /// <param name="disposing">Whether this is being called from Dispose or the finalizer.</param>
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
        /// Finalizer.
        /// </summary>
        ~QuantumKeyDistribution()
        {
            Dispose(false);
        }
    }
}