namespace EasyQ.Quantum.Cryptography {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Random;
    open EasyQ.Quantum.Cryptography;

    /// # Summary
    /// Prepares a qubit in a state according to the BB84 protocol.
    /// 
    /// # Input
    /// ## bit
    /// The bit value to encode (0 or 1).
    /// ## basis
    /// The basis to use for encoding (0 for computational, 1 for Hadamard).
    ///
    /// # Output
    /// None - modifies the qubit in place.
    operation PrepareBB84State(bit : Bool, basis : Bool, q : Qubit) : Unit {
        // If the bit is 1, apply X to get |1⟩ instead of |0⟩
        if (bit) {
            X(q);
        }
        
        // If the basis is Hadamard, apply H to put the qubit in +/- basis
        if (basis) {
            H(q);
        }
    }
    
    /// # Summary
    /// Measures a qubit according to the BB84 protocol.
    ///
    /// # Input
    /// ## qubit
    /// The qubit to measure.
    /// ## basis
    /// The basis to use for measurement (0 for computational, 1 for Hadamard).
    ///
    /// # Output
    /// The result of the measurement (0 or 1).
    operation MeasureBB84State(qubit : Qubit, basis : Bool) : Bool {
        // If measuring in Hadamard basis, apply H before measuring
        if (basis) {
            H(qubit);
        }
        
        // Measure in computational basis and convert to bool
        let result = M(qubit) == One;
        
        // Reset qubit to |0⟩ before releasing
        Reset(qubit);
        
        return result;
    }
    
    /// # Summary
    /// Encodes a list of bits using random bases according to the BB84 protocol (senders side).
    ///
    /// # Input
    /// ## bits
    /// The list of bits to encode.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of chosen bases (to be kept secret initially)
    /// - The prepared qubits (to be transmitted to receiver)
    operation EncodeBB84Message(bits : Bool[]) : (Bool[], Qubit[]) {
        // Choose random bases for encoding
        mutable bases = [false, size = Length(bits)];
        
        // Allocate qubits
        use qubits = Qubit[Length(bits)];
        
        // Prepare each qubit according to the bit value and chosen basis
        for i in 0..Length(bits) - 1 {
            // Choose a random basis (0 or 1)
            set bases w/= i <- DrawRandomBool(0.5);
            
            // Prepare the qubit in the appropriate state
            PrepareBB84State(bits[i], bases[i], qubits[i]);
        }
        
        return (bases, qubits);
    }
    
    /// # Summary
    /// Receives and measures qubits according to the BB84 protocol (receivers side).
    ///
    /// # Input
    /// ## qubits
    /// The qubits received from sender.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of randomly chosen measurement bases
    /// - The measurement results
    operation ReceiveBB84Message(qubits : Qubit[]) : (Bool[], Bool[]) {
        let numQubits = Length(qubits);
        
        // Choose random bases for measurement
        mutable bases = [false, size = numQubits];
        mutable results = [false, size = numQubits];
        
        // Measure each qubit in the chosen basis
        for i in 0..numQubits - 1 {
            // Choose a random basis (0 or 1)
            set bases w/= i <- DrawRandomBool(0.5);
            
            // Measure the qubit in the chosen basis
            set results w/= i <- MeasureBB84State(qubits[i], bases[i]);
        }
        
        return (bases, results);
    }
    
    /// # Summary
    /// Simulates the quantum part of the BB84 protocol.
    ///
    /// # Input
    /// ## bitLength
    /// The number of bits to encode and send.
    /// ## eavesdropping
    /// If true, simulates an eavesdropper.
    ///
    /// # Output
    /// Tuple containing senders and receivers raw keys, along with the matching bases.
    operation SimulateBB84(bitLength : Int, eavesdropping : Bool) : (Bool[], Bool[], Bool[]) {
        // Generate random bits for sender to send
        mutable senderBits = [false, size = bitLength];
        for i in 0..bitLength - 1 {
            set senderBits w/= i <- DrawRandomBool(0.5);
        }
        
        // sender encodes her bits in random bases
        let (senderBases, qubits) = EncodeBB84Message(senderBits);
        
        // If there's an eavesdropper, Eve measures the qubits in random bases
        if (eavesdropping) {
            mutable eveBases = [false, size = bitLength];
            mutable eveResults = [false, size = bitLength];
            
            for i in 0..bitLength - 1 {
                // Eve chooses a random basis
                set eveBases w/= i <- DrawRandomBool(0.5);
                
                // Eve measures in her chosen basis
                set eveResults w/= i <- MeasureBB84State(qubits[i], eveBases[i]);
                
                // Eve needs to re-prepare the qubit to send to receiver
                // This introduces disturbance if Eve's basis doesn't match senders
                Reset(qubits[i]);
                PrepareBB84State(eveResults[i], eveBases[i], qubits[i]);
            }
        }
        
        // receiver measures the qubits in random bases
        let (receiverBases, receiverResults) = ReceiveBB84Message(qubits);
        
        // Determine which bits to keep
        mutable matchingBases = [false, size = bitLength];
        for i in 0..bitLength - 1 {
            set matchingBases w/= i <- senderBases[i] == receiverBases[i];
        }
        
        return (senderBits, receiverResults, matchingBases);
    }
    
