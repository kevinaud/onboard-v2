namespace Onboard.Core.Tests.Steps.MacOs;

using System;
using System.Threading.Tasks;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.MacOs;

[TestFixture]
public class InstallMacVsCodeStepTests
{
    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;
    private Mock<IFileSystem> fileSystem = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
        fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenCodeCliExists_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("which", "code", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(0, "/usr/local/bin/code", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
        fileSystem.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenAppBundleExists_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("which", "code", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));
        fileSystem
            .Setup(fs => fs.DirectoryExists("/Applications/Visual Studio Code.app"))
            .Returns(true);

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenAppBundleMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("which", "code", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));
        fileSystem
            .Setup(fs => fs.DirectoryExists("/Applications/Visual Studio Code.app"))
            .Returns(false);

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenBrewSucceeds_WritesSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "install --cask visual-studio-code", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess("Visual Studio Code installed via Homebrew."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenBrewFails_ThrowsInvalidOperationException()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "install --cask visual-studio-code", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "brew error"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallMacVsCodeStep CreateStep()
    {
        return new InstallMacVsCodeStep(processRunner.Object, userInteraction.Object, fileSystem.Object);
    }
}
