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
