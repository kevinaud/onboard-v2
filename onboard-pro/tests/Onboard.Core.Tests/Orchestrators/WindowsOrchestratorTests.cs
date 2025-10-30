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
        var processRunner = new FakeProcessRunner(new Dictionary<(string, string, bool, bool), ProcessResult>
        {
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("wsl.exe", "-l -q", false, false), new ProcessResult(0, "Ubuntu-22.04\r\n", string.Empty) },
            { ("wsl.exe", "-d \"Ubuntu-22.04\" -- cat /etc/os-release", false, true), new ProcessResult(0, string.Empty, string.Empty) },
            { ("where", "git.exe", false, false), new ProcessResult(0, "C\\Git\\git.exe", string.Empty) },
            { ("where", "code.cmd", false, false), new ProcessResult(0, "C\\VSCode\\code.cmd", string.Empty) },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", false, false), new ProcessResult(0, "True", string.Empty) },
            { ("git", "config --global user.name", false, false), new ProcessResult(0, "Test User", string.Empty) },
            { ("git", "config --global user.email", false, false), new ProcessResult(0, "test@example.com", string.Empty) },
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

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string, bool, bool), ProcessResult>
        {
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("wsl.exe", "-l -q", false, false), new ProcessResult(0, "Ubuntu-22.04\r\n", string.Empty) },
            { ("wsl.exe", "-d \"Ubuntu-22.04\" -- cat /etc/os-release", false, true), new ProcessResult(0, string.Empty, string.Empty) },
            { ("where", "git.exe", false, false), new ProcessResult(1, string.Empty, "not found") },
            { ("where", "code.cmd", false, false), new ProcessResult(0, "C\\VSCode\\code.cmd", string.Empty) },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", false, false), new ProcessResult(0, "True", string.Empty) },
            { ("git", "config --global user.name", false, false), new ProcessResult(0, "Test User", string.Empty) },
            { ("git", "config --global user.email", false, false), new ProcessResult(0, "test@example.com", string.Empty) },
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

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string, bool, bool), ProcessResult>
        {
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", false, false), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", false, false), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { ("wsl.exe", "-l -q", false, false), new ProcessResult(1, string.Empty, "WSL not installed") },
            { ("where", "git.exe", false, false), new ProcessResult(1, string.Empty, "git not found") },
            { ("where", "code.cmd", false, false), new ProcessResult(1, string.Empty, "code not found") },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", false, false), new ProcessResult(1, string.Empty, "Docker not installed") },
            { ("git", "config --global user.name", false, false), new ProcessResult(1, string.Empty, string.Empty) },
            { ("git", "config --global user.email", false, false), new ProcessResult(1, string.Empty, string.Empty) },
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

    [Test]
    public async Task ExecuteAsync_WhenInteractiveStepRuns_PromptsOutsideStatus()
    {
        var capturedSummary = new List<StepResult>();
        bool isInsideStatus = false;

        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

        ui.Setup(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, Func<IStatusContext, Task> action, CancellationToken token) =>
            {
                isInsideStatus = true;

                try
                {
                    await action(new TestStatusContext(ui.Object, token)).ConfigureAwait(false);
                }
                finally
                {
                    isInsideStatus = false;
                }
            });

        ui.Setup(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()))
            .Callback((IReadOnlyCollection<StepResult> results) =>
            {
                capturedSummary.Clear();
                capturedSummary.AddRange(results);
            });

        ui.Setup(x => x.Ask(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string prompt, string? _) =>
            {
                Assert.That(isInsideStatus, Is.False, "Prompts should execute outside the status spinner.");
                return prompt.Contains("email", StringComparison.OrdinalIgnoreCase) ? "test@example.com" : "Test User";
            });

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string, bool, bool), ProcessResult>
        {
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", false, false), new ProcessResult(0, "State : Enabled", string.Empty) },
            { ("wsl.exe", "-l -q", false, false), new ProcessResult(0, "Ubuntu-22.04\r\n", string.Empty) },
            { ("wsl.exe", "-d \"Ubuntu-22.04\" -- cat /etc/os-release", false, true), new ProcessResult(0, string.Empty, string.Empty) },
            { ("where", "git.exe", false, false), new ProcessResult(0, "C\\Git\\git.exe", string.Empty) },
            { ("where", "code.cmd", false, false), new ProcessResult(0, "C\\VSCode\\code.cmd", string.Empty) },
            { ("powershell", "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", false, false), new ProcessResult(0, "True", string.Empty) },
            { ("git", "config --global user.name", false, false), new ProcessResult(1, string.Empty, string.Empty) },
            { ("git", "config --global user.email", false, false), new ProcessResult(1, string.Empty, string.Empty) },
            { ("git", "config --global user.name \"Test User\"", false, false), new ProcessResult(0, string.Empty, string.Empty) },
            { ("git", "config --global user.email \"test@example.com\"", false, false), new ProcessResult(0, string.Empty, string.Empty) },
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

        Assert.That(capturedSummary.Last().StepName, Is.EqualTo("Configure Git user identity"));
        Assert.That(capturedSummary.Last().Status, Is.EqualTo(StepStatus.Executed));
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
        private readonly IReadOnlyDictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult> responses;

        public FakeProcessRunner(IReadOnlyDictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult> responses)
        {
            this.responses = responses;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments)
        {
            return this.RunAsync(fileName, arguments, requestElevation: false, useShellExecute: false);
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation)
        {
            return this.RunAsync(fileName, arguments, requestElevation, useShellExecute: false);
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation, bool useShellExecute)
        {
            if (!responses.TryGetValue((fileName, arguments, requestElevation, useShellExecute), out var result))
            {
                throw new InvalidOperationException($"Unexpected command: {fileName} {arguments} (elevated: {requestElevation}, shell: {useShellExecute})");
            }

            return Task.FromResult(result);
        }
    }
}
