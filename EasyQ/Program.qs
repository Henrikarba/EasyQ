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
        DemonstrateQuantumSearch();
        
        // Demonstrate string search
        DemonstrateStringSearch();
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
    operation DemonstrateQuantumSearch() : Unit {
        Message("\n2. Quantum Database Search");
        Message("-------------------------");
        
        // Define database parameters
        let databaseSize = 8;
        let targetIndex = 3;
        
        // Perform simple search
        Message($"Searching in a database of size {databaseSize} for item at index {targetIndex}");
        let result = QuantumDatabaseSearch(databaseSize, targetIndex);
        Message($"Quantum search found item at index: {result}");
        
        // Demonstrate multiple item search
        let markedItems = [1, 6];
        Message($"\nSearching for one of multiple items at indices {markedItems}");
        let multiResult = QuantumMultiSearch(databaseSize, markedItems);
        Message($"Quantum search found item at index: {multiResult}");
        
        // Check if the result is one of our marked items
        let found = ContainsInt(multiResult, markedItems);
        Message($"Is the result ({multiResult}) one of our marked items? {found}");
    }
    
    /// # Summary
    /// Demonstrates the quantum string search capabilities
    operation DemonstrateStringSearch() : Unit {
        Message("\n3. Quantum String Search");
        Message("---------------------");
        
        // Create a sample dictionary of words
        let wordList = [
            "quantum", "computer", "algorithm", "superposition", 
            "entanglement", "qubit", "interference", "oracle",
            "register", "classical", "simulation", "speedup"
        ];
        
        // The database size needs to be a power of 2
        let databaseSize = 16; // Next power of 2 above dictionary size
        
        // Demonstrate exact word lookup
        let wordToFind = "qubit";
        Message($"Looking for word: '{wordToFind}'");
        
        let (found, foundWord) = LookupWord(databaseSize, wordToFind, wordList);
        
        if found {
            Message($"Successfully found: '{foundWord}'");
        } else {
            Message("Word not found in the database.");
        }
        
        // Demonstrate finding similar words
        let queryWord = "quantum";
        let maxResults = 3;
        Message($"\nFinding {maxResults} words similar to '{queryWord}':");
        
        let similarWords = FindSimilarWords(databaseSize, queryWord, wordList, maxResults);
        
        for word in similarWords {
            Message($"Similar word: '{word}'");
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