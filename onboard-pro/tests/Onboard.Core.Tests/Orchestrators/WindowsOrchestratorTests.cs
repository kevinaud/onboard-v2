namespace Onboard.Core.Tests.Orchestrators;

using System;
using System.Collections.Generic;
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
        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);
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
            new ExecutionOptions(IsDryRun: false),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteHeader("Windows host onboarding"), Times.Once);
        ui.Verify(x => x.WriteLine("Checking Verify Windows Subsystem for Linux prerequisites..."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Verify Windows Subsystem for Linux prerequisites already configured."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Install Git for Windows already configured."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Install Visual Studio Code already configured."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Install Docker Desktop already configured."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Configure Git user identity already configured."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Once);
        ui.Verify(x => x.Prompt(It.IsAny<string>()), Times.Never);
        ui.Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("Running", StringComparison.Ordinal))), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenStepFails_ThrowsOnboardingStepException()
    {
        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

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
            new ExecutionOptions(IsDryRun: false),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        var exception = Assert.ThrowsAsync<OnboardingStepException>(() => orchestrator.ExecuteAsync())!;

        Assert.That(exception.Message, Does.Contain("Install Git for Windows"));
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_WithDryRun_OnlyReportsSteps()
    {
        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

        var processRunner = new FakeProcessRunner(new Dictionary<(string, string), ProcessResult>());

        var configuration = new OnboardingConfiguration();
        var enableWslStep = new EnableWslFeaturesStep(processRunner, ui.Object, configuration);
        var installGitStep = new InstallGitForWindowsStep(processRunner, ui.Object);
        var installVsCodeStep = new InstallWindowsVsCodeStep(processRunner, ui.Object);
        var installDockerStep = new InstallDockerDesktopStep(processRunner, ui.Object, configuration);
        var configureGitStep = new ConfigureGitUserStep(processRunner, ui.Object);

        var orchestrator = new WindowsOrchestrator(
            ui.Object,
            new ExecutionOptions(IsDryRun: true),
            enableWslStep,
            installGitStep,
            installVsCodeStep,
            installDockerStep,
            configureGitStep);

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteHeader("Windows host onboarding"), Times.Once);
        ui.Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("Dry run: would execute", StringComparison.Ordinal))), Times.AtLeastOnce);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding dry run complete."), Times.Once);
        ui.Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("Checking", StringComparison.Ordinal))), Times.Never);
        ui.Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("Running", StringComparison.Ordinal))), Times.Never);
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
