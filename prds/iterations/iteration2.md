### **Iteration 2: Core Abstractions & Services**

**Goal:** Define all core interfaces and models, and implement the concrete services for external interactions. This builds the testable foundation.

1.  **Define Models:**
    *   Create `src/Onboard.Core/Models/ProcessResult.cs` exactly as defined in the design document.
    *   Create `src/Onboard.Core/Models/PlatformFacts.cs` exactly as defined in the design document.

2.  **Define Abstractions (Interfaces):**
    *   Create `src/Onboard.Core/Abstractions/IProcessRunner.cs`.
    *   Create `src/Onboard.Core/Abstractions/IUserInteraction.cs`.
    *   Create `src/Onboard.Core/Abstractions/IPlatformDetector.cs`.
    *   Create `src/Onboard.Core/Abstractions/IOnboardingStep.cs`.
    *   Create `src/Onboard.Core/Abstractions/IPlatformOrchestrator.cs`.

3.  **Implement Concrete Services:**
    *   Implement `src/Onboard.Core/Services/ProcessRunner.cs`. This class will implement `IProcessRunner` and use `System.Diagnostics.Process` to execute commands.
    *   Implement `src/Onboard.Core/Services/ConsoleUserInteraction.cs`. This class will implement `IUserInteraction` and use `System.Console` for all output. Implement colored output for headers, successes, warnings, and errors.
    *   Implement `src/Onboard.Core/Services/PlatformDetector.cs`. This class will implement `IPlatformDetector`. Use `System.Runtime.InteropServices.RuntimeInformation` to detect OS and architecture. Check for the `WSL_DISTRO_NAME` environment variable to set the `IsWsl` flag.
