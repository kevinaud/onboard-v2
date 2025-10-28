### **iterations.md**

This document provides a sequential, iterative plan for migrating the legacy onboarding system to the new **Onboard Pro** C# architecture. Each iteration represents a discrete set of tasks that should be completed and verified before proceeding to the next.

---

### **Iteration 1: Project Scaffolding & Foundational Setup**

**Goal:** Establish the complete directory structure, solution, and projects. Configure the development environment.

1.  **Create the Root Directory:**
    *   Create a new directory named `onboard-pro`.

2.  **Initialize .NET Solution and Projects:**
    *   Inside `onboard-pro`, create the solution file:
        ```bash
        dotnet new sln -n Onboard
        ```
    *   Create the `src` directory.
    *   Create the main console application project:
        ```bash
        dotnet new console -n Onboard.Console -o src/Onboard.Console
        ```
    *   Create the core logic class library:
        ```bash
        dotnet new classlib -n Onboard.Core -o src/Onboard.Core
        ```
    *   Create the unit test project:
        ```bash
        dotnet new nunit -n Onboard.Core.Tests -o tests/Onboard.Core.Tests
        ```

3.  **Link Projects to Solution:**
    *   Add all three projects to the solution:
        ```bash
        dotnet sln add src/Onboard.Console/Onboard.Console.csproj
        dotnet sln add src/Onboard.Core/Onboard.Core.csproj
        dotnet sln add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj
        ```

4.  **Establish Project References:**
    *   `Onboard.Console` must reference `Onboard.Core`:
        ```bash
        dotnet add src/Onboard.Console/Onboard.Console.csproj reference src/Onboard.Core/Onboard.Core.csproj
        ```
    *   `Onboard.Core.Tests` must reference `Onboard.Core`:
        ```bash
        dotnet add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj reference src/Onboard.Core/Onboard.Core.csproj
        ```

5.  **Install NuGet Packages:**
    *   For `Onboard.Console`:
        ```bash
        dotnet add src/Onboard.Console/Onboard.Console.csproj package Microsoft.Extensions.Hosting
        ```    *   For `Onboard.Core.Tests`:
        ```bash
        dotnet add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj package Moq
        ```
        *(NUnit and the Test SDK are already included by the `nunit` template).*

6.  **Create Directory Structure:**
    *   Create the directory structure specified in the design document within the `src` and `tests` directories. You can use `mkdir -p`. At the end of this step, the file system should match the layout in section 3.1 of the design document, even if the files are empty.

7.  **Configure Dev Container:**
    *   Create the `.devcontainer/devcontainer.json` file.
    *   Configure it to use the `mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm` image as specified.

---

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

---

### **Iteration 3: Dependency Injection & The Composition Root**

**Goal:** Wire all services together in `Program.cs` and implement the orchestrator selection logic.

1.  **Implement `Program.cs`:**
    *   Replace the contents of `src/Onboard.Console/Program.cs` with the code from section 4.5 of the design document.
    *   Create empty placeholder classes for all orchestrators (`WindowsOrchestrator`, `MacOsOrchestrator`, etc.) in `src/Onboard.Console/Orchestrators/` so the DI registration works. Each should have an empty `ExecuteAsync` method.
    *   Create empty placeholder classes for the initial onboarding steps (`ConfigureGitUserStep`, `InstallVsCodeStep`, etc.) so the DI registration works.
    *   At this stage, running the application should correctly detect the platform and select an orchestrator, which will then do nothing. For example, running on Windows should print "Starting Windows Host Onboarding..." and then exit.

---

### **Iteration 4: The First Onboarding Step (Git Identity)**

**Goal:** Implement a complete, shared, and unit-tested onboarding step.

1.  **Implement `ConfigureGitUserStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/Shared/ConfigureGitUserStep.cs`.
    *   Inject `IProcessRunner` and `IUserInteraction`.
    *   **`ShouldExecuteAsync()`:** Run `git config --global user.name`. If the process returns a non-zero exit code, this method should return `true`.
    *   **`ExecuteAsync()`:** Use `_ui.Prompt(...)` to ask for the user's name and email. Then, use `_processRunner.RunAsync(...)` to execute `git config --global user.name "..."` and `git config --global user.email "..."`.

2.  **Write Unit Tests:**
    *   Create `tests/Onboard.Core.Tests/Steps/ConfigureGitUserStepTests.cs`.
    *   Write a test for the "happy path" where the config is missing, the user provides input, and the correct `git` commands are executed. Use `Moq` to mock `IProcessRunner` and `IUserInteraction`.
    *   Write a test for the case where the config already exists. `ShouldExecuteAsync` should return `false`, and `ExecuteAsync` should not be called.

