namespace EasyQ.Core {
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;
    
    /// # Summary
    /// Represents a complex number with real and imaginary parts
    newtype Complex = (
        Real : Double,
        Imaginary : Double
    );
    
    /// # Summary
    /// Represents a quantum state with amplitude and phase
    newtype QuantumState = (
        Amplitude : Double,
        Phase : Double
    );
    
    /// # Summary
    /// Represents a database entry with index and value
    newtype DatabaseEntry = (
        Index : Int,
        Value : String
    );
    
    /// # Summary
    /// Creates a new Complex number
    ///
    /// # Input
    /// ## real
    /// The real part of the complex number
    /// ## imaginary
    /// The imaginary part of the complex number
    ///
    /// # Output
    /// A Complex number with the specified real and imaginary parts
    function CreateComplex(real : Double, imaginary : Double) : Complex {
        return Complex(real, imaginary);
    }
    
    /// # Summary
    /// Creates a new QuantumState
    ///
    /// # Input
    /// ## amplitude
    /// The amplitude of the quantum state
    /// ## phase
    /// The phase of the quantum state in radians
    ///
    /// # Output
    /// A QuantumState with the specified amplitude and phase
    function CreateQuantumState(amplitude : Double, phase : Double) : QuantumState {
        return QuantumState(amplitude, phase);
    }
    
    /// # Summary
    /// Creates a new DatabaseEntry
    ///
    /// # Input
    /// ## index
    /// The index of the database entry
    /// ## value
    /// The value of the database entry
    ///
    /// # Output
    /// A DatabaseEntry with the specified index and value
    function CreateDatabaseEntry(index : Int, value : String) : DatabaseEntry {
        return DatabaseEntry(index, value);
    }
    
    /// # Summary
    /// Adds two Complex numbers
    ///
    /// # Input
    /// ## a
    /// The first Complex number
    /// ## b
    /// The second Complex number
    ///
    /// # Output
    /// The sum of the two Complex numbers
    function AddComplex(a : Complex, b : Complex) : Complex {
        return Complex(
            a::Real + b::Real,
            a::Imaginary + b::Imaginary
        );
    }
    
    /// # Summary
    /// Multiplies two Complex numbers
    ///
    /// # Input
    /// ## a
    /// The first Complex number
    /// ## b
    /// The second Complex number
    ///
    /// # Output
    /// The product of the two Complex numbers
    function MultiplyComplex(a : Complex, b : Complex) : Complex {
        let realPart = a::Real * b::Real - a::Imaginary * b::Imaginary;
        let imaginaryPart = a::Real * b::Imaginary + a::Imaginary * b::Real;
        return Complex(realPart, imaginaryPart);
    }
}