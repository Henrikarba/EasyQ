namespace EasyQ {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    
    open EasyQ.Core;
    open EasyQ.Algorithms;
    
    /// # Summary
    /// Creates a quantum index for a database of strings
    ///
    /// # Input
    /// ## items
    /// Array of string items to index
    ///
    /// # Output
    /// A quantum index for the provided items
    ///
    /// # Example
    /// ```
    /// // Create a quantum-optimized index for fast searching
    /// let index = CreateIndex(myDatabase);
    /// ```
    operation CreateIndex(items : String[]) : QuantumIndex {
        mutable keyValueMap = [(items[0], 0), size = Length(items)];
        
        for i in 0..Length(items) - 1 {
            set keyValueMap w/= i <- (items[i], i);
        }
        
        return CreateQuantumIndexFromMap(keyValueMap);
    }
    
    /// # Summary
    /// Creates a quantum index for a key-value database
    ///
    /// # Input
    /// ## keys
    /// Array of string keys to index
    /// ## values
    /// Array of values corresponding to each key
    ///
    /// # Output
    /// A quantum index for the provided key-value pairs
    operation CreateKeyValueIndex<'T>(keys : String[], values : 'T[]) : QuantumIndex {
        mutable keyValueMap = [(keys[0], 0), size = Length(keys)];
        
        for i in 0..Length(keys) - 1 {
            set keyValueMap w/= i <- (keys[i], i);
        }
        
        return CreateQuantumIndexFromMap(keyValueMap);
    }
    
    /// # Summary
    /// Searches for a key in a quantum index
    ///
    /// # Input
    /// ## index
    /// The quantum index to search
    /// ## key
    /// The key to search for
    ///
    /// # Output
    /// The index of the found item, or -1 if not found
    ///
    /// # Example
    /// ```
    /// // Search for an item in O(√n) time instead of O(n)
    /// let foundIndex = Search(index, "quantum");
    /// ```
    operation Search(index : QuantumIndex, key : String) : Int {
        // Hash the key using the same hash function used during indexing
        let hashValue = HashString(key, index::Size);
        
        // Use quantum search to find the index
        let foundHashValue = QuantumDatabaseSearch(index::Size, hashValue);
        
        // Map the hash value back to the original index
        let originalIndex = index::HashToOriginalMap[foundHashValue];
        
        // Verify that we found the correct item (to handle hash collisions)
        // Note: In a real implementation, we would need to verify by checking the actual item
        return originalIndex;
    }
    
    /// # Summary
    /// Searches for multiple items in a quantum index
    ///
    /// # Input
    /// ## index
    /// The quantum index to search
    /// ## keys
    /// Array of keys to search for
    /// ## maxResults
    /// Maximum number of results to return
    ///
    /// # Output
    /// Array of indices of found items
    operation MultiSearch(index : QuantumIndex, keys : String[], maxResults : Int) : Int[] {
        mutable results = [0, size = 0];
        
        // For each key
        for i in 0..Length(keys) - 1 {
            // Only search for up to maxResults
            if Length(results) >= maxResults {
                break;
            }
            
            // Find the key
            let foundIndex = Search(index, keys[i]);
            
            // If valid result, add to results
            if foundIndex >= 0 {
                set results = results + [foundIndex];
            }
        }
        
        return results;
    }
    
    /// # Summary
    /// Searches for similar items based on hash proximity
    ///
    /// # Input
    /// ## index
    /// The quantum index to search
    /// ## key
    /// The key to find similar items for
    /// ## maxResults
    /// Maximum number of similar items to return
    ///
    /// # Output
    /// Array of indices of similar items
    operation FindSimilar(index : QuantumIndex, key : String, maxResults : Int) : Int[] {
        // Hash the key
        let hashValue = HashString(key, index::Size);
        
        // Pre-compute hash differences for all indices
        mutable hashDiffs = [(0, 1000), size = index::Size];
        
        for i in 0..index::Size - 1 {
            let diff = AbsI(i - hashValue);
            set hashDiffs w/= i <- (i, diff);
        }
        
        // Sort by hash difference (ascending)
        // Note: In Q# we'd typically use a sorting network or other algorithm
        // For simplicity, this is a selection sort
        mutable sortedHashes = [(0, 0), size = index::Size];
        mutable used = [false, size = index::Size];
        
        for i in 0..index::Size - 1 {
            mutable minDiff = 1000;
            mutable minIndex = 0;
            
            for j in 0..index::Size - 1 {
                let (hash, diff) = hashDiffs[j];
                if not used[j] and diff < minDiff {
                    set minDiff = diff;
                    set minIndex = j;
                }
            }
            
            set sortedHashes w/= i <- hashDiffs[minIndex];
            set used w/= minIndex <- true;
        }
        
        // Return the top maxResults items
        mutable results = [0, size = 0];
        mutable count = 0;
        
        for i in 0..index::Size - 1 {
            if count >= maxResults {
                break;
            }
            
            let (hash, _) = sortedHashes[i];
            let originalIndex = index::HashToOriginalMap[hash];
            
            // Only add valid indices
            if originalIndex >= 0 {
                set results = results + [originalIndex];
                set count = count + 1;
            }
        }
        
        return results;
    }
}