    /// # Summary
    /// Performs key reconciliation and error estimation to detect eavesdropping.
    ///
    /// # Input
    /// ## senderBits
    /// Senders raw key bits.
    /// ## receiverBits
    /// Receivers raw key bits.
    /// ## matchingBases
    /// Boolean array indicating which positions used matching bases.
    /// ## sampleSize
    /// Number of bits to sacrifice for error detection.
    ///
    /// # Output
    /// Tuple containing the error rate and the sifted key (with sample bits removed).
    operation KeyReconciliation(
        senderBits : Bool[], 
        receiverBits : Bool[], 
        matchingBases : Bool[], 
        sampleSize : Int
    ) : (Double, Bool[]) {
        // First, extract only the bits where bases matched
        mutable siftedsenderBits = [];
        mutable siftedreceiverBits = [];
        
        for i in 0..Length(senderBits) - 1 {
            if (matchingBases[i]) {
                set siftedsenderBits += [senderBits[i]];
                set siftedreceiverBits += [receiverBits[i]];
            }
        }
        
        let siftedLength = Length(siftedsenderBits);
        
        // Make sure we don't try to sample more bits than we have
        let actualSampleSize = Min([sampleSize, siftedLength / 2]);
        
        // Choose random bit positions to compare for error detection
        mutable sampledIndices = [];
        mutable remainingIndices = [];
        
        // Generate random indices without replacement
        mutable availableIndices = SequenceI(0, siftedLength - 1);
        
        for i in 0..actualSampleSize - 1 {
            let randomIndex = GenerateRandomInt(0, Length(availableIndices) - 1);
            set sampledIndices += [availableIndices[randomIndex]];
            set availableIndices = availableIndices[0..randomIndex-1] + availableIndices[randomIndex+1..Length(availableIndices)-1];
        }
        
        // The remaining indices will be used for the final key
        for i in 0..siftedLength - 1 {
            if not ContainsInt(sampledIndices, i) {
                set remainingIndices += [i];
            }
        }
        
        // Count errors in the sampled bits
        mutable errorCount = 0;
        for i in sampledIndices {
            if (siftedsenderBits[i] != siftedreceiverBits[i]) {
                set errorCount += 1;
            }
        }
        
        // Calculate error rate
        let errorRate = IntAsDouble(errorCount) / IntAsDouble(actualSampleSize);
        
        // Extract the final key from the remaining bits
        mutable finalKey = [];
        for i in remainingIndices {
            set finalKey += [siftedsenderBits[i]];
        }
        
        return (errorRate, finalKey);
    }

    operation ContainsInt(array : Int[], value : Int) : Bool {
        for item in array {
            if item == value {
                return true;
            }
        }
        return false;
    }
        
    /// # Summary
    /// Performs privacy amplification to extract a shorter, more secure key.
    ///
    /// # Input
    /// ## rawKey
    /// The raw key after sifting.
    /// ## finalLength
    /// The desired length of the final key.
    /// ## seed
    /// A seed for the hash function.
    ///
    /// # Output
    /// The final secure key.
    operation PrivacyAmplification(rawKey : Bool[], finalLength : Int, seed : Int) : Bool[] {
        // For now, this is a simplified version that just performs
        // a kind of one-time pad with a hash function
        // In a real implementation, this would use more sophisticated methods
        
        let rawLength = Length(rawKey);
        
        // Don't try to extract more bits than we have
        let actualFinalLength = Min([finalLength, rawLength]);
        
        mutable finalKey = [false, size = actualFinalLength];
        
        // Simple hash function - in a real implementation, use a cryptographic hash
        for i in 0..actualFinalLength - 1 {
            // XOR bits from different positions based on seed
            mutable result = false;
            let hashPositions = [
                (i + seed) % rawLength,
                (i + seed * 2) % rawLength,
                (i + seed * 3) % rawLength
            ];
            
            for pos in hashPositions {
                set result = result != rawKey[pos]; // XOR
            }
            
            set finalKey w/= i <- result;
        }
        
        return finalKey;
    }
    
    /// # Summary
    /// Performs the complete BB84 protocol simulation.
    ///
    /// # Input
    /// ## keyLength
    /// The desired length of the final key.
    /// ## eavesdropping
    /// Whether to simulate an eavesdropper.
    /// ## errorThreshold
    /// Maximum allowed error rate before aborting.
    ///
    /// # Output
    /// Tuple containing success flag, error rate, and final key.
    operation BB84Protocol(
        keyLength : Int, 
        eavesdropping : Bool, 
        errorThreshold : Double
    ) : (Bool, Double, Bool[]) {
        // We need to start with more bits because some will be lost in sifting
        // and error detection
        let initialBits = 4 * keyLength; // Generous estimate
        
        // Perform quantum exchange
        let (senderBits, receiverBits, matchingBases) = SimulateBB84(initialBits, eavesdropping);
        
        // Perform key reconciliation and error detection
        // Use about 25% of the sifted bits for error detection
        let sampleSize = initialBits / 4;
        let (errorRate, siftedKey) = KeyReconciliation(senderBits, receiverBits, matchingBases, sampleSize);
        
        // If error rate is too high, abort (potential eavesdropper)
        if (errorRate > errorThreshold) {
            return (false, errorRate, []);
        }
        
        // Perform privacy amplification to extract final key
        let seed = 42; // In a real implementation, use a secure random seed
        let finalKey = PrivacyAmplification(siftedKey, keyLength, seed);
        
        return (true, errorRate, finalKey);
    }
}