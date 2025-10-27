namespace Onboard.Core.Tests.Steps.WslGuest;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.WslGuest;

[TestFixture]
public class AptUpdateStepTests
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
    public async Task ShouldExecuteAsync_AlwaysReturnsTrue()
    {
        var step = CreateStep();

        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_WhenAptUpdateSucceeds_WritesSuccessMessage()
    {
        processRunner
            .Setup(runner => runner.RunAsync("sudo", "apt-get update"))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteSuccess("APT package lists updated."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenAptUpdateFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("sudo", "apt-get update"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private AptUpdateStep CreateStep()
    {
        return new AptUpdateStep(processRunner.Object, userInteraction.Object);
    }
}
