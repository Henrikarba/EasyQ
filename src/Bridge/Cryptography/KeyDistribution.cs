using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.Quantum.Simulation.Core;
using EasyQ.Quantum.Cryptography;

namespace EasyQ.Bridge.Cryptography
{
    /// <summary>
    /// Configuration options for quantum key distribution.
    /// </summary>
    public class QuantumKeyDistributionOptions
    {
        /// <summary>
        /// The desired length of the generated key in bits.
        /// </summary>
        public int KeyLength { get; set; } = 256;

        /// <summary>
        /// Security level (1-5). Higher values increase security but decrease efficiency.
        /// </summary>
        public int SecurityLevel { get; set; } = 3;

        /// <summary>
        /// The minimum security parameter (CHSH value) required to consider a channel secure.
        /// Classical limit is 2.0. Quantum maximum is 2√2 ≈ 2.83.
        /// Recommended values: 2.2-2.4 for standard security.
        /// </summary>
        public double SecurityThreshold { get; set; } = 2.2;

        /// <summary>
        /// Maximum number of attempts to generate a key before giving up.
        /// </summary>
        public int? MaxAttempts { get; set; } = 5;

        /// <summary>
        /// Whether to log detailed information about the key distribution process.
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Authentication mode to use for key verification.
        /// </summary>
        public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.Standard;

        /// <summary>
        /// Custom pre-shared authentication secret. If null, a random one will be generated.
        /// </summary>
        public byte[]? PreSharedSecret { get; set; } = null;

        /// <summary>
        /// Determines if error correction should be performed on the raw key.
        /// </summary>
        public bool EnableErrorCorrection { get; set; } = true;

        /// <summary>
        /// The maximum acceptable error rate before aborting key generation.
        /// </summary>
        public double MaxAcceptableErrorRate { get; set; } = 0.12;
    }

    /// <summary>
    /// Authentication modes for quantum key distribution.
    /// </summary>
    public enum AuthenticationMode
    {
        /// <summary>
        /// No authentication is performed.
        /// </summary>
        None,

        /// <summary>
        /// Standard authentication using pre-shared secret.
        /// </summary>
        Standard,

        /// <summary>
        /// Enhanced authentication with additional quantum verification.
        /// </summary>
        Enhanced
    }

    /// <summary>
    /// Results of a quantum key distribution operation.
    /// </summary>
    public class KeyDistributionResult
    {
        /// <summary>
        /// Whether the key distribution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The generated key (null if unsuccessful).
        /// </summary>
        public byte[]? Key { get; set; }

        /// <summary>
        /// Authentication tag for key verification.
        /// </summary>
        public byte[]? AuthenticationTag { get; set; }

        /// <summary>
        /// The security parameter value (CHSH inequality test result).
        /// </summary>
        public double SecurityParameter { get; set; }

        /// <summary>
        /// The observed error rate between sender and receiver keys.
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// The number of entangled pairs created during the process.
        /// </summary>
        public int EntangledPairsCreated { get; set; }

        /// <summary>
        /// Reason for failure (if unsuccessful).
        /// </summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Provides a simple API for quantum key distribution, hiding all quantum computing complexity.
    /// Uses the E91 protocol with entanglement-based quantum key distribution.
    /// </summary>
    public class QuantumKeyDistribution : IDisposable
    {
        private readonly QuantumSimulator _simulator;
        private bool _disposed = false;
        private readonly QuantumKeyDistributionOptions _options;

