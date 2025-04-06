namespace EasyQ.Quantum.Cryptography {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Random;

    /// # Summary
    /// Prepares a qubit in a state according to the enhanced BB84 protocol.
    /// 
    /// # Input
    /// ## bit
    /// The bit value to encode (0 or 1).
    /// ## basis
    /// The basis to use for encoding (0 for computational, 1 for Hadamard, 2 for Y basis).
    /// ## qubit
    /// The qubit to prepare.
    /// ## enhancedSecurity
    /// Whether to use the enhanced 6-state protocol (using three bases instead of two).
    /// ## noiseProtection
    /// Whether to apply error correction encoding.
    operation PrepareEnhancedState(
        bit : Bool, 
        basis : Int, 
        qubit : Qubit, 
        enhancedSecurity : Bool, 
        noiseProtection : Bool
    ) : Unit {
        // If the bit is 1, apply X to get |1⟩ instead of |0⟩
        if (bit) {
            X(qubit);
        }
        
        // Apply basis transformation
        if (basis == 1) {
            // Hadamard basis (X/Z measurements)
            H(qubit);
        } elif (basis == 2 and enhancedSecurity) {
            // Y basis for 6-state protocol (enhanced security)
            H(qubit);
            S(qubit);
        }
        
        // Apply additional phase randomization for noise protection
        if (noiseProtection) {
            // Random phase to help detect coherent attacks
            let randomPhase = DrawRandomDouble(0.0, 2.0 * PI());
            Rz(randomPhase, qubit);
        }
    }
    
    /// # Summary
    /// Measures a qubit according to the enhanced BB84 protocol.
    ///
    /// # Input
    /// ## qubit
    /// The qubit to measure.
    /// ## basis
    /// The basis to use for measurement (0-2).
    /// ## enhancedSecurity
    /// Whether using the enhanced 6-state protocol with Y-basis.
    ///
    /// # Output
    /// The result of the measurement (true for 1, false for 0).
    operation MeasureEnhancedState(qubit : Qubit, basis : Int, enhancedSecurity : Bool) : Bool {
        // Measurement in appropriate basis
        if (basis == 1) {
            // Hadamard basis
            H(qubit);
        } elif (basis == 2 and enhancedSecurity) {
            // Y basis (only for 6-state protocol)
            Adjoint S(qubit);
            H(qubit);
        }
        
        // Measure in computational basis and convert to bool
        let result = M(qubit) == One;
        
        // Reset qubit to |0⟩ before releasing
        Reset(qubit);
        
        return result;
    }
    
    /// # Summary
    /// Encodes a list of bits using random bases according to the enhanced BB84 protocol.
    ///
    /// # Input
    /// ## bits
    /// The list of bits to encode.
    /// ## enhancedSecurity
    /// Whether to use the 6-state protocol with three bases.
    /// ## decoyStates
    /// Whether to include decoy states to detect photon-number splitting attacks.
    /// ## noiseProtection
    /// Whether to apply phase randomization for protection against coherent attacks.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of chosen bases (to be kept secret initially)
    /// - The list of decoy flags (which qubits are decoys)
    /// - The prepared qubits (to be transmitted to receiver)
    operation EncodeEnhancedMessage(
        bits : Bool[], 
        enhancedSecurity : Bool, 
        decoyStates : Bool, 
        noiseProtection : Bool
    ) : (Int[], Bool[], Qubit[]) {
        let numBits = Length(bits);
        
        // Allocate arrays for bases and decoy flags
        mutable bases = [0, size = numBits];
        mutable decoyFlags = [false, size = numBits];
        
        // Allocate qubits
        use qubits = Qubit[numBits];
        
        // Prepare each qubit according to the bit value and chosen basis
        for i in 0..numBits - 1 {
            // Choose a random basis
            if (enhancedSecurity) {
                // Three bases for 6-state protocol
                set bases w/= i <- DrawRandomInt(0, 2);
            } else {
                // Two bases for standard BB84
                set bases w/= i <- DrawRandomInt(0, 1);
            }
            
            // Determine if this position should be a decoy state
            let isDecoy = decoyStates and DrawRandomBool(0.1); // 10% of bits as decoys
            set decoyFlags w/= i <- isDecoy;
            
            // If it's a decoy state, prepare a special state for detecting attacks
            if (isDecoy) {
                // Create a weak coherent pulse (simulated)
                // In a real system, this would have a different mean photon number
                let decoyBit = DrawRandomBool(0.5);
                PrepareEnhancedState(decoyBit, bases[i], qubits[i], enhancedSecurity, noiseProtection);
            } else {
                // Regular encoding for key bits
                PrepareEnhancedState(bits[i], bases[i], qubits[i], enhancedSecurity, noiseProtection);
            }
        }
        
        return (bases, decoyFlags, qubits);
    }
    
    /// # Summary
    /// Receives and measures qubits according to the enhanced BB84 protocol.
    ///
    /// # Input
    /// ## qubits
    /// The qubits received from sender.
    /// ## enhancedSecurity
    /// Whether using the 6-state protocol.
    ///
    /// # Output
    /// Tuple containing:
    /// - The list of randomly chosen measurement bases
    /// - The measurement results
    operation ReceiveEnhancedMessage(qubits : Qubit[], enhancedSecurity : Bool) : (Int[], Bool[]) {
        let numQubits = Length(qubits);
        
        // Allocate arrays for bases and results
        mutable bases = [0, size = numQubits];
        mutable results = [false, size = numQubits];
        
        // Measure each qubit in the chosen basis
        for i in 0..numQubits - 1 {
            // Choose a random basis
            if (enhancedSecurity) {
                // Three bases for 6-state protocol
                set bases w/= i <- DrawRandomInt(0, 2);
            } else {
                // Two bases for standard BB84
                set bases w/= i <- DrawRandomInt(0, 1);
            }
            
            // Measure the qubit in the chosen basis
            set results w/= i <- MeasureEnhancedState(qubits[i], bases[i], enhancedSecurity);
        }
        
        return (bases, results);
    }
    
    /// # Summary
    /// Simulates an eavesdropper with various attack strategies.
    ///
    /// # Input
    /// ## qubits
    /// The qubits being transmitted.
    /// ## strategy
    /// The eavesdropping strategy (0: intercept-resend, 1: collective, 2: coherent).
    /// ## enhancedSecurity
    /// Whether using the enhanced 6-state protocol.
    operation SimulateEavesdropper(
        qubits : Qubit[], 
        strategy : Int, 
        enhancedSecurity : Bool
    ) : Unit {
        let numQubits = Length(qubits);
        
        if (strategy == 0) {
            // Intercept-resend attack (simplest)
            for i in 0..numQubits - 1 {
                // Eve chooses a random basis
                let eveBasis = enhancedSecurity 
                    ? DrawRandomInt(0, 2) 
                    | DrawRandomInt(0, 1);
                
                // Eve measures in her chosen basis
                let eveResult = MeasureEnhancedState(qubits[i], eveBasis, enhancedSecurity);
                
                // Eve re-prepares the qubit to send to Bob
                Reset(qubits[i]);
                PrepareEnhancedState(eveResult, eveBasis, qubits[i], enhancedSecurity, false);
            }
        } elif (strategy == 1) {
            // Collective attack simulation (simplified)
            // In a real collective attack, Eve would entangle with ancilla qubits
            // but due to Q# simulation constraints, we'll simulate the effect
            
            for i in 0..numQubits - 1 {
                // Apply some arbitrary rotation to simulate partial information gain
                Ry(PI() / 8.0, qubits[i]);
                Rz(PI() / 12.0, qubits[i]);
            }
        } elif (strategy == 2 and enhancedSecurity) {
            // Coherent attack simulation (very simplified)
            // Real coherent attacks would be much more sophisticated
            
            // Apply correlated noise across multiple qubits
            for i in 0..numQubits - 2 {
                // Create some correlation between adjacent qubits
                H(qubits[i]);
                CNOT(qubits[i], qubits[i + 1]);
                Adjoint H(qubits[i]);
                
                // Apply subtle phase shift
                Rz(PI() / 20.0, qubits[i]);
            }
        }
    }
    
    /// # Summary
    /// Simulates the quantum part of the enhanced BB84 protocol.
    ///
    /// # Input
    /// ## bitLength
    /// The number of bits to encode and send.
    /// ## enhancedSecurity
    /// Whether to use the 6-state protocol for enhanced security.
    /// ## decoyStates
    /// Whether to use decoy states to detect photon number splitting attacks.
    /// ## noiseProtection
    /// Whether to apply noise resilience techniques.
    /// ## eavesdropping
    /// If true, simulates an eavesdropper with the specified strategy.
    /// ## eavesdropperStrategy
    /// The strategy the eavesdropper uses (0:intercept-resend, 1:collective, 2:coherent).
    ///
    /// # Output
    /// Tuple containing:
    /// - sender's raw bits
    /// - receiver's raw results
    /// - The bases used by sender
    /// - The bases used by receiver
    /// - Which states were decoys (if decoyStates is true)
    operation SimulateEnhancedBB84(
        bitLength : Int, 
        enhancedSecurity : Bool, 
        decoyStates : Bool, 
        noiseProtection : Bool, 
        eavesdropping : Bool, 
        eavesdropperStrategy : Int
    ) : (Bool[], Bool[], Int[], Int[], Bool[]) {
        // Generate random bits for sender to send
        mutable senderBits = [false, size = bitLength];
        for i in 0..bitLength - 1 {
            set senderBits w/= i <- DrawRandomBool(0.5);
        }
        
        // Sender encodes her bits in random bases
        let (senderBases, decoyFlags, qubits) = EncodeEnhancedMessage(
            senderBits, enhancedSecurity, decoyStates, noiseProtection);
        
        // Simulate eavesdropping if enabled
        if (eavesdropping) {
            SimulateEavesdropper(qubits, eavesdropperStrategy, enhancedSecurity);
        }
        
        // Receiver measures the qubits in random bases
        let (receiverBases, receiverResults) = ReceiveEnhancedMessage(qubits, enhancedSecurity);
        
        return (senderBits, receiverResults, senderBases, receiverBases, decoyFlags);
    }
    
    /// # Summary
    /// Performs sifting to keep only bits where bases match, with additional
    /// analysis for decoy states and security parameter estimation.
    ///
    /// # Input
    /// ## senderBits
    /// The original bits sent by sender.
    /// ## receiverResults
    /// The measurement results obtained by receiver.
    /// ## senderBases
    /// The bases used by sender for encoding.
    /// ## receiverBases
    /// The bases used by receiver for measurement.
    /// ## decoyFlags
    /// Which bits were sent as decoy states.
    /// ## enhancedSecurity
    /// Whether 6-state protocol was used.
    ///
    /// # Output
    /// Tuple containing:
    /// - sifted sender bits
    /// - sifted receiver bits
    /// - decoy state analysis results (error rate on decoys)
    /// - estimated QBER (Quantum Bit Error Rate)
    function PerformAdvancedSifting(
        senderBits : Bool[], 
        receiverResults : Bool[], 
        senderBases : Int[], 
        receiverBases : Int[], 
        decoyFlags : Bool[], 
        enhancedSecurity : Bool
    ) : (Bool[], Bool[], Double, Double) {
        let numBits = Length(senderBits);
        
        // Arrays to store sifted bits
        mutable siftedSenderBits = [];
        mutable siftedReceiverBits = [];
        
        // Track decoy state statistics
        mutable decoyErrors = 0;
        mutable decoyCount = 0;
        
        // Track per-basis error rates for enhanced security analysis
        mutable zBasisErrors = 0;
        mutable zBasisCount = 0;
        mutable xBasisErrors = 0;
        mutable xBasisCount = 0;
        mutable yBasisErrors = 0;
        mutable yBasisCount = 0;
        
        // Process each bit position
        for i in 0..numBits - 1 {
            let basesMatch = senderBases[i] == receiverBases[i];
            let error = senderBits[i] != receiverResults[i];
            
            // Handle decoy states separately
            if (Length(decoyFlags) > i and decoyFlags[i]) {
                set decoyCount += 1;
                
                // If bases match, check for errors in decoy states
                if (basesMatch and error) {
                    set decoyErrors += 1;
                }
            } else {
                // For regular bits, collect statistics by basis
                if (basesMatch) {
                    if (senderBases[i] == 0) {
                        // Z basis
                        set zBasisCount += 1;
                        if (error) {
                            set zBasisErrors += 1;
                        }
                    } elif (senderBases[i] == 1) {
                        // X basis
                        set xBasisCount += 1;
                        if (error) {
                            set xBasisErrors += 1;
                        }
                    } elif (enhancedSecurity and senderBases[i] == 2) {
                        // Y basis (only for 6-state protocol)
                        set yBasisCount += 1;
                        if (error) {
                            set yBasisErrors += 1;
                        }
                    }
                    
                    // Add to sifted key bits
                    set siftedSenderBits += [senderBits[i]];
                    set siftedReceiverBits += [receiverResults[i]];
                }
            }
        }
        
        // Calculate error rates
        let decoyErrorRate = decoyCount > 0 
            ? IntAsDouble(decoyErrors) / IntAsDouble(decoyCount) 
            | 0.0;
        
        // Calculate overall QBER
        let totalMatches = zBasisCount + xBasisCount + (enhancedSecurity ? yBasisCount | 0);
        let totalErrors = zBasisErrors + xBasisErrors + (enhancedSecurity ? yBasisErrors | 0);
        
        let qber = totalMatches > 0 
            ? IntAsDouble(totalErrors) / IntAsDouble(totalMatches) 
            | 0.0;
        
        return (siftedSenderBits, siftedReceiverBits, decoyErrorRate, qber);
    }
    
    /// # Summary
    /// Performs error correction using a simplified cascade-like algorithm.
    ///
    /// # Input
    /// ## senderBits
    /// The sifted bits from the sender.
    /// ## receiverBits
    /// The sifted bits from the receiver.
    /// ## errorRate
    /// The estimated quantum bit error rate.
    ///
    /// # Output
    /// Tuple containing the corrected receiver bits and the number of bits leaked during correction.
    operation PerformErrorCorrection(
        senderBits : Bool[], 
        receiverBits : Bool[], 
        errorRate : Double
    ) : (Bool[], Int) {
        let numBits = Length(senderBits);
        
        // Skip correction if too few bits or excessive errors
        if (numBits < 8 or errorRate > 0.15) {
            return (receiverBits, 0);
        }
        
        // Initialize corrected bits as receiver's bits
        mutable correctedBits = receiverBits;
        
        // Leaked information counter
        mutable leakedBits = 0;
        
        // Use different block sizes for error correction
        let blockSizes = [16, 8, 4, 2];
        
        // For each block size
        for blockSize in blockSizes {
            // Only process if block size doesn't exceed available bits
            if (blockSize <= numBits) {
                // Number of blocks
                let numBlocks = (numBits + blockSize - 1) / blockSize;
                
                // For each block
                for block in 0..numBlocks - 1 {
                    // Calculate block start and end
                    let startIdx = block * blockSize;
                    let endIdx = Min([numBits - 1, startIdx + blockSize - 1]);
                    
                    // Only process valid blocks
                    if (endIdx >= startIdx) {
                        // Calculate parity of sender's block
                        mutable senderParity = false;
                        for i in startIdx..endIdx {
                            set senderParity = senderParity != senderBits[i];
                        }
                        
                        // Calculate parity of receiver's block
                        mutable receiverParity = false;
                        for i in startIdx..endIdx {
                            set receiverParity = receiverParity != correctedBits[i];
                        }
                        
                        // If parities don't match, find and fix an error
                        if (senderParity != receiverParity) {
                            // For simplicity in simulation, flip a random bit in the block
                            // In a real implementation, binary search would locate the error
                            let flipIdx = startIdx + DrawRandomInt(0, endIdx - startIdx);
                            set correctedBits w/= flipIdx <- not correctedBits[flipIdx];
                            
                            // Each parity check leaks approximately 1 bit of information
                            set leakedBits += 1;
                        }
                    }
                }
            }
        }
        
        return (correctedBits, leakedBits);
    }
    
    /// # Summary
    /// Performs privacy amplification to extract a shorter, more secure key.
    ///
    /// # Input
    /// ## rawKey
    /// The raw key after sifting and error correction.
    /// ## finalLength
    /// The desired length of the final key.
    /// ## leakedBits
    /// Number of bits potentially leaked during error correction.
    /// ## qber
    /// Quantum bit error rate (used to estimate eavesdropper's knowledge).
    ///
    /// # Output
    /// The final secure key after privacy amplification.
    operation PerformPrivacyAmplification(
        rawKey : Bool[], 
        finalLength : Int, 
        leakedBits : Int, 
        qber : Double
    ) : Bool[] {
        let rawLength = Length(rawKey);
        
        // Calculate secure key length based on conservative estimates
        // The amount we need to sacrifice depends on:
        // 1. Bits leaked during error correction
        // 2. Eavesdropper's estimated information based on QBER
        
        // Estimate eavesdropper's information (in bits)
        // For BB84, a simple estimate is that the eavesdropper gains qber*2 bits
        let eveInfo = IntAsDouble(leakedBits) + IntAsDouble(rawLength) * qber * 2.0;
        
        // Calculate actual secure length (with safety margin)
        let secureLength = Max([1, Min([finalLength, rawLength - Ceiling(eveInfo) - 4])]);
        
        // Apply toeplitz-like hash function (simplified for simulation)
        mutable finalKey = [false, size = secureLength];
        
        // Generate random seed for universal hash function
        let seed = DrawRandomInt(1, 1000000);
        
        // Apply hash function
        for i in 0..secureLength - 1 {
            // Apply several "hash" operations by XORing bits from various positions
            mutable result = false;
            
            // Create a pseudo-random selection of bits to XOR together
            for j in 0..Min([5, rawLength - 1]) {
                let position = (seed * (i + 1) * (j + 1)) % rawLength;
                set result = result != rawKey[position];
            }
            
            set finalKey w/= i <- result;
        }
        
        return finalKey;
    }
    
    /// # Summary
    /// Creates a message authentication code (MAC) for the final key using 
    /// a pre-shared secret to prevent man-in-the-middle attacks.
    ///
    /// # Input
    /// ## key
    /// The key to authenticate.
    /// ## presharedSecret
    /// A pre-shared secret for authentication.
    ///
    /// # Output
    /// An authentication tag for the key.
    function CreateKeyMAC(key : Bool[], presharedSecret : Bool[]) : Bool[] {
        let tagLength = Min([32, Length(presharedSecret)]);
        mutable tag = [false, size = tagLength];
        
        // Simple universal hash function using the pre-shared secret
        for i in 0..tagLength - 1 {
            mutable result = false;
            
            for j in 0..Length(key) - 1 {
                // Only use the bit if the corresponding secret bit is 1
                if (j < Length(presharedSecret) and presharedSecret[j]) {
                    set result = result != key[(i + j) % Length(key)];
                }
            }
            
            set tag w/= i <- result;
        }
        
        return tag;
    }
    
    /// # Summary
    /// Verifies a message authentication code (MAC) for the final key.
    ///
    /// # Input
    /// ## key
    /// The key to verify.
    /// ## tag
    /// The authentication tag to verify.
    /// ## presharedSecret
    /// A pre-shared secret for authentication.
    ///
    /// # Output
    /// Whether the key is authentic.
    function VerifyKeyMAC(key : Bool[], tag : Bool[], presharedSecret : Bool[]) : Bool {
        let computedTag = CreateKeyMAC(key, presharedSecret);
        
        // Check if tags match
        let tagLength = Min([Length(tag), Length(computedTag)]);
        mutable authentic = Length(tag) == Length(computedTag);
        
        for i in 0..tagLength - 1 {
            if (tag[i] != computedTag[i]) {
                set authentic = false;
            }
        }
        
        return authentic;
    }
    
    /// # Summary
    /// Performs the complete enhanced BB84 protocol simulation.
    ///
    /// # Input
    /// ## keyLength
    /// The desired length of the final key.
    /// ## enhancedSecurity
    /// Whether to use the 6-state protocol for enhanced security.
    /// ## decoyStates
    /// Whether to use decoy states to detect photon-number splitting attacks.
    /// ## noiseProtection
    /// Whether to apply noise resilience techniques.
    /// ## eavesdropping
    /// Whether to simulate an eavesdropper.
    /// ## eavesdropperStrategy
    /// The eavesdropper strategy (0-2) if eavesdropping is enabled.
    /// ## errorThreshold
    /// Maximum allowed error rate before aborting.
    /// ## presharedSecret
    /// A pre-shared secret for authentication (to prevent MITM attacks).
    ///
    /// # Output
    /// Tuple containing:
    /// - Success flag
    /// - Error rate
    /// - Final key
    /// - Authentication tag
    operation EnhancedBB84Protocol(
        keyLength : Int, 
        enhancedSecurity : Bool, 
        decoyStates : Bool, 
        noiseProtection : Bool, 
        eavesdropping : Bool, 
        eavesdropperStrategy : Int, 
        errorThreshold : Double,
        presharedSecret : Bool[]
    ) : (Bool, Double, Bool[], Bool[]) {
        // We need to start with more bits than the final key length
        let initialFactor = decoyStates ? 8 | 5;
        let initialBits = initialFactor * keyLength;
        
        // Perform quantum exchange
        let (senderBits, receiverResults, senderBases, receiverBases, decoyFlags) = 
            SimulateEnhancedBB84(
                initialBits, 
                enhancedSecurity, 
                decoyStates, 
                noiseProtection, 
                eavesdropping, 
                eavesdropperStrategy
            );
        
        // Perform sifting and error estimation
        let (siftedSenderBits, siftedReceiverBits, decoyErrorRate, qber) = 
            PerformAdvancedSifting(
                senderBits, 
                receiverResults, 
                senderBases, 
                receiverBases, 
                decoyFlags, 
                enhancedSecurity
            );
        
        // Check both QBER and decoy state error rates
        let effectiveErrorRate = qber > decoyErrorRate ? qber | decoyErrorRate;
        
        // If error rate is too high, abort
        if (effectiveErrorRate > errorThreshold) {
            return (false, effectiveErrorRate, [false, size = 0], [false, size = 0]);
        }
        
        // Error correction
        // Handle empty sifted bits case
        let (correctedBits, leakedBits) = Length(siftedSenderBits) > 0 and Length(siftedReceiverBits) > 0
            ? PerformErrorCorrection(siftedSenderBits, siftedReceiverBits, qber)
            | ([false, size = 0], 0);
        
        // Privacy amplification - reduce key to protect against partial information leakage
        let finalKey = PerformPrivacyAmplification(
            correctedBits, 
            keyLength, 
            leakedBits, 
            qber
        );
        
        // Create message authentication code to verify against man-in-the-middle attacks
        let authTag = CreateKeyMAC(finalKey, presharedSecret);
        
        return (true, effectiveErrorRate, finalKey, authTag);
    }
    
    /// # Summary
    /// Calculates the binary entropy function.
    ///
    /// # Input 
    /// ## p
    /// Probability value between 0 and 1.
    ///
    /// # Output
    /// The binary entropy H(p) = -p*log(p) - (1-p)*log(1-p).
    function BinaryEntropy(p : Double) : Double {
        if (p <= 0.0 or p >= 1.0) {
            return 0.0;
        }
        
        // Calculate binary entropy using Log base e
        // To convert to log base 2, we divide by Log(2.0)
        let logBase2Conversion = Log(2.0);
        
        // We implement the formula: H(p) = -p*log₂(p) - (1-p)*log₂(1-p)
        // Using log base conversion: log₂(x) = ln(x)/ln(2)
        let term1 = -p * Log(p) / logBase2Conversion;
        let term2 = -(1.0 - p) * Log(1.0 - p) / logBase2Conversion;
        
        return term1 + term2;
    }
    
    /// # Summary
    /// Calculates the minimum value in an array of integers.
    ///
    /// # Input
    /// ## values
    /// Array of integers.
    ///
    /// # Output
    /// The minimum value.
    function MinI(values : Int[]) : Int {
        mutable minValue = 0;
        let len = Length(values);
        
        if (len > 0) {
            set minValue = values[0];
            for i in 1..len - 1 {
                if (values[i] < minValue) {
                    set minValue = values[i];
                }
            }
        }
        
        return minValue;
    }
    
    /// # Summary
    /// Calculates the maximum value in an array of integers.
    ///
    /// # Input
    /// ## values
    /// Array of integers.
    ///
    /// # Output
    /// The maximum value.
    function MaxI(values : Int[]) : Int {
        mutable maxValue = 0;
        let len = Length(values);
        
        if (len > 0) {
            set maxValue = values[0];
            for i in 1..len - 1 {
                if (values[i] > maxValue) {
                    set maxValue = values[i];
                }
            }
        }
        
        return maxValue;
    }
    
    /// # Summary
    /// Converts a Bool array to a readable binary string representation.
    ///
    /// # Input
    /// ## bits
    /// The array of boolean values.
    ///
    /// # Output
    /// A string representation of the bits.
    function BitsToString(bits : Bool[]) : String {
        mutable result = "";
        for bit in bits {
            set result = result + (bit ? "1" | "0");
        }
        return result;
    }
}