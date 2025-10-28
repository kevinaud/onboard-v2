### **Iteration 17: UX-Oriented Orchestration**

**Goal:** Integrate the new interaction APIs into the orchestration pipeline to deliver the polished spinner workflow and completion summary.

1.  **Sequential Flow Enhancements:**
    *   Update `Onboard.Console/Orchestrators/SequentialOrchestrator.cs` to wrap execution in `RunStatusAsync`, emit rich status lines with Spectre markup, collect `StepResult` details (Executed, Skipped, Failed), and invoke `ShowSummary` when complete.

2.  **Startup Experience:**
    *   Modify `Program.cs` to call `ShowWelcomeBanner` with the resolved OS/architecture before dispatching to the orchestrator.
    *   Ensure platform-specific orchestrators surface spinner-friendly descriptions and return human-readable skip reasons stored in `StepResult`.

3.  **Testing Coverage:**
    *   Update existing unit tests to mock the expanded `IUserInteraction` contract (including faking `RunStatusAsync`).
    *   Add `SpectreUserInteractionTests` using `Spectre.Console.Testing` to verify markup output for success, skipped, and error states.
