namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;
using Moq;

[TestFixture]
public class InstallGitHubCliStepTests
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
  public async Task ShouldExecuteAsync_WhenCliAlreadyInstalled_ReturnsFalse()
  {
    processRunner
      .Setup(runner => runner.RunAsync("where", "gh.exe"))
      .ReturnsAsync(new ProcessResult(0, "C:/gh.exe", string.Empty));

    var step = CreateStep();
    bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(shouldExecute, Is.False);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenCliMissing_ReturnsTrue()
  {
    processRunner
      .Setup(runner => runner.RunAsync("where", "gh.exe"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

    var step = CreateStep();
    bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(shouldExecute, Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenWingetSucceeds_WritesSuccess()
  {
    string originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    string tempDirectory = Path.Combine(Path.GetTempPath(), $"gh-cli-{Guid.NewGuid():N}");
    string cliPath = Path.Combine(tempDirectory, "gh.exe");

    try
    {
      Directory.CreateDirectory(tempDirectory);
      await File.WriteAllTextAsync(cliPath, string.Empty).ConfigureAwait(false);

      processRunner
        .Setup(runner =>
          runner.RunAsync(
            "winget",
            It.Is<string>(args => args.Contains("GitHub.cli", StringComparison.OrdinalIgnoreCase))
          )
        )
        .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
      environmentRefresher
        .Setup(refresher => refresher.RefreshAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
      processRunner
        .Setup(runner => runner.RunAsync("where", "gh.exe"))
        .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
      userInteraction.Setup(ui => ui.WriteSuccess("GitHub CLI installed via winget."));

      var step = CreateStep();
      await step.ExecuteAsync().ConfigureAwait(false);

      processRunner.VerifyAll();
      userInteraction.VerifyAll();
      environmentRefresher.VerifyAll();
      Assert.That(configuration.GitHubCliPath, Is.EqualTo(cliPath));
      string updatedPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
      bool containsCliDirectory = updatedPath
        .Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Any(segment =>
          string.Equals(segment.Trim(), Path.GetDirectoryName(cliPath), StringComparison.OrdinalIgnoreCase)
        );
      Assert.That(containsCliDirectory, Is.True, "PATH should include the GitHub CLI installation directory.");
    }
    finally
    {
      Environment.SetEnvironmentVariable("PATH", originalPath);
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
  public void ExecuteAsync_WhenWingetFails_Throws()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "winget",
          It.Is<string>(args => args.Contains("GitHub.cli", StringComparison.OrdinalIgnoreCase))
        )
      )
      .ReturnsAsync(new ProcessResult(1, string.Empty, "failed"));

    var step = CreateStep();

    Assert.That(
      async () => await step.ExecuteAsync().ConfigureAwait(false),
      Throws.TypeOf<InvalidOperationException>()
    );
    processRunner.VerifyAll();
  }

  private InstallGitHubCliStep CreateStep()
  {
    return new InstallGitHubCliStep(
      processRunner.Object,
      userInteraction.Object,
      configuration,
      environmentRefresher.Object
    );
  }
}
