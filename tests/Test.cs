using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Bridge.Cryptography;

namespace EasyQTests.Cryptography
{
    public class KeyDistributionTests : ITestRunner
    {
        public string Name => "Quantum Key Distribution Tests";
        public string Description => "Tests for E91 quantum key distribution protocol";

        public async Task RunAsync()
        {
            // Run all key distribution tests
            await BasicKeyDistributionTest();
            Console.WriteLine();
            await VerifyChannelSecurityTest();
            Console.WriteLine();
            await EfficiencySimulationTest();
            Console.WriteLine();
            await SecurityLevelComparisonTest();
        }

        private async Task BasicKeyDistributionTest()
        {
            Console.WriteLine("\nBasic Key Distribution Test");
            Console.WriteLine("---------------------------");
            
            // Create options with logging enabled for test visibility
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 64,         // Request a 64-bit key (shorter for test purposes)
                SecurityLevel = 2,      // Lower security level for faster tests
                SecurityThreshold = 2.2, // Standard security threshold
                EnableLogging = true,   // Enable logging for test visibility
                MaxAttempts = 2         // Limit attempts for faster testing
            };
            
            Console.WriteLine("Initializing quantum key distribution with these options:");
            Console.WriteLine($"- Key Length: {options.KeyLength} bits");
            Console.WriteLine($"- Security Level: {options.SecurityLevel} (1-5)");
            Console.WriteLine($"- Security Threshold: {options.SecurityThreshold} (CHSH value)");
            
            using var qkd = new QuantumKeyDistribution(options);
            
            Console.WriteLine("\nGenerating quantum-secured key...");
            var result = await qkd.GenerateKeyAsync();
            
            if (result.Success)
            {
                Console.WriteLine("\nKey distribution successful!");
                Console.WriteLine($"Generated key size: {(result.Key?.Length ?? 0) * 8} bits");
                Console.WriteLine($"Security parameter: {result.SecurityParameter:F2} (CHSH value)");
                Console.WriteLine($"Security margin: {QuantumKeyDistribution.CalculateSecurityMargin(result.SecurityParameter):F1}%");
                Console.WriteLine($"Error rate: {result.ErrorRate:P2}");
                Console.WriteLine($"Entangled pairs created: {result.EntangledPairsCreated}");
                
                if (result.Key != null)
                {
                    Console.WriteLine("\nKey preview (first 32 bits):");
                    DisplayKeyBits(result.Key, Math.Min(4, result.Key.Length));
                }
            }
            else
            {
                Console.WriteLine("\nKey distribution failed:");
                Console.WriteLine($"- Reason: {result.FailureReason}");
                Console.WriteLine($"- Security parameter: {result.SecurityParameter:F2}");
                Console.WriteLine($"- Error rate: {result.ErrorRate:P2}");
            }
        }
        
        private async Task VerifyChannelSecurityTest()
        {
            Console.WriteLine("\nChannel Security Verification Test");
            Console.WriteLine("--------------------------------");
            
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 32,       // Small key size for verification only
                SecurityLevel = 3,    // Medium security level
                EnableLogging = true
            };
            
            using var qkd = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Verifying quantum channel security...");
            var (isSecure, securityParameter, errorRate) = await qkd.VerifyChannelSecurityAsync();
            
            Console.WriteLine("\nVerification results:");
            Console.WriteLine($"- Channel secure: {isSecure}");
            Console.WriteLine($"- Security parameter: {securityParameter:F2} (CHSH value)");
            Console.WriteLine($"- Security margin: {QuantumKeyDistribution.CalculateSecurityMargin(securityParameter):F1}%");
            Console.WriteLine($"- Error rate: {errorRate:P2}");
            
