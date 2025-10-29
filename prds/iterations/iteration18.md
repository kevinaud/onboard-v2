### **Iteration 18: Hardening Diagnostics and Prerequisite Handling**

**Goal:** This iteration focuses on improving the tool's robustness and user experience by fixing critical bugs in the diagnostic and system-check layers. We will resolve a crash in the verbose logging output and completely overhaul the Windows Subsystem for Linux (WSL) prerequisite check to handle modern system configurations and UAC elevation correctly.

---

#### **1. Fix Verbose Mode UI Rendering Crash**

*   **Context:** The `--verbose` flag is intended to show developers the exact external commands the application is running in real-time. This is a critical feature for troubleshooting.
*   **Problem:** When running with `--verbose`, the application crashes internally the moment it tries to print a debug message. The log file reveals an `InvalidOperationException: Could not find color or style 'DEBUG'`. This occurs because our UI library, Spectre.Console, is parsing the literal string `[DEBUG]` as a style instruction (like `[red]`) instead of as plain text.
*   **Solution:**
    1.  Navigate to the `WriteDebug` method within `Onboard.Console/Services/SpectreUserInteraction.cs`.
    2.  Modify the call to `this.console.MarkupLine`. The hardcoded string `[DEBUG]` must be escaped for Spectre.Console by doubling the opening bracket.
    3.  Change `"[grey][DEBUG]..."` to `"[grey][[DEBUG]]..."`. This tells the library to render the literal text `[DEBUG]` instead of trying to apply a "DEBUG" style.

---

#### **2. Implement Robust, UAC-Aware WSL Prerequisite Check**

*   **Context:** The first step on Windows is to verify that WSL2 is properly installed. Our initial implementation used `wsl --status`, which proved to be unreliable. A more accurate method is to check the status of the required Windows optional features directly.
*   **Problem 1: Inaccurate Detection.** The `wsl --status` command can return a non-zero (failure) exit code if the legacy WSL1 component is missing, even when the essential WSL2 components are fully enabled. This causes our tool to fail incorrectly on a perfectly valid WSL2 setup.
*   **Problem 2: Permissions.** The correct way to check these features is with `dism.exe`. However, `dism.exe` requires administrator elevation to run. Our application is designed to run as a **standard user** to ensure all user-specific configurations (like `.gitconfig`) are applied correctly. Instructing the user to run the entire tool from an elevated terminal is not an acceptable solution as it would break other steps.
*   **Solution:** We will implement a "spot elevation" pattern. The application will remain unelevated but will trigger a UAC prompt to run the specific `dism.exe` commands with administrator rights. This requires a careful orchestration because elevated processes cannot have their output directly captured by a standard process.

    1.  **Enhance `IProcessRunner` for Elevation:**
        *   In `Onboard.Core/Abstractions/IProcessRunner.cs`, add a new optional boolean parameter to the `RunAsync` method signature: `bool requestElevation = false`. This will allow any step to request that its command be run with elevated privileges.

    2.  **Implement the Elevation Logic in `ProcessRunner`:**
        *   In `Onboard.Core/Services/ProcessRunner.cs`, refactor the `RunAsync` method to act as a router. If `requestElevation` is `false`, it will call the existing `RunStandardAsync` logic. If `true`, it will call a new `RunElevatedWindowsAsync` method.
        *   The new `RunElevatedWindowsAsync` method will implement the following pattern:
            a. Create a temporary file path using `Path.GetTempFileName()`.
            b. Construct a PowerShell command string that executes the target process (e.g., `dism.exe`) and redirects all of its output (stdout and stderr) to the temporary file. Example: `& 'dism.exe' ... 2>&1 | Out-File -FilePath '...'`.
            c. Create a `ProcessStartInfo` object for `powershell.exe`. Set its `Verb` property to `"runas"` (this is what triggers the UAC prompt) and `UseShellExecute` to `true`. The arguments will be the command string from the previous step.
            d. Start the process and wait for it to exit. This is when the user will see and interact with the UAC prompt.
            e. After the process completes, read the contents of the temporary file to get the command's output.
            f. Clean up by deleting the temporary file.
            g. Return the captured output and exit code in a `ProcessResult` object.

    3.  **Update `EnableWslFeaturesStep` to Use Elevation:**
        *   In `Onboard.Core/Steps/Windows/EnableWslFeaturesStep.cs`, modify the `IsFeatureEnabledAsync` helper method.
        *   The calls to `processRunner.RunAsync` for `dism.exe` must now include the new argument: `requestElevation: true`.
        *   The method should check the status of two features: `Microsoft-Windows-Subsystem-Linux` and `VirtualMachinePlatform`. The overall prerequisite check only passes if both features are enabled *and* the required Ubuntu distribution is found.

---

#### **3. Expand Test Coverage for UI and Process Execution**

*   **Context:** The bugs discovered in this iteration highlight gaps in our testing strategy, specifically around the concrete implementation of the UI and the new, complex process execution logic.
*   **Task 1: Create a Presentation Layer Test Suite.**
    *   Add the `Spectre.Console.Testing` NuGet package to the `Onboard.Core.Tests` project.
    *   Create a new test file, `SpectreUserInteractionTests.cs`.
    *   Using Spectre's `TestConsole`, write a regression test for the verbose mode bug. The test should call `WriteDebug` and assert that the output contains the literal string `[DEBUG]` and does not throw an exception.
    *   Add tests for `WriteSuccess`, `WriteError`, and `WriteWarning` to validate that they produce the expected markup (e.g., `[green]✓ ...[/]`). This will protect against future UI rendering bugs.
*   **Task 2: Update Unit Tests for Elevation Logic.**
    *   In the unit tests for `EnableWslFeaturesStep`, we cannot test the UAC prompt itself. However, we must verify the *intent* to elevate.
    *   Update the tests to verify that the mock `IProcessRunner.RunAsync` method is called with the `requestElevation` parameter set to `true` for the `dism.exe` commands. This ensures the step is correctly requesting the elevation it needs.