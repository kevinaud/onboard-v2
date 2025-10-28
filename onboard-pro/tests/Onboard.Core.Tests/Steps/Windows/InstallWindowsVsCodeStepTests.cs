namespace Onboard.Core.Tests.Steps.Windows;

using System;
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

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
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
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Microsoft.VisualStudioCode -e --source winget"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess("Visual Studio Code installed via winget."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
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
        return new InstallWindowsVsCodeStep(processRunner.Object, userInteraction.Object);
    }
}
