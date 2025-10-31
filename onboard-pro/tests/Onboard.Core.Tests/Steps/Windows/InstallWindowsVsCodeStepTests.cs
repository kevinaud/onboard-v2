namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Windows;

[TestFixture]
public class InstallWindowsVsCodeStepTests
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
        configuration = new OnboardingConfiguration();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenCodeCmdExists_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, "C:/Program Files/Microsoft VS Code/bin/code.cmd", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenCodeCmdMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWingetSucceeds_WritesSuccess()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"code-cli-{Guid.NewGuid():N}");
        string cliPath = Path.Combine(tempDirectory, "code.cmd");

        Directory.CreateDirectory(tempDirectory);
        await File.WriteAllTextAsync(cliPath, string.Empty).ConfigureAwait(false);

        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Microsoft.VisualStudioCode -e --source winget"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        environmentRefresher
            .Setup(refresher => refresher.RefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userInteraction.Setup(ui => ui.WriteSuccess("Visual Studio Code installed via winget."));
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));

        try
        {
            var step = CreateStep();
            await step.ExecuteAsync().ConfigureAwait(false);

            processRunner.VerifyAll();
            userInteraction.VerifyAll();
            environmentRefresher.VerifyAll();
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
        }
        finally
        {
            if (File.Exists(cliPath))
            {
                File.Delete(cliPath);
            }

            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Test]
    public void ExecuteAsync_WhenWingetFails_ThrowsInvalidOperationException()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Microsoft.VisualStudioCode -e --source winget"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "winget error"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallWindowsVsCodeStep CreateStep()
    {
        return new InstallWindowsVsCodeStep(processRunner.Object, userInteraction.Object, configuration, environmentRefresher.Object);
    }
}