3.  **Update Orchestrators:**
    *   Inject `ConfigureGitUserStep` into the constructor of `WindowsOrchestrator`, `MacOsOrchestrator`, `UbuntuOrchestrator`, and `WslGuestOrchestrator`.
    *   Add the step to the `_steps` collection in each orchestrator.

---

### **Iteration 5: The First Platform-Aware Step (Install VS Code)**

**Goal:** Implement the `PlatformAwareStep` pattern to handle OS-specific logic within a single step.

1.  **Implement `PlatformAwareStep.cs`:**
    *   Create the abstract class `src/Onboard.Core/Steps/PlatformAware/PlatformAwareStep.cs` exactly as defined in the design document.

2.  **Implement `InstallVsCodeStep.cs`:**
    *   Create `src/Onboard.Core/Steps/PlatformAware/InstallVsCodeStep.cs`, inheriting from `PlatformAwareStep`.
    *   **`Configure()`:**
        *   **Windows Strategy:**
            *   `ShouldExecute`: Check if `code.cmd` exists on the PATH.
            *   `Execute`: Run `winget install --id Microsoft.VisualStudioCode -e --source winget`.
        *   **macOS Strategy:**
            *   `ShouldExecute`: Check if `code` exists on the PATH or if `/Applications/Visual Studio Code.app` exists.
            *   `Execute`: Run `brew install --cask visual-studio-code`.
        *   **Linux Strategy:**
            *   `ShouldExecute`: Check if `code` exists on the PATH.
            *   `Execute`: Replicate the .deb download and install logic from the legacy `install_prereqs_wsl.sh` script.
            ```bash
            # Reference commands
            curl -L "https://update.code.visualstudio.com/latest/linux-deb-x64/stable" -o "/tmp/vscode.deb"
            sudo apt-get install -y "/tmp/vscode.deb"
            rm -f "/tmp/vscode.deb"
            ```

3.  **Update Orchestrators:**
    *   Inject `InstallVsCodeStep` into the relevant orchestrators and add it to their step sequence.

---

### **Iteration 6: Windows Host Onboarding**

**Goal:** Fully implement the `WindowsOrchestrator` with all its required steps.

1.  **Create `InstallGitForWindowsStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/Windows/`.
    *   `ShouldExecute`: Check for `git.exe` on the PATH.
    *   `Execute`: Run `winget install --id Git.Git -e --source winget`.

2.  **Create `InstallDockerDesktopStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/Windows/`.
    *   `ShouldExecute`: Check for `Docker Desktop.exe` in Program Files.
    *   `Execute`: Run `winget install --id Docker.DockerDesktop -e --source winget`.

3.  **Create `EnableWslFeaturesStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/Windows/`.
    *   This is a critical step. It must be idempotent.
    *   `ShouldExecute`: Use `dism.exe` to check the status of the required features.
        ```powershell
        # Reference command to check status (parse output)
        dism.exe /online /get-featureinfo /featurename:Microsoft-Windows-Subsystem-Linux
        ```
    *   `Execute`: Run the `dism.exe` commands to enable the features. The app may need to detect if it's running as an administrator and warn the user if not.
        ```powershell
        # Reference commands
        dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
        dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
        ```

4.  **Update `WindowsOrchestrator`:**
    *   Inject and order all the new Windows-specific steps. The logical order should be: `EnableWslFeaturesStep`, `InstallGitForWindowsStep`, `InstallVsCodeStep`, `InstallDockerDesktopStep`, `ConfigureGitUserStep`.

---

### **Iteration 7: WSL Guest Onboarding**

**Goal:** Fully implement the `WslGuestOrchestrator` and the `--mode wsl-guest` logic.

1.  **Finalize `--mode` Flag Parsing:**
    *   Ensure the logic in `Program.cs` correctly identifies the `--mode wsl-guest` flag and that this properly selects the `WslGuestOrchestrator` when run inside WSL.

2.  **Create `AptUpdateStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/WslGuest/`.
    *   `ShouldExecute`: Always return `true` for simplicity, or check the age of the apt cache files in `/var/lib/apt/lists/`.
    *   `Execute`: Run `sudo apt-get update`.

3.  **Create `InstallWslPrerequisitesStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/WslGuest/`.
    *   `ShouldExecute`: Check for the presence of a key package like `build-essential`.
    *   `Execute`: Install all required packages.
        ```bash
        # Reference command from legacy install_prereqs_wsl.sh
        sudo apt-get install -y git gh curl chezmoi python3 build-essential
        ```

4.  **Create `ConfigureWslGitCredentialHelperStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/WslGuest/`.
    *   `ShouldExecute`: Run `git config --global credential.helper` and check if the output matches the target value.
    *   `Execute`: Set the credential helper to point to the Windows GCM.
        ```bash
        # Reference command
        git config --global credential.helper '/mnt/c/Program\ Files/Git/mingw64/bin/git-credential-manager.exe'
        ```

