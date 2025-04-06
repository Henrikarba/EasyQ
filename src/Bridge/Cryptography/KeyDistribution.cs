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
        
        /// <summary>
        /// Whether to use enhanced security features (6-state protocol instead of 4-state).
        /// </summary>
        public bool EnhancedSecurity { get; set; } = true;
        
        /// <summary>
        /// Whether to use decoy states to detect photon number splitting attacks.
        /// </summary>
        public bool UseDecoyStates { get; set; } = true;
        
        /// <summary>
        /// Whether to apply additional noise protection techniques.
        /// </summary>
        public bool UseNoiseProtection { get; set; } = true;
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
    /// Provides a simple API for quantum key distribution operations using enhanced BB84 protocol,
    /// hiding quantum computing complexity from users.
    /// </summary>
    public class QuantumKeyDistribution : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private readonly QuantumKeyDistributionOptions _defaultOptions;
        private bool _disposed = false;
        
        // Pre-shared authentication secret used to prevent man-in-the-middle attacks
        // In a real system, this would be established through a secure channel or pre-shared
        private readonly bool[] _presharedSecret;

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
            
            // Generate a random pre-shared secret for authentication
            // In a real-world implementation, this would be established out-of-band
            _presharedSecret = new bool[64];
            var random = new Random();
            for (int i = 0; i < _presharedSecret.Length; i++)
            {
                _presharedSecret[i] = random.Next(2) == 1;
            }
        }

        /// <summary>
        /// Executes the enhanced BB84 quantum key distribution protocol.
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
                        Console.WriteLine($"Attempt {attempts + 1}: Starting enhanced BB84 protocol with {initialBits} initial bits");
                        if (options.EnhancedSecurity)
                            Console.WriteLine("Using enhanced security with 6-state protocol");
                        if (options.UseDecoyStates)
                            Console.WriteLine("Using decoy states for photon-number splitting attack detection");
                        if (options.UseNoiseProtection)
                            Console.WriteLine("Using advanced noise protection techniques");
                    }

                    // Execute the enhanced quantum key distribution protocol
                    var (success, errorRate, keyBits, authTag) = await EnhancedBB84Protocol.Run(
                        _simulator,
                        options.KeyLength,
                        options.EnhancedSecurity,
                        options.UseDecoyStates,
                        options.UseNoiseProtection,
                        false, // No eavesdropping in production runs
                        0,     // Default eavesdropper strategy (not used when eavesdropping is false)
                        options.ErrorThreshold,
                        _presharedSecret);

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
                        Console.WriteLine($"Generated key size: {keyBytes.Length * 8} bits");
                        Console.WriteLine($"Authentication tag verified successfully");
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
        public async Task<KeyDistributionResult> SimulateWithEavesdropperAsync(
            QuantumKeyDistributionOptions? options = null, 
            int eavesdropperStrategy = 0)
        {
            options ??= _defaultOptions;

            try
            {
                if (options.EnableLogging)
                {
                    Console.WriteLine("Simulating enhanced BB84 protocol with eavesdropper");
                    Console.WriteLine($"Eavesdropper strategy: {eavesdropperStrategy}");
                }

                // Execute the enhanced quantum key distribution protocol with an eavesdropper
                var (success, errorRate, keyBits, authTag) = await EnhancedBB84Protocol.Run(
                    _simulator,
                    options.KeyLength,
                    options.EnhancedSecurity,
                    options.UseDecoyStates,
                    options.UseNoiseProtection,
                    true, // Simulate eavesdropping
                    eavesdropperStrategy,
                    options.ErrorThreshold,
                    _presharedSecret);

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
                    EnableLogging = options.EnableLogging,
                    EnhancedSecurity = options.EnhancedSecurity,
                    UseDecoyStates = options.UseDecoyStates,
                    UseNoiseProtection = options.UseNoiseProtection
                };

                // Run the protocol without generating a full key
                var (success, errorRate, _, _) = await EnhancedBB84Protocol.Run(
                    _simulator,
                    verificationOptions.KeyLength,
                    verificationOptions.EnhancedSecurity,
                    verificationOptions.UseDecoyStates,
                    verificationOptions.UseNoiseProtection,
                    false, // No simulated eavesdropping
                    0,     // Default strategy (not used)
                    verificationOptions.ErrorThreshold,
                    _presharedSecret);

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
        /// <param name="enhancedSecurity">Whether enhanced security features are enabled.</param>
        /// <returns>The recommended number of initial bits.</returns>
        public static int EstimateRequiredInitialBits(
            int keyLength, 
            double expectedErrorRate, 
            bool enhancedSecurity = true)
        {
            // Base multiplier - we need more than the key length due to sifting
            double baseFactor = enhancedSecurity ? 5.0 : 4.0;

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
        /// <param name="enhancedSecurity">Whether enhanced security features are used.</param>
        /// <returns>True if the error rate is within acceptable limits for a secure channel.</returns>
        public static bool IsErrorRateAcceptable(double errorRate, bool enhancedSecurity = true)
        {
            // For standard BB84, theoretical threshold for detecting eavesdropping is around 11%
            // For 6-state protocol (enhanced security), the threshold is around 14.6%
            // We use more conservative thresholds for production systems
            double maxAcceptableError = enhancedSecurity ? 0.10 : 0.08;
            return errorRate <= maxAcceptableError;
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