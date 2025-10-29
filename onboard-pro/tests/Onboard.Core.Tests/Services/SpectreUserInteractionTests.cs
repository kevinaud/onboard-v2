namespace Onboard.Core.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Onboard.Console.Services;
using Onboard.Core.Models;

using Spectre.Console.Testing;

using CPU = Onboard.Core.Models.Architecture;
using OS = Onboard.Core.Models.OperatingSystem;

[TestFixture]
public class SpectreUserInteractionTests
{
    [Test]
    public void ShowWelcomeBanner_WritesPlatformInformation()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));
        var platformFacts = new PlatformFacts(OS.Windows, CPU.X64, IsWsl: false, HomeDirectory: "C:/Users/test");

        interaction.ShowWelcomeBanner(platformFacts);

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("Onboard Pro"));
        Assert.That(output, Does.Contain("Windows"));
        Assert.That(output, Does.Contain("X64"));
    }

    [Test]
    public void WriteSuccess_RendersCheckmarkWithMessage()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));

        interaction.WriteSuccess("All good");

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("✓ All good"));
    }

    [Test]
    public void WriteWarning_RendersWarningIndicator()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));

        interaction.WriteWarning("Heads up");

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("⚠ Heads up"));
    }

    [Test]
    public void WriteError_RendersCrossWithMessage()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));

        interaction.WriteError("Something failed");

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("✗ Something failed"));
    }

    [Test]
    public void WriteDebug_WhenVerbose_PrintsLiteralDebugTag()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, true));

        interaction.WriteDebug("command details");

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("[DEBUG] command details"));
    }

    [Test]
    public void ShowSummary_RendersStatusTable()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));
        var results = new[]
        {
            new StepResult("Step A", StepStatus.Executed),
            new StepResult("Step B", StepStatus.Skipped, "Already configured"),
            new StepResult(
                "Step C",
                StepStatus.Failed,
                Exception: new InvalidOperationException("boom")),
        };

        interaction.ShowSummary(results);

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("Step A"));
        Assert.That(output, Does.Contain("Executed"));
        Assert.That(output, Does.Contain("Step B"));
        Assert.That(output, Does.Contain("Already configured"));
        Assert.That(output, Does.Contain("Step C"));
        Assert.That(output, Does.Contain("boom"));
    }

    [Test]
    public async Task RunStatusAsync_ExecutesActionWithinStatusContext()
    {
        var console = new TestConsole();
        var interaction = new SpectreUserInteraction(console, NullLogger<SpectreUserInteraction>.Instance, new ExecutionOptions(false, false));

        await interaction.RunStatusAsync(
            "Checking Step",
            context =>
            {
                context.UpdateStatus("Running Step");
                context.WriteSuccess("Step completed.");
                return Task.CompletedTask;
            },
            CancellationToken.None).ConfigureAwait(false);

        string output = console.Output.ToString();

        Assert.That(output, Does.Contain("Step completed."));
    }
}
