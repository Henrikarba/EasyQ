namespace EasyQ.Core {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;

    /// # Summary
    /// Prepares a register of qubits in equal superposition.
    ///
    /// # Input
    /// ## qubits
    /// The qubits to prepare in superposition
    operation PrepareEqualSuperposition(qubits : Qubit[]) : Unit {
        ApplyToEach(H, qubits);
    }

    /// # Summary
    /// Measures a qubit register and returns the corresponding integer
    ///
    /// # Input
    /// ## qubits
    /// The qubit register to measure
    ///
    /// # Output
    /// The integer value corresponding to the measurement
    operation MeasureInteger(qubits : Qubit[]) : Int {
        mutable result = 0;
        for i in 0..Length(qubits) - 1 {
            if M(qubits[i]) == One {
                set result = result + 2^i;
            }
        }
        return result;
    }

    /// # Summary
    /// Converts an integer to an array of booleans representing its binary form
    ///
    /// # Input
    /// ## value
    /// The integer to convert
    /// ## bitSize
    /// The number of bits to use in the representation
    ///
    /// # Output
    /// Array of booleans representing the binary form of the integer
    function IntToBoolArray(value : Int, bitSize : Int) : Bool[] {
        return IntAsBoolArray(value, bitSize);
    }

    /// # Summary
    /// Converts an array of booleans to its integer representation
    ///
    /// # Input
    /// ## bits
    /// Array of booleans representing a binary number
    ///
    /// # Output
    /// The integer representation of the boolean array
    function BoolArrayToInt(bits : Bool[]) : Int {
        mutable result = 0;
        for i in 0..Length(bits) - 1 {
            if bits[i] {
                set result = result + 2^i;
            }
        }
        return result;
    }

    /// # Summary
    /// Calculates the number of bits required to represent a given integer
    ///
    /// # Input
    /// ## value
    /// The integer value
    ///
    /// # Output
    /// Number of bits needed to represent the value
    function BitsRequired(value : Int) : Int {
        return BitSizeI(value);
    }

    /// # Summary
    /// Simple hash function to convert a string to an integer
    ///
    /// # Input
    /// ## word
    /// The string to hash
    /// ## maxValue
    /// The maximum hash value (to ensure it fits in our database size)
    ///
    /// # Output
    /// A hash value between 0 and maxValue-1
    function HashString(word : String, maxValue : Int) : Int {
        mutable hash = 0;
        
        // Simple polynomial rolling hash function
        for i in 0..Length(word) - 1 {
            // Use each position to create a hash
            // Since Q# doesn't have a direct way to get character ASCII values,
            // we'll use the characters' positions in the string as a simplification
            let charValue = (i % 26) + 65; // Simplified approach using position in string
            
            // Update hash (polynomial rolling hash)
            set hash = ((hash * 31) % maxValue + charValue) % maxValue;
        }
        
        return hash;
    }
    
    /// # Summary
    /// Maps a list of strings to their hash values
    ///
    /// # Input
    /// ## words
    /// The list of strings to hash
    /// ## databaseSize
    /// The size of the database (maximum hash value + 1)
    ///
    /// # Output
    /// An array of hash values corresponding to the input strings
    function MapWordsToIndices(words : String[], databaseSize : Int) : Int[] {
        mutable indices = [];
        
        for i in 0..Length(words) - 1 {
            set indices = indices + [HashString(words[i], databaseSize)];
        }
        
        return indices;
    }
}