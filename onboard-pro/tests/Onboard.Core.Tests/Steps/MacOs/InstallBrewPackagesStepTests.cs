namespace Onboard.Core.Tests.Steps.MacOs;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.MacOs;

[TestFixture]
public class InstallBrewPackagesStepTests
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
    public async Task ShouldExecuteAsync_WhenPackagesInstalled_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "list gh"))
            .ReturnsAsync(new ProcessResult(0, "gh", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenPackagesMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "list gh"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Error"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenInstallSucceeds_WritesSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "install git gh chezmoi"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteSuccess("Homebrew packages installed (git, gh, chezmoi)."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenInstallFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("brew", "install git gh chezmoi"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallBrewPackagesStep CreateStep()
    {
        return new InstallBrewPackagesStep(processRunner.Object, userInteraction.Object);
    }
}
