namespace Onboard.Core.Tests.Steps;

using Moq;

using NUnit.Framework;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.PlatformAware;

using Architecture = Onboard.Core.Models.Architecture;
using OperatingSystem = Onboard.Core.Models.OperatingSystem;

[TestFixture]
public class InstallVsCodeStepTests
{
    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
    }

    [Test]
    public async Task ShouldExecuteAsync_Windows_WhenCodeCmdPresent_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, "C:/Program Files/Microsoft VS Code/bin/code.cmd", string.Empty));

        var step = CreateStep(OperatingSystem.Windows);
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_Windows_WhenCodeCmdMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

        var step = CreateStep(OperatingSystem.Windows);
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_Windows_InvokesWingetInstall()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Microsoft.VisualStudioCode -e --source winget"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess(It.IsAny<string>()));

        var step = CreateStep(OperatingSystem.Windows);
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.Verify(ui => ui.WriteSuccess("Visual Studio Code installed via winget."), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Mac_InvokesBrewInstall()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "install --cask visual-studio-code"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess(It.IsAny<string>()));

        var step = CreateStep(OperatingSystem.MacOs);
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.Verify(ui => ui.WriteSuccess("Visual Studio Code installed via Homebrew."), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Linux_PerformsDownloadInstallAndCleanup()
    {
        processRunner
            .Setup(runner => runner.RunAsync("curl", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("sudo", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("rm", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess(It.IsAny<string>()));

        var step = CreateStep(OperatingSystem.Linux);
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.Verify(runner => runner.RunAsync("curl", It.IsAny<string>()), Times.Once);
        processRunner.Verify(runner => runner.RunAsync("sudo", It.IsAny<string>()), Times.Once);
        processRunner.Verify(runner => runner.RunAsync("rm", It.IsAny<string>()), Times.Once);
        userInteraction.Verify(ui => ui.WriteSuccess("Visual Studio Code installed via apt."), Times.Once);
    }

    private InstallVsCodeStep CreateStep(OperatingSystem operatingSystem)
    {
        var facts = new PlatformFacts(operatingSystem, Architecture.X64, false, "/home/test");
        return new InstallVsCodeStep(facts, processRunner.Object, userInteraction.Object);
    }
}
