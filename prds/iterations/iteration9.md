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
