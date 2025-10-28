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
