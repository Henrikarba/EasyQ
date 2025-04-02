namespace EasyQ.Algorithms {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arrays;
    open EasyQ.Core;
    
    /// # Summary
    /// Searches for a specific word in a virtual database of words
    /// using quantum search algorithm.
    ///
    /// # Input
    /// ## databaseSize
    /// The size of the quantum database to use (must be a power of 2)
    /// ## wordToFind
    /// The word to search for
    /// ## allWords
    /// The complete list of words that could be in the database
    ///
    /// # Output
    /// The index/hash of the word in the database
    operation SearchWord(databaseSize : Int, wordToFind : String, allWords : String[]) : Int {
        // Calculate the hash of the word we're looking for
        let targetHash = HashString(wordToFind, databaseSize);
        
        // Use the quantum search to find this hash
        let foundHash = QuantumDatabaseSearch(databaseSize, targetHash);
        
        return foundHash;
    }
    
    /// # Summary
    /// Searches for a word in a list and returns the matching word
    ///
    /// # Input
    /// ## databaseSize
    /// The size of the quantum database (must be a power of 2 and >= length of wordList)
    /// ## wordToFind
    /// The word to search for
    /// ## wordList
    /// The list of words to search through
    ///
    /// # Output
    /// (Bool, String) - Success flag and the found word (or empty if not found)
    operation LookupWord(databaseSize : Int, wordToFind : String, wordList : String[]) : (Bool, String) {
        // First hash the target word
        let targetHash = HashString(wordToFind, databaseSize);
        
        // Use quantum search to find the hash
        let foundHash = QuantumDatabaseSearch(databaseSize, targetHash);
        
        // Check if any word in our list matches this hash
        mutable foundWord = "";
        mutable found = false;
        
        for word in wordList {
            if HashString(word, databaseSize) == foundHash and word == wordToFind {
                set foundWord = word;
                set found = true;
                break;
            }
        }
        
        return (found, foundWord);
    }
    
    /// # Summary
    /// Finds similar words based on hash proximity
    ///
    /// # Input
    /// ## databaseSize
    /// The size of the quantum database
    /// ## queryWord
    /// The query word to find similar matches for
    /// ## wordList
    /// The list of all words to match against
    /// ## maxResults
    /// Maximum number of similar words to return
    ///
    /// # Output
    /// Array of similar words
    operation FindSimilarWords(
        databaseSize : Int, 
        queryWord : String, 
        wordList : String[], 
        maxResults : Int
    ) : String[] {
        // Get the hash of our query word
        let queryHash = HashString(queryWord, databaseSize);
        
        // Pre-compute all word hashes
        let wordHashes = MapWordsToIndices(wordList, databaseSize);
        
        // For simplicity, we'll just simulate the quantum part here
        // In a full implementation, we would use quantum search with multiple targets
        mutable similarWords = ["", size = 0];
        
        // Find words with hashes close to our query hash
        for i in 0..Length(wordList) - 1 {
            let hashDiff = AbsI(wordHashes[i] - queryHash);
            
            // If hash is close or exact match (but not the same word)
            if hashDiff < 3 and wordList[i] != queryWord {
                set similarWords = similarWords + [wordList[i]];
                
                // Break if we have enough results
                if Length(similarWords) >= maxResults {
                    break;
                }
            }
        }
        
        return similarWords;
    }
}