namespace EasyQ.Quantum.Search {
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Measurement;

    /// # Summary
    /// Performs Grover's search algorithm to find a marked item in an unstructured database.
    /// 
    /// # Input
    /// ## numQubits
    /// The number of qubits needed to represent the search space (log2 of size).
    /// ## markedItemFunction
    /// A function that marks the item being searched for (oracle).
    /// ## iterations
    /// The number of Grover iterations to perform. If not specified, uses the optimal number.
    ///
    /// # Output
    /// The index of the marked item, if found.
    operation GroverSearch(
        numQubits : Int,
        markedItemFunction : ((Qubit[]) => Unit is Adj),
        iterations : Int
    ) : Int {
        use qubits = Qubit[numQubits];

        // Initialize qubits in superposition
        ApplyToEachA(H, qubits);
        
        // Apply Grover iterations
        for _ in 1..iterations {
            // Oracle - marks the item we're looking for
            markedItemFunction(qubits);
            
            // Diffusion operator - amplifies the marked state
            ApplyGroverDiffusionOperator(qubits);
        }
        
        // Measure the register to get a result
        let result = MeasureAllQubits(qubits);
        
        // Reset all qubits before releasing
        ResetAll(qubits);
        
        return result;
    }

    operation MeasureAllQubits(qubits : Qubit[]) : Int {
        let results = ForEach(MResetZ, qubits);
        let bits = Mapped(ResultAsBool, results);
        return BoolArrayAsInt(bits);
    }

    /// # Summary
    /// Performs the diffusion step of Grover's algorithm, which amplifies 
    /// the amplitude of the marked state.
    operation ApplyGroverDiffusionOperator(qubits : Qubit[]) : Unit is Adj {
        within {
            ApplyToEachA(H, qubits);
            ApplyToEachA(X, qubits);
        } apply {
            Controlled Z(Most(qubits), Tail(qubits));
        }
        ApplyToEachA(X, qubits);
        ApplyToEachA(H, qubits);
    }
    
    /// # Summary
    /// Creates an oracle function for a specific target item.
    /// 
    /// # Input
    /// ## numQubits
    /// The number of qubits in the state register.
    /// ## targetItem
    /// The index of the item to mark.
    ///
    /// # Output
    /// A unitary operation that marks the target item.
    function CreateOracleForItem(numQubits : Int, targetItem : Int) : ((Qubit[]) => Unit is Adj) {
    return (qubits => Oracle(qubits, targetItem));
}
    
    /// # Summary
    /// Oracle implementation that marks a specific target item.
    operation Oracle(qubits : Qubit[], targetItem : Int) : Unit is Adj {
        // Convert target to binary and check if each bit matches
        let targetBits = IntAsBoolArray(targetItem, Length(qubits));
        
        // Apply Z to mark the target state
        // First apply X gates to qubits where targetBits are 0
        ApplyPauliFlipToMatchTarget(qubits, targetBits);
        
        // Apply a multi-controlled Z gate to mark the specific item
        Controlled Z(Most(qubits), Tail(qubits));
        
        // Undo the X gates
        ApplyPauliFlipToMatchTarget(qubits, targetBits);
    }
    
    /// # Summary
    /// Applies X gates to qubits where the target bits are 0.
    /// This transforms the target state to the state |11...1>.
    operation ApplyPauliFlipToMatchTarget(qubits : Qubit[], targetBits : Bool[]) : Unit is Adj {
        for i in 0..Length(qubits) - 1 {
            if (not targetBits[i]) {
                X(qubits[i]);
            }
        }
    }
    
    /// # Summary
    /// Calculates the optimal number of Grover iterations for a given problem size.
    ///
    /// # Input
    /// ## numItems
    /// The total number of items in the search space.
    /// ## numMarkedItems
    /// The estimated number of items that match the search criteria.
    ///
    /// # Output
    /// The optimal number of iterations to maximize the probability of finding a marked item.
    function CalculateOptimalIterations(numItems : Int, numMarkedItems : Int) : Int {
        let angle = ArcSin(Sqrt(IntAsDouble(numMarkedItems) / IntAsDouble(numItems)));
        let iterations = Round(PI() / (4.0 * angle) - 0.5);
        return Max([1, iterations]);
    }
}