        /// <summary>
        /// Initializes a new instance of the QuantumKeyDistribution class with default options.
        /// </summary>
        public QuantumKeyDistribution() : this(new QuantumKeyDistributionOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the QuantumKeyDistribution class with specified options.
        /// </summary>
        /// <param name="options">Configuration options for quantum key distribution.</param>
        public QuantumKeyDistribution(QuantumKeyDistributionOptions options)
        {
            _simulator = new QuantumSimulator();
            _options = options ?? new QuantumKeyDistributionOptions();
        }

        /// <summary>
        /// Generates a cryptographically secure key using quantum key distribution.
        /// </summary>
        /// <returns>The result of the key distribution process.</returns>
        public async Task<KeyDistributionResult> GenerateKeyAsync()
        {
            if (_options.KeyLength <= 0)
            {
                throw new ArgumentException("Key length must be greater than zero", nameof(_options.KeyLength));
            }

            if (_options.SecurityLevel < 1 || _options.SecurityLevel > 5)
            {
                throw new ArgumentException("Security level must be between 1 and 5", nameof(_options.SecurityLevel));
            }

            // Number of key generation attempts
            int maxAttempts = _options.MaxAttempts ?? 5;
            int attempts = 0;

            // Generate or use pre-shared authentication secret
            var authSecret = await GenerateOrUseAuthSecret();

            // Save the number of entangled pairs created for reporting
            int entangledPairsCreated = 0;

            if (_options.EnableLogging)
            {
                Console.WriteLine($"Starting quantum key distribution (QKD) process");
                Console.WriteLine($"Requested key length: {_options.KeyLength} bits");
                Console.WriteLine($"Security level: {_options.SecurityLevel} (1-5)");
                Console.WriteLine($"Security threshold: {_options.SecurityThreshold}");
            }

            while (attempts < maxAttempts)
            {
                attempts++;

                if (_options.EnableLogging)
                {
                    Console.WriteLine($"\nAttempt {attempts} of {maxAttempts}");
                }

                try
                {
                    // Calculate the number of bytes needed
                    int keyByteLength = (_options.KeyLength + 7) / 8;

                    // Execute the quantum key distribution protocol
                    var result = await GenerateQuantumSecureKey.Run(
                        _simulator, 
                        _options.KeyLength, 
                        _options.SecurityLevel,
                        _options.SecurityThreshold,
                        authSecret);

                    // Extract the results
                    bool success = result.Item1;
                    double securityParameter = result.Item2;
                    double errorRate = result.Item3;
                    var keyBits = result.Item4;
                    var authTag = result.Item5;

                    // Update the number of entangled pairs created
                    // This is an estimate based on the protocol and security level
                    entangledPairsCreated = EstimateRequiredPairs(_options.KeyLength, _options.SecurityLevel, errorRate);

                    if (_options.EnableLogging)
                    {
                        Console.WriteLine($"QKD result: {(success ? "Success" : "Failed")}");
                        Console.WriteLine($"Security parameter: {securityParameter:F3}");
                        Console.WriteLine($"Error rate: {errorRate:P2}");
                        Console.WriteLine($"Estimated entangled pairs: {entangledPairsCreated}");
                    }

                    // If key generation was successful
                    if (success && keyBits.Count > 0)
                    {
                        // Convert bit array to byte array
                        byte[] key = QArrayToBytesArray(keyBits);
                        byte[] tag = QArrayToBytesArray(authTag);

                        // Verify authentication if enabled and not in 'None' mode
                        bool authenticVerified = _options.AuthenticationMode == AuthenticationMode.None ||
                                               await VerifyAuthenticationKey(key, tag, authSecret);

                        if (!authenticVerified)
                        {
                            if (_options.EnableLogging)
                            {
                                Console.WriteLine("Authentication verification failed");
                            }
                            continue; // Try again
                        }

                        if (_options.EnableLogging)
                        {
                            Console.WriteLine($"Generated {key.Length * 8} key bits successfully");
                            if (_options.AuthenticationMode != AuthenticationMode.None)
                            {
                                Console.WriteLine("Authentication verified successfully");
                            }
                        }

                        // Return successful result
                        return new KeyDistributionResult
                        {
                            Success = true,
                            Key = key,
                            AuthenticationTag = tag,
                            SecurityParameter = securityParameter,
                            ErrorRate = errorRate,
                            EntangledPairsCreated = entangledPairsCreated
                        };
                    }
                    else
                    {
                        // Key generation failed, try again
                        if (_options.EnableLogging)
                        {
                            Console.WriteLine("Key generation failed, retrying...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_options.EnableLogging)
                    {
                        Console.WriteLine($"Error during key generation: {ex.Message}");
                    }
                }
            }

            // All attempts failed
            string failureReason = "Maximum attempts reached without successful key generation";
            if (_options.EnableLogging)
            {
                Console.WriteLine(failureReason);
            }

            return new KeyDistributionResult
            {
                Success = false,
                Key = null,
                SecurityParameter = 0,
                ErrorRate = 0,
                EntangledPairsCreated = entangledPairsCreated,
                FailureReason = failureReason
            };
        }

        /// <summary>
        /// Verifies the security of a quantum channel without generating a full key.
        /// Useful for checking if eavesdropping is detected.
        /// </summary>
        /// <returns>Tuple containing: (channel is secure, security parameter, error rate)</returns>
        public async Task<(bool isSecure, double securityParameter, double errorRate)> VerifyChannelSecurityAsync()
        {
            if (_options.EnableLogging)
            {
                Console.WriteLine("Verifying quantum channel security...");
            }

            try
            {
                // Define parameters for a smaller test
                int testKeyLength = Math.Min(32, _options.KeyLength);

                // Generate or use pre-shared authentication secret
                var authSecret = await GenerateOrUseAuthSecret();

                // Execute the quantum key distribution protocol in verification mode
                var result = await GenerateQuantumSecureKey.Run(
                    _simulator, 
                    testKeyLength, 
                    _options.SecurityLevel,
                    _options.SecurityThreshold,
                    authSecret);

                // Extract the results
                bool success = result.Item1;
                double securityParameter = result.Item2;
                double errorRate = result.Item3;

                bool isSecure = success && securityParameter >= _options.SecurityThreshold && errorRate <= _options.MaxAcceptableErrorRate;

                if (_options.EnableLogging)
                {
                    Console.WriteLine($"Channel security verification result: {(isSecure ? "Secure" : "Potentially compromised")}");
                    Console.WriteLine($"Security parameter: {securityParameter:F3} (minimum: {_options.SecurityThreshold})");
                    Console.WriteLine($"Error rate: {errorRate:P2} (maximum: {_options.MaxAcceptableErrorRate:P2})");
                }

                return (isSecure, securityParameter, errorRate);
            }
            catch (Exception ex)
            {
                if (_options.EnableLogging)
                {
                    Console.WriteLine($"Error during channel verification: {ex.Message}");
                }
                return (false, 0, 0);
            }
        }

        /// <summary>
        /// Simulates the protocol efficiency under various conditions.
        /// Useful for estimating performance without running the full protocol.
        /// </summary>
        /// <returns>Dictionary mapping condition descriptions to key yield ratios</returns>
        public Dictionary<string, double> SimulateProtocolEfficiency()
        {
            var results = new Dictionary<string, double>();

            // Test different combinations of security parameters and error rates
            double[] securityParams = { 2.0, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8 };
            double[] errorRates = { 0.01, 0.03, 0.05, 0.07, 0.1 };

            foreach (var s in securityParams)
            {
                foreach (var e in errorRates)
                {
                    // Skip invalid combinations
                    if (s <= 2.0 || e > 0.15) continue;

                    // Use the Q# method's logic to estimate yield
                    int rawBits = 1000; // Arbitrary number for estimation
                    int secureKeySize = EstimateKeyYield(s, e, rawBits);
                    double yield = secureKeySize / (double)rawBits;

                    string key = $"S={s:F2},E={e:P0}";
                    results[key] = yield;
                }
            }

            return results;
        }

        /// <summary>
        /// Estimates the key yield based on security parameter and error rate.
        /// </summary>
        /// <param name="securityParameter">The security parameter (CHSH value)</param>
        /// <param name="errorRate">The error rate</param>
        /// <param name="numRawBits">The number of raw key bits</param>
        /// <returns>The estimated number of secure key bits that can be extracted</returns>
        private int EstimateKeyYield(double securityParameter, double errorRate, int numRawBits)
        {
            // Security margin: how far above the classical bound (2.0) the security parameter is
            double securityMargin = securityParameter - 2.0;
            
            // Classical bound for CHSH inequality is 2.0
            // Quantum maximum is 2√2 ≈ 2.83
            
            if (securityMargin <= 0.0)
            {
                // No secure bits can be generated if we don't exceed the classical bound
                return 0;
            }

            double binaryEntropy = BinaryEntropy(errorRate);
            
            // Calculate secure key rate r ≈ 1 - H(e) - leakage
            // where e is the error rate and leakage is potential information leakage
            
            // Estimated information leakage (decreases as security parameter increases)
            double maxDeviation = 0.83;  // 2√2 - 2 ≈ 0.83
            double leakage = 1.0 - (securityMargin / maxDeviation);
            double clampedLeakage = Math.Clamp(leakage, 0.0, 1.0);
            
            // Calculate secure key rate
            double rate = 1.0 - binaryEntropy - clampedLeakage;
            // Ensure non-negative rate
            double secureRate = Math.Max(0.0, rate);
            
            // Calculate estimated secure bits
            return Math.Max(0, (int)Math.Floor(numRawBits * secureRate));
        }

        /// <summary>
        /// The binary entropy function, used in information theory calculations.
        /// </summary>
        private double BinaryEntropy(double p)
        {
            if (p <= 0.0 || p >= 1.0)
            {
                return 0.0;
            }
            
            // h(p) = -p log₂(p) - (1-p) log₂(1-p)
            double term1 = p == 0 ? 0 : -p * Math.Log2(p);
            double term2 = (1-p) == 0 ? 0 : -(1-p) * Math.Log2(1-p);
            
            return term1 + term2;
        }

        /// <summary>
        /// Calculates the security margin percentage based on the security parameter.
        /// </summary>
        /// <param name="securityParameter">The security parameter (CHSH value)</param>
        /// <returns>The security margin as a percentage</returns>
        public static double CalculateSecurityMargin(double securityParameter)
        {
            double classicalLimit = 2.0;
            double quantumMax = 2.0 * Math.Sqrt(2.0); // ≈ 2.83
            
            double margin = securityParameter - classicalLimit;
            double maxMargin = quantumMax - classicalLimit; // ≈ 0.83
            
            // Return as a percentage of the maximum possible quantum advantage
            return Math.Max(0, Math.Min(100, 100 * margin / maxMargin));
        }

        /// <summary>
        /// Estimates the number of entangled pairs needed based on desired key length and parameters.
        /// </summary>
        /// <param name="keyLength">Desired final key length in bits</param>
        /// <param name="securityLevel">Security level (1-5)</param>
        /// <param name="expectedErrorRate">Expected error rate</param>
        /// <returns>Estimated number of entangled pairs needed</returns>
        public static int EstimateRequiredPairs(int keyLength, int securityLevel, double expectedErrorRate)
        {
            // Base efficiency - proportion of pairs that contribute to raw key
            double baseEfficiency = 0.25; // In E91, we expect about 25% of pairs contribute to raw key
            
            // Adjust efficiency based on error rate - errors reduce usable bits
            double errorAdjustment = 1.0 - expectedErrorRate * 2;
            
            // Security overhead - more security tests means fewer bits for the key
            double securityOverhead = 1.0 + (securityLevel - 1) * 0.5;
            
            // Privacy amplification reduces final key length
            double privacyReduction = 0.7; // Typical value - we keep about 70% after privacy amplification
            
            // Calculate total efficiency
            double totalEfficiency = baseEfficiency * errorAdjustment * privacyReduction / securityOverhead;
            
            // Calculate required pairs with a safety margin
            int requiredPairs = (int)Math.Ceiling(keyLength / totalEfficiency * 1.2);
            
            return requiredPairs;
        }

        /// <summary>
        /// Generates or uses a pre-shared authentication secret.
        /// </summary>
        private async Task<IQArray<bool>> GenerateOrUseAuthSecret()
        {
            // If a pre-shared secret was provided, use it
            if (_options.PreSharedSecret != null && _options.PreSharedSecret.Length > 0)
            {
                return ByteArrayToQArray(_options.PreSharedSecret);
            }

            // Otherwise, generate a quantum random secret
            // The size depends on the authentication mode
            int secretLength = _options.AuthenticationMode == AuthenticationMode.Enhanced ? 128 : 32;
            
            // Generate random bits using quantum random number generation
            var randomBits = await EntanglementBasedRandomness.Run(_simulator, secretLength);
            
            return randomBits;
        }

        /// <summary>
        /// Verifies the authenticity of a key using its authentication tag.
        /// </summary>
        private async Task<bool> VerifyAuthenticationKey(byte[] key, byte[] tag, IQArray<bool> authSecret)
        {
            if (_options.AuthenticationMode == AuthenticationMode.None)
            {
                return true; // No authentication needed
            }

            try
            {
                // Convert the key and tag to QArrays
                var keyBits = ByteArrayToQArray(key);
                var tagBits = ByteArrayToQArray(tag);

                // Call the Q# verification function
                return await EasyQ.Quantum.Cryptography.VerifyAuthentication.Run(_simulator, keyBits, tagBits, authSecret);
            }
            catch (Exception ex)
            {
                if (_options.EnableLogging)
                {
                    Console.WriteLine($"Authentication verification error: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Converts an IQArray<bool> to a byte array.
        /// </summary>
        private byte[] QArrayToBytesArray(IQArray<bool> bits)
        {
            int byteCount = (bits.Count + 7) / 8;
            byte[] bytes = new byte[byteCount];

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << (i % 8));
                }
            }

            return bytes;
        }

        /// <summary>
        /// Converts a byte array to an IQArray<bool>.
        /// </summary>
        private IQArray<bool> ByteArrayToQArray(byte[] bytes)
        {
            var bits = new bool[bytes.Length * 8];

            for (int i = 0; i < bytes.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bits[i * 8 + j] = ((bytes[i] >> j) & 1) == 1;
                }
            }

            return new QArray<bool>(bits);
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
        ~QuantumKeyDistribution()
        {
            Dispose(false);
        }
    }
}