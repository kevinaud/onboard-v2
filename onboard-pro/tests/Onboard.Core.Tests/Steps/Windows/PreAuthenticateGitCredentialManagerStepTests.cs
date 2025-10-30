namespace Onboard.Core.Tests.Steps.Windows;

using System.Threading.Tasks;

using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;

using Moq;

[TestFixture]
public class PreAuthenticateGitCredentialManagerStepTests
{
    private const string GcmPath = "C:/gcm.exe";

    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;
    private OnboardingConfiguration configuration = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
        configuration = new OnboardingConfiguration { GitCredentialManagerPath = GcmPath };
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenCredentialExists_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("C:/gcm.exe"))))
            .ReturnsAsync(new ProcessResult(0, "username=someone\r\npassword=12345", string.Empty));

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenCredentialMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.IsAny<string>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenLoginSucceeds_WritesSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("gh", "auth login --hostname github.com --git-protocol https --web", false, true))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
        userInteraction.Setup(ui => ui.WriteSuccess("Git Credential Manager authenticated with GitHub."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenLoginFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("gh", "auth login --hostname github.com --git-protocol https --web", false, true))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "login failed"));

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private PreAuthenticateGitCredentialManagerStep CreateStep()
    {
        return new PreAuthenticateGitCredentialManagerStep(processRunner.Object, userInteraction.Object, configuration);
    }
}
