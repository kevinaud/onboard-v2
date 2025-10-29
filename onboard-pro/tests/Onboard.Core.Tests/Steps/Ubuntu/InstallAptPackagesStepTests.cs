namespace Onboard.Core.Tests.Steps.Ubuntu;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Ubuntu;

[TestFixture]
public class InstallAptPackagesStepTests
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
    public async Task ShouldExecuteAsync_WhenBuildEssentialMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dpkg", "-s build-essential"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not installed"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenBuildEssentialInstalled_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dpkg", "-s build-essential"))
            .ReturnsAsync(new ProcessResult(0, "Status: install ok installed", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenInstallSucceeds_WritesSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("sudo", "apt-get install -y git gh curl chezmoi python3 build-essential"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteSuccess("Apt packages installed."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenInstallFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("sudo", "apt-get install -y git gh curl chezmoi python3 build-essential"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private InstallAptPackagesStep CreateStep()
    {
        return new InstallAptPackagesStep(processRunner.Object, userInteraction.Object);
    }
}
