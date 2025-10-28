### **Iteration 13: Refactoring Platform-Aware Steps**

**Goal:** Eliminate the `PlatformAwareStep` anti-pattern to adhere to the Single Responsibility Principle and improve maintainability.

1.  **Decompose `InstallVsCodeStep`:**
    *   Create three distinct classes:
        *   `src/Onboard.Core/Steps/Windows/InstallWindowsVsCodeStep.cs` (containing the `winget` logic).
        *   `src/Onboard.Core/Steps/MacOs/InstallMacVsCodeStep.cs` (containing the `brew` logic).
        *   `src/Onboard.Core/Steps/Linux/InstallLinuxVsCodeStep.cs` (containing the `.deb`/`apt` logic).
    *   Ensure each new step implements `IOnboardingStep` directly and contains its specific unit tests.

2.  **Update Composition Root (`Program.cs`):**
    *   Remove the registration for the generic `InstallVsCodeStep`.
    *   Update the orchestrator registrations to inject the specific concrete step they need (e.g., `WindowsOrchestrator` now demands `InstallWindowsVsCodeStep`).

3.  **Remove Legacy Abstraction:**
    *   Delete `src/Onboard.Core/Steps/PlatformAware/PlatformAwareStep.cs` once it is no longer used.
