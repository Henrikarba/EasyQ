namespace EasyQ {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Diagnostics;
    
    open EasyQ.Core;
    open EasyQ.Crypto;
    open EasyQ.Algorithms;

    @EntryPoint()
    operation HelloQuantum() : Unit {
        Message("EasyQ Framework Demonstration");
        Message("-----------------------------");
        
        // Demonstrate random number generation
        DemonstrateRandomNumbers();
        
        // Demonstrate database search
        DemonstrateSearch();
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
    /// Demonstrates the quantum database search capabilities
    operation DemonstrateSearch() : Unit {
        Message("\n4. Quantum Search");
        Message("-----------------------------------");
        
        // Sample database - in a real application, this could be much larger
        let sampleData = [
            "quantum", "computer", "algorithm", "superposition", 
            "entanglement", "qubit", "interference", "oracle",
            "register", "classical", "simulation", "speedup"
        ];
        
        Message("Creating quantum index for sample data...");
        let index = CreateIndex(sampleData);
        
        // Exact search
        let searchKey = "qubit";
        Message($"Searching for '{searchKey}'...");
        
        let foundIndex = Search(index, searchKey);
        
        if foundIndex >= 0 and foundIndex < Length(sampleData) {
            Message($"Found '{searchKey}' at index {foundIndex}: {sampleData[foundIndex]}");
        } else {
            Message($"'{searchKey}' not found in the database.");
        }
        
        // Similar items search
        let queryKey = "quantum";
        let maxResults = 3;
        Message($"\nFinding {maxResults} items similar to '{queryKey}':");
        
        let similarIndices = FindSimilar(index, queryKey, maxResults);
        
        for idx in similarIndices {
            if idx >= 0 and idx < Length(sampleData) {
                Message($"Similar item: '{sampleData[idx]}'");
            }
        }
        
        // Key-value example with dictionary
        Message("\nKey-value search example:");
        
        let keys = ["user1", "user2", "user3", "user4"];
        let values = ["Alice", "Bob", "Charlie", "Dave"];
        
        let userIndex = CreateKeyValueIndex(keys, values);
        
        let userKey = "user3";
        Message($"Looking up '{userKey}'...");
        
        let userIdx = Search(userIndex, userKey);
        
        if userIdx >= 0 and userIdx < Length(values) {
            Message($"Found '{userKey}': {values[userIdx]}");
        } else {
            Message($"User not found.");
        }
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