namespace Onboard.Core.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Services;
using Onboard.Core.Tests.TestDoubles;

[TestFixture]
public class ProcessRunnerTests
{
    private string? originalDebianFrontend;

    [SetUp]
    public void SetUp()
    {
        originalDebianFrontend = Environment.GetEnvironmentVariable("DEBIAN_FRONTEND");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", originalDebianFrontend);
    }

    [Test]
    public async Task RunAsync_OnLinux_InsertsDebianFrontendWhenMissing()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Ignore("Linux-specific behaviour");
        }

        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", null);

        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance, new ExecutionOptions(IsDryRun: false, IsVerbose: false), new NullUserInteraction());

        var result = await runner.RunAsync("env", string.Empty);

        Assert.That(result.StandardOutput, Does.Contain("DEBIAN_FRONTEND=noninteractive"));
    }

    [Test]
    public async Task RunAsync_OnLinux_PreservesExistingDebianFrontend()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Ignore("Linux-specific behaviour");
        }

        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", "dialog");

        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance, new ExecutionOptions(IsDryRun: false, IsVerbose: false), new NullUserInteraction());

        var result = await runner.RunAsync("env", string.Empty);

        Assert.That(result.StandardOutput, Does.Contain("DEBIAN_FRONTEND=dialog"));
    }

    [Test]
    public async Task RunAsync_LogsCommandLifecycle()
    {
        var logger = new InMemoryLogger<ProcessRunner>();
        var runner = new ProcessRunner(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: false), new NullUserInteraction());

        var result = await runner.RunAsync("dotnet", "--version");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(logger.Entries, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(logger.Entries[0].Message, Does.Contain("Executing command dotnet --version"));
        Assert.That(logger.Entries[^1].Message, Does.Contain("StdOut:"));
    }

    [Test]
    public async Task RunAsync_WhenVerbose_WritesDebugMessages()
    {
        var logger = new InMemoryLogger<ProcessRunner>();
        var userInteraction = new RecordingUserInteraction();
        var runner = new ProcessRunner(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: true), userInteraction);

        var result = await runner.RunAsync("dotnet", "--version");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(userInteraction.DebugMessages, Does.Contain("Executing: dotnet --version"));
        Assert.That(userInteraction.DebugMessages.Any(m => m.StartsWith("Completed with exit code", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public async Task RunAsync_WithDryRunAndVerbose_ReturnsSuccessAndLogsDryRun()
    {
        var userInteraction = new RecordingUserInteraction();
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance, new ExecutionOptions(IsDryRun: true, IsVerbose: true), userInteraction);

        var result = await runner.RunAsync("nonexistent", "--arg");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StandardOutput, Is.EqualTo(string.Empty));
            Assert.That(userInteraction.DebugMessages, Is.EquivalentTo(new[] { "[DRY-RUN] Would execute: nonexistent --arg" }));
        });
    }

    private sealed class NullUserInteraction : IUserInteraction
    {
        public string Ask(string prompt, string? defaultValue = null) => defaultValue ?? string.Empty;

        public bool Confirm(string prompt, bool defaultValue = false) => defaultValue;

        public void ShowSummary(IReadOnlyCollection<StepResult> results)
        {
        }

        public void ShowWelcomeBanner(PlatformFacts platformFacts)
        {
        }

        public Task RunStatusAsync(string statusMessage, Func<IStatusContext, Task> action, CancellationToken cancellationToken = default)
        {
            return action(new NullStatusContext());
        }

        public void WriteDebug(string message)
        {
        }

        public void WriteError(string message)
        {
        }

        public void WriteNormal(string message)
        {
        }

        public void WriteMarkdown(string markdown)
        {
        }

        public void WriteSuccess(string message)
        {
        }

        public void WriteWarning(string message)
        {
        }
    }

    private sealed class RecordingUserInteraction : IUserInteraction
    {
        public List<string> DebugMessages { get; } = new();

        public string Ask(string prompt, string? defaultValue = null) => defaultValue ?? string.Empty;

        public bool Confirm(string prompt, bool defaultValue = false) => defaultValue;

        public Task RunStatusAsync(string statusMessage, Func<IStatusContext, Task> action, CancellationToken cancellationToken = default)
        {
            return action(new NullStatusContext());
        }

        public void ShowSummary(IReadOnlyCollection<StepResult> results)
        {
        }

        public void ShowWelcomeBanner(PlatformFacts platformFacts)
        {
        }

        public void WriteDebug(string message) => this.DebugMessages.Add(message);

        public void WriteError(string message)
        {
        }

        public void WriteNormal(string message)
        {
        }

        public void WriteMarkdown(string markdown)
        {
        }

        public void WriteSuccess(string message)
        {
        }

        public void WriteWarning(string message)
        {
        }
    }

    private sealed class NullStatusContext : IStatusContext
    {
        public void UpdateStatus(string status)
        {
        }

        public void WriteDebug(string message)
        {
        }

        public void WriteError(string message)
        {
        }

        public void WriteNormal(string message)
        {
        }

        public void WriteSuccess(string message)
        {
        }

        public void WriteWarning(string message)
        {
        }
    }
}
