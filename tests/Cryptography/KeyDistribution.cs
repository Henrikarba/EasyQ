using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyQ.Bridge.Cryptography;

namespace EasyQTests.Cryptography
{
    public class KeyDistributionTests : ITestRunner
    {
        public string Name => "Quantum Key Distribution Tests";
        public string Description => "Tests for quantum key distribution using the E91 protocol";

        public async Task RunAsync()
        {
            // Run all key distribution tests
            await BasicKeyDistributionTest();
            Console.WriteLine();
            await VerifyChannelSecurityTest();
            Console.WriteLine();
            await SecurityLevelComparisonTest();
            Console.WriteLine();
            await AuthenticationModeTest();
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
                MaxAttempts = 3         // Limit attempts for faster testing
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
        
        private async Task SecurityLevelComparisonTest()
        {
            Console.WriteLine("\nSecurity Level Comparison Test");
            Console.WriteLine("----------------------------");
            
            // Use a shorter key length for testing to speed things up
            int testKeyLength = 32;
            
            // Compare different security levels
            Console.WriteLine($"Comparing key generation with different security levels (1-5) for {testKeyLength}-bit keys...");
            
            // Table header
            Console.WriteLine("\n| Security Level | Success | Key Size | Security Parameter | Error Rate | Pairs |");
            Console.WriteLine("|---------------|---------|----------|-------------------|-----------|-------|");
            
            for (int level = 1; level <= 5; level++)
            {
                var options = new QuantumKeyDistributionOptions
                {
                    KeyLength = testKeyLength,
                    SecurityLevel = level,
                    EnableLogging = false,
                    MaxAttempts = 2  // Limit attempts for faster testing
                };
                
                using var qkd = new QuantumKeyDistribution(options);
                
                var startTime = DateTime.Now;
                var result = await qkd.GenerateKeyAsync();
                var duration = DateTime.Now - startTime;
                
                string resultStr = $"| {level} | {result.Success} | {(result.Key?.Length ?? 0) * 8} bits | {result.SecurityParameter:F2} | {result.ErrorRate:P1} | {result.EntangledPairsCreated} |";
                Console.WriteLine(resultStr);
            }
            
            Console.WriteLine("\nHigher security levels:");
            Console.WriteLine("- Create more entangled pairs");
            Console.WriteLine("- Perform more rigorous security checks");
            Console.WriteLine("- Take longer to generate keys");
            Console.WriteLine("- Provide higher resistance to eavesdropping");
        }
        
        private async Task AuthenticationModeTest()
        {
            Console.WriteLine("\nAuthentication Mode Test");
            Console.WriteLine("----------------------");
            
            Console.WriteLine("Testing different authentication modes:");
            
            // Table header
            Console.WriteLine("\n| Authentication Mode | Success | Key Size | Notes |");
            Console.WriteLine("|---------------------|---------|----------|-------|");
            
            // Test each authentication mode
            foreach (AuthenticationMode mode in Enum.GetValues(typeof(AuthenticationMode)))
            {
                var options = new QuantumKeyDistributionOptions
                {
                    KeyLength = 32,
                    SecurityLevel = 2,
                    EnableLogging = false,
                    AuthenticationMode = mode,
                    MaxAttempts = 2
                };
                
                string notes = "";
                
                // For Standard mode, test with a pre-shared secret
                if (mode == AuthenticationMode.Standard)
                {
                    // Generate a simple pre-shared secret
                    options.PreSharedSecret = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
                    notes = "With pre-shared secret";
                }
                
                using var qkd = new QuantumKeyDistribution(options);
                
                var result = await qkd.GenerateKeyAsync();
                
                string resultStr = $"| {mode} | {result.Success} | {(result.Key?.Length ?? 0) * 8} bits | {notes} |";
                Console.WriteLine(resultStr);
            }
            
            Console.WriteLine("\nNotes on authentication modes:");
            Console.WriteLine("- None: No authentication, faster but less secure");
            Console.WriteLine("- Standard: Uses pre-shared secret for authentication");
            Console.WriteLine("- Enhanced: Uses quantum verification for stronger authentication");
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