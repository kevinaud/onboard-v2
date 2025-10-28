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
