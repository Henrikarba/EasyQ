using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Bridge.Cryptography;

namespace EasyQTests.Cryptography
{
    public class KeyDistributionTests : ITestRunner
    {
        public string Name => "Quantum Key Distribution Tests";
        public string Description => "Test quantum key distribution protocols";

        public async Task RunAsync()
        {
            Console.WriteLine("Quantum Key Distribution Tests");
            Console.WriteLine("===============================");

            await RunBasicKeyDistributionTest();
            Console.WriteLine();
            
            await RunEnhancedSecurityTest();
            Console.WriteLine();
            
            await RunEavesdropperDetectionTest();
            Console.WriteLine();
            
            await RunConfigurationOptionsTest();
            Console.WriteLine();
            
            await RunChannelSecurityVerificationTest();
        }

        private async Task RunBasicKeyDistributionTest()
        {
            Console.WriteLine("\nTest 1: Basic Key Distribution");
            Console.WriteLine("--------------------------------");
            
            // Basic options with minimal logging
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 128,
                EnableLogging = true,
                // Using standard BB84 for this test
                EnhancedSecurity = false,
                UseDecoyStates = false,
                UseNoiseProtection = false
            };
            
            using var keyDistribution = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Generating a 128-bit quantum key using standard BB84...");
            var result = await keyDistribution.GenerateKeyAsync();
            
            if (result.Success)
            {
                Console.WriteLine($"Key generation successful!");
                Console.WriteLine($"  - Generated key length: {result.Key.Length * 8} bits");
                Console.WriteLine($"  - Error rate: {result.ErrorRate:P2}");
                Console.WriteLine($"  - Raw bits exchanged: {result.RawBitsExchanged}");
                Console.WriteLine($"  - Sifted bits: {result.SiftedBitsCount}");
                
                // Print part of the key as hex for demonstration
                PrintPartialKey(result.Key);
            }
            else
            {
                Console.WriteLine($"Key generation failed: {result.FailureReason}");
            }
        }
        
        private async Task RunEnhancedSecurityTest()
        {
            Console.WriteLine("\nTest 2: Enhanced Security Features");
            Console.WriteLine("-----------------------------------");
            
            // Enhanced security options
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 128,
                EnableLogging = true,
                EnhancedSecurity = true,
                UseDecoyStates = true,
                UseNoiseProtection = true
            };
            
            using var keyDistribution = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Generating a 128-bit key with enhanced security features...");
            Console.WriteLine("  - Using 6-state protocol (instead of 4-state BB84)");
            Console.WriteLine("  - Using decoy states to detect photon-number splitting attacks");
            Console.WriteLine("  - Using noise protection techniques");
            
            var result = await keyDistribution.GenerateKeyAsync();
            
