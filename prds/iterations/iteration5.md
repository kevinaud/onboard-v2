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
