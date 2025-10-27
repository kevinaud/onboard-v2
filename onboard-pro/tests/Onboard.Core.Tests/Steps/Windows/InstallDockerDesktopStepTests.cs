namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Windows;

[TestFixture]
public class InstallDockerDesktopStepTests
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
    public async Task ShouldExecuteAsync_WhenDockerDesktopDetected_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync(
                "powershell",
                It.Is<string>(arguments => arguments.Contains("Docker Desktop.exe", StringComparison.OrdinalIgnoreCase))))
            .ReturnsAsync(new ProcessResult(0, "True", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenDockerDesktopMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync(
                "powershell",
                It.Is<string>(arguments => arguments.Contains("Docker Desktop.exe", StringComparison.OrdinalIgnoreCase))))
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
            .Setup(runner => runner.RunAsync(
                "powershell",
                It.Is<string>(arguments => arguments.Contains("Docker Desktop.exe", StringComparison.OrdinalIgnoreCase))))
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
            .Setup(runner => runner.RunAsync("winget", "install --id Docker.DockerDesktop -e --source winget"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var messages = new List<string>();
        userInteraction.Setup(ui => ui.WriteSuccess("Docker Desktop installed via winget."));
        userInteraction.Setup(ui => ui.WriteLine(It.IsAny<string>())).Callback<string>(messages.Add);

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        Assert.That(messages.Any(message => message.Contains("WSL integration", StringComparison.OrdinalIgnoreCase)), Is.True);
        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenWingetFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Docker.DockerDesktop -e --source winget"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "winget error"));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallDockerDesktopStep CreateStep()
    {
        return new InstallDockerDesktopStep(processRunner.Object, userInteraction.Object);
    }
}
