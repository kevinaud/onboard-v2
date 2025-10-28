### **Iteration 16: Spectre Presentation Layer**

**Goal:** Replace the legacy console interaction layer with a Spectre.Console-driven implementation while keeping the core library free of UI dependencies.

1.  **Evolve the UI Abstractions:**
    *   Extend `Onboard.Core/Abstractions/IUserInteraction.cs` with the richer API (`WriteNormal`, `WriteSuccess`, `WriteWarning`, `WriteError`, `WriteDebug`, `ShowWelcomeBanner`, `ShowSummary`, `RunStatusAsync`, `Ask`, `Confirm`).
    *   Introduce `Onboard.Core/Abstractions/IStatusContext.cs` to expose spinner updates without leaking Spectre types.

2.  **Add Shared UX Models:**
    *   Create `Onboard.Core/Models/StepResult.cs` containing the `StepStatus` enum and `StepResult` record used for completion summaries.

3.  **Relocate the Concrete UI Layer:**
    *   Remove `ConsoleUserInteraction` from `Onboard.Core` and add `Onboard.Console/Services/SpectreUserInteraction.cs` that implements the new interaction contract using Spectre.Console (including the welcome banner, semantic colors, and spinner wrapper).
    *   Reference the `Spectre.Console` package in `Onboard.Console/Onboard.Console.csproj` and wire the new service into the DI container.
