Here is a companion design document for the UX Polish iteration.

***

# Design Document: Enhanced User Experience (UX)

**Target Iteration:** UX Polish (Spectre.Console Integration)
**Goal:** elevate the tool from a basic command-line script to a professional, polished, and reassuring developer product.

## 1. UX Vision

The current UX is functional but "noisy." It prints every check and action linearly, making it hard to quickly scan for what actually *changed* versus what was just *checked*.

The new UX will be **calm, clear, and interactive**.

*   **Calm:** It only uses motion (spinners) when actively working. It doesn't spam the history buffer.
*   **Clear:** It uses semantic colors (Green = Done, Grey = Skipped/Idempotent, Red = Error, Cyan = Info) consistently.
*   **Interactive:** Prompts are unmistakably requests for user input, validated in real-time before the user can proceed.

### 1.1. Visual Mockups

#### **Startup & Initialization**
Instantly recognizable branding confirms the user is running the right tool.

```text
   ____        _                         _   _____
  / __ \      | |                       | | |  __ \
 | |  | |_ __ | |__   ___   __ _ _ __ __| | | |__) | __ ___
 | |  | | '_ \| '_ \ / _ \ / _` | '__/ _` | |  ___/ '__/ _ \
 | |__| | | | | |_) | (_) | (_| | | | (_| | | |   | | | (_) |
  \____/|_| |_|_.__/ \___/ \__,_|_|  \__,_| |_|   |_|  \___/
 ───────────────────────────────────────────────────────────
 Welcome to Onboard Pro for [cyan]Windows[/]
 detected [grey]win-x64[/]
```

#### **The Execution Loop (Active)**
Instead of scrolling text, a single animated line at the bottom shows current activity. Completed steps are "stamped" into the history above it.

```text
 [grey]⏭  Install Git for Windows (Already up to date)[/]
 [green]✓  Install Visual Studio Code[/]
 ⠋ Installing Docker Desktop...  <-- [Animated Spinner, always at the bottom]
```

#### **Interactive Prompts**
When a step needs input, it pauses the spinner cleanly and presents a validated prompt.

```text
 [grey]⏭  Install Git for Windows (Already up to date)[/]
 [green]✓  Install Visual Studio Code[/]
 [green]✓  Install Docker Desktop[/]

 [bold]Configure Git Identity[/]
 > Git requires a user identity for commits.
 ? Please enter your full name: [Kevin Aud]_
```

#### **Completion Summary**
A final report provides confidence that the system is ready to use.

```text
 ───────────────────────────────────────────────────────────
 [bold green]Onboarding Complete![/]

 | Step                           | Status   | Action Taken           |
 |--------------------------------|----------|------------------------|
 | Install Git                    | Skipped  | Found v2.43.0          |
 | Install Visual Studio Code     | Executed | Installed via winget   |
 | Install Docker Desktop         | Executed | Installed via winget   |
 | Configure Git Identity         | Executed | Set to 'Kevin Aud'     |

 [cyan]i[/] Restart your terminal to ensure all path changes take effect.
```

---

## 2. Architectural Changes

To support this rich UX while maintaining testability, we will refine our abstractions and move concrete presentation logic to the correct layer.

### 2.1. Project Restructuring
*   **Move:** Concrete UI implementations should reside in the Presentation Layer, not the Core layer.
    *   Move `Onboard.Core/Services/ConsoleUserInteraction.cs` -> `Onboard.Console/Services/SpectreUserInteraction.cs`.
    *   `Onboard.Core` will no longer have *any* console dependencies, making it a pure domain library.

### 2.2. Abstraction Upgrades (`IUserInteraction`)
The current interface is too simple for rich UIs. We need to add capabilities for stateful operations (spinners) and richer prompts.

```csharp
// Onboard.Core/Abstractions/IUserInteraction.cs

public interface IUserInteraction
{
    // Basic logging (using semantic markup internally)
    void WriteNormal(string message);
    void WriteSuccess(string message);
    void WriteWarning(string message);
    void WriteError(string message);
    void WriteDebug(string message); // For -v verbose mode

    // Rich components
    void ShowWelcomeBanner(string osName, string archName);
    void ShowSummary(IEnumerable<StepResult> results);

    // Stateful operations
    // Allows the orchestrator to wrap a sequence of actions in a spinner.
    Task RunStatusAsync(string initialMessage, Func<IStatusContext, Task> action);

    // Interactive prompts
    string Ask(string question, string? defaultValue = null, Func<string, bool>? validator = null);
    bool Confirm(string question, bool defaultValue = true);
}

// New interface to abstract Spectre's specific StatusContext
// This allows the Orchestrator to update the spinner text without depending on Spectre directly.
public interface IStatusContext
{
    void Update(string message);
}
```

### 2.3. New Models
To support the summary table, we need to track the result of each step explicitly.

```csharp
// Onboard.Core/Models/StepResult.cs
public enum StepStatus { Executed, Skipped, Failed }

public record StepResult(
    string StepName,
    StepStatus Status,
    string Details // e.g., "Installed via winget" or "Already configured"
);
```

---

## 3. Implementation Details (Spectre Integration)

The new `Onboard.Console/Services/SpectreUserInteraction.cs` will implement the upgraded interface using `Spectre.Console`.

### 3.1. Status Spinner Implementation
This leverages `AnsiConsole.Status()` to perform the heavy lifting of animation and terminal cursor management.

```csharp
public async Task RunStatusAsync(string initialMessage, Func<IStatusContext, Task> action)
{
    // Map our abstract IStatusContext to Spectre's concrete StatusContext
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync(initialMessage, async spectreCtx =>
        {
            var wrapper = new SpectreStatusContextWrapper(spectreCtx);
            await action(wrapper);
        });
}

private class SpectreStatusContextWrapper : IStatusContext
{
    private readonly StatusContext _context;
    public SpectreStatusContextWrapper(StatusContext context) => _context = context;
    public void Update(string message) => _context.Status(message);
}
```

### 3.2. Orchestrator Updates
The `SequentialOrchestrator` needs to handle the new `StepResult` tracking and use the `RunStatusAsync` method.

```csharp
// In SequentialOrchestrator.ExecuteAsync()

var results = new List<StepResult>();

await _ui.RunStatusAsync("Starting onboarding...", async ctx =>
{
    foreach (var step in _steps)
    {
        ctx.Update($"Checking [bold]{step.Description}[/]...");
        // ... logic to check shouldExecute ...

        if (!shouldExecute)
        {
             // Use Markup for rich history
             _ui.WriteNormal($"[grey]⏭  {step.Description} (Skipped)[/]");
             results.Add(new StepResult(step.Description, StepStatus.Skipped, "Already configured"));
             continue;
        }

        ctx.Update($"Executing [bold]{step.Description}[/]...");
        // ... execute step ...
        
        _ui.WriteNormal($"[green]✓  {step.Description}[/]");
        results.Add(new StepResult(step.Description, StepStatus.Executed, "Success"));
    }
});

_ui.ShowSummary(results);
```

### 3.3. Iteration 17 orchestration refinements

The production implementation goes a step further than the pseudocode above:

- Each step is wrapped in `RunStatusAsync` so spinner updates transition from "Checking" to "Running" without clearing the console or losing pending prompts.
- Dry-run mode records a skipped `StepResult` (`Status = StepStatus.Skipped`, `Details = "Dry run"`) and prints `[grey]` skip markup to keep the transcript consistent.
- Platform orchestrators now supply user-facing skip reasons (for example "Already configured" or "Dry run") so the final summary table explains why work was skipped.
- Failures capture the thrown `OnboardingStepException` message inside the `StepResult` before rethrowing, ensuring the summary shows a red row even when execution aborts early.
- After processing the full list the orchestrator invokes `ShowSummary(results)` once, producing a Spectre table with the step name, status, and details columns.

These refinements keep the UX calm: the console shows one spinner at a time, while the history above it contains the same markup that eventually appears in the completion summary.

---

## 4. Testing Implications

Integrating a rich UI library can complicate testing if not managed correctly.

### 4.1. Core/Orchestrator Tests (No Change)
Because we updated the `IUserInteraction` abstraction, our existing unit tests in `Onboard.Core.Tests` remain valid. We simply mock the new methods.
*   Mock `RunStatusAsync` to just immediately execute the passed delegate.
*   Mock `Ask` to return pre-determined strings.

### 4.2. Testing the UI Implementation (New)
To ensure the Spectre integration itself doesn't break, we can add specific tests for `SpectreUserInteraction` using `Spectre.Console.Testing`.

*   **Test Project:** Add a new test file `SpectreUserInteractionTests.cs` in `Onboard.Core.Tests` (or a separate integration test project if preferred).
*   **Mechanism:** Inject `AnsiConsole.Console = new TestConsole();` into the `SpectreUserInteraction` and assert on the `TestConsole.Output`.

```csharp
[Test]
public void WriteSuccess_OutputsGreenCheckmark()
{
    var console = new TestConsole();
    var ui = new SpectreUserInteraction(console); // Inject the test console

    ui.WriteSuccess("It worked");

    Assert.That(console.Output, Does.Contain("✓"));
    // We can even assert on the ANSI color codes if we need extreme precision
}
```