### **Iteration 15: Enhanced Interactive Debugging**

**Goal:** Provide optional, highly verbose real-time console output to allow developers to see exactly which commands are being executed and their raw output during manual tests.

1.  **Implement Verbose Mode Flag:**
    *   Update `CommandLineOptions.cs` and `CommandLineOptionsParser.cs` to support a new `--verbose` (alias `-v`) flag.
    *   Update `ExecutionOptions.cs` to carry this new boolean flag.

2.  **Enhance `IUserInteraction` for Verbosity:**
    *   Add a `WriteDebug(string message)` method to the `IUserInteraction` interface.
    *   Update `ConsoleUserInteraction.cs` to only output these messages when the `--verbose` flag is active (likely by injecting `ExecutionOptions` into it). Use a distinct color (e.g., DarkGray) to differentiate debug noise from standard progress.

3.  **Transparent `ProcessRunner`:**
    *   Modify `ProcessRunner.cs` to accept `ExecutionOptions` and `IUserInteraction`.
    *   When Verbose mode is active:
        *   Print the *exact* command and arguments being executed before they run (e.g., `[DEBUG] Executing: git clone https://...`).
        *   Print the full `StandardOutput` and `StandardError` captured from the process, even if it succeeded. This is crucial for understanding *why* a `ShouldExecuteAsync` check might have returned `false` unexpectedly during testing.

4.  **Detailed Dry-Run:**
    *   Update the `SequentialOrchestrator`'s dry-run loop. Instead of just printing the step description, it should ideally be able to ask the step *what* it would do.
    *   *Refinement:* A simpler approach for now is to have `ProcessRunner` respect both `IsDryRun` and `IsVerbose`. If both are true, it prints `[DRY-RUN] Would execute: <command> <args>` and returns a fake success result, rather than the orchestrator just skipping the whole step. This allows you to see the *exact* commands a dry run would trigger.

---

### Implementation Plan

1. Extend CLI parsing and option models to surface an `IsVerbose` flag (`CommandLineOptions`, parser, and `ExecutionOptions`) with NUnit coverage for the new syntax.
2. Enhance `IUserInteraction`/`ConsoleUserInteraction` with a `WriteDebug` channel that only emits when verbose mode is active while preserving transcript logging.
3. Inject `ExecutionOptions` and `IUserInteraction` into `ProcessRunner` so verbose runs echo command lifecycles and dry-run requests short-circuit with `[DRY-RUN]` diagnostics.
4. Refine `SequentialOrchestrator` dry-run handling to execute the standard `ShouldExecuteAsync` checks, report idempotent status, and reuse the new debug hooks for visibility.
5. Update affected onboarding steps/tests and documentation to reflect the verbose/dry-run behaviour and ensure local builds/tests (plus dry-run smoke) continue to pass.

---

### Implementation Notes (2025-10-28)

- `CommandLineOptions`, `ExecutionOptions`, and the parser now surface an `IsVerbose` flag with NUnit coverage for long/short syntax, duplicate detection, and boolean values.
- `IUserInteraction` exposes `WriteDebug`, and `ConsoleUserInteraction` mirrors every debug message to the transcript log while only rendering to the console when verbose mode is active (dark gray styling).
- `ProcessRunner` consumes `ExecutionOptions`/`IUserInteraction`, short-circuits in dry-run mode with `[DRY-RUN]` messages, and prints command lifecycle output (command line, exit code, stdout/stderr) when verbose mode is on.
- `SequentialOrchestrator` always evaluates `ShouldExecuteAsync` even during dry runs, reports "already configured" states, and prints explicit dry-run lines for actionable steps.
- Windows orchestrator tests were expanded to cover the new dry-run semantics, and service/tests for the parser, user interaction, and process runner verify the verbose behaviour.
