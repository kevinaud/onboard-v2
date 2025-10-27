namespace Onboard.Core.Tests.Steps.WslGuest;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.WslGuest;

[TestFixture]
public class ConfigureWslGitCredentialHelperStepTests
{
    private const string HelperPath = "/mnt/c/Program Files/Git/mingw64/bin/git-credential-manager.exe";

    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenHelperNotConfigured_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("git", "config --global credential.helper"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not set"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenHelperDiffers_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("git", "config --global credential.helper"))
            .ReturnsAsync(new ProcessResult(0, "/usr/bin/cache", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenHelperMatches_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("git", "config --global credential.helper"))
            .ReturnsAsync(new ProcessResult(0, HelperPath + "\n", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenConfigurationSucceeds_WritesSuccessMessage()
    {
        processRunner
            .Setup(runner => runner.RunAsync("git", $"config --global credential.helper \"{HelperPath}\""))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteSuccess("Configured Git credential helper for Windows GCM."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenConfigurationFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("git", $"config --global credential.helper \"{HelperPath}\""))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private ConfigureWslGitCredentialHelperStep CreateStep()
    {
        return new ConfigureWslGitCredentialHelperStep(processRunner.Object, userInteraction.Object);
    }
}
