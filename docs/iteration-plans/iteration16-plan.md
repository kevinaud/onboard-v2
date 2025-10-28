# Iteration 16 Plan — Spectre Presentation Layer

## Scope Breakdown
- Expand `IUserInteraction` to the new contract (`WriteNormal`, `WriteSuccess`, `WriteWarning`, `WriteError`, `WriteDebug`, `ShowWelcomeBanner`, `ShowSummary`, `RunStatusAsync`, `Ask`, `Confirm`). `WriteNormal` replaces legacy `WriteLine` and `WriteHeader` is retired in favour of banner/summary entry points.
- Introduce `IStatusContext` abstraction inside `Onboard.Core.Abstractions` so steps/orchestrators can report spinner progress without referencing Spectre types.
- Add `StepStatus` enum and `StepResult` record under `Onboard.Core.Models` for use by orchestrators when building completion summaries.

## Implementation Sequence
1. **Contract Updates (Core)**
   - Modify `IUserInteraction` signature and relocate supporting XML docs.
   - Add new `IStatusContext` interface plus any supporting delegates needed for `RunStatusAsync`.
   - Update Core services/tests that compile against the old methods (temporary shims in tests until Spectre adapters arrive).
2. **Model Additions (Core)**
   - Introduce `StepStatus` + `StepResult` and ensure existing orchestrators compile (no behavioural changes yet).
3. **Console Layer Migration (Console project)**
   - Delete `ConsoleUserInteraction` from Core.
   - Add `SpectreUserInteraction` in `Onboard.Console/Services` that implements the expanded interface via `IAnsiConsole` + Spectre primitives.
   - Create an internal `SpectreStatusContext` adapter implementing `IStatusContext` by wrapping `StatusContext`.
4. **Dependency Injection**
   - Add `Spectre.Console` package reference to `Onboard.Console`.
   - Register `SpectreUserInteraction`, `IAnsiConsole` factory, and supporting status adapter in `Program.cs`.
5. **Testing Prep**
   - Add `Spectre.Console.Testing` to the test project.
   - Supply basic fakes/mocks for `IAnsiConsole` and `IStatusContext` to keep existing step/orchestrator tests compiling. Full Spectre interaction coverage deferred to Iteration 17 per roadmap.

## Risk & Mitigation
- **Interface ripple**: Expanding `IUserInteraction` touches many tests. Mitigate by introducing helper builders in `tests/Onboard.Core.Tests/TestDoubles`.
- **Console dependency leak**: Ensure `Onboard.Core` project file drops references to the removed console service and does not reference Spectre package.
- **Status context lifecycle**: Keep adapter internal to console project; expose minimal surface (`SetStatus`, `WriteLine`, `Dispose`).

## Validation Strategy
- `dotnet build -warnaserror`
- `dotnet test --no-build`
- Manual dry-run of `Onboard.Console` to confirm Spectre host boots, banner prints, and no dependency errors occur (detailed UX polish arrives in Iteration 17).
