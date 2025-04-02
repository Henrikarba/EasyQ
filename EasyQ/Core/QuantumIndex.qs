namespace EasyQ.Core {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;
    
    /// # Summary
    /// Represents a quantum-enhanced index for database searching
    ///
    /// # Description
    /// QuantumIndex provides a developer-friendly abstraction for utilizing
    /// quantum search algorithms without requiring in-depth quantum knowledge.
    newtype QuantumIndex = (
        // The size of the index (must be a power of 2)
        Size : Int,
        
        // Maps hash values to original indices
        HashToOriginalMap : Int[],
        
        // Maps original indices to hash values
        OriginalToHashMap : Int[]
    );
    
    /// # Summary
    /// Creates a new QuantumIndex for a database
    ///
    /// # Input
    /// ## databaseSize
    /// The size of the database to index
    ///
    /// # Output
    /// A new QuantumIndex instance
    function CreateQuantumIndex(databaseSize : Int) : QuantumIndex {
        // Calculate the next power of 2 to ensure quantum compatibility
        let indexSize = NextPowerOfTwo(databaseSize);
        
        // Create the mapping arrays
        mutable hashToOriginal = [0, size = indexSize];
        mutable originalToHash = [0, size = databaseSize];
        
        // Initialize the mappings
        for i in 0..databaseSize - 1 {
            // For simplicity in this example, we're using a 1:1 mapping
            // In a real implementation, this would use a proper hash function
            let hashValue = i % indexSize;
            
            set hashToOriginal w/= hashValue <- i;
            set originalToHash w/= i <- hashValue;
        }
        
        return QuantumIndex(
            indexSize,
            hashToOriginal,
            originalToHash
        );
    }
    
    /// # Summary
    /// Gets the next power of 2 greater than or equal to a given number
    ///
    /// # Input
    /// ## n
    /// The input number
    ///
    /// # Output
    /// The next power of 2
    function NextPowerOfTwo(n : Int) : Int {
        mutable value = 1;
        
        while value < n {
            set value = value * 2;
        }
        
        return value;
    }
    
    /// # Summary
    /// Creates a QuantumIndex from an existing key-value mapping
    ///
    /// # Input
    /// ## keyValueMap
    /// Array of key-value pairs where the key is a string and the value is an integer
    ///
    /// # Output
    /// A quantum index for the provided mapping
    function CreateQuantumIndexFromMap(keyValueMap : (String, Int)[]) : QuantumIndex {
        let databaseSize = Length(keyValueMap);
        let indexSize = NextPowerOfTwo(databaseSize);
        
        mutable hashToOriginal = [0, size = indexSize];
        mutable originalToHash = [0, size = databaseSize];
        
        for i in 0..databaseSize - 1 {
            let (key, originalIndex) = keyValueMap[i];
            let hashValue = HashString(key, indexSize);
            
            set hashToOriginal w/= hashValue <- originalIndex;
            set originalToHash w/= i <- hashValue;
        }
        
        return QuantumIndex(
            indexSize,
            hashToOriginal,
            originalToHash
        );
    }
}