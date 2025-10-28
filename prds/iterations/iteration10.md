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