5.  **Update `WslGuestOrchestrator`:**
    *   Inject and order the new steps: `AptUpdateStep`, `InstallWslPrerequisitesStep`, `InstallVsCodeStep` (it's platform-aware and will do the Linux install), `ConfigureWslGitCredentialHelperStep`, `ConfigureGitUserStep`.

---

### **Iteration 8: macOS & Ubuntu Onboarding**

**Goal:** Implement the orchestrators for native macOS and Ubuntu.

1.  **Create `InstallHomebrewStep.cs` (macOS):**
    *   Location: `src/Onboard.Core/Steps/MacOs/`.
    *   `ShouldExecute`: Check for `brew` on the PATH.
    *   `Execute`: Run the official Homebrew installation script.
        ```bash
        # Reference command
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        ```

2.  **Create `InstallBrewPackagesStep.cs` (macOS):**
    *   Location: `src/Onboard.Core/Steps/MacOs/`.
    *   `ShouldExecute`: Check for a key package like `gh`.
    *   `Execute`: Install all required packages.
        ```bash
        # Reference command
        brew install git gh chezmoi
        ```

3.  **Create `InstallAptPackagesStep.cs` (Ubuntu):**
    *   This will be very similar to `InstallWslPrerequisitesStep.cs` but for the native Ubuntu context.

4.  **Update `MacOsOrchestrator`:**
    *   Inject and order steps: `InstallHomebrewStep`, `InstallBrewPackagesStep`, `InstallVsCodeStep`, `ConfigureGitUserStep`.

5.  **Update `UbuntuOrchestrator`:**
    *   Inject and order steps: `AptUpdateStep`, `InstallAptPackagesStep`, `InstallVsCodeStep`, `ConfigureGitUserStep`.

---

### **Iteration 9: Project Cloning Step**

**Goal:** Create the final major onboarding step to clone the project repository.

1.  **Implement `CloneProjectRepoStep.cs`:**
    *   Location: `src/Onboard.Core/Steps/Shared/`.
    *   This step is complex. It needs to be aware of the file system. You will need to add an `IFileSystem` abstraction to be fully testable, or accept minor untestability here for simplicity.
    *   **Logic:**
        1.  Define the target repository URL and local path (`~/projects/mental-health-app-frontend`).
        2.  `ShouldExecute`: Return `true` if the target path does not exist. If it does exist and is a Git repository, also return `true` (to trigger an update). If it exists and is *not* a Git repository, return `false` and issue a warning.
        3.  `Execute`:
            *   If the directory doesn't exist, run `git clone <url> <path>`.
            *   If it is a valid Git repo, `cd` into it and run `git pull --ff-only`.
            *   If the parent directory (`~/projects`) doesn't exist, create it first.

2.  **Update Orchestrators:**
    *   Add the `CloneProjectRepoStep` as one of the final steps in the `MacOsOrchestrator`, `UbuntuOrchestrator`, and `WslGuestOrchestrator`.

---

### **Iteration 10: Bootstrapper Scripts & Release Workflow**

**Goal:** Create the user-facing entry points and the automated build pipeline.

1.  **Create `setup.sh`:**
    *   Create a new `setup.sh` file in the project root.
    *   Implement the logic exactly as described in section 6.2 of the design document. It must detect OS/Arch, construct a URL to a GitHub Release, download the binary, make it executable, and pass all command-line arguments (`$@`) to it.

2.  **Create `setup.ps1`:**
    *   Create a new `setup.ps1` file in the project root.
    *   Implement the logic from section 6.2. It will download the Windows binary to `$env:TEMP` and execute it.

3.  **Create Release Workflow:**
    *   Create `.github/workflows/release.yml`.
    *   Configure it to trigger on new tags (e.g., `v*.*.*`).
    *   Implement a matrix build strategy for all target platforms (`win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`).
    *   Each job in the matrix must run the corresponding `dotnet publish` command from section 6.1 of the design document.
    *   Use a community action (e.g., `actions/upload-release-asset`) to upload the compiled binaries to the created GitHub Release.

---

### **Iteration 11: Final Documentation and Polish**

**Goal:** Finalize the project with user-facing documentation and a review of all output.

1.  **Create `README.md`:**
    *   Write a clear, concise `README.md` for the `onboard-pro` project.
    *   Include the one-liner for macOS/Linux/WSL.
    *   Provide the detailed, multi-step instructions for the Windows host setup process.
    *   Explain the purpose of the project and its architecture.

2.  **Review User-Facing Text:**
    *   Go through every string literal passed to `IUserInteraction` methods (`WriteHeader`, `WriteSuccess`, etc.).
    *   Ensure the language is clear, helpful, and consistent across all steps and orchestrators.

3.  **Error Handling Review:**
    *   Review the `try/catch` block in `Program.cs` and the general error handling in the orchestrators. Ensure that if a step fails, the application exits gracefully with a meaningful error message.
Here are the additional iterations to address the most pressing issues, formatted for inclusion in `iterations.md`.

***

### **Iteration 12: Reliability and Diagnostics**

**Goal:** Improve the robustness of external process execution and add persistent logging to aid in debugging user issues.

1.  **Enhance `IProcessRunner` for Linux:**
    *   Modify `ProcessRunner.cs` to automatically inject the `DEBIAN_FRONTEND=noninteractive` environment variable into the process start info when running on Linux. This prevents `apt-get` from hanging on standard input requests (like `tzdata` configuration).

2.  **Implement Persistent Logging:**
    *   Introduce `Serilog` and `Serilog.Sinks.File` packages to `Onboard.Console`.
    *   Configure standard .NET `ILogger<T>` in `Program.cs` to write to a log file in a standard location (e.g., `%TEMP%/onboard-pro.log` on Windows, `/tmp/onboard-pro.log` on Linux/macOS).
    *   Inject `ILogger<ProcessRunner>` into `ProcessRunner` and log *all* command executions (command, arguments, exit code, and a truncated version of stdout/stderr) at the `Debug` level.
    *   Update `ConsoleUserInteraction` to also log all user-facing output to the file logger, ensuring a complete transcript of the session exists.

---

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

---

### **Iteration 14: Centralized Configuration**

**Goal:** Extract hardcoded values (like distro versions) into a centralized configuration source to prevent future maintenance headaches.

1.  **Create Configuration Model:**
    *   Create `src/Onboard.Core/Models/OnboardingConfiguration.cs`.
    *   Add properties for standard values, for example:
        ```csharp
        public string WslDistroName { get; init; } = "Ubuntu-22.04";
        public string WslDistroImage { get; init; } = "Ubuntu-22.04";
        ```

2.  **Register Configuration:**
    *   In `Program.cs`, register this configuration object as a singleton. (For now, hardcoded defaults in the class are acceptable, but this paves the way for reading from a JSON file later if needed).

3.  **Refactor Steps to use Configuration:**
    *   Update `EnableWslFeaturesStep.cs` to inject `OnboardingConfiguration` and use `WslDistroName` instead of the hardcoded "Ubuntu-22.04" string.
    *   Update `InstallDockerDesktopStep.cs` to use the same configuration value when checking/updating `settings-store.json`.

To significantly improve the experience of manual testing and troubleshooting, you need real-time visibility into what the tool is actually doing "under the hood." The current design hides too much information from the console user.

Here is an additional iteration focused specifically on interactive debuggability.

### **Iteration 15: Enhanced Interactive Debugging**

**Goal:** Provide optional, highly verbose real-time console output to allow developers to see exactly which commands are being executed and their raw output during manual tests.

1.  **Implement Verbose Mode Flag:**
    *   Update `CommandLineOptions.cs` and `CommandLineOptionsParser.cs` to support a new `--verbose` (alias `-v`) flag.
    *   Update `ExecutionOptions.cs` to carry this new boolean flag.

2.  **Enhance `IUserInteraction` for Verbosity:**
    *   Add a `WriteDebug(string message)` method to the `IUserInteraction` interface.
    *   Update `ConsoleUserInteraction.cs` to only output these messages when the `--verbose` flag is active (likely by injecting `ExecutionOptions` into it). Use a distinct color (e.g., DarkGray) to differentiate debug noise from standard progress.

3.  **Transparent `ProcessRunner`:**
    *   Modify `ProcessRunner.cs` to accept `ExecutionOptions` and `IUserInteraction`.
    *   When Verbose mode is active:
        *   Print the *exact* command and arguments being executed before they run (e.g., `[DEBUG] Executing: git clone https://...`).
        *   Print the full `StandardOutput` and `StandardError` captured from the process, even if it succeeded. This is crucial for understanding *why* a `ShouldExecuteAsync` check might have returned `false` unexpectedly during testing.

4.  **Detailed Dry-Run:**
    *   Update the `SequentialOrchestrator`'s dry-run loop. Instead of just printing the step description, it should ideally be able to ask the step *what* it would do.
    *   *Refinement:* A simpler approach for now is to have `ProcessRunner` respect both `IsDryRun` and `IsVerbose`. If both are true, it prints `[DRY-RUN] Would execute: <command> <args>` and returns a fake success result, rather than the orchestrator just skipping the whole step. This allows you to see the *exact* commands a dry run would trigger.