            if (result.Success)
            {
                Console.WriteLine($"Key generation successful!");
                Console.WriteLine($"  - Generated key length: {result.Key.Length * 8} bits");
                Console.WriteLine($"  - Error rate: {result.ErrorRate:P2}");
                Console.WriteLine($"  - Raw bits exchanged: {result.RawBitsExchanged}");
                Console.WriteLine($"  - Sifted bits: {result.SiftedBitsCount}");
                
                // Print part of the key as hex for demonstration
                PrintPartialKey(result.Key);
            }
            else
            {
                Console.WriteLine($"Key generation failed: {result.FailureReason}");
            }
        }
        
        private async Task RunEavesdropperDetectionTest()
        {
            Console.WriteLine("\nTest 3: Eavesdropper Detection");
            Console.WriteLine("-------------------------------");
            
            // Options for eavesdropper detection test
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 64,  // Smaller key for faster simulation
                EnableLogging = true,
                EnhancedSecurity = true,
                UseDecoyStates = true,
                UseNoiseProtection = true,
                ErrorThreshold = 0.15  // Higher threshold to allow for some simulation variance
            };
            
            using var keyDistribution = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Testing eavesdropper detection with different attack strategies:");
            
            // Try different eavesdropper strategies
            for (int strategy = 0; strategy < 3; strategy++)
            {
                Console.WriteLine($"\nSimulating eavesdropper with strategy {strategy}:");
                
                switch (strategy)
                {
                    case 0:
                        Console.WriteLine("  - Intercept-resend attack (simplest)");
                        break;
                    case 1:
                        Console.WriteLine("  - Collective attack (more advanced)");
                        break;
                    case 2:
                        Console.WriteLine("  - Coherent attack (most sophisticated)");
                        break;
                }
                
                var result = await keyDistribution.SimulateWithEavesdropperAsync(options, strategy);
                
                if (!result.Success)
                {
                    Console.WriteLine($"Eavesdropper successfully detected!");
                    Console.WriteLine($"  - Error rate: {result.ErrorRate:P2}");
                    Console.WriteLine($"  - Failure reason: {result.FailureReason}");
                }
                else
                {
                    Console.WriteLine($"Warning: Eavesdropper not detected");
                    Console.WriteLine($"  - Error rate: {result.ErrorRate:P2}");
                    Console.WriteLine($"  - This can happen with limited simulations or sophisticated attacks");
                    
                    // Print part of the key as hex for demonstration
                    PrintPartialKey(result.Key);
                }
            }
        }
        
        private async Task RunConfigurationOptionsTest()
        {
            Console.WriteLine("\nTest 4: Configuration Options");
            Console.WriteLine("-----------------------------");
            
            Console.WriteLine("Testing different key lengths:");
            
            int[] keyLengths = { 32, 64, 128, 256 };
            
            foreach (int keyLength in keyLengths)
            {
                Console.WriteLine($"\nGenerating a {keyLength}-bit key:");
                
                var options = new QuantumKeyDistributionOptions
                {
                    KeyLength = keyLength,
                    EnableLogging = false,  // Reduce logging for readability
                    EnhancedSecurity = true
                };
                
                using var keyDistribution = new QuantumKeyDistribution(options);
                var result = await keyDistribution.GenerateKeyAsync();
                
                if (result.Success)
                {
                    Console.WriteLine($"  - Success! Generated {result.Key.Length * 8} bits with error rate: {result.ErrorRate:P2}");
                    Console.WriteLine($"  - Raw/Sifted/Final: {result.RawBitsExchanged}/{result.SiftedBitsCount}/{result.Key.Length * 8}");
                }
                else
                {
                    Console.WriteLine($"  - Failed: {result.FailureReason}");
                }
            }
            
            Console.WriteLine("\nTesting error threshold settings:");
            
            double[] thresholds = { 0.05, 0.10, 0.15 };
            
            foreach (double threshold in thresholds)
            {
                Console.WriteLine($"\nSetting error threshold to {threshold:P0}:");
                
                var options = new QuantumKeyDistributionOptions
                {
                    KeyLength = 64,
                    ErrorThreshold = threshold,
                    EnableLogging = false
                };
                
                using var keyDistribution = new QuantumKeyDistribution(options);
                var result = await keyDistribution.GenerateKeyAsync();
                
                if (result.Success)
                {
                    Console.WriteLine($"  - Success! Error rate {result.ErrorRate:P2} below threshold {threshold:P2}");
                }
                else
                {
                    Console.WriteLine($"  - Failed: Error rate {result.ErrorRate:P2} exceeded threshold {threshold:P2}");
                }
            }
        }
        
        private async Task RunChannelSecurityVerificationTest()
        {
            Console.WriteLine("\nTest 5: Channel Security Verification");
            Console.WriteLine("------------------------------------");
            
            var options = new QuantumKeyDistributionOptions
            {
                EnableLogging = true,
                KeyLength = 32  // Small key for verification
            };
            
            using var keyDistribution = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Verifying channel security without eavesdropper:");
            var (isSecureNoEavesdropper, errorRateNoEavesdropper) = await keyDistribution.VerifyChannelSecurityAsync();
            Console.WriteLine($"  - Channel secure: {isSecureNoEavesdropper}");
            Console.WriteLine($"  - Error rate: {errorRateNoEavesdropper:P2}");
            
            // Use another instance to simulate eavesdropping for comparison
            Console.WriteLine("\nSimulating eavesdropper during verification:");
            
            // We need to use the simulate method, then verify
            using var eavesdroppedChannel = new QuantumKeyDistribution(options);
            var eavesdropperResult = await eavesdroppedChannel.SimulateWithEavesdropperAsync();
            
            Console.WriteLine($"  - Simulated channel secure: {eavesdropperResult.Success}");
            Console.WriteLine($"  - Error rate: {eavesdropperResult.ErrorRate:P2}");
            
            // Compare the results
            Console.WriteLine("\nComparison demonstrates quantum security principles:");
            Console.WriteLine($"  - Eavesdropping typically introduces errors: {errorRateNoEavesdropper:P2} vs {eavesdropperResult.ErrorRate:P2}");
            Console.WriteLine($"  - This is a fundamental quantum security property that protects against attacks");
        }
        
        // Helper method to print part of the key in hexadecimal format
        private void PrintPartialKey(byte[] key)
        {
            int bytesToShow = Math.Min(8, key.Length);
            
            Console.Write("  - Key sample (hex): ");
            for (int i = 0; i < bytesToShow; i++)
            {
                Console.Write($"{key[i]:X2}");
                if (i < bytesToShow - 1) Console.Write(":");
            }
            
            if (bytesToShow < key.Length)
            {
                Console.Write("...");
            }
            
            Console.WriteLine();
        }
    }
}