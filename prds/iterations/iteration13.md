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

## Baseline Review

- `PlatformAwareStep` previously multiplexed per-OS installers for Visual Studio Code and added hidden state to the constructor.
- Orchestrators on every platform consumed the shared `InstallVsCodeStep`; a change for one OS risked side-effects on the others.
- Unit tests for the original step mocked platform facts rather than the real command invocations required on each operating system.

## Decomposition Plan

- Promote discrete `Install<Platform>VsCodeStep` classes so each step expresses the native package manager and idempotency check explicitly.
- Update dependency injection to register the new concrete types and remove the obsolete shared installer.
- Keep orchestrator ordering identical while swapping in the correct VS Code installer for each runtime target.
- Port the NUnit coverage from the shared step into targeted suites that validate success, skip, and failure cases per platform.

## Delivered Outcome

- Onboard.Core now provides `InstallWindowsVsCodeStep`, `InstallMacVsCodeStep`, and `InstallLinuxVsCodeStep` alongside focused unit tests in the matching folders.
- `Program.cs` registers each installer explicitly and the orchestrators consume the correct type through constructor injection.
- The deprecated `PlatformAwareStep` abstraction and its dependencies have been removed from the codebase.
