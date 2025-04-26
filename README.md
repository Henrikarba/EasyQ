# EasyQ

EasyQ is a quantum computing framework designed to make quantum algorithms accessible to regular developers, without requiring specialized knowledge of quantum mechanics or quantum computing principles.

## Project Overview

The core philosophy of EasyQ is to provide a developer-friendly API that abstracts away the complexity of quantum operations, allowing regular software engineers to leverage quantum algorithms in their applications. All operations use pure quantum logic with no classical fallbacks.

EasyQ currently provides:

- **Quantum Search**: Implementation of Grover's algorithm for searching unstructured data
- **Quantum Key Distribution**: E91 protocol implementation for secure communications
- **Quantum Random Number Generation**: True random number generation using quantum properties

## Early Documentation

I've set up some auto-generated docs you can check out here: [EasyQ](https://easyq.henrikarba.eu/)

It's still super early, so it might be a bit rough around the edges.
Once I get the Go and Python packages out, I'll also put together some easier-to-follow docs.

Feel free to reach out if anything's confusing — I'm happy to help!

## Current Status

EasyQ is currently in active development:

- All functionality has been tested through quantum simulation
- Quantum Search works correctly in simulation environments
- Quantum Key Distribution is implemented theoretically but hasn't been verified on actual quantum hardware, so its functionality on real quantum computers is not yet confirmed
- No testing has been performed on actual quantum hardware yet
- Primary focus is on compatibility with Microsoft's quantum computing platform, as indicated by the use of Q#

## Implementation

The framework is built with two main layers:

1. **Quantum Layer** (.qs files): Core quantum operations written in Q#
2. **Bridge Layer** (.cs files): Developer-friendly APIs that abstract quantum complexity

## Requirements

- .NET 9.0 or higher
- Microsoft Quantum Development Kit

## Planned Features

- Package distribution for easy integration
- Language bindings for:
  - Go
  - Python
- Additional quantum algorithms

## Using It

Since EasyQ is not yet available as a package, you'll need to clone the repository and include it in your project

## Testing

The repository includes a test suite that demonstrates the capabilities of EasyQ:

```bash
cd tests
dotnet run
```

## Notes

- This framework has not been tested on real quantum hardware
- The code is designed to be future-proof, allowing for seamless transition to actual quantum computers when available
- Performance in simulation does not reflect actual performance on quantum hardware
