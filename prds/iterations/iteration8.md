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
