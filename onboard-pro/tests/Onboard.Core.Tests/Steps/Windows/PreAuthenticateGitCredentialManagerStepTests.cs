namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.IO;
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
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"gh-cli-{Guid.NewGuid():N}");
        string cliPath = Path.Combine(tempDirectory, "gh.exe");
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(cliPath, string.Empty);

        try
        {
            var sequence = new MockSequence();
            processRunner
                .InSequence(sequence)
                .Setup(runner => runner.RunAsync("where", "gh.exe"))
                .ReturnsAsync(new ProcessResult(0, $"{cliPath}\r\n", string.Empty));

            processRunner
                .InSequence(sequence)
                .Setup(runner => runner.RunAsync(cliPath, "auth login --hostname github.com --git-protocol https --web", false, true))
                .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

            userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
            userInteraction.Setup(ui => ui.WriteSuccess("Git Credential Manager authenticated with GitHub."));

            var step = CreateStep();
            await step.ExecuteAsync().ConfigureAwait(false);

            processRunner.VerifyAll();
            userInteraction.VerifyAll();
            Assert.That(configuration.GitHubCliPath, Is.EqualTo(cliPath));
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
    public void ExecuteAsync_WhenLoginFails_Throws()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"gh-cli-{Guid.NewGuid():N}");
        string cliPath = Path.Combine(tempDirectory, "gh.exe");
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(cliPath, string.Empty);

        try
        {
            var sequence = new MockSequence();
            processRunner
                .InSequence(sequence)
                .Setup(runner => runner.RunAsync("where", "gh.exe"))
                .ReturnsAsync(new ProcessResult(0, $"{cliPath}\r\n", string.Empty));

            processRunner
                .InSequence(sequence)
                .Setup(runner => runner.RunAsync(cliPath, "auth login --hostname github.com --git-protocol https --web", false, true))
                .ReturnsAsync(new ProcessResult(1, string.Empty, "login failed"));

            userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));

            var step = CreateStep();
            Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
            processRunner.VerifyAll();
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
    public async Task ExecuteAsync_WhenCliPathCached_UsesCachedPath()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"gh-{Guid.NewGuid():N}.exe");
        await File.WriteAllTextAsync(tempFile, string.Empty).ConfigureAwait(false);

        try
        {
            configuration.GitHubCliPath = tempFile;

            processRunner
                .Setup(runner => runner.RunAsync(tempFile, "auth login --hostname github.com --git-protocol https --web", false, true))
                .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

            userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
            userInteraction.Setup(ui => ui.WriteSuccess("Git Credential Manager authenticated with GitHub."));

            var step = CreateStep();
            await step.ExecuteAsync().ConfigureAwait(false);

            processRunner.VerifyAll();
            userInteraction.VerifyAll();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_WhenCliPathCannotBeResolved_FallsBackToGhOnPath()
    {
        var sequence = new MockSequence();
        processRunner
            .InSequence(sequence)
            .Setup(runner => runner.RunAsync("where", "gh.exe"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

        processRunner
            .InSequence(sequence)
            .Setup(runner => runner.RunAsync("gh", "auth login --hostname github.com --git-protocol https --web", false, true))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
        userInteraction.Setup(ui => ui.WriteSuccess("Git Credential Manager authenticated with GitHub."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
        Assert.That(configuration.GitHubCliPath, Is.Null);
    }

    private PreAuthenticateGitCredentialManagerStep CreateStep()
    {
        return new PreAuthenticateGitCredentialManagerStep(processRunner.Object, userInteraction.Object, configuration);
    }
}
