namespace EasyQ {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Diagnostics;
    
    open EasyQ.Core;
    open EasyQ.Crypto;

    @EntryPoint()
    operation Main() : Unit {
        Message("EasyQ Framework Demonstration");
        Message("-----------------------------");
        
        // Demonstrate random number generation
        DemonstrateRandomNumbers();
    }
    
    /// # Summary
    /// Demonstrates the quantum random number generator capabilities
    operation DemonstrateRandomNumbers() : Unit {
        Message("\n1. Quantum Random Number Generation");
        Message("----------------------------------");
        
        // Generate a single random integer
        let randomInt = GenerateRandomInt(1, 100);
        Message($"Random integer between 1 and 100: {randomInt}");
        
        // Generate an array of random integers
        let randomArray = GenerateRandomIntArray(5, 1, 9);
        Message($"Random array of 5 integers between 1 and 9: {randomArray}");
        
        // Generate random doubles
        let randomDouble = GenerateRandomDouble();
        Message($"Random double between 0 and 1: {randomDouble}");
        
        let randomRangeDouble = GenerateRandomDoubleRange(10.5, 20.5);
        Message($"Random double between 10.5 and 20.5: {randomRangeDouble}");
    }
    
    /// # Summary
    /// Checks if an integer item is in an integer array
    ///
    /// # Input
    /// ## item
    /// The integer to check
    /// ## array
    /// The integer array to search
    ///
    /// # Output
    /// True if the item is in the array, false otherwise
    function ContainsInt(item : Int, array : Int[]) : Bool {
        for element in array {
            if element == item {
                return true;
            }
        }
        return false;
    }
}