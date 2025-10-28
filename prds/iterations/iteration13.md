### **Iteration 13: Refactoring Platform-Aware Steps**

**Goal:** Eliminate the `PlatformAwareStep` anti-pattern to adhere to the Single Responsibility Principle and improve maintainability.

1.  [x] **Decompose `InstallVsCodeStep`:**
    *   [x] Create three distinct classes:
        *   [x] `src/Onboard.Core/Steps/Windows/InstallWindowsVsCodeStep.cs` (containing the `winget` logic).
        *   [x] `src/Onboard.Core/Steps/MacOs/InstallMacVsCodeStep.cs` (containing the `brew` logic).
        *   [x] `src/Onboard.Core/Steps/Linux/InstallLinuxVsCodeStep.cs` (containing the `.deb`/`apt` logic).
    *   [x] Ensure each new step implements `IOnboardingStep` directly and contains its specific unit tests.

2.  [x] **Update Composition Root (`Program.cs`):**
    *   [x] Remove the registration for the generic `InstallVsCodeStep`.
    *   [x] Update the orchestrator registrations to inject the specific concrete step they need (e.g., `WindowsOrchestrator` now demands `InstallWindowsVsCodeStep`).

3.  [x] **Remove Legacy Abstraction:**
    *   [x] Delete `src/Onboard.Core/Steps/PlatformAware/PlatformAwareStep.cs` once it is no longer used.