            if (isSecure)
            {
                Console.WriteLine("\nThe quantum channel appears to be secure and free from eavesdropping.");
            }
            else
            {
                Console.WriteLine("\nWarning: The quantum channel may be compromised. Possible eavesdropping detected.");
            }
        }
        
        private async Task EfficiencySimulationTest()
        {
            Console.WriteLine("\nProtocol Efficiency Simulation Test");
            Console.WriteLine("---------------------------------");
            
            var options = new QuantumKeyDistributionOptions
            {
                KeyLength = 256,
                SecurityLevel = 3
            };
            
            using var qkd = new QuantumKeyDistribution(options);
            
            Console.WriteLine("Simulating E91 protocol efficiency under various conditions...");
            var efficiencyResults = qkd.SimulateProtocolEfficiency();
            
            Console.WriteLine("\nTheoretical key yields (% of raw bits convertible to secure key):");
            Console.WriteLine("\n| Security Parameter | Error Rate | Key Yield |");
            Console.WriteLine("|-------------------|-----------|----------|");
            
            // Sort by security parameter and error rate for better readability
            var sortedKeys = new List<string>(efficiencyResults.Keys);
            sortedKeys.Sort((a, b) => {
                // Parse S=x.xx,E=y% format
                double secParamA = double.Parse(a.Substring(2, 4));
                double secParamB = double.Parse(b.Substring(2, 4));
                
                if (secParamA != secParamB)
                    return secParamB.CompareTo(secParamA); // Higher security parameter first
                    
                double errorRateA = double.Parse(a.Substring(8, a.Length - 9)) / 100.0;
                double errorRateB = double.Parse(b.Substring(8, b.Length - 9)) / 100.0;
                
                return errorRateA.CompareTo(errorRateB); // Lower error rate first
            });
            
            foreach (var key in sortedKeys)
            {
                Console.WriteLine($"| {key.Replace(",", " | ")} | {efficiencyResults[key]:P1} |");
            }
            
            // Calculate average yield
            double avgYield = 0;
            foreach (var yield in efficiencyResults.Values)
                avgYield += yield;
            avgYield /= efficiencyResults.Count;
            
            Console.WriteLine($"\nAverage key yield: {avgYield:P1}");
            Console.WriteLine($"Estimated pairs needed for a 256-bit key: {QuantumKeyDistribution.EstimateRequiredPairs(256, 3, 0.05)}");
        }
        
        private async Task SecurityLevelComparisonTest()
        {
            Console.WriteLine("\nSecurity Level Comparison Test");
            Console.WriteLine("----------------------------");
            
            // Compare different security levels
            Console.WriteLine("Comparing key generation with different security levels (1-5)...");
            
            // Table header
            Console.WriteLine("\n| Security Level | Entangled Pairs | Key Size | Security Parameter | Error Rate | Duration |");
            Console.WriteLine("|---------------|----------------|----------|-------------------|-----------|----------|");
            
            for (int level = 1; level <= 5; level++)
            {
                var options = new QuantumKeyDistributionOptions
                {
                    KeyLength = 64,
                    SecurityLevel = level,
                    EnableLogging = false
                };
                
                using var qkd = new QuantumKeyDistribution(options);
                
                var startTime = DateTime.Now;
                var result = await qkd.GenerateKeyAsync();
                var duration = DateTime.Now - startTime;
                
                string resultStr;
                if (result.Success)
                {
                    resultStr = $"| {level} | {result.EntangledPairsCreated} | {(result.Key?.Length ?? 0) * 8} bits | {result.SecurityParameter:F2} | {result.ErrorRate:P1} | {duration.TotalSeconds:F1}s |";
                }
                else
                {
                    resultStr = $"| {level} | {result.EntangledPairsCreated} | Failed | {result.SecurityParameter:F2} | {result.ErrorRate:P1} | {duration.TotalSeconds:F1}s |";
                }
                
                Console.WriteLine(resultStr);
            }
            
            Console.WriteLine("\nHigher security levels:");
            Console.WriteLine("- Create more entangled pairs");
            Console.WriteLine("- Perform more rigorous security checks");
            Console.WriteLine("- Take longer to generate keys");
            Console.WriteLine("- Provide higher resistance to eavesdropping");
        }
        
        // Helper method to display key bits
        private void DisplayKeyBits(byte[] key, int maxBytes)
        {
            for (int i = 0; i < Math.Min(maxBytes, key.Length); i++)
            {
                // Convert byte to binary string with leading zeros
                string binary = Convert.ToString(key[i], 2).PadLeft(8, '0');
                Console.Write(binary + " ");
            }
            
            if (key.Length > maxBytes)
            {
                Console.Write("...");
            }
            
            Console.WriteLine();
        }
    }
}