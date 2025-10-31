namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;

using Moq;

[TestFixture]
public class InstallDockerDesktopStepTests
{
    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;
    private Mock<IEnvironmentRefresher> environmentRefresher = null!;
    private OnboardingConfiguration configuration = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
        environmentRefresher = new Mock<IEnvironmentRefresher>(MockBehavior.Strict);
        configuration = new OnboardingConfiguration { WslDistroName = "Ubuntu-22.04" };
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenDockerDesktopDetected_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("powershell", It.Is<string>(args => args.Contains("Docker Desktop.exe", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, "True\r\n", string.Empty));

        var step = CreateStep();

        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenDockerDesktopMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("powershell", It.Is<string>(args => args.Contains("Docker Desktop.exe", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, "False", string.Empty));

        var step = CreateStep();

        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenDetectionFails_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("powershell", It.Is<string>(args => args.Contains("Docker Desktop.exe", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

        var step = CreateStep();

        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWingetSucceeds_PrintsSuccessAndGuidance()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", It.Is<string>(args => args.Contains("Docker.DockerDesktop", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        environmentRefresher
            .Setup(refresher => refresher.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        userInteraction.Setup(ui => ui.WriteSuccess("Docker Desktop installed via winget."));
        userInteraction.Setup(ui => ui.WriteNormal("Launch Docker Desktop and accept the terms of service if prompted."));
        userInteraction.Setup(ui => ui.WriteNormal(It.Is<string>(message => message.Contains(configuration.WslDistroName, StringComparison.OrdinalIgnoreCase))));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
        environmentRefresher.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenWingetFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", It.Is<string>(args => args.Contains("Docker.DockerDesktop", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "winget error"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallDockerDesktopStep CreateStep() => new(processRunner.Object, userInteraction.Object, configuration, environmentRefresher.Object);
}
