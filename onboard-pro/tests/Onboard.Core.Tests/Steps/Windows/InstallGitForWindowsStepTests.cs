namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Windows;

[TestFixture]
public class InstallGitForWindowsStepTests
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
    public async Task ShouldExecuteAsync_WhenGitExecutableFound_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "git.exe"))
            .ReturnsAsync(new ProcessResult(0, "C:/Program Files/Git/bin/git.exe", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenGitExecutableMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "git.exe"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "INFO: Could not find files"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWingetSucceeds_PrintsSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Git.Git -e --source winget"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess("Git for Windows installed via winget."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenWingetFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("winget", "install --id Git.Git -e --source winget"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "winget error"));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallGitForWindowsStep CreateStep()
    {
        return new InstallGitForWindowsStep(processRunner.Object, userInteraction.Object);
    }
}
