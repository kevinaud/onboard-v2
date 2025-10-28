namespace Onboard.Core.Tests.Orchestrators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Onboard.Console.Orchestrators;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Windows;

[TestFixture]
public class WindowsOrchestratorTests
{
    [Test]
    public async Task ExecuteAsync_SkipsStepsWhenAlreadyConfigured()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });
        var processRunner = new FakeProcessRunner(new Dictionary<(string, string), ProcessResult>
        {
            { ("wsl.exe", "--status"), new ProcessResult(0, "Version: 1.0", string.Empty) },
            { ("wsl.exe", "-l -q"), new ProcessResult(0, "Ubuntu-22.04", string.Empty) },
            { ("where", "git.exe"), new ProcessResult(0, "C\\Git\\git.exe", string.Empty) },
            { ("where", "code.cmd"), new ProcessResult(0, "C\\VSCode\\code.cmd", string.Empty) },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\""), new ProcessResult(0, "True", string.Empty) },
            { ("git", "config --global user.name"), new ProcessResult(0, "Test User", string.Empty) },
            { ("git", "config --global user.email"), new ProcessResult(0, "test@example.com", string.Empty) },
        });

        var configuration = new OnboardingConfiguration();
        var enableWslStep = new EnableWslFeaturesStep(processRunner, ui.Object, configuration);
        var installGitStep = new InstallGitForWindowsStep(processRunner, ui.Object);
        var installVsCodeStep = new InstallWindowsVsCodeStep(processRunner, ui.Object);
        var installDockerStep = new InstallDockerDesktopStep(processRunner, ui.Object, configuration);
        var configureGitStep = new ConfigureGitUserStep(processRunner, ui.Object);

        var orchestrator = new WindowsOrchestrator(
            ui.Object,
            new ExecutionOptions(IsDryRun: false, IsVerbose: false),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteNormal("Starting Windows host onboarding..."), Times.Once);
        ui.Verify(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        ui.Verify(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Once);

        Assert.That(capturedSummary, Has.Count.EqualTo(5));
        Assert.That(capturedSummary.All(result => result.Status == StepStatus.Skipped), "Expected all steps to be skipped.");
        bool allAlreadyConfigured = capturedSummary.All(result => string.Equals(result.SkipReason, "Already configured", StringComparison.Ordinal));
        Assert.That(allAlreadyConfigured, "Expected skip reason to report already configured.");
        Assert.That(capturedSummary.Select(result => result.StepName), Is.EqualTo(new[]
        {
            "Verify Windows Subsystem for Linux prerequisites",
            "Install Git for Windows",
            "Install Visual Studio Code",
            "Install Docker Desktop",
            "Configure Git user identity",
        }));
    }

    [Test]
    public void ExecuteAsync_WhenStepFails_ThrowsOnboardingStepException()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string), ProcessResult>
        {
            { ("wsl.exe", "--status"), new ProcessResult(0, "Version: 1.0", string.Empty) },
            { ("wsl.exe", "-l -q"), new ProcessResult(0, "Ubuntu-22.04", string.Empty) },
            { ("where", "git.exe"), new ProcessResult(1, string.Empty, "not found") },
            { ("where", "code.cmd"), new ProcessResult(0, "C\\VSCode\\code.cmd", string.Empty) },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\""), new ProcessResult(0, "True", string.Empty) },
            { ("git", "config --global user.name"), new ProcessResult(0, "Test User", string.Empty) },
            { ("git", "config --global user.email"), new ProcessResult(0, "test@example.com", string.Empty) },
        });

        var configuration = new OnboardingConfiguration();
        var enableWslStep = new EnableWslFeaturesStep(processRunner, ui.Object, configuration);
        var installGitStep = new InstallGitForWindowsStep(processRunner, ui.Object);
        var installVsCodeStep = new InstallWindowsVsCodeStep(processRunner, ui.Object);
        var installDockerStep = new InstallDockerDesktopStep(processRunner, ui.Object, configuration);
        var configureGitStep = new ConfigureGitUserStep(processRunner, ui.Object);

        var orchestrator = new WindowsOrchestrator(
            ui.Object,
            new ExecutionOptions(IsDryRun: false, IsVerbose: false),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        var exception = Assert.ThrowsAsync<OnboardingStepException>(() => orchestrator.ExecuteAsync())!;

        Assert.That(exception.Message, Does.Contain("Install Git for Windows"));
        ui.Verify(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Never);

        Assert.That(capturedSummary, Has.Count.EqualTo(2));
        Assert.That(capturedSummary[0].StepName, Is.EqualTo("Verify Windows Subsystem for Linux prerequisites"));
        Assert.That(capturedSummary[0].Status, Is.EqualTo(StepStatus.Skipped));
        Assert.That(capturedSummary[1].StepName, Is.EqualTo("Install Git for Windows"));
        Assert.That(capturedSummary[1].Status, Is.EqualTo(StepStatus.Failed));
        Assert.That(capturedSummary[1].Exception, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_WithDryRun_OnlyReportsSteps()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string), ProcessResult>
        {
            { ("wsl.exe", "--status"), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { ("wsl.exe", "-l -q"), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { ("where", "git.exe"), new ProcessResult(1, string.Empty, "git not found") },
            { ("where", "code.cmd"), new ProcessResult(1, string.Empty, "code not found") },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\""), new ProcessResult(1, string.Empty, "Docker not installed") },
            { ("git", "config --global user.name"), new ProcessResult(1, string.Empty, string.Empty) },
            { ("git", "config --global user.email"), new ProcessResult(1, string.Empty, string.Empty) },
        });

        var configuration = new OnboardingConfiguration();
        var enableWslStep = new EnableWslFeaturesStep(processRunner, ui.Object, configuration);
        var installGitStep = new InstallGitForWindowsStep(processRunner, ui.Object);
        var installVsCodeStep = new InstallWindowsVsCodeStep(processRunner, ui.Object);
        var installDockerStep = new InstallDockerDesktopStep(processRunner, ui.Object, configuration);
        var configureGitStep = new ConfigureGitUserStep(processRunner, ui.Object);

        var orchestrator = new WindowsOrchestrator(
            ui.Object,
            new ExecutionOptions(IsDryRun: true, IsVerbose: false),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteNormal("Starting Windows host onboarding..."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding dry run complete."), Times.Once);
        ui.Verify(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()), Times.Exactly(5));

        Assert.That(capturedSummary, Has.Count.EqualTo(5));
        Assert.That(capturedSummary.All(result => result.Status == StepStatus.Skipped));
        bool allDryRun = capturedSummary.All(result => string.Equals(result.SkipReason, "Dry run", StringComparison.Ordinal));
        Assert.That(allDryRun);
    }

    private static Mock<IUserInteraction> CreateUserInteractionMock(Action<IReadOnlyCollection<StepResult>> summaryCallback)
    {
        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

        ui.Setup(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((string _, Func<IStatusContext, Task> action, CancellationToken token) => action(new TestStatusContext(ui.Object, token)));

        ui.Setup(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()))
            .Callback(summaryCallback);

        return ui;
    }

    private sealed class TestStatusContext : IStatusContext
    {
        private readonly IUserInteraction interaction;
        private readonly CancellationToken cancellationToken;

        public TestStatusContext(IUserInteraction interaction, CancellationToken cancellationToken)
        {
            this.interaction = interaction;
            this.cancellationToken = cancellationToken;
        }

        public void UpdateStatus(string status)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
        }

        public void WriteNormal(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteNormal(message);
        }

        public void WriteSuccess(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteSuccess(message);
        }

        public void WriteWarning(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteWarning(message);
        }

        public void WriteError(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteError(message);
        }

        public void WriteDebug(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteDebug(message);
        }
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly IReadOnlyDictionary<(string FileName, string Arguments), ProcessResult> responses;

        public FakeProcessRunner(IReadOnlyDictionary<(string FileName, string Arguments), ProcessResult> responses)
        {
            this.responses = responses;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments)
        {
            if (!responses.TryGetValue((fileName, arguments), out var result))
            {
                throw new InvalidOperationException($"Unexpected command: {fileName} {arguments}");
            }

            return Task.FromResult(result);
        }
    }
}
