### **Iteration 3: Dependency Injection & The Composition Root**

**Goal:** Wire all services together in `Program.cs` and implement the orchestrator selection logic.

1.  **Implement `Program.cs`:**
    *   Replace the contents of `src/Onboard.Console/Program.cs` with the code from section 4.5 of the design document.
    *   Create empty placeholder classes for all orchestrators (`WindowsOrchestrator`, `MacOsOrchestrator`, etc.) in `src/Onboard.Console/Orchestrators/` so the DI registration works. Each should have an empty `ExecuteAsync` method.
    *   Create empty placeholder classes for the initial onboarding steps (`ConfigureGitUserStep`, `InstallVsCodeStep`, etc.) so the DI registration works.
    *   At this stage, running the application should correctly detect the platform and select an orchestrator, which will then do nothing. For example, running on Windows should print "Starting Windows Host Onboarding..." and then exit.
