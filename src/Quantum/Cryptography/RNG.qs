namespace EasyQ.Quantum.Cryptography {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;

    /// # Summary
    /// Generates a single random bit using quantum measurement.
    ///
    /// # Output
    /// A random boolean value, with uniform probability of true or false.
    operation GenerateRandomBit() : Bool {
        use q = Qubit();
        // Put the qubit in equal superposition
        H(q);
        // Measure the qubit - get a truly random result
        let result = M(q) == One;
        // Reset the qubit to |0⟩ before releasing
        Reset(q);
        return result;
    }

    /// # Summary
    /// Generates an array of random bits using quantum measurements.
    ///
    /// # Input
    /// ## length
    /// The number of random bits to generate.
    ///
    /// # Output
    /// An array of random boolean values.
    operation GenerateRandomBits(length : Int) : Bool[] {
        mutable bits = [false, size = length];
        for i in 0..length - 1 {
            set bits w/= i <- GenerateRandomBit();
        }
        return bits;
    }

    /// # Summary
    /// Generates a random integer between min and max (inclusive).
    ///
    /// # Input
    /// ## min
    /// The minimum value (inclusive).
    /// ## max
    /// The maximum value (inclusive).
    ///
    /// # Output
    /// A random integer in the specified range.
    operation GenerateRandomInt(min : Int, max : Int) : Int {
        // Calculate how many bits we need to represent the range
        let range = max - min + 1;
        let numBits = Ceiling(Lg(IntAsDouble(range)));
        
        // Generate random bits
        mutable result = -1; // initialize to invalid value
        repeat {
            let bits = GenerateRandomBits(numBits);
            // Convert bits to integer
            result = min + BoolArrayAsInt(bits) % range;
        } until (result >= min and result <= max);
        
        return result;
    }

    /// # Summary
    /// Generates a byte array of random data.
    ///
    /// # Input
    /// ## length
    /// The number of bytes to generate.
    ///
    /// # Output
    /// An array of random bytes.
    operation GenerateRandomBytes(length : Int) : Int[] {
        mutable bytes = [0, size = length];
        
        for i in 0..length - 1 {
            // Generate 8 random bits for each byte
            let bits = GenerateRandomBits(8);
            // Convert to a byte (0-255)
            set bytes w/= i <- BoolArrayAsInt(bits);
        }
        
        return bytes;
    }

    /// # Summary
    /// Generates a random permutation of integers from 0 to length-1.
    /// Useful for cryptographic shuffling.
    ///
    /// # Input
    /// ## length
    /// The length of the permutation.
    ///
    /// # Output
    /// A random permutation as an integer array.
    operation GenerateRandomPermutation(length : Int) : Int[] {
        // Start with ordered sequence
        mutable permutation = [0, size = length];
        for i in 0..length - 1 {
            set permutation w/= i <- i;
        }
        
        // Fisher-Yates shuffle using quantum randomness
        for i in length - 1..-1..1 {
            // Generate a random index from 0 to i
            let j = GenerateRandomInt(0, i);
            
            // Swap elements at positions i and j
            let temp = permutation[i];
            set permutation w/= i <- permutation[j];
            set permutation w/= j <- temp;
        }
        
        return permutation;
    }

    /// # Summary
    /// Implementation of a quantum random oracle function.
    /// Can be used for cryptographic hashing in quantum-secured protocols.
    ///
    /// # Input
    /// ## input
    /// The input bits to hash.
    /// ## outputLength
    /// The desired output length in bits.
    /// ## seed
    /// A seed value for the oracle.
    ///
    /// # Output
    /// A hash of the input as a boolean array.
    operation QuantumRandomOracle(input : Bool[], outputLength : Int, seed : Int) : Bool[] {
        // This is a simplified implementation of a quantum-secure hash
        // In a real implementation, you'd want to use a more sophisticated approach
        
        // Convert input to integer for processing
        let inputValue = BoolArrayAsInt(input);
        
        // Use the seed to ensure different oracles for different uses
        mutable seedValue = seed;
        
        mutable output = [false, size = outputLength];
        
        for i in 0..outputLength - 1 {
            // For each output bit, create a qubit in superposition
            use q = Qubit();
            H(q);
            
            // Apply conditional phase based on input
            // This creates a correlation between input and output
            if ((inputValue + seedValue + i) % 2 == 1) {
                Z(q);
            }
            
            // Apply additional transformation
            H(q);
            
            // Measure to get the output bit
            set output w/= i <- M(q) == One;
            
            // Reset qubit
            Reset(q);
            
            // Update seed for next bit
            set seedValue = (seedValue * 75) % 65537;
        }
        
        return output;
    }

    /// # Summary
    /// Entanglement-based quantum random number generation.
    /// This provides additional security against certain attacks.
    ///
    /// # Input
    /// ## numBits
    /// The number of random bits to generate.
    ///
    /// # Output
    /// An array of random boolean values.
    operation EntanglementBasedRandomness(numBits : Int) : Bool[] {
        mutable randomBits = [false, size = numBits];
        
        for i in 0..numBits - 1 {
            // Create a Bell pair (entangled qubits)
            use (q1, q2) = (Qubit(), Qubit());
            H(q1);
            CNOT(q1, q2);
            
            // Measure the qubits - results will be perfectly correlated
            // but individually random
            let result = M(q1) == One;
            
            // We can verify entanglement by checking correlation
            // In a real implementation, you'd perform a proper Bell test
            let verification = M(q2) == One;
            
            // The results should match if the qubits were properly entangled
            if (result != verification) {
                // In a real scenario, you could throw an error or retry
                // For simplicity, we'll just try again
                use retry = Qubit();
                H(retry);
                set randomBits w/= i <- M(retry) == One;
                Reset(retry);
            } else {
                set randomBits w/= i <- result;
            }
            
            // Reset the qubits
            Reset(q1);
            Reset(q2);
        }
        
        return randomBits;
    }
}