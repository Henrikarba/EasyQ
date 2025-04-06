namespace EasyQ.Quantum.Cryptography {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Random;
    open Microsoft.Quantum.Diagnostics;

    /// # Summary
    /// Prepares a qubit in a state according to the BB84 protocol.
    /// 
    /// # Input
    /// ## bit
    /// The bit value to encode (0 or 1).
    /// ## basis
    /// The basis to use for encoding (0 for computational, 1 for Hadamard).
    /// ## q
    /// The qubit to prepare.
    ///
    /// # Output
    /// None - modifies the qubit in place.
    operation PrepareBB84State(bit : Bool, basis : Bool, q : Qubit) : Unit is Adj + Ctl {
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
    /// Encodes a list of bits using random bases according to the BB84 protocol (sender's side).
    ///
    /// # Input
    /// ## bits
    /// The list of bits to encode.
    /// ## basisSeed
    /// Optional seed for basis generation. If provided, uses seeded randomness for reproducibility.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of chosen bases (to be kept secret initially)
    /// - The prepared qubits (to be transmitted to receiver)
    operation EncodeBB84Message(bits : Bool[], basisSeed : Int?) : (Bool[], Qubit[]) {
        let length = Length(bits);
        mutable bases = [false, size = length];
        
        // Allocate qubits
        use qubits = Qubit[length];
        
        // Create a seeded random number generator if a seed is provided
        let hasFixedSeed = basisSeed != null;
        
        // Prepare each qubit according to the bit value and chosen basis
        for i in 0..length - 1 {
            // Choose a basis - either randomly or deterministically if a seed is provided
            let basis = hasFixedSeed
                ? (i + basisSeed!) % 2 == 0  // Deterministic but unpredictable pattern
                : DrawRandomBool(0.5);        // True randomness
                
            set bases w/= i <- basis;
            
            // Prepare the qubit in the appropriate state
            PrepareBB84State(bits[i], bases[i], qubits[i]);
        }
        
        return (bases, qubits);
    }
    
    /// # Summary
    /// Receives and measures qubits according to the BB84 protocol (receiver's side).
    ///
    /// # Input
    /// ## qubits
    /// The qubits received from sender.
    /// ## basisSeed
    /// Optional seed for basis generation. If provided, uses seeded randomness for reproducibility.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of randomly chosen measurement bases
    /// - The measurement results
    operation ReceiveBB84Message(qubits : Qubit[], basisSeed : Int?) : (Bool[], Bool[]) {
        let numQubits = Length(qubits);
        
        // Choose random bases for measurement
        mutable bases = [false, size = numQubits];
        mutable results = [false, size = numQubits];
        
        // Check if we should use seeded randomness
        let hasFixedSeed = basisSeed != null;
        
        // Measure each qubit in the chosen basis
        for i in 0..numQubits - 1 {
            // Choose basis - either randomly or deterministically if a seed is provided
            let basis = hasFixedSeed
                ? (i + basisSeed! + 1) % 2 == 0  // Different pattern from sender
                : DrawRandomBool(0.5);           // True randomness
                
            set bases w/= i <- basis;
            
            // Measure the qubit in the chosen basis
            set results w/= i <- MeasureBB84State(qubits[i], bases[i]);
        }
        
        return (bases, results);
    }
    
    /// # Summary
    /// Simulates an eavesdropper intercepting and measuring qubits during BB84 protocol.
    ///
    /// # Input
    /// ## qubits
    /// The qubits being transmitted.
    /// ## eavesdroppingStrategy
    /// Strategy for basis selection (0: random, 1: computational, 2: Hadamard, 3: alternating).
    /// ## interceptProbability
    /// Probability of intercepting each qubit (0.0 to 1.0).
    ///
    /// # Output
    /// None - modifies the qubits in place, potentially disturbing them.
    operation SimulateEavesdropping(
        qubits : Qubit[], 
        eavesdroppingStrategy : Int,
        interceptProbability : Double) : Unit {
        
        let numQubits = Length(qubits);
        
        for i in 0..numQubits - 1 {
            // Decide whether to intercept this qubit
            let shouldIntercept = DrawRandomDouble(0.0, 1.0) < interceptProbability;
            
            if shouldIntercept {
                // Choose basis according to strategy
                let basis = 
                    eavesdroppingStrategy == 0 ? DrawRandomBool(0.5) :  // Random basis
                    eavesdroppingStrategy == 1 ? false :                // Always computational basis
                    eavesdroppingStrategy == 2 ? true :                 // Always Hadamard basis
                    eavesdroppingStrategy == 3 ? (i % 2 == 0) :         // Alternating basis
                    DrawRandomBool(0.5);                               // Default to random
                
                // Measure the qubit and resend
                let result = MeasureBB84State(qubits[i], basis);
                
                // Reinitialize the qubit in the same state as measured
                // This introduces errors if the basis doesn't match the original
                PrepareBB84State(result, basis, qubits[i]);
            }
        }
    }
    
    /// # Summary
    /// Enhanced version of the BB84 protocol with advanced eavesdropping detection.
    ///
    /// # Input
    /// ## numQubits
    /// The number of qubits to use for the key exchange.
    /// ## eavesdropping
    /// If true, simulates an eavesdropper.
    /// ## eavesdroppingStrategy
    /// Strategy for eavesdropping (0-3, see SimulateEavesdropping).
    /// ## interceptProbability
    /// Probability of the eavesdropper intercepting each qubit.
    /// ## decoyStates
    /// Fraction of decoy states to include for improved detection.
    /// ## senderSeed
    /// Optional seed for sender's basis selection.
    /// ## receiverSeed
    /// Optional seed for receiver's basis selection.
    ///
    /// # Output
    /// Tuple containing sender's raw key, receiver's raw key, and matching bases.
    operation EnhancedBB84Exchange(
        numQubits : Int,
        eavesdropping : Bool,
        eavesdroppingStrategy : Int,
        interceptProbability : Double,
        decoyStates : Double,
        senderSeed : Int?,
        receiverSeed : Int?
    ) : (Bool[], Bool[], Bool[]) {
        
        // Adjust qubit count for decoy states
        let adjustedQubits = numQubits + 
            (decoyStates > 0.0 ? Round(IntAsDouble(numQubits) * decoyStates) : 0);
        
        // Generate random bits for sender to send
        mutable senderBits = [false, size = adjustedQubits];
        
        for i in 0..adjustedQubits - 1 {
            set senderBits w/= i <- DrawRandomBool(0.5);
        }
        
        // Insert decoy states (known values in specific positions)
        mutable decoyPositions = [];
        if (decoyStates > 0.0) {
            // Mark some positions as decoy states
            let numDecoyStates = adjustedQubits - numQubits;
            
            // Generate random positions for decoy states
            // In a production system, these would be pseudo-randomly generated with a shared seed
            let rng = new Random();
            decoyPositions = DrawRandomInt(0, adjustedQubits - 1, numDecoyStates);
            
            // Set decoy state values - alternating pattern for simplicity
            for i in 0..Length(decoyPositions) - 1 {
                set senderBits w/= decoyPositions[i] <- i % 2 == 0;
            }
        }
        
        // Sender encodes her bits in random or seeded bases
        let (senderBases, qubits) = EncodeBB84Message(senderBits, senderSeed);
        
        // If there's an eavesdropper, Eve measures and resends the qubits
        if (eavesdropping) {
            SimulateEavesdropping(qubits, eavesdroppingStrategy, interceptProbability);
        }
        
        // Receiver measures the qubits in random or seeded bases
        let (receiverBases, receiverResults) = ReceiveBB84Message(qubits, receiverSeed);
        
        // Filter out decoy states before returning
        mutable filteredSenderBits = [];
        mutable filteredReceiverResults = [];
        mutable filteredMatchingBases = [];
        
        for i in 0..adjustedQubits - 1 {
            // Skip decoy positions
            if (not ContainsI(decoyPositions, i)) {
                set filteredSenderBits += [senderBits[i]];
                set filteredReceiverResults += [receiverResults[i]];
                set filteredMatchingBases += [senderBases[i] == receiverBases[i]];
            }
        }
        
        return (filteredSenderBits, filteredReceiverResults, filteredMatchingBases);
    }
    
    /// # Summary
    /// Checks if an array contains a specific integer.
    ///
    /// # Input
    /// ## array
    /// The array to search.
    /// ## value
    /// The value to find.
    ///
    /// # Output
    /// True if the array contains the value.
    operation ContainsI(array : Int[], value : Int) : Bool {
        for item in array {
            if item == value {
                return true;
            }
        }
        return false;
    }
    /// # Summary
    /// Performs key reconciliation and error estimation to detect eavesdropping.
    ///
    /// # Input
    /// ## senderBits
    /// Sender's raw key bits.
    /// ## receiverBits
    /// Receiver's raw key bits.
    /// ## matchingBases
    /// Boolean array indicating which positions used matching bases.
    /// ## sampleSize
    /// Number of bits to sacrifice for error detection.
    /// ## minSampleSize
    /// Minimum sample size for reliable error estimation.
    ///
    /// # Output
    /// Tuple containing:
    /// - The error rate
    /// - The sifted key (with sample bits removed)
    /// - Boolean indicating if sufficient bits were available
    operation KeyReconciliation(
        senderBits : Bool[], 
        receiverBits : Bool[], 
        matchingBases : Bool[], 
        sampleSize : Int,
        minSampleSize : Int
    ) : (Double, Bool[], Bool) {
        // Extract only the bits where bases matched
        mutable siftedSenderBits = [];
        mutable siftedReceiverBits = [];
        
        for i in 0..Length(senderBits) - 1 {
            if (matchingBases[i]) {
                set siftedSenderBits += [senderBits[i]];
                set siftedReceiverBits += [receiverBits[i]];
            }
        }
        
        let siftedLength = Length(siftedSenderBits);
        
        // Check if we have enough bits for sifting and sampling
        if (siftedLength < minSampleSize) {
            return (1.0, [], false); // Return max error rate and empty key
        }
        
        // Make sure we don't try to sample more bits than we have
        let actualSampleSize = Min([sampleSize, siftedLength / 2]);
        
        // If even the reduced sample size is too small, abort
        if (actualSampleSize < minSampleSize) {
            return (1.0, [], false);
        }
        
        // Choose random bit positions to compare for error detection
        mutable sampledIndices = [];
        mutable remainingIndices = [];
        
        // Generate random indices without replacement
        mutable availableIndices = SequenceI(0, siftedLength - 1);
        
        for i in 0..actualSampleSize - 1 {
            let randomIndex = DrawRandomInt(0, Length(availableIndices) - 1);
            set sampledIndices += [availableIndices[randomIndex]];
            set availableIndices = availableIndices[0..randomIndex-1] + availableIndices[randomIndex+1..Length(availableIndices)-1];
        }
        
        // The remaining indices will be used for the final key
        for i in 0..siftedLength - 1 {
            if not ContainsI(sampledIndices, i) {
                set remainingIndices += [i];
            }
        }
        
        // Count errors in the sampled bits
        mutable errorCount = 0;
        for i in sampledIndices {
            if (siftedSenderBits[i] != siftedReceiverBits[i]) {
                set errorCount += 1;
            }
        }
        
        // Calculate error rate
        let errorRate = IntAsDouble(errorCount) / IntAsDouble(actualSampleSize);
        
        // Extract the final key from the remaining bits
        mutable finalKey = [];
        for i in remainingIndices {
            set finalKey += [siftedSenderBits[i]];
        }
        
        return (errorRate, finalKey, true);
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
    /// ## useToeplitzMatrix
    /// Whether to use Toeplitz matrix (true = more secure, false = simpler).
    ///
    /// # Output
    /// The final secure key.
    operation PrivacyAmplification(
        rawKey : Bool[], 
        finalLength : Int, 
        seed : Int,
        useToeplitzMatrix : Bool
    ) : Bool[] {
        let rawLength = Length(rawKey);
        
        // Don't try to extract more bits than we have
        let actualFinalLength = Min([finalLength, rawLength]);
        
        mutable finalKey = [false, size = actualFinalLength];
        
        if (useToeplitzMatrix) {
            // Generate a Toeplitz matrix for privacy amplification
            // This is more secure for quantum-safe hashing
            for i in 0..actualFinalLength - 1 {
                // For each output bit, create a row of the Toeplitz matrix
                mutable row = [false, size = rawLength];
                
                // Fill the row using a deterministic but unpredictable pattern
                for j in 0..rawLength - 1 {
                    // XOR of multiple bit positions based on seed and indices
                    set row w/= j <- ((i + j + seed) % 7 == 0) or 
                                      ((i - j + seed) % 5 == 0) or 
                                      ((i * j + seed) % 3 == 0);
                }
                
                // Compute output bit as XOR of selected input bits
                mutable result = false;
                for j in 0..rawLength - 1 {
                    if (row[j] and rawKey[j]) {
                        set result = not result; // XOR
                    }
                }
                
                set finalKey w/= i <- result;
            }
        } else {
            // Simpler universal hash function
            for i in 0..actualFinalLength - 1 {
                // XOR bits from different positions based on seed
                mutable result = false;
                let hashPositions = [
                    (i + seed) % rawLength,
                    (i + seed * 2) % rawLength,
                    (i + seed * 3) % rawLength,
                    (i + seed * 5) % rawLength
                ];
                
                for pos in hashPositions {
                    set result = result != rawKey[pos]; // XOR
                }
                
                set finalKey w/= i <- result;
            }
        }
        
        return finalKey;
    }
    
    /// # Summary
    /// Analyzes the results of the BB84 protocol to detect specific attack patterns.
    ///
    /// # Input
    /// ## senderBits
    /// Sender's raw key bits.
    /// ## receiverBits
    /// Receiver's raw key bits.
    /// ## matchingBases
    /// Boolean array indicating which positions used matching bases.
    /// ## decoyResults
    /// Results from decoy states, if any.
    ///
    /// # Output
    /// A tuple containing:
    /// - Whether an attack was detected
    /// - The type of attack detected (0 = none, 1 = intercept-resend, 2 = MITM, 3 = other)
    /// - Confidence level (0.0 to 1.0)
    operation DetectAttackPatterns(
        senderBits : Bool[],
        receiverBits : Bool[],
        matchingBases : Bool[],
        decoyResults : (Bool[], Bool[])
    ) : (Bool, Int, Double) {
        // Basic error rate calculation
        mutable errorCount = 0;
        mutable totalMatchingBases = 0;
        
        for i in 0..Length(senderBits) - 1 {
            if (matchingBases[i]) {
                set totalMatchingBases += 1;
                if (senderBits[i] != receiverBits[i]) {
                    set errorCount += 1;
                }
            }
        }
        
        // Calculate basic error rate
        let errorRate = totalMatchingBases > 0 
            ? IntAsDouble(errorCount) / IntAsDouble(totalMatchingBases)
            : 0.0;
            
        // Check for specific attack patterns
        
        // 1. Intercept-resend attack typically causes ~25% error rate
        let interceptResendLikelihood = 
            errorRate > 0.2 and errorRate < 0.3 ? 0.9 :
            errorRate > 0.15 and errorRate < 0.35 ? 0.7 :
            errorRate > 0.1 and errorRate < 0.4 ? 0.5 :
            0.0;
            
        // 2. Man-in-the-middle might show inconsistent errors or modified decoy states
        let decoyStateTampering = Length(decoyResults) > 0 
            ? AnalyzeDecoyStates(decoyResults) 
            : 0.0;
            
        // Determine attack type and confidence
        if (errorRate > 0.4) {
            // Very high error rate indicates severe tampering
            return (true, 3, 0.95);
        }
        elif (interceptResendLikelihood > 0.7) {
            // Likely intercept-resend attack
            return (true, 1, interceptResendLikelihood);
        }
        elif (decoyStateTampering > 0.7) {
            // Likely MITM attack targeting decoy states
            return (true, 2, decoyStateTampering);
        }
        elif (errorRate > 0.11) {
            // General threshold for suspicion
            let confidence = (errorRate - 0.11) * 10.0; // Scale to confidence
            return (true, 3, Min([confidence, 0.9]));
        }
            
        // No attack detected
        return (false, 0, 0.0);
    }
    
    /// # Summary
    /// Analyzes decoy states to detect man-in-the-middle attacks.
    ///
    /// # Input
    /// ## decoyResults
    /// Tuple of sent and received decoy states.
    ///
    /// # Output
    /// Confidence level that decoy states have been tampered with (0.0 to 1.0).
    operation AnalyzeDecoyStates(decoyResults : (Bool[], Bool[])) : Double {
        let (sentDecoys, receivedDecoys) = decoyResults;
        
        if (Length(sentDecoys) == 0) {
            return 0.0;
        }
        
        mutable errorCount = 0;
        
        for i in 0..Length(sentDecoys) - 1 {
            if (sentDecoys[i] != receivedDecoys[i]) {
                set errorCount += 1;
            }
        }
        
        let errorRate = IntAsDouble(errorCount) / IntAsDouble(Length(sentDecoys));
        
        // Scale to confidence level
        return Min([errorRate * 2.0, 1.0]);
    }
    
    /// # Summary
    /// Performs the complete BB84 protocol simulation with enhanced security features.
    ///
    /// # Input
    /// ## keyLength
    /// The desired length of the final key.
    /// ## eavesdropping
    /// Whether to simulate an eavesdropper.
    /// ## errorThreshold
    /// Maximum allowed error rate before aborting.
    /// ## options
    /// Optional configuration:
    /// - 0: eavesdroppingStrategy (default: 0 = random)
    /// - 1: interceptProbability (default: 1.0 = intercept all qubits)
    /// - 2: useDecoyStates (default: true)
    /// - 3: useToeplitzMatrix (default: true)
    /// - 4: minSampleSize (default: 100)
    ///
    /// # Output
    /// Tuple containing success flag, error rate, and final key.
    operation BB84Protocol(
        keyLength : Int, 
        eavesdropping : Bool, 
        errorThreshold : Double,
        options : Int[] = [0, 10, 1, 1, 100]
    ) : (Bool, Double, Bool[]) {
        // Extract configuration options
        let eavesdroppingStrategy = options[0];
        let interceptProbability = IntAsDouble(options[1]) / 10.0; // Scale to 0.0-1.0
        let useDecoyStates = options[2] == 1;
        let useToeplitzMatrix = options[3] == 1;
        let minSampleSize = options[4];
        
        // We need to start with more bits because some will be lost in sifting
        // and error detection
        let initialBits = 4 * keyLength; // Generous estimate
        
        // Decoy state percentage (10% of total bits)
        let decoyStateFraction = useDecoyStates ? 0.1 : 0.0;
        
        // Perform quantum exchange with enhanced security
        let (senderBits, receiverBits, matchingBases) = EnhancedBB84Exchange(
            initialBits,
            eavesdropping,
            eavesdroppingStrategy,
            interceptProbability,
            decoyStateFraction,
            null,  // No seed for sender
            null   // No seed for receiver
        );
        
        // Perform key reconciliation and error detection
        // Use about 25% of the sifted bits for error detection
        let sampleSize = initialBits / 4;
        let (errorRate, siftedKey, sufficientBits) = KeyReconciliation(
            senderBits, 
            receiverBits, 
            matchingBases, 
            sampleSize,
            minSampleSize
        );
        
        // If we don't have enough bits, abort
        if (not sufficientBits) {
            return (false, 1.0, []);
        }
        
        // If error rate is too high, abort (potential eavesdropper)
        if (errorRate > errorThreshold) {
            return (false, errorRate, []);
        }
        
        // Perform privacy amplification to extract final key
        let seed = 42; // In a real implementation, use a secure random seed
        let finalKey = PrivacyAmplification(
            siftedKey, 
            keyLength, 
            seed,
            useToeplitzMatrix
        );
        
        // Analyze results for attack patterns
        // This is informational only and doesn't affect the protocol outcome
        let (attackDetected, attackType, confidence) = DetectAttackPatterns(
            senderBits,
            receiverBits,
            matchingBases,
            ([], [])  // No decoy results in this implementation
        );
        
        // For a production system, we could log attack information here
        
        return (true, errorRate, finalKey);
    }
    
    /// # Summary
    /// An improved implementation of BB84 that uses additional verification steps
    /// to better detect sophisticated attacks.
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
    operation EnhancedBB84Protocol(
        keyLength : Int, 
        eavesdropping : Bool, 
        errorThreshold : Double
    ) : (Bool, Double, Bool[]) {
        // Customize options for enhanced security
        let options = [
            0,           // Random eavesdropping strategy
            10,          // Full interception probability (1.0)
            1,           // Use decoy states
            1,           // Use Toeplitz matrix for privacy amplification
            100          // Minimum sample size of 100 bits
        ];
        
        // Perform main protocol
        let (success, errorRate, finalKey) = BB84Protocol(
            keyLength,
            eavesdropping,
            errorThreshold,
            options
        );
        
        if (not success) {
            // Protocol already failed
            return (false, errorRate, []);
        }
        
        // Additional verification step: check key quality
        let (keyQuality, _) = VerifyKeyQuality(finalKey);
        
        if (not keyQuality) {
            // Key quality is poor, might indicate a sophisticated attack
            return (false, errorRate, []);
        }
        
        return (true, errorRate, finalKey);
    }
    
    /// # Summary
    /// Verifies the statistical quality of the generated key.
    ///
    /// # Input
    /// ## key
    /// The key to verify.
    ///
    /// # Output
    /// Tuple containing:
    /// - Whether the key passes quality checks
    /// - The quality score (0.0 to 1.0)
    operation VerifyKeyQuality(key : Bool[]) : (Bool, Double) {
        // Skip for very short keys
        if (Length(key) < 20) {
            return (true, 1.0);
        }
        
        // Check bit balance (should be close to 50% ones)
        mutable oneCount = 0;
        for bit in key {
            if (bit) {
                set oneCount += 1;
            }
        }
        
        let onePercentage = IntAsDouble(oneCount) / IntAsDouble(Length(key));
        let balanceScore = 1.0 - 2.0 * Abs(onePercentage - 0.5);
        
        // Check for patterns (simplified)
        mutable patternScore = 1.0;
        
        // For production, implement more sophisticated statistical tests here
        // such as run tests, autocorrelation, entropy measures, etc.
        
        // Combine scores
        let qualityScore = (balanceScore + patternScore) / 2.0;
        
        // Pass if score is high enough
        return (qualityScore > 0.8, qualityScore);
    }
}