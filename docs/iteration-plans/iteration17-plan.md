# Iteration 17 Rollout Plan

## Objectives
- Drive every onboarding step through `IUserInteraction.RunStatusAsync` so step lifecycle messaging renders with Spectre spinners.
- Capture `StepResult` data (executed / skipped with reason / failed) in the orchestrator and present a completion summary via `ShowSummary` once the run finishes.
- Introduce a Spectre-powered welcome banner at startup before orchestrator dispatch, using detected `PlatformFacts`.
- Ensure platform orchestrators provide spinner-friendly descriptions and skip reasons consumable by the summary experience.

## Sequencing
1. **Orchestration Flow Design**
   - Refactor `SequentialOrchestrator` to own a `List<StepResult>`.
   - Use `RunStatusAsync` per step: perform `ShouldExecuteAsync`, decide dry-run behaviour, run the step, and push the matching `StepResult` entry.
   - Surface pause-friendly messaging through `IStatusContext` (`UpdateStatus` for switching from "Checking" to "Running").
2. **Startup Refinements**
   - Call `ShowWelcomeBanner` in `Program.Main` immediately after resolving services.
   - Ensure orchestrator descriptions remain concise so headings and summary rows read well.
3. **Summary + Skip Reasons**
   - Provide human readable skip reasons (e.g., "Already configured", "Dry run") from orchestrator logic when `ShouldExecuteAsync` returns `false` or dry-run mode short-circuits execution.
4. **Spectre UI Enhancements**
   - Extend `SpectreUserInteraction` if needed to support richer summary output (e.g., include icons/markup consistent with UX doc) while keeping the core contract unchanged.

## Testing Strategy
- **Orchestrator unit tests**: Update mocks to verify `RunStatusAsync` invocation, step ordering, and summary generation (capturing `StepResult` via mock callbacks). Cover happy path, dry-run, and failure propagation.
- **Step-level regression tests**: Ensure existing Windows step tests compile against the new interaction patterns (mock `RunStatusAsync` where needed).
- **Spectre UI tests**: Add `Spectre.Console.Testing` support in `Onboard.Core.Tests` to validate markup produced by `SpectreUserInteraction` for success, skipped, failure, and summary scenarios.
- **End-to-end smoke**: Maintain validation scripts (`dotnet format`, `dotnet build`, `dotnet test`, dry-run) as part of Phase 30.

## Risk Mitigation
- **Exception handling**: Wrap status execution with try/catch to translate failures into `StepResult` entries before rethrowing `OnboardingStepException`.
- **Dry-run correctness**: Guard against accidentally invoking `ExecuteAsync` when `IsDryRun` is true; include unit tests.
- **Spinner UX**: Use consistent status strings to avoid flicker and keep summary output aligned with expectations from PRD 17.
