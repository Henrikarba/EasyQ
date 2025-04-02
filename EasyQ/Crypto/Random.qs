namespace EasyQ.Crypto {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open EasyQ.Core;

    /// # Summary
    /// Generates a random integer between min and max (both inclusive)
    /// using quantum randomness.
    ///
    /// # Input
    /// ## min
    /// The minimum value (inclusive) for the random number
    /// ## max
    /// The maximum value (inclusive) for the random number
    ///
    /// # Output
    /// A random integer between min and max (both inclusive)
    operation GenerateRandomInt(min : Int, max : Int) : Int {
        // Calculate how many bits we need to represent the range
        let range = max - min + 1; // +1 to make max inclusive
        let numBits = BitsRequired(range - 1);
        
        mutable resultInt = 0;
        mutable validResult = false;
        
        // Keep trying until we get a number within our range
        repeat {
            // Generate a random integer using quantum bits
            set resultInt = MeasureRandomInt(numBits);
            
            // Check if it's within our range
            set validResult = resultInt < range;
        } until validResult;
        
        // Return the random number shifted by min
        return resultInt + min;
    }
    
    /// # Summary
    /// Measures multiple qubits to generate a random integer
    ///
    /// # Input
    /// ## numBits
    /// Number of bits needed to represent the desired range
    ///
    /// # Output
    /// A random integer between 0 and 2^numBits - 1
    operation MeasureRandomInt(numBits : Int) : Int {
        mutable result = 0;
        
        // Allocate qubits and put them in superposition
        use qubits = Qubit[numBits];
        PrepareEqualSuperposition(qubits);
        
        // Measure qubits and get the result
        set result = MeasureInteger(qubits);
        
        // Reset all qubits to |0⟩ state before releasing
        ResetAll(qubits);
        
        return result;
    }
    
    /// # Summary
    /// Generates an array of random integers
    ///
    /// # Input
    /// ## length
    /// Length of the array to generate
    /// ## min
    /// Minimum value (inclusive) for each number
    /// ## max
    /// Maximum value (inclusive) for each number
    ///
    /// # Output
    /// Array of random integers
    operation GenerateRandomIntArray(length : Int, min : Int, max : Int) : Int[] {
        mutable result = [0, size = length];
        
        for i in 0..length - 1 {
            set result w/= i <- GenerateRandomInt(min, max);
        }
        
        return result;
    }
    
    /// # Summary
    /// Generates a random double between 0.0 and 1.0
    ///
    /// # Output
    /// A random double between 0.0 and 1.0
    operation GenerateRandomDouble() : Double {
        // Generate a random integer with high precision (24 bits)
        let randomInt = GenerateRandomInt(0, 2^24);
        
        // Convert to double between 0 and 1
        return IntAsDouble(randomInt) / IntAsDouble(2^24);
    }
    
    /// # Summary
    /// Generates a random double between min and max
    ///
    /// # Input
    /// ## min
    /// Minimum value (inclusive)
    /// ## max
    /// Maximum value (exclusive)
    ///
    /// # Output
    /// A random double between min and max
    operation GenerateRandomDoubleRange(min : Double, max : Double) : Double {
        let random = GenerateRandomDouble();
        return min + random * (max - min);
    